using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
#if UNITY_EDITOR
using System.ComponentModel;
#endif

namespace uLipSync.Timeline
{

[System.Serializable]
#if UNITY_EDITOR
[DisplayName("uLipSync")]
#endif
public class uLipSyncClip : PlayableAsset, ITimelineClipAsset
{
    public BakedData bakedData;

    public ClipCaps clipCaps
    {
        get => 
            ClipCaps.ClipIn | 
            ClipCaps.SpeedMultiplier |
            ClipCaps.Blending;
    }

    public override Playable CreatePlayable(PlayableGraph graph, GameObject go)
    {
        var behaviour = new uLipSyncBehaviour();
        behaviour.asset = this;
        var playable = ScriptPlayable<uLipSyncBehaviour>.Create(graph, behaviour);
        return playable;
    }
}

}