// The game's sea, in WebGL2. These are ports of the game's own effects, in Fade.MonoGame/Assets/Fish/Shaders,
// and the numbers are the game's. Change them there first, and bring them over.
//
//   SEA_FS      SeabedEffect.fx (the water, the shafts of light, the ridges, the surface, the moon, the
//               plankton of the night) and SpecksEffect.fx (the specks), drawn together at the game's own
//               size. The fish go in behind the farthest ridge
//   SPRITE_*    the fish (FlockEffect.fx), drawn first for SEA_FS to take in
//   SCREEN_FS   ScreenEffect.fx: the wobble of the water and the dark corners, in steps with a checkerboard
//               between them
//   LIGHT_FS    ScreenEffect.fx's ripples of light. In the game they go over everything on the screen, so
//               here they are a canvas of their own, on top of the page
//
// What is not the game's is Descent, Night comes from the scroll and not the tide, and the sizes: the page
// can be any shape, so Size is what fits it, and the wobble is worked out in the game's 570 x 320.

const HEAD = `#version 300 es
precision highp float;
#define saturate(x) clamp(x, 0.0, 1.0)
`;

export const QUAD_VS = `#version 300 es
layout(location = 0) in vec2 aPos;
out vec2 vUv;
void main() {
  // down the screen, as the game's uv goes
  vUv = vec2(aPos.x * 0.5 + 0.5, 0.5 - aPos.y * 0.5);
  gl_Position = vec4(aPos, 0.0, 1.0);
}`;

const COMMON = `
float hash(vec2 p) {
  vec3 p3 = fract(vec3(p.x, p.y, p.x) * 0.1031);
  p3 += dot(p3, p3.yzx + 33.33);
  return fract((p3.x + p3.y) * p3.z);
}
// cut a smooth 0 to 1 into steps, with a checkerboard between one step and the next
float bands(float v, float count, vec2 px) {
  float checker = fract((px.x + px.y) * 0.5) * 2.0;
  return floor(v * count + checker * 0.5) / count;
}
`;


