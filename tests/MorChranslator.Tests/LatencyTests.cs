using System.Collections.Concurrent;
using System.Threading.Channels;
using MorChranslator.Base;
using MorChranslator.Base.Outputs;
using MorChranslator.Base.Translators;
using MorChranslator.Recognizers;
using MorChranslator.Subtitles;
using MorChranslator.Utility;

internal static class LatencyTests
{
    public static void Register(Action<string, Func<Task>> test, Action<bool, string> assert)
    {
        void Check(bool value, string message = "Latency regression assertion failed") => assert(value, message);
        async Task Wait(Func<bool> condition)
        {
            using var timeout = new CancellationTokenSource(3000);
            while (!condition()) await Task.Delay(10, timeout.Token);
        }

        test("Sentence previews require stable punctuation and suppress repeated hypotheses", () =>
        {
            var policy = new StreamingTranslationPreview();
            Check(policy.Observe("Hello world.") == null);
            Check(policy.Observe("Hello world. Next sentence") == "Hello world.");
            for (var i = 0; i < 10; i++) Check(policy.Observe("Hello world. Next sentence") == null);
            Check(policy.Observe("Hello world. Next sentence!") == null);
            Check(policy.Observe("Hello world. Next sentence! More") == "Hello world. Next sentence!");
            policy.Reset();
            Check(policy.Observe("Hello world.") == null && policy.Observe("Hello world.") == "Hello world.");
            return Task.CompletedTask;
        });
        test("Sentence boundaries handle CJK and closing quotes without splitting decimals or abbreviations", () =>
        {
            foreach (var text in new[] { "Dr. Smith is here", "Mr. Jones is here", "The value is 3.14 today", "U.S. policy is changing" })
            {
                var policy = new StreamingTranslationPreview();
                Check(policy.Observe(text) == null && policy.Observe(text) == null, text);
            }
            foreach (var sentence in new[] { "你好世界。", "今日は晴れです！", "He said \"Go.\"", "Is this ready?" })
            {
                var policy = new StreamingTranslationPreview();
                Check(policy.Observe(sentence) == null);
                Check(policy.Observe(sentence + " More") == sentence, sentence);
            }
            Check(!StreamingTranslationPreview.IsCompatible("Hello worldly", "Hello world"));
            Check(!StreamingTranslationPreview.IsCompatible("A revised sentence.", "An old sentence."));
            Check(StreamingTranslationPreview.IsCompatible("Hello world. More", "Hello world."));
            return Task.CompletedTask;
        });
        test("Continuous unpunctuated hypotheses preview stable whole words and CJK growth", () =>
        {
            var policy = new StreamingTranslationPreview();
            var words = string.Join(" ", Enumerable.Range(1, 12).Select(i => "word" + i));
            Check(policy.Observe(words + " thir") == null);
            Check(policy.Observe(words + " thirteen") == words);
            Check(policy.Observe(words + " thirteen fourteen") == null);
            var cjk = new StreamingTranslationPreview();
            var prefix = new string('你', 24);
            Check(cjk.Observe(prefix + "好") == null);
            Check(cjk.Observe(prefix + "再") == prefix);
            return Task.CompletedTask;
        });
        test("A stable sentence translates before final STT without shrinking source or duplicating subtitles", async () =>
        {
            using var f = new PreviewFixture();
            f.Recognizer.Emit("Hello world.", false);
            f.Recognizer.Emit("Hello world. Next sentence", false);
            var preview = await f.Translator.Next();
            Check(preview.Text == "Hello world.");
            preview.Complete.TrySetResult("こんにちは");
            await Wait(() => f.Events.Any(e => e.TranslatedText == "こんにちは"));
            var shown = f.Events.Last();
            Check(!shown.IsFinal && shown.IsTranslationUpdate && shown.OriginalText == "Hello world. Next sentence");
            f.Recognizer.Emit("Hello world. Next sentence continues", false);
            Check(f.Events.Last().TranslatedText == "こんにちは" && f.Translator.Calls == 1);
            f.Recognizer.Emit("Hello world. Next sentence continues.", true);
            var final = await f.Translator.Next();
            Check(final.Text == "Hello world. Next sentence continues.");
            final.Complete.TrySetResult("final translation");
            await Wait(() => f.Events.Any(e => e.TranslatedText == "final translation"));
            var history = new SubtitleHistory(5);
            foreach (var entry in f.Events) history.Apply(entry);
            Check(history.Entries.Count == 1 && history.Entries[0].Id == shown.Id && history.Entries[0].IsFinal);
        });
        test("An identical final result reuses a completed preview and sends delayed output once", async () =>
        {
            using var f = new PreviewFixture();
            var delayed = new ConcurrentQueue<string>();
            f.Pipeline.AddOutput(new Custom((_, translated, _) => delayed.Enqueue(translated), true));
            f.Recognizer.Emit("Hello world.", false); f.Recognizer.Emit("Hello world.", false);
            (await f.Translator.Next()).Complete.TrySetResult("translated");
            await Wait(() => f.Events.Any(e => e.IsTranslationUpdate));
            Check(delayed.IsEmpty);
            f.Recognizer.Emit("Hello world.", true);
            await Wait(() => !delayed.IsEmpty);
            Check(delayed.SequenceEqual(new[] { "translated" }) && f.Translator.Calls == 1);
            Check(f.Events.Last().IsFinal && f.Events.Last().TranslatedText == "translated");
        });
        test("Changing translation direction does not reuse a preview from the previous language", async () =>
        {
            using var f = new PreviewFixture();
            f.Recognizer.Emit("Hello world.", false); f.Recognizer.Emit("Hello world.", false);
            (await f.Translator.Next()).Complete.TrySetResult("Japanese preview");
            await Wait(() => f.Events.Any(e => e.IsTranslationUpdate));
            AppConfig.ConfigObject.TargetLanguage = "zh";
            f.Recognizer.Emit("Hello world.", true);
            Check(f.Events.Last().TranslatedText == "");
            var final = await f.Translator.Next();
            final.Complete.TrySetResult("Chinese final");
            await Wait(() => f.Events.Any(e => e.TranslatedText == "Chinese final"));
            Check(f.Translator.Calls == 2);
        });
        test("Final STT cancels a blocked preview and starts authoritative translation promptly", async () =>
        {
            using var f = new PreviewFixture();
            f.Recognizer.Emit("Hello world.", false); f.Recognizer.Emit("Hello world. More", false);
            var preview = await f.Translator.Next();
            f.Recognizer.Emit("Hello world. More complete.", true);
            Check(preview.Token.IsCancellationRequested);
            var final = await f.Translator.Next(); // The unfinished preview is never released by the test.
            Check(final.Text == "Hello world. More complete.");
            final.Complete.TrySetResult("authoritative");
            await Wait(() => f.Events.Any(e => e.TranslatedText == "authoritative"));
            Check(f.Events.Where(e => e.IsTranslationUpdate).All(e => e.IsFinal));
        });
        test("Revised hypotheses invalidate previews even if a provider ignores cancellation", async () =>
        {
            using var f = new PreviewFixture(ignoreCancellation: true);
            f.Recognizer.Emit("An old sentence.", false); f.Recognizer.Emit("An old sentence. More", false);
            var preview = await f.Translator.Next();
            f.Recognizer.Emit("A corrected sentence", false);
            Check(preview.Token.IsCancellationRequested && f.Events.Last().TranslatedText == "");
            f.Recognizer.Emit("A corrected sentence.", true);
            preview.Complete.TrySetResult("obsolete translation");
            var final = await f.Translator.Next();
            final.Complete.TrySetResult("correct translation");
            await Wait(() => f.Events.Any(e => e.TranslatedText == "correct translation"));
            Check(f.Events.All(e => e.TranslatedText != "obsolete translation"));
        });
        test("Pending previews coalesce separately and never displace queued final results", async () =>
        {
            using var f = new PreviewFixture();
            f.Recognizer.Emit("first final", true);
            var first = await f.Translator.Next();
            f.Recognizer.Emit("second final", true);
            f.Recognizer.Emit("First sentence.", false); f.Recognizer.Emit("First sentence. Second sentence.", false);
            f.Recognizer.Emit("First sentence. Second sentence. Third sentence.", false);
            f.Recognizer.Emit("First sentence. Second sentence. Third sentence. More", false);
            Check(f.Translator.Calls == 1);
            first.Complete.TrySetResult("first");
            var second = await f.Translator.Next();
            Check(second.Text == "second final"); second.Complete.TrySetResult("second");
            var preview = await f.Translator.Next();
            Check(preview.Text == "First sentence. Second sentence. Third sentence.");
            preview.Complete.TrySetResult("latest preview");
            await Wait(() => f.Events.Any(e => e.TranslatedText == "latest preview"));
            Check(f.Translator.Calls == 3);
        });
        test("Stop cancels provisional work and suppresses further subtitle updates", async () =>
        {
            using var f = new PreviewFixture();
            f.Recognizer.Emit("Hello world.", false); f.Recognizer.Emit("Hello world.", false);
            var preview = await f.Translator.Next();
            await Task.Run(f.Pipeline.Stop).WaitAsync(TimeSpan.FromSeconds(2));
            f.Recognizer.Emit("Hello world.", true);
            Check(preview.Token.IsCancellationRequested && f.Events.All(e => !e.IsTranslationUpdate));
        });
        test("Legacy, transcription-only and Gemini paths keep existing output behavior", async () =>
        {
            using (var legacy = new PreviewFixture(loopback: false))
            {
                legacy.Recognizer.Emit("Hello world.", false); legacy.Recognizer.Emit("Hello world.", false);
                Check(legacy.Translator.Calls == 0);
                legacy.Recognizer.Emit("Hello world.", true);
                (await legacy.Translator.Next()).Complete.TrySetResult("legacy final");
                await Wait(() => legacy.Events.Any(e => e.TranslatedText == "legacy final"));
                Check(legacy.Translator.Calls == 1);
            }
            using (var transcription = new PreviewFixture(transcriptionOnly: true))
            {
                transcription.Recognizer.Emit("Hello world.", false); transcription.Recognizer.Emit("Hello world.", false);
                transcription.Recognizer.Emit("Hello world.", true);
                await Wait(() => transcription.Events.Any(e => e.IsTranslationUpdate));
                Check(transcription.Translator.Calls == 0);
            }
            using (var gemini = new PreviewFixture(recognizer: 2))
            {
                gemini.Recognizer.Emit("Hello|こんにちは", false);
                gemini.Recognizer.Emit("Hello|こんにちは", true);
                Check(gemini.Translator.Calls == 0 && gemini.Events.Last().IsFinal && gemini.Events.Last().TranslatedText == "こんにちは");
            }
        });
        test("Speech segmentation preserves short pauses and ends at the exact silence boundary", () =>
        {
            var segmenter = new SpeechSegmenter(16000);
            var segments = new List<float[]>();
            segmenter.Push(new float[3840], true, segments.Add);
            segmenter.Push(new float[960], false, segments.Add);
            segmenter.Push(new float[3840], true, segments.Add);
            segmenter.Push(new float[5759], false, segments.Add);
            Check(segments.Count == 0);
            segmenter.Push(new float[1], false, segments.Add);
            Check(segments.Count == 1 && segments[0].Length == 14400);
            return Task.CompletedTask;
        });
        test("Continuous speech caps are packet-independent without lost or duplicated samples", () =>
        {
            var audio = Enumerable.Range(0, 208000).Select(i => i / 208000f).ToArray();
            foreach (var packetSize in new[] { 1, 960, 7937, audio.Length })
            {
                var segmenter = new SpeechSegmenter(16000);
                var segments = new List<float[]>();
                for (var offset = 0; offset < audio.Length; offset += packetSize)
                    segmenter.Push(audio.AsSpan(offset, Math.Min(packetSize, audio.Length - offset)), true, segments.Add);
                segmenter.Push(new float[5760], false, segments.Add);
                Check(segments.Select(s => s.Length).SequenceEqual(new[] { 80000, 80000, 53760 }));
                Check(segments.SelectMany(s => s).Take(audio.Length).SequenceEqual(audio));
            }
            return Task.CompletedTask;
        });
    }

