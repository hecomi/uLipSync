using UnityEngine;
using System.Collections.Generic;

namespace uLipSync
{
	[ExecuteAlways]
	public class uLipSyncAnimator : MonoBehaviour
	{
		[System.Serializable]
		public class AnimatorInfo
		{
			public string phoneme;
			public int index = -1;
			public float maxWeight = 1f;

			public float weight { get; set; } = 0f;
			public float weightVelocity { get; set; } = 0f;
			public int nameHash;
			public string name;
		}

		public UpdateMethod updateMethod = UpdateMethod.LateUpdate;
		public Animator animator;

		/// <summary>
		/// Animator and animatorinfo for controlling the Animator parameters.
		/// </summary>
		public List<AnimatorInfo> parameters = new List<AnimatorInfo>();
		public float minVolume = -2.5f;
		public float maxVolume = -1.5f;
		[Range(0f, 0.3f)] public float smoothness = 0.05f;

		LipSyncInfo _info = new LipSyncInfo();
		bool _lipSyncUpdated = false;
		float _volume = 0f;
		float _openCloseVelocity = 0f;
		protected float volume => _volume;

#if UNITY_EDITOR
		bool _isAnimationBaking = false;
		float _animBakeDeltaTime = 1f / 60;
#endif

		public void OnLipSyncUpdate(LipSyncInfo info)
		{
			_info = info;
			_lipSyncUpdated = true;
			if (updateMethod == UpdateMethod.LipSyncUpdateEvent)
			{
				UpdateVolume();
				UpdateVowels();
				_lipSyncUpdated = false;
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
#if UNITY_EDITOR
			if (_isAnimationBaking)
				return;
#endif
			if (updateMethod != UpdateMethod.LipSyncUpdateEvent)
			{
				UpdateVolume();
				UpdateVowels();
				_lipSyncUpdated = false;
			}

			if (updateMethod == UpdateMethod.Update)
			{
				OnApplyAnimator();
			}
		}

		void LateUpdate()
		{
#if UNITY_EDITOR
			if (_isAnimationBaking)
				return;
#endif
			if (updateMethod == UpdateMethod.LateUpdate)
			{
				OnApplyAnimator();
			}

		}
		void FixedUpdate()
		{
#if UNITY_EDITOR
			if (_isAnimationBaking)
				return;
#endif
			if (updateMethod == UpdateMethod.FixedUpdate)
			{
				OnApplyAnimator();
			}
		}

		float SmoothDamp(float value, float target, ref float velocity)
		{
#if UNITY_EDITOR
			return Mathf.SmoothDamp(value, target, ref velocity, smoothness, Mathf.Infinity, _animBakeDeltaTime);
#else
        	return Mathf.SmoothDamp(value, target, ref velocity, smoothness);
#endif
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
#if UNITY_EDITOR
			_volume = SmoothDamp(_volume, normVol, ref _openCloseVelocity);
#else
        _volume = SmoothDamp(_volume, normVol, ref _openCloseVelocity);
#endif
		}

		void UpdateVowels()
		{
			float sum = 0f;
			var ratios = _info.phonemeRatios;

			foreach (var par in parameters)
			{
				float targetWeight = 0f;
				// float targetWeight = (par.phoneme == phoneme) ? 1f : 0f;
				if (ratios != null && !string.IsNullOrEmpty(par.phoneme))
				{
					ratios.TryGetValue(par.phoneme, out targetWeight);
				}
				float weightVel = par.weightVelocity;
				par.weight = SmoothDamp(par.weight, targetWeight, ref weightVel);
				par.weightVelocity = weightVel;
				sum += par.weight;
			}

			foreach (var par in parameters)
			{
				par.weight = sum > 0f ? par.weight / sum : 0f;
			}
		}

		public void ApplyAnimator()
		{
			if (updateMethod == UpdateMethod.External)
			{
				OnApplyAnimator();
			}
		}

		protected virtual void OnApplyAnimator()
		{
			if (!animator)
				return;

			// use hash
			foreach (var par in parameters)
			{
				if (par.index < 0)
					continue;
				animator.SetFloat(par.nameHash, 0f);
			}

			foreach (var par in parameters)
			{
				if (par.index < 0)
					continue;
				float weight = animator.GetFloat(par.nameHash);
				weight += par.weight * par.maxWeight * volume;
				animator.SetFloat(par.nameHash, weight);
			}
		}
	}
}