export const SEA_FS = HEAD + `
in vec2 vUv;
out vec4 outColor;
uniform float Time;
// no run of pops here: the water stays as it is between them
#define Clock Time
#define Mood 0.0
#define Rainbow 0.0
uniform float Night;    // the page going down: 0 is the day, 1 the night (Tide.y)
uniform float Descent;  // how far down the page has gone, as a share of the screen
uniform float Water;    // where the tide is (Tide.x)
uniform vec2 Size;
uniform sampler2D Shoal; // the fish, far off, with their alpha multiplied in
${COMMON}
#define WaterTop vec3(0.090, 0.290, 0.410)
#define WaterLow vec3(0.055, 0.115, 0.250)
#define RayColor vec3(0.450, 0.780, 0.800)
#define NightTop vec3(0.030, 0.080, 0.180)
#define NightLow vec3(0.008, 0.018, 0.065)
#define MoonRay vec3(0.520, 0.640, 0.950)
#define PlanktonColor vec3(0.200, 0.950, 0.820)
#define SURFACE_HIGH 1.0
#define SURFACE_LOW 10.0
#define SkyDay vec3(0.135, 0.375, 0.495)
#define SkyNight vec3(0.042, 0.105, 0.225)
#define FoamColor vec3(0.300, 0.560, 0.660)
#define MOON_ACROSS 0.762
#define MOON_DOWN 46.0
#define MOON_SIZE 9.0
#define MoonColor vec3(0.930, 0.950, 1.000)
#define RaySlant 0.55
#define FarBase 0.640
#define FarRough 46.0
#define MidBase 0.800
#define MidRough 40.0
#define NearBase 0.945
#define NearRough 26.0
#define CamSway 16.0
#define SnowColor vec3(0.620, 0.780, 0.820)

float noise1(float x, float seed) {
  float i = floor(x);
  float f = fract(x);
  f = f * f * (3.0 - 2.0 * f);
  return mix(hash(vec2(i, seed)), hash(vec2(i + 1.0, seed)), f);
}
// how far down the screen the top of a ridge is. the page going down lifts it, the near ones more
float ground(float x, float base, float rough, float seed, float lift) {
  float n = noise1(x * 0.011, seed) * 0.6 + noise1(x * 0.034, seed + 3.0) * 0.28 + noise1(x * 0.097, seed + 7.0) * 0.12;
  return floor(base * Size.y + (0.5 - n) * rough - lift);
}
float depthAt(vec2 px) { return px.y + Descent * Size.y * 0.7; }
vec3 water(vec2 px) {
  float v = bands(saturate(depthAt(px) / Size.y), 18.0, px);
  float deep = smoothstep(0.0, 0.9, v);
  return mix(mix(WaterTop, WaterLow, deep), mix(NightTop, NightLow, deep), saturate(Night));
}
float moon(vec2 px, vec2 middle) {
  return step(length(px - middle), MOON_SIZE);
}
vec3 rainbow(float hue) {
  vec3 color = saturate(abs(fract(hue + vec3(0.0, 0.6667, 0.3333)) * 6.0 - 3.0) - 1.0);
  return mix(vec3(1.0), color, 0.7);
}
float snow(vec2 px, float square, vec2 speed, float seed, float share, float size) {
  vec2 q = px + 0.5 - Clock * speed;
  q.x += sin(Clock * 0.45 + px.y * 0.03 + seed) * (2.5 + 4.0 * Mood);
  vec2 cell = floor(q / square);
  vec2 spot = size + vec2(hash(cell + seed), hash(cell + seed + 17.0)) * (square - 2.0 * size);
  vec2 away = abs(q - cell * square - spot);
  float hit = saturate(size - away.x) * saturate(size - away.y) * step(hash(cell + seed + 5.0), share);
  float bright = 0.35 + 0.65 * hash(cell + seed + 9.0);
  float twinkle = 0.75 + 0.25 * sin(Time * 1.1 + hash(cell + seed + 3.0) * 40.0);
  return hit * bright * twinkle;
}

void main() {
  vec2 px = floor(vUv * Size);
  float lift = Descent * Size.y;
  float night = saturate(Night);
  float sway = sin(Time * 0.07) * CamSway * 0.6 + sin(Time * 0.031 + 1.0) * CamSway * 0.4;
  float across = px.x - px.y * RaySlant + floor(sway * 0.15);
  float cloud = sin(across * 0.0045 + Time * 0.11) * 0.6 + sin(across * 0.0021 - Time * 0.06 + 2.0) * 0.4;
  float light = (0.85 + 0.4 * Mood) * (1.0 + 0.1 * cloud);

  vec3 sea = water(px) * light;
  vec3 color = sea;
  vec3 rock = water(vec2(px.x, Size.y)) * light * 0.45;

  // the fish, behind every ridge and under the light
  vec4 shoal = texelFetch(Shoal, ivec2(px.x, Size.y - 1.0 - px.y), 0);
  color = color * (1.0 - shoal.a) + shoal.rgb;

  float farX = px.x + floor(sway * 0.25);
  float farTop = ground(farX, FarBase, FarRough, 11.0, lift * 0.32);
  color = mix(color, mix(sea, rock, 0.22), step(farTop, px.y));

  float midX = px.x + floor(sway * 0.5) + 300.0;
  float midTop = ground(midX, MidBase, MidRough, 23.0, lift * 0.42);
  vec3 midFog = mix(sea, rock, 0.5);
  float midRock = step(midTop, px.y);
  vec3 midColor = midFog + 0.035 * step(px.y, midTop + 0.5);
  color = mix(color, midColor, midRock);

  float ray = sin(across * 0.019 + Time * 0.10) + 0.7 * sin(across * 0.043 - Time * 0.07 + 1.3) + 0.5 * sin(across * 0.071 + Time * 0.045 + 4.0);
  ray = smoothstep(0.45, 1.9, ray) * (0.85 + 0.15 * sin(Time * 0.6 + across * 0.1));
  float reach = saturate(1.0 - (depthAt(px) / Size.y) * 1.05 + night * 0.35);
  ray = bands(saturate(ray * reach * (0.55 + 1.3 * Mood) * (1.0 + 0.3 * cloud)), 10.0, px);
  color += mix(RayColor * 0.2, MoonRay * 0.11, night) * ray;

  // the little lights of the night
  vec2 grain = floor((px + vec2(sin(Time * 0.2 + px.y * 0.02) * 6.0, Time * 5.0)) / 2.0);
  float plankton = step(0.988, hash(grain + 57.0));
  float twinkle = 0.5 + 0.5 * sin(Time * 1.7 + hash(grain + 3.0) * 40.0);
  color += PlanktonColor * plankton * twinkle * twinkle * 0.55 * night;

  // the moon, seen through the water, with its glow and the shaft of its light
  vec2 moonAt = vec2(floor(Size.x * MOON_ACROSS), MOON_DOWN);
  vec2 bent = px + vec2(floor(sin(px.y * 0.45 + Time * 1.6) * 1.2 + 0.5), 0.0);
  float moonLit = moon(bent, moonAt);
  float moonGlow = saturate(1.0 - length(px - moonAt) / (MOON_SIZE * 3.2));
  float moonFall = saturate(1.0 - abs(px.x - moonAt.x - (px.y - moonAt.y) * 0.12) / (MOON_SIZE * 1.6)) * saturate(1.0 - (px.y - moonAt.y) / (Size.y * 0.8)) * step(moonAt.y, px.y);
  color += MoonRay * bands(saturate(moonGlow * moonGlow * 0.5 + moonFall * 0.22), 8.0, px) * night;
  color = mix(color, MoonColor, moonLit * 0.95 * smoothstep(0.35, 0.9, night));

  // the surface, which goes up off of the top of the screen as the page goes down
  float swell = sin(px.x * 0.050 + Time * 0.9) * 1.6 + sin(px.x * 0.113 - Time * 0.6 + 2.0) * 1.0;
  float surface = floor(mix(SURFACE_LOW, SURFACE_HIGH, saturate(Water)) + swell + 0.5);
  float under = px.y + lift * 1.5 - surface;
  vec3 sky = mix(SkyDay, SkyNight, night) * (0.96 + 0.04 * sin(px.x * 0.21 + Time * 1.3));
  float sheen = bands(saturate(1.0 - under / 9.0), 5.0, px) * step(0.0, under);
  color += mix(RayColor, MoonRay, night) * sheen * 0.05;
  color = mix(color, sky, step(under, -0.5));
  color = mix(color, mix(FoamColor, SkyNight * 1.8, night), step(abs(under), 0.5) * 0.55);

  // SpecksEffect.fx
  float specks = snow(px, 19.0, vec2(1.5, 3.0), 40.0, 0.7 + 0.3 * Mood, 1.0) * 0.5 + snow(px, 31.0, vec2(3.0, 6.0), 80.0, 0.7 + 0.3 * Mood, 1.0 + 0.4 * Mood);
  specks += snow(px, 23.0, vec2(-2.5, 4.5), 160.0, Mood, 1.0) * Mood;
  specks *= 1.0 + 0.8 * Mood;
  vec3 speck = mix(SnowColor, rainbow(px.x * 0.0045 + px.y * 0.0030 - Time * 0.12), Rainbow);
  float bright = mix(0.22, 0.6, Rainbow) * specks;
  color = mix(color, speck, saturate(bright));

  // the sand, in front of the specks
  float nearX = px.x + floor(sway) + 700.0;
  float nearTop = ground(nearX, NearBase, NearRough, 37.0, lift * 0.5);
  vec3 nearFog = mix(sea, rock, 0.8);
  float nearRock = step(nearTop, px.y);
  float grit = step(0.86, hash(vec2(nearX, px.y))) * 0.02;
  vec3 nearColor = nearFog + 0.05 * step(px.y, nearTop + 0.5) + grit;
  color = mix(color, nearColor, nearRock);

  outColor = vec4(color, 1.0);
}`;

