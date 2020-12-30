using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;

namespace uLipSync
{

[CustomEditor(typeof(uLipSyncBlendShape))]
public class uLipSyncBlendShapeEditor : Editor
{
    uLipSyncBlendShape blendShape { get { return target as uLipSyncBlendShape; } }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawSkinnedMeshRenderer();
        DrawBlendShapes();

        serializedObject.ApplyModifiedProperties();
    }

    void DrawSkinnedMeshRenderer()
    {
        EditorUtil.DrawProperty(serializedObject, nameof(blendShape.findFromChildren));

        if (blendShape.findFromChildren)
        {
            var skinnedMeshRenderers = blendShape.GetComponentsInChildren<SkinnedMeshRenderer>();
            int index = 0;
            for (int i = 0; i < skinnedMeshRenderers.Length; ++i)
            {
                var skinnedMeshRenderer = skinnedMeshRenderers[i];
                if (skinnedMeshRenderer == blendShape.skinnedMeshRenderer)
                {
                    index = i;
                    break;
                }
            }
            var names = skinnedMeshRenderers.Select(x => x.gameObject.name).ToArray();
            var newIndex = EditorGUILayout.Popup("Skinned Mesh Renderer", index, names);
            if (newIndex != index)
            {
                Undo.RecordObject(target, "Change Skinned Mesh Renderer");
                blendShape.skinnedMeshRenderer = skinnedMeshRenderers[newIndex];
            }
        }
        else
        {
            EditorUtil.DrawProperty(serializedObject, nameof(blendShape.skinnedMeshRenderer));
        }
    }

    void DrawBlendShapes()
    {
        for (int i = (int)Vowel.A; i <= (int)Vowel.None && i < blendShape.blendShapeList.Count; ++i)
        {
            var info = blendShape.blendShapeList[i];
            DrawBlendShape((Vowel)i, info);
        }
    }

    void DrawBlendShape(Vowel vowel, BlendShapeInfo info)
    {
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.PrefixLabel(vowel.ToString());
        DrawBlendShapePopup(info);
        DrawFactor(info);

        EditorGUILayout.EndHorizontal();
    }

    void DrawBlendShapePopup(BlendShapeInfo info)
    {
        var newIndex = EditorGUILayout.Popup(info.index + 1, GetBlendShapeArray(), GUILayout.MinWidth(200f));
        if (newIndex != info.index + 1)
        {
            Undo.RecordObject(target, "Change Blend Shape");
            info.index = newIndex - 1;
        }
    }

    string[] GetBlendShapeArray()
    {
        if (blendShape.skinnedMeshRenderer == null)
        {
            return new string[0];
        }
        
        var mesh = blendShape.skinnedMeshRenderer.sharedMesh;
        var names = new List<string>();
        names.Add("None");
        for (int i = 0; i < mesh.blendShapeCount; ++i)
        {
            var name = mesh.GetBlendShapeName(i);
            names.Add(name);
        }
        return names.ToArray();
    }

    void DrawFactor(BlendShapeInfo info)
    {
        float factor = EditorGUILayout.Slider(info.factor, 0f, 2f);
        if (factor != info.factor)
        {
            Undo.RecordObject(target, "Change Blend Factor");
            info.factor = factor;
        }
    }
}

}
