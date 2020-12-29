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

        EditorUtil.DrawProperty(serializedObject, nameof(config.lpcOrder));
        EditorUtil.DrawProperty(serializedObject, nameof(config.sampleCount));
        EditorUtil.DrawProperty(serializedObject, nameof(config.checkSecondDerivative));
        EditorUtil.DrawProperty(serializedObject, nameof(config.checkThirdFormant));
        EditorUtil.DrawProperty(serializedObject, nameof(config.filterH));

        serializedObject.ApplyModifiedProperties();
    }
}

}