// aA: x, y (the middle, in pixels), size in pixels, how wide a fish is (1 facing right, -1 left, and between
// them as it turns). aB: the cell of the atlas (x, y), how far out
// it is (the line fades). aC: the color.
export const SPRITE_VS = `#version 300 es
layout(location = 0) in vec2 aPos;
layout(location = 1) in vec4 aA;
layout(location = 2) in vec4 aB;
layout(location = 3) in vec4 aC;
uniform vec2 Size;
uniform vec2 Cells; // how many cells the atlas has across and down
out vec2 vAtlas;
out vec4 vColor;
flat out float vLine;
void main() {
  vec2 corner = aPos * 0.5 + 0.5;
  // off of the pixels, or a slow fish would go along in jumps
  vec2 at = aA.xy + (corner - 0.5) * aA.z * vec2(aA.w, 1.0);
  vAtlas = (aB.xy + corner) / Cells;
  vColor = aC;
  vLine = aB.z;
  gl_Position = vec4(at / Size * vec2(2.0, -2.0) + vec2(-1.0, 1.0), 0.0, 1.0);
}`;

export const SPRITE_FS = `#version 300 es
precision highp float;
#define saturate(x) clamp(x, 0.0, 1.0)
in vec2 vAtlas;
in vec4 vColor;
flat in float vLine;
out vec4 outColor;
uniform sampler2D Atlas;
#define LINE_DARK 0.16
void main() {
  // FlockEffect.fx: far off, the dark line around a fish is lost in the water
  vec4 c = texture(Atlas, vAtlas, -1.0); // a sharper level than the size wants, or a far fish is a blob
  c.rgb /= max(c.a, 0.001); // the atlas has its alpha multiplied in, for the small sizes to be clean
  float shade = dot(c.rgb, vec3(0.299, 0.587, 0.114));
  c.a *= 1.0 - step(shade, LINE_DARK) * saturate(vLine);
  outColor = c * vColor;
  if (outColor.a <= 0.004) discard;
}`;

