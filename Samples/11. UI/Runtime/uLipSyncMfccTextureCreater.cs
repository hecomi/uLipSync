using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

public static class uLipSyncMfccTextureCreater
{
    [BurstCompile]
    struct CreateTextureJob : IJob
    {
        [WriteOnly] public NativeArray<Color32> texColors;
        [ReadOnly][DeallocateOnJobCompletion] public NativeArray<float> array;
        [ReadOnly] public int width;
        [ReadOnly] public int height;
        [ReadOnly] public float min;
        [ReadOnly] public float max;

        public Color ToRGB(float hue)
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

    public static Texture2D CreateTexture(uLipSync.Profile profile, int index)
    {
        var tex = Texture2D.whiteTexture;

        if (!profile || profile.mfccs.Count == 0) return tex;

        float min, max;
        profile.CalcMinMax(out min, out max);

        var mfcc = profile.mfccs[index];
        var list = mfcc.mfccCalibrationDataList;
        if (list.Count == 0) return tex;

        var width = list[0].array.Length;
        var height = list.Count;

        tex = new Texture2D(width, height);
        tex.filterMode = FilterMode.Point;
        var texColors = tex.GetPixelData<Color32>(0);
        var array = new NativeArray<float>(width * height, Allocator.TempJob);

        for (int i = 0; i < height; ++i)
        {
            int offset = width * i;
            var slice = new NativeSlice<float>(array, offset, width);
            slice.CopyFrom(list[i].array);
        }

        var job = new CreateTextureJob()
        {
            texColors = texColors,
            array = array,
            width = width,
            height = height,
            min = min,
            max = max,
        };
        job.Schedule().Complete();

        tex.Apply();
        return tex;
    }
}
