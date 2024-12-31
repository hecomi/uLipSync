using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using uLipSync;
using UnityEngine;

///
/
///
public class BakedDataWizard : ScriptableWizard
{
    SerializedObject _serializedObject;

    [SerializeField]
    Profile profile;
    [SerializeField]
    string inputDirectory = "";
    [SerializeField]
    string outputDirectory = "";
    
    StringBuilder _message = new();
    
    [MenuItem("Window/uLipSync/Baked Data Wizard")]
    static void Open()
    {
        DisplayWizard<BakedDataWizard>("Baked Data Directory Mirror Generator", "Generate");
    }

    protected override bool DrawWizardGUI()
    {
        _serializedObject ??= new SerializedObject(this);

        _serializedObject.Update();

        EditorGUILayout.Separator();
        EditorGUILayout.LabelField("Baked Data Profile", EditorStyles.boldLabel);
        EditorUtil.DrawProperty(_serializedObject, nameof(profile));
        EditorGUILayout.Separator();

        // Input Directory
        EditorGUILayout.LabelField("Baked Data Input Directory", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        EditorUtil.DrawProperty(_serializedObject, nameof(inputDirectory));
        if (GUILayout.Button("Browse", GUILayout.Width(75)))
        {
            var path = EditorUtility.OpenFolderPanel("Baked Data Input Directory", Application.dataPath, "BakedDataInput");
            if (!string.IsNullOrEmpty(path))
            {
                inputDirectory = EditorUtil.GetAssetPath(path);
            }
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Separator();

        // Output Directory
        EditorGUILayout.LabelField("Baked Data Output Directory", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        EditorUtil.DrawProperty(_serializedObject, nameof(outputDirectory));
        if (GUILayout.Button("Browse", GUILayout.Width(75)))
        {
            var path = EditorUtility.OpenFolderPanel("Baked Data Output Directory", Application.dataPath, "OverrideOutput");
            if (!string.IsNullOrEmpty(path))
            {
                outputDirectory = EditorUtil.GetAssetPath(path);
            }
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Separator();

        DrawMessage();

        _serializedObject.ApplyModifiedProperties();

        return true;
    }

    private void OnWizardCreate()
    {
        // This method is called when the "Generate" button is clicked.
        OnWizardOtherButton();
    }

    void OnWizardOtherButton()
    {
        var dataList = new List<BakedData>();
        
        // Failsafe: Check the directories actually exist first
        if (!Directory.Exists(inputDirectory))
        {
            Debug.LogError($"Input directory does not exist: {inputDirectory}");
            return;
        }

        if (!Directory.Exists(outputDirectory))
        {
            Debug.LogError($"Output directory does not exist: {outputDirectory}");
            return;
        }

        // Step 1: Get a list of all audioclips in the input directory 
        var clipIDList = AssetDatabase.FindAssets("t:AudioClip", new[] { inputDirectory });
        //generate a list of all audioclip paths
        List<string> audioClipPaths = clipIDList.Select(AssetDatabase.GUIDToAssetPath).ToList();

        var audioClipCount = audioClipPaths.Count;
        var bakedDataGenerated = 0;

        // Step 2: For each audioclip, create a BakedData scriptable object with the defaultData set to the BakedData scriptable object
        foreach (var audioClipPath in audioClipPaths)
        {
            
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(audioClipPath);
            if (clip == null) continue;

            // Generate the relative path and the output path for the bakedDataObject
            var relativePath = audioClipPath[(inputDirectory.Length + 1)..];
            var bakedDataPath = Path.Combine(outputDirectory, relativePath);
            var bakedDataDirectory = Path.GetDirectoryName(bakedDataPath);
            var bakedDataAssetName = Path.GetFileNameWithoutExtension(bakedDataPath) + ".asset";
            if (bakedDataDirectory == null) continue;
            var bakedDataAssetPath = Path.Combine(bakedDataDirectory, bakedDataAssetName);

            // Ensure the output directory exists
            if (!Directory.Exists(bakedDataDirectory))
            {
                Directory.CreateDirectory(bakedDataDirectory);
            }

            // Check if the BakedData asset already exists
            var data = AssetDatabase.LoadAssetAtPath<BakedData>(bakedDataAssetPath);
            if (data == null)
            {
                //Create the baked data
                data = CreateInstance<BakedData>();
                data.profile = profile;
                data.audioClip = clip;
                data.name = clip.name;
                
                var editor = (BakedDataEditor)Editor.CreateEditor(data, typeof(BakedDataEditor));
                editor.Bake();
                
                AssetDatabase.CreateAsset(data, bakedDataAssetPath);
                bakedDataGenerated++;
                
                var progress = (float)dataList.Count / audioClipPaths.Count;
                var msg = $"Baking... {dataList.Count}/{audioClipPaths.Count}";
                EditorUtility.DisplayProgressBar("uLipSync", msg, progress);
            }
            else
            {
                // Update the existing BakedData asset
                data = CreateInstance<BakedData>();
                data.profile = profile;
                data.audioClip = clip;
                data.name = clip.name;
                
                var editor = (BakedDataEditor)Editor.CreateEditor(data, typeof(BakedDataEditor));
                editor.Bake();
                
                EditorUtility.SetDirty(data);
                var progress = (float)dataList.Count / audioClipPaths.Count;
                var msg = $"Baking... {dataList.Count}/{audioClipPaths.Count}";
                EditorUtility.DisplayProgressBar("uLipSync", msg, progress);
            }
        }
        
        EditorUtility.ClearProgressBar();

        // Save all changes to the asset database
        AssetDatabase.SaveAssets();

        // Step 5: Display a message with the number of BakedData scriptable objects found and the number of BakedData scriptable objects created
        _message.Clear();
        _message.AppendLine($"Found {audioClipCount} audioClips.");
        _message.AppendLine($"Created {bakedDataGenerated} BakedData scriptable objects.");
        Debug.Log(_message.ToString());

        Repaint(); // Repaint the GUI to update the message
    }

    void DrawMessage()
    {
        if (_message.Length == 0) return;

        EditorGUILayout.HelpBox(_message.ToString(), MessageType.Info);
    }
}
