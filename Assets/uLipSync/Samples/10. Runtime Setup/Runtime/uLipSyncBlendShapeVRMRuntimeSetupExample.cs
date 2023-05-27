using UnityEngine;
using System.Collections.Generic;

public class uLipSyncBlendShapeVRMRuntimeSetupExample : MonoBehaviour
{
    [System.Serializable]
    public class PhonemeBlendShapeInfo
    {
        public string phoneme;
        public string blendShapeClip;
    }

    public GameObject target;
    public uLipSync.Profile profile;
    public List<PhonemeBlendShapeInfo> phonemeBlendShapeTable = new List<PhonemeBlendShapeInfo>();
    
    uLipSync.uLipSync _lipsync;
    uLipSync.uLipSyncBlendShapeVRM _blendShape;

#if USE_VRM0X // defined by the Version Definition in asmdef

    void Start()
    {
        if (!target) return;

        SetupBlendShpae();
        SetupLipSync();
    }

    void SetupBlendShpae()
    {
        _blendShape = target.AddComponent<uLipSync.uLipSyncBlendShapeVRM>();

        foreach (var info in phonemeBlendShapeTable)
        {
            _blendShape.AddBlendShape(info.phoneme, info.blendShapeClip);
        }
    }

    void SetupLipSync()
    {
        if (!_blendShape) return;

        _lipsync = target.AddComponent<uLipSync.uLipSync>();
        _lipsync.profile = profile;
        _lipsync.onLipSyncUpdate.AddListener(_blendShape.OnLipSyncUpdate);
    }

#else

    void Start()
    {
        Debug.LogWarning("Please import VRM assets to use this component and set USE_VRM0X to asmdef.");
    }

#endif
}