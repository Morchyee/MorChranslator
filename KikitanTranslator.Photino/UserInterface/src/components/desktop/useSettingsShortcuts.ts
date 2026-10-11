import {useEffect} from "react";

const pages = ["translation", "speech", "subtitles", "general", "about"];

export function useSettingsShortcuts(navigate: (page: string) => void) {
    useEffect(() => {
        const onKey = (event: KeyboardEvent) => {
            if (event.defaultPrevented || event.repeat || event.isComposing || !event.altKey ||
                event.ctrlKey || event.metaKey || event.shiftKey) return;
            if (event.target instanceof Element && event.target.closest("input, textarea, select, [contenteditable='true']")) return;
            const index = Number(event.key) - 1;
            if (!Number.isInteger(index) || index < 0 || index >= pages.length) return;
            event.preventDefault();
            navigate(pages[index]);
        };
        window.addEventListener("keydown", onKey);
        return () => window.removeEventListener("keydown", onKey);
    }, [navigate]);
}
