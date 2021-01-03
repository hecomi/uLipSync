using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace uLipSync
{

[CustomEditor(typeof(Profile))]
public class ProfileEditor : Editor
{
    Profile profile { get { return target as Profile; } }

    public override void OnInspectorGUI()
    {
        Draw(true, true);
    }

    public void Draw(bool drawTips, bool drawVisualizer)
    {
        serializedObject.Update();

        bool isDefaultAsset = 
            profile.name == Common.defaultProfileMan ||
            profile.name == Common.defaultProfileWoman;

        if (EditorUtil.SimpleFoldout("Formant", true))
        {
            ++EditorGUI.indentLevel;
            DrawFormant(ref profile.formantA, "A", isDefaultAsset);
            DrawFormant(ref profile.formantI, "I", isDefaultAsset);
            DrawFormant(ref profile.formantU, "U", isDefaultAsset);
            DrawFormant(ref profile.formantE, "E", isDefaultAsset);
            DrawFormant(ref profile.formantO, "O", isDefaultAsset);
            EditorGUILayout.Separator();
            if (isDefaultAsset)
            {
                EditorGUILayout.HelpBox("Cannot change parameters in a default asset.", MessageType.None);
            }
            else
            {
                DrawFormantResetButtons();

                if (drawTips)
                {
                    if (EditorUtil.SimpleFoldout("Tips", true))
                    {
                        DrawTips();

                        EditorGUILayout.Separator();
                    }
                }
            }
            --EditorGUI.indentLevel;

            EditorGUILayout.Separator();
        }

        if (drawVisualizer)
        {
            if (EditorUtil.SimpleFoldout("Visualizer", true))
            {
                ++EditorGUI.indentLevel;
                EditorUtil.DrawFormants(profile);
                --EditorGUI.indentLevel;

                EditorGUILayout.Separator();
            }
        }

        if (EditorUtil.SimpleFoldout("Settings", true))
        {
            ++EditorGUI.indentLevel;
            EditorUtil.DrawProperty(serializedObject, nameof(profile.useErrorRange));
            EditorUtil.DrawProperty(serializedObject, nameof(profile.maxErrorRange));
            EditorUtil.DrawProperty(serializedObject, nameof(profile.minLog10H));
            --EditorGUI.indentLevel;
        }

        serializedObject.ApplyModifiedProperties();
    }

    void DrawFormant(ref FormantPair formant, string name, bool isDefaultAsset)
    {
        var f1 = EditorGUILayout.Slider($"{name} - F1", formant.f1, 0f, 4000f);
        if (f1 != formant.f1 && !isDefaultAsset)
        {
            Undo.RecordObject(target, $"Changed {name} F1");
            if (f1 > formant.f2) f1 = formant.f2;
            formant.f1 = f1;
        }

        var f2 = EditorGUILayout.Slider($"{name} - F2", formant.f2, 0f, 4000f);
        if (f2 != formant.f2 && !isDefaultAsset)
        {
            Undo.RecordObject(target, $"Changed {name} F2");
            if (f2 < formant.f1) f2 = formant.f1;
            formant.f2 = f2;
        }
    }

    void DrawFormantResetButtons()
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Reset (Man)", EditorStyles.miniButtonLeft, GUILayout.Width(120)))
        {
            ResetFormant(Common.averageFormantMan);
        }
        if (GUILayout.Button("Reset (Woman)", EditorStyles.miniButtonRight, GUILayout.Width(120)))
        {
            ResetFormant(Common.averageFormantWoman);
        }
        EditorGUILayout.EndHorizontal();
    }

    void DrawTips()
    {
        var man = Common.averageFormantMan;
        var woman = Common.averageFormantWoman;
        EditorGUILayout.HelpBox(
            " Average formant frequencies:\n" +
            " ----------------------------\n" +
            " Man\t\t\t\tWoman:\n" +
            $" A:\tF1: {man[Vowel.A].f1} / F2: {man[Vowel.A].f2}\t\tF1: {woman[Vowel.A].f1} / F2: {woman[Vowel.A].f2}\n" +
            $" I:\tF1: {man[Vowel.I].f1} / F2: {man[Vowel.I].f2}\t\tF1: {woman[Vowel.I].f1} / F2: {woman[Vowel.I].f2}\n" +
            $" U:\tF1: {man[Vowel.U].f1} / F2: {man[Vowel.U].f2}\t\tF1: {woman[Vowel.U].f1} / F2: {woman[Vowel.U].f2}\n" +
            $" E:\tF1: {man[Vowel.E].f1} / F2: {man[Vowel.E].f2}\t\tF1: {woman[Vowel.E].f1} / F2: {woman[Vowel.E].f2}\n" +
            $" O:\tF1: {man[Vowel.O].f1} / F2: {man[Vowel.O].f2}\t\tF1: {woman[Vowel.O].f1} / F2: {woman[Vowel.O].f2}",
            MessageType.None);
    }

    void ResetFormant(Dictionary<Vowel, FormantPair> formant)
    {
        Undo.RecordObject(target, "Reset formant");
        profile.formantA = formant[Vowel.A];
        profile.formantI = formant[Vowel.I];
        profile.formantU = formant[Vowel.U];
        profile.formantE = formant[Vowel.E];
        profile.formantO = formant[Vowel.O];
    }
}

}
