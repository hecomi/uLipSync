using UnityEngine;

namespace uLipSync.Timeline
{

[ExecuteAlways]
public class uLipSyncTimelineEvent : MonoBehaviour
{
    public LipSyncUpdateEvent onLipSyncUpdate = new LipSyncUpdateEvent();
    public BakedData bakedData { get; set; }

    public void OnFrame(float t)
    {
        if (!bakedData) return;
        var info = bakedData.GetLipSyncInfo(t);
        onLipSyncUpdate.Invoke(info);
    }
}

}
