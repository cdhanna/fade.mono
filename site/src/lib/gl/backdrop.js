// The sea behind the page: one WebGL2 canvas, fixed behind everything. The sea and the fish are drawn at the
// game's own chunky size into a small picture, and that goes through the game's screen effect onto the
// canvas, as it does in the game. See shaders.js for which effect each pass is.
//
// The page does not touch it: the pointer does nothing, and neither does a click.
// The ripples of light are on a second canvas, in front of the page: the game has them over everything.
// Going down the page is a dive: the camera sinks, the ridges come up, and the day turns to night.

import { QUAD_VS, SEA_FS, SPRITE_VS, SPRITE_FS, SCREEN_FS, LIGHT_FS } from './shaders.js';

// the art, as Fade.MonoGame/Assets/Fish/Textures/Art32 has it. Bass is the seahorse.
const ART = ['Goldfish', 'Catfish', 'Angelfish', 'Bass', 'Pufferfish', 'Jellyfish', 'Crab'];
const CELL = 32;
const ATLAS_W = 4, ATLAS_H = 2;

// THE SCHOOLS IN THE BACKGROUND in fish_headers_school, per second, with the distances and speeds cut down:
// these are farther off than the game's, which are 11 pixels to these 7
const FLOCK = {
  speedMin: 3, speedMax: 10, near: 34, gap: 7, avoid: 26, edge: 24,
  cohere: 0.3, align: 0.5, pullGap: 9, pullAvoid: 14, home: 0.04, pullEdge: 0.6,
};
const STEER_MAX = 4; // how hard a fish can steer, per second. far off, it comes around slowly
const FACE_EASE = 3; // how quickly it turns to face the other way, per second
const HAZE = [165 / 255, 195 / 255, 230 / 255];
const FISH_ALPHA = 0.5; // how much of a far fish shows through the water
const MAX_INSTANCES = 256;

function compile(gl, vs, fs) {
  const make = (type, src) => {
    const sh = gl.createShader(type);
    gl.shaderSource(sh, src);
    gl.compileShader(sh);
    if (!gl.getShaderParameter(sh, gl.COMPILE_STATUS)) throw new Error(gl.getShaderInfoLog(sh));
    return sh;
  };
  const p = gl.createProgram();
  gl.attachShader(p, make(gl.VERTEX_SHADER, vs));
  gl.attachShader(p, make(gl.FRAGMENT_SHADER, fs));
  gl.linkProgram(p);
  if (!gl.getProgramParameter(p, gl.LINK_STATUS)) throw new Error(gl.getProgramInfoLog(p));
  const u = {};
  const n = gl.getProgramParameter(p, gl.ACTIVE_UNIFORMS);
  for (let i = 0; i < n; i++) {
    const info = gl.getActiveUniform(p, i);
    u[info.name.replace(/\[0\]$/, '')] = gl.getUniformLocation(p, info.name);
  }
  return { p, u };
}

function loadAtlas(gl, base) {
  const canvas = document.createElement('canvas');
  canvas.width = ATLAS_W * CELL;
  canvas.height = ATLAS_H * CELL;
  const ctx = canvas.getContext('2d');
  const tex = gl.createTexture();
  const upload = () => {
    gl.bindTexture(gl.TEXTURE_2D, tex);
    gl.pixelStorei(gl.UNPACK_FLIP_Y_WEBGL, false);
    gl.pixelStorei(gl.UNPACK_PREMULTIPLY_ALPHA_WEBGL, true);
    gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA8, gl.RGBA, gl.UNSIGNED_BYTE, canvas);
    gl.pixelStorei(gl.UNPACK_PREMULTIPLY_ALPHA_WEBGL, false);
    // smooth, at the size of a far fish, so that it glides over the pixels. Two levels: any more and the
    // cells of the atlas run into each other
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAX_LEVEL, 2);
    gl.generateMipmap(gl.TEXTURE_2D);
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.LINEAR_MIPMAP_LINEAR);
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.LINEAR);
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.CLAMP_TO_EDGE);
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.CLAMP_TO_EDGE);
  };
  upload();
  const loads = ART.map((name, i) => new Promise((done) => {
    const img = new Image();
    img.onload = () => {
      ctx.drawImage(img, (i % ATLAS_W) * CELL, Math.floor(i / ATLAS_W) * CELL);
      done(true);
    };
    img.onerror = () => done(false);
    img.src = `${base}media/fish/${name}.png`;
  }));
  const ready = Promise.all(loads).then((ok) => {
    upload();
    return ok.every(Boolean);
  });
  return { tex, ready };
}

