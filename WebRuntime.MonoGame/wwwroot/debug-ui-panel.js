// Standalone-export debug-UI panel. This is the plain-JS twin of
// Playground/src/debug-ui-panel.ts: same envelope-driven Tweakpane
// layout, same fbasic-packed color handling, same expansion-state
// persistence — just without TypeScript types and without the
// Playground's monogame-host postMessage callbacks.
//
// Loaded lazily by wwwroot/index.html when the user opts in via
// `window.fadeDebug.enable()` or `?debug=1`. The two copies of the
// panel (this one and the Playground TS one) need to stay in sync
// behaviour-wise; if you change one, mirror it to the other.
//
// Wire format mirrors the Playground envelope:
//   { gen, queue, autoInspector, metadata?, entities? }
//
// Callbacks come from index.html's fadeDebug.enable() and call
// DotNet.invokeMethodAsync directly into Index.razor.cs's JSInvokables
// — there's no iframe round-trip in standalone mode.

import { Pane } from './tweakpane.min.js';

const ENTITY_REFRESH_MS = 250;           // 4 Hz refresh of open entity folders (data only)

// DebugControlType (browser ordering — must match
// Fade.MonoGame.Game/DebugUISystem.Browser.cs).
const CT = {
    WINDOW_START: 0, WINDOW_END: 1, SEPARATOR: 3,
    TREE_START: 4, TREE_END: 5,
    BUTTON: 10, CHECKBOX: 11,
    FLOAT_SLIDER: 15, INT_SLIDER: 16,
    LABEL: 17, TEXT: 18, TEXTFIELD: 19,
    CONSOLE: 20, INSPECTOR: 21,
    ARG_FLOAT: 22, ARG_INT: 23, ARG_STRING: 24,
};

const KIND_BOOL = 0, KIND_INT = 1, KIND_FLOAT = 2, KIND_STRING = 3;

// Snapshot refreshes within this window after a user change are
// skipped — see Playground/src/debug-ui-panel.ts for the why.
const REFRESH_LOCKOUT_MS = 1500;

// Set true while applySnapshotValue is mutating a bound object and
// calling blade.refresh(). Tweakpane's refresh() can re-enter our
// user-change handler synchronously; this flag lets us tell echo
// events apart from real user input.
let applyingSnapshot = false;

