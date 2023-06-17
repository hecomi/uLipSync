using UnityEngine;
using System.Collections.Generic;

namespace uLipSync
{

public class uLipSyncAnimator : MonoBehaviour
{
    [System.Serializable]
    public class AnimatorInfo
    {
        public string phoneme;
        public string name;
        public int nameHash;
        public int index = 0;
        public float maxWeight = 1f;

        public float weight { get; set; } = 0f;
        public float weightVelocity { get; set; } = 0f;
    }

    public UpdateMethod updateMethod = UpdateMethod.LateUpdate;
    public Animator animator;

    public List<AnimatorInfo> parameters = new List<AnimatorInfo>();
    public float minVolume = -2.5f;
    public float maxVolume = -1.5f;
    [Range(0f, 0.3f)] public float smoothness = 0.05f;
	[Range(0.0001f, 0.01f)] public float minimalValueThreshold = 0.001f;

    LipSyncInfo _info = new LipSyncInfo();
    bool _lipSyncUpdated = false;
    float _volume = 0f;
    float _openCloseVelocity = 0f;
    protected float volume => _volume;

    void UpdateLipSync()
    {
        UpdateVolume();
        UpdateVowels();
        _lipSyncUpdated = false;
    }

    public void OnLipSyncUpdate(LipSyncInfo info)
    {
        _info = info;
        _lipSyncUpdated = true;
        if (updateMethod == UpdateMethod.LipSyncUpdateEvent)
        {
            UpdateLipSync();
            OnApplyAnimator();
        }
    }

	void Awake()
    {
        foreach (AnimatorInfo par in parameters)
        {
            par.nameHash = Animator.StringToHash(par.name);
        }
    }

    void Update()
    {
        if (updateMethod != UpdateMethod.LipSyncUpdateEvent)
        {
            UpdateLipSync();
        }

        if (updateMethod == UpdateMethod.Update)
        {
            OnApplyAnimator();
        }
    }

    void LateUpdate()
    {
        if (updateMethod == UpdateMethod.LateUpdate)
        {
            OnApplyAnimator();
        }
    }

    void FixedUpdate()
    {
        if (updateMethod == UpdateMethod.FixedUpdate)
        {
            OnApplyAnimator();
        }
    }

    float SmoothDamp(float value, float target, float threshold, ref float velocity)
    {
        float smoothedValue = Mathf.SmoothDamp(value, target, ref velocity, smoothness);

		if (Mathf.Abs(smoothedValue) < threshold)
		{
			smoothedValue = 0f;
		}

    	return smoothedValue;
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
        _volume = SmoothDamp(_volume, normVol, minimalValueThreshold, ref _openCloseVelocity);
    }

    void UpdateVowels()
    {
        float sum = 0f;
        var ratios = _info.phonemeRatios;

        foreach (var param in parameters)
        {
            float targetWeight = 0f;
            if (ratios != null && !string.IsNullOrEmpty(param.phoneme))
            {
                ratios.TryGetValue(param.phoneme, out targetWeight);
            }
            float weightVel = param.weightVelocity;
            param.weight = SmoothDamp(param.weight, targetWeight, minimalValueThreshold, ref weightVel);
            param.weightVelocity = weightVel;
            sum += param.weight;
        }

        foreach (var param in parameters)
        {
            param.weight = sum > 0f ? param.weight / sum : 0f;
        }
    }

    public void ApplyAnimator()
    {
        if (updateMethod == UpdateMethod.External)
        {
            OnApplyAnimator();
        }
    }

    void OnApplyAnimator()
    {
        if (!animator) return;

        foreach (var param in parameters)
        {
            if (param.index < 0) continue;
            animator.SetFloat(param.nameHash, 0f);
        }

        foreach (var param in parameters)
        {
            if (param.index < 0) continue;
            float weight = animator.GetFloat(param.nameHash);
            weight += param.weight * param.maxWeight * volume;
            animator.SetFloat(param.nameHash, weight);
        }
    }
}

}
