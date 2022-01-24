using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text;

namespace uLipSync
{

[CustomEditor(typeof(uLipSync))]
public class uLipSyncEditor : Editor
{
    uLipSync lipSync { get { return target as uLipSync; } }
    Profile profile { get { return lipSync.profile; } }

    Editor _profileEditor;
    List<float[]> _mfccHistory = new List<float[]>();

    float minVolume = 0f;
    float maxVolume = -100f;
    float _smoothVolume = 0f;
    StringBuilder _recognizedPhonemes = new StringBuilder();

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        if (EditorUtil.Foldout("Parameters", true))
        {
            ++EditorGUI.indentLevel;

            EditorUtil.DrawProperty(serializedObject, nameof(lipSync.outputSoundGain));
            EditorUtil.DrawProperty(serializedObject, nameof(lipSync.audioSourceProxy));

            EditorGUILayout.Separator();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.Space(10f, false);
            EditorUtil.DrawProperty(serializedObject, nameof(lipSync.onLipSyncUpdate));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Separator();

            --EditorGUI.indentLevel;
        }

        if (EditorUtil.Foldout("Profile", true))
        {
            ++EditorGUI.indentLevel;

            DrawProfile();

            EditorGUILayout.Separator();

            --EditorGUI.indentLevel;
        }

        if (EditorUtil.Foldout("Runtime Information", false))
        {
            ++EditorGUI.indentLevel;

            if (Application.isPlaying)
            {
                DrawRawVolume();
                DrawRMSVolume();
                DrawRecognition();
            }
            else
            {
                EditorGUILayout.HelpBox("Current RMS Volume is shown here in runtime.", MessageType.Info);
            }

            EditorGUILayout.Separator();

            if (Application.isPlaying)
            {
                DrawCurrentMfcc();
            }
            else
            {
                EditorGUILayout.HelpBox("Current MFCC is shown here in runtime.", MessageType.Info);
            }

            EditorGUILayout.Separator();

            EditorGUILayout.HelpBox("While runtime information is shown, FPS drop occurs due to the heavy editor process.", MessageType.Warning);
            Repaint();

            --EditorGUI.indentLevel;
        }

        serializedObject.ApplyModifiedProperties();
    }

    void DrawProfile()
    {
        EditorGUILayout.BeginHorizontal();
        {
            EditorUtil.DrawProperty(serializedObject, nameof(profile));
            if (GUILayout.Button("Create", EditorStyles.miniButtonRight, GUILayout.Width(60)))
            {
                lipSync.profile = EditorUtil.CreateAssetInRoot<Profile>($"{Common.assetName}-Profile-New");
            }
        }
        EditorGUILayout.EndHorizontal();

        CreateCachedEditor(profile, typeof(ProfileEditor), ref _profileEditor);
        var editor = _profileEditor as ProfileEditor;
        if (editor) 
        {
            editor.uLipSync = lipSync;
            editor.Draw(Application.isPlaying);
        }
    }

    void DrawRawVolume()
    {
        float volume = Mathf.Log10(lipSync.result.rawVolume);
        if (volume != Mathf.NegativeInfinity && volume != Mathf.Infinity)
        {
            _smoothVolume += (volume - _smoothVolume) * 0.9f;
            minVolume = Mathf.Min(minVolume, _smoothVolume);
            maxVolume = Mathf.Max(maxVolume, _smoothVolume);
        }

        EditorGUILayout.LabelField("Current Volume", _smoothVolume.ToString());
        EditorGUILayout.LabelField("Min Volume", minVolume.ToString());
        EditorGUILayout.LabelField("Max Volume", maxVolume.ToString());
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
        if (!lipSync.mfcc.IsCreated) return;

        var editor = _profileEditor as ProfileEditor;
        if (!editor) return;

        if (!EditorApplication.isPaused)
        {
            var array = new float[lipSync.mfcc.Length];
            lipSync.mfcc.CopyTo(array);
            _mfccHistory.Add(array);
            while (_mfccHistory.Count > 64) _mfccHistory.RemoveAt(0);
            while (_mfccHistory.Count < 64) _mfccHistory.Add(array);
        }

        foreach (var mfcc in _mfccHistory)
        {
            EditorUtil.DrawMfcc(mfcc, editor.max, editor.min, 1f);
        }
    }

    void DrawRecognition()
    {
        var phoeneme = lipSync.result.phoneme;
        if (Application.isPlaying &&
            !EditorApplication.isPaused &&
            lipSync.isActiveAndEnabled &&
            !string.IsNullOrEmpty(phoeneme))
        {
            _recognizedPhonemes.Append(lipSync.result.phoneme[0]);
            while (_recognizedPhonemes.Length > 64)
            {
                _recognizedPhonemes.Remove(0, 1);
            }
        }
        var arr = _recognizedPhonemes.ToString().ToCharArray();
        System.Array.Reverse(arr);
        var str = new string(arr);
        EditorGUILayout.LabelField("Recognized Phoeneme", str);
    }
}

}
