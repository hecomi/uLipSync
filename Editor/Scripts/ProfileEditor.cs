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

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        if (EditorUtil.SimpleFoldout("Import / Export JSON", false))
        {
            ++EditorGUI.indentLevel;
            DrawImportExport();
            --EditorGUI.indentLevel;

            EditorGUILayout.Separator();
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
            EditorUtil.DrawProperty(serializedObject, nameof(profile.maxError));
            profile.mfccDataCount = Mathf.Clamp(profile.mfccDataCount, 1, 256);
            profile.melFilterBankChannels = Mathf.Clamp(profile.melFilterBankChannels, 12, 48);
            profile.targetSampleRate = Mathf.Clamp(profile.targetSampleRate, 1000, 96000);
            profile.sampleCount = Mathf.ClosestPowerOfTwo(profile.sampleCount);
            --EditorGUI.indentLevel;

            EditorGUILayout.Separator();
        }

        if (EditorUtil.SimpleFoldout("MFCC", true))
        {
            ++EditorGUI.indentLevel;
            CalcMinMax();
            DrawMFCC(profile.a, "A");
            DrawMFCC(profile.i, "I");
            DrawMFCC(profile.u, "U");
            DrawMFCC(profile.e, "E");
            DrawMFCC(profile.o, "O");
            --EditorGUI.indentLevel;
        }

        serializedObject.ApplyModifiedProperties();
    }

    void CalcMinMax()
    {
        max = float.MinValue;
        min = float.MaxValue;
        foreach (var data in new MfccData[] { profile.a, profile.i, profile.u, profile.e, profile.o })
        {
            for (int j = 0; j < data.mfccList.Count; ++j)
            {
                var array = data.mfccList[j].array;
                for (int i = 0; i < array.Length; ++i)
                {
                    var x = array[i];
                    max = Mathf.Max(max, x);
                    min = Mathf.Min(min, x);
                }
            }
        }
    }

    void DrawMFCC(MfccData data, string name)
    {
        if (!EditorUtil.SimpleFoldout(name, true)) return;

        ++EditorGUI.indentLevel;

        foreach (var mfcc in data.mfccList)
        {
            EditorUtil.DrawMfcc(mfcc.array, max, min, 2f);
        }

        --EditorGUI.indentLevel;
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
