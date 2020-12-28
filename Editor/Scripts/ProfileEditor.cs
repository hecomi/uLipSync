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
        EditorGUILayout.LabelField("Formant", EditorStyles.boldLabel);
        ++EditorGUI.indentLevel;
        DrawFormant(ref profile.formantA, "A");
        DrawFormant(ref profile.formantI, "I");
        DrawFormant(ref profile.formantU, "U");
        DrawFormant(ref profile.formantE, "E");
        DrawFormant(ref profile.formantO, "O");
        EditorGUILayout.Space();
        DrawFormantResetButtons();
        --EditorGUI.indentLevel;

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Calibration", EditorStyles.boldLabel);
        DrawMicSelector();
        EditorGUILayout.Space();
        DrawAudioClipAndRecordAndStopButtons("A", ref profile.audioClipForCalibA);
        DrawAudioClipAndRecordAndStopButtons("I", ref profile.audioClipForCalibI);
        DrawAudioClipAndRecordAndStopButtons("U", ref profile.audioClipForCalibU);
        DrawAudioClipAndRecordAndStopButtons("E", ref profile.audioClipForCalibE);
        DrawAudioClipAndRecordAndStopButtons("O", ref profile.audioClipForCalibO);

        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Calibration", GUILayout.Width(120)))
        {

        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
        EditorUtil.DrawProperty(serializedObject, "maxError");
        EditorUtil.DrawProperty(serializedObject, "minLog10H");

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Visualizer", EditorStyles.boldLabel);
        EditorUtil.DrawFormants(profile);

        EditorGUILayout.LabelField("Tips", EditorStyles.boldLabel);
        DrawTips();
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

    void DrawMicSelector()
    {
        EditorUtil.DrawMicSelector(ref profile.micIndex);
    }

    void DrawAudioClipAndRecordAndStopButtons(string vowel, ref AudioClip clip)
    {
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField(vowel, GUILayout.Width(20));

        var newClip = EditorGUILayout.ObjectField(clip, typeof(AudioClip), false) as AudioClip;
        if (newClip != clip)
        {
            Undo.RecordObject(target, $"Changed audio clip");
            clip = newClip;
        }

        if (GUILayout.Button("Record", EditorStyles.miniButtonLeft)) 
        {
            StartRecord();
        }
        if (GUILayout.Button("Stop", EditorStyles.miniButtonRight)) 
        {
            StopRecord(ref clip, $"Vowel {vowel}");
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

    void StartRecord()
    {
        if (calibrator)
        {
            Debug.LogWarning("Already recording!");
            return;
        }

        var go = new GameObject("== Calibrator ==");
        calibrator = go.AddComponent<MicInputCalibrator>();
        calibrator.isAutoStart = false;
        calibrator.index = profile.micIndex;
        calibrator.StartRecord();
    }

    void StopRecord(ref AudioClip clip, string clipName)
    {
        if (!calibrator)
        {
            Debug.LogWarning("Not recording!");
            return;
        }

        var data = new float[calibrator.clip.samples];
        calibrator.clip.GetData(data, 0);
        clip = AudioClip.Create(clipName, calibrator.clip.samples, 1, calibrator.clip.frequency, false);
        clip.SetData(data, 0);
        calibrator.StopRecord();
        DestroyImmediate(calibrator.gameObject);
    }
}

}
