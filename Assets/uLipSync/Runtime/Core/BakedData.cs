using UnityEngine;
using Unity.Burst;
using System.Collections.Generic;

namespace uLipSync
{

[System.Serializable]
public struct BakedPhonemeRatio
{
    public string phoneme;
    public float ratio;
}

[System.Serializable]
public struct BakedFrame
{
    public float volume;
    public List<BakedPhonemeRatio> phonemes;

    public static BakedFrame zero
    {
        get => new BakedFrame()
        {
            volume = 0,
            phonemes = new List<BakedPhonemeRatio>(),
        };
    }
}

[BurstCompile]
[CreateAssetMenu(menuName = Common.AssetName + "/Baked Data")]
public class BakedData : ScriptableObject
{
    public Profile profile;
    public AudioClip audioClip;

    [HideInInspector] public Profile bakedProfile;
    [HideInInspector] public AudioClip bakedAudioClip;

    public float duration = 0f;
    public List<BakedFrame> frames = new List<BakedFrame>();

    public bool isValid
    {
        get => duration > 0f && frames.Count > 0;
    }

    public bool isDataChanged
    {
        get => (profile != bakedProfile) || (audioClip != bakedAudioClip);
    }

    public BakedFrame GetFrame(float t)
    {
        if (frames == null || frames.Count == 0) return BakedFrame.zero;

        var phonemeCount = frames[0].phonemes.Count;
        var frame = new BakedFrame
        {
            phonemes = new List<BakedPhonemeRatio>()
        };
        int index0 = (int)Mathf.Floor(t * 60f);
        int index1 = index0 + 1;
        index0 = Mathf.Clamp(index0, 0, frames.Count - 1);
        index1 = Mathf.Clamp(index1, 0, frames.Count - 1);
        var frame0 = frames[index0];
        var frame1 = frames[index1];
        bool isOutOfRange = index0 == index1;
        float a = t * 60f - index0;

        for (int i = 0; i < phonemeCount; ++i)
        {
            frame.volume = isOutOfRange ? 0f : Mathf.Lerp(frame0.volume, frame1.volume, a);
            frame.phonemes.Add(new BakedPhonemeRatio()
            {
                phoneme = frame0.phonemes[i].phoneme,
                ratio = Mathf.Lerp(frame0.phonemes[i].ratio, frame1.phonemes[i].ratio, a)
            });
        }

        return frame;
    }

    public LipSyncInfo GetLipSyncInfo(float t)
    {
        var frame = GetFrame(t);
        return GetLipSyncInfo(frame);
    }

    public static LipSyncInfo GetLipSyncInfo(BakedFrame frame)
    {
        var info = new LipSyncInfo
        {
            phonemeRatios = new Dictionary<string, float>()
        };

        float maxRatio = 0f;
        foreach (var pr in frame.phonemes)
        {
            if (pr.ratio > maxRatio)
            {
                info.phoneme = pr.phoneme;
                maxRatio = pr.ratio;
            }

            if (info.phonemeRatios.ContainsKey(pr.phoneme))
            {
                info.phonemeRatios[pr.phoneme] += pr.ratio;
            }
            else
            {
                info.phonemeRatios.Add(pr.phoneme, pr.ratio);
            }
        }

        float minVol = Common.DefaultMinVolume;
        float maxVol = Common.DefaultMaxVolume;
        float normVol = Mathf.Log10(frame.volume);
        normVol = (normVol - minVol) / (maxVol - minVol);
        normVol = Mathf.Clamp(normVol, 0f, 1f);
        info.volume = normVol;
        info.rawVolume = frame.volume;

        return info;
    }

    public static Color[] phonemeColors = new Color[]
    {
        Color.red,
        Color.cyan,
        Color.yellow,
        Color.magenta,
        Color.green,
        Color.blue,
        Color.gray,
    };
}

}