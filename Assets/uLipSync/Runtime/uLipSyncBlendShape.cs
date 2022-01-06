using UnityEngine;
using System.Collections.Generic;

namespace uLipSync
{

public class uLipSyncBlendShape : MonoBehaviour
{
    [System.Serializable]
    public class BlendShapeInfo
    {
        public string phoneme;
        public int index = -1;
        public float maxWeight = 1f;

        public float weight { get; set; } = 0f;
        public float weightVelocity { get; set; } = 0f;
        public float normalizedWeight { get; set; } = 0f;
    }

    public SkinnedMeshRenderer skinnedMeshRenderer;
    public List<BlendShapeInfo> blendShapes = new List<BlendShapeInfo>();
    public float minVolume = -2.5f;
    public float maxVolume = -1.5f;
    [Range(0f, 0.3f)] public float smoothness = 0.05f;

    LipSyncInfo _info = new LipSyncInfo();
    bool _lipSyncUpdated = false;
    float _volume = 0f;
    float _openCloseVelocity = 0f;

    public void OnLipSyncUpdate(LipSyncInfo info)
    {
        _info = info;
        _lipSyncUpdated = true;
    }

    void Update()
    {
        UpdateVolume();
        UpdateVowels();
        _lipSyncUpdated = false;
    }

    void LateUpdate()
    {
        LateUpdateBlendShapes();
    }

    void UpdateVolume()
    {
        float normVol = 0f;
        if (_lipSyncUpdated && _info.rawVolume > 0f)
        {
            normVol = Mathf.Log10(_info.rawVolume);
            normVol = (normVol - minVolume) / Mathf.Max(maxVolume - minVolume, 1e-4f);
            normVol = Mathf.Clamp(normVol, 0f, 1f);
        }
        _volume = Mathf.SmoothDamp(_volume, normVol, ref _openCloseVelocity, 0.05f);
    }

    void UpdateVowels()
    {
        float sum = 0f;
        var ratios = _info.phonemeRatios;

        foreach (var bs in blendShapes)
        {
            float targetWeight = 0f;
            if (ratios != null) ratios.TryGetValue(bs.phoneme, out targetWeight);
            float weightVel = bs.weightVelocity;
            bs.weight = Mathf.SmoothDamp(bs.weight, targetWeight, ref weightVel, smoothness);
            bs.weightVelocity = weightVel;
            sum += bs.weight;
        }

        foreach (var bs in blendShapes)
        {
            bs.normalizedWeight = sum > 0f ? bs.weight / sum : 0f;
        }
    }

    protected virtual void LateUpdateBlendShapes()
    {
        if (!skinnedMeshRenderer) return;

        foreach (var bs in blendShapes)
        {
            if (bs.index < 0) continue;
            skinnedMeshRenderer.SetBlendShapeWeight(bs.index, 0f);
        }

        foreach (var bs in blendShapes)
        {
            if (bs.index < 0) continue;
            float weight = skinnedMeshRenderer.GetBlendShapeWeight(bs.index);
            weight += bs.normalizedWeight * bs.maxWeight * _volume * 100;
            skinnedMeshRenderer.SetBlendShapeWeight(bs.index, weight);
        }
    }
}

}

