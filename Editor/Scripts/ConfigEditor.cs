using UnityEditor;

namespace uLipSync
{

[CustomEditor(typeof(Config))]
public class ConfigEditor : Editor
{
    Config config { get { return target as Config; } }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorUtil.DrawProperty(serializedObject, nameof(config.sampleCount));
        EditorUtil.DrawProperty(serializedObject, nameof(config.lpcOrder));
        EditorUtil.DrawProperty(serializedObject, nameof(config.frequencyResolution));
        EditorUtil.DrawProperty(serializedObject, nameof(config.maxFrequency));
        EditorUtil.DrawProperty(serializedObject, nameof(config.windowFunc));
        EditorUtil.DrawProperty(serializedObject, nameof(config.checkSecondDerivative));
        EditorUtil.DrawProperty(serializedObject, nameof(config.checkThirdFormant));

        var newFilterH = EditorGUILayout.Slider("Filter H", config.filterH, 0f, 1f);
        if (newFilterH != config.filterH)
        {
            Undo.RecordObject(config, "Change FilterH");
            config.filterH = newFilterH;
        }

        serializedObject.ApplyModifiedProperties();
    }
}

}
