import {test} from "node:test";
import assert from "node:assert/strict";
import {readFileSync} from "node:fs";
import vm from "node:vm";
import ts from "typescript";

function load(path, modules, globals = {}) {
    const exports = {};
    const code = ts.transpileModule(readFileSync(new URL(path, import.meta.url), "utf8"), {
        compilerOptions: {module: ts.ModuleKind.CommonJS, jsx: ts.JsxEmit.ReactJSX, target: ts.ScriptTarget.ES2020}
    }).outputText;
    vm.runInNewContext(code, {exports, require: name => modules[name], ...globals});
    return exports;
}
const element = (type, props) => ({type, props});
const guidance = load("../src/components/desktop/SetupGuidance.tsx", {
    "@mui/material": {Button: "button", Typography: "text"},
    "react/jsx-runtime": {jsx: element, jsxs: element}
});
const defaults = {desktop_translation: true, recognizer: 0, desktop_translation_provider: "groq", speech_to_text_only: false};
const steps = changes => JSON.parse(JSON.stringify(guidance.missingSetup({...defaults, ...changes})));

test("Setup follows selected providers, shared keys and transcription-only mode", () => {
    assert.deepEqual(steps({}), [{page: "translation", provider: "Groq"}]);
    assert.deepEqual(steps({recognizer: 1}), [{page: "speech", provider: "Groq Whisper"}, {page: "translation", provider: "Groq"}]);
    assert.deepEqual(steps({recognizer: 1, groq_api_key_configured: true}), []);
    assert.deepEqual(steps({recognizer: 1, speech_to_text_only: true}), [{page: "speech", provider: "Groq Whisper"}]);
    assert.deepEqual(steps({speech_to_text_only: true}), []);
    for (const provider of ["google", "deepl"])
        assert.equal(steps({desktop_translation_provider: provider, groq_api_key_configured: true}).length, 1);
    assert.deepEqual(steps({desktop_translation_provider: "google", google_cloud_api_key_configured: true}), []);
    assert.deepEqual(steps({desktop_translation_provider: "deepl", deepl_api_key_configured: true}), []);
});

test("Combined Gemini recognition does not ask for an unused translation key; legacy mode has no desktop guidance", () => {
    assert.deepEqual(steps({recognizer: 2}), [{page: "speech", provider: "Gemini Live"}]);
    assert.deepEqual(steps({recognizer: 2, gemini_api_key_configured: true}), []);
    assert.deepEqual(steps({desktop_translation: false}), []);
});

test("Setup actions navigate to their setting and disappear after acknowledged key state", () => {
    const navigated = [], props = {config: {...defaults, recognizer: 1}, t: key => key, onNavigate: page => navigated.push(page)};
    const view = guidance.default(props);
    assert.equal(view.props["aria-labelledby"], "setup-title");
    const list = view.props.children.find(child => child.type === "ul");
    list.props.children.forEach(item => item.props.children.find(child => child.type === "button").props.onClick());
    assert.deepEqual(navigated, ["speech", "translation"]);
    assert.equal(guidance.default({...props, config: {...props.config, groq_api_key_configured: true}}), null);
});

test("Settings shortcuts navigate, protect editing and composition, and release their listener", () => {
    let listener, cleanup;
    const navigated = [];
    class Element {constructor(editable) {this.editable = editable;} closest() {return this.editable;}}
    const shortcuts = load("../src/components/desktop/useSettingsShortcuts.ts", {react: {useEffect: effect => {cleanup = effect();}}}, {
        Element, window: {addEventListener: (_, callback) => {listener = callback;}, removeEventListener: (_, callback) => {assert.equal(callback, listener); listener = null;}}
    });
    shortcuts.useSettingsShortcuts(page => navigated.push(page));
    let prevented = 0;
    const key = {key: "2", altKey: true, target: new Element(false), preventDefault: () => prevented++};
    for (const value of ["1", "2", "3", "4", "5"]) listener({...key, key: value});
    assert.deepEqual(navigated, ["translation", "speech", "subtitles", "general", "about"]);
    for (const change of [{altKey: false}, {ctrlKey: true}, {metaKey: true}, {shiftKey: true}, {repeat: true},
        {isComposing: true}, {defaultPrevented: true}, {target: new Element(true)}, {key: "0"}, {key: "6"}, {key: "a"}]) listener({...key, ...change});
    assert.equal(navigated.length, 5); assert.equal(prevented, 5);
    cleanup(); assert.equal(listener, null);
});
