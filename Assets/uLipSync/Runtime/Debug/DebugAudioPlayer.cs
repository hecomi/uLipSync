using UnityEngine;

namespace uLipSync.Debugging
{

public class DebugAudioPlayer : MonoBehaviour
{
    public string directory;

    bool _isNewClipSet = false;
    AudioClip _newClip = null;

    public AudioClip newClip
    {
        get => _newClip;
        set 
        { 
            _newClip = value;
            _isNewClipSet = true;
        }
    }

    void Update()
    {
        if (!_isNewClipSet) return;
        _isNewClipSet = false;
        
        var source = GetComponent<AudioSource>();
        if (!source) return;

        source.Stop();
        source.clip = _newClip;
        source.Play();
    }
}

}