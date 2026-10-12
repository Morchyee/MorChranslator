import {test} from "node:test";
import assert from "node:assert/strict";
import {readFileSync, existsSync} from "node:fs";

test("HTML, settings and localized presentation use exact MorChranslator brand", () => {
    const read = path => readFileSync(new URL(path, import.meta.url), "utf8");
    const html = read("../index.html");
    assert.match(html, /<title>MorChranslator<\/title>/);
    assert.match(html, /href="\/public\/MorChranslator.svg"/);
    assert.ok(existsSync(new URL("../public/MorChranslator.svg", import.meta.url)));
    const settings = read("../src/pages/DesktopSettings.tsx");
    assert.match(settings, /<span>MorChranslator<\/span>/);
    assert.doesNotMatch(settings, /Kikitan|Desktop Translator|DesktopTranslator/);
    for (const locale of ["en", "zh-CN", "ja-JP"]) {
        const catalog = JSON.parse(read(`../src/i18n/locales/${locale}.json`));
        for (const [key, value] of Object.entries(catalog)) {
            if (key === "about.originsHelp") { assert.match(value, /Kikitan Translator/); continue; }
            assert.doesNotMatch(value, /Kikitan|Desktop Translator|DesktopTranslator/);
        }
        assert.match(catalog["common.opening"], /MorChranslator/);
    }
});
