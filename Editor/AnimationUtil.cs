using UnityEngine;
using UnityEditor;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace uLipSync
{

internal class AnimationCurveBindingData
{
    public EditorCurveBinding binding;
    public AnimationCurve curve;
}

public class CreateAnimationClipFromBakedDataInput
{
    public GameObject gameObject;
    public AnimationBakableMonoBehaviour animBake;
    public float sampleFrameRate = 60f;
    public float threshold = 0f;
    public BakedData bakedData;
}

public static class AnimationUtil
{
    public static AnimationClip CreateAnimationClipFromBakedData(CreateAnimationClipFromBakedDataInput input)
    {
        var clip = new AnimationClip();

        var animBake = input.animBake;
        var bakedData = input.bakedData;
        if (!animBake || !bakedData) return clip;

        var dataList = new List<AnimationCurveBindingData>();
        var bindingBase = new EditorCurveBinding()
        {
            path = EditorUtil.GetRelativeHierarchyPath(animBake.target, input.gameObject),
            type = typeof(SkinnedMeshRenderer)
        };
        var props = animBake.GetPropertyNames();

        foreach (var prop in props)
        {
            var binding = bindingBase;
            binding.propertyName = prop;

            dataList.Add(new AnimationCurveBindingData()
            {
                binding = binding,
                curve = new AnimationCurve(),
            });
        }

        animBake.OnAnimationBakeStart();

        var frame = bakedData.GetFrame(0f);
        var info = BakedData.GetLipSyncInfo(frame);
        animBake.OnAnimationBakeUpdate(info, 0f);

        var dt = 1f / input.sampleFrameRate;
        var maxWeight = animBake.maxWeight;
        var minWeight = animBake.minWeight;
        var weightScale = maxWeight - minWeight;
        var weightThreshLo = minWeight + weightScale * (input.threshold * 0.1f);
        var weightThreshHi = maxWeight - weightScale * (input.threshold * 0.1f);

        var initWeights = animBake.GetPropertyWeights();
        var preKeyWeights = initWeights.ToList();
        var preFrameWeights = initWeights.ToList();

        for (float time = 0f; time <= bakedData.duration; time += dt)
        {
            frame = bakedData.GetFrame(time);
            info = BakedData.GetLipSyncInfo(frame);
            animBake.OnAnimationBakeUpdate(info, dt);

            var weights = animBake.GetPropertyWeights();
            if (preFrameWeights.Count == 0) preFrameWeights = weights;

            for (int i = 0; i < dataList.Count; ++i)
            {
                var data = dataList[i];
                var curve = data.curve;
                var weight = weights[i];
                if (weight < weightThreshLo) weight = minWeight;
                if (weight > weightThreshHi) weight = maxWeight;
                var preFrameWeight = preFrameWeights[i];

                if (time == 0f)
                {
                    curve.AddKey(0f, preFrameWeight);
                    continue;
                }

                // Points where the weight changes from 0 or 1 are added regardless of the threshold.
                bool isEdgePeakKey = 
                    (preFrameWeight <= weightThreshLo && weight >= weightThreshLo) ||
                    (preFrameWeight >= weightThreshHi && weight <= weightThreshHi);
                if (isEdgePeakKey)
                {
                    curve.AddKey(time - dt, preFrameWeight);
                    preKeyWeights[i] = preFrameWeight;
                }

                // Start points with a weight of 0 or 1 are added regardless of the threshold.
                var preKeyWeight = preKeyWeights[i];
                var dWeight = Mathf.Abs(weight - preKeyWeight);
                bool isThresholdExceeded = dWeight > input.threshold * weightScale;
                bool isStartKey = 
                    (weight <= weightThreshLo && preFrameWeight >= weightThreshLo) ||
                    (weight >= weightThreshHi && preFrameWeight <= weightThreshHi);
                if (isThresholdExceeded || isStartKey)
                {
                    preKeyWeights[i] = weight;
                    curve.AddKey(time, weight);
                }
            }

            preFrameWeights = weights;
        }

        animBake.OnAnimationBakeEnd();

        for (int i = 0; i < dataList.Count; ++i)
        {
            var data = dataList[i];
            var binding = data.binding;
            var curve = data.curve;

            for (int j = 0; j < curve.length - 1; ++j)
            {
                var key = curve[j];
                key.weightedMode = WeightedMode.Both;
                key.inWeight = key.outWeight = 1f / 4f;

                if (j == 0)
                {
                    var nextKey = curve[j + 1];
                    var dwn = nextKey.value - key.value;
                    var dtn = nextKey.time - key.time;
                    key.inTangent = key.outTangent = dwn / dtn;
                }
                else if (j == curve.length - 1)
                {
                    var prevKey = curve[j - 1];
                    var dwp = key.value - prevKey.value;
                    var dtp = key.time - prevKey.time;
                    key.inTangent = key.outTangent = dwp / dtp;
                }
                else if (key.value < weightThreshLo || key.value > weightThreshHi)
                {
                    key.inTangent = key.outTangent = 0f;
                }
                else
                {
                    var prevKey = curve[j - 1];
                    var nextKey = curve[j + 1];

                    var dwn = nextKey.value - key.value;
                    var dtn = nextKey.time - key.time;
                    var dwp = key.value - prevKey.value;
                    var dtp = key.time - prevKey.time;
                    key.inWeight = dtn / (dtp + dtn) * (2f / 3f);
                    key.outWeight = dtp / (dtp + dtn) * (2f / 3f);;

                    if (dwp * dwn < 0f)
                    {
                        key.inTangent = key.outTangent = 0f;
                    }
                    else
                    {
                        var a = Mathf.Lerp(dwp / dtp, dwn / dtn, dtp / (dtp + dtn));
                        key.inTangent = key.outTangent = a;
                    }
                }

                curve.MoveKey(j, key);
            }

            AnimationUtility.SetEditorCurve(clip, data.binding, data.curve);
        }

        return clip;
    }
}

}