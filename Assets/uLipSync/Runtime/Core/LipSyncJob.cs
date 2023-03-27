using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;
using UnityEngine;

namespace uLipSync
{

[BurstCompile]
public struct LipSyncJob : IJob
{
    public struct Info
    {
        public float volume;
        public int mainPhonemeIndex;
    }

    [ReadOnly] public NativeArray<float> input;
    [ReadOnly] public int startIndex;
    [ReadOnly] public int outputSampleRate;
    [ReadOnly] public int targetSampleRate;
    [ReadOnly] public bool useZeroPadding;
    [ReadOnly] public int melFilterBankChannels;
    [ReadOnly] public CompareMethod compareMethod;
    [ReadOnly] public NativeArray<float> means;
    [ReadOnly] public NativeArray<float> standardDeviations;
    [ReadOnly] public NativeArray<float> phonemes;
    public NativeArray<float> mfcc;
    public NativeArray<float> mfccWithStandardization;
    public NativeArray<float> scores;
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

        int cutoff = targetSampleRate / 2;
        int range = 500;
        Algorithm.LowPassFilter(ref buffer, outputSampleRate, cutoff, range);

        Algorithm.DownSample(buffer, out var data, outputSampleRate, targetSampleRate);

        Algorithm.PreEmphasis(ref data, 0.97f);

        Algorithm.HammingWindow(ref data);

        Algorithm.Normalize(ref data, 1f);

        NativeArray<float> dataWithZeroPadding;
        if (useZeroPadding)
        {
            Algorithm.ZeroPadding(ref data, out dataWithZeroPadding);
        }
        else
        {
            dataWithZeroPadding = new NativeArray<float>(data.Length, Allocator.Temp); 
            data.CopyTo(dataWithZeroPadding);
        }

        Algorithm.FFT(dataWithZeroPadding, out var spectrum);

        Algorithm.MelFilterBank(spectrum, out var melSpectrum, targetSampleRate, melFilterBankChannels);

        for (int i = 0; i < melSpectrum.Length; ++i)
        {
            melSpectrum[i] = 10f * math.log10(melSpectrum[i]);
        }

        Algorithm.DCT(melSpectrum, out var melCepstrum);

        for (int i = 1; i <= mfcc.Length; ++i)
        {
            mfcc[i - 1] = melCepstrum[i];
        }
        
        for (int i = 0; i < mfcc.Length; ++i)
        {
            mfccWithStandardization[i] = (mfcc[i] - means[i]) / standardDeviations[i];
        }

        for (int i = 0; i < scores.Length; ++i)
        {
            scores[i] = CalcScore(i);
        }

        info[0] = new Info()
        {
            volume = volume,
            mainPhonemeIndex = GetVowel(),
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

    float CalcScore(int index)
    {
        switch (compareMethod)
        {
            case CompareMethod.EuclideanDistance:
                return CalcEuclideanDistanceScore(index);
            case CompareMethod.CosineSimilarity:
                return CalcCosineSimilarityScore(index);
        }
        return 0f;
    }

    float CalcEuclideanDistanceScore(int index)
    {
        int n = mfccWithStandardization.Length;
        var phoneme = new NativeSlice<float>(phonemes, index * n, n);
        
        var distance = 0f;
        for (int i = 0; i < n; ++i)
        {
            distance += math.pow(mfccWithStandardization[i] - phoneme[i], 2f);
        }
        distance = math.sqrt(distance);

        return 1f / distance;
    }

    float CalcCosineSimilarityScore(int index)
    {
        int n = mfccWithStandardization.Length;
        var phoneme = new NativeSlice<float>(phonemes, index * n, n);
        var mfccNorm = Algorithm.Norm(mfccWithStandardization);
        var phonemeNorm = Algorithm.Norm(phoneme);
        
        float prod = 0f;
        for (int i = 0; i < n; ++i)
        {
            prod += mfccWithStandardization[i] * phoneme[i];
        }
        float similarity = prod / (mfccNorm * phonemeNorm);

        return (1f + similarity) / 2f;
    }

    int GetVowel()
    {
        int index = -1;
        float maxScore = -1f;
        for (int i = 0; i < scores.Length; ++i)
        {
            var score = scores[i];
            if (score > maxScore)
            {
                index = i;
                maxScore = score;
            }
        }
        return index;
    }
}

}
