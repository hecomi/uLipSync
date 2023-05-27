using UnityEngine;
using UnityEngine.Timeline;
using UnityEditor;
using UnityEditor.Timeline;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.IO;

namespace uLipSync
{

public class TimelineSetupHeplerWindow : ScriptableWizard
{
    SerializedObject _serializedObject;

    [SerializeField]
    TimelineAsset timeline;

    [SerializeField]
    bool foldoutTimeline = true;

    [SerializeField]
    bool foldoutLipSync = true;

    [SerializeField]
    int audioTrackIndex = 0;

    [SerializeField]
    int lipsyncTrackIndex = 0;

    [SerializeField]
    Profile profile;

    [SerializeField]
    string outputDirectory = "";

    [SerializeField]
    bool useExistingAsset = false;

    StringBuilder _message = new StringBuilder();

    [MenuItem("Window/uLipSync/Timeline Setup Helper")]
    static void Open()
    {
        var path = DisplayWizard<TimelineSetupHeplerWindow>(
            "uLipSync Timeline Setup Helper", 
            "",
            "Create Clips");
    }

    protected override bool DrawWizardGUI()
    {
        _message.Clear();

        if (_serializedObject == null)
        {
            _serializedObject = new SerializedObject(this);
        }

        _serializedObject.Update();

        EditorGUILayout.Separator();

        var style = new GUIStyle(EditorStyles.foldoutHeader);
        style.fixedWidth = EditorGUIUtility.currentViewWidth;

        foldoutTimeline = EditorGUILayout.Foldout(foldoutTimeline, "Timeline", style);
        if (foldoutTimeline)
        {
            EditorGUILayout.Separator();
            ++EditorGUI.indentLevel;
            DrawTimeline();
            --EditorGUI.indentLevel;
            EditorGUILayout.Separator();
        }

        foldoutLipSync = EditorGUILayout.Foldout(foldoutLipSync, "LipSync", style);
        if (foldoutLipSync)
        {
            EditorGUILayout.Separator();
            ++EditorGUI.indentLevel;
            DrawLipSync();
            --EditorGUI.indentLevel;
            EditorGUILayout.Separator();
        }

        DrawMessage();

        _serializedObject.ApplyModifiedProperties();

        return true;
    }

    void OnWizardOtherButton()
    {
        if (!timeline) return;

        var audioTrack = GetTracks<AudioTrack>()[audioTrackIndex];
        var lipsyncTrack = GetTracks<Timeline.uLipSyncTrack>()[lipsyncTrackIndex];

        foreach (var lipsyncClip in lipsyncTrack.GetClips())
        {
            lipsyncTrack.DeleteClip(lipsyncClip);
        }

        List<BakedData> bakedDataList = new List<BakedData>();
        if (useExistingAsset)
        {
            bakedDataList = AssetDatabase.FindAssets("t:uLipSync.BakedData")
                .Select(x => AssetDatabase.GUIDToAssetPath(x))
                .Select(x => AssetDatabase.LoadAssetAtPath<BakedData>(x))
                .ToList();
        }

        EditorUtil.CreateOutputDirectory(outputDirectory);

        foreach (var audioClip in audioTrack.GetClips())
        {
            var audioClipAsset = audioClip.asset as AudioPlayableAsset;
            if (!audioClipAsset || !audioClipAsset.clip) continue;

            var lipsyncClip = lipsyncTrack.CreateClip<Timeline.uLipSyncClip>();
            lipsyncClip.displayName = audioClip.displayName;
            lipsyncClip.start = audioClip.start;
            lipsyncClip.duration = audioClip.duration;
            lipsyncClip.clipIn = audioClip.clipIn;
            lipsyncClip.timeScale = audioClip.timeScale;

            var lipsyncClipAsset = lipsyncClip.asset as Timeline.uLipSyncClip;

            if (useExistingAsset)
            {
                foreach (var data in bakedDataList)
                {
                    if (data.audioClip != audioClipAsset.clip) continue;
                    lipsyncClipAsset.bakedData = data;
                    break;
                }
            }

            if (!lipsyncClipAsset.bakedData)
            {
                var data = ScriptableObject.CreateInstance<BakedData>();
                data.profile = profile;
                data.audioClip = audioClipAsset.clip;
                data.name = data.audioClip.name;

                var editor = (BakedDataEditor)Editor.CreateEditor(data, typeof(BakedDataEditor));
                editor.Bake();
                Debug.Log("hoge");

                var path = Path.Combine(outputDirectory, data.name + ".asset");
                AssetDatabase.DeleteAsset(path); 
                AssetDatabase.CreateAsset(data, path);

                lipsyncClipAsset.bakedData = data;
            }
        }

        AssetDatabase.SaveAssets();

        EditorUtility.SetDirty(timeline);
        TimelineEditor.Refresh(RefreshReason.ContentsAddedOrRemoved);

        Selection.activeObject = timeline;
    }

    List<TrackAsset> GetTracks<Type>()
    {
        var list = new List<TrackAsset>();
        if (!timeline) return list;

        foreach (var track in timeline.GetOutputTracks())
        {
            if (typeof(Type) != track.GetType()) continue;
            list.Add(track);
        }
        return list;
    }

    void DrawTimeline()
    {
        EditorUtil.DrawProperty(_serializedObject, nameof(timeline));

        if (!timeline)
        {
            _message.Append("* Timeline aseet is not set.");
        }

        var audioTrackNames = GetTracks<AudioTrack>();
        audioTrackIndex = EditorGUILayout.Popup(
            "Audio Track", 
            audioTrackIndex, 
            audioTrackNames.Select((x, i) => $"{i + 1}. {x.name}").ToArray());

        var lipsyncTrackNames = GetTracks<Timeline.uLipSyncTrack>();
        lipsyncTrackIndex = EditorGUILayout.Popup(
            "uLipSync Track", 
            lipsyncTrackIndex, 
            lipsyncTrackNames.Select((x, i) => $"{i + 1}. {x.name}").ToArray());
    }

    void DrawLipSync()
    {
        EditorUtil.DrawProperty(_serializedObject, nameof(profile));
        EditorUtil.DrawProperty(_serializedObject, nameof(useExistingAsset));

        EditorGUILayout.BeginHorizontal();
        EditorUtil.DrawProperty(_serializedObject, nameof(outputDirectory));
        if (GUILayout.Button("...", GUILayout.Width(24)))
        {
            var path = EditorUtility.OpenFolderPanel("uLipSync Timeline Setup Helper Output Directory", Application.dataPath, "BakedData");
            outputDirectory = EditorUtil.GetAssetPath(path);
        }
        EditorGUILayout.EndHorizontal();

        if (!profile)
        {
            if (_message.Length > 0) _message.AppendLine();
            _message.Append("* Profile is not set.");
        }
    }

    void DrawMessage()
    {
        if (_message.Length == 0) return;

        EditorGUILayout.HelpBox(_message.ToString(), MessageType.Warning);
    }
}

}