// a pixel-sized target of the sea, read with no smoothing
function target(gl, w, h) {
  const tex = gl.createTexture();
  gl.bindTexture(gl.TEXTURE_2D, tex);
  gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA8, w, h, 0, gl.RGBA, gl.UNSIGNED_BYTE, null);
  gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.NEAREST);
  gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.NEAREST);
  gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.MIRRORED_REPEAT);
  gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.CLAMP_TO_EDGE);
  const fb = gl.createFramebuffer();
  gl.bindFramebuffer(gl.FRAMEBUFFER, fb);
  gl.framebufferTexture2D(gl.FRAMEBUFFER, gl.COLOR_ATTACHMENT0, gl.TEXTURE_2D, tex, 0);
  return { tex, fb, w, h };
}

// the ripples of light, on their own canvas, which the page blends over itself
function startLight(canvas) {
  const gl = canvas?.getContext('webgl2', { alpha: false, antialias: false, depth: false, stencil: false, powerPreference: 'low-power' });
  if (!gl) return null;
  const { p, u } = compile(gl, QUAD_VS, LIGHT_FS);
  gl.bindBuffer(gl.ARRAY_BUFFER, gl.createBuffer());
  gl.bufferData(gl.ARRAY_BUFFER, new Float32Array([-1, -1, 1, -1, -1, 1, 1, 1]), gl.STATIC_DRAW);
  gl.enableVertexAttribArray(0);
  gl.vertexAttribPointer(0, 2, gl.FLOAT, false, 0, 0);
  gl.useProgram(p);
  return {
    // at twice the sea's pixels: in the game they are finer than the art
    resize(w, h) {
      canvas.width = w * 2;
      canvas.height = h * 2;
      gl.viewport(0, 0, w * 2, h * 2);
      gl.uniform2f(u.Size, w, h);
    },
    draw(time) {
      gl.uniform1f(u.Time, time);
      gl.drawArrays(gl.TRIANGLE_STRIP, 0, 4);
    },
  };
}

/**
 * Starts the sea on canvas, and the ripples of light on lightCanvas. Resolves to null without WebGL2 (the
 * page keeps its still picture), else to { stop, setDepth, scrollBy }.
 */
