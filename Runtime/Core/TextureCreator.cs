using System;
using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace uLipSync
{

public static class TextureCreator
{
    static NativeArray<Color32> GetOrCreatePixelData(Texture2D tex)
    {
#if UNITY_2020_1_OR_NEWER
        return tex.GetPixelData<Color32>(0);
#else
        return new NativeArray<Color32>(tex.width * tex.height, Allocator.TempJob);
#endif
    }

    static void ApplyPixelData(Texture2D tex, NativeArray<Color32> array)
    {
#if !UNITY_2020_1_OR_NEWER
        var colors = new Color32[tex.width * tex.height];
        array.CopyTo(colors);
        array.Dispose();
        tex.SetPixels32(colors);
#endif
        tex.Apply();
    }

    [BurstCompile]
    struct CreateBakedDataWaveTextureJob : IJob
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

                currentColor += (targetColor - currentColor) * (1f - smooth);

                for (int y = 0; y < height; ++y)
                {
                    var index = width * y + x;
                    var color = currentColor;
                    var dy = (y - height / 2f) / (height / 2f);
                    dy = math.abs(dy);
                    dy = math.pow(dy, 2f);
                    color.a = dy > volumes[x] ? 0f : 1f;
                    texColors[index] = color;
                }
            }
        }
    }

    public static Texture2D CreateBakedDataWaveTexture(BakedData data, int width, int height, float smooth = 0.85f)
    {
        var tex = new Texture2D(width, height);

        if (!data.isValid) return tex;
        
        var texColors = GetOrCreatePixelData(tex);
        var phonemeColorsTmp = new NativeArray<Color>(BakedData.phonemeColors, Allocator.TempJob);
        int phonemeCount = data.frames[0].phonemes.Count;
        var phonemeRatiosTmp = new NativeArray<float>(width * phonemeCount, Allocator.TempJob);
        var volumesTmp = new NativeArray<float>(width, Allocator.TempJob);
        var duration = data.duration;

        for (int x = 0; x < width; ++x)
        {
            var t = (float)x / width * duration;
            var frame = data.GetFrame(t);
            for (int i = 0; i < phonemeCount; ++i)
            {
                phonemeRatiosTmp[x * phonemeCount + i] = frame.phonemes[i].ratio;
            }
            volumesTmp[x] = frame.volume;
        }

        var job = new CreateBakedDataWaveTextureJob()
        {
            texColors = texColors,
            phonemeColors = phonemeColorsTmp,
            phonemeRatios = phonemeRatiosTmp,
            volumes = volumesTmp,
            width = width,
            height = height,
            phonemeCount = phonemeCount,
            smooth = Mathf.Clamp(smooth, 0f, 1f),
        };
        job.Schedule().Complete();

        ApplyPixelData(tex, texColors);
        return tex;
    }

    [BurstCompile]
    struct CreateMfccTextureJob : IJob
    {
        [WriteOnly] public NativeArray<Color32> texColors;
        [ReadOnly][DeallocateOnJobCompletion] public NativeArray<float> array;
        [ReadOnly] public int width;
        [ReadOnly] public int height;
        [ReadOnly] public float min;
        [ReadOnly] public float max;

        Color ToRGB(float hue)
        {
            hue = (1f - math.cos(math.PI * hue)) * 0.5f;
            hue = 1f - hue;
            hue *= 5f;
            var x = 1 - math.abs(hue % 2f - 1f);
            return
                hue < 1f ? new Color(1f, x, 0f) :
                hue < 2f ? new Color(x, 1f, 0f) :
                hue < 3f ? new Color(0f, 1f, x) :
                hue < 4f ? new Color(0f, x, 1f) :
                new Color(x * 0.5f, 0f, 0.5f);
        }

        public void Execute()
        {
            var maxMinusMin = max - min;
            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    var index = width * y + x;
                    var value = (array[index] - min) / maxMinusMin;
                    texColors[index] = ToRGB(value);
                }
            }
        }
    }
    
    public static Texture2D CreateMfccTexture(Texture2D tex, Profile profile, int index)
    {
        if (!profile) return tex;

        if (index < 0 || index >= profile.mfccs.Count) return tex;
        
        var mfcc = profile.mfccs[index];
        return CreateMfccTexture(tex, mfcc, Common.MfccMinValue, Common.MfccMaxValue);
    }

    public static Texture2D CreateMfccTexture(Texture2D tex, MfccData mfcc, float min, float max)
    {
        var list = mfcc.mfccCalibrationDataList;
        if (list.Count == 0) return tex;

        var width = list[0].array.Length;
        var height = list.Count;

        if (!tex || tex.width != width || tex.height != height)
        {
            tex = new Texture2D(width, height)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
        }

        var texColors = GetOrCreatePixelData(tex);
        var array = new NativeArray<float>(width * height, Allocator.TempJob);

        for (int i = 0; i < height; ++i)
        {
            int offset = width * i;
            var slice = new NativeSlice<float>(array, offset, width);
            slice.CopyFrom(list[i].array);
        }

        var job = new CreateMfccTextureJob()
        {
            texColors = texColors,
            array = array,
            width = width,
            height = height,
            min = min,
            max = max,
        };
        job.Schedule().Complete();

        ApplyPixelData(tex, texColors);

        return tex;
    }
}

}