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
    static Color[] phonemeColors = new Color[]
    {
        Color.red,
        Color.blue,
        Color.yellow,
        Color.magenta,
        Color.green,
        Color.cyan,
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

        if (EditorUtil.Foldout("Data", false))
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
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        EditorGUI.BeginDisabledGroup(!isValid);
        if (GUILayout.Button(data.isDataChanged ? "  * Bake  " : "  Bake  "))
        {
            Bake();
        }
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();
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
        DrawWave();
        DrawFrames();
    }

    void DrawWave()
    {
        var rect = EditorGUILayout.GetControlRect(GUILayout.Height(100));
        rect.xMin += 16;

        EditorUtil.DrawBackgroundRect(rect);
        if (data.audioClip)
        {
            EditorUtil.DrawWave(rect, data.audioClip);
        }

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button(" Play "))
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

    void DrawFrames()
    {
        var rect = EditorGUILayout.GetControlRect(GUILayout.Height(100));
        rect.xMin += 16;

        EditorUtil.DrawBackgroundRect(rect);

        if (!data.audioClip) return;

        var n = data.frames.Count;
        if (n == 0) return;

        var phonemeCount = data.frames[0].phonemes.Count;
        var pointsList = new List<Vector3[]>();

        float max = 0f;
        for (int j = 0; j < phonemeCount; ++j)
        {
            for (int i = 0; i < n; ++i)
            {
                var frame = data.frames[i];
                var val = frame.phonemes[j].ratio * frame.volume;
                max = Mathf.Max(val, max);
            }
        }
        max = Mathf.Max(max, 0.01f);

        for (int j = 0; j < phonemeCount; ++j)
        {
            var points = new Vector3[n];

            for (int i = 0; i < n; ++i)
            {
                var frame = data.frames[i];
                var x = (float)i / Mathf.Max(n - 1, 1);
                var y = frame.phonemes[j].ratio * frame.volume / max;
                x = rect.x + x * rect.width;
                y = rect.y + (1f - y) * rect.height;
                points[i] = new Vector3(x, y, 0f);
            }

            pointsList.Add(points);
        }

        for (int i = 0; i < pointsList.Count; ++i)
        {
            var colorIndex = i % phonemeColors.Length;
            Handles.color = phonemeColors[colorIndex];
            Handles.DrawAAPolyLine(2f, pointsList[i]);
        }

        EditorGUILayout.Separator();

        var legendHeight = EditorUtil.lineHeightWithMargin * phonemeCount;
        rect = EditorGUILayout.GetControlRect(GUILayout.Height(legendHeight));
        rect.xMin += 16;
        rect.height = EditorGUIUtility.singleLineHeight;
        for (int j = 0; j < phonemeCount; ++j)
        {
            var colorIndex = j % phonemeColors.Length;
            var p0 = new Vector3(rect.x, rect.y + rect.height / 2, 0f);
            var p1 = p0 + new Vector3(32f, 0f, 0f);
            Handles.color = phonemeColors[colorIndex];
            Handles.DrawAAPolyLine(2f, new Vector3[] { p0, p1 });

            var labelRect = rect;
            labelRect.xMin += 48f;
            GUI.Label(labelRect, data.frames[0].phonemes[j].phoneme);

            rect.y += EditorUtil.lineHeightWithMargin;
        }
    }

    void Bake()
    {
        data.bakedProfile = data.profile;
        data.bakedAudioClip = data.audioClip;
        data.frames.Clear();

        var clip = data.audioClip;
        int samplePerFrame = clip.frequency / 60 * clip.channels;
        var buf = new float[samplePerFrame];

        data.duration = clip.length;

        var go = new GameObject("uLipSync Baking...");
        var ls = go.AddComponent<uLipSync>();
        ls.OnBakeStart(data.profile);

        for (int offset = 0; offset < clip.samples; offset += samplePerFrame)
        {
            clip.GetData(buf, offset);
            ls.OnBakeUpdate(buf, clip.channels);

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
            EditorUtility.DisplayProgressBar("uLipSync Bake", "Baking...", progress);
        }

        EditorUtility.ClearProgressBar();

        ls.OnBakeEnd();
        DestroyImmediate(go);
    }
}

}
