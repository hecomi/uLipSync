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

        bool doNotChangeAssetValues = 
            config.name == Common.defaultConfig ||
            config.name == Common.calibrationConfig;

        if (doNotChangeAssetValues)
        {
            doNotChangeAssetValues = !EditorUtil.EditorOnlyToggle("Allow Change Default Asset", "Config", false);
        }

        if (doNotChangeAssetValues)
        {
            EditorGUILayout.IntField("Sample Count", config.sampleCount);
            EditorGUILayout.IntField("Lpc Order", config.lpcOrder);
            EditorGUILayout.IntField("Frequency Resolution", config.frequencyResolution);
            EditorGUILayout.IntField("Max Frequency", config.maxFrequency);
            EditorGUILayout.EnumPopup("Window Func", config.windowFunc);
            EditorGUILayout.Toggle("Check Second Derivative", config.checkSecondDerivative);
            EditorGUILayout.Toggle("Check Third Formant", config.checkThirdFormant);
        }
        else
        {
            EditorUtil.DrawProperty(serializedObject, nameof(config.sampleCount));
            EditorUtil.DrawProperty(serializedObject, nameof(config.lpcOrder));
            EditorUtil.DrawProperty(serializedObject, nameof(config.frequencyResolution));
            EditorUtil.DrawProperty(serializedObject, nameof(config.maxFrequency));
            EditorUtil.DrawProperty(serializedObject, nameof(config.windowFunc));
            EditorUtil.DrawProperty(serializedObject, nameof(config.checkSecondDerivative));
            EditorUtil.DrawProperty(serializedObject, nameof(config.checkThirdFormant));
        }

        var newFilterH = EditorGUILayout.Slider("Filter H", config.filterH, 0f, 1f);
        if (!doNotChangeAssetValues && newFilterH != config.filterH)
        {
            Undo.RecordObject(config, "Change FilterH");
            config.filterH = newFilterH;
        }

        if (doNotChangeAssetValues)
        {
            EditorGUILayout.Separator();
            EditorGUILayout.HelpBox(@"Cannot change parameters in a default asset. Please check ""Allow Change Default Asset"" if you really want to change.", MessageType.Info);
        }

        serializedObject.ApplyModifiedProperties();
    }
}

}
