using UnityEngine;

namespace uLipSync
{

[RequireComponent(typeof(AudioSource))]
public class uLipSyncCalibrationAudioPlayer : MonoBehaviour
{
    public AudioClip clip;
    public float start = 0f;
    public float end = 1f;
    public float crossFadeDuration = 0.05f;

    AudioClip _tmpClip;
    float[] _data;
    int _currentPos = 0;
    bool _audioReadCalled = false;
    int _sampleRate = 0;
    int _sampleCount = 0;
    int _channels = 1;
    int _crossFadeDataCount = 0;
    int playDataSampleCount => _sampleCount - _crossFadeDataCount;

    public bool isPlaying
    {
        get 
        {
            var source = GetComponent<AudioSource>();
            return source ? source.isPlaying : false;
        }
    }

    void OnEnable()
    {
        Apply();
    }

    void OnDisable()
    {
        Destroy(_tmpClip);
    }

    void Update()
    {
        if (!_audioReadCalled) return;

        _sampleRate = AudioSettings.outputSampleRate;
    }

    public void Apply()
    {
        if (!clip) return;

        var source = GetComponent<AudioSource>();
        if (!source) return;

        var startPos = (int)(clip.samples * start);
        var endPos = (int)(clip.samples * end);
        var freq = clip.frequency;
        _sampleCount = endPos - startPos;
        _channels = clip.channels;
        _crossFadeDataCount = (int)(_sampleRate * crossFadeDuration);
        _crossFadeDataCount = Mathf.Min(_crossFadeDataCount, _sampleCount / 2 - 1);

        _data = new float[_sampleCount * _channels];
        clip.GetData(_data, startPos);

        var name = $"{clip.name}-{startPos}-{endPos}";
        _tmpClip = AudioClip.Create(
            name, 
            playDataSampleCount,
            _channels, 
            freq, 
            true, 
            OnAudioRead, 
            OnAudioSetPosition);
        source.clip = _tmpClip;
        source.loop = true;
        source.Play();
    }

    void OnAudioRead(float[] data)
    {
        _audioReadCalled = true;

        for (int i = 0; i < data.Length / _channels; ++i)
        {
            for (int ch = 0; ch < _channels; ++ch)
            {
                int index = i * _channels + ch;

                if (_currentPos < _crossFadeDataCount)
                {
                    float t = (float)_currentPos / _crossFadeDataCount;
                    float sin = Mathf.Sin(Mathf.PI * 0.5f * t);
                    float cos = Mathf.Cos(Mathf.PI * 0.5f * t);
                    int indexS = _currentPos;
                    int indexE = _sampleCount - (_crossFadeDataCount - _currentPos);
                    float dataS = _data[indexS * _channels + ch];
                    float dataE = _data[indexE * _channels + ch];
                    data[index] = dataS * sin + dataE * cos;
                }
                else
                {
                    data[index] = _data[_currentPos * _channels + ch];
                }
            }

            _currentPos = (_currentPos + 1) % playDataSampleCount;
        }
    }

    void OnAudioSetPosition(int newPosition)
    {
        _currentPos = newPosition;
    }

    public void Pause()
    {
        var source = GetComponent<AudioSource>();
        if (!source) return;

        source.Pause();
    }

    public void UnPause()
    {
        var source = GetComponent<AudioSource>();
        if (!source) return;

        source.UnPause();
    }
}

}
