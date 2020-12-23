using UnityEngine;
using System.Collections.Generic;

namespace uLipSync
{

[RequireComponent(typeof(AudioSource))]
public class LipSync : MonoBehaviour
{
    Queue<LipSyncInfo> lipSyncInfo_ = new Queue<LipSyncInfo>();

    public Config config;
    public bool muteInputSound = false;
    public LipSyncUpdateEvent onLipSyncUpdate = new LipSyncUpdateEvent();

    float[] input_ = null;
    int sampleRate_ = 48000;

    void Awake()
    {
        sampleRate_ = AudioSettings.outputSampleRate;
    }

    void Update()
    {
        lock (lipSyncInfo_)
        {
            while (lipSyncInfo_.Count > 0)
            {
                var info = lipSyncInfo_.Dequeue();
                if (onLipSyncUpdate != null) 
                {
                    onLipSyncUpdate.Invoke(info);
                }
            }
        }
    }

	void OnAudioFilterRead(float[] data, int channels)
	{
        if (!config) return;

        if (input_ == null || input_.Length != data.Length)
        {
            input_ = new float[data.Length / channels];
        }

		for (int i = 0, n = 0; i < data.Length && n < input_.Length; i += channels) 
        {
			input_[n] = data[i];
			++n;
		}

        var df = sampleRate_ / data.Length;
		float vol = Core.GetVolume(input_);
        var formant = Core.GetFormants(input_, config, df);
		var vowel = Core.GetVowel(formant, config);

        lock (lipSyncInfo_)
        {
            lipSyncInfo_.Enqueue(new LipSyncInfo {
                volume = vol,
                formant = formant,
                vowel = vowel,
            });
        }

        if (muteInputSound)
        {
            System.Array.Clear(data, 0, data.Length);   
        }
	}
}

}
