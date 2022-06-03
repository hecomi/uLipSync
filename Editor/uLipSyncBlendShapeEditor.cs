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
    ReorderableList _reorderableList = null;

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

        if (EditorUtil.Foldout("Skinned Mesh Renderer", true))
        {
            ++EditorGUI.indentLevel;
            DrawSkinnedMeshRenderer();
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

    protected void DrawSkinnedMeshRenderer()
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

    protected void DrawSkinnedMeshRendererInChildren()
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

    protected void DrawBlendShapeReorderableList()
    {
        if (_reorderableList == null)
        {
            _reorderableList = new ReorderableList(blendShape.blendShapes, typeof(MfccData));
            _reorderableList.drawHeaderCallback = rect => 
            {
                rect.xMin -= EditorGUI.indentLevel * 12f;
                EditorGUI.LabelField(rect, "Phoneme - BlendShape Table");
            };
            _reorderableList.draggable = true;
            _reorderableList.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                DrawBlendShapeListItem(rect, index);
            };
            _reorderableList.elementHeightCallback = index =>
            {
                return GetBlendShapeListItemHeight(index);
            };
        }

        EditorGUILayout.Separator();
        EditorGUILayout.BeginHorizontal();
        var indent = EditorGUI.indentLevel * 12f;
        EditorGUILayout.Space(indent, false);
        EditorGUILayout.BeginVertical();
        _reorderableList.DoLayoutList();
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();
    }

    protected void DrawBlendShapeListItem(Rect rect, int index)
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

        float weight = EditorGUI.Slider(rect, "Max Weight", bs.maxWeight, 0f, 1f);
        if (weight != bs.maxWeight)
        {
            Undo.RecordObject(target, "Change Max Weight");
            bs.maxWeight = weight;
        }

        rect.y += singleLineHeight;
    }

    protected virtual float GetBlendShapeListItemHeight(int index)
    {
        return 64f;
    }

    protected virtual string[] GetBlendShapeArray()
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

    protected void DrawParameters()
    {
        Undo.RecordObject(target, "Change Volume Min/Max");
        EditorGUILayout.MinMaxSlider(
            "Volume Min/Max (Log10)",
            ref blendShape.minVolume, 
            ref blendShape.maxVolume,
            -5f, 0f);

        var rect = EditorGUILayout.GetControlRect(GUILayout.Height(0f));
        rect.x += EditorGUIUtility.labelWidth;
        rect.width -= EditorGUIUtility.labelWidth;
        rect.height = EditorGUIUtility.singleLineHeight;
        EditorGUILayout.BeginHorizontal();
        {
            var origColor = GUI.color;
            var style = new GUIStyle(GUI.skin.label);
            style.fontSize = 9;
            GUI.color = Color.gray;

            var minPos = rect;
            minPos.x -= 24f;
            minPos.y -= 12f;
            if (blendShape.minVolume > -4.5f)
            {
                minPos.x += (blendShape.minVolume + 5f) / 5f * rect.width - 30f;
            }
            EditorGUI.LabelField(minPos, $"{blendShape.minVolume.ToString("F2")}", style);

            var maxPos = rect;
            var maxX = (blendShape.maxVolume + 5f) / 5f * rect.width;
            maxPos.y -= 12f;
            if (maxX < maxPos.width - 48f)
            {
                maxPos.x += maxX;
            }
            else
            {
                maxPos.x += maxPos.width - 48f;
            }
            EditorGUI.LabelField(maxPos, $"{blendShape.maxVolume.ToString("F2")}", style);
            GUI.color = origColor;
        }
        EditorGUILayout.EndHorizontal();

        EditorUtil.DrawProperty(serializedObject, nameof(blendShape.smoothness));
    }
}

}
