using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace uLipSync
{

public class BakedDataWizard : ScriptableWizard
{
    SerializedObject _serializedObject;

    enum InputType
    {
        List,
        Directory,
    }

    [SerializeField]
    Profile profile;

    [SerializeField]
    InputType inputType = InputType.List;

    [SerializeField]
#if UNITY_2020_2_OR_NEWER
    [NonReorderable] 
#endif
    List<AudioClip> audioClips = new List<AudioClip>();

    [SerializeField]
    string inputDirectory = "";

    [SerializeField]
    string outputDirectory = "";

    [SerializeField]
    bool foldoutInput = true;

    [SerializeField]
    bool foldoutOutput = true;

    StringBuilder _message = new StringBuilder();

    [MenuItem("Window/uLipSync/Baked Data Generator")]
    static void Open()
    {
        var path = DisplayWizard<BakedDataWizard>("uLipSync Baked Data Generator", "", "Generate");
    }

    protected override bool DrawWizardGUI()
    {
        _message.Clear();

        if (_serializedObject == null)
        {
            _serializedObject = new SerializedObject(this);
        }

        _serializedObject.Update();

        EditorGUILayout.Separator();

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
        if (!profile) return;

        var dataList = new List<BakedData>();

        foreach (var clip in audioClips)
        {
            var data = ScriptableObject.CreateInstance<BakedData>();
            data.profile = profile;
            data.audioClip = clip;
            data.name = clip.name;

            var editor = (BakedDataEditor)Editor.CreateEditor(data, typeof(BakedDataEditor));
            editor.Bake();

            dataList.Add(data);

            var progress = (float)dataList.Count / audioClips.Count;
            var msg = $"Baking... {dataList.Count}/{audioClips.Count}";
            EditorUtility.DisplayProgressBar("uLipSync", msg, progress);
        }

        EditorUtility.DisplayProgressBar("uLipSync", "Create output directory...", 1f);
        EditorUtil.CreateOutputDirectory(outputDirectory);

        int i = 0;
        foreach (var data in dataList)
        {
            var path = Path.Combine(outputDirectory, data.name + ".asset");
            AssetDatabase.CreateAsset(data, path);
            var progress = (float)(i++) / dataList.Count;
            var msg = $"Create asset... {i}/{dataList.Count}";
            EditorUtility.DisplayProgressBar("uLipSync", msg, progress);
        }

        EditorUtility.ClearProgressBar();
    }

    void DrawInput()
    {
        EditorUtil.DrawProperty(_serializedObject, nameof(profile));
        EditorUtil.DrawProperty(_serializedObject, nameof(inputType));

        if (inputType == InputType.List)
        {
            DrawListInput();
        }
        else
        {
            DrawDirectoryInput();
        }

        if (!profile)
        {
            if (_message.Length > 0) _message.AppendLine();
            _message.Append("* Profile is not set.");
        }

        if (audioClips.Count == 0)
        {
            if (_message.Length > 0) _message.AppendLine();
            _message.Append("* There is no input AudioClip.");
        }
    }

    void DrawOutput()
    {
        EditorGUILayout.BeginHorizontal();
        EditorUtil.DrawProperty(_serializedObject, nameof(outputDirectory));
        if (GUILayout.Button("...", GUILayout.Width(24)))
        {
            var path = EditorUtility.OpenFolderPanel("uLipSync Baked Data Output Directory", Application.dataPath, "BakedData");
            outputDirectory = EditorUtil.GetAssetPath(path);
        }
        EditorGUILayout.EndHorizontal();
    }

    void DrawListInput()
    {
        EditorUtil.DrawProperty(_serializedObject, nameof(audioClips));
    }

    void DrawDirectoryInput()
    {
        EditorGUILayout.BeginHorizontal();
        EditorUtil.DrawProperty(_serializedObject, nameof(inputDirectory));
        if (GUILayout.Button("...", GUILayout.Width(24)))
        {
            var defaultPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, inputDirectory);
            var path = EditorUtility.OpenFolderPanel("uLipSync Baked Data Input Directory", defaultPath, "");
            if (!string.IsNullOrEmpty(path))
            {
                inputDirectory = EditorUtil.GetAssetPath(path);
                audioClips.Clear();
            }
            var clipIdList = AssetDatabase.FindAssets("t:AudioClip", new string[] { inputDirectory });
            if (clipIdList.Length > 0)
            {
                foreach (var guid in clipIdList)
                {
                    var clipPath = AssetDatabase.GUIDToAssetPath(guid);
                    var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
                    audioClips.Add(clip);
                }
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorUtil.DrawProperty(_serializedObject, nameof(audioClips));
    }

    void DrawMessage()
    {
        if (_message.Length == 0) return;

        EditorGUILayout.HelpBox(_message.ToString(), MessageType.Warning);
    }
}

}