using UnityEngine;
using UnityEngine.Timeline;
using UnityEditor;
using UnityEditor.Timeline;
using System.Collections.Generic;

namespace uLipSync.Timeline
{

[CustomEditor(typeof(uLipSyncClip))]
public class uLipSyncClipEditor : Editor
{
    uLipSyncClip clip { get => target as uLipSyncClip; }
    BakedData data { get => clip.bakedData; }
    Editor _bakedDataEditor = null;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        if (EditorUtil.Foldout("Data", true, "-uLipSyncClip"))
        {
            ++EditorGUI.indentLevel;
            DrawBakedData();
            --EditorGUI.indentLevel;
            EditorGUILayout.Separator();
        }

        if (EditorUtil.Foldout("Parameters", true, "-uLipSyncClip"))
        {
            ++EditorGUI.indentLevel;
            DrawParameters();
            --EditorGUI.indentLevel;
            EditorGUILayout.Separator();
        }

        serializedObject.ApplyModifiedProperties();
    }

    void DrawBakedData()
    {
        EditorUtil.DrawProperty(serializedObject, nameof(clip.bakedData));

        if (data)
        {
            EditorGUILayout.Separator();

            CreateCachedEditor(data, typeof(BakedDataEditor), ref _bakedDataEditor);
            var editor = _bakedDataEditor as BakedDataEditor;
            if (editor) 
            {
                editor.OnInspectorGUI();
            }
        }
    }

    void DrawParameters()
    {
        EditorUtil.DrawProperty(serializedObject, nameof(clip.volume));
        EditorUtil.DrawProperty(serializedObject, nameof(clip.timeOffset));
    }
}

[CustomTimelineEditor(typeof(uLipSyncClip))]
public class uLipSyncClipTimelineEditor : ClipEditor
{
    Dictionary<uLipSyncClip, Texture2D> _textures = new Dictionary<uLipSyncClip, Texture2D>();

    void DrawBackground(ClipBackgroundRegion region)
    {
        EditorUtil.DrawBackgroundRect(
            region.position, 
            new Color(0f, 0f, 0f, 0.3f), 
            Color.clear);
    }

    void DrawWave(TimelineClip clip, ClipBackgroundRegion region)
    {
        var ls = clip.asset as uLipSyncClip;
        var data = ls.bakedData;
        if (!data) return;

        var audioClip = data.audioClip;
        if (!audioClip) return;

        var rect = region.position;
        var duration = region.endTime - region.startTime;
        var width = (float)(rect.width * audioClip.length / duration);
        var left = Mathf.Max((float)clip.clipIn, (float)region.startTime);
        var offset = (float)(width * left / audioClip.length);
        rect.x -= offset;
        rect.width = width;

        Texture2D tex;
        _textures.TryGetValue(ls, out tex);

        var texWidth = Mathf.Clamp((int)rect.width, 128, 4096);
        var texHeight = (int)rect.height;
        var dw = tex ? Mathf.Abs(tex.width - texWidth) : 0;
        var dh = tex ? Mathf.Abs(tex.height - texHeight) : 0;

        if (!tex || dw > 10 || dh > 10)
        {
            tex = data.CreateTexture(texWidth, texHeight);
            _textures[ls] = tex;
        }

        if (!tex) return;

        GUI.DrawTexture(rect, tex);
    }

    public override void DrawBackground(TimelineClip clip, ClipBackgroundRegion region)
    {
        DrawBackground(region);
        DrawWave(clip, region);
    }
}

}
