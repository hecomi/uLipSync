using UnityEngine;
using System.Collections.Generic;

namespace uLipSync.Samples
{
    
[RequireComponent(typeof(AudioSource))]
public class AudioListPlayer : MonoBehaviour
{
    public List<AudioClip> audioClips = new List<AudioClip>();
    AudioSource _audioSource = null;
    int _index = 0;

    void Start()
    {
        _audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        UpdateTogglePlay();
        UpdateAudioClipChange();
    }

    void UpdateTogglePlay()
    {
        if (!Input.GetKeyDown(KeyCode.Space)) return;
        
        if (_audioSource.isPlaying)
        {
            _audioSource.Stop();
        }
        else
        {
            _audioSource.Play();
        }
    }

    void UpdateAudioClipChange()
    {
        bool forward = Input.GetKeyDown(KeyCode.RightArrow);
        bool backward = Input.GetKeyDown(KeyCode.LeftArrow);
        if (!forward && !backward) return;

        if (forward) _index += 1;
        if (backward) _index -= 1;

        int n = audioClips.Count;
        if (_index < 0) _index += n;
        _index = _index % n;

        _audioSource.clip = audioClips[_index];
        _audioSource.Play();
    }
}

}