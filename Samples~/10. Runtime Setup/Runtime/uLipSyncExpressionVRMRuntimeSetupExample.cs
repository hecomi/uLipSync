using UnityEngine;
using System.Collections.Generic;

public class uLipSyncExpressionVRMRuntimeSetupExample : MonoBehaviour
{
#if USE_VRM10
    [System.Serializable]
    public class PhonemeBlendShapeInfo
    {
        public string phoneme;
        public UniVRM10.ExpressionPreset expression;
    }

    public GameObject target;
    public uLipSync.Profile profile;
    public List<PhonemeBlendShapeInfo> phonemeBlendShapeTable = new();
    
    uLipSync.uLipSync _lipsync;
    uLipSync.uLipSyncExpressionVRM _expression;

    void Start()
    {
        if (!target) return;

        SetupExpression();
        SetupLipSync();
    }

    void SetupExpression()
    {
        _expression = target.AddComponent<uLipSync.uLipSyncExpressionVRM>();

        foreach (var info in phonemeBlendShapeTable)
        {
            _expression.AddBlendShape(info.phoneme, info.expression.ToString());
        }
    }

    void SetupLipSync()
    {
        if (!_expression) return;

        _lipsync = target.AddComponent<uLipSync.uLipSync>();
        _lipsync.profile = profile;
        _lipsync.onLipSyncUpdate.AddListener(_expression.OnLipSyncUpdate);
    }

#else

    void Start()
    {
        Debug.LogWarning("Please import VRM assets to use this component and set USE_VRM10 to asmdef.");
    }

#endif
}