using UnityEngine;
using System.Collections.Generic;

namespace uLipSync
{

public class LipSync : MonoBehaviour
{
    Queue<LipSyncInfo> lipSyncInfo_ = new Queue<LipSyncInfo>();

    public Config config;
    public bool muteInputSound = false;
    public LipSyncUpdateEvent onLipSyncUpdate = new LipSyncUpdateEvent();

    public int sampleCount 
    { 
        get { return config ? config.sampleCount : 1024; }
    }

    float[] data_ = null;
    int sampleRate_ = 48000;
    int dataIndex_ = 0;

    void Awake()
    {
        sampleRate_ = AudioSettings.outputSampleRate;
        data_ = new float[sampleCount];
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

	void OnAudioFilterRead(float[] input, int channels)
	{
        if (data_ == null) return;

        dataIndex_ = dataIndex_ % data_.Length;
		for (int i = 0; i < input.Length; i += channels) 
        {
			data_[dataIndex_] = input[i];
            dataIndex_ = (dataIndex_ + 1) % data_.Length;
		}

        var df = sampleRate_ / input.Length;
		float vol = Core.GetVolume(data_);
        var formant = Core.GetFormants(data_, dataIndex_, config, df);
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
            System.Array.Clear(input, 0, input.Length);   
        }
	}
}

}
