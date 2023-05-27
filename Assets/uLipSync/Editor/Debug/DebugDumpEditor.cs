using System.Text;
using UnityEngine;
using UnityEditor;

namespace uLipSync.Debugging
{

[CustomEditor(typeof(DebugDump))]
public class DebugDumpEditor : Editor
{
    DebugDump dump => target as DebugDump;
    uLipSync lipsync => dump?.lipsync;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
#if !ULIPSYNC_DEBUG
        EditorGUILayout.HelpBox(
            "You need to define ULIPSYNC_DEBUG to enable debug dump functions.",
            MessageType.Error);
        EditorGUILayout.Space();
#endif

        var buttonLayout = GUILayout.Width(48);
        
        EditorUtil.DrawProperty(serializedObject, nameof(dump.prefix));
        
        EditorGUILayout.Space();
        
        EditorGUILayout.BeginHorizontal();
        EditorUtil.DrawProperty(serializedObject, nameof(dump.dataFile));
        if (GUILayout.Button("Save", EditorStyles.miniButton, buttonLayout))
        {
            dump.DumpData();
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        EditorUtil.DrawProperty(serializedObject, nameof(dump.spectrumFile));
        if (GUILayout.Button("Save", EditorStyles.miniButton, buttonLayout))
        {
            dump.DumpSpectrum();
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        EditorUtil.DrawProperty(serializedObject, nameof(dump.melSpectrumFile));
        if (GUILayout.Button("Save", EditorStyles.miniButton, buttonLayout))
        {
            dump.DumpMelSpectrum();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorUtil.DrawProperty(serializedObject, nameof(dump.melCepstrumFile));
        if (GUILayout.Button("Save", EditorStyles.miniButton, buttonLayout))
        {
            dump.DumpMelCepstrum();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorUtil.DrawProperty(serializedObject, nameof(dump.mfccFile));
        if (GUILayout.Button("Save", EditorStyles.miniButton, buttonLayout))
        {
            dump.DumpMfcc();
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button(" Save All "))
        {
            dump.DumpAll();
        }
        EditorGUILayout.EndHorizontal();

        serializedObject.ApplyModifiedProperties();
    }
}

}