export function mountDebugUiPanel(opts) {
    const root = opts.container;
    root.replaceChildren();
    root.style.padding = '6px';
    root.style.overflowY = 'auto';
    root.style.height = '100%';
    root.style.boxSizing = 'border-box';
    root.style.display = 'flex';
    root.style.flexDirection = 'column';
    root.style.gap = '8px';

    let disposed = false;

    // One Pane per `begin debug window "name"`.
    const fbasicWindows = new Map();

    // Inspector pane state.
    let inspectorEnabled = false;
    let inspectorPane = null;
    let inspectorContainer = null;
    let metadataFolder = null;
    let metadataFields = [];
    let metadataSchemaPending = false;
    const inspectorTypeRoots = new Map();
    const inspectorEntities = new Map();
    const inspectorTypePending = new Set();
    const schemaCache = new Map();

    // Per-type sync lock + last-applied id signature (collapse bursts;
    // no double-adds across awaits).
    const lastIdsSignature = new Map();
    const syncInFlight = new Map();
    const entityAddPending = new Map();

    let lastGen = null;

    // Sticky expansion state across program restarts.
    const expandedState = new Map();
    function recallExpand(key, fallback) {
        const stored = expandedState.get(key);
        return stored === undefined ? fallback : stored;
    }
    function attachFoldTracker(folder, key) {
        try {
            folder.on && folder.on('fold', (ev) => {
                expandedState.set(key, ev.expanded);
            });
        } catch { /* ignore — Pane vs FolderApi event surface drift */ }
    }

    let entityRefreshTimer = null;

    let idleHint = null;
    showIdleHint('Run your program to see custom debug windows + the Inspector here.');

    function applyFrameEnvelope(env) {
        if (disposed) return;

        if (lastGen !== null && env.gen !== lastGen) {
            wipeAllProgramState();
        }
        lastGen = env.gen;

        if (env.autoInspector && !inspectorEnabled) {
            inspectorEnabled = true;
            buildInspectorPane();
        } else if (!env.autoInspector && inspectorEnabled) {
            inspectorEnabled = false;
            disposeInspector();
        }

        if (env.queue && env.queue.length > 0) {
            clearIdleHint();
            const grouped = groupByWindow(env.queue);
            for (const [name, windowCmds] of grouped) {
                ensureWindow(name);
                renderWindow(name, windowCmds);
            }
        }

        if (inspectorEnabled && env.autoInspector) {
            if (env.metadata) applyMetadataFromEnvelope(env.metadata);
            if (env.entities) applyEntityIdsFromEnvelope(env.entities);
            clearIdleHint();
        }
    }

    function wipeAllProgramState() {
        for (const w of fbasicWindows.values()) {
            try { w.pane.dispose(); } catch { /* ignore */ }
            w.container.remove();
        }
        fbasicWindows.clear();
        disposeInspector();
        inspectorEnabled = false;
        showIdleHint('Run your program to see custom debug windows + the Inspector here.');
    }

    function makeSectionContainer() {
        const div = document.createElement('div');
        root.appendChild(div);
        return div;
    }

    function ensureWindow(name) {
        if (fbasicWindows.has(name)) return;
        const container = makeSectionContainer();
        const key = `win:${name}`;
        const pane = new Pane({ container, title: name, expanded: recallExpand(key, true) });
        attachFoldTracker(pane, key);
        fbasicWindows.set(name, { pane, container, structHash: '', bindings: new Map() });
    }

    function renderWindow(name, cmds) {
        const entry = fbasicWindows.get(name);
        if (!entry) return;
        const hash = computeStructHash(cmds);
        if (hash !== entry.structHash) {
            entry.structHash = hash;
            rebuildWindow(entry, name, cmds);
        } else {
            refreshWindow(entry, cmds);
        }
    }

    function rebuildWindow(entry, name, cmds) {
        try { entry.pane.dispose(); } catch { /* ignore */ }
        entry.bindings.clear();
        entry.container.replaceChildren();
        const paneKey = `win:${name}`;
        entry.pane = new Pane({ container: entry.container, title: name, expanded: recallExpand(paneKey, true) });
        attachFoldTracker(entry.pane, paneKey);

        const stack = [entry.pane];
        const treePathStack = [];
        const here = () => stack[stack.length - 1];

        for (let i = 0; i < cmds.length; i++) {
            const c = cmds[i];
            switch (c.t) {
                case CT.TREE_START: {
                    const label = c.l || 'tree';
                    treePathStack.push(label);
                    const treeKey = `${paneKey}/tree:${treePathStack.join('/')}`;
                    const f = here().addFolder({ title: label, expanded: recallExpand(treeKey, false) });
                    attachFoldTracker(f, treeKey);
                    stack.push(f);
                    break;
                }
                case CT.TREE_END:
                    if (stack.length > 1) {
                        stack.pop();
                        treePathStack.pop();
                    }
                    break;
                case CT.SEPARATOR:
                    try { here().addBlade({ view: 'separator' }); } catch { /* ignore */ }
                    break;
                case CT.LABEL: {
                    const obj = { v: c.s ?? '' };
                    here().addBinding(obj, 'v', { label: c.l || '', readonly: true });
                    break;
                }
                case CT.TEXT: {
                    const obj = { v: c.s ?? '' };
                    here().addBinding(obj, 'v', { label: '', readonly: true });
                    break;
                }
                case CT.BUTTON: {
                    const btn = here().addButton({ title: c.l || 'button' });
                    btn.on('click', () => opts.sendFbasicChange(c.id, KIND_BOOL, 'true'));
                    break;
                }
                case CT.CHECKBOX: {
                    const obj = { v: !!c.i };
                    const blade = here().addBinding(obj, 'v', { label: c.l || 'checkbox' });
                    blade.on('change', (ev) =>
                        opts.sendFbasicChange(c.id, KIND_INT, ev.value ? '1' : '0'));
                    entry.bindings.set(c.id, { obj, blade, type: c.t });
                    break;
                }
                case CT.INT_SLIDER: {
                    const argMin = peekArg(cmds, i, CT.ARG_INT);
                    const argMax = peekArg(cmds, i + 1, CT.ARG_INT);
                    const min = argMin ? argMin.i : 0;
                    const max = argMax ? argMax.i : 100;
                    const obj = { v: c.i };
                    const blade = here().addBinding(obj, 'v', { label: c.l || 'int', min, max, step: 1 });
                    blade.on('change', (ev) =>
                        opts.sendFbasicChange(c.id, KIND_INT, String(Math.round(ev.value))));
                    entry.bindings.set(c.id, { obj, blade, type: c.t });
                    break;
                }
                case CT.FLOAT_SLIDER: {
                    const argMin = peekArg(cmds, i, CT.ARG_FLOAT);
                    const argMax = peekArg(cmds, i + 1, CT.ARG_FLOAT);
                    const min = argMin ? argMin.f : 0;
                    const max = argMax ? argMax.f : 100;
                    const obj = { v: c.f ?? 0 };
                    const blade = here().addBinding(obj, 'v', { label: c.l || 'float', min, max });
                    blade.on('change', (ev) =>
                        opts.sendFbasicChange(c.id, KIND_FLOAT, String(ev.value)));
                    entry.bindings.set(c.id, { obj, blade, type: c.t });
                    break;
                }
                case CT.TEXTFIELD: {
                    const obj = { v: c.s ?? '' };
                    const blade = here().addBinding(obj, 'v', { label: c.l || 'text' });
                    blade.on('change', (ev) =>
                        opts.sendFbasicChange(c.id, KIND_STRING, ev.value ?? ''));
                    entry.bindings.set(c.id, { obj, blade, type: c.t });
                    break;
                }
                case CT.ARG_FLOAT:
                case CT.ARG_INT:
                case CT.ARG_STRING:
                case CT.WINDOW_START:
                case CT.WINDOW_END:
                case CT.INSPECTOR:
                case CT.CONSOLE:
                    break;
                default:
                    try {
                        here().addBlade({
                            view: 'text', label: c.l || `type ${c.t}`,
                            parse: (s) => s, value: `(type ${c.t} not bridged)`,
                        });
                    } catch { /* ignore */ }
                    break;
            }
        }
    }

    function refreshWindow(entry, cmds) {
        for (const c of cmds) {
            const b = entry.bindings.get(c.id);
            if (!b) continue;
            if (isBladeFocused(b.blade)) continue;
            let next = b.obj.v;
            switch (b.type) {
                case CT.CHECKBOX: next = !!c.i; break;
                case CT.INT_SLIDER: next = c.i; break;
                case CT.FLOAT_SLIDER: next = c.f ?? 0; break;
                case CT.TEXTFIELD: next = c.s ?? ''; break;
                default: continue;
            }
            if (b.obj.v !== next) {
                b.obj.v = next;
                try { b.blade.refresh && b.blade.refresh(); } catch { /* ignore */ }
            }
        }
    }

    function computeStructHash(cmds) {
        let h = '';
        for (const c of cmds) {
            if (c.t === CT.ARG_FLOAT || c.t === CT.ARG_INT || c.t === CT.ARG_STRING) continue;
            h += c.id + '|' + c.t + '|' + (c.l || '') + ';';
        }
        return h;
    }

    function peekArg(cmds, from, type) {
        for (let i = from + 1; i < cmds.length; i++) {
            if (cmds[i].t === type) return cmds[i];
            if (cmds[i].t !== CT.ARG_FLOAT && cmds[i].t !== CT.ARG_INT && cmds[i].t !== CT.ARG_STRING) return undefined;
        }
        return undefined;
    }

    function groupByWindow(cmds) {
        const result = new Map();
        let current = null;
        let depth = 0;
        for (const cmd of cmds) {
            if (cmd.t === CT.WINDOW_START) {
                if (depth === 0) {
                    current = cmd.l || 'window';
                    if (!result.has(current)) result.set(current, []);
                }
                depth++;
            } else if (cmd.t === CT.WINDOW_END) {
                depth--;
                if (depth === 0) current = null;
            } else if (current !== null) {
                result.get(current).push(cmd);
            }
        }
        const ONLY_INSPECTOR = new Set([CT.INSPECTOR, CT.CONSOLE, CT.ARG_FLOAT, CT.ARG_INT, CT.ARG_STRING]);
        for (const [name, list] of Array.from(result)) {
            if (list.length === 0) { result.delete(name); continue; }
            if (list.every((c) => ONLY_INSPECTOR.has(c.t))) result.delete(name);
        }
        return result;
    }

    function buildInspectorPane() {
        if (inspectorPane) return;
        clearIdleHint();
        inspectorContainer = makeSectionContainer();
        inspectorPane = new Pane({
            container: inspectorContainer,
            title: 'Inspector',
            expanded: recallExpand('insp', true),
        });
        attachFoldTracker(inspectorPane, 'insp');
        startEntityRefreshPolling();
    }

    function disposeInspector() {
        stopEntityRefreshPolling();
        try { inspectorPane && inspectorPane.dispose(); } catch { /* ignore */ }
        if (inspectorContainer) inspectorContainer.remove();
        inspectorPane = null;
        inspectorContainer = null;
        metadataFolder = null;
        metadataFields = [];
        metadataSchemaPending = false;
        inspectorTypeRoots.clear();
        inspectorEntities.clear();
        inspectorTypePending.clear();
        lastIdsSignature.clear();
        syncInFlight.clear();
        entityAddPending.clear();
    }

    async function ensureMetadataFolder(snapshot) {
        if (!inspectorPane || metadataFolder || metadataSchemaPending) return;
        metadataSchemaPending = true;
        try {
            const schema = await ensureSchema('metadata');
            if (!schema || disposed || !inspectorPane) return;
            metadataFolder = inspectorPane.addFolder({
                title: 'Metadata',
                expanded: recallExpand('insp/metadata', true),
            });
            attachFoldTracker(metadataFolder, 'insp/metadata');
            metadataFields = buildBindingsFor(metadataFolder, 'metadata', 0, schema, snapshot);
        } finally {
            metadataSchemaPending = false;
        }
    }

    function applyMetadataFromEnvelope(snapshot) {
        if (!inspectorPane) return;
        if (!metadataFolder) { void ensureMetadataFolder(snapshot); return; }
        for (const f of metadataFields) applySnapshotValue(f, snapshot[f.field.path]);
    }

    function applyEntityIdsFromEnvelope(byType) {
        if (!inspectorPane) return;
        for (const [typeName, idsRaw] of Object.entries(byType)) {
            const ids = Array.isArray(idsRaw) ? idsRaw : [];
            const signature = ids.join(',');
            if (lastIdsSignature.get(typeName) === signature) continue;

            if (!inspectorTypeRoots.has(typeName)) {
                if (inspectorTypePending.has(typeName)) continue;
                inspectorTypePending.add(typeName);
                void buildTypeFolder(typeName, ids)
                    .then(() => { lastIdsSignature.set(typeName, signature); })
                    .finally(() => { inspectorTypePending.delete(typeName); });
                continue;
            }

            const previous = syncInFlight.get(typeName) ?? Promise.resolve();
            const next = previous.then(async () => {
                if (disposed) return;
                if (lastIdsSignature.get(typeName) === signature) return;
                await syncTypeFolder(typeName, ids);
                lastIdsSignature.set(typeName, signature);
            }).catch((e) => {
                console.warn('[debug-ui] syncTypeFolder threw', typeName, e);
            });
            syncInFlight.set(typeName, next);
        }
    }

    async function buildTypeFolder(typeName, initialIds) {
        if (!inspectorPane) return;
        const typeKey = `insp/type:${typeName}`;
        const folder = inspectorPane.addFolder({
            title: `${capitalize(typeName)}s (${initialIds.length})`,
            expanded: recallExpand(typeKey, false),
        });
        attachFoldTracker(folder, typeKey);
        inspectorTypeRoots.set(typeName, folder);
        inspectorEntities.set(typeName, new Map());
        await syncTypeFolder(typeName, initialIds);
    }

    async function syncTypeFolder(typeName, ids) {
        const parent = inspectorTypeRoots.get(typeName);
        if (!parent) return;
        const existing = inspectorEntities.get(typeName);
        let pending = entityAddPending.get(typeName);
        if (!pending) { pending = new Set(); entityAddPending.set(typeName, pending); }

        parent.title = `${capitalize(typeName)}s (${ids.length})`;

        const wantSet = new Set(ids);
        for (const [id, ef] of existing) {
            if (!wantSet.has(id)) {
                try { ef.folder.dispose(); } catch { /* ignore */ }
                existing.delete(id);
            }
        }

        // Fetch friendly labels once per sync — same endpoint as the
        // reference-type dropdowns. Best-effort with a short timeout
        // so a hung host can't keep new folder titles in limbo.
        let labels = {};
        if (opts.getLabels) {
            try {
                labels = await Promise.race([
                    opts.getLabels(typeName),
                    new Promise((_, reject) =>
                        setTimeout(() => reject(new Error('getLabels timeout')), 1500)),
                ]);
            } catch { /* fall back to "<type> #<id>" */ }
        }
        const entityTitle = (id) => {
            const named = labels[String(id)];
            return named && named.length > 0 ? named : `${typeName} #${id}`;
        };

        // Re-title existing folders too — asset paths can change
        // without the id set changing.
        for (const [id, ef] of existing) {
            const desired = entityTitle(id);
            if (ef.folder.title !== desired) ef.folder.title = desired;
        }

        for (const id of ids) {
            if (existing.has(id) || pending.has(id)) continue;
            pending.add(id);
            try {
                const schema = await ensureEntitySchema(typeName, id);
                if (!schema || disposed) continue;
                if (existing.has(id)) continue;
                const snap = await opts.getEntity(typeName, id);
                if (!snap || disposed) continue;
                const parentNow = inspectorTypeRoots.get(typeName);
                if (!parentNow) return;
                if (existing.has(id)) continue;
                const entKey = `insp/type:${typeName}/ent:${id}`;
                const entInitiallyExpanded = recallExpand(entKey, false);
                const sub = parentNow.addFolder({ title: entityTitle(id), expanded: entInitiallyExpanded });
                const ef = { folder: sub, fields: [], expanded: entInitiallyExpanded };
                sub.on('fold', (ev) => {
                    ef.expanded = ev.expanded;
                    expandedState.set(entKey, ev.expanded);
                });
                ef.fields = buildBindingsFor(sub, typeName, id, schema, snap);
                existing.set(id, ef);
            } finally {
                pending.delete(id);
            }
        }
    }

    async function ensureSchema(typeName) {
        const cached = schemaCache.get(typeName);
        if (cached) return cached;
        const fresh = await opts.getSchema(typeName);
        if (fresh) schemaCache.set(typeName, fresh);
        return fresh;
    }
    async function ensureEntitySchema(typeName, id) {
        if (opts.getEntitySchema) {
            const perEntity = await opts.getEntitySchema(typeName, id);
            if (perEntity) return perEntity;
        }
        return ensureSchema(typeName);
    }

    function buildBindingsFor(folder, typeName, id, schema, snapshot) {
        const out = [];
        for (const field of schema) {
            const built = buildOneBinding(folder, typeName, id, field, snapshot);
            for (const b of built) out.push(b);
        }
        return out;
    }

    function buildOneBinding(folder, typeName, id, field, snapshot) {
        const initial = snapshot[field.path];
        const fieldOpts = {
            label: field.label || field.path,
            readonly: !!field.readOnly,
        };
        if (typeof field.min === 'number') fieldOpts.min = field.min;
        if (typeof field.max === 'number') fieldOpts.max = field.max;
        if (field.type === 'int') fieldOpts.step = 1;

        if (field.type === 'image') {
            const src = typeof initial === 'string' ? initial : '';
            const img = createImagePreview(folder, field, src);
            return img ? [{ blade: {}, bound: { v: src }, field, imageEl: img }] : [];
        }
        if (field.type === 'int' && field.referenceType) {
            const sel = createRefSelect(folder, typeName, id, field, Number(initial ?? 0));
            return sel ? [{ blade: {}, bound: { v: Number(initial ?? 0) }, field, selectEl: sel }] : [];
        }

        // Vec2 / Vec3 → one plain scalar binding per component (no
        // draggable point picker, no slider — matches the Playground
        // panel after the user's "too intense" feedback).
        if (field.type === 'vec2' || field.type === 'vec3') {
            const arr = Array.isArray(initial) ? initial : [];
            const components = field.type === 'vec2' ? ['X', 'Y'] : ['X', 'Y', 'Z'];
            const built = [];
            for (let i = 0; i < components.length; i++) {
                const comp = components[i];
                const compInitial = Number(arr[i] ?? 0);
                const compBound = { v: compInitial };
                const compOpts = {
                    label: `${field.label || field.path}.${comp.toLowerCase()}`,
                    readonly: !!field.readOnly,
                };
                const blade = folder.addBinding(compBound, 'v', compOpts);
                const bf = { blade, bound: compBound, field, component: comp };
                blade.on('change', (ev) => {
                    if (applyingSnapshot) return;
                    bf.lastInteractedAt = performance.now();
                    pushEdit(typeName, id, field, ev.value, comp);
                });
                built.push(bf);
            }
            return built;
        }

        let bound;
        try {
            switch (field.type) {
                case 'color': {
                    // C# ships color as fbasic-packed int (bytes
                    // [R,G,B,A] low→high). Tweakpane's number-color
                    // wants `0xRRGGBBAA` — byte-reverse between the
                    // two.
                    const fbasicInt = typeof initial === 'number' ? initial : 0xffffffff | 0;
                    bound = { v: fbasicToTweakpaneColor(fbasicInt) };
                    fieldOpts.color = { type: 'int', alpha: true };
                    break;
                }
                case 'bool': bound = { v: !!initial }; break;
                case 'int':
                case 'float': bound = { v: Number(initial ?? 0) }; break;
                case 'string':
                default: bound = { v: String(initial ?? '') }; break;
            }
            const blade = folder.addBinding(bound, 'v', fieldOpts);
            const out = { blade, bound, field };
            blade.on('change', (ev) => {
                if (applyingSnapshot) return;
                out.lastInteractedAt = performance.now();
                pushEdit(typeName, id, field, ev.value);
            });
            return [out];
        } catch (e) {
            console.warn('[debug-ui] failed to bind', typeName, id, field.path, e);
            return [];
        }
    }

    function pushEdit(typeName, id, field, value, component) {
        try {
            switch (field.type) {
                case 'vec2':
                case 'vec3': {
                    if (!component) return;
                    if (typeof value !== 'number' || !Number.isFinite(value)) return;
                    void opts.setField(typeName, id, `${field.path}.${component}`, JSON.stringify(value));
                    return;
                }
                case 'color': {
                    if (typeof value !== 'number' || !Number.isFinite(value)) return;
                    const fbasicInt = tweakpaneToFbasicColor(value);
                    void opts.setField(typeName, id, field.path, JSON.stringify(fbasicInt | 0));
                    return;
                }
                case 'int':
                    void opts.setField(typeName, id, field.path, JSON.stringify(Math.round(value)));
                    return;
                default:
                    void opts.setField(typeName, id, field.path, JSON.stringify(value));
                    return;
            }
        } catch (e) {
            console.warn('[debug-ui] setField failed', typeName, id, field.path, e);
        }
    }

    function createImagePreview(folder, field, initialSrc) {
        const row = document.createElement('div');
        row.style.cssText = 'padding: 4px 6px; display: flex; flex-direction: column; gap: 4px;';
        const lbl = document.createElement('div');
        lbl.style.cssText = 'font-size: 11px; opacity: 0.7;';
        lbl.textContent = field.label || field.path;
        const img = document.createElement('img');
        img.style.cssText = 'max-width: 100%; max-height: 128px; image-rendering: pixelated; background: #00000022; border: 1px solid #ffffff14; align-self: flex-start;';
        if (initialSrc) img.src = initialSrc;
        row.append(lbl, img);
        folderContentEl(folder).appendChild(row);
        return img;
    }

    function createRefSelect(folder, typeName, id, field, initial) {
        const row = document.createElement('div');
        row.style.cssText = 'padding: 4px 6px; display: flex; align-items: center; gap: 8px;';
        const lbl = document.createElement('label');
        lbl.style.cssText = 'font-size: 11px; opacity: 0.7; min-width: 80px;';
        lbl.textContent = field.label || field.path;
        const sel = document.createElement('select');
        sel.style.cssText = 'flex: 1; background: #0008; color: #ddd; border: 1px solid #ffffff14; padding: 2px 4px; font-size: 11px;';
        sel.disabled = !!field.readOnly;
        void refreshRefSelectOptions(sel, field, initial);
        sel.addEventListener('change', () => {
            const v = Number(sel.value);
            void opts.setField(typeName, id, field.path, JSON.stringify(v));
        });
        row.append(lbl, sel);
        folderContentEl(folder).appendChild(row);
        return sel;
    }

    function folderContentEl(folder) {
        const outer = folder.element;
        const inner = outer.querySelector(':scope > .tp-fldv_c');
        return inner ?? outer;
    }

    async function refreshRefSelectOptions(sel, field, currentValue) {
        if (!field.referenceType) return;
        const refType = field.referenceType;
        // listEntities IS the critical path — without an id list there's
        // nothing to show. getLabels is best-effort: if the host hangs
        // or rejects, fall back to numeric labels so the dropdown stays
        // usable.
        let ids = [];
        try { ids = await opts.listEntities(refType); }
        catch (e) { console.warn('[debug-ui] listEntities failed', refType, e); }
        let labels = {};
        if (opts.getLabels) {
            try {
                labels = await Promise.race([
                    opts.getLabels(refType),
                    new Promise((_, reject) =>
                        setTimeout(() => reject(new Error('getLabels timeout')), 1500)),
                ]);
            } catch (e) { /* fall back to "type #id" */ }
        }
        const want = new Set([0, currentValue, ...ids]);
        const sorted = Array.from(want).sort((a, b) => a - b);
        const labelText = (id) => {
            if (id === 0) return '(none)';
            const named = labels[String(id)];
            return named && named.length > 0 ? named : `${refType} #${id}`;
        };
        const wantSignature = sorted.map((id) => `${id}|${labelText(id)}`).join(',');
        const existingSignature = Array.from(sel.options).map((o) => `${o.value}|${o.textContent ?? ''}`).join(',');
        if (existingSignature === wantSignature) return;
        sel.replaceChildren();
        for (const id of sorted) {
            const opt = document.createElement('option');
            opt.value = String(id);
            opt.textContent = labelText(id);
            if (id === currentValue) opt.selected = true;
            sel.appendChild(opt);
        }
    }

    function startEntityRefreshPolling() {
        stopEntityRefreshPolling();
        entityRefreshTimer = setInterval(async () => {
            if (disposed || !inspectorEnabled) return;
            if (anyPopupOpen()) return;
            for (const [typeName, map] of inspectorEntities) {
                for (const [id, ef] of map) {
                    if (!ef.expanded) continue;
                    const snap = await opts.getEntity(typeName, id);
                    if (!snap) continue;
                    for (const f of ef.fields) {
                        applySnapshotValue(f, snap[f.field.path]);
                        if (f.selectEl && f.field.referenceType) {
                            void refreshRefSelectOptions(f.selectEl, f.field, Number(snap[f.field.path] ?? 0));
                        }
                    }
                }
            }
        }, ENTITY_REFRESH_MS);
    }

    function anyPopupOpen() {
        return !!root.querySelector('.tp-popv-v');
    }
    function stopEntityRefreshPolling() {
        if (entityRefreshTimer) { clearInterval(entityRefreshTimer); entityRefreshTimer = null; }
    }

    function applySnapshotValue(f, value) {
        if (value === undefined || value === null) return;
        if (f.lastInteractedAt && performance.now() - f.lastInteractedAt < REFRESH_LOCKOUT_MS) return;
        if (isBladeFocused(f.blade)) return;
        if (f.field.type === 'image' && f.imageEl) {
            const s = typeof value === 'string' ? value : '';
            if (s && f.imageEl.src !== s) f.imageEl.src = s;
            if (!s) f.imageEl.removeAttribute('src');
            return;
        }
        if (f.field.type === 'int' && f.field.referenceType && f.selectEl) {
            const s = String(Number(value));
            if (f.selectEl.value !== s) f.selectEl.value = s;
            return;
        }
        switch (f.field.type) {
            case 'vec2':
            case 'vec3': {
                const arr = Array.isArray(value) ? value : [];
                const idx = f.component === 'Y' ? 1 : f.component === 'Z' ? 2 : 0;
                f.bound.v = Number(arr[idx] ?? 0);
                break;
            }
            case 'color': {
                const fbasicInt = typeof value === 'number' ? value : 0;
                f.bound.v = fbasicToTweakpaneColor(fbasicInt);
                break;
            }
            case 'bool': f.bound.v = !!value; break;
            case 'int':
            case 'float': f.bound.v = Number(value); break;
            case 'string':
            default: f.bound.v = String(value); break;
        }
        applyingSnapshot = true;
        try { f.blade.refresh && f.blade.refresh(); }
        catch { /* ignore */ }
        finally { applyingSnapshot = false; }
    }

    function isBladeFocused(blade) {
        if (!blade || typeof blade !== 'object') return false;
        const el = blade.element;
        if (!(el instanceof HTMLElement)) return false;
        const active = document.activeElement;
        if (active && el.contains(active)) return true;
        if (el.querySelector('.tp-popv-v')) return true;
        return false;
    }

    function capitalize(s) { return s ? s[0].toUpperCase() + s.slice(1) : s; }

    function reverseColorBytes(n) {
        const v = n | 0;
        return (
            ((v & 0xff) << 24) |
            (((v >>> 8) & 0xff) << 16) |
            (((v >>> 16) & 0xff) << 8) |
            ((v >>> 24) & 0xff)
        ) >>> 0;
    }
    const fbasicToTweakpaneColor = reverseColorBytes;
    const tweakpaneToFbasicColor = reverseColorBytes;

    function showIdleHint(text) {
        clearIdleHint();
        idleHint = document.createElement('div');
        idleHint.style.padding = '8px';
        idleHint.style.opacity = '0.6';
        idleHint.style.fontSize = '12px';
        idleHint.textContent = text;
        root.appendChild(idleHint);
    }
    function clearIdleHint() {
        if (idleHint) { idleHint.remove(); idleHint = null; }
    }

    function dispose() {
        if (disposed) return;
        disposed = true;
        stopEntityRefreshPolling();
        for (const w of fbasicWindows.values()) {
            try { w.pane.dispose(); } catch { /* ignore */ }
            w.container.remove();
        }
        fbasicWindows.clear();
        try { inspectorPane && inspectorPane.dispose(); } catch { /* ignore */ }
        if (inspectorContainer) inspectorContainer.remove();
        inspectorPane = null;
        inspectorContainer = null;
        root.replaceChildren();
    }

    return { applyFrameEnvelope, dispose };
}

// Parse the raw JSON envelope shape produced by
// DebugUISystem.Browser.cs's BuildFrameEnvelope. Tolerates the legacy
// "bare queue array" wire shape just like the TS version does.
export function parseDebugUiEnvelope(json) {
    const empty = { gen: 0, queue: [], autoInspector: false };
    if (!json) return empty;
    let raw;
    try { raw = JSON.parse(json); }
    catch { return empty; }
    if (Array.isArray(raw)) return { gen: 0, queue: raw, autoInspector: false };
    if (!raw || typeof raw !== 'object') return empty;
    return {
        gen: typeof raw.gen === 'number' ? raw.gen : 0,
        queue: Array.isArray(raw.queue) ? raw.queue : [],
        autoInspector: !!raw.autoInspector,
        metadata: raw.metadata && typeof raw.metadata === 'object' ? raw.metadata : null,
        entities: raw.entities && typeof raw.entities === 'object' ? raw.entities : undefined,
    };
}
