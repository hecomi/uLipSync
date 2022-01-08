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
        var audioTracks = timelineAsset.GetOutputTracks();
        var audioTrack = audioTracks.FirstOrDefault(x => x.GetType() == typeof(AudioTrack));
        foreach (var clip in audioTrack.GetClips())
        {
            var start = clip.start;
            var end = clip.end;
            var clipIn = clip.clipIn;
        }
        return ScriptPlayable<uLipSyncMixer>.Create(graph, inputCount);
    }
}

}