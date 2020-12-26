using UnityEngine;
using UnityEditor;
using System.Linq;

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
    }

    void DrawDevices()
    {
        var mics = MicUtil.GetDeviceList();
        var micNames = mics.Select(x => x.name).ToArray();
        var index = mic.index;
        mic.index = EditorGUILayout.Popup("Device", index, micNames);
    }

    void DrawProps()
    {
        EditorGUILayout.LabelField("Frequency", $"Min: {mic.device.minFreq} Hz / Max: {mic.device.maxFreq} Hz");
        if (Application.isPlaying)
        {
            EditorGUILayout.Toggle("Is Ready", mic.isReady);
            EditorGUILayout.Toggle("Is Recording", mic.isRecording);
        }
    }
}

}
