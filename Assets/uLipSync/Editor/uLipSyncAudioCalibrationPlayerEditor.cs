using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace uLipSync
{

[CustomEditor(typeof(uLipSyncCalibrationAudioPlayer))]
public class uLipSyncCalibrationAudioPlayerEditor : Editor
{
    uLipSyncCalibrationAudioPlayer player { get { return target as uLipSyncCalibrationAudioPlayer; } }

    bool _requireRepaint = false;
    bool _requireApply = false;
    bool _isDraggingStart = false;
    bool _isDraggingEnd = false;
    bool isDragging => _isDraggingStart || _isDraggingEnd;
    List<string> _messages = new List<string>();

    public override void OnInspectorGUI()
    {
        _requireRepaint = false;

        serializedObject.Update();

        DrawClip();

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button(" Play "))
        {
            AudioUtil.PlayClip(player.clip);
        }
        if (GUILayout.Button(" Stop "))
        {
            AudioUtil.StopClip(player.clip);
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Separator();

        DrawWave();
        EditorGUILayout.Separator();
        DrawParameters();

        if (Application.isPlaying && _requireApply)
        {
            player.Apply();
            _requireApply = false;
        }

        serializedObject.ApplyModifiedProperties();

        if (_requireRepaint)
        {
            Repaint();
        }
        else
        {
            EditorUtility.SetDirty(target);
        }

        if (isDragging)
        {
            player.Pause();
        }
        else
        {
            player.UnPause();
        }

        DrawHelpBox();
    }

    public override bool RequiresConstantRepaint()
    {
        return 
            _requireRepaint || 
            player.isPlaying ||
            base.RequiresConstantRepaint();
    }

    void DrawClip()
    {
        var nextClip = (AudioClip)EditorGUILayout.ObjectField("Clip", player.clip, typeof(AudioClip), true);
        if (nextClip == player.clip) return;
        player.clip = nextClip;
        _requireApply = true;
    }

    void DrawWave()
    {
        var rect = EditorGUILayout.GetControlRect(GUILayout.Height(100));
        EditorUtil.DrawBackgroundRect(rect);

        if (!player.clip) return;

        EditorUtil.DrawWave(rect, player.clip, new EditorUtil.DrawWaveOption());
        var preWaveStart = player.start;
        var preWaveEnd = player.end;

        DrawTrimArea(rect, true, ref player.start, ref _isDraggingStart, ref _isDraggingEnd);
        DrawTrimArea(rect, false, ref player.end, ref _isDraggingEnd, ref _isDraggingStart);
        DrawCrossFadeArea(rect);

        player.start = Mathf.Clamp(player.start, 0f, preWaveEnd - 0.001f);
        player.end = Mathf.Clamp(player.end, preWaveStart + 0.001f, 1f);
    }

    void DrawTrimArea(Rect rect, bool isStart, ref float range, ref bool isDraggingSelf, ref bool isDraggingOther)
    {
        var trimArea = rect;
        if (isStart)
        {
            trimArea.width *= range;
        }
        else
        {
            trimArea.width *= 1f - range;
            trimArea.x += rect.width - trimArea.width;
        }
        EditorGUI.DrawRect(trimArea, new Color(0f, 0f, 0f, 0.7f));

        var deltaPixels = ProcessDrag(trimArea, ref isDraggingSelf, ref isDraggingOther);
        range += deltaPixels / rect.width;

        var borderRect = trimArea;
        if (isStart)
        {
            borderRect.x += trimArea.width;
        }
        borderRect.width = 1;
        var borderColor = isDraggingSelf ?
            new Color(1f, 0f, 0f, 1f) :
            new Color(1f, 1f, 1f, 0.5f);
        EditorGUI.DrawRect(borderRect, borderColor);
    }

    float ProcessDrag(Rect dragRect, ref bool isDraggingSelf, ref bool isDraggingOther)
    {
        float delta = 0f;

        var mouseRect = dragRect;
        mouseRect.x -= 10;
        mouseRect.width += 20;
        EditorGUIUtility.AddCursorRect(mouseRect, MouseCursor.SplitResizeLeftRight);

        if (!isDraggingOther &&
            Event.current.type == EventType.MouseDrag)
        {
            if (mouseRect.Contains(Event.current.mousePosition))
            {
                isDraggingSelf = true;
            }

            if (isDraggingSelf)
            {
                delta = Event.current.delta.x;
                _requireApply = true;
            }

            _requireRepaint = true;
        }
        else if (Event.current.type == EventType.MouseUp)
        {
            isDraggingSelf = false;
        }

        return delta;
    }

    void DrawCrossFadeArea(Rect rect)
    {
        var range = player.crossFadeDuration / player.clip.length;
        range = Mathf.Min(range, player.end - player.start);
        rect.x += (player.end - range) * rect.width;
        rect.width *= range;
        EditorGUI.DrawRect(rect, new Color(0f, 1f, 1f, 0.2f));
    }

    void DrawParameters()
    {
        float preDuration = player.crossFadeDuration;
        player.crossFadeDuration = EditorGUILayout.Slider("Cross Fade", preDuration, 0f, 0.1f);
        if (player.crossFadeDuration != preDuration)
        {
            _requireApply = true;
        }
    }

    void DrawHelpBox()
    {
        var lipSync = player.GetComponent<uLipSync>();
        if (lipSync && lipSync.outputSoundGain < Mathf.Epsilon)
        {
            var msg = 
                "uLipSync.outputSoundGain is zero." +
                "It means that you will not be able to hear the sound.";
            _messages.Add(msg);
        }

        foreach (var msg in _messages)
        {
            EditorGUILayout.HelpBox(msg, MessageType.Warning);
        }

        _messages.Clear();
    }
}

}
