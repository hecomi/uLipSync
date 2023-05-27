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
    [Range(0f, 1f)] public float volume = 1f;
    [Range(-0.3f, 0.3f)] public float timeOffset = 0f;

    public uLipSyncBehaviour behaviour { get; private set; }

    public override double duration => bakedData ? bakedData.duration : base.duration;

    public ClipCaps clipCaps
    {
        get => 
            ClipCaps.ClipIn | 
            ClipCaps.SpeedMultiplier |
            ClipCaps.Blending;
    }

    public override Playable CreatePlayable(PlayableGraph graph, GameObject go)
    {
        var playable = ScriptPlayable<uLipSyncBehaviour>.Create(graph);
        behaviour = playable.GetBehaviour();
        behaviour.asset = this;
        return playable;
    }
}

}