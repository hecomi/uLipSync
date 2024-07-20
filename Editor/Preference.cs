using System;
using UnityEditor;

namespace uLipSync
{
    
internal class EditorPrefsStr
{
    public const string DisplayWaveformOnTimeline = "uLipSync-Timeline-DisplayWaveform";
    public const string MaxWidthOfWaveformTextureOnTimeline = "uLipSync-Timeline-MaxWidthOfWaveformTexture";
}

internal class EditorPrefsDefault
{
    public const int MinWidthOfWaveformTextureOnTimeline = 32;
    public const int MaxWidthOfWaveformTextureOnTimeline = 2048;
}

public static class Preference
{
    public static bool displayWaveformOnTimeline
    {
        get => EditorPrefs.GetBool(EditorPrefsStr.DisplayWaveformOnTimeline, true);
        set => EditorPrefs.SetBool(EditorPrefsStr.DisplayWaveformOnTimeline, value);
    }
    
    public static int minWidthOfWaveformTextureOnTimeline => EditorPrefsDefault.MinWidthOfWaveformTextureOnTimeline;
    
    public static int maxWidthOfWaveformTextureOnTimeline
    {
        get => EditorPrefs.GetInt(EditorPrefsStr.MaxWidthOfWaveformTextureOnTimeline, EditorPrefsDefault.MaxWidthOfWaveformTextureOnTimeline);
        set => EditorPrefs.SetInt(EditorPrefsStr.MaxWidthOfWaveformTextureOnTimeline, value);
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
        
        {
            int current = Preference.maxWidthOfWaveformTextureOnTimeline;
            int result = EditorGUILayout.IntField("Max Width Of Waveform Texture On Timeline", current);
            if (current != result)
            {
                Preference.maxWidthOfWaveformTextureOnTimeline = Math.Clamp(
                    result, 
                    EditorPrefsDefault.MinWidthOfWaveformTextureOnTimeline,
                    EditorPrefsDefault.MaxWidthOfWaveformTextureOnTimeline);
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