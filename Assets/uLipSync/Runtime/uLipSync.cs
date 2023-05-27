using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using System.Collections.Generic;

namespace uLipSync
{

public class uLipSync : MonoBehaviour
{
    public Profile profile;
    public LipSyncUpdateEvent onLipSyncUpdate = new LipSyncUpdateEvent();
    [Range(0f, 1f)] public float outputSoundGain = 1f;

    public uLipSyncAudioSource audioSourceProxy;
    uLipSyncAudioSource _currentAudioSourceProxy;

    JobHandle _jobHandle;
    object _lockObject = new object();
    bool _allocated = false;
    int _index = 0;
    bool _isDataReceived = false;

    NativeArray<float> _rawInputData;
    NativeArray<float> _inputData;
    NativeArray<float> _mfcc;
    NativeArray<float> _mfccForOther;
    NativeArray<float> _means;
    NativeArray<float> _standardDeviations;
    NativeArray<float> _phonemes;
    NativeArray<float> _scores;
    NativeArray<LipSyncJob.Info> _info;
    List<int> _requestedCalibrationVowels = new List<int>();
    Dictionary<string, float> _ratios = new Dictionary<string, float>();

    public NativeArray<float> mfcc => _mfccForOther;
    public LipSyncInfo result { get; private set; } = new LipSyncInfo();
    
#if ULIPSYNC_DEBUG
    NativeArray<float> _debugData;
    NativeArray<float> _debugSpectrum;
    NativeArray<float> _debugMelSpectrum;
    NativeArray<float> _debugMelCepstrum;
    NativeArray<float> _debugDataForOther;
    NativeArray<float> _debugSpectrumForOther;
    NativeArray<float> _debugMelSpectrumForOther;
    NativeArray<float> _debugMelCepstrumForOther;
    public NativeArray<float> data => _debugDataForOther;
    public NativeArray<float> spectrum => _debugSpectrumForOther;
    public NativeArray<float> melSpectrum => _debugMelSpectrumForOther;
    public NativeArray<float> melCepstrum => _debugMelCepstrumForOther;
#endif

    int inputSampleCount
    {
        get 
        {  
            if (!profile) return AudioSettings.outputSampleRate;
            float r = (float)AudioSettings.outputSampleRate / profile.targetSampleRate;
            return Mathf.CeilToInt(profile.sampleCount * r);
        }
    }
    
    int mfccNum => profile ? profile.mfccNum : 12;

    void Awake()
    {
        UpdateAudioSource();
    }

    void OnEnable()
    {
        AllocateBuffers();
    }

    void OnDisable()
    {
        _jobHandle.Complete();
        DisposeBuffers();
    }

    void Update()
    {
        if (!profile) return;
        if (!_jobHandle.IsCompleted) return;

        UpdateResult();
        InvokeCallback();
        UpdateCalibration();
        UpdatePhonemes();
        ScheduleJob();

        UpdateBuffers();
        UpdateAudioSource();
    }

    void AllocateBuffers()
    {
        if (_allocated)
        {
            DisposeBuffers();
        }
        _allocated = true;

        _jobHandle.Complete();

        lock (_lockObject)
        {
            int n = inputSampleCount;
            int phonemeCount = profile ? profile.mfccs.Count : 1;
            _rawInputData = new NativeArray<float>(n, Allocator.Persistent);
            _inputData = new NativeArray<float>(n, Allocator.Persistent); 
            _mfcc = new NativeArray<float>(mfccNum, Allocator.Persistent); 
            _mfccForOther = new NativeArray<float>(mfccNum, Allocator.Persistent); 
            _means = new NativeArray<float>(mfccNum, Allocator.Persistent); 
            _standardDeviations = new NativeArray<float>(mfccNum, Allocator.Persistent); 
            _scores = new NativeArray<float>(phonemeCount, Allocator.Persistent);
            _phonemes = new NativeArray<float>(mfccNum * phonemeCount, Allocator.Persistent);
            _info = new NativeArray<LipSyncJob.Info>(1, Allocator.Persistent);
#if ULIPSYNC_DEBUG
            _debugData = new NativeArray<float>(profile.sampleCount, Allocator.Persistent);
            _debugDataForOther = new NativeArray<float>(profile.sampleCount, Allocator.Persistent);
            _debugSpectrum = new NativeArray<float>(profile.sampleCount, Allocator.Persistent);
            _debugSpectrumForOther = new NativeArray<float>(profile.sampleCount, Allocator.Persistent);
            _debugMelSpectrum = new NativeArray<float>(profile.melFilterBankChannels, Allocator.Persistent);
            _debugMelSpectrumForOther = new NativeArray<float>(profile.melFilterBankChannels, Allocator.Persistent);
            _debugMelCepstrum = new NativeArray<float>(profile.melFilterBankChannels, Allocator.Persistent);
            _debugMelCepstrumForOther = new NativeArray<float>(profile.melFilterBankChannels, Allocator.Persistent);
#endif
        }
    }

    void DisposeBuffers()
    {
        if (!_allocated) return;
        _allocated = false;

        _jobHandle.Complete();

        lock (_lockObject)
        {
            _rawInputData.Dispose();
            _inputData.Dispose();
            _mfcc.Dispose();
            _mfccForOther.Dispose();
            _means.Dispose();
            _standardDeviations.Dispose();
            _scores.Dispose();
            _phonemes.Dispose();
            _info.Dispose();
#if ULIPSYNC_DEBUG
            _debugData.Dispose();
            _debugDataForOther.Dispose();
            _debugSpectrum.Dispose();
            _debugSpectrumForOther.Dispose();
            _debugMelSpectrum.Dispose();
            _debugMelSpectrumForOther.Dispose();
            _debugMelCepstrum.Dispose();
            _debugMelCepstrumForOther.Dispose();
#endif
        }
    }

