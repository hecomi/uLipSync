using System;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Reflection;

namespace uLipSync.Debugging
{

    [CustomEditor(typeof(DebugAudioPlayer))]
    public class DebugAudioPlayerEditor : Editor
    {
        DebugAudioPlayer player => target as DebugAudioPlayer;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawDirectory();
            DrawAudioClips();

            serializedObject.ApplyModifiedProperties();
        }

        void DrawDirectory()
        {
            EditorGUILayout.BeginHorizontal();

            var directory = EditorGUILayout.TextField("Directory", player.directory);
            if (directory != player.directory)
            {
                Undo.RecordObject(target, "Change Directory");
                player.directory = directory;
                EditorUtility.SetDirty(target);
            }

            if (GUILayout.Button("...", EditorStyles.miniButton, GUILayout.Width(24)))
            {
                try
                {
                    var dir = Path.GetDirectoryName(player.directory);
                    dir = EditorUtility.SaveFolderPanel("AudioClip Directory", dir, "");
                    dir = DebugUtil.GetRelativePath(Application.dataPath, dir);
                    player.directory = Path.Combine("Assets", dir);
                }
                catch (System.Exception e)
                {
                    Debug.LogError(e.Message);
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        void DrawAudioClips()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Clips");
            ++EditorGUI.indentLevel;

            var files = Directory.GetFiles(player.directory, "*.wav");
            foreach (var file in files)
            {
                var asset = AssetDatabase.LoadAssetAtPath<AudioClip>(file);
                if (!asset) continue;
                DrawAudioClip(asset);
            }

            --EditorGUI.indentLevel;
        }

        void DrawAudioClip(AudioClip clip)
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(clip.name, GUILayout.Width(100));
            EditorGUILayout.ObjectField(clip, typeof(AudioClip), true);

            if (GUILayout.Button("Play", EditorStyles.miniButton, GUILayout.Width(64)))
            {
                if (Application.isPlaying)
                {
                    Play(clip);
                }
                else
                {
                    PausePreviewClip();
                    PlayPreviewClip(clip);
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        void Play(AudioClip clip)
        {
            player.newClip = clip;
        }

        void PausePreviewClip()
        {
            var audioUtil = typeof(Editor).Assembly.GetType("UnityEditor.AudioUtil");
            var pause = audioUtil.GetMethod("PausePreviewClip", BindingFlags.Static | BindingFlags.Public);
            pause.Invoke(null, null);
        }

        void PlayPreviewClip(AudioClip clip)
        {
            var audioUtil = typeof(Editor).Assembly.GetType("UnityEditor.AudioUtil");
            var play = audioUtil.GetMethod("PlayPreviewClip", BindingFlags.Static | BindingFlags.Public);
            play.Invoke(null, new object[] {clip, 0, null});
        }
    }

}