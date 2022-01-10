using UnityEngine;
using UnityEditor;

namespace uLipSync
{

[CustomEditor(typeof(uLipSyncBakedDataPlayer))]
public class uLipSyncBakedDataPlayerEditor : Editor
{
    uLipSyncBakedDataPlayer player { get => target as uLipSyncBakedDataPlayer; }
    BakedData data { get => player.bakedData; }
    Editor _bakedDataEditor = null;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        if (EditorUtil.Foldout("Data", true, "-uLipSyncBaledDataPlayer"))
        {
            ++EditorGUI.indentLevel;
            DrawBakedData();
            --EditorGUI.indentLevel;
            EditorGUILayout.Separator();
        }

        if (EditorUtil.Foldout("Parameters", true, "-uLipSyncBaledDataPlayer"))
        {
            ++EditorGUI.indentLevel;
            DrawParameters();
            --EditorGUI.indentLevel;
            EditorGUILayout.Separator();
        }

        if (EditorUtil.Foldout("Play", true, "-uLipSyncBaledDataPlayer"))
        {
            ++EditorGUI.indentLevel;
            DrawPlay();
            --EditorGUI.indentLevel;
            EditorGUILayout.Separator();
        }

        serializedObject.ApplyModifiedProperties();
    }

    void DrawBakedData()
    {
        EditorUtil.DrawProperty(serializedObject, nameof(player.bakedData));

        if (data)
        {
            EditorGUILayout.Separator();

            CreateCachedEditor(data, typeof(BakedDataEditor), ref _bakedDataEditor);
            var editor = _bakedDataEditor as BakedDataEditor;
            if (editor) 
            {
                editor.OnInspectorGUI();
            }
        }
    }

    void DrawParameters()
    {
        EditorUtil.DrawProperty(serializedObject, nameof(player.audioSource));
        EditorUtil.DrawProperty(serializedObject, nameof(player.playOnAwake));
        EditorUtil.DrawProperty(serializedObject, nameof(player.playAudioSource));
        EditorUtil.DrawProperty(serializedObject, nameof(player.volume));
        EditorUtil.DrawProperty(serializedObject, nameof(player.timeOffset));

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.Space(10f, false);
        EditorUtil.DrawProperty(serializedObject, nameof(player.onLipSyncUpdate));
        EditorGUILayout.EndHorizontal();
    }

    void DrawPlay()
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button(" Play "))
        {
            player.Play();
        }
        if (GUILayout.Button(" Stop "))
        {
            player.Stop();
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Separator();
    }
}

}
