import {Button, Typography} from "@mui/material";
import {config} from "../../util/constants";
import {Translate} from "../../i18n/localization";

export function missingSetup(c: config): {page: "speech" | "translation", provider: string}[] {
    if (!c.desktop_translation) return [];
    const steps: {page: "speech" | "translation", provider: string}[] = [];
    if (c.recognizer === 1 && !c.groq_api_key_configured) steps.push({page: "speech", provider: "Groq Whisper"});
    if (c.recognizer === 2 && !c.gemini_api_key_configured) steps.push({page: "speech", provider: "Gemini Live"});
    // Gemini supplies both source and translation through the recognition pipeline.
    if (!c.speech_to_text_only && c.recognizer !== 2) {
        const providers = {
            groq: {name: "Groq", configured: c.groq_api_key_configured},
            google: {name: "Google Cloud Translation", configured: c.google_cloud_api_key_configured},
            deepl: {name: "DeepL API", configured: c.deepl_api_key_configured}
        };
        const provider = providers[c.desktop_translation_provider];
        if (provider && !provider.configured) steps.push({page: "translation", provider: provider.name});
    }
    return steps;
}

export default function SetupGuidance({config, t, onNavigate}: {
    config: config, t: Translate, onNavigate: (page: string) => void
}) {
    const steps = missingSetup(config);
    if (steps.length === 0) return null;
    return <section className="setup-guidance" aria-labelledby="setup-title">
        <Typography id="setup-title" component="h2" variant="h2">{t("setup.title")}</Typography>
        <Typography color="text.secondary" variant="body2">{t("setup.description")}</Typography>
        <ul>{steps.map(step => <li key={step.page}>
            <span>{t(step.page === "speech" ? "setup.speech" : "setup.translation", {provider: step.provider})}</span>
            <Button variant="outlined" size="small" onClick={() => onNavigate(step.page)}>
                {t(step.page === "speech" ? "setup.configureSpeech" : "setup.configureTranslation")}
            </Button>
        </li>)}</ul>
        <Typography color="text.secondary" variant="body2">{t("setup.privacy")}</Typography>
    </section>;
}
