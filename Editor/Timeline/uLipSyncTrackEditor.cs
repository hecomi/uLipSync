using UnityEngine;
using UnityEngine.Timeline;
using UnityEditor.Timeline;

namespace uLipSync.Timeline
{

[CustomTimelineEditor(typeof(uLipSyncTrack))]
public class uLipSyncTrackTrackEditor : TrackEditor
{
    Texture2D _iconTexture;

    public override void OnCreate(TrackAsset track, TrackAsset copiedFrom)
    {
        base.OnCreate(track, copiedFrom);
        track.name = copiedFrom ? copiedFrom.name : "uLipSync Track";
    }

    public override TrackDrawOptions GetTrackOptions(TrackAsset track, Object binding)
    {
        if (!_iconTexture)
        {
            _iconTexture = Resources.Load<Texture2D>("uLipSync-Icon");
        }
        var options = base.GetTrackOptions(track, binding);
        options.icon = _iconTexture;
        return options;
    }
}

}
