using UnityEngine;
using UnityEditor;

namespace uLipSync
{

[CustomEditor(typeof(uLipSyncMicrophone))]
public class uLipSyncMicrophoneEditor : Editor
{
    uLipSyncMicrophone mic { get { return target as uLipSyncMicrophone; } }

    public override void OnInspectorGUI()
    {
        mic.UpdateMicInfo();
        DrawDevices();
        DrawProps();

        if (Application.isPlaying)
        {
            DrawButtons();
        }
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
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        if (mic.isRecording && GUILayout.Button($"Record & Play (last {mic.clip.length} sec)", GUILayout.Width(180)))
        {
            mic.StopRecordAndCreateAudioClip();
        }

        if (!mic.isRecording && mic.isPlaying && GUILayout.Button("Stop", GUILayout.Width(120)))
        {
            mic.source.Stop();
        }

        if (GUILayout.Button(!mic.isRecording ? "Start Mic" : "Stop Mic", GUILayout.Width(120)))
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

        EditorGUILayout.EndHorizontal();
    }
}

}
