namespace KikitanTranslator.Recognizers;

// Sample-count timing remains deterministic when capture delivers packets in bursts.
internal sealed class SpeechSegmenter(uint sampleRate)
{
    private readonly float[] _preRoll = new float[checked((int)sampleRate / 5)]; // 200 ms
    private readonly int _endSilence = checked((int)sampleRate * 360 / 1000);
    private readonly int _minimumSpeech = checked((int)sampleRate * 240 / 1000);
    private readonly int _maximumSamples = checked((int)sampleRate * 5); // Bound batch STT latency during continuous speech.
    private readonly List<float> _buffer = [];
    private int _preRollOffset, _preRollCount, _speechSamples, _silentSamples;
    private bool _collecting;

    public void Push(ReadOnlySpan<float> samples, bool speech, Action<float[]> completed)
    {
        foreach (var sample in samples)
        {
            if (!_collecting)
            {
                if (!speech)
                {
                    _preRoll[_preRollOffset] = sample;
                    _preRollOffset = (_preRollOffset + 1) % _preRoll.Length;
                    _preRollCount = Math.Min(_preRollCount + 1, _preRoll.Length);
                    continue;
                }
                for (var i = 0; i < _preRollCount; i++)
                    _buffer.Add(_preRoll[(_preRollOffset - _preRollCount + i + _preRoll.Length) % _preRoll.Length]);
                _preRollOffset = _preRollCount = 0;
                _collecting = true;
            }

            _buffer.Add(sample);
            if (speech) { _speechSamples++; _silentSamples = 0; }
            else _silentSamples++;

            if (_silentSamples < _endSilence && _buffer.Count < _maximumSamples) continue;
            var audio = _speechSamples >= _minimumSpeech ? _buffer.ToArray() : null;
            Reset();
            if (audio != null) completed(audio);
        }
    }

    public void Reset()
    {
        _buffer.Clear();
        _preRollOffset = _preRollCount = _speechSamples = _silentSamples = 0;
        _collecting = false;
    }
}