    void UpdateBuffers()
    {
        if (inputSampleCount != _rawInputData.Length ||
            profile.mfccs.Count * mfccNum != _phonemes.Length
#if ULIPSYNC_DEBUG
            || profile.melFilterBankChannels != _debugMelSpectrum.Length
#endif
        )
        {
            lock (_lockObject)
            {
                DisposeBuffers();
                AllocateBuffers();
            }
        }
    }

    void UpdateResult()
    {
        _jobHandle.Complete();
        _mfccForOther.CopyFrom(_mfcc);
#if ULIPSYNC_DEBUG
        _debugDataForOther.CopyFrom(_debugData);
        _debugSpectrumForOther.CopyFrom(_debugSpectrum);
        _debugMelSpectrumForOther.CopyFrom(_debugMelSpectrum);
        _debugMelCepstrumForOther.CopyFrom(_debugMelCepstrum);
#endif
        
        int index = _info[0].mainPhonemeIndex;
        string mainPhoneme = profile.GetPhoneme(index);

        float sumScore = 0f;
        for (int i = 0; i < _scores.Length; ++i)
        {
            sumScore += _scores[i];
        }

        _ratios.Clear();
        for (int i = 0; i < _scores.Length; ++i)
        {
            var phoneme = profile.GetPhoneme(i);
            var ratio = sumScore > 0f ? _scores[i] / sumScore : 0f;
            if (!_ratios.ContainsKey(phoneme))
            {
                _ratios.Add(phoneme, 0f);
            }
            _ratios[phoneme] += ratio;
        }

        float rawVol = _info[0].volume;
        float minVol = Common.DefaultMinVolume;
        float maxVol = Common.DefaultMaxVolume;
        float normVol = Mathf.Log10(rawVol);
        normVol = (normVol - minVol) / (maxVol - minVol);
        normVol = Mathf.Clamp(normVol, 0f, 1f);

        result = new LipSyncInfo()
        {
            phoneme = mainPhoneme,
            volume = normVol,
            rawVolume = rawVol,
            phonemeRatios = _ratios,
        };
    }

    void InvokeCallback()
    {
        if (onLipSyncUpdate == null) return;

        onLipSyncUpdate.Invoke(result);
    }

    void UpdatePhonemes()
    {
        int index = 0;
        foreach (var data in profile.mfccs)
        {
            foreach (var value in data.mfccNativeArray)
            {
                if (index >= _phonemes.Length) break;
                _phonemes[index++] = value;
            }
        }
    }

    void ScheduleJob()
    {
        if (!_isDataReceived) return;
        _isDataReceived = false;

        int index = 0;
        lock (_lockObject)
        {
            _inputData.CopyFrom(_rawInputData);
            _means.CopyFrom(profile.means);
            _standardDeviations.CopyFrom(profile.standardDeviation);
            index = _index;
        }

        var lipSyncJob = new LipSyncJob()
        {
            input = _inputData,
            startIndex = index,
            outputSampleRate = AudioSettings.outputSampleRate,
            targetSampleRate = profile.targetSampleRate,
            melFilterBankChannels = profile.melFilterBankChannels,
            means = _means,
            standardDeviations = _standardDeviations,
            mfcc = _mfcc,
            phonemes = _phonemes,
            compareMethod = profile.compareMethod,
            scores = _scores,
            info = _info,
#if ULIPSYNC_DEBUG
            debugData = _debugData,
            debugSpectrum = _debugSpectrum,
            debugMelSpectrum = _debugMelSpectrum,
            debugMelCepstrum = _debugMelCepstrum,
#endif
        };

        _jobHandle = lipSyncJob.Schedule();
    }

    public void RequestCalibration(int index)
    {
        _requestedCalibrationVowels.Add(index);
    }

    void UpdateCalibration()
    {
        if (!profile) return;

        foreach (var index in _requestedCalibrationVowels)
        {
            profile.UpdateMfcc(index, mfcc, true);
        }

        _requestedCalibrationVowels.Clear();
    }

    void UpdateAudioSource()
    {
        if (audioSourceProxy == _currentAudioSourceProxy) return;

        if (_currentAudioSourceProxy)
        {
            _currentAudioSourceProxy.onAudioFilterRead.RemoveListener(OnDataReceived);
        }

        if (audioSourceProxy)
        {
            audioSourceProxy.onAudioFilterRead.AddListener(OnDataReceived);
        }

        _currentAudioSourceProxy = audioSourceProxy;
    }

    public void OnDataReceived(float[] input, int channels)
    {
        if (_rawInputData.Length == 0) return;

        lock (_lockObject)
        {
            int n = _rawInputData.Length;
            _index = _index % n;
            for (int i = 0; i < input.Length; i += channels) 
            {
                _rawInputData[_index++ % n] = input[i];
            }
        }

        if (math.abs(outputSoundGain - 1f) > math.EPSILON)
        {
            int n = input.Length;
            for (int i = 0; i < n; ++i) 
            {
                input[i] *= outputSoundGain;
            }
        }

        _isDataReceived = true;
    }

    void OnAudioFilterRead(float[] input, int channels)
    {
        if (!audioSourceProxy)
        {
            OnDataReceived(input, channels);
        }
    }

#if UNITY_EDITOR
    public void OnBakeStart(Profile profile)
    {
        this.profile = profile;
        AllocateBuffers();
    }

    public void OnBakeEnd()
    {
        DisposeBuffers();
    }

    public void OnBakeUpdate(float[] input, int channels)
    {
        OnDataReceived(input, channels);
        UpdateBuffers();
        UpdatePhonemes();
        ScheduleJob();
        _jobHandle.Complete();
        UpdateResult();
    }
#endif
}

}
