using UnityEngine;
using UnityEditor;

namespace uLipSync
{

[CustomEditor(typeof(Profile))]
public class ProfileEditor : Editor
{
    Profile profile { get { return target as Profile; } }
    public float min = 0f;
    public float max = 0f;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorUtil.DrawProperty(serializedObject, nameof(profile.mfccDataCount));
        EditorUtil.DrawProperty(serializedObject, nameof(profile.targetSampleRate));
        profile.targetSampleRate = Mathf.Max(profile.targetSampleRate, 1000);
        CalcMinMax();
        Draw(profile.a, "A");
        Draw(profile.i, "I");
        Draw(profile.u, "U");
        Draw(profile.e, "E");
        Draw(profile.o, "O");
        serializedObject.ApplyModifiedProperties();
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
        if (!EditorUtil.SimpleFoldout(name, true)) return;

        ++EditorGUI.indentLevel;

        foreach (var mfcc in data.mfccList)
        {
            EditorUtil.DrawMfcc(mfcc.array, max, min, 2f);
        }

        --EditorGUI.indentLevel;
    }
}

}
