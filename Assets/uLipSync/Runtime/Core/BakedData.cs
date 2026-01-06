using System;
using UnityEngine;
using Unity.Burst;
using System.Collections.Generic;
using System.Linq;

namespace uLipSync
{
    [System.Serializable]
    public struct BakedPhonemeRatio
    {
        public string phoneme;
        public float ratio;
    }

    [System.Serializable]
    public struct BakedFrame
    {
        public float volume;
        public List<BakedPhonemeRatio> phonemes;

        public static BakedFrame zero
        {
            get => new BakedFrame()
            {
                volume = 0,
                phonemes = new List<BakedPhonemeRatio>(),
            };
        }
    }

    [BurstCompile]
    [CreateAssetMenu(menuName = Common.AssetName + "/Baked Data")]
    public class BakedData : ScriptableObject
    {
        public Profile profile;
        public AudioClip audioClip;

        [HideInInspector] public Profile bakedProfile;
        [HideInInspector] public AudioClip bakedAudioClip;

        public float duration = 0f;
        public List<BakedFrame> frames = new();
        public List<string> phonemes = new();
        public bool isSparse = false;

        public bool isValid => duration > 0f && frames.Count > 0;

        public bool isDataChanged => (profile != bakedProfile) || (audioClip != bakedAudioClip);

        public BakedFrame GetFrame(float t)
        {
            if (frames == null || frames.Count == 0) return BakedFrame.zero;

            int index0 = (int)Mathf.Floor(t * 60f);
            int index1 = index0 + 1;
            index0 = Mathf.Clamp(index0, 0, frames.Count - 1);
            index1 = Mathf.Clamp(index1, 0, frames.Count - 1);
            var frame0 = frames[index0];
            var frame1 = frames[index1];
            bool isOutOfRange = index0 == index1;
            float a = t * 60f - index0;

            var frame = new BakedFrame
            {
                phonemes = new List<BakedPhonemeRatio>()
            };

            if (!isSparse)
            {
                // Old format: assume each frame has all phonemes in order
                var phonemeCount = bakedProfile.GetPhonemeNames().Length; // Assuming Profile has phonemes list
                for (int i = 0; i < phonemeCount; ++i)
                {
                    float ratio0 = frame0.phonemes[i].ratio;
                    float ratio1 = frame1.phonemes[i].ratio;
                    float ratio = Mathf.Lerp(ratio0, ratio1, a);
                    string phoneme = bakedProfile.GetPhonemeNames()[i];
                    frame.phonemes.Add(new BakedPhonemeRatio { phoneme = phoneme, ratio = ratio });
                }

                frame.volume = isOutOfRange ? 0f : Mathf.Lerp(frame0.volume, frame1.volume, a);
            }
            else
            {
                // Sparse format: use phonemes list and interpolate ratios
                var phonemeCount = phonemes.Count;
                for (int i = 0; i < phonemeCount; ++i)
                {
                    string phoneme = phonemes[i];
                    float ratio0 = GetRatio(frame0, phoneme);
                    float ratio1 = GetRatio(frame1, phoneme);
                    float ratio = Mathf.Lerp(ratio0, ratio1, a);
                    frame.phonemes.Add(new BakedPhonemeRatio { phoneme = phoneme, ratio = ratio });
                }

                frame.volume = isOutOfRange ? 0f : Mathf.Lerp(frame0.volume, frame1.volume, a);
            }

            return frame;
        }

        private float GetRatio(BakedFrame frame, string phoneme)
        {
            foreach (var pr in frame.phonemes)
            {
                if (pr.phoneme == phoneme) return pr.ratio;
            }

            return 0f;
        }

        public LipSyncInfo GetLipSyncInfo(float t)
        {
            var frame = GetFrame(t);
            return GetLipSyncInfo(frame);
        }

        public static LipSyncInfo GetLipSyncInfo(BakedFrame frame)
        {
            var info = new LipSyncInfo
            {
                phonemeRatios = new Dictionary<string, float>()
            };

            float maxRatio = 0f;
            foreach (var pr in frame.phonemes)
            {
                if (pr.ratio > maxRatio)
                {
                    info.phoneme = pr.phoneme;
                    maxRatio = pr.ratio;
                }

                if (info.phonemeRatios.ContainsKey(pr.phoneme))
                {
                    info.phonemeRatios[pr.phoneme] += pr.ratio;
                }
                else
                {
                    info.phonemeRatios.Add(pr.phoneme, pr.ratio);
                }
            }

            float minVol = Common.DefaultMinVolume;
            float maxVol = Common.DefaultMaxVolume;
            float normVol = Mathf.Log10(frame.volume);
            normVol = (normVol - minVol) / (maxVol - minVol);
            normVol = Mathf.Clamp(normVol, 0f, 1f);
            info.volume = normVol;
            info.rawVolume = frame.volume;

            return info;
        }

        public static Color[] phonemeColors =
        {
            Color.red,
            Color.cyan,
            Color.yellow,
            Color.magenta,
            Color.green,
            Color.blue,
            Color.gray,
        };

        [ContextMenu("Convert to Sparse")]
        private void ConvertToSparseEditor()
        {
            ConvertToSparse();

#if UNITY_EDITOR
            //Serialize the asset to save the changes
            UnityEditor.AssetDatabase.SaveAssets();
#endif
        }

        public void ConvertToSparse()
        {
            if (isSparse) return;

            // Set the phonemes list from the baked profile
            phonemes = new List<string>(bakedProfile.GetPhonemeNames());

            // Copy the old frames list
            var oldFrames = new List<BakedFrame>(frames);

            // Clear the existing frames list
            frames.Clear();

            // Create a new list of sparse frames
            var newFrames = new List<BakedFrame>();
            foreach (BakedFrame oldFrame in oldFrames)
            {
                BakedFrame newFrame = new BakedFrame
                {
                    volume = oldFrame.volume,
                    phonemes = new List<BakedPhonemeRatio>()
                };

                // Only include phonemes with non-zero ratios
                foreach (BakedPhonemeRatio pr in oldFrame.phonemes.Where(pr => pr.ratio > 0f))
                {
                    newFrame.phonemes.Add(new BakedPhonemeRatio
                    {
                        phoneme = pr.phoneme,
                        ratio = pr.ratio
                    });
                }

                newFrames.Add(newFrame);
            }

            // Assign the new sparse frames list back to frames
            frames = newFrames;
            isSparse = true;


#if UNITY_EDITOR
            //Serialize the asset to save the changes
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        [ContextMenu("Convert to Dense")]
        public void SetSparseFalse()
        {
            isSparse = false;
        }
    }
}
