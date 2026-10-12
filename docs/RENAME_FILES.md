# MorChranslator 修改文件清单

以下为品牌代码提交相对于起点 7276dd5 的完整路径清单；R 为改名，A 为新增，M 为内容更新，D 为 Git 对低相似度改名的旧端表示。旧路径仅用于记录改名前后对应关系，不是当前产品标识。原有 99 项用户改动/删除未纳入这些提交。

```text
M	.github/workflows/build.yml
R091	.idea/.idea.kikitan-translator/.idea/.gitignore	.idea/.idea.MorChranslator/.idea/.gitignore
R100	.idea/.idea.kikitan-translator/.idea/encodings.xml	.idea/.idea.MorChranslator/.idea/encodings.xml
R100	.idea/.idea.kikitan-translator/.idea/indexLayout.xml	.idea/.idea.MorChranslator/.idea/indexLayout.xml
R075	.idea/.idea.kikitan-translator/.idea/tauri-signer.iml	.idea/.idea.MorChranslator/.idea/tauri-signer.iml
R100	.idea/.idea.kikitan-translator/.idea/vcs.xml	.idea/.idea.MorChranslator/.idea/vcs.xml
M	.vscode/launch.json
M	.vscode/settings.json
M	.vscode/tasks.json
M	CODEBASE_REVIEW.md
A	Directory.Build.props
D	KikitanTranslator.Base/Properties/AssemblyInfo.cs
D	KikitanTranslator.Photino/Resources/wwwroot/icon.png
D	KikitanTranslator.Photino/Resources/wwwroot/kikitan_logo.ico
D	KikitanTranslator.Photino/flatpak/com.github.yusufozmen01.kikitan-translator.desktop
R087	KikitanTranslator.Base/Capture/ICapture.cs	MorChranslator.Base/Capture/ICapture.cs
R099	KikitanTranslator.Base/Capture/Loopback.cs	MorChranslator.Base/Capture/Loopback.cs
R098	KikitanTranslator.Base/Capture/Microphone.cs	MorChranslator.Base/Capture/Microphone.cs
R099	KikitanTranslator.Base/Capture/SystemLoopback.cs	MorChranslator.Base/Capture/SystemLoopback.cs
R098	KikitanTranslator.Base/Diagnostics/LatencyDiagnostics.cs	MorChranslator.Base/Diagnostics/LatencyDiagnostics.cs
R092	KikitanTranslator.Base/Logger.cs	MorChranslator.Base/Logger.cs
R092	KikitanTranslator.Base/KikitanTranslator.Base.csproj	MorChranslator.Base/MorChranslator.Base.csproj
R093	KikitanTranslator.Base/Outputs/Chatbox.cs	MorChranslator.Base/Outputs/Chatbox.cs
R085	KikitanTranslator.Base/Outputs/ConsoleOut.cs	MorChranslator.Base/Outputs/ConsoleOut.cs
R086	KikitanTranslator.Base/Outputs/Custom.cs	MorChranslator.Base/Outputs/Custom.cs
R074	KikitanTranslator.Base/Outputs/IOutput.cs	MorChranslator.Base/Outputs/IOutput.cs
R093	KikitanTranslator.Base/Outputs/OSC.cs	MorChranslator.Base/Outputs/OSC.cs
A	MorChranslator.Base/Properties/AssemblyInfo.cs
R099	KikitanTranslator.Base/Recognizers/Bing.cs	MorChranslator.Base/Recognizers/Bing.cs
R098	KikitanTranslator.Base/Recognizers/Gemini.cs	MorChranslator.Base/Recognizers/Gemini.cs
R098	KikitanTranslator.Base/Recognizers/GroqRecognizer.cs	MorChranslator.Base/Recognizers/GroqRecognizer.cs
R086	KikitanTranslator.Base/Recognizers/IRecognizer.cs	MorChranslator.Base/Recognizers/IRecognizer.cs
R098	KikitanTranslator.Base/Recognizers/SpeechSegmenter.cs	MorChranslator.Base/Recognizers/SpeechSegmenter.cs
R092	KikitanTranslator.Base/Recognizers/Types.cs	MorChranslator.Base/Recognizers/Types.cs
R099	KikitanTranslator.Base/StreamingTranslationPreview.cs	MorChranslator.Base/StreamingTranslationPreview.cs
R097	KikitanTranslator.Base/Kikitan.cs	MorChranslator.Base/TranslationPipeline.cs
R095	KikitanTranslator.Base/Translators/DeepLTranslator.cs	MorChranslator.Base/Translators/DeepLTranslator.cs
R079	KikitanTranslator.Base/Translators/GeminiStub.cs	MorChranslator.Base/Translators/GeminiStub.cs
R095	KikitanTranslator.Base/Translators/GoogleCloudTranslator.cs	MorChranslator.Base/Translators/GoogleCloudTranslator.cs
R097	KikitanTranslator.Base/Translators/GoogleTranslate.cs	MorChranslator.Base/Translators/GoogleTranslate.cs
R095	KikitanTranslator.Base/Translators/GroqTranslator.cs	MorChranslator.Base/Translators/GroqTranslator.cs
R086	KikitanTranslator.Base/Translators/ITranslator.cs	MorChranslator.Base/Translators/ITranslator.cs
R098	KikitanTranslator.Base/Translators/TranslationHttp.cs	MorChranslator.Base/Translators/TranslationHttp.cs
R097	KikitanTranslator.Base/Utility/AppConfig.cs	MorChranslator.Base/Utility/AppConfig.cs
R092	KikitanTranslator.Base/Utility/Constants.cs	MorChranslator.Base/Utility/Constants.cs
R098	KikitanTranslator.Base/Utility/CredentialProtection.cs	MorChranslator.Base/Utility/CredentialProtection.cs
R098	KikitanTranslator.Base/Utility/CurlImpersonate.cs	MorChranslator.Base/Utility/CurlImpersonate.cs
R065	KikitanTranslator.Base/Utility/IErrorHandler.cs	MorChranslator.Base/Utility/IErrorHandler.cs
R099	KikitanTranslator.Base/Utility/Languages.cs	MorChranslator.Base/Utility/Languages.cs
R098	KikitanTranslator.Base/Utility/Silero.cs	MorChranslator.Base/Utility/Silero.cs
R100	KikitanTranslator.Base/libcurl/cacert.pem	MorChranslator.Base/libcurl/cacert.pem
R100	KikitanTranslator.Base/libcurl/libcurl.dll	MorChranslator.Base/libcurl/libcurl.dll
R100	KikitanTranslator.Base/libcurl/libcurl.so	MorChranslator.Base/libcurl/libcurl.so
R076	KikitanTranslator.Console/KikitanTranslator.Console.csproj	MorChranslator.Console/MorChranslator.Console.csproj
R057	KikitanTranslator.Console/Program.cs	MorChranslator.Console/Program.cs
R096	KikitanTranslator.Overlay/Form1.Designer.cs	MorChranslator.Overlay/Form1.Designer.cs
R098	KikitanTranslator.Overlay/Form1.cs	MorChranslator.Overlay/Form1.cs
R100	KikitanTranslator.Overlay/KikitanTranslator.Overlay.csproj	MorChranslator.Overlay/MorChranslator.Overlay.csproj
R093	KikitanTranslator.Overlay/OpenVROverlay.cs	MorChranslator.Overlay/OpenVROverlay.cs
R094	KikitanTranslator.Overlay/OverlayReader.cs	MorChranslator.Overlay/OverlayReader.cs
R099	KikitanTranslator.Overlay/OverlayServer.cs	MorChranslator.Overlay/OverlayServer.cs
R084	KikitanTranslator.Overlay/Program.cs	MorChranslator.Overlay/Program.cs
R098	KikitanTranslator.Photino/Connector.cs	MorChranslator.Photino/Connector.cs
R097	KikitanTranslator.Photino/DesktopControlServer.cs	MorChranslator.Photino/DesktopControlServer.cs
R086	KikitanTranslator.Photino/DesktopSubtitleResult.cs	MorChranslator.Photino/DesktopSubtitleResult.cs
R081	KikitanTranslator.Photino/ErrorHandler.cs	MorChranslator.Photino/ErrorHandler.cs
R089	KikitanTranslator.Photino/Handlers/Control.cs	MorChranslator.Photino/Handlers/Control.cs
R092	KikitanTranslator.Photino/Handlers/Fetch.cs	MorChranslator.Photino/Handlers/Fetch.cs
R063	KikitanTranslator.Photino/Handlers/IHandler.cs	MorChranslator.Photino/Handlers/IHandler.cs
R080	KikitanTranslator.Photino/Handlers/ManualTranslate.cs	MorChranslator.Photino/Handlers/ManualTranslate.cs
R096	KikitanTranslator.Photino/Handlers/MessageHandler.cs	MorChranslator.Photino/Handlers/MessageHandler.cs
R092	KikitanTranslator.Photino/Handlers/OpenURL.cs	MorChranslator.Photino/Handlers/OpenURL.cs
R080	KikitanTranslator.Photino/Handlers/Quit.cs	MorChranslator.Photino/Handlers/Quit.cs
R078	KikitanTranslator.Photino/Handlers/SendState.cs	MorChranslator.Photino/Handlers/SendState.cs
R082	KikitanTranslator.Photino/Handlers/UpdateApp.cs	MorChranslator.Photino/Handlers/UpdateApp.cs
R098	KikitanTranslator.Photino/Handlers/UpdateConfig.cs	MorChranslator.Photino/Handlers/UpdateConfig.cs
R088	KikitanTranslator.Photino/Manager.cs	MorChranslator.Photino/Manager.cs
R084	KikitanTranslator.Photino/KikitanTranslator.Photino.csproj	MorChranslator.Photino/MorChranslator.Photino.csproj
R094	KikitanTranslator.Photino/OscWatcher.cs	MorChranslator.Photino/OscWatcher.cs
R093	KikitanTranslator.Photino/OverlayWriter.cs	MorChranslator.Photino/OverlayWriter.cs
R094	KikitanTranslator.Photino/Program.cs	MorChranslator.Photino/Program.cs
A	MorChranslator.Photino/Resources/wwwroot/MorChranslator_logo.ico
A	MorChranslator.Photino/Resources/wwwroot/icon.png
R100	KikitanTranslator.Photino/Resources/wwwroot/silero_vad.onnx	MorChranslator.Photino/Resources/wwwroot/silero_vad.onnx
R095	KikitanTranslator.Photino/SubtitleWriter.cs	MorChranslator.Photino/SubtitleWriter.cs
R100	KikitanTranslator.Photino/UserInterface/.gitignore	MorChranslator.Photino/UserInterface/.gitignore
R100	KikitanTranslator.Photino/UserInterface/.gitmodules	MorChranslator.Photino/UserInterface/.gitmodules
R100	KikitanTranslator.Photino/UserInterface/eslint.config.js	MorChranslator.Photino/UserInterface/eslint.config.js
R070	KikitanTranslator.Photino/UserInterface/index.html	MorChranslator.Photino/UserInterface/index.html
R099	KikitanTranslator.Photino/UserInterface/package-lock.json	MorChranslator.Photino/UserInterface/package-lock.json
R097	KikitanTranslator.Photino/UserInterface/package.json	MorChranslator.Photino/UserInterface/package.json
R100	KikitanTranslator.Photino/UserInterface/postcss.config.js	MorChranslator.Photino/UserInterface/postcss.config.js
R100	KikitanTranslator.Photino/UserInterface/public/desktop-translator.svg	MorChranslator.Photino/UserInterface/public/MorChranslator.svg
R100	KikitanTranslator.Photino/UserInterface/public/OSC.mp4	MorChranslator.Photino/UserInterface/public/OSC.mp4
R100	KikitanTranslator.Photino/UserInterface/public/boothlogo.svg	MorChranslator.Photino/UserInterface/public/boothlogo.svg
R100	KikitanTranslator.Photino/UserInterface/public/buymeacoffeelogo.svg	MorChranslator.Photino/UserInterface/public/buymeacoffeelogo.svg
R100	KikitanTranslator.Photino/UserInterface/public/discordlogo.webp	MorChranslator.Photino/UserInterface/public/discordlogo.webp
R100	KikitanTranslator.Photino/UserInterface/public/images/cn.png	MorChranslator.Photino/UserInterface/public/images/cn.png
R100	KikitanTranslator.Photino/UserInterface/public/images/cn2.png	MorChranslator.Photino/UserInterface/public/images/cn2.png
R100	KikitanTranslator.Photino/UserInterface/public/images/en.png	MorChranslator.Photino/UserInterface/public/images/en.png
R100	KikitanTranslator.Photino/UserInterface/public/images/en2.png	MorChranslator.Photino/UserInterface/public/images/en2.png
R100	KikitanTranslator.Photino/UserInterface/public/images/jp.png	MorChranslator.Photino/UserInterface/public/images/jp.png
R100	KikitanTranslator.Photino/UserInterface/public/images/jp2.png	MorChranslator.Photino/UserInterface/public/images/jp2.png
R100	KikitanTranslator.Photino/UserInterface/public/images/kr.png	MorChranslator.Photino/UserInterface/public/images/kr.png
R100	KikitanTranslator.Photino/UserInterface/public/images/kr2.png	MorChranslator.Photino/UserInterface/public/images/kr2.png
R100	KikitanTranslator.Photino/UserInterface/public/images/settings.png	MorChranslator.Photino/UserInterface/public/images/settings.png
R100	KikitanTranslator.Photino/UserInterface/public/theme.mp4	MorChranslator.Photino/UserInterface/public/theme.mp4
R100	KikitanTranslator.Photino/UserInterface/scripts/.env.example	MorChranslator.Photino/UserInterface/scripts/.env.example
R081	KikitanTranslator.Photino/UserInterface/scripts/build.py	MorChranslator.Photino/UserInterface/scripts/build.py
R069	KikitanTranslator.Photino/UserInterface/scripts/build_and_deploy_kikitan.py	MorChranslator.Photino/UserInterface/scripts/build_and_deploy_MorChranslator.py
R083	KikitanTranslator.Photino/UserInterface/scripts/build_test.py	MorChranslator.Photino/UserInterface/scripts/build_test.py
R100	KikitanTranslator.Photino/UserInterface/scripts/new_announcement.py	MorChranslator.Photino/UserInterface/scripts/new_announcement.py
R100	KikitanTranslator.Photino/UserInterface/scripts/requirements.txt	MorChranslator.Photino/UserInterface/scripts/requirements.txt
R088	KikitanTranslator.Photino/UserInterface/scripts/tauri.test.json	MorChranslator.Photino/UserInterface/scripts/tauri.test.json
R100	KikitanTranslator.Photino/UserInterface/src-tauri/.gitignore	MorChranslator.Photino/UserInterface/src-tauri/.gitignore
R100	KikitanTranslator.Photino/UserInterface/src-tauri/Cargo.lock	MorChranslator.Photino/UserInterface/src-tauri/Cargo.lock
R100	KikitanTranslator.Photino/UserInterface/src-tauri/Cargo.toml	MorChranslator.Photino/UserInterface/src-tauri/Cargo.toml
R100	KikitanTranslator.Photino/UserInterface/src-tauri/build.rs	MorChranslator.Photino/UserInterface/src-tauri/build.rs
R100	KikitanTranslator.Photino/UserInterface/src-tauri/capabilities/migrated.json	MorChranslator.Photino/UserInterface/src-tauri/capabilities/migrated.json
R100	KikitanTranslator.Photino/UserInterface/src-tauri/gen/schemas/acl-manifests.json	MorChranslator.Photino/UserInterface/src-tauri/gen/schemas/acl-manifests.json
R100	KikitanTranslator.Photino/UserInterface/src-tauri/gen/schemas/capabilities.json	MorChranslator.Photino/UserInterface/src-tauri/gen/schemas/capabilities.json
R100	KikitanTranslator.Photino/UserInterface/src-tauri/gen/schemas/desktop-schema.json	MorChranslator.Photino/UserInterface/src-tauri/gen/schemas/desktop-schema.json
R100	KikitanTranslator.Photino/UserInterface/src-tauri/gen/schemas/windows-schema.json	MorChranslator.Photino/UserInterface/src-tauri/gen/schemas/windows-schema.json
R100	KikitanTranslator.Photino/UserInterface/src-tauri/icons/kikitan_logo.ico	MorChranslator.Photino/UserInterface/src-tauri/icons/MorChranslator_logo.ico
R100	KikitanTranslator.Photino/UserInterface/src-tauri/icons/kikitan_overlay.jpg	MorChranslator.Photino/UserInterface/src-tauri/icons/MorChranslator_overlay.jpg
R100	KikitanTranslator.Photino/UserInterface/src-tauri/icons/kikitan_testbuild.ico	MorChranslator.Photino/UserInterface/src-tauri/icons/MorChranslator_testbuild.ico
R100	KikitanTranslator.Photino/UserInterface/src-tauri/src/audio.rs	MorChranslator.Photino/UserInterface/src-tauri/src/audio.rs
R100	KikitanTranslator.Photino/UserInterface/src-tauri/src/data_out.rs	MorChranslator.Photino/UserInterface/src-tauri/src/data_out.rs
R100	KikitanTranslator.Photino/UserInterface/src-tauri/src/filesys.rs	MorChranslator.Photino/UserInterface/src-tauri/src/filesys.rs
R100	KikitanTranslator.Photino/UserInterface/src-tauri/src/main.rs	MorChranslator.Photino/UserInterface/src-tauri/src/main.rs
R100	KikitanTranslator.Photino/UserInterface/src-tauri/src/process_manager.rs	MorChranslator.Photino/UserInterface/src-tauri/src/process_manager.rs
R082	KikitanTranslator.Photino/UserInterface/src-tauri/src/vrc_commands.rs	MorChranslator.Photino/UserInterface/src-tauri/src/vrc_commands.rs
R091	KikitanTranslator.Photino/UserInterface/src-tauri/tauri.conf.json	MorChranslator.Photino/UserInterface/src-tauri/tauri.conf.json
R100	KikitanTranslator.Photino/UserInterface/src/DesktopApp.tsx	MorChranslator.Photino/UserInterface/src/DesktopApp.tsx
R100	KikitanTranslator.Photino/UserInterface/src/components/Quickstart.tsx	MorChranslator.Photino/UserInterface/src/components/Quickstart.tsx
R100	KikitanTranslator.Photino/UserInterface/src/components/Scroll.tsx	MorChranslator.Photino/UserInterface/src/components/Scroll.tsx
R100	KikitanTranslator.Photino/UserInterface/src/components/desktop/CredentialEditor.tsx	MorChranslator.Photino/UserInterface/src/components/desktop/CredentialEditor.tsx
R100	KikitanTranslator.Photino/UserInterface/src/components/desktop/SettingsControls.tsx	MorChranslator.Photino/UserInterface/src/components/desktop/SettingsControls.tsx
R100	KikitanTranslator.Photino/UserInterface/src/components/desktop/SetupGuidance.tsx	MorChranslator.Photino/UserInterface/src/components/desktop/SetupGuidance.tsx
R100	KikitanTranslator.Photino/UserInterface/src/components/desktop/credentialState.ts	MorChranslator.Photino/UserInterface/src/components/desktop/credentialState.ts
R100	KikitanTranslator.Photino/UserInterface/src/components/desktop/theme.ts	MorChranslator.Photino/UserInterface/src/components/desktop/theme.ts
R100	KikitanTranslator.Photino/UserInterface/src/components/desktop/useCredentialDrafts.ts	MorChranslator.Photino/UserInterface/src/components/desktop/useCredentialDrafts.ts
R100	KikitanTranslator.Photino/UserInterface/src/components/desktop/useSettingsShortcuts.ts	MorChranslator.Photino/UserInterface/src/components/desktop/useSettingsShortcuts.ts
R100	KikitanTranslator.Photino/UserInterface/src/dev/preview.tsx	MorChranslator.Photino/UserInterface/src/dev/preview.tsx
R100	KikitanTranslator.Photino/UserInterface/src/globals.css	MorChranslator.Photino/UserInterface/src/globals.css
R100	KikitanTranslator.Photino/UserInterface/src/i18n/LocaleProvider.tsx	MorChranslator.Photino/UserInterface/src/i18n/LocaleProvider.tsx
R100	KikitanTranslator.Photino/UserInterface/src/i18n/error-rules.json	MorChranslator.Photino/UserInterface/src/i18n/error-rules.json
R097	KikitanTranslator.Photino/UserInterface/src/i18n/locales/en.json	MorChranslator.Photino/UserInterface/src/i18n/locales/en.json
R097	KikitanTranslator.Photino/UserInterface/src/i18n/locales/ja-JP.json	MorChranslator.Photino/UserInterface/src/i18n/locales/ja-JP.json
R096	KikitanTranslator.Photino/UserInterface/src/i18n/locales/zh-CN.json	MorChranslator.Photino/UserInterface/src/i18n/locales/zh-CN.json
R100	KikitanTranslator.Photino/UserInterface/src/i18n/localization.ts	MorChranslator.Photino/UserInterface/src/i18n/localization.ts
R100	KikitanTranslator.Photino/UserInterface/src/main.tsx	MorChranslator.Photino/UserInterface/src/main.tsx
R097	KikitanTranslator.Photino/UserInterface/src/page.tsx	MorChranslator.Photino/UserInterface/src/page.tsx
R100	KikitanTranslator.Photino/UserInterface/src/pages/Changelogs.tsx	MorChranslator.Photino/UserInterface/src/pages/Changelogs.tsx
R096	KikitanTranslator.Photino/UserInterface/src/pages/DesktopSettings.tsx	MorChranslator.Photino/UserInterface/src/pages/DesktopSettings.tsx
R099	KikitanTranslator.Photino/UserInterface/src/pages/Kikitan.tsx	MorChranslator.Photino/UserInterface/src/pages/MorChranslator.tsx
R099	KikitanTranslator.Photino/UserInterface/src/pages/Settings.tsx	MorChranslator.Photino/UserInterface/src/pages/Settings.tsx
R100	KikitanTranslator.Photino/UserInterface/src/pages/desktop.css	MorChranslator.Photino/UserInterface/src/pages/desktop.css
R100	KikitanTranslator.Photino/UserInterface/src/style/carousel.css	MorChranslator.Photino/UserInterface/src/style/carousel.css
R100	KikitanTranslator.Photino/UserInterface/src/util/constants.ts	MorChranslator.Photino/UserInterface/src/util/constants.ts
R079	KikitanTranslator.Photino/UserInterface/src/util/localization.ts	MorChranslator.Photino/UserInterface/src/util/localization.ts
R098	KikitanTranslator.Photino/UserInterface/src/util/photino.ts	MorChranslator.Photino/UserInterface/src/util/photino.ts
R100	KikitanTranslator.Photino/UserInterface/src/vite-env.d.ts	MorChranslator.Photino/UserInterface/src/vite-env.d.ts
R100	KikitanTranslator.Photino/UserInterface/tailwind.config.ts	MorChranslator.Photino/UserInterface/tailwind.config.ts
A	MorChranslator.Photino/UserInterface/tests/branding.test.mjs
R100	KikitanTranslator.Photino/UserInterface/tests/credential-hook.test.mjs	MorChranslator.Photino/UserInterface/tests/credential-hook.test.mjs
R100	KikitanTranslator.Photino/UserInterface/tests/credential-state.test.mjs	MorChranslator.Photino/UserInterface/tests/credential-state.test.mjs
R100	KikitanTranslator.Photino/UserInterface/tests/desktop-bridge.test.mjs	MorChranslator.Photino/UserInterface/tests/desktop-bridge.test.mjs
R098	KikitanTranslator.Photino/UserInterface/tests/desktop-entry.test.mjs	MorChranslator.Photino/UserInterface/tests/desktop-entry.test.mjs
R100	KikitanTranslator.Photino/UserInterface/tests/localization.test.mjs	MorChranslator.Photino/UserInterface/tests/localization.test.mjs
R100	KikitanTranslator.Photino/UserInterface/tests/setup-guidance.test.mjs	MorChranslator.Photino/UserInterface/tests/setup-guidance.test.mjs
R100	KikitanTranslator.Photino/UserInterface/tsconfig.json	MorChranslator.Photino/UserInterface/tsconfig.json
R100	KikitanTranslator.Photino/UserInterface/tsconfig.node.json	MorChranslator.Photino/UserInterface/tsconfig.node.json
R085	KikitanTranslator.Photino/UserInterface/vite.config.ts	MorChranslator.Photino/UserInterface/vite.config.ts
R100	KikitanTranslator.Photino/UserInterface/yarn.lock	MorChranslator.Photino/UserInterface/yarn.lock
A	MorChranslator.Photino/flatpak/com.github.yusufozmen01.MorChranslator.desktop
R069	KikitanTranslator.Photino/flatpak/com.github.yusufozmen01.kikitan-translator.json	MorChranslator.Photino/flatpak/com.github.yusufozmen01.MorChranslator.json
R098	KikitanTranslator.Resources/DesktopText.cs	MorChranslator.Resources/DesktopText.cs
R094	KikitanTranslator.Resources/ErrorMessages/messages.Designer.cs	MorChranslator.Resources/ErrorMessages/messages.Designer.cs
R094	KikitanTranslator.Resources/ErrorMessages/messages.ja.Designer.cs	MorChranslator.Resources/ErrorMessages/messages.ja.Designer.cs
R100	KikitanTranslator.Resources/ErrorMessages/messages.ja.resx	MorChranslator.Resources/ErrorMessages/messages.ja.resx
R094	KikitanTranslator.Resources/ErrorMessages/messages.ko.Designer.cs	MorChranslator.Resources/ErrorMessages/messages.ko.Designer.cs
R100	KikitanTranslator.Resources/ErrorMessages/messages.ko.resx	MorChranslator.Resources/ErrorMessages/messages.ko.resx
R100	KikitanTranslator.Resources/ErrorMessages/messages.resx	MorChranslator.Resources/ErrorMessages/messages.resx
R094	KikitanTranslator.Resources/ErrorMessages/messages.zh.Designer.cs	MorChranslator.Resources/ErrorMessages/messages.zh.Designer.cs
R100	KikitanTranslator.Resources/ErrorMessages/messages.zh.resx	MorChranslator.Resources/ErrorMessages/messages.zh.resx
R098	KikitanTranslator.Resources/LanguageResources/languages.Designer.cs	MorChranslator.Resources/LanguageResources/languages.Designer.cs
R098	KikitanTranslator.Resources/LanguageResources/languages.ja.Designer.cs	MorChranslator.Resources/LanguageResources/languages.ja.Designer.cs
R100	KikitanTranslator.Resources/LanguageResources/languages.ja.resx	MorChranslator.Resources/LanguageResources/languages.ja.resx
R098	KikitanTranslator.Resources/LanguageResources/languages.ko.Designer.cs	MorChranslator.Resources/LanguageResources/languages.ko.Designer.cs
R100	KikitanTranslator.Resources/LanguageResources/languages.ko.resx	MorChranslator.Resources/LanguageResources/languages.ko.resx
R100	KikitanTranslator.Resources/LanguageResources/languages.resx	MorChranslator.Resources/LanguageResources/languages.resx
R098	KikitanTranslator.Resources/LanguageResources/languages.zh.Designer.cs	MorChranslator.Resources/LanguageResources/languages.zh.Designer.cs
R100	KikitanTranslator.Resources/LanguageResources/languages.zh.resx	MorChranslator.Resources/LanguageResources/languages.zh.resx
R092	KikitanTranslator.Resources/KikitanTranslator.Resources.csproj	MorChranslator.Resources/MorChranslator.Resources.csproj
A	MorChranslator.Resources/ProductIdentity.cs
R057	KikitanTranslator.Subtitles/KikitanTranslator.Subtitles.csproj	MorChranslator.Subtitles/MorChranslator.Subtitles.csproj
R075	KikitanTranslator.Subtitles/Program.cs	MorChranslator.Subtitles/Program.cs
R098	KikitanTranslator.Subtitles/SubtitleAppearanceDialog.cs	MorChranslator.Subtitles/SubtitleAppearanceDialog.cs
R095	KikitanTranslator.Subtitles/SubtitleHistory.cs	MorChranslator.Subtitles/SubtitleHistory.cs
R086	KikitanTranslator.Subtitles/SubtitlePreferences.cs	MorChranslator.Subtitles/SubtitlePreferences.cs
R097	KikitanTranslator.Subtitles/SubtitleWindow.cs	MorChranslator.Subtitles/SubtitleWindow.cs
R070	kikitan-translator.sln	MorChranslator.sln
R089	kikitan-translator.sln.DotSettings.user	MorChranslator.sln.DotSettings.user
M	PROGRESS.md
M	README.md
M	docs/DIAGNOSTIC_TESTING_ZH.md
M	docs/ENGINEERING_AUDIT.md
M	docs/LATENCY_DIAGNOSTICS.md
M	docs/MANUAL_TESTS.md
M	docs/MODERNIZATION_2026-10-11.md
M	docs/PERFORMANCE.md
A	docs/RENAME_AUDIT.md
A	docs/RENAMING.md
M	docs/SENTENCE_LATENCY.md
M	docs/UI_DESIGN.md
M	docs/VALIDATION.md
M	scripts/Start-LatencyDiagnostics.ps1
M	scripts/build-release.ps1
D	tests/DesktopTranslator.Tests/DesktopTranslator.Tests.csproj
A	tests/MorChranslator.Tests/BrandingTests.cs
R098	tests/DesktopTranslator.Tests/DiagnosticsTests.cs	tests/MorChranslator.Tests/DiagnosticsTests.cs
R097	tests/DesktopTranslator.Tests/LatencyTests.cs	tests/MorChranslator.Tests/LatencyTests.cs
A	tests/MorChranslator.Tests/MorChranslator.Tests.csproj
R092	tests/DesktopTranslator.Tests/Program.cs	tests/MorChranslator.Tests/Program.cs
```

交付记录另外新增/更新：PROGRESS.md、docs/RENAME_AUDIT.md、docs/RENAME_FILES.md、docs/RENAME_RELEASE_NOTES.md、docs/RENAME_VALIDATION.md。
