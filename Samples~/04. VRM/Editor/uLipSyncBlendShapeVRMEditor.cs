using UnityEditor;
using UnityEditorInternal;
using System.Linq;
using VRM;

namespace uLipSync
{

[CustomEditor(typeof(uLipSyncBlendShapeVRM))]
public class uLipSyncBlendShapeVRMEditor : uLipSyncBlendShapeEditor
{
    uLipSyncBlendShapeVRM blendShape { get { return target as uLipSyncBlendShapeVRM; } }
    BlendShapeAvatar _avatar = null;

    void OnEnable()
    {
        var proxy = blendShape.GetComponent<VRMBlendShapeProxy>();
        if (proxy)
        {
            _avatar = proxy.BlendShapeAvatar;
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        if (EditorUtil.Foldout("LipSync Update Method", true))
        {
            ++EditorGUI.indentLevel;
            EditorUtil.DrawProperty(serializedObject, nameof(blendShape.updateMethod));
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
        if (!_avatar) return new string[] { "" };

        return _avatar.Clips.Select(x => x.BlendShapeName).ToArray();
    }
}

}
