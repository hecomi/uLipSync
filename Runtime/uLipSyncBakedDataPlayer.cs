using UnityEngine;
using System.Collections.Generic;

namespace uLipSync
{

public class uLipSyncBakedDataPlayer : MonoBehaviour
{
    public AudioSource audioSource = null;
    public BakedData bakedData = null;
    public bool playOnAwake = true;
    public bool playAudioSource = true;
    [Range(0f, 0.3f)] public float timeOffset = 0.1f;
    public LipSyncUpdateEvent onLipSyncUpdate = new LipSyncUpdateEvent();

    bool _isFirstPlay = true;
    bool _isPlaying = false;
    double _startTime = 0.0;
    public bool isPlaying { get => _isPlaying; }

    void OnEnable()
    {
        _isFirstPlay = true;
    }

    void OnDisable()
    {
        _isFirstPlay = false;
    }

    void Update()
    {
        if (!_isPlaying)
        {
            if (_isFirstPlay && playOnAwake)
            {
                _isFirstPlay = false;
                Play();
            }
            else
            {
                return;
            }
        }

        if (!bakedData)
        {
            Stop();
            return;
        }

        if (AudioSettings.dspTime - _startTime > bakedData.duration)
        {
            Stop();
            return;
        }

        UpdateCallback();
    }

    void UpdateCallback()
    {
        if (!bakedData) return;

        var t = AudioSettings.dspTime - _startTime;
        var info = bakedData.GetLipSyncInfo((float)t + timeOffset);
        onLipSyncUpdate.Invoke(info);
    }

    public void Play()
    {
        if (!bakedData) return;

        _isPlaying = true;
        _startTime = AudioSettings.dspTime;

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
        if (!audioSource)
        {
            audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
        }
        audioSource.clip = bakedData.audioClip;
        audioSource.loop = false;
        audioSource.PlayDelayed(0.01f);
    }

    void StopAudioSource()
    {
        if (!audioSource) return;

        audioSource.Stop();
    }
}

}
