using UnityEngine;
using System.Collections.Generic;

namespace uLipSync
{

[System.Serializable]
public class BlendShapeInfo
{
    public int index = -1;
    public float factor = 1f;
    public float blend = 0f;
}

public class uLipSyncBlendShape : MonoBehaviour
{
    public SkinnedMeshRenderer skinnedMeshRenderer;
    public List<BlendShapeInfo> blendShapeList = new List<BlendShapeInfo>()
    {
        new BlendShapeInfo(),
        new BlendShapeInfo(),
        new BlendShapeInfo(),
        new BlendShapeInfo(),
        new BlendShapeInfo(),
        new BlendShapeInfo(),
    };
    float volume_ = 0f;

#if UNITY_EDITOR
    public bool findFromChildren;
#endif

    public void OnLipSyncUpdate(LipSyncInfo info)
    {
        foreach (var kv in info.vowels)
        {
            int i = (int)kv.Key;
            blendShapeList[i].blend = kv.Value;
        }
        volume_ = info.volume;
    }

    void LateUpdate()
    {
        if (!skinnedMeshRenderer) return;

        foreach (var info in blendShapeList)
        {
            if (info.index < 0) continue;

            float blend = info.blend * info.factor * volume_ * 100;
            skinnedMeshRenderer.SetBlendShapeWeight(info.index, blend);
        }
    }
}

}

