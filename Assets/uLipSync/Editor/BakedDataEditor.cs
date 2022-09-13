using System;
using UnityEngine;
using UnityEditor;
using System.Text;
using System.Collections.Generic;

namespace uLipSync
{

[CustomEditor(typeof(BakedData))]
public class BakedDataEditor : Editor
{
    BakedData data { get => target as BakedData; }
    StringBuilder _msg = new StringBuilder();
    public static Color[] phonemeColors = new Color[]
    {
        Color.red,
        Color.cyan,
        Color.yellow,
        Color.magenta,
        Color.green,
        Color.blue,
        Color.gray,
    };

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        if (EditorUtil.Foldout("Bake", true))
        {
            ++EditorGUI.indentLevel;
            DrawBake();
            DrawMessage();
            --EditorGUI.indentLevel;
            EditorGUILayout.Separator();
        }

        if (EditorUtil.Foldout("Data", false, "-BakedData"))
        {
            ++EditorGUI.indentLevel;
            DrawBakedData();
            --EditorGUI.indentLevel;
            EditorGUILayout.Separator();
        }

        serializedObject.ApplyModifiedProperties();
    }

    void DrawBake()
    {
        EditorUtil.DrawProperty(serializedObject, nameof(data.profile));
        EditorUtil.DrawProperty(serializedObject, nameof(data.audioClip));

        bool isValid = data.profile && data.audioClip;

        EditorGUILayout.Separator();
        EditorGUI.BeginDisabledGroup(!isValid);
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button(data.isDataChanged ? "  * Bake  " : "  Bake  "))
        {
            Bake();
        }
        EditorGUILayout.EndHorizontal();
        EditorGUI.EndDisabledGroup();
    }

    void DrawMessage()
    {
        _msg.Clear();

        if (!data.profile)
        {
            _msg.Append("* Please set Profile.");
        }

        if (!data.audioClip)
        {
            if (_msg.Length > 0) _msg.Append("\n");
            _msg.Append("* Please set AudioClip");
        }

        if (data.isDataChanged)
        {
            if (_msg.Length > 0) _msg.Append("\n");
            _msg.Append("* Some parameter has changed. Please bake the data again.");
        }

        if (_msg.Length > 0)
        {
            EditorGUILayout.Separator();
            EditorGUILayout.HelpBox(_msg.ToString(), MessageType.Warning);
        }
    }

    void DrawBakedData()
    {
        EditorGUILayout.LabelField("Duration", data.duration.ToString("F3") + " (Sec)");
        EditorGUILayout.Separator();

        var rect = EditorGUILayout.GetControlRect(GUILayout.Height(100));
        rect.xMin += 15 * EditorGUI.indentLevel;
        EditorUtil.DrawBackgroundRect(rect);
        DrawWave(rect);

        rect = EditorGUILayout.GetControlRect(GUILayout.Height(100));
        rect.xMin += 15 * EditorGUI.indentLevel;
        EditorUtil.DrawBackgroundRect(rect);
        DrawFrames(rect);
    }

    void DrawWave(Rect rect)
    {
        if (data.audioClip)
        {
            bool hasFrame = !data.isDataChanged && data.frames.Count > 0;
            var currentColor = new Color();
            var smooth = 0.15f;
            var option = new EditorUtil.DrawWaveOption();
            option.waveScale = 1f;
            if (hasFrame)
            {
                option.colorFunc = x => {
                    var t = x * data.duration;
                    var frame = data.GetFrame(t);
                    var color = new Color();
                    for (int i = 0; i < frame.phonemes.Count; ++i)
                    {
                        var colorIndex = i % phonemeColors.Length;
                        color += phonemeColors[colorIndex] * frame.phonemes[i].ratio;
                    }
                    currentColor += (color - currentColor) * smooth;
                    return currentColor;
                };
            }
            EditorUtil.DrawWave(rect, data.audioClip, option);
        }

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button(" Preview "))
        {
            AudioUtil.PlayClip(data.audioClip);
        }
        if (GUILayout.Button(" Stop "))
        {
            AudioUtil.StopClip(data.audioClip);
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Separator();
    }

    void DrawFrames(Rect rect)
    {
        if (!data.audioClip) return;

        int n = data.frames.Count;
        if (n == 0) return;

        int maxN = 512;
        int skip = (int)Mathf.Ceil((float)n / maxN);

        var phonemeCount = data.frames[0].phonemes.Count;
        var ratioPointsList = new List<Vector3[]>();

        float maxRatio = 0f;
        for (int j = 0; j < phonemeCount; ++j)
        {
            for (int i = 0; i < n; ++i)
            {
                var frame = data.frames[i];
                var val = frame.phonemes[j].ratio * frame.volume;
                maxRatio = Mathf.Max(val, maxRatio);
            }
        }
        maxRatio = Mathf.Max(maxRatio, 0.01f);

        for (int j = 0; j < phonemeCount; ++j)
        {
            var points = new Vector3[n];

            for (int i = 0; i < n; ++i)
            {
                var index = Mathf.Min(i * skip, data.frames.Count - 1);
                var frame = data.frames[index];
                var x = (float)index / Mathf.Max(n - 1, 1);
                var y = frame.phonemes[j].ratio * frame.volume / maxRatio;
                x = rect.x + x * rect.width;
                y = rect.y + (1f - y) * rect.height;
                points[i] = new Vector3(x, y, 0f);
            }

            ratioPointsList.Add(points);
        }

        for (int i = 0; i < ratioPointsList.Count; ++i)
        {
            var colorIndex = i % phonemeColors.Length;
            Handles.color = phonemeColors[colorIndex];
            Handles.DrawAAPolyLine(2f, ratioPointsList[i]);
        }

        EditorGUILayout.Separator();

        var legendHeight = EditorGUIUtility.singleLineHeight * phonemeCount;
        var legendBaseRect = EditorGUILayout.GetControlRect(GUILayout.Height(EditorGUIUtility.singleLineHeight));
        legendBaseRect.xMin += 15 * EditorGUI.indentLevel;

        rect = legendBaseRect;
        rect.height = EditorGUIUtility.singleLineHeight;
        var width = EditorGUIUtility.currentViewWidth - rect.xMin;
        var lineWidth = 32f;
        var lineMargin = 6f;
        var legendMargin = 12f;
        var legendAddLines = 0;

        for (int j = 0; j < phonemeCount; ++j)
        {
            var phoneme = data.frames[0].phonemes[j].phoneme;
            var labelWidth = GUI.skin.label.CalcSize(new GUIContent(phoneme)).x;

            if (rect.x + lineWidth + lineMargin + labelWidth > width)
            {
                rect.xMin = legendBaseRect.xMin;
                rect.y += EditorGUIUtility.singleLineHeight;
                ++legendAddLines;
            }

            var colorIndex = j % phonemeColors.Length;
            var p0 = new Vector3(rect.x, rect.y + rect.height / 2, 0f);
            var p1 = p0 + new Vector3(lineWidth, 0f, 0f);
            Handles.color = phonemeColors[colorIndex];
            Handles.DrawAAPolyLine(2f, new Vector3[] { p0, p1 });

            rect.xMin += lineWidth + lineMargin;
            GUI.Label(rect, phoneme);
            rect.xMin += labelWidth + legendMargin;
        }

        EditorGUILayout.GetControlRect(GUILayout.Height(EditorGUIUtility.singleLineHeight * legendAddLines));
    }

    public void Bake()
    {
        data.bakedProfile = data.profile;
        data.bakedAudioClip = data.audioClip;
        data.frames.Clear();

        var clip = data.audioClip;
        int samplePerFrame = clip.frequency / 60 * clip.channels;
        var buffer = new float[clip.samples * clip.channels];
        var tempBuffer = new float[samplePerFrame];

        data.duration = clip.length;

        var go = new GameObject("uLipSync Baking...");
        var ls = go.AddComponent<uLipSync>();
        ls.OnBakeStart(data.profile);

        clip.GetData(buffer, 0);

        for (int offset = 0; offset < buffer.Length - samplePerFrame; offset += samplePerFrame)
        {
            Array.Copy(buffer, offset, tempBuffer, 0, samplePerFrame);
            ls.OnBakeUpdate(tempBuffer, clip.channels);

            var frame = new BakedFrame();
            frame.volume = ls.result.rawVolume;
            frame.phonemes = new List<BakedPhonemeRatio>();

            foreach (var kv in ls.result.phonemeRatios)
            {
                var pr = new BakedPhonemeRatio();
                pr.phoneme = kv.Key;
                pr.ratio = kv.Value;
                frame.phonemes.Add(pr);
            }

            data.frames.Add(frame);

            var progress = (float)offset / clip.samples;
        }

        ls.OnBakeEnd();
        DestroyImmediate(go);
    }
}

}
