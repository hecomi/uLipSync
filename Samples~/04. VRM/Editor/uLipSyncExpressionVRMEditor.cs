using UnityEditor;
using System.Linq;
#if USE_VRM10
using UniVRM10;
#endif

namespace uLipSync
{

#if USE_VRM10

[CustomEditor(typeof(uLipSyncExpressionVRM))]
public class uLipSyncExpressionVRMEditor : uLipSyncBlendShapeEditor
{
    uLipSyncExpressionVRM expression => target as uLipSyncExpressionVRM;
    Vrm10Instance vrm10Instance;

    void OnEnable()
    {
        vrm10Instance = expression.GetComponent<Vrm10Instance>();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        if (EditorUtil.Foldout("LipSync Update Method", true))
        {
            ++EditorGUI.indentLevel;
            EditorUtil.DrawProperty(serializedObject, nameof(expression.updateMethod));
            --EditorGUI.indentLevel;
            EditorGUILayout.Separator();
        }

        if (EditorUtil.Foldout("Parameters", true))
        {
            ++EditorGUI.indentLevel;
            DrawParameters();
            --EditorGUI.indentLevel;
            EditorGUILayout.Separator();
        }

        if (EditorUtil.Foldout("Blend Shapes", true))
        {
            ++EditorGUI.indentLevel;
            DrawBlendShapeReorderableList();
            --EditorGUI.indentLevel;
            EditorGUILayout.Separator();
        }

        serializedObject.ApplyModifiedProperties();
    }

    protected override string[] GetBlendShapeArray()
    {
        if (!vrm10Instance || !vrm10Instance.Vrm) return new string[] { "" };

        return vrm10Instance.Vrm.Expression.Clips.Select(x => x.Clip.name).ToArray();
    }
}

#else

[CustomEditor(typeof(uLipSyncExpressionVRM))]
public class uLipSyncExpressionVRMEditor : uLipSyncBlendShapeEditor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox("Please import VRM 1.0 package to use this component.", MessageType.Warning);
    }
}

#endif

}
