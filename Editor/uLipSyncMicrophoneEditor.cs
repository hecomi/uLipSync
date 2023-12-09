using UnityEngine;
using UnityEditor;

namespace uLipSync
{

[CustomEditor(typeof(uLipSyncMicrophone))]
public class uLipSyncMicrophoneEditor : Editor
{
#if !UNITY_WEBGL || UNITY_EDITOR 
    uLipSyncMicrophone mic => target as uLipSyncMicrophone;
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        if (!Application.isPlaying)
        {
            mic.UpdateMicInfo();
        }

        DrawProps();

        if (Application.isPlaying)
        {
            DrawButtons();
        }

        serializedObject.ApplyModifiedProperties();
    }

    void DrawProps()
    {
        if (EditorUtil.Foldout("Device", true, "-uLipSync-Microphone"))
        {
            ++EditorGUI.indentLevel;
            EditorUtil.DrawMicSelector(ref mic.index);
            EditorUtil.DrawProperty(serializedObject, "isAutoStart");
            --EditorGUI.indentLevel;
            EditorGUILayout.Separator();
        }

        if (EditorUtil.Foldout("Advanced", false, "-uLipSync-Microphone"))
        {
            ++EditorGUI.indentLevel;
            EditorUtil.DrawProperty(serializedObject, "latencyTolerance");
            EditorUtil.DrawProperty(serializedObject, "bufferTime");
            --EditorGUI.indentLevel;
            EditorGUILayout.Separator();
        }

        if (EditorUtil.Foldout("Information", true, "-uLipSync-Microphone"))
        {
            var onStyle = new GUIStyle(EditorStyles.label);
            onStyle.normal.textColor = Color.green;

            var offStyle = new GUIStyle(EditorStyles.label);
            offStyle.normal.textColor = Color.red;

            ++EditorGUI.indentLevel;

            EditorGUILayout.LabelField("Mic to AudioSource Latency", $"{mic.latency:0.00} s");
            EditorGUILayout.LabelField("Frequency", $"Min: {mic.device.minFreq} Hz / Max: {mic.device.maxFreq} Hz");

            if (Application.isPlaying)
            {
                if (mic.isReady) EditorGUILayout.LabelField("Is Ready", "Ready", onStyle);
                else EditorGUILayout.LabelField("Is Ready", "Not Ready", offStyle);

                if (mic.isRecording) EditorGUILayout.LabelField("Is Recording", "Recording", onStyle);
                else EditorGUILayout.LabelField("Is Recording", "Stop", offStyle);
                
                EditorGUILayout.LabelField("Latency", $"{mic.latency:0.00}", !mic.isOutOfSync ? onStyle : offStyle);
            }

            --EditorGUI.indentLevel;
        }
    }

    void DrawButtons()
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        if (mic.isRecording && 
            mic.clip &&
            GUILayout.Button($"Record & Play (last {mic.clip.length} sec)", GUILayout.Width(180)))
        {
            mic.StopRecordAndCreateAudioClip();
        }

        if (!mic.isRecording && 
            mic.isMicClipSet && 
            mic.isPlaying && 
            GUILayout.Button("Stop", GUILayout.Width(120)))
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
#endif
}

}
