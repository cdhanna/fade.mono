// Standalone debug inspector for exported MonoGame games.
//
// Embedded in the same HTML page as the canvas + Blazor runtime so it
// can call window.theInstance.invokeMethodAsync directly — no parent
// window / postMessage hop. The same IDebugProvider abstraction the
// Playground uses powers this panel; the only difference is the
// transport.
//
// Opt-in: only mounts when the URL has ?inspector=1 (or when
// window.__fadeShowInspector is set true before this script loads).
// Keeps the default exported game clean while letting users with
// "View → Inspector" hotkeys or debug builds light it up.
//
// Tweakpane is loaded via ESM CDN (esm.sh) to avoid bundling. If you
// need fully-offline exports, bake a local tweakpane.min.js next to
// this file and swap the import URL.

(function () {
    'use strict';

    const params = new URLSearchParams(location.search);
    const want = params.get('inspector') === '1' || window.__fadeShowInspector === true;
    if (!want) return;

    // Wait for window.theInstance (the Index.razor.cs DotNetObjectReference)
    // to land. Same pattern the standalone-boot block uses.
    function waitForInstance() {
        return new Promise((resolve) => {
            const tick = () => {
                if (window.theInstance) resolve();
                else setTimeout(tick, 100);
            };
            tick();
        });
    }

    const inv = (m, ...args) => window.theInstance.invokeMethodAsync(m, ...args);
    const parseOrNull = (s) => { try { return JSON.parse(s); } catch { return null; } };

    waitForInstance().then(async () => {
        // Tweakpane shipped alongside the runtime as a local ES module
        // (./tweakpane.min.js, ~150 KB) so the inspector works fully
        // offline. Earlier versions of this file fell back to esm.sh
        // when the local bundle was missing; the local bundle is now
        // always present from the WebRuntime.MonoGame publish step.
        let Pane;
        try {
            const mod = await import('./tweakpane.min.js');
            Pane = mod.Pane;
        } catch (e) {
            console.error('[fade-inspector] failed to load Tweakpane (./tweakpane.min.js):', e);
            return;
        }

        // Floating overlay container, top-right of the viewport.
        const host = document.createElement('div');
        host.id = 'fade-inspector';
        Object.assign(host.style, {
            position: 'fixed',
            top: '8px',
            right: '8px',
            width: '320px',
            maxHeight: 'calc(100vh - 16px)',
            overflowY: 'auto',
            zIndex: '10000',
            pointerEvents: 'auto',
            background: 'transparent',
        });
        document.body.appendChild(host);

        const pane = new Pane({ container: host, title: 'Inspector' });

        // Same set of poll cadences as the Playground panel.
        const METADATA_POLL_MS = 500;
        const ENTITY_REFRESH_MS = 250;

        const types = parseOrNull(await inv('DebugListTypes')) ?? [];
        if (types.length === 0) {
            const note = document.createElement('div');
            note.textContent = 'no debug providers';
            note.style.padding = '6px';
            host.appendChild(note);
            return;
        }

        // Schema cache — fetched once per type. Used for metadata
        // (singleton) and as a fallback for providers without a per-
        // entity override.
        const schemaCache = new Map();
        async function getSchema(t) {
            if (schemaCache.has(t)) return schemaCache.get(t);
            const s = parseOrNull(await inv('DebugGetSchema', t)) ?? [];
            schemaCache.set(t, s);
            return s;
        }
        // Per-entity schema — preferred for entity folders so providers
        // with dynamic fields (Effect shader parameters) surface them.
        async function getEntitySchema(t, id) {
            const s = parseOrNull(await inv('DebugGetEntitySchema', t, id));
            if (Array.isArray(s) && s.length > 0) return s;
            return getSchema(t);
        }

        // Build a binding for one schema field; returns { obj, blade }
        // or null if unsupported.
        function buildBinding(folder, type, id, field, snapshot) {
            const initial = snapshot[field.path];
            const opts = { label: field.label || field.path, readonly: !!field.readOnly };
            if (typeof field.min === 'number') opts.min = field.min;
            if (typeof field.max === 'number') opts.max = field.max;
            if (field.type === 'int') opts.step = 1;

            // image — direct <img> appended to the folder element.
            if (field.type === 'image') {
                const src = typeof initial === 'string' ? initial : '';
                const img = createImagePreview(folder, field, src);
                return { obj: { v: src }, blade: null, field, imageEl: img };
            }
            // int + referenceType — populate a <select> from the
            // referenced provider's ListIds.
            if (field.type === 'int' && field.referenceType) {
                const sel = createRefSelect(folder, type, id, field, Number(initial ?? 0));
                return { obj: { v: Number(initial ?? 0) }, blade: null, field, selectEl: sel };
            }

            let obj;
            switch (field.type) {
                case 'vec2': {
                    const a = Array.isArray(initial) ? initial : [0, 0];
                    obj = { v: { x: Number(a[0] ?? 0), y: Number(a[1] ?? 0) } };
                    break;
                }
                case 'vec3': {
                    const a = Array.isArray(initial) ? initial : [0, 0, 0];
                    obj = { v: { x: Number(a[0] ?? 0), y: Number(a[1] ?? 0), z: Number(a[2] ?? 0) } };
                    break;
                }
                case 'color': {
                    const a = Array.isArray(initial) ? initial : [255, 255, 255, 255];
                    obj = { v: { r: Number(a[0] ?? 255), g: Number(a[1] ?? 255), b: Number(a[2] ?? 255), a: Number(a[3] ?? 255) } };
                    opts.color = { type: 'int', alpha: true };
                    break;
                }
                case 'bool': obj = { v: !!initial }; break;
                case 'int':
                case 'float': obj = { v: Number(initial ?? 0) }; break;
                case 'string':
                default: obj = { v: String(initial ?? '') }; break;
            }
            const blade = folder.addBinding(obj, 'v', opts);
            blade.on('change', (ev) => {
                pushEdit(type, id, field, ev.value);
            });
            return { obj, blade, field };
        }

        function pushEdit(type, id, field, value) {
            const setLeaf = (path, v) =>
                inv('DebugSetField', type, id, path, JSON.stringify(v)).catch(() => {});
            switch (field.type) {
                case 'vec2':
                    setLeaf(field.path + '.X', value.x);
                    setLeaf(field.path + '.Y', value.y);
                    return;
                case 'vec3':
                    setLeaf(field.path + '.X', value.x);
                    setLeaf(field.path + '.Y', value.y);
                    setLeaf(field.path + '.Z', value.z);
                    return;
                case 'color':
                    setLeaf(field.path + '.R', Math.round(value.r));
                    setLeaf(field.path + '.G', Math.round(value.g));
                    setLeaf(field.path + '.B', Math.round(value.b));
                    setLeaf(field.path + '.A', Math.round(value.a));
                    return;
                case 'int':
                    setLeaf(field.path, Math.round(value));
                    return;
                default:
                    setLeaf(field.path, value);
                    return;
            }
        }

        // Helpers for image and ref-select fields.
        function createImagePreview(folder, field, initialSrc) {
            const row = document.createElement('div');
            row.style.cssText = 'padding:4px 6px;display:flex;flex-direction:column;gap:4px;';
            const lbl = document.createElement('div');
            lbl.style.cssText = 'font-size:11px;opacity:0.7;';
            lbl.textContent = field.label || field.path;
            const img = document.createElement('img');
            img.style.cssText = 'max-width:100%;max-height:128px;image-rendering:pixelated;background:#00000022;border:1px solid #ffffff14;align-self:flex-start;';
            if (initialSrc) img.src = initialSrc;
            row.append(lbl, img);
            folder.element.appendChild(row);
            return img;
        }

        function createRefSelect(folder, type, id, field, initial) {
            const row = document.createElement('div');
            row.style.cssText = 'padding:4px 6px;display:flex;align-items:center;gap:8px;';
            const lbl = document.createElement('label');
            lbl.style.cssText = 'font-size:11px;opacity:0.7;min-width:80px;';
            lbl.textContent = field.label || field.path;
            const sel = document.createElement('select');
            sel.style.cssText = 'flex:1;background:#0008;color:#ddd;border:1px solid #ffffff14;padding:2px 4px;font-size:11px;';
            sel.disabled = !!field.readOnly;
            void refreshRefSelectOptions(sel, field, initial);
            sel.addEventListener('change', () => {
                void inv('DebugSetField', type, id, field.path, JSON.stringify(Number(sel.value))).catch(() => {});
            });
            row.append(lbl, sel);
            folder.element.appendChild(row);
            return sel;
        }

        async function refreshRefSelectOptions(sel, field, current) {
            if (!field.referenceType) return;
            const ids = parseOrNull(await inv('DebugListEntities', field.referenceType)) ?? [];
            const want = new Set([0, current, ...ids]);
            const sorted = Array.from(want).sort((a, b) => a - b);
            const existing = Array.from(sel.options).map((o) => Number(o.value));
            if (existing.length === sorted.length && existing.every((v, i) => v === sorted[i])) return;
            sel.replaceChildren();
            for (const id of sorted) {
                const opt = document.createElement('option');
                opt.value = String(id);
                opt.textContent = id === 0 ? '(none)' : field.referenceType + ' #' + id;
                if (id === current) opt.selected = true;
                sel.appendChild(opt);
            }
        }

        // Apply a fresh snapshot into existing bindings — used by both
        // the metadata poll + the open-entity-folder refresh poll.
        function refreshFields(fields, snapshot) {
            for (const f of fields) {
                const value = snapshot[f.field.path];
                if (value === undefined || value === null) continue;
                // image — push new base64 src.
                if (f.field.type === 'image' && f.imageEl) {
                    const s = typeof value === 'string' ? value : '';
                    if (s && f.imageEl.src !== s) f.imageEl.src = s;
                    else if (!s) f.imageEl.removeAttribute('src');
                    continue;
                }
                // int + ref — update select value + (periodically) options.
                if (f.field.type === 'int' && f.field.referenceType && f.selectEl) {
                    const sv = String(Number(value));
                    if (f.selectEl.value !== sv) f.selectEl.value = sv;
                    void refreshRefSelectOptions(f.selectEl, f.field, Number(value));
                    continue;
                }
                switch (f.field.type) {
                    case 'vec2':
                        f.obj.v = { x: Number(value[0] ?? 0), y: Number(value[1] ?? 0) }; break;
                    case 'vec3':
                        f.obj.v = { x: Number(value[0] ?? 0), y: Number(value[1] ?? 0), z: Number(value[2] ?? 0) }; break;
                    case 'color':
                        f.obj.v = { r: Number(value[0] ?? 0), g: Number(value[1] ?? 0), b: Number(value[2] ?? 0), a: Number(value[3] ?? 255) }; break;
                    case 'bool': f.obj.v = !!value; break;
                    case 'int':
                    case 'float': f.obj.v = Number(value); break;
                    case 'string':
                    default: f.obj.v = String(value); break;
                }
                try { f.blade.refresh(); } catch { /* ignore */ }
            }
        }

        // Metadata folder — always at the top, perma-expanded.
        const entityFolders = new Map(); // type -> {folder, perId: Map(id -> {folder, fields, expanded})}
        let metadataFields = [];
        if (types.includes('metadata')) {
            const f = pane.addFolder({ title: 'Metadata', expanded: true });
            const schema = await getSchema('metadata');
            const snap = parseOrNull(await inv('DebugGetEntity', 'metadata', 0)) ?? {};
            metadataFields = schema.map((field) => buildBinding(f, 'metadata', 0, field, snap)).filter(Boolean);
        }
        // Entity-type folders.
        for (const t of types.filter((x) => x !== 'metadata').sort()) {
            const parent = pane.addFolder({ title: capitalize(t) + 's', expanded: false });
            entityFolders.set(t, { folder: parent, perId: new Map() });
            await refreshEntityList(t);
        }

        // Polling — same cadences as the Playground panel.
        setInterval(async () => {
            const snap = parseOrNull(await inv('DebugGetEntity', 'metadata', 0));
            if (snap) refreshFields(metadataFields, snap);
        }, METADATA_POLL_MS);
        setInterval(async () => {
            for (const [t, group] of entityFolders) {
                for (const [id, ef] of group.perId) {
                    if (!ef.expanded) continue;
                    const snap = parseOrNull(await inv('DebugGetEntity', t, id));
                    if (snap) refreshFields(ef.fields, snap);
                }
            }
        }, ENTITY_REFRESH_MS);

        // Periodic top-level entity list refresh so newly-created
        // sprites/transforms appear without needing a panel reload.
        setInterval(async () => {
            for (const t of entityFolders.keys()) {
                await refreshEntityList(t);
            }
        }, 1500);

        async function refreshEntityList(t) {
            const group = entityFolders.get(t);
            if (!group) return;
            const ids = parseOrNull(await inv('DebugListEntities', t)) ?? [];
            group.folder.title = capitalize(t) + 's (' + ids.length + ')';
            for (const [id, ef] of group.perId) {
                if (!ids.includes(id)) {
                    try { ef.folder.dispose(); } catch { /* ignore */ }
                    group.perId.delete(id);
                }
            }
            for (const id of ids) {
                if (group.perId.has(id)) continue;
                const schema = await getEntitySchema(t, id);
                const snap = parseOrNull(await inv('DebugGetEntity', t, id));
                if (!snap) continue;
                const sub = group.folder.addFolder({ title: t + ' #' + id, expanded: false });
                const ef = { folder: sub, fields: [], expanded: false };
                sub.on('fold', (ev) => { ef.expanded = ev.expanded; });
                ef.fields = schema.map((field) => buildBinding(sub, t, id, field, snap)).filter(Boolean);
                group.perId.set(id, ef);
            }
        }

        function capitalize(s) { return s ? s[0].toUpperCase() + s.slice(1) : s; }

        console.log('[fade-inspector] mounted — types:', types);
    });
})();