// everything in makeWaves that depends only on which wave it is, worked out once, as the game's compiler does
function fract(x) { return x - Math.floor(x); }
function randomVal(v) { return fract(Math.sin(v * 12.9898 + 2523.2361 * 78.233) * 43758.5453) - 0.5; }
function waves(offset) {
  return Array.from({ length: 16 }, (_, n) => {
    const i = n + offset;
    const x = randomVal(i), y = randomVal(i + 151.523);
    const len = Math.hypot(x, y) || 1;
    return [x / len, y / len, randomVal(i + 1.6516), randomVal(i + 123)];
  });
}
const table = (name, rows) => `const vec4 ${name}[16] = vec4[16](${rows.map((r) => `vec4(${r.map((v) => v.toFixed(5)).join(', ')})`).join(', ')});`;

// the ripples of light of makeWaves, and what the wobble needs of them
const LIGHT = `
${table('WAVE_A', waves(0.1))}
${table('WAVE_B', waves(0.26))}
float wavesA(vec2 uv, float t) {
  float result = 0.0;
  for (int n = 0; n < 16; n++) {
    vec4 w = WAVE_A[n];
    float s = sin(dot(uv, w.xy) * w.z + t);
    result += w.w * smoothstep(0.0, 1.0, s);
  }
  return result;
}
float wavesB(vec2 uv, float t) {
  float result = 0.0;
  for (int n = 0; n < 16; n++) {
    vec4 w = WAVE_B[n];
    float s = sin(dot(uv, w.xy) * w.z + t);
    result += w.w * smoothstep(0.0, 1.0, s);
  }
  return result;
}
// x is the ripples of light, y the second set of waves by itself
vec2 lightAt(vec2 fit, float t) {
  vec2 uv2 = fit * 60.0;
  float result1 = wavesA(uv2 + vec2(t, 0.0), t);
  float result2 = wavesB(uv2 - vec2(t * 0.8, 0.0), t * 0.8 + 0.06);
  result1 = smoothstep(0.4, 1.1, 1.0 - abs(result1));
  result2 = smoothstep(0.4, 1.1, 1.0 - abs(result2));
  return vec2(2.0 * smoothstep(0.35, 1.8, (result1 + result2) * 0.5), result2);
}
`;

export const LIGHT_FS = HEAD + `
in vec2 vUv;
out vec4 outColor;
uniform float Time;
uniform vec2 Size;
${LIGHT}
#define LIGHT_COLOR vec3(0.45, 0.4, 0.8)
#define LIGHT_POWER 0.11 // the game's is 0.08, and the page is darker than its board
void main() {
  // the sea's Size, so that these are where the wobble has them
  outColor = vec4(LIGHT_COLOR * lightAt(vUv * Size / vec2(570.0, 320.0), Time).x * LIGHT_POWER, 1.0);
}`;

export const SCREEN_FS = HEAD + `
in vec2 vUv;
out vec4 outColor;
uniform sampler2D Sea;
uniform float Time;
uniform vec2 Size;
uniform float Motion;
${LIGHT}
#define VIGNETTE_TINT vec3(0.60, 0.67, 0.84)

void main() {
  // the wobble is worked out on the game's screen, so the waves stay the size they are in the game
  vec2 game = Size / vec2(570.0, 320.0);
  vec2 uv = vUv;
  vec2 fit = uv * game;
  float t = Time;
  vec2 lit = lightAt(fit, t);
  float result = lit.x;

  vec2 p = lit * 0.001 + 0.05 * sin(fit * 16.0 - cos(fit.yx * 16.0 + t)) * 0.1;
  p /= game;
  p *= Motion;

  // the sea's picture is the other way up: GL's rows start at the bottom
  vec2 at = uv + p;
  vec4 tex = texture(Sea, vec2(at.x, 1.0 - at.y));
  vec4 color = tex;

  // the corners, darker and bluer, in steps
  vec2 cell = floor(uv * Size);
  vec2 fromMiddle = ((cell + 0.5) / Size - 0.5) * vec2(2.3, 2.0);
  float corner = smoothstep(0.8, 1.6, length(fromMiddle));
  float checker = fract((cell.x + cell.y) * 0.5) * 2.0;
  corner = floor(corner * 12.0 + checker * 0.5) / 12.0;
  color.rgb *= mix(vec3(1.0), VIGNETTE_TINT, corner);

  outColor = vec4(color.rgb, 1.0);
}`;

