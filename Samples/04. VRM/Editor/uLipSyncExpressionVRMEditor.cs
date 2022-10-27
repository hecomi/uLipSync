using UnityEditor;
using UnityEditorInternal;
using System.Linq;
using UniVRM10;
using VRM;

namespace uLipSync
{

[CustomEditor(typeof(uLipSyncExpressionVRM))]
public class uLipSyncExpressionVRMEditor : uLipSyncBlendShapeEditor
{
    uLipSyncExpressionVRM expression { get { return target as uLipSyncExpressionVRM; } }
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

}
