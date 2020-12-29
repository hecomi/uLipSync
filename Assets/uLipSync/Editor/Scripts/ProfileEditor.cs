using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace uLipSync
{

[CustomEditor(typeof(Profile))]
public class ProfileEditor : Editor
{
    Profile profile { get { return target as Profile; } }

    // Ref: http://bousure639.gjgd.net/Entry/164/
    Dictionary<Vowel, FormantPair> averageFormantMan = new Dictionary<Vowel, FormantPair>()
    {
        { Vowel.A, new FormantPair(775, 1163) },
        { Vowel.I, new FormantPair(263, 2263) },
        { Vowel.U, new FormantPair(363, 1300) },
        { Vowel.E, new FormantPair(475, 1738) },
        { Vowel.O, new FormantPair(550, 838) },
    };
    Dictionary<Vowel, FormantPair> averageFormantWoman = new Dictionary<Vowel, FormantPair>()
    {
        { Vowel.A, new FormantPair(888, 1363) },
        { Vowel.I, new FormantPair(325, 2725) },
        { Vowel.U, new FormantPair(375, 1675) },
        { Vowel.E, new FormantPair(483, 2317) },
        { Vowel.O, new FormantPair(483, 925) },
    };

    MicInputCalibrator calibrator;

    public override void OnInspectorGUI()
    {
        Draw(true, true);
    }

    public void Draw(bool drawTips, bool drawVisualizer)
    {
        serializedObject.Update();

        if (EditorUtil.SimpleFoldout("Formant", ref profile.foldOutFormant))
        {
            ++EditorGUI.indentLevel;
            DrawFormant(ref profile.formantA, "A");
            DrawFormant(ref profile.formantI, "I");
            DrawFormant(ref profile.formantU, "U");
            DrawFormant(ref profile.formantE, "E");
            DrawFormant(ref profile.formantO, "O");
            EditorGUILayout.Separator();
            DrawFormantResetButtons();
            --EditorGUI.indentLevel;

            EditorGUILayout.Separator();
        }

        if (drawTips)
        {
            if (EditorUtil.SimpleFoldout("Tips", ref profile.foldOutTips))
            {
                DrawTips();

                EditorGUILayout.Separator();
            }
        }

        if (drawVisualizer)
        {
            if (EditorUtil.SimpleFoldout("Visualizer", ref profile.foldOutVisualizer))
            {
                ++EditorGUI.indentLevel;
                EditorUtil.DrawFormants(profile);
                --EditorGUI.indentLevel;

                EditorGUILayout.Separator();
            }
        }

        profile.foldOutSettings = EditorGUILayout.Foldout(profile.foldOutSettings, "Settings", EditorStyles.foldoutHeader);
        if (profile.foldOutSettings)
        {
            ++EditorGUI.indentLevel;
            EditorUtil.DrawProperty(serializedObject, nameof(profile.maxError));
            EditorUtil.DrawProperty(serializedObject, nameof(profile.minLog10H));
            --EditorGUI.indentLevel;
        }

        serializedObject.ApplyModifiedProperties();
    }

    void DrawFormant(ref FormantPair formant, string name)
    {
        var f1 = EditorGUILayout.Slider($"{name} - F1", formant.f1, 0f, 4000f);
        if (f1 != formant.f1)
        {
            Undo.RecordObject(target, $"Changed {name} F1");
            if (f1 > formant.f2) f1 = formant.f2;
            formant.f1 = f1;
        }

        var f2 = EditorGUILayout.Slider($"{name} - F2", formant.f2, 0f, 4000f);
        if (f2 != formant.f2)
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
        if (GUILayout.Button("Reset (Man)", GUILayout.Width(120)))
        {
            ResetFormant(averageFormantMan);
        }
        if (GUILayout.Button("Reset (Woman)", GUILayout.Width(120)))
        {
            ResetFormant(averageFormantWoman);
        }
        EditorGUILayout.EndHorizontal();
    }

    void DrawTips()
    {
        var man = averageFormantMan;
        var woman = averageFormantWoman;
        EditorGUILayout.HelpBox(
            "Average formant frequencies:\n" +
            "----------------------------\n" +
            "Man\t\t\t\tWoman:\n" +
            $"A:\tF1: {man[Vowel.A].f1} / F2: {man[Vowel.A].f2}\t\tF1: {woman[Vowel.A].f1} / F2: {woman[Vowel.A].f2}\n" +
            $"I:\tF1: {man[Vowel.I].f1} / F2: {man[Vowel.I].f2}\t\tF1: {woman[Vowel.I].f1} / F2: {woman[Vowel.I].f2}\n" +
            $"U:\tF1: {man[Vowel.U].f1} / F2: {man[Vowel.U].f2}\t\tF1: {woman[Vowel.U].f1} / F2: {woman[Vowel.U].f2}\n" +
            $"E:\tF1: {man[Vowel.E].f1} / F2: {man[Vowel.E].f2}\t\tF1: {woman[Vowel.E].f1} / F2: {woman[Vowel.E].f2}\n" +
            $"O:\tF1: {man[Vowel.O].f1} / F2: {man[Vowel.O].f2}\t\tF1: {woman[Vowel.O].f1} / F2: {woman[Vowel.O].f2}",
            MessageType.Info);
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
