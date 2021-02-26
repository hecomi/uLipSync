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
        for (int i = 0; i < blendShape.blendShapes.Count; ++i)
        {
            DrawBlendShape(i);
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

    void DrawBlendShape(int index)
    {
        var bs = blendShape.blendShapes[index];

        if (EditorUtil.SimpleFoldout(bs.phoneme, true, "-BlendShape"))
        {
            ++EditorGUI.indentLevel;

            bs.phoneme = EditorGUILayout.TextField("Phoneme", bs.phoneme);
            DrawBlendShapePopup(bs);
            DrawFactor(bs);
            DrawRemoveUpDown(index);

            --EditorGUI.indentLevel;
        }
    }

    void DrawBlendShapePopup(uLipSyncBlendShape.BlendShapeInfo bs)
    {
        var newIndex = EditorGUILayout.Popup("BlendShape", bs.index + 1, GetBlendShapeArray());
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
        float weight = EditorGUILayout.Slider("Max Weight", bs.maxWeight, 0f, 2f);
        if (weight != bs.maxWeight)
        {
            Undo.RecordObject(target, "Change Max Weight");
            bs.maxWeight = weight;
        }
    }

    void DrawRemoveUpDown(int index)
    {
        EditorGUILayout.BeginHorizontal();

        GUILayout.FlexibleSpace();

        if (GUILayout.Button(" Remove ", EditorStyles.miniButtonLeft))
        {
            blendShape.RemoveBlendShape(index);
        }

        var blendShapes = blendShape.blendShapes;

        if (GUILayout.Button(" ▲ ", EditorStyles.miniButtonMid))
        {
            if (index >= 1)
            {
                var tmp = blendShapes[index];
                blendShapes[index] = blendShapes[index - 1];
                blendShapes[index - 1] = tmp;
            }
        }
        if (GUILayout.Button(" ▼ ", EditorStyles.miniButtonRight))
        {
            if (index < blendShapes.Count - 1)
            {
                var tmp = blendShapes[index];
                blendShapes[index] = blendShapes[index + 1];
                blendShapes[index + 1] = tmp;
            }
        }

        EditorGUILayout.EndHorizontal();
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
