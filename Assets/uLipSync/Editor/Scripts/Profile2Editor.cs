using UnityEngine;
using UnityEditor;

namespace uLipSync
{

[CustomEditor(typeof(Profile2))]
public class Profile2Editor : Editor
{
    Profile2 profile { get { return target as Profile2; } }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var origColor = Handles.color;

        var area = GUILayoutUtility.GetRect(Screen.width, 300f);
        area = EditorGUI.IndentedRect(area);
        var margin = new EditorUtil.Margin(10, 10f, 30f, 40f);
        var range = new Vector2(4000f, 1f);

        float xMin = area.x + margin.left;
        float xMax = area.xMax - margin.right;
        float yMin = area.y + margin.top;
        float yMax = area.yMax - margin.bottom;
        float width = xMax - xMin;
        float height = yMax - yMin;

        EditorUtil.DrawGrid(
            area,
            Color.white, 
            new Color(1f, 1f, 1f, 0.5f), 
            margin,
            range,
            new Vector2(8f, 1f));

        var colors = new Color[5]
        {
            Color.red,
            Color.green,
            Color.blue,
            Color.cyan,
            Color.magenta,
        };

        for (int j = (int)Vowel.A; j <= (int)Vowel.O; ++j)
        {
            var vowel = (Vowel)j;
            var nativeArray = profile.GetNativeArrayForEditor(vowel);
            if (nativeArray == null) continue; 
            var n = nativeArray.Length;
            var points = new Vector3[n];
            float max = Algorithm.GetMaxValue(ref nativeArray);
            float min = Mathf.Log10(1e-2f);
            for (int i = 0; i < n && i < n; ++i)
            {
                float val = nativeArray[i] / max;
                val = Mathf.Log10(10f * val);
                val = (val - min) / (1f - min);
                val = Mathf.Max(val, 0f);
                float x = xMin + width * i / (n - 1);
                float y = yMax - height * val;
                points[i] = new Vector3(x, y, 0f);
            }

            Handles.color = colors[j];
            Handles.DrawAAPolyLine(3f, points);
        }

        Handles.color = origColor;
    }
}

}
