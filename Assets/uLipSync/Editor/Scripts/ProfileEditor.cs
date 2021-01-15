using UnityEngine;
using UnityEditor;

namespace uLipSync
{

[CustomEditor(typeof(Profile))]
public class ProfileEditor : Editor
{
    Profile profile { get { return target as Profile; } }
    float min = 0f, max = 0f;

    public override void OnInspectorGUI()
    {
        CalcMinMax();
        Draw(profile.a, "A");
        Draw(profile.i, "I");
        Draw(profile.u, "U");
        Draw(profile.e, "E");
        Draw(profile.o, "O");
    }

    void CalcMinMax()
    {
        max = float.MinValue;
        min = float.MaxValue;
        foreach (var data in new MfccData[] { profile.a, profile.i, profile.u, profile.e, profile.o })
        {
            for (int j = 0; j < data.mfccList.Count; ++j)
            {
                var array = data.mfccList[j].array;
                for (int i = 0; i < array.Length; ++i)
                {
                    var x = array[i];
                    max = Mathf.Max(max, x);
                    min = Mathf.Min(min, x);
                }
            }
        }
    }

    void Draw(MfccData data, string name)
    {
        if (!EditorUtil.Foldout(name, true)) return;

        ++EditorGUI.indentLevel;

        var h = 5;
        var totalHeight = h * data.mfccList.Count;
        var area = GUILayoutUtility.GetRect(Screen.width, totalHeight);
        area = EditorGUI.IndentedRect(area);
        var w = area.width / 12;
        var maxMinusMin = max - min;

        for (int j = 0; j < data.mfccList.Count; ++j)
        {
            var y = h * j;
            for (int i = 0; i < 12; ++i)
            {
                var x = w * i;
                var rect = new Rect(area.x + x, area.y + y, w, h);
                var value = (data.mfccList[j].array[i] - min) / maxMinusMin;
                var color = ToRGB(value);
                Handles.DrawSolidRectangleWithOutline(rect, color, color);
            }
        }

        --EditorGUI.indentLevel;
    }

    Color ToRGB(float hue)
    {
        hue = 1f - hue;
        hue = hue * 5f;
        var x = 1 - Mathf.Abs(hue % 2f - 1f);
        return
            hue < 1f ? new Color(1f, x, 0f) :
            hue < 2f ? new Color(x, 1f, 0f) :
            hue < 3f ? new Color(0f, 1f, x) :
            hue < 4f ? new Color(0f, x, 1f) :
            new Color(x * 0.5f, 0f, 0.5f);
    }
}

}
