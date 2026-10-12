namespace MorChranslator.Base;

// Hypotheses are revisable. Only preview a prefix seen in two consecutive updates,
// and advance by a sentence or substantial text growth rather than every token.
internal sealed class StreamingTranslationPreview
{
    private string _previous = "";
    private int _scheduledLength, _scheduledWords, _scheduledCjk;

    public string? Observe(string text)
    {
        text = text.Trim();
        var shared = 0;
        while (shared < Math.Min(text.Length, _previous.Length) && text[shared] == _previous[shared]) shared++;
        _previous = text;
        var stable = text[..shared];
        var boundary = 0;
        for (var i = 0; i < stable.Length; i++)
        {
            if (!IsSentenceEnd(stable, i)) continue;
            var end = i + 1;
            while (end < stable.Length && IsClosingQuote(stable[end])) end++;
            boundary = end;
        }
        string candidate;
        if (boundary > _scheduledLength) candidate = stable[..boundary];
        else
        {
            // A shared prefix can end in the middle of an English word. CJK text
            // has no required whitespace, so retain its complete shared characters.
            if (stable.Length == 0) return null;
            if (!IsCjk(stable[^1]) && !char.IsWhiteSpace(stable[^1]))
            {
                var lastSpace = stable.LastIndexOfAny([' ', '\t', '\r', '\n']);
                stable = lastSpace < 0 ? "" : stable[..lastSpace];
            }
            candidate = stable.TrimEnd();
            if (CountWords(candidate) - _scheduledWords < 12 && CountCjk(candidate) - _scheduledCjk < 24) return null;
        }
        if (candidate.Length <= _scheduledLength) return null;
        _scheduledLength = candidate.Length;
        _scheduledWords = CountWords(candidate);
        _scheduledCjk = CountCjk(candidate);
        return candidate;
    }

    public void Reset()
    {
        _previous = "";
        _scheduledLength = _scheduledWords = _scheduledCjk = 0;
    }

    public static bool IsCompatible(string text, string prefix)
    {
        text = text.Trim();
        if (!text.StartsWith(prefix, StringComparison.Ordinal)) return false;
        // Do not accept a prefix ending halfway through a corrected word.
        return text.Length == prefix.Length || !char.IsLetterOrDigit(prefix[^1]) ||
            IsCjk(prefix[^1]) || !char.IsLetterOrDigit(text[prefix.Length]);
    }

    private static bool IsSentenceEnd(string text, int index)
    {
        if (text[index] is '。' or '！' or '？' or '!' or '?') return true;
        if (text[index] != '.') return false;
        var next = index + 1;
        while (next < text.Length && IsClosingQuote(text[next])) next++;
        if (next < text.Length && !char.IsWhiteSpace(text[next])) return false;
        var start = index;
        while (start > 0 && !char.IsWhiteSpace(text[start - 1])) start--;
        var word = text[start..index].TrimStart('"', '\'', '“', '‘');
        // Conservative exclusions; punctuation from STT is not a linguistic guarantee.
        if (word.Length == 1 || word.Contains('.')) return false;
        return word.ToLowerInvariant() is not ("mr" or "mrs" or "ms" or "dr" or "prof" or "sr" or "jr" or "st" or "vs" or "etc");
    }

    private static bool IsClosingQuote(char value) => value is '"' or '\'' or '”' or '’' or '」' or '』' or ')';
    private static bool IsCjk(char value) => value is >= '\u3040' and <= '\u30ff' or >= '\u3400' and <= '\u9fff' or >= '\uac00' and <= '\ud7af';
    private static int CountCjk(string text) => text.Count(IsCjk);
    private static int CountWords(string text)
    {
        var count = 0;
        var inWord = false;
        foreach (var value in text)
        {
            if (char.IsWhiteSpace(value)) inWord = false;
            else if (!inWord) { count++; inWord = true; }
        }
        return count;
    }
}
