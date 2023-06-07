using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;
using UnityEngine;
using UnityEngine.UIElements;

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
    [ReadOnly] public int melFilterBankChannels;
    [ReadOnly] public CompareMethod compareMethod;
    [ReadOnly] public NativeArray<float> means;
    [ReadOnly] public NativeArray<float> standardDeviations;
    [ReadOnly] public NativeArray<float> phonemes;
	[ReadOnly] public bool useDelta;
	[ReadOnly] public NativeArray<int> bufferMelCepOffset;
    public NativeArray<float> mfcc;
    public NativeArray<float> scores;
    [WriteOnly] public NativeArray<Info> info;
	public NativeArray<float> bufferMelCep;

#if ULIPSYNC_DEBUG
    [WriteOnly] public NativeArray<float> debugData;
    [WriteOnly] public NativeArray<float> debugSpectrum;
    [WriteOnly] public NativeArray<float> debugMelSpectrum;
    [WriteOnly] public NativeArray<float> debugMelCepstrum;
#endif

    int cutoff => targetSampleRate / 2;
    int range => 500;
	int bufferSize => 3;
	int calcLength => (int)mfcc.Length/2;  //Todo: calculate the total length mfcc divide by the number of delta's

    public void Execute()
    {
        float volume = Algorithm.GetRMSVolume(input);
        Algorithm.CopyRingBuffer(input, out var buffer, startIndex);
        Algorithm.LowPassFilter(ref buffer, outputSampleRate, cutoff, range);
        Algorithm.DownSample(buffer, out var data, outputSampleRate, targetSampleRate);
        Algorithm.PreEmphasis(ref data, 0.97f);
        Algorithm.HammingWindow(ref data);
        Algorithm.Normalize(ref data, 1f);
        Algorithm.FFT(data, out var spectrum);
        Algorithm.MelFilterBank(spectrum, out var melSpectrum, targetSampleRate, melFilterBankChannels);
        Algorithm.PowerToDb(ref melSpectrum);
        Algorithm.DCT(melSpectrum, out var melCepstrum);

		if (useDelta)
		{
			// Fill the first slot of buffer with current melCepstrum
			bufferMelCep.Slice(bufferMelCepOffset[0], calcLength).CopyFrom(melCepstrum.Slice(0, calcLength));

			// Calculate delta
			Algorithm.CalculateDelta(bufferMelCep, out var deltaMelCepstrum);

			// Move the buffer values up one slot. Slice doesn't work with NativeArray, so we have to copy the values manually.
			NativeArray<float> tempBuffer = new NativeArray<float>(calcLength, Allocator.Temp);
			for (int j = bufferSize - 1; j > 0; j--)
			{
				int srcOffset = bufferMelCepOffset[j - 1];
				int dstOffset = bufferMelCepOffset[j];
				bufferMelCep.Slice(srcOffset, calcLength).CopyTo(tempBuffer);
				for (int k = 0; k < calcLength; k++)
				{
					bufferMelCep[dstOffset + k] = tempBuffer[k];
				}
			}

			// Copy the cepstrum and delta to mfcc
			for (int i = 0; i <= calcLength-1; ++i)
			{
				// don't use the first value of melCepstrum, because it's the power of the signal?
				mfcc[i] = melCepstrum[i+1];
				mfcc[i + 12] = deltaMelCepstrum[i];
			}
			deltaMelCepstrum.Dispose();
			tempBuffer.Dispose();
		}
		else
		{
			for (int i = 1; i <= mfcc.Length; ++i)
			{
				mfcc[i - 1] = melCepstrum[i];
			}
		}

        CalcScores();

        info[0] = new Info()
        {
            volume = volume,
            mainPhonemeIndex = GetVowel(),
        };

#if ULIPSYNC_DEBUG
        data.CopyTo(debugData);
        spectrum.CopyTo(debugSpectrum);
        melSpectrum.CopyTo(debugMelSpectrum);
        melCepstrum.CopyTo(debugMelCepstrum);
#endif

        buffer.Dispose();
        data.Dispose();
        spectrum.Dispose();
        melSpectrum.Dispose();
        melCepstrum.Dispose();
    }

	/// <summary>
	/// Calculates the scores of each phoneme. The scores can be calculated by the following methods.
	/// - L1 Norm (Manhattan Distance)
	/// - L2 Norm (Euclidean Distance)
	/// - Cosine Similarity (Cosine Distance)
	/// </summary>
    void CalcScores()
    {
        float sum = 0f;

        for (int i = 0; i < scores.Length; ++i)
        {
            float score = CalcScore(i);
            scores[i] = score;
            sum += score;
        }

        for (int i = 0; i < scores.Length; ++i)
        {
            scores[i] = sum > 0 ? scores[i] / sum : 0f;
        }
    }

    float CalcScore(int index)
    {
        switch (compareMethod)
        {
            case CompareMethod.L1Norm:
                return CalcL1NormScore(index);
            case CompareMethod.L2Norm:
                return CalcL2NormScore(index);
            case CompareMethod.CosineSimilarity:
                return CalcCosineSimilarityScore(index);
        }
        return 0f;
    }

    float CalcL1NormScore(int index)
    {
        int n = mfcc.Length;
        var phoneme = new NativeSlice<float>(phonemes, index * n, n);

        var distance = 0f;
        for (int i = 0; i < n; ++i)
        {
            float x = (mfcc[i] - means[i]) / standardDeviations[i];
            float y = (phoneme[i] - means[i]) / standardDeviations[i];
            distance += math.abs(x - y);
        }
        distance /= n;

        return math.pow(10f, -distance);
    }

    float CalcL2NormScore(int index)
    {
        int n = mfcc.Length;
        var phoneme = new NativeSlice<float>(phonemes, index * n, n);

        var distance = 0f;
        for (int i = 0; i < n; ++i)
        {
            float x = (mfcc[i] - means[i]) / standardDeviations[i];
            float y = (phoneme[i] - means[i]) / standardDeviations[i];
            distance += math.pow(x - y, 2f);
        }
        distance = math.sqrt(distance / n);

        return math.pow(10f, -distance);
    }

    float CalcCosineSimilarityScore(int index)
    {
        int n = mfcc.Length;
        var phoneme = new NativeSlice<float>(phonemes, index * n, n);
        float mfccNorm = 0f;
        float phonemeNorm = 0f;

        float prod = 0f;
        for (int i = 0; i < n; ++i)
        {
            float x = (mfcc[i] - means[i]) / standardDeviations[i];
            float y = (phoneme[i] - means[i]) / standardDeviations[i];
            mfccNorm += x * x;
            phonemeNorm += y * y;
            prod += x * y;
        }
        mfccNorm = math.sqrt(mfccNorm);
        phonemeNorm = math.sqrt(phonemeNorm);
        float similarity = prod / (mfccNorm * phonemeNorm);
        similarity = math.max(similarity, 0f);

        return math.pow(similarity, 100f);
    }

	/// <summary>
	/// Gets the index of the phoneme with the highest score.
	/// </summary>
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
