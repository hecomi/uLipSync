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
        public float vowelChangeVelocity { get; set; } = 0f;
        public float weight { get; set; } = 0f;
        public float normalizedWeight { get; set; } = 0f;
    }

    public SkinnedMeshRenderer skinnedMeshRenderer;
    public List<BlendShapeInfo> blendShapes = new List<BlendShapeInfo>();
    public bool applyVolume = false;
    [Range(0f, 0.2f)] public float openDuration = 0.05f;
    [Range(0f, 0.2f)] public float closeDuration = 0.1f;
    [Range(0f, 0.2f)] public float vowelChangeDuration = 0.04f;

    float _openVelocity = 0f;
    float _closeVelocity = 0f;
    List<float> _vowelChangeVelocity = new List<float>();

    string _phoneme = "";
    float _phonemeTimer = 0f;
    float _volume = 0f;
    bool _lipSyncUpdated = false;

    public void OnLipSyncUpdate(LipSyncInfo lipSync)
    {
        _phoneme = lipSync.phoneme;

        if (lipSync.volume > Mathf.Epsilon)
        {
            var targetVolume = applyVolume ? lipSync.volume : 1f;
            _volume = Mathf.SmoothDamp(_volume, targetVolume, ref _openVelocity, openDuration);
        }
        else
        {
            var targetVolume = applyVolume ? lipSync.volume : 0f;
            _volume = Mathf.SmoothDamp(_volume, targetVolume, ref _closeVelocity, closeDuration);
        }

        _lipSyncUpdated = true;
    }

    void Update()
    {
        float sum = 0f;

        foreach (var bs in blendShapes)
        {
            float targetWeight = (bs.phoneme == _phoneme) ? 1f : 0f;
            float vowelChangeVelocity = bs.vowelChangeVelocity;
            bs.weight = Mathf.SmoothDamp(bs.weight, targetWeight, ref vowelChangeVelocity, vowelChangeDuration);
            bs.vowelChangeVelocity = vowelChangeVelocity;
            sum += bs.weight;
        }

        foreach (var bs in blendShapes)
        {
            bs.normalizedWeight = sum > 0f ? bs.weight / sum : 0f;
        }
    }

    void LateUpdate()
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

        if (_lipSyncUpdated)
        {
            _lipSyncUpdated = false;
        }
        else
        {
            _volume = Mathf.SmoothDamp(_volume, 0f, ref _closeVelocity, closeDuration);
        }
    }
}

}

