using UnityEngine;
using UnityEditor;
using System.IO;

namespace uLipSync
{

[CustomEditor(typeof(Profile))]
public class ProfileEditor : Editor
{
    Profile profile { get { return target as Profile; } }
    public float min = 0f;
    public float max = 0f;
    public uLipSync uLipSync { get; set; }
    bool isCalibrating_ = false;

    public override void OnInspectorGUI()
    {
        Draw(false);
    }

    public void Draw(bool showCalibration)
    {
        serializedObject.Update();

        if (EditorUtil.SimpleFoldout("MFCC", true))
        {
            ++EditorGUI.indentLevel;
            CalcMinMax();
            for (int i = 0; i < profile.mfccs.Count; ++i)
            {
                DrawMFCC(i, showCalibration);
            }
            DrawAddMFCC();
            --EditorGUI.indentLevel;
        }

        if (EditorUtil.SimpleFoldout("Parameters", true))
        {
            ++EditorGUI.indentLevel;
            EditorUtil.DrawProperty(serializedObject, nameof(profile.mfccDataCount));
            EditorUtil.DrawProperty(serializedObject, nameof(profile.melFilterBankChannels));
            EditorUtil.DrawProperty(serializedObject, nameof(profile.targetSampleRate));
            EditorUtil.DrawProperty(serializedObject, nameof(profile.sampleCount));
            EditorUtil.DrawProperty(serializedObject, nameof(profile.minVolume));
            EditorUtil.DrawProperty(serializedObject, nameof(profile.maxVolume));
            profile.mfccDataCount = Mathf.Clamp(profile.mfccDataCount, 1, 256);
            profile.melFilterBankChannels = Mathf.Clamp(profile.melFilterBankChannels, 12, 48);
            profile.targetSampleRate = Mathf.Clamp(profile.targetSampleRate, 1000, 96000);
            profile.sampleCount = Mathf.ClosestPowerOfTwo(profile.sampleCount);
            --EditorGUI.indentLevel;

            EditorGUILayout.Separator();
        }

        if (EditorUtil.SimpleFoldout("Import / Export JSON", false))
        {
            ++EditorGUI.indentLevel;
            DrawImportExport();
            --EditorGUI.indentLevel;

            EditorGUILayout.Separator();
        }

        serializedObject.ApplyModifiedProperties();
    }

    void CalcMinMax()
    {
        max = float.MinValue;
        min = float.MaxValue;
        foreach (var data in profile.mfccs)
        {
            for (int j = 0; j < data.mfccCalibrationDataList.Count; ++j)
            {
                var array = data.mfccCalibrationDataList[j].array;
                for (int i = 0; i < array.Length; ++i)
                {
                    var x = array[i];
                    max = Mathf.Max(max, x);
                    min = Mathf.Min(min, x);
                }
            }
        }
    }

    void DrawMFCC(int index, bool showCalibration)
    {
        var data = profile.mfccs[index];
        
        if (!EditorUtil.SimpleFoldout(data.name, true)) return;

        ++EditorGUI.indentLevel;

        data.name = EditorGUILayout.TextField("Phoneme", data.name);
        EditorGUILayout.Separator();

        foreach (var mfcc in data.mfccCalibrationDataList)
        {
            EditorUtil.DrawMfcc(mfcc.array, max, min, 2f);
        }

        if (!uLipSync) showCalibration = false;

        EditorGUILayout.BeginHorizontal();
        {
            GUILayout.FlexibleSpace();

            if (GUILayout.Button(" Remove ", EditorStyles.miniButtonLeft))
            {
                profile.RemoveMfcc(index);
            }

            if (GUILayout.Button(" ▲ ", EditorStyles.miniButtonMid))
            {
                if (index >= 1)
                {
                    var tmp = profile.mfccs[index];
                    profile.mfccs[index] = profile.mfccs[index - 1];
                    profile.mfccs[index - 1] = tmp;
                }
            }

            if (GUILayout.Button(" ▼ ", showCalibration ? EditorStyles.miniButtonMid : EditorStyles.miniButtonRight))
            {
                if (index < profile.mfccs.Count - 1)
                {
                    var tmp = profile.mfccs[index];
                    profile.mfccs[index] = profile.mfccs[index + 1];
                    profile.mfccs[index + 1] = tmp;
                }
            }

            if (showCalibration)
            {
                // GUILayout.Button(" Calib ", EditorStyles.miniButtonRight);
                var text = new GUIContent(" Calib ");
                var rect = GUILayoutUtility.GetRect(text, EditorStyles.miniButtonRight);
                var e = Event.current;
                if (e != null && rect.Contains(e.mousePosition))
                {
                    if (e.type == EventType.MouseDown)
                    {
                        isCalibrating_ = true;
                    }
                    else if (e.type == EventType.MouseUp)
                    {
                        isCalibrating_ = false;
                    }

                    if (isCalibrating_)
                    {
                        uLipSync.RequestCalibration(index);
                    }

                    Repaint();
                }
                else if (e.isMouse)
                {
                    isCalibrating_ = false;
                }
                GUI.Button(rect, text);
            }
        }
        EditorGUILayout.EndHorizontal();

        --EditorGUI.indentLevel;
    }

    void DrawAddMFCC()
    {
        EditorGUILayout.Separator();

        EditorGUILayout.BeginHorizontal();

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("  Add Phoneme  "))
        {
            profile.AddMfcc("New Data");
        }

        EditorGUILayout.EndHorizontal();
    }

    void DrawImportExport()
    {
        EditorGUILayout.BeginHorizontal();
        profile.jsonPath = EditorGUILayout.TextField("File Path", profile.jsonPath);
        if (GUILayout.Button("...", EditorStyles.miniButton, GUILayout.Width(24)))
        {
            try
            {
                var dir = Path.GetDirectoryName(profile.jsonPath);
                var file = Path.GetFileName(profile.jsonPath);
                profile.jsonPath = EditorUtility.SaveFilePanel("Select Profile", dir, file, "json");
            }
            catch
            {
                profile.jsonPath = EditorUtility.SaveFilePanel("Select Profile", "", "profile", "json");
                profile.Export(profile.jsonPath);
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("  Import  ", EditorStyles.miniButtonLeft))
        {
            profile.Import(profile.jsonPath);
        }
        if (GUILayout.Button("  Export  ", EditorStyles.miniButtonRight))
        {
            profile.Export(profile.jsonPath);
        }
        EditorGUILayout.EndHorizontal();
    }
}

}
