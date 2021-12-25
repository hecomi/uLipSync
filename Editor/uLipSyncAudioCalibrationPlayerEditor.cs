using UnityEngine;
using UnityEditor;

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
    bool _drawCursor = false;

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
        DrawCrossFade();
        DrawHelpBox();

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

        Handles.DrawSolidRectangleWithOutline(
            rect,
            new Color(0f, 0f, 0f, 0.2f),
            new Color(1f, 1f, 1f, 0.2f));

        if (!player.clip) return;

        var preWaveStart = player.start;
        var preWaveEnd = player.end;

        EditorUtil.DrawWave(rect, player.clip);
        DrawTrimArea(rect, true, ref player.start, ref _isDraggingStart, ref _isDraggingEnd);
        DrawTrimArea(rect, false, ref player.end, ref _isDraggingEnd, ref _isDraggingStart);
        DrawCursor(rect);

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
            _requireRepaint = true;
        }

        return delta;
    }

    void DrawCursor(Rect rect)
    {
        if (!Application.isPlaying) return;

        EditorGUILayout.Separator();
        _drawCursor = EditorGUILayout.Toggle("Show Cursor", _drawCursor);
        if (!_drawCursor) return;

        var cursorRect = rect;
        var range = player.end - player.start;
        rect.x += (player.start + player.position * range) * rect.width;
        rect.width = 5f;
        EditorGUI.DrawRect(rect, new Color(1f, 1f, 0f, 0.2f));

        _requireRepaint = true;
    }

    void DrawCrossFade()
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
            EditorGUILayout.HelpBox(msg, MessageType.Warning);
        }
    }
}

}
