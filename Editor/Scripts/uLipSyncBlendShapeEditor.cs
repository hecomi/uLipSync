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

        if (EditorUtil.Foldout("Skinned Mesh Renderer", true))
        {
            ++EditorGUI.indentLevel;
            DrawSkinnedMeshRenderer();
            --EditorGUI.indentLevel;
            EditorGUILayout.Separator();
        }

        if (EditorUtil.Foldout("Blend Shapes", true))
        {
            ++EditorGUI.indentLevel;
            DrawBlendShapes();
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

        serializedObject.ApplyModifiedProperties();
    }

    void DrawSkinnedMeshRenderer()
    {
        var findFromChildren = EditorUtil.EditorOnlyToggle("Find From Children", "uLipSyncBlendShape", true);
        EditorUtil.DrawProperty(serializedObject, nameof(findFromChildren));

        if (findFromChildren)
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
        foreach (var info in blendShape.blendShapes)
        {
            DrawBlendShape(info);
        }

        EditorGUILayout.Separator();

        DrawAddBlendShapeButtons();
    }

    void DrawAddBlendShapeButtons()
    {
        EditorGUILayout.BeginHorizontal();

        GUILayout.FlexibleSpace();
        if (GUILayout.Button(" Add New BlendShape "))
        {
            blendShape.AddBlendShapeInfo();
        }

        EditorGUILayout.EndHorizontal();
    }

    void DrawBlendShape(uLipSyncBlendShape.BlendShapeInfo bs)
    {
        EditorGUILayout.BeginHorizontal();

        bs.phenome = EditorGUILayout.TextField(bs.phenome, GUILayout.Width(64));
        DrawBlendShapePopup(bs);
        DrawFactor(bs);

        EditorGUILayout.EndHorizontal();
    }

    void DrawBlendShapePopup(uLipSyncBlendShape.BlendShapeInfo bs)
    {
        var newIndex = EditorGUILayout.Popup(bs.index + 1, GetBlendShapeArray(), GUILayout.MinWidth(200f));
        if (newIndex != bs.index + 1)
        {
            Undo.RecordObject(target, "Change Blend Shape");
            bs.index = newIndex - 1;
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

    void DrawFactor(uLipSyncBlendShape.BlendShapeInfo bs)
    {
        float factor = EditorGUILayout.Slider(bs.maxWeight, 0f, 2f);
        if (factor != bs.maxWeight)
        {
            Undo.RecordObject(target, "Change Blend Factor");
            bs.maxWeight = factor;
        }
    }

    void DrawParameters()
    {
        EditorUtil.DrawProperty(serializedObject, nameof(blendShape.applyVolume));
        EditorUtil.DrawProperty(serializedObject, nameof(blendShape.openDuration));
        EditorUtil.DrawProperty(serializedObject, nameof(blendShape.closeDuration));
        EditorUtil.DrawProperty(serializedObject, nameof(blendShape.vowelChangeDuration));
    }
}

}
