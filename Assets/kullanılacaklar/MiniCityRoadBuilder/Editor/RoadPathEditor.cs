using Alperen.CityRoads;
using UnityEditor;
using UnityEngine;

namespace Alperen.CityRoads.Editor
{
    [CustomEditor(typeof(RoadPath))]
    public class RoadPathEditor : UnityEditor.Editor
    {
        private RoadPath road;

        private void OnEnable()
        {
            road = (RoadPath)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Hızlı İşlemler", EditorStyles.boldLabel);

            if (GUILayout.Button("Yolu Yeniden Oluştur"))
            {
                road.Rebuild();
                EditorUtility.SetDirty(road);
            }

            if (GUILayout.Button("Seçili Yol Türünün Ölçülerini Uygula"))
            {
                Undo.RecordObject(road, "Yol ayarlarını uygula");
                road.ApplyPreset(road.Preset);
                EditorUtility.SetDirty(road);
            }

            if (GUILayout.Button("Noktaların Sırasını Ters Çevir"))
            {
                Undo.RecordObject(road, "Yol noktalarını ters çevir");
                road.ReversePoints();
                EditorUtility.SetDirty(road);
            }

            if (GUILayout.Button("Bütün Noktaları Temizle"))
            {
                if (EditorUtility.DisplayDialog(
                    "Yol Noktalarını Temizle",
                    "Bu yolun bütün noktaları silinsin mi?",
                    "Sil",
                    "İptal"))
                {
                    Undo.RecordObject(road, "Yol noktalarını temizle");
                    road.ClearPoints();
                    EditorUtility.SetDirty(road);
                }
            }

            EditorGUILayout.HelpBox(
                "Scene ekranında mavi noktaları taşıyarak virajı ve yol şeklini düzenleyebilirsin.",
                MessageType.Info);
        }

        private void OnSceneGUI()
        {
            if (road == null || road.ControlPoints == null)
            {
                return;
            }

            for (int i = 0; i < road.ControlPoints.Count; i++)
            {
                Vector3 worldPoint = road.transform.TransformPoint(
                    road.ControlPoints[i]);

                Handles.color = Color.cyan;
                Handles.Label(
                    worldPoint + Vector3.up * 0.55f,
                    "P" + (i + 1));

                EditorGUI.BeginChangeCheck();

                Vector3 movedWorldPoint = Handles.PositionHandle(
                    worldPoint,
                    Quaternion.identity);

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(road, "Yol noktasını taşı");
                    road.ControlPoints[i] = road.transform.InverseTransformPoint(
                        movedWorldPoint);
                    road.Rebuild();
                    EditorUtility.SetDirty(road);
                }
            }
        }
    }
}
