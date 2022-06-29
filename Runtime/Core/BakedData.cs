using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
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
[CreateAssetMenu(menuName = Common.assetName + "/Baked Data")]
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
        var frame = new BakedFrame();
        frame.phonemes = new List<BakedPhonemeRatio>();

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
        var info = new LipSyncInfo();
        info.phonemeRatios = new Dictionary<string, float>();

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

        float minVol = Common.defaultMinVolume;
        float maxVol = Common.defaultMaxVolume;
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

#if UNITY_2020_1_OR_NEWER

    [BurstCompile]
    struct CreateTextureJob : IJob
    {
        [WriteOnly] public NativeArray<Color32> texColors;
        [DeallocateOnJobCompletion][ReadOnly] public NativeArray<Color> phonemeColors;
        [DeallocateOnJobCompletion][ReadOnly] public NativeArray<float> phonemeRatios;
        [DeallocateOnJobCompletion][ReadOnly] public NativeArray<float> volumes;
        [ReadOnly] public int width;
        [ReadOnly] public int height;
        [ReadOnly] public int phonemeCount;
        [ReadOnly] public float smooth;

        public void Execute()
        {
            var currentColor = new Color();

            for (int x = 0; x < width; ++x)
            {
                var targetColor = new Color();
                for (int i = 0; i < phonemeCount; ++i)
                {
                    var colorIndex = i % phonemeColors.Length;
                    var ratioIndex = x * phonemeCount + i;
                    targetColor += phonemeColors[colorIndex] * phonemeRatios[ratioIndex];
                }

                currentColor += (targetColor - currentColor) * smooth;

                for (int y = 0; y < height; ++y)
                {
                    var index = width * y + x;
                    var color = currentColor;
                    var dy = ((float)y - height / 2f) / (height / 2f);
                    dy = math.abs(dy);
                    dy = math.pow(dy, 2f);
                    color.a = dy > volumes[x] ? 0f : 1f;
                    texColors[index] = color;
                }
            }
        }
    }

    public Texture2D CreateTexture(int width, int height)
    {
        if (!isValid) return Texture2D.whiteTexture;

        var tex = new Texture2D(width, height);
        var texColors = tex.GetPixelData<Color32>(0);
        var phonemeColorsTmp = new NativeArray<Color>(phonemeColors, Allocator.TempJob);
        int phonemeCount = frames[0].phonemes.Count;
        var phonemeRatiosTmp = new NativeArray<float>(width * phonemeCount, Allocator.TempJob);
        var volumesTmp = new NativeArray<float>(width, Allocator.TempJob);

        for (int x = 0; x < width; ++x)
        {
            var t = (float)x / width * duration;
            var frame = GetFrame(t);
            for (int i = 0; i < phonemeCount; ++i)
            {
                phonemeRatiosTmp[x * phonemeCount + i] = frame.phonemes[i].ratio;
            }
            volumesTmp[x] = frame.volume;
        }

        var job = new CreateTextureJob()
        {
            texColors = texColors,
            phonemeColors = phonemeColorsTmp,
            phonemeRatios = phonemeRatiosTmp,
            volumes = volumesTmp,
            width = width,
            height = height,
            phonemeCount = phonemeCount,
            smooth = 0.15f,
        };
        job.Schedule().Complete();

        tex.Apply();
        return tex;
    }

#else

    public Texture2D CreateTexture(int width, int height)
    {
        if (!isValid) return Texture2D.whiteTexture;

        var colors = new Color[width * height];
        var currentColor = new Color();
        var smooth = 0.15f;

        for (int x = 0; x < width; ++x)
        {
            var t = (float)x / width * duration;
            var frame = GetFrame(t);
            var targetColor = new Color();

            for (int i = 0; i < frame.phonemes.Count; ++i)
            {
                var colorIndex = i % phonemeColors.Length;
                targetColor += phonemeColors[colorIndex] * frame.phonemes[i].ratio;
            }

            currentColor += (targetColor - currentColor) * smooth;

            for (int y = 0; y < height; ++y)
            {
                var index = width * y + x;
                var color = currentColor;
                var dy = ((float)y - height / 2f) / (height / 2f);
                dy = Mathf.Abs(dy);
                dy = Mathf.Pow(dy, 2f);
                color.a = dy > frame.volume ? 0f : 1f;
                colors[index] = color;
            }
        }

        var tex = new Texture2D(width, height);
        tex.SetPixels(colors);
        tex.Apply();

        return tex;
    }

#endif

}

}