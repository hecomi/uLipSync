using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;
using System.Linq;
#if UNITY_EDITOR
using System.ComponentModel;
#endif

namespace uLipSync.Timeline
{

[System.Serializable]
[TrackColor(0.2f, 0.8f, 0.3f)]
[TrackClipType(typeof(uLipSyncClip))]
[TrackBindingType(typeof(uLipSyncTimelineEvent))]
#if UNITY_EDITOR
[DisplayName("uLipSync Track")]
#endif
public class uLipSyncTrack : TrackAsset
{
    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
        UpdateClipNames();

        var playable = ScriptPlayable<uLipSyncMixer>.Create(graph, inputCount);
        var mixer = playable.GetBehaviour();
        mixer.clips = GetClips().ToArray();

        return playable;
    }

    void UpdateClipNames()
    {
        foreach (var clip in GetClips())
        {
            var asset = clip.asset as uLipSyncClip;
            if (asset && asset.bakedData && asset.bakedData.audioClip)
            {
                clip.displayName = asset.bakedData.audioClip.name;
            }
        }
    }
}

}