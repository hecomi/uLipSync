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

    public float deltaFreq
    {
        get { return (float)sampleRate_ / sampleCount; }
    }

    float[] data_ = null;
    int sampleRate_ = 48000;
    int dataIndex_ = 0;

    public float[] H { get; private set; } = null;
    public LipSyncInfo lastInfo { get; private set; }

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
                lastInfo = lipSyncInfo_.Dequeue();
                if (onLipSyncUpdate != null) 
                {
                    onLipSyncUpdate.Invoke(lastInfo);
                }
            }
        }
    }

	void OnAudioFilterRead(float[] input, int channels)
	{
        if (data_ == null) return;

        int preIndex = dataIndex_;
        dataIndex_ = dataIndex_ % data_.Length;
		for (int i = 0; i < input.Length; i += channels) 
        {
			data_[dataIndex_] = input[i];
            dataIndex_ = (dataIndex_ + 1) % data_.Length;
		}

        if (muteInputSound)
        {
            System.Array.Clear(input, 0, input.Length);   
        }

        if (dataIndex_ > preIndex) return;

		float vol = Core.GetVolume(data_);

        H = Core.CalcLpcSpectralEnvelope(data_, dataIndex_, config);
        var formant = Core.GetFormants(H, deltaFreq);

		var vowel = Core.GetVowel(formant, config);

        lock (lipSyncInfo_)
        {
            lipSyncInfo_.Enqueue(new LipSyncInfo {
                volume = vol,
                formant = formant,
                vowel = vowel,
            });
        }
	}
}

}
