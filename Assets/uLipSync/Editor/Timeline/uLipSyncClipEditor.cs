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
    internal class TextureCache
    {
        public Texture2D texture;
        public bool forceUpdate = false;
    }

    Dictionary<uLipSyncClip, TextureCache> _textures = new Dictionary<uLipSyncClip, TextureCache>();

    void RemoveCachedTexture(uLipSyncClip clip)
    {
        if (!_textures.ContainsKey(clip)) return;

        var cache = _textures[clip];
        Object.DestroyImmediate(cache.texture);
        _textures.Remove(clip);
    }

    Texture2D CreateCachedTexture(uLipSyncClip clip, int width, int height)
    {
        RemoveCachedTexture(clip);

        var data = clip.bakedData;
        if (!data) return null;

        width = Mathf.Clamp(width, 128, 4096);
        var tex = data.CreateTexture(width, height);
        var cache = new TextureCache { texture = tex };
        _textures.Add(clip, cache);

        return tex;
    }

    Texture2D GetOrCreateCachedTexture(uLipSyncClip clip, int width, int height)
    {
        if (!_textures.ContainsKey(clip))
        {
            return CreateCachedTexture(clip, width, height);
        }

        var cache = _textures[clip];
        if (cache.forceUpdate || !cache.texture)
        {
            return CreateCachedTexture(clip, width, height);
        }

        var dw = Mathf.Abs(cache.texture.width - width);
        var dh = Mathf.Abs(cache.texture.height - height);
        if (dw > 10 || dh > 10)
        {
            return CreateCachedTexture(clip, width, height);
        }

        return cache.texture;
    }

    public override void DrawBackground(TimelineClip clip, ClipBackgroundRegion region)
    {
        DrawBackground(region);
        DrawWave(clip, region);
    }

    void DrawBackground(ClipBackgroundRegion region)
    {
        EditorUtil.DrawBackgroundRect(
            region.position, 
            new Color(0f, 0f, 0f, 0.3f), 
            Color.clear);
    }

    void DrawWave(TimelineClip timelineClip, ClipBackgroundRegion region)
    {
        var clip = timelineClip.asset as uLipSyncClip;
        var data = clip.bakedData;
        if (!data) return;

        var audioClip = data.audioClip;
        if (!audioClip) return;

        var rect = region.position;
        var duration = region.endTime - region.startTime;
        var width = (float)(rect.width * audioClip.length / duration);
        var left = Mathf.Max((float)timelineClip.clipIn, (float)region.startTime);
        var offset = (float)(width * left / audioClip.length);
        rect.x -= offset;
        rect.width = width;

        var tex = GetOrCreateCachedTexture(clip, (int)rect.width, (int)rect.height);
        if (!tex) return;

        GUI.DrawTexture(rect, tex);
    }

    public override void OnClipChanged(TimelineClip timelineClip)
    {
        var clip = timelineClip.asset as uLipSyncClip;

        if (!_textures.ContainsKey(clip)) return;

        _textures[clip].forceUpdate = true;
    }
}

}
