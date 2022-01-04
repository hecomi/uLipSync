using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Linq;
using System.Collections.Generic;

namespace uLipSync
{

[CustomEditor(typeof(uLipSyncBlendShape))]
public class uLipSyncBlendShapeEditor : Editor
{
    uLipSyncBlendShape blendShape { get { return target as uLipSyncBlendShape; } }
    ReorderableList reorderableList_ = null;

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
            //DrawBlendShapes();
            DrawBlendShapeReorderableList();
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
            DrawSkinnedMeshRendererInChildren();
        }
        else
        {
            EditorUtil.DrawProperty(serializedObject, nameof(blendShape.skinnedMeshRenderer));
        }
    }

    void DrawSkinnedMeshRendererInChildren()
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

    void DrawBlendShapeReorderableList()
    {
        if (reorderableList_ == null)
        {
            reorderableList_ = new ReorderableList(blendShape.blendShapes, typeof(MfccData));
            reorderableList_.drawHeaderCallback = rect => 
            {
                rect.xMin -= EditorGUI.indentLevel * 12f;
                EditorGUI.LabelField(rect, "MFCCs");
            };
            reorderableList_.draggable = true;
            reorderableList_.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                DrawBlendShapeListItem(rect, index);
            };
            reorderableList_.elementHeightCallback = index =>
            {
                return GetBlendShapeListItemHeight(index);
            };
        }

        EditorGUILayout.Separator();
        EditorGUILayout.BeginHorizontal();
        var indent = EditorGUI.indentLevel * 12f;
        EditorGUILayout.Space(indent, false);
        reorderableList_.DoLayoutList();
        EditorGUILayout.EndHorizontal();
    }

    void DrawBlendShapeListItem(Rect rect, int index)
    {
        rect.y += 2f;
        rect.height = EditorGUIUtility.singleLineHeight;

        var bs = blendShape.blendShapes[index];
        float singleLineHeight = 
            EditorGUIUtility.singleLineHeight + 
            EditorGUIUtility.standardVerticalSpacing;

        bs.phoneme = EditorGUI.TextField(rect, "Phoneme", bs.phoneme);

        rect.y += singleLineHeight;

        var newIndex = EditorGUI.Popup(rect, "BlendShape", bs.index + 1, GetBlendShapeArray());
        if (newIndex != bs.index + 1)
        {
            Undo.RecordObject(target, "Change Blend Shape");
            bs.index = newIndex - 1;
        }

        rect.y += singleLineHeight;

        float weight = EditorGUI.Slider(rect, "Max Weight", bs.maxWeight, 0f, 2f);
        if (weight != bs.maxWeight)
        {
            Undo.RecordObject(target, "Change Max Weight");
            bs.maxWeight = weight;
        }

        rect.y += singleLineHeight;
    }

    float GetBlendShapeListItemHeight(int index)
    {
        return 64f;
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

    void DrawParameters()
    {
        EditorUtil.DrawProperty(serializedObject, nameof(blendShape.applyVolume));
        EditorUtil.DrawProperty(serializedObject, nameof(blendShape.openDuration));
        EditorUtil.DrawProperty(serializedObject, nameof(blendShape.closeDuration));
        EditorUtil.DrawProperty(serializedObject, nameof(blendShape.vowelChangeDuration));
    }
}

}
