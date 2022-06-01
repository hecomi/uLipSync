using UnityEngine;
using System.Collections.Generic;

namespace uLipSync.Timeline
{

[ExecuteAlways]
public class uLipSyncTimelineEvent : MonoBehaviour
{
    public LipSyncUpdateEvent onLipSyncUpdate = new LipSyncUpdateEvent();

    public void OnFrame(BakedFrame frame)
    {
        var info = BakedData.GetLipSyncInfo(frame);
        onLipSyncUpdate.Invoke(info);
    }
}

}
