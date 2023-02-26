using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;

namespace uLipSync
{

[BurstCompile]
public struct LipSyncJob : IJob
{
    public struct Info
    {
        public float volume;
        public int mainVowelIndex;
    }

    [ReadOnly] public NativeArray<float> input;
    [ReadOnly] public int startIndex;
    [ReadOnly] public int outputSampleRate;
    [ReadOnly] public int targetSampleRate;
    [ReadOnly] public int melFilterBankChannels;
    public NativeArray<float> mfcc;
    public NativeArray<float> phonemes;
    public NativeArray<float> distances;
    public NativeArray<Info> info;
    
#if ULIPSYNC_DEBUG
    public NativeArray<float> debugData;
    public NativeArray<float> debugSpectrum;
    public NativeArray<float> debugMelSpectrum;
    public NativeArray<float> debugMelCepstrum;
#endif

    public void Execute()
    {
        float volume = Algorithm.GetRMSVolume(input);

        Algorithm.CopyRingBuffer(input, out var buffer, startIndex);

        int cutoff = targetSampleRate / 2 - 200;
        int range = 200;
        Algorithm.LowPassFilter(ref buffer, outputSampleRate, cutoff, range);

        Algorithm.DownSample(buffer, out var data, outputSampleRate, targetSampleRate);

        Algorithm.PreEmphasis(ref data, 0.97f);

        Algorithm.HammingWindow(ref data);

        Algorithm.Normalize(ref data, 100f);

        Algorithm.ZeroPadding(ref data, out var dataWithZeroPadding);

        Algorithm.FFT(dataWithZeroPadding, out var spectrum);

        Algorithm.MelFilterBank(spectrum, out var melSpectrum, targetSampleRate, melFilterBankChannels);

        for (int i = 0; i < melSpectrum.Length; ++i)
        {
            melSpectrum[i] = math.log10(melSpectrum[i]);
        }

        Algorithm.DCT(melSpectrum, out var melCepstrum);

        for (int i = 1; i <= mfcc.Length; ++i)
        {
            mfcc[i - 1] = melCepstrum[i];
        }

        for (int i = 0; i < distances.Length; ++i)
        {
            distances[i] = CalcTotalDistance(i);
        }

        info[0] = new Info()
        {
            volume = volume,
            mainVowelIndex = GetVowel(),
        };
        
#if ULIPSYNC_DEBUG
        dataWithZeroPadding.CopyTo(debugData);
        spectrum.CopyTo(debugSpectrum);
        melSpectrum.CopyTo(debugMelSpectrum);
        melCepstrum.CopyTo(debugMelCepstrum);
#endif

        buffer.Dispose();
        data.Dispose();
        dataWithZeroPadding.Dispose();
        spectrum.Dispose();
        melSpectrum.Dispose();
        melCepstrum.Dispose();
    }

    int GetVowel()
    {
        int index = -1;
        float minDistance = float.MaxValue;
        int n = phonemes.Length / mfcc.Length;
        for (int i = 0; i < n; ++i)
        {
            var distance = CalcTotalDistance(i);
            if (distance < minDistance)
            {
                index = i;
                minDistance = distance;
            }
        }
        return index;
    }

    float CalcTotalDistance(int index)
    {
        var distance = 0f;
        int offset = index * mfcc.Length;
        for (int i = 0; i < mfcc.Length; ++i)
        {
            distance += math.abs(mfcc[i] - phonemes[i + offset]);
        }
        return distance;
    }
}

}
