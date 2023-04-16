using System;
using UnityEngine;
using UnityEditor;
using System.Text;

namespace uLipSync
{

public class AnimationWizard : ScriptableWizard
{
    SerializedObject _serializedObject;

    [SerializeField]
    Animator animator;

    [SerializeField]
    AnimationBakableMonoBehaviour animBake;

    [SerializeField]
#if UNITY_2020_2_OR_NEWER
    [NonReorderable] 
#endif
    BakedData[] bakedDataList = Array.Empty<BakedData>();

    [SerializeField][Tooltip("Sampling interval at which keyframes are inserted")]
    float sampleFrameRate = 60f;

    [SerializeField][Range(0f, 1f)][Tooltip("Differential weight from the previous keyframe when inserting a keyframe")]
    float threshold = 0f;

    [SerializeField]
    string outputDirectory = "";

    [SerializeField]
    bool foldoutInput = true;

    [SerializeField]
    bool foldoutOutput = true;

    readonly StringBuilder _message = new StringBuilder();

    [MenuItem("Window/uLipSync/Animation Clip Generator")]
    static void Open()
    {
        var path = DisplayWizard<AnimationWizard>("uLipSync Animation Clip Generator", "", "Generate");
    }

    protected override bool DrawWizardGUI()
    {
        _message.Clear();

        if (_serializedObject == null)
        {
            _serializedObject = new SerializedObject(this);
        }

        _serializedObject.Update();

        var style = new GUIStyle(EditorStyles.foldoutHeader);
        style.fixedWidth = EditorGUIUtility.currentViewWidth;
        foldoutInput = EditorGUILayout.Foldout(foldoutInput, "Input", style);
        if (foldoutInput)
        {
            EditorGUILayout.Separator();
            ++EditorGUI.indentLevel;
            DrawInput();
            --EditorGUI.indentLevel;
            EditorGUILayout.Separator();
        }

        foldoutOutput = EditorGUILayout.Foldout(foldoutOutput, "Output", style);
        if (foldoutOutput)
        {
            EditorGUILayout.Separator();
            ++EditorGUI.indentLevel;
            DrawOutput();
            --EditorGUI.indentLevel;
            EditorGUILayout.Separator();
        }

        DrawMessage();

        _serializedObject.ApplyModifiedProperties();

        return true;
    }

    void OnWizardCreate()
    {
    }

    void OnWizardOtherButton()
    {
        if (!animator || animBake == null || bakedDataList.Length == 0) return;

        if (string.IsNullOrEmpty(outputDirectory))
        {
            outputDirectory = "Assets";
        }

        EditorUtility.DisplayProgressBar("uLipSync", "Create output directory...", 1f);
        EditorUtil.CreateOutputDirectory(outputDirectory);

        int bakedDataIndex = 0;
        foreach (var bakedData in bakedDataList)
        {
            var progress = (float)(bakedDataIndex++) / bakedDataList.Length;
            var msg = $"Create animation... {bakedDataIndex}/{bakedDataList.Length}";
            EditorUtility.DisplayProgressBar("uLipSync", msg, progress);

            var clip = AnimationUtil.CreateAnimationClipFromBakedData(
                new CreateAnimationClipFromBakedDataInput 
                {
                    gameObject = animator.gameObject,
                    animBake = animBake,
                    bakedData = bakedData,
                    sampleFrameRate = sampleFrameRate,
                    threshold = threshold,
                });


            var path = $"{outputDirectory}/{bakedData.name}.anim";
            path = AssetDatabase.GenerateUniqueAssetPath(path);
            AssetDatabase.CreateAsset(clip, path);
        }

        EditorUtility.ClearProgressBar();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    void DrawInput()
    {
        DrawProperties();
    }

    void DrawProperties()
    {
        EditorUtil.DrawProperty(_serializedObject, nameof(animator));
        EditorUtil.DrawProperty(_serializedObject, nameof(animBake));
        EditorUtil.DrawProperty(_serializedObject, nameof(bakedDataList));
        EditorUtil.DrawProperty(_serializedObject, nameof(sampleFrameRate));
        EditorUtil.DrawProperty(_serializedObject, nameof(threshold));

        if (!animator) 
        {
            _message.Append("* Animator is not set.");
        }

        if (animBake == null) 
        {
            _message.AppendLine(); 
            _message.Append("* Blend Shape is not set.");
        }

        if (bakedDataList.Length == 0) 
        {
            _message.AppendLine(); 
            _message.Append("* Baked Data List is not set.");
        }
    }

    void DrawOutput()
    {
        EditorGUILayout.BeginHorizontal();
        EditorUtil.DrawProperty(_serializedObject, nameof(outputDirectory));
        if (GUILayout.Button("...", GUILayout.Width(24)))
        {
            var path = EditorUtility.OpenFolderPanel("uLipSync AnimationClip Output Directory", Application.dataPath, "AnimClip");
            outputDirectory = EditorUtil.GetAssetPath(path);
        }
        EditorGUILayout.EndHorizontal();
    }

    void DrawMessage()
    {
        if (_message.Length == 0) return;

        EditorGUILayout.HelpBox(_message.ToString(), MessageType.Warning);
    }
}

}