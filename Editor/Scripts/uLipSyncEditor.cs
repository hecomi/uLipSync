using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace uLipSync
{

[CustomEditor(typeof(uLipSync))]
public class uLipSync2Editor : Editor
{
    uLipSync lipSync { get { return target as uLipSync; } }
    Profile profile { get { return lipSync.profile; } }

    Editor profileEditor_;
    List<float[]> mfccHistory_ = new List<float[]>();

    public override void OnInspectorGUI()
    {
        if (EditorUtil.Foldout("Profile", true))
        {
            ++EditorGUI.indentLevel;

            EditorUtil.DrawProperty(serializedObject, nameof(lipSync.profile));

            if (EditorUtil.SimpleFoldout("Setting", false))
            {
                ++EditorGUI.indentLevel;

                CreateCachedEditor(lipSync.profile, typeof(ProfileEditor), ref profileEditor_);
                var editor = profileEditor_ as ProfileEditor;
                if (editor) editor.OnInspectorGUI();

                EditorGUILayout.Separator();

                --EditorGUI.indentLevel;
            }

            EditorGUILayout.Separator();

            --EditorGUI.indentLevel;
        }

        if (EditorUtil.Foldout("Callback", true))
        {
            ++EditorGUI.indentLevel;

            EditorUtil.DrawProperty(serializedObject, nameof(lipSync.onLipSyncUpdate));

            --EditorGUI.indentLevel;
        }

        if (EditorUtil.Foldout("Result", true))
        {
            ++EditorGUI.indentLevel;
            ++EditorGUI.indentLevel;
            ++EditorGUI.indentLevel;

            EditorGUILayout.Separator();

            DrawCurrentMfcc();

            EditorGUILayout.Separator();

            --EditorGUI.indentLevel;
            --EditorGUI.indentLevel;
            --EditorGUI.indentLevel;

            Repaint();
        }
    }

    void DrawCurrentMfcc()
    {
        if (!lipSync.mfcc.IsCreated || !lipSync.isMfccUpdated) return;

        var editor = profileEditor_ as ProfileEditor;
        if (!editor) return;

        if (!EditorApplication.isPaused)
        {
            var array = new float[lipSync.mfcc.Length];
            lipSync.mfcc.CopyTo(array);
            mfccHistory_.Add(array);
            while (mfccHistory_.Count > 64) mfccHistory_.RemoveAt(0);
            while (mfccHistory_.Count < 64) mfccHistory_.Add(array);
        }

        foreach (var mfcc in mfccHistory_)
        {
            EditorUtil.DrawMfcc(mfcc, editor.max, editor.min, 1f);
        }
    }
}

}
