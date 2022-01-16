using UnityEngine;
using System.Collections.Generic;

namespace uLipSync.Timeline
{

[ExecuteAlways]
public class uLipSyncTimelineEvent : MonoBehaviour
{
    public LipSyncUpdateEvent onLipSyncUpdate = new LipSyncUpdateEvent();
    BakedFrame _frame = BakedFrame.zero;
    bool _isTimelineActive = false;

    public void OnFrame(BakedFrame frame)
    {
        var info = BakedData.GetLipSyncInfo(frame);
        onLipSyncUpdate.Invoke(info);

        _frame = frame;
        _isTimelineActive = true;
    }

    public void OnStop()
    {
        _isTimelineActive = false;
    }

    void Update()
    {
        if (!_isTimelineActive) return;

        var info = BakedData.GetLipSyncInfo(_frame);
        onLipSyncUpdate.Invoke(info);
    }
}

}
