using UnityEngine;
using System.Collections.Generic;

namespace uLipSync.Timeline
{

[ExecuteAlways]
public class uLipSyncTimelineEvent : MonoBehaviour
{
    public LipSyncUpdateEvent onLipSyncUpdate = new LipSyncUpdateEvent();
    BakedFrame _frame = BakedFrame.zero;

    public void OnFrame(BakedFrame frame)
    {
        _frame = frame;
        var info = BakedData.GetLipSyncInfo(_frame);
        onLipSyncUpdate.Invoke(info);
    }
}

}
