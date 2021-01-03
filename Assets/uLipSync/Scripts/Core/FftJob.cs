using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;

namespace uLipSync
{

[BurstCompile]
public struct FftJob : IJob
{
    [ReadOnly] public NativeArray<float> input;
    [ReadOnly] public int startIndex;
    [ReadOnly] public float volumeThresh;
    public NativeArray<float> spectrum;

    public void Execute()
    {
        int N = input.Length;

        float volume = Algorithm.GetRMSVolume(ref input);
        if (volume < volumeThresh)
        {
            return;
        }

        // copy input ring buffer to a temporary array
        var data = new NativeArray<float>(N, Allocator.Temp);
        Algorithm.CopyRingBuffer(ref input, ref data, startIndex);

        // multiply hanning window function
        for (int i = 0; i < N; ++i)
        {
            data[i] *= 0.5f - 0.5f * math.cos(2f * math.PI * i / (N - 1));
        }

        // Cooley-tukey FFT
        var spectrumComplex = new NativeArray<float2>(N, Allocator.Temp);
        for (int i = 0; i < N; ++i)
        {
            spectrumComplex[i] = new float2(data[i], 0f);
        }
        Fft(ref spectrumComplex, N);

        for (int i = 0; i < N; ++i)
        {
            spectrum[i] = math.length(spectrumComplex[i]);
        }

        data.Dispose();
        spectrumComplex.Dispose();
    }

    void Fft(ref NativeArray<float2> spectrum, int N)
    {
        if (N < 2) return;

        var even = new NativeArray<float2>(N / 2, Allocator.Temp);
        var odd = new NativeArray<float2>(N / 2, Allocator.Temp);
        for (int i = 0; i < N / 2; ++i)
        {
            even[i] = spectrum[i * 2];
            odd[i] = spectrum[i * 2 + 1];
        }
        Fft(ref even, N / 2);
        Fft(ref odd, N / 2);

        for (int i = 0; i < N / 2; ++i)
        {
            var e = even[i];
            var o = odd[i];
            float theta = -2f * math.PI * i / N;
            var c = new float2(math.cos(theta), math.sin(theta));
            c = new float2(c.x * o.x - c.y * o.y, c.x * o.y + c.y * o.x);
            spectrum[i] = e + c;
            spectrum[N / 2 + i] = e - c;
        }

        even.Dispose();
        odd.Dispose();
    }
}

}