export async function startBackdrop(canvas, { lightCanvas = null, still = false, base = '/' } = {}) {
  const gl = canvas.getContext('webgl2', { alpha: false, antialias: false, depth: false, stencil: false, powerPreference: 'low-power' });
  if (!gl) return null;

  let sea, sprite, screen, light;
  try {
    sea = compile(gl, QUAD_VS, SEA_FS);
    sprite = compile(gl, SPRITE_VS, SPRITE_FS);
    screen = compile(gl, QUAD_VS, SCREEN_FS);
    light = startLight(lightCanvas);
  } catch (e) {
    console.warn('backdrop: a shader did not compile', e);
    return null;
  }

  const quad = gl.createBuffer();
  gl.bindBuffer(gl.ARRAY_BUFFER, quad);
  gl.bufferData(gl.ARRAY_BUFFER, new Float32Array([-1, -1, 1, -1, -1, 1, 1, 1]), gl.STATIC_DRAW);
  const quadVao = gl.createVertexArray();
  gl.bindVertexArray(quadVao);
  gl.enableVertexAttribArray(0);
  gl.vertexAttribPointer(0, 2, gl.FLOAT, false, 0, 0);

  const instanceData = new Float32Array(MAX_INSTANCES * 12);
  const instances = gl.createBuffer();
  const spriteVao = gl.createVertexArray();
  gl.bindVertexArray(spriteVao);
  gl.bindBuffer(gl.ARRAY_BUFFER, quad);
  gl.enableVertexAttribArray(0);
  gl.vertexAttribPointer(0, 2, gl.FLOAT, false, 0, 0);
  gl.bindBuffer(gl.ARRAY_BUFFER, instances);
  gl.bufferData(gl.ARRAY_BUFFER, instanceData.byteLength, gl.DYNAMIC_DRAW);
  for (let i = 0; i < 3; i++) {
    gl.enableVertexAttribArray(i + 1);
    gl.vertexAttribPointer(i + 1, 4, gl.FLOAT, false, 48, i * 16);
    gl.vertexAttribDivisor(i + 1, 1);
  }
  gl.bindVertexArray(null);

  const atlas = loadAtlas(gl, base);
  let hasArt = false;

  // ---- sizes -------------------------------------------------------------------------------------
  // the game is 320 pixels tall; the page keeps its pixels about that big, and every pixel a whole number of
  // CSS pixels. A narrow phone gets smaller ones, or there would be no room for a school.
  let world = null;
  let shoal = null; // the fish, which the sea takes in behind its ridges
  let scale = 3;
  let cssW = 1, cssH = 1;
  let quality = Math.min(window.devicePixelRatio || 1, 1.5);

  function resize() {
    cssW = Math.max(1, canvas.clientWidth || window.innerWidth);
    cssH = Math.max(1, canvas.clientHeight || window.innerHeight);
    canvas.width = Math.round(cssW * quality);
    canvas.height = Math.round(cssH * quality);
    scale = Math.max(2, Math.round(Math.min(cssH / 320, cssW / 300)));
    const w = Math.ceil(cssW / scale), h = Math.ceil(cssH / scale);
    if (!world || world.w !== w || world.h !== h) {
      for (const old of [world, shoal]) if (old) { gl.deleteTexture(old.tex); gl.deleteFramebuffer(old.fb); }
      world = target(gl, w, h);
      shoal = target(gl, w, h);
      light?.resize(w, h);
    }
  }
  resize();

  // ---- the schools -------------------------------------------------------------------------------
  const rnd = (a, b) => a + Math.random() * (b - a);
  const fish = [];
  const schools = [];

  function addSchool(kind, n, size, x, y) {
    const school = { kind, x, y };
    schools.push(school);
    for (let i = 0; i < n; i++) {
      const a = rnd(0, Math.PI * 2);
      fish.push({
        school, kind, size,
        x: x + rnd(-16, 16), y: y + rnd(-10, 10),
        vx: Math.cos(a) * FLOCK.speedMin, vy: Math.sin(a) * FLOCK.speedMin,
        face: 1, want: 1,
      });
    }
  }
  function placeSchools() {
    const w = world.w, h = world.h;
    const room = Math.max(1, Math.round((w * h) / (570 * 320)));
    // a phone has room for the words or for the sea, and the words come first
    if (w <= 400) {
      addSchool(2, 5, 7, w * 0.5, h * 0.14);
      addSchool(0, 5, 7, w * 0.5, h * 0.5);
      return;
    }
    addSchool(0, 9, 7, w * 0.2, h * 0.28);
    addSchool(1, 8, 7, w * 0.76, h * 0.5);
    addSchool(2, 8, 7, w * 0.82, h * 0.18);
    addSchool(4, 3, 9, w * 0.12, h * 0.48);
    addSchool(3, 1, 10, w * 0.3, h * 0.56);
    addSchool(5, 1, 10, w * 0.9, h * 0.36);
    if (room > 1) addSchool(0, 9, 7, w * 0.64, h * 0.34);
  }
  placeSchools();

  function stepFish(dt) {
    for (const f of fish) {
      let cx = 0, cy = 0, ax = 0, ay = 0, near = 0, sx = 0, sy = 0;
      for (const o of fish) {
        if (o === f) continue;
        const dx = o.x - f.x, dy = o.y - f.y;
        const d = Math.hypot(dx, dy) || 1;
        if (o.kind === f.kind) {
          if (d < FLOCK.near) { cx += o.x; cy += o.y; ax += o.vx; ay += o.vy; near++; }
          if (d < FLOCK.gap) { const k = (FLOCK.gap - d) / FLOCK.gap; sx -= (dx / d) * k * FLOCK.pullGap * 10; sy -= (dy / d) * k * FLOCK.pullGap * 10; }
        } else if (d < FLOCK.avoid) {
          const k = (FLOCK.avoid - d) / FLOCK.avoid;
          sx -= (dx / d) * k * FLOCK.pullAvoid; sy -= (dy / d) * k * FLOCK.pullAvoid;
        }
      }
      if (near) {
        sx += (cx / near - f.x) * FLOCK.cohere + (ax / near - f.vx) * FLOCK.align;
        sy += (cy / near - f.y) * FLOCK.cohere + (ay / near - f.vy) * FLOCK.align;
      }
      // stay about with its own school, which wanders
      sx += (f.school.x - f.x) * FLOCK.home;
      sy += (f.school.y - f.y) * FLOCK.home;
      const e = FLOCK.edge;
      if (f.x < e) sx += (e - f.x) * FLOCK.pullEdge * 4;
      if (f.x > world.w - e) sx -= (f.x - world.w + e) * FLOCK.pullEdge * 4;
      if (f.y < e + 8) sy += (e + 8 - f.y) * FLOCK.pullEdge * 4;
      if (f.y > world.h - e) sy -= (f.y - world.h + e) * FLOCK.pullEdge * 4;

      // the words and the panels are down the middle of the page, so the margins are where a fish can be seen
      if (world.w > 400) {
        const clear = world.w * 0.3;
        const off = f.x - world.w / 2;
        if (Math.abs(off) < clear) sx += Math.sign(off || 1) * (clear - Math.abs(off)) * 1.2;
      }

      const pull = Math.hypot(sx, sy);
      if (pull > STEER_MAX) { sx *= STEER_MAX / pull; sy *= STEER_MAX / pull; }

      f.vx += sx * dt;
      f.vy += sy * dt;
      const speed = Math.hypot(f.vx, f.vy) || 1;
      const clamped = Math.max(FLOCK.speedMin, Math.min(FLOCK.speedMax, speed));
      f.vx *= clamped / speed;
      f.vy *= clamped / speed;
      f.x += f.vx * dt;
      f.y += f.vy * dt;
      if (Math.abs(f.vx) > 1.5) f.want = f.vx < 0 ? -1 : 1;
      f.face += (f.want - f.face) * Math.min(1, dt * FACE_EASE);
    }
    for (const s of schools) {
      s.x += Math.sin(time * 0.13 + s.kind * 2.1) * 3 * dt;
      s.y += Math.cos(time * 0.09 + s.kind * 1.3) * 1.5 * dt;
      s.x = Math.max(30, Math.min(world.w - 30, s.x));
      if (world.w > 400 && Math.abs(s.x - world.w / 2) < world.w * 0.32) s.x = world.w / 2 + Math.sign(s.x - world.w / 2 || 1) * world.w * 0.32;
      // up in the open water: the ridges hide what is below
      s.y = Math.max(40, Math.min(world.h * 0.6, s.y));
    }
  }

  // ---- what the page feeds in --------------------------------------------------------------------
  let depth = 0;
  let depthNow = 0;
  function setDepth(t) {
    depth = t;
    pokeFrame();
  }
  function scrollBy(dy) {
    // the fish are a long way off, so they barely go by
    const shift = (dy / scale) * 0.03;
    for (const f of fish) f.y -= shift;
  }

  // ---- drawing -----------------------------------------------------------------------------------
  let time = rnd(0, 200);
  const motion = still ? 0 : 1;

  function drawSea() {
    gl.bindFramebuffer(gl.FRAMEBUFFER, world.fb);
    gl.viewport(0, 0, world.w, world.h);
    gl.disable(gl.BLEND);
    gl.useProgram(sea.p);
    const u = sea.u;
    gl.uniform2f(u.Size, world.w, world.h);
    gl.uniform1f(u.Time, time);
    gl.uniform1f(u.Night, Math.min(1, Math.max(0, (depthNow - 0.25) / 0.6)));
    gl.uniform1f(u.Descent, depthNow * 0.55);
    gl.uniform1f(u.Water, 0.5 + 0.5 * Math.sin(time * 0.12));
    gl.activeTexture(gl.TEXTURE0);
    gl.bindTexture(gl.TEXTURE_2D, shoal.tex);
    gl.uniform1i(u.Shoal, 0);
    gl.bindVertexArray(quadVao);
    gl.drawArrays(gl.TRIANGLE_STRIP, 0, 4);
  }

  // the fish, by themselves, for the sea to take in
  function drawFish() {
    gl.bindFramebuffer(gl.FRAMEBUFFER, shoal.fb);
    gl.viewport(0, 0, shoal.w, shoal.h);
    gl.clearColor(0, 0, 0, 0);
    gl.clear(gl.COLOR_BUFFER_BIT);
    const night = Math.min(1, Math.max(0, (depthNow - 0.25) / 0.6));
    const dim = [1 - 0.35 * night, 1 - 0.28 * night, 1 - 0.12 * night];
    const tint = HAZE.map((c, i) => c * dim[i]);
    if (!hasArt) return;
    fish.forEach((f, n) => {
      instanceData.set([f.x, f.y, f.size, f.face,
        f.kind % ATLAS_W, Math.floor(f.kind / ATLAS_W), 0.8, 0,
        tint[0], tint[1], tint[2], FISH_ALPHA], n * 12);
    });
    gl.useProgram(sprite.p);
    gl.uniform2f(sprite.u.Size, world.w, world.h);
    gl.uniform2f(sprite.u.Cells, ATLAS_W, ATLAS_H);
    gl.activeTexture(gl.TEXTURE0);
    gl.bindTexture(gl.TEXTURE_2D, atlas.tex);
    gl.uniform1i(sprite.u.Atlas, 0);
    gl.enable(gl.BLEND);
    gl.blendFuncSeparate(gl.SRC_ALPHA, gl.ONE_MINUS_SRC_ALPHA, gl.ONE, gl.ONE_MINUS_SRC_ALPHA);
    gl.bindVertexArray(spriteVao);
    gl.bindBuffer(gl.ARRAY_BUFFER, instances);
    gl.bufferSubData(gl.ARRAY_BUFFER, 0, instanceData.subarray(0, fish.length * 12));
    gl.drawArraysInstanced(gl.TRIANGLE_STRIP, 0, 4, fish.length);
    gl.disable(gl.BLEND);
  }

  function drawScreen() {
    gl.bindFramebuffer(gl.FRAMEBUFFER, null);
    gl.viewport(0, 0, canvas.width, canvas.height);
    gl.useProgram(screen.p);
    const u = screen.u;
    gl.uniform2f(u.Size, world.w, world.h);
    gl.uniform1f(u.Time, time);
    gl.uniform1f(u.Motion, motion || 0.0001);
    gl.activeTexture(gl.TEXTURE0);
    gl.bindTexture(gl.TEXTURE_2D, world.tex);
    gl.uniform1i(u.Sea, 0);
    gl.bindVertexArray(quadVao);
    gl.drawArrays(gl.TRIANGLE_STRIP, 0, 4);
  }

  function render() {
    drawFish();
    drawSea();
    drawScreen();
    light?.draw(time);
  }

  function tick(dt) {
    time += dt;
    depthNow += (depth - depthNow) * Math.min(1, dt * 5);
    stepFish(dt);
  }

  // ---- the loop ----------------------------------------------------------------------------------
  let running = true;
  let raf = 0;
  let last = performance.now();
  let acc = 0;
  let slow = 0;

  function loop(now) {
    if (!running) return;
    raf = requestAnimationFrame(loop);
    const elapsed = Math.min(now - last, 100);
    last = now;
    acc += elapsed / 1000;
    // a slow device: the water at a lower resolution. It is pixels anyway.
    slow = slow * 0.95 + (elapsed > 26 ? 1 : 0) * 0.05;
    if (slow > 0.6 && quality > 0.5) {
      quality = Math.max(0.5, quality * 0.7);
      slow = 0;
      resize();
    }
    let steps = 0;
    while (acc >= 1 / 60 && steps < 3) {
      acc -= 1 / 60;
      tick(1 / 60);
      steps++;
    }
    if (acc > 0.2) acc = 0;
    render();
  }

  atlas.ready.then((ok) => {
    hasArt = ok;
    if (still) pokeFrame();
  });

  // with reduced motion there is one picture, drawn again when something changes it
  let pending = 0;
  function pokeFrame() {
    if (!still || pending) return;
    pending = requestAnimationFrame(() => {
      pending = 0;
      depthNow = depth;
      render();
    });
  }

  function onResize() {
    resize();
    pokeFrame();
  }
  window.addEventListener('resize', onResize);

  function onVisibility() {
    if (still) return;
    if (document.hidden) {
      running = false;
      cancelAnimationFrame(raf);
    } else if (!running) {
      running = true;
      last = performance.now();
      raf = requestAnimationFrame(loop);
    }
  }
  document.addEventListener('visibilitychange', onVisibility);

  // a moment of the sea already going
  for (let i = 0; i < 120; i++) tick(1 / 60);
  await atlas.ready;
  if (still) {
    running = false;
    render();
  } else {
    raf = requestAnimationFrame(loop);
  }

  return {
    setDepth,
    scrollBy,
    stop() {
      running = false;
      cancelAnimationFrame(raf);
      window.removeEventListener('resize', onResize);
      document.removeEventListener('visibilitychange', onVisibility);
    },
  };
}
