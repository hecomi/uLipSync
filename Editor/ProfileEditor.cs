using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
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
    ReorderableList reorderableList_ = null;

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
            DrawMfccReorderableList(showCalibration);
            --EditorGUI.indentLevel;
        }

        if (EditorUtil.SimpleFoldout("Advanced Parameters", true))
        {
            ++EditorGUI.indentLevel;
            EditorUtil.DrawProperty(serializedObject, nameof(profile.mfccDataCount));
            EditorUtil.DrawProperty(serializedObject, nameof(profile.melFilterBankChannels));
            EditorUtil.DrawProperty(serializedObject, nameof(profile.targetSampleRate));
            EditorUtil.DrawProperty(serializedObject, nameof(profile.sampleCount));
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

    void DrawMfccReorderableList(bool showCalibration)
    {
        if (reorderableList_ == null)
        {
            reorderableList_ = new ReorderableList(profile.mfccs, typeof(MfccData));
            reorderableList_.drawHeaderCallback = rect => 
            {
                rect.xMin -= EditorGUI.indentLevel * 12f;
                EditorGUI.LabelField(rect, "MFCCs");
            };
            reorderableList_.draggable = true;
            reorderableList_.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                DrawMFCC(rect, index, showCalibration);
            };
            reorderableList_.elementHeightCallback = index =>
            {
                return GetMFCCHeight(index);
            };
            reorderableList_.onAddCallback = index =>
            {
                profile.AddMfcc("New Phoneme");
            };
            reorderableList_.onRemoveCallback = list =>
            {
                profile.RemoveMfcc(list.index);
            };
        }

        EditorGUILayout.Separator();
        EditorGUILayout.BeginHorizontal();
        var indent = EditorGUI.indentLevel * 12f;
        EditorGUILayout.Space(indent, false);
        reorderableList_.DoLayoutList();
        EditorGUILayout.EndHorizontal();
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

    void DrawMFCC(Rect position, int index, bool showCalibration)
    {
        var data = profile.mfccs[index];

        position = EditorGUI.IndentedRect(position);
        position.xMin += 12;
        position.height = EditorUtil.lineHeightWithMargin;

        if (!EditorUtil.SimpleFoldout(position, data.name, true, "MfccData")) return;
        position.y += EditorUtil.lineHeightWithMargin;

        data.name = EditorGUI.TextField(position, "Phoneme", data.name);
        position.y += EditorUtil.lineHeightWithMargin;
        position.y += 5f;

        var mfccPos = new Rect(position);
        if (showCalibration)
        {
            mfccPos.xMax -= 60;
        }

        foreach (var mfcc in data.mfccCalibrationDataList)
        {
            EditorUtil.DrawMfcc(mfccPos, mfcc.array, max, min, 2f);
            mfccPos.y += 2f;
        }

        var calibButtonPos = new Rect(position);
        calibButtonPos.xMin = mfccPos.xMax + 8;
        calibButtonPos.height = 2f * data.mfccCalibrationDataList.Count;
        if (showCalibration)
        {
            var text = new GUIContent(" Calib ");
            var e = Event.current;
            if (e != null && calibButtonPos.Contains(e.mousePosition))
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

            var style = new GUIStyle(GUI.skin.button);
            style.fixedHeight = 20f;
            GUI.Button(calibButtonPos, text, style);
        }
    }

    float GetMFCCHeight(int index)
    {
        string name = "";
        float height = 20f;

        if (index < profile.mfccs.Count)
        {
            var data = profile.mfccs[index];
            name = data.name;

            if (EditorUtil.IsFoldOutOpened(name, true, "MfccData"))
            {
                height += 32f;
                height += data.mfccCalibrationDataList.Count * 2f;
            }
        }

        return height;
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
