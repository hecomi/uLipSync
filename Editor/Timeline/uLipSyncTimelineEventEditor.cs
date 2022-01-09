using UnityEngine.Events;
using UnityEditor;

namespace uLipSync.Timeline
{

[CustomEditor(typeof(uLipSyncTimelineEvent))]
public class uLipSyncTimelineEventEditor : Editor
{
    uLipSyncTimelineEvent timelineEvent => target as uLipSyncTimelineEvent;
    int _listenerCount = 0;

    void OnEnable()
    {
        _listenerCount = timelineEvent.onLipSyncUpdate.GetPersistentEventCount();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.Separator();

        EditorUtil.DrawProperty(serializedObject, nameof(timelineEvent.onLipSyncUpdate));
        var count = timelineEvent.onLipSyncUpdate.GetPersistentEventCount();
        if (count > _listenerCount)
        {
            timelineEvent.onLipSyncUpdate.SetPersistentListenerState(
                count - 1, 
                UnityEventCallState.EditorAndRuntime);
        }
        _listenerCount = count;

        serializedObject.ApplyModifiedProperties();
    }
}

}
