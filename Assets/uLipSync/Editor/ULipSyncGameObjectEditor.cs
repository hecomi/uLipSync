using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace uLipSync
{
    [CustomEditor(typeof(ULipSyncGameObject))]
    public class ULipSyncGameObjectEditor : Editor
    {
        private ULipSyncGameObject LipSyncGameObject => target as ULipSyncGameObject;
        private ReorderableList _reorderableList;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (EditorUtil.Foldout("LipSync Update Method", true))
            {
                ++EditorGUI.indentLevel;
                EditorUtil.DrawProperty(serializedObject, nameof(LipSyncGameObject.updateMethod));
                --EditorGUI.indentLevel;
                EditorGUILayout.Separator();
            }

            if (EditorUtil.Foldout("Parameters", true))
            {
                ++EditorGUI.indentLevel;
                DrawParameters();
                --EditorGUI.indentLevel;
                EditorGUILayout.Separator();
            }


            if (EditorUtil.Foldout("GameObjects", true))
            {
                ++EditorGUI.indentLevel;
                DrawGameObjectReorderableList();
                --EditorGUI.indentLevel;
                EditorGUILayout.Separator();
            }

            serializedObject.ApplyModifiedProperties();
        }

        void DrawGameObjectReorderableList()
        {
            if (_reorderableList == null)
            {
                _reorderableList = new ReorderableList(LipSyncGameObject.gameObjects, typeof(MfccData));
                _reorderableList.drawHeaderCallback = rect =>
                {
                    rect.xMin -= EditorGUI.indentLevel * 12f;
                    EditorGUI.LabelField(rect, "Phoneme - Game Object Field");
                };
                _reorderableList.draggable = true;
                _reorderableList.drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.BeginVertical();
                    var itemRect = new Rect(rect);
                    DrawGameObjectItemList(itemRect, index);
                    EditorGUILayout.EndVertical();
                    /*
                    var objectRect = new Rect(rect);
                    objectRect.xMin = objectRect.xMax - 64f;
                    objectRect.height = 64f;
                    DrawGameObject(objectRect, index);*/
                    EditorGUILayout.EndHorizontal();
                };
                _reorderableList.elementHeightCallback = index => 40f;
            }

            EditorGUILayout.Separator();
            EditorGUILayout.BeginHorizontal();
            var indent = EditorGUI.indentLevel * 12f;
            EditorGUILayout.Space(indent, false);
            EditorGUILayout.BeginVertical();
            _reorderableList.DoLayoutList();
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        void DrawGameObjectItemList(Rect rect, int index)
        {
            rect.y += 2;
            rect.height = EditorGUIUtility.singleLineHeight;
            ULipSyncGameObject.GameObjectInfo item = LipSyncGameObject.gameObjects[index];
            var singleLineHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            var phonemeLabel = string.IsNullOrEmpty(item.phoneme) ? "Default" : "Phoneme";
            item.phoneme = EditorGUI.TextField(rect, phonemeLabel, item.phoneme);

            rect.y += singleLineHeight;

            GameObject newGameObject =
                (GameObject)EditorGUI.ObjectField(rect, "Game Object", item.gameObject, typeof(GameObject), true);
            if (newGameObject != item.gameObject)
            {
                Undo.RecordObject(target, "Change Game Object");
                item.gameObject = newGameObject;
            }
        }


        void DrawParameters()
        {
            EditorUtil.DrawProperty(serializedObject, nameof(LipSyncGameObject.minVolume));
            EditorUtil.DrawProperty(serializedObject, nameof(LipSyncGameObject.minDuration));
        }
    }
}