    private sealed class PreviewFixture : IDisposable
    {
        public readonly FakeRecognizer Recognizer = new();
        public readonly TranslationProbe Translator;
        public readonly TranslationPipeline Pipeline;
        public readonly ConcurrentQueue<DesktopSubtitleResult> Events = new();
        public PreviewFixture(bool loopback = true, bool transcriptionOnly = false, int recognizer = 0, bool ignoreCancellation = false)
        {
            AppConfig.ConfigObject = new() { Recognizer = recognizer, SourceLanguage = "en", TargetLanguage = "ja", SpeechToTextOnly = transcriptionOnly };
            Translator = new TranslationProbe(ignoreCancellation);
            Pipeline = new TranslationPipeline(Recognizer, Translator, new Errors(), loopback);
            Pipeline.OnSubtitle += (id, source, translation, final, update) => Events.Enqueue(new(source, translation, final, id, update));
            Pipeline.Start();
        }
        public void Dispose() { Translator.ReleaseAll(); Pipeline.Dispose(); }
    }

    private sealed record TranslationRequest(string Text, CancellationToken Token)
    {
        public readonly TaskCompletionSource<string?> Complete = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
    private sealed class TranslationProbe(bool ignoreCancellation) : ITranslator
    {
        private readonly Channel<TranslationRequest> _requests = Channel.CreateUnbounded<TranslationRequest>();
        private readonly ConcurrentQueue<TranslationRequest> _all = new();
        public int Calls => _all.Count;
        public Task<TranslationRequest> Next() => _requests.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(2));
        public string? Translate(string text, string source, string target) => throw new NotSupportedException();
        public async Task<string?> TranslateAsync(string text, string source, string target, CancellationToken token)
        {
            var request = new TranslationRequest(text, token);
            _all.Enqueue(request); _requests.Writer.TryWrite(request);
            return ignoreCancellation ? await request.Complete.Task : await request.Complete.Task.WaitAsync(token);
        }
        public void ReleaseAll() { foreach (var request in _all) request.Complete.TrySetResult(""); }
        public void Dispose() { }
    }
}
