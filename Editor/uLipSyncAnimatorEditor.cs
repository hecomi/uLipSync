using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Linq;
using System.Collections.Generic;

namespace uLipSync
{

[CustomEditor(typeof(uLipSyncAnimator))]
public class uLipSyncAnimatorEditor : Editor
{
    uLipSyncAnimator anim => target as uLipSyncAnimator;
    ReorderableList _reorderableList = null;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        if (EditorUtil.Foldout("LipSync Update Method", true))
        {
            ++EditorGUI.indentLevel;
            EditorUtil.DrawProperty(serializedObject, nameof(anim.updateMethod));
            --EditorGUI.indentLevel;
            EditorGUILayout.Separator();
        }

        if (EditorUtil.Foldout("Animator", true))
        {
            ++EditorGUI.indentLevel;
            DrawAnimator();
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

        if (EditorUtil.Foldout("Animator Controller Parameters", true))
        {
            ++EditorGUI.indentLevel;
            if (anim.animator != null && anim.animator.isActiveAndEnabled)
            {
                DrawAnimatorReorderableList();
            }
            else
            {
                EditorGUILayout.HelpBox("Animator is not available! To edit parameters open the prefab or have game object in scene.", MessageType.Warning);
            }
            --EditorGUI.indentLevel;
            EditorGUILayout.Separator();
        }

        serializedObject.ApplyModifiedProperties();
    }

    protected void DrawAnimator()
    {
        var findFromChildren = EditorUtil.EditorOnlyToggle("Find From Children", "uLipSyncAnimator", true);
        EditorUtil.DrawProperty(serializedObject, nameof(findFromChildren));

        if (findFromChildren)
        {
            DrawAnimatorsInChildren();
        }
        else
        {
            if (anim.animator == null)
            {
                EditorGUILayout.HelpBox("Animator is not assigned.", MessageType.Warning);
                EditorUtil.DrawProperty(serializedObject, nameof(anim.animator));
            }
            else
            {
                EditorUtil.DrawProperty(serializedObject, nameof(anim.animator));
            }
        }
    }

    protected void DrawAnimatorsInChildren()
    {
        var animators = anim.GetComponentsInChildren<Animator>();
        if (animators.Length == 0)
        {
            EditorGUILayout.HelpBox("Animator is not found in children.", MessageType.Warning);
        }
        else
        {
            int index = animators.ToList().FindIndex(x => x == anim.animator);
            var names = animators.Select(x => x.gameObject.name).ToArray();
            var newIndex = EditorGUILayout.Popup("Animators", index, names);
            if (newIndex != index)
            {
                Undo.RecordObject(target, "Change Animator");
                anim.animator = animators[newIndex];
            }
        }
    }

    protected void DrawAnimatorReorderableList()
    {
        if (_reorderableList == null)
        {
            _reorderableList = new ReorderableList(anim.parameters, typeof(MfccData));
            _reorderableList.drawHeaderCallback = rect =>
            {
                rect.xMin -= EditorGUI.indentLevel * 12f;
                EditorGUI.LabelField(rect, "Phoneme - Parameter Table");
            };
            _reorderableList.draggable = true;
            _reorderableList.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                DrawParameterListItem(rect, index);
            };
            _reorderableList.elementHeightCallback = index =>
            {
                return GetParameterListItemHeight(index);
            };
        }

        EditorGUILayout.Separator();
        EditorGUILayout.BeginHorizontal();
        var indent = EditorGUI.indentLevel * 12f;
        EditorGUILayout.Space(indent, false);
        _reorderableList.DoLayoutList();
        EditorGUILayout.EndHorizontal();
    }

    protected void DrawParameterListItem(Rect rect, int index)
    {
        var animator = anim.animator;
        if (!animator) return;

        if (!animator.isInitialized)
        {
            animator.Rebind();
        }

        rect.y += 2f;
        rect.height = EditorGUIUtility.singleLineHeight;

        var animatorParams = animator.parameters;
        var param = anim.parameters[index];
        float singleLineHeight =
            EditorGUIUtility.singleLineHeight +
            EditorGUIUtility.standardVerticalSpacing;

        param.phoneme = EditorGUI.TextField(rect, "Phoneme", param.phoneme);

        rect.y += singleLineHeight;

        var curIndex = Mathf.Max(param.index, 0);
        var newIndex = EditorGUI.Popup(rect, "Parameter", curIndex, GetParameterArray());
        if (newIndex != curIndex || 
            param.name != animatorParams[curIndex].name)
        {
            Undo.RecordObject(target, "Change Parameter");
            param.index = Mathf.Clamp(newIndex, 0, animatorParams.Length - 1);
            param.name = animatorParams[param.index].name;
            param.nameHash = Animator.StringToHash(param.name);
        }

        rect.y += singleLineHeight;

        float weight = EditorGUI.Slider(rect, "Max Weight", param.maxWeight, 0f, 1f);
        if (weight != param.maxWeight)
        {
            Undo.RecordObject(target, "Change Max Weight");
            param.maxWeight = weight;
        }

        rect.y += singleLineHeight;
    }

    protected virtual float GetParameterListItemHeight(int index)
    {
        return 64f;
    }

    protected virtual string[] GetParameterArray()
    {
        if (anim.animator == null)
        {
            return new string[0];
        }

        var parameters = anim.animator.parameters;
        var names = new List<string>();
        for (int i = 0; i < parameters.Length; ++i)
        {
            var name = parameters[i].name;
            names.Add(name);
        }

        return names.ToArray();
    }

    protected void DrawParameters()
    {
        Undo.RecordObject(target, "Change Volume Min/Max");
        EditorGUILayout.MinMaxSlider(
            "Volume Min/Max (Log10)",
            ref anim.minVolume,
            ref anim.maxVolume,
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
            if (anim.minVolume > -4.5f)
            {
                minPos.x += (anim.minVolume + 5f) / 5f * rect.width - 30f;
            }
            EditorGUI.LabelField(minPos, $"{anim.minVolume.ToString("F2")}", style);

            var maxPos = rect;
            var maxX = (anim.maxVolume + 5f) / 5f * rect.width;
            maxPos.y -= 12f;
            if (maxX < maxPos.width - 48f)
            {
                maxPos.x += maxX;
            }
            else
            {
                maxPos.x += maxPos.width - 48f;
            }
            EditorGUI.LabelField(maxPos, $"{anim.maxVolume.ToString("F2")}", style);
            GUI.color = origColor;
        }
        EditorGUILayout.EndHorizontal();

        EditorUtil.DrawProperty(serializedObject, nameof(anim.smoothness));
        EditorUtil.DrawProperty(serializedObject, nameof(anim.minimalValueThreshold));
    }
}

}