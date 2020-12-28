using UnityEngine;
using UnityEditor;

namespace uLipSync
{

[CustomEditor(typeof(uLipSyncMicInput))]
public class uLipSyncMicInputEditor : Editor
{
    uLipSyncMicInput mic { get { return target as uLipSyncMicInput; } }

    public override void OnInspectorGUI()
    {
        mic.UpdateMicInfo();
        DrawDevices();
        DrawProps();
        DrawButtons();
    }

    void DrawDevices()
    {
        EditorUtil.DrawMicSelector(ref mic.index);
    }

    void DrawProps()
    {
        mic.isAutoStart = EditorGUILayout.Toggle("Is Auto Start", mic.isAutoStart);
        EditorGUILayout.LabelField("Frequency", $"Min: {mic.device.minFreq} Hz / Max: {mic.device.maxFreq} Hz");
        if (Application.isPlaying)
        {
            EditorGUILayout.Toggle("Is Ready", mic.isReady);
            EditorGUILayout.Toggle("Is Recording", mic.isRecording);
        }
    }

    void DrawButtons()
    {
        if (!Application.isPlaying) return;

        var buttonStyle = EditorStyles.miniButton;
        var str = "Start";
        if (mic.isRecording) 
        {
            buttonStyle.normal.textColor = Color.gray;
            buttonStyle.focused.textColor = Color.gray;
            buttonStyle.hover.textColor = Color.gray;
            buttonStyle.active.textColor = Color.gray;
            str = "Stop";
        }
        if (GUILayout.Button(str, buttonStyle))
        {
            if (mic.isRecording)
            {
                mic.StopRecord();
            }
            else
            {
                mic.StartRecord();
            }
        }
    }
}

}
