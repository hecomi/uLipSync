using UnityEditor;

namespace uLipSync
{
    
internal class EditorPrefsStr
{
    public const string DisplayWaveformOnTimeline = "uLipSync-Timeline-DisplayWaveform";
}

public static class Preference
{
    public static bool displayWaveformOnTimeline
    {
        get => EditorPrefs.GetBool(EditorPrefsStr.DisplayWaveformOnTimeline, true);
        set => EditorPrefs.SetBool(EditorPrefsStr.DisplayWaveformOnTimeline, value);
    }
}

public class PreferenceProvider : SettingsProvider
{
    const string PreferencePath = "Preferences/uLipSync";
    const int LabelWidth = 200;

    PreferenceProvider(string path, SettingsScope scopes) : base(path, scopes)
    {
    }

    public override void OnGUI(string searchContext)
    {
        EditorGUILayout.Separator();
        
        var defaultLabelWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = LabelWidth;
        
        ++EditorGUI.indentLevel;
        
        {
            bool current = Preference.displayWaveformOnTimeline;
            bool result = EditorGUILayout.Toggle("Display Waveform On Timeline", current);
            if (current != result)
            {
                Preference.displayWaveformOnTimeline = result;
            }
        }
        
        --EditorGUI.indentLevel;
        
        EditorGUIUtility.labelWidth = defaultLabelWidth;
    }

    [SettingsProvider]
    public static SettingsProvider CreateSettingProvider()
    {
        return new PreferenceProvider(PreferencePath, SettingsScope.User);
    }
}

}