using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;

namespace uLipSync.Samples
{

[RequireComponent(typeof(AudioSource))]
public class RuntimeAudioClipPlayer : MonoBehaviour
{
    public InputField inputField;
    
    public void Play()
    {
        StartCoroutine(DownloadAndPlay(false));
    }
    
    public void PlayOneShot()
    {
        StartCoroutine(DownloadAndPlay(true));
    }
    
    IEnumerator DownloadAndPlay(bool oneshot)
    {
        var source = GetComponent<AudioSource>();
        if (!source) yield return null;
        
        var url = inputField.text;
        var type = AudioType.WAV;
        using (var www = UnityWebRequestMultimedia.GetAudioClip(url, type))
        {
            yield return www.SendWebRequest();
            var clip = DownloadHandlerAudioClip.GetContent(www);
            clip.name = inputField.text;
            source.timeSamples = 0;
            if (oneshot)
            {
                source.PlayOneShot(clip);
            }
            else
            {
                source.loop = false;
                source.clip = clip;
                source.Play();
            }
        }
    }
}

}