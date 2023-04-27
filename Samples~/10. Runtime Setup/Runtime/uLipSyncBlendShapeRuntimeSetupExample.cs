using UnityEngine;
using System.Collections.Generic;

public class uLipSyncBlendShapeRuntimeSetupExample : MonoBehaviour
{
    [System.Serializable]
    public class PhonemeBlendShapeInfo
    {
        public string phoneme;
        public string blendShape;
    }

    public GameObject target;
    public uLipSync.Profile profile;
    public string skinnedMeshRendererName = "MTH_DEF";
    public List<PhonemeBlendShapeInfo> phonemeBlendShapeTable = new List<PhonemeBlendShapeInfo>();
    
    uLipSync.uLipSync _lipsync;
    uLipSync.uLipSyncBlendShape _blendShape;

    void Start()
    {
        if (!target) return;

        SetupBlendShpae();
        SetupLipSync();
    }

    void SetupBlendShpae()
    {
        var targetTform = uLipSync.Util.FindChildRecursively(target.transform, skinnedMeshRendererName);
        if (!targetTform) 
        {
            Debug.LogWarning($"There is no GameObject named \"{skinnedMeshRendererName}\"");
            return;
        }

        var smr = targetTform.GetComponent<SkinnedMeshRenderer>();
        if (!smr) 
        {
            Debug.LogWarning($"\"{skinnedMeshRendererName}\" does not have SkinnedMeshRenderer.");
            return;
        }

        _blendShape = target.AddComponent<uLipSync.uLipSyncBlendShape>();
        _blendShape.skinnedMeshRenderer = smr;

        foreach (var info in phonemeBlendShapeTable)
        {
            _blendShape.AddBlendShape(info.phoneme, info.blendShape);
        }
    }

    void SetupLipSync()
    {
        if (!_blendShape) return;

        _lipsync = target.AddComponent<uLipSync.uLipSync>();
        _lipsync.profile = profile;
        _lipsync.onLipSyncUpdate.AddListener(_blendShape.OnLipSyncUpdate);
    }
}