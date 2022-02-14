using UnityEngine;
using UnityEditor;
using System.Text;
using System.Collections.Generic;

namespace uLipSync
{

internal class AnimationCurveBindingData
{
    public EditorCurveBinding binding;
    public AnimationCurve curve;
}

public class AnimationWizard : ScriptableWizard
{
    SerializedObject _serializedObject;

    [SerializeField]
    Animator animator;

    [SerializeField]
    uLipSyncBlendShape blendShape;

    [SerializeField][NonReorderable] 
    BakedData[] bakedDataList = new BakedData[0];

    [SerializeField][Tooltip("Sampling interval at which keyframes are inserted")]
    float sampleFrameRate = 60f;

    [SerializeField][Range(0f, 20f)][Tooltip("Differential weight from the previous keyframe when inserting a keyframe")]
    float threshold = 0f;

    [SerializeField]
    string outputDirectory = "";

    [SerializeField]
    bool foldoutInput = true;

    [SerializeField]
    bool foldoutOutput = true;

    StringBuilder _message = new StringBuilder();

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

    string GetRelativeHierarchyPath(GameObject target, GameObject parent)
    {
        if (!target || !parent) return "";

        string path = "";
        var gameObj = target;
        while (gameObj && gameObj != parent)
        {
            if (!string.IsNullOrEmpty(path)) path = "/" + path;
            path = gameObj.name + path;
            gameObj = gameObj.transform.parent.gameObject;
        }

        if (gameObj != parent) return "";

        return path;
    }

    void OnWizardOtherButton()
    {
        if (!animator || !blendShape || bakedDataList.Length == 0) return;

        EditorUtility.DisplayProgressBar("uLipSync", "Create output directory...", 1f);
        EditorUtil.CreateOutputDirectory(outputDirectory);

        var bindingBase = new EditorCurveBinding();
        bindingBase.path = GetRelativeHierarchyPath(
            blendShape.skinnedMeshRenderer.gameObject, 
            animator.gameObject);
        bindingBase.type = typeof(SkinnedMeshRenderer);

        int i = 0;
        foreach (var bakedData in bakedDataList)
        {
            var progress = (float)(i++) / bakedDataList.Length;
            var msg = $"Create animation... {i}/{bakedDataList.Length}";
            EditorUtility.DisplayProgressBar("uLipSync", msg, progress);

            var clip = new AnimationClip();
            var dataList = new Dictionary<int, AnimationCurveBindingData>();

            foreach (var bs in blendShape.blendShapes)
            {
                if (bs.index < 0) continue;

                var mesh = blendShape.skinnedMeshRenderer.sharedMesh;
                var name = mesh.GetBlendShapeName(bs.index);
                var binding = bindingBase;
                binding.propertyName = "blendShape." + name;

                dataList.Add(bs.index, new AnimationCurveBindingData()
                {
                    binding = binding,
                    curve = new AnimationCurve(),
                });
            }

            blendShape.OnAnimationBakeStart();

            var preKeyWeights = new Dictionary<int, float>();
            var preFrameWeights = new Dictionary<int, float>();
            float dt = 1f / sampleFrameRate;
            for (float time = 0f; time <= bakedData.duration; time += dt)
            {
                var frame = bakedData.GetFrame(time);
                var info = BakedData.GetLipSyncInfo(frame);
                blendShape.OnAnimationBakeUpdate(info, dt);

                var nextWeights = blendShape.GetAnimationBakeBlendShapes();
                if (preFrameWeights.Count == 0) preFrameWeights = nextWeights;

                foreach (var kv in dataList)
                {
                    var index = kv.Key;
                    if (!nextWeights.ContainsKey(index)) continue;
                    var curve = kv.Value.curve;
                    var weight = nextWeights[index];

                    // Points where the weight changes from 0 or 1 are added regardless of the threshold.
                    var preFrameWeight = preFrameWeights[index];
                    if ((preFrameWeight < 1f && weight > 1f) ||
                        (preFrameWeight > 99f && weight < 99f))
                    {
                        curve.AddKey(time - dt, preFrameWeight);
                        preKeyWeights[index] = preFrameWeight;
                    }

                    if (!preKeyWeights.ContainsKey(index))
                    {
                        preKeyWeights.Add(index, -100f);
                    }
                    var preKeyWeight = preKeyWeights[index];

                    // Start points with a weight of 0 or 1 are added regardless of the threshold.
                    bool skipCheckingThreshold = 
                        (weight < 1f && preFrameWeight > 1f) ||
                        (weight > 99f && preFrameWeight < 99f);
                    if (Mathf.Abs(preKeyWeight - weight) > threshold || skipCheckingThreshold)
                    {
                        preKeyWeights[index] = weight;
                        curve.AddKey(time, weight);
                    }
                }

                preFrameWeights = nextWeights;
            }

            blendShape.OnAnimationBakeEnd();

            foreach (var kv in dataList)
            {
                var data = kv.Value;
                var binding = data.binding;
                var curve = data.curve;
                for (int j = 0; j < curve.length; ++j)
                {
                    var key = curve[j];
                    key.weightedMode = WeightedMode.Both;
                    key.inWeight = 1f / 3f;
                    if (j == 0 || j == curve.length - 1 || key.value < 1f || key.value > 99f)
                    {
                        key.inTangent = key.outTangent = 0f;
                    }
                    else
                    {
                        var prevKey = curve[j - 1];
                        var nextKey = curve[j + 1];
                        var a = (nextKey.value - prevKey.value) / (nextKey.time - prevKey.time);
                        key.inTangent = key.outTangent = a;
                    }
                    curve.MoveKey(j, key);
                }
                AnimationUtility.SetEditorCurve(clip, data.binding, data.curve);
            }

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
        EditorUtil.DrawProperty(_serializedObject, nameof(blendShape));
        EditorUtil.DrawProperty(_serializedObject, nameof(bakedDataList));
        EditorUtil.DrawProperty(_serializedObject, nameof(sampleFrameRate));
        EditorUtil.DrawProperty(_serializedObject, nameof(threshold));

        if (!animator) 
        {
            _message.Append("* Animator is not set.");
        }

        if (!blendShape) 
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