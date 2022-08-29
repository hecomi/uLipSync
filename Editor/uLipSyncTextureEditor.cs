using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Linq;
using System.Collections.Generic;

namespace uLipSync
{

[CustomEditor(typeof(uLipSyncTexture))]
public class uLipSyncTextureEditor : Editor
{
    uLipSyncTexture texture { get { return target as uLipSyncTexture; } }
    ReorderableList _reorderableList = null;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        if (EditorUtil.Foldout("LipSync Update Method", true))
        {
            ++EditorGUI.indentLevel;
            EditorUtil.DrawProperty(serializedObject, nameof(texture.updateMethod));
            --EditorGUI.indentLevel;
            EditorGUILayout.Separator();
        }

        if (EditorUtil.Foldout("Renderer", true))
        {
            ++EditorGUI.indentLevel;
            DrawRenderer();
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

        if (EditorUtil.Foldout("Textures", true))
        {
            ++EditorGUI.indentLevel;
            DrawTextureReorderableList();
            --EditorGUI.indentLevel;
            EditorGUILayout.Separator();
        }

        serializedObject.ApplyModifiedProperties();
    }

    protected void DrawRenderer()
    {
        EditorUtil.DrawProperty(serializedObject, nameof(texture.targetRenderer));
    }

    protected void DrawTextureReorderableList()
    {
        if (_reorderableList == null)
        {
            _reorderableList = new ReorderableList(texture.textures, typeof(MfccData));
            _reorderableList.drawHeaderCallback = rect => 
            {
                rect.xMin -= EditorGUI.indentLevel * 12f;
                EditorGUI.LabelField(rect, "Phoneme - Texture Table");
            };
            _reorderableList.draggable = true;
            _reorderableList.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                DrawTextureListItem(rect, index);
            };
            _reorderableList.elementHeightCallback = index =>
            {
                return GetTextureListItemHeight(index);
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

    protected void DrawTextureListItem(Rect rect, int index)
    {
        rect.y += 2f;
        rect.height = EditorGUIUtility.singleLineHeight;

        var tex = texture.textures[index];
        float singleLineHeight = 
            EditorGUIUtility.singleLineHeight + 
            EditorGUIUtility.standardVerticalSpacing;

        var phonemeLabel = string.IsNullOrEmpty(tex.phoneme) ? "Default" : "Phoneme";
        tex.phoneme = EditorGUI.TextField(rect, phonemeLabel, tex.phoneme);

        rect.y += singleLineHeight;

        var newTexture = (Texture)EditorGUI.ObjectField(rect, "Texture", tex.texture, typeof(Texture), true);
        if (newTexture != tex.texture)
        {
            Undo.RecordObject(target, "Change Texture");
            tex.texture = newTexture;
        }

        rect.y += singleLineHeight;

        var uvScale = EditorGUI.Vector2Field(rect, "UV Scale", tex.uvScale);
        if (uvScale != tex.uvScale)
        {
            Undo.RecordObject(target, "Change UV Scale");
            tex.uvScale = uvScale;
        }

        rect.y += singleLineHeight;

        var uvOffset = EditorGUI.Vector2Field(rect, "UV Offset", tex.uvOffset);
        if (uvOffset != tex.uvOffset)
        {
            Undo.RecordObject(target, "Change UV Offset");
            tex.uvOffset = uvOffset;
        }

        rect.y += singleLineHeight;
    }

    protected virtual float GetTextureListItemHeight(int index)
    {
        return 84f;
    }

    protected void DrawParameters()
    {
        EditorUtil.DrawProperty(serializedObject, nameof(texture.minVolume));
        EditorUtil.DrawProperty(serializedObject, nameof(texture.minDuration));
    }
}

}
