using UnityEngine;
using UnityEditor;
using System.Linq;

namespace uLipSync
{

public static class EditorUtil
{
    public static string GetKey(string title, string category)
    {
        return $"{Common.assetName}-{category}-{title}";
    }

    private static string GetFoldOutKey(string title)
    {
        return GetKey(title, "FoldOut");
    }

    public static bool EditorOnlyToggle(string title, string category, bool initialState)
    {
        var keyTitle = title.Replace(" ", "_");
        var key = GetKey(keyTitle, category);
        var value = EditorPrefs.GetBool(key, initialState);
        var newValue = EditorGUILayout.Toggle(title, value);
        if (newValue != value)
        {
            EditorPrefs.SetBool(key, newValue);
        }
        return newValue;
    }

    public static bool IsFoldOutOpened(string title)
    {
        return EditorPrefs.GetBool(GetFoldOutKey(title));
    }

    public static bool Foldout(string title, bool initialState)
    {
        var style = new GUIStyle("ShurikenModuleTitle");
        style.font = new GUIStyle(EditorStyles.label).font;
        style.border = new RectOffset(15, 7, 4, 4);
        style.fixedHeight = 22;
        style.contentOffset = new Vector2(20f, -2f);

        var key = GetFoldOutKey(title);
        bool display = EditorPrefs.GetBool(key, initialState);

        var rect = GUILayoutUtility.GetRect(16f, 22f, style);
        GUI.Box(rect, title, style);

        var e = Event.current;

        var toggleRect = new Rect(rect.x + 4f, rect.y + 2f, 13f, 13f);
        if (e.type == EventType.Repaint) 
        {
            EditorStyles.foldout.Draw(toggleRect, false, false, display, false);
        }

        if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition)) 
        {
            EditorPrefs.SetBool(key, !display);
            e.Use();
        }

        return display;
    }

    public static bool SimpleFoldout(string title, bool initialState)
    {
        var key = GetFoldOutKey(title);
        bool display = EditorPrefs.GetBool(key, initialState);
        bool newDisplay = EditorGUILayout.Foldout(display, title, EditorStyles.foldoutHeader);
        if (newDisplay != display) EditorPrefs.SetBool(key, newDisplay);
        return newDisplay;
    }

    public static void DrawProperty(SerializedObject obj, string propName)
    {
        var prop = obj.FindProperty(propName);
        if (prop == null) return;
        EditorGUILayout.PropertyField(prop);
    }

    public static void DrawMicSelector(ref int index)
    {
        var mics = MicUtil.GetDeviceList();
        var micNames = mics.Select(x => x.name).ToArray();
        index = EditorGUILayout.Popup("Device", index, micNames);
    }
}

}
