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
        serializedObject.Update();

        if (EditorUtil.Foldout("Profile", true))
        {
            ++EditorGUI.indentLevel;

            EditorUtil.DrawProperty(serializedObject, nameof(profile));

            if (EditorUtil.SimpleFoldout("Setting", false))
            {
                ++EditorGUI.indentLevel;

                CreateCachedEditor(profile, typeof(ProfileEditor), ref profileEditor_);
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

        if (EditorUtil.Foldout("Runtime Information", false))
        {
            ++EditorGUI.indentLevel;

            bool shouldRepaint = false;

            EditorGUILayout.Separator();

            if (EditorUtil.SimpleFoldout("Volume", true)) 
            {
                ++EditorGUI.indentLevel;
                if (Application.isPlaying)
                {
                    DrawRMSVolume();
                    shouldRepaint = true;
                }
                else
                {
                    EditorGUILayout.HelpBox("Current RMS Volume is shown here in runtime.", MessageType.Info);
                }
                --EditorGUI.indentLevel;

                EditorGUILayout.Separator();
            }

            if (EditorUtil.SimpleFoldout("MFCC", true)) 
            {
                ++EditorGUI.indentLevel;
                if (Application.isPlaying)
                {
                    ++EditorGUI.indentLevel;
                    DrawCurrentMfcc();
                    shouldRepaint = true;
                    --EditorGUI.indentLevel;
                }
                else
                {
                    EditorGUILayout.HelpBox("Current MFCC is shown here in runtime.", MessageType.Info);
                }
                --EditorGUI.indentLevel;

                EditorGUILayout.Separator();
            }

            if (shouldRepaint)
            {
                EditorGUILayout.HelpBox("While runtime information is shown, FPS drop occurs due to the heavy editor process.", MessageType.Info);
                Repaint();
            }

            --EditorGUI.indentLevel;

            EditorGUILayout.Separator();
        }

        serializedObject.ApplyModifiedProperties();
    }

    void DrawRMSVolume()
    {
        EditorGUILayout.BeginHorizontal();
        {
            EditorGUILayout.PrefixLabel("Normalized Volume");
            var rect = EditorGUILayout.GetControlRect(true);
            rect.y += rect.height * 0.3f;
            rect.height *= 0.4f;
            Handles.DrawSolidRectangleWithOutline(rect, new Color(0f, 0f, 0f, 0.2f), new Color(0f, 0f, 0f, 0.5f));
            rect.width -= 2;
            rect.width *= Mathf.Clamp(lipSync.result.volume, 0f, 1f);
            rect.height -= 2;
            rect.y += 1;
            rect.x += 1;
            Handles.DrawSolidRectangleWithOutline(rect, Color.green, new Color(0f, 0f, 0f, 0f));
        }
        EditorGUILayout.EndHorizontal();
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
