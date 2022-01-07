using UnityEngine;
using System.Collections.Generic;

namespace uLipSync
{

[RequireComponent(typeof(AudioSource))]
public class uLipSyncBakedDataPlayer : MonoBehaviour
{
    public BakedData bakedData = null;
    public bool playOnAwake = true;
    public bool playAudioSource = true;
    public LipSyncUpdateEvent onLipSyncUpdate = new LipSyncUpdateEvent();

    bool _isPlaying = false;
    float _startTime = 0f;
    public bool isPlaying { get => _isPlaying; }

    void Awake()
    {
        if (playOnAwake)
        {
            Play();
        }
    }

    void Update()
    {
        if (!_isPlaying) return;

        if (!bakedData)
        {
            Stop();
            return;
        }

        var t = Time.time - _startTime;
        if (t > bakedData.duration)
        {
            Stop();
            return;
        }

        var frame = bakedData.GetFrame(t);
        var info = new LipSyncInfo();
        info.phonemeRatios = new Dictionary<string, float>();

        float maxRatio = 0f;
        foreach (var pr in frame.phonemes)
        {
            if (pr.ratio > maxRatio)
            {
                info.phoneme = pr.phoneme;
            }
            info.phonemeRatios.Add(pr.phoneme, pr.ratio);
        }

        float minVol = Common.defaultMinVolume;
        float maxVol = Common.defaultMaxVolume;
        float normVol = Mathf.Log10(frame.volume);
        normVol = (normVol - minVol) / (maxVol - minVol);
        normVol = Mathf.Clamp(normVol, 0f, 1f);
        info.volume = normVol;
        info.rawVolume = frame.volume;

        onLipSyncUpdate.Invoke(info);
    }

    public void Play()
    {
        if (!bakedData) return;

        _isPlaying = true;
        _startTime = Time.time;

        if (playAudioSource) PlayAudioSource();
    }

    public void Play(BakedData data)
    {
        bakedData = data;
        Play();
    }

    public void Stop()
    {
        if (!_isPlaying) return;

        _isPlaying = false;

        if (playAudioSource) StopAudioSource();
    }

    void PlayAudioSource()
    {
        var source = GetComponent<AudioSource>();
        if (!source) return;

        source.clip = bakedData.audioClip;
        source.loop = false;
        source.Play();
    }

    void StopAudioSource()
    {
        var source = GetComponent<AudioSource>();
        if (!source) return;

        source.Stop();
    }
}

}
