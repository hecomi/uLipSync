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
    int _position = 0;
    bool _audioReadCalled = false;
    float _audioReadTime = 0f;
    int _audioReadBasePos = 0;
    int _sampleRate = 0;
    int _samples = 0;
    int _channels = 1;
    int _crossFadeLen = 0;

    public float position 
    { 
        get
        {
            if (_data == null || !_tmpClip) return 0f;
            var dt = _audioReadCalled ? 0f : Time.time - _audioReadTime;
            var pos = (float)_audioReadBasePos / _data.Length;
            pos += dt / _tmpClip.length;
            while (pos > 1f) pos -= 1f;
            return pos;
        }
    }

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
        if (_audioReadCalled)
        {
            _audioReadCalled = false;
            _audioReadTime = Time.time;
            _sampleRate = AudioSettings.outputSampleRate;
        }
    }

    public void Apply()
    {
        if (!clip) return;

        var source = GetComponent<AudioSource>();
        if (!source) return;

        var startPos = (int)(clip.samples * start);
        var endPos = (int)(clip.samples * end);
        var freq = clip.frequency;
        _samples = endPos - startPos;
        _channels = clip.channels;
        _crossFadeLen = (int)(_sampleRate * crossFadeDuration);
        _crossFadeLen = Mathf.Min(_crossFadeLen, _samples / 2);

        _data = new float[_samples * _channels];
        clip.GetData(_data, startPos);

        var name = $"{clip.name}-{startPos}-{endPos}";
        _tmpClip = AudioClip.Create(
            name, 
            _samples - _crossFadeLen, 
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
        _audioReadBasePos = _position;

        for (int i = 0; i < data.Length / _channels; ++i)
        {
            for (int ch = 0; ch < _channels; ++ch)
            {
                if (_position < _crossFadeLen)
                {
                    float t = (float)_position / _crossFadeLen;
                    float sin = Mathf.Sin(Mathf.PI * 0.5f * t);
                    float cos = Mathf.Cos(Mathf.PI * 0.5f * t);
                    int indexS = _position;
                    int indexE = _samples - (_crossFadeLen - _position);
                    float dataS = _data[indexS * _channels + ch];
                    float dataE = _data[indexE * _channels + ch];
                    data[i] = dataS * sin + dataE * cos;
                }
                else
                {
                    data[i] = _data[_position * _channels + ch];
                }
            }

            _position = (_position + 1) % (_samples - _crossFadeLen);
        }
    }

    void OnAudioSetPosition(int newPosition)
    {
        _position = newPosition;
    }
}

}
