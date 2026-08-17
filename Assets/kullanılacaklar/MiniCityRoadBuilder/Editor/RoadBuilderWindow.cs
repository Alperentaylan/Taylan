using System.IO;
using Alperen.CityRoads;
using UnityEditor;
using UnityEngine;

namespace Alperen.CityRoads.Editor
{
    public class RoadBuilderWindow : EditorWindow
    {
        private const string PackageRoot = "Assets/MiniCityRoadBuilder";
        private const string MaterialsFolder = PackageRoot + "/Materials";
        private const string AsphaltPath = MaterialsFolder + "/Asphalt.mat";
        private const string MarkingPath = MaterialsFolder + "/RoadMarking.mat";
        private const string SidewalkPath = MaterialsFolder + "/Sidewalk.mat";

        private RoadPreset selectedPreset = RoadPreset.Sokak;
        private bool createClosedLoop;
        private float placementHeight;
        private Material asphaltMaterial;
        private Material markingMaterial;
        private Material sidewalkMaterial;

        private RoadPath activeRoad;
        private bool isDrawing;
        private Vector3 previewPoint;
        private bool hasPreviewPoint;

        [MenuItem("Tools/Mini City/Yol Çizici")]
        public static void Open()
        {
            RoadBuilderWindow window = GetWindow<RoadBuilderWindow>();
            window.titleContent = new GUIContent("Yol Çizici");
            window.minSize = new Vector2(340f, 430f);
            window.Show();
        }

        private void OnEnable()
        {
            EnsureDefaultMaterials();
            LoadDefaultMaterials();
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("MINI CITY ROAD BUILDER", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Yol türünü seç, çizimi başlat ve Scene ekranına tıklayarak yol noktalarını yerleştir.",
                MessageType.Info);

            EditorGUILayout.Space(6);

            selectedPreset = (RoadPreset)EditorGUILayout.EnumPopup(
                new GUIContent("Yol Türü"),
                selectedPreset);

            createClosedLoop = EditorGUILayout.Toggle(
                new GUIContent("Kapalı Döngü", "Çevre yolu gibi sonu başa bağlanan yollar."),
                createClosedLoop);

            placementHeight = EditorGUILayout.FloatField(
                new GUIContent("Boş Zeminde Yükseklik"),
                placementHeight);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Materyaller", EditorStyles.boldLabel);

            asphaltMaterial = (Material)EditorGUILayout.ObjectField(
                "Asfalt",
                asphaltMaterial,
                typeof(Material),
                false);

            markingMaterial = (Material)EditorGUILayout.ObjectField(
                "Şerit Çizgisi",
                markingMaterial,
                typeof(Material),
                false);

            sidewalkMaterial = (Material)EditorGUILayout.ObjectField(
                "Kaldırım",
                sidewalkMaterial,
                typeof(Material),
                false);

            EditorGUILayout.Space(10);

            GUI.enabled = !isDrawing;

            if (GUILayout.Button("YENİ YOL ÇİZMEYE BAŞLA", GUILayout.Height(42)))
            {
                BeginDrawing();
            }

            GUI.enabled = isDrawing;

            if (GUILayout.Button("YOLU BİTİR  (ENTER)", GUILayout.Height(34)))
            {
                FinishDrawing();
            }

            if (GUILayout.Button("SON NOKTAYI SİL  (BACKSPACE)", GUILayout.Height(30)))
            {
                RemoveLastPoint();
            }

            GUI.enabled = true;

            EditorGUILayout.Space(8);

            if (isDrawing && activeRoad != null)
            {
                EditorGUILayout.HelpBox(
                    "Çizim açık: " + activeRoad.ControlPoints.Count +
                    " nokta. Scene ekranında sol tıkla nokta ekle.",
                    MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Kontroller:\n" +
                    "• Sol tık: Yol noktası ekle\n" +
                    "• Enter: Yolu bitir\n" +
                    "• Backspace / Sağ tık: Son noktayı sil\n" +
                    "• Escape: Çizimi bitir\n" +
                    "• Alt + fare: Scene kamerasını normal kullan",
                    MessageType.None);
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Varsayılan Materyalleri Yeniden Oluştur"))
            {
                CreateOrReplaceDefaultMaterials();
                LoadDefaultMaterials();
            }
        }

        private void BeginDrawing()
        {
            if (isDrawing)
            {
                FinishDrawing();
            }

            GameObject roadObject = new GameObject("Road_" + selectedPreset);
            Undo.RegisterCreatedObjectUndo(roadObject, "Yeni yol oluştur");

            activeRoad = roadObject.AddComponent<RoadPath>();
            activeRoad.ApplyPreset(selectedPreset);
            activeRoad.ClosedLoop = createClosedLoop;
            activeRoad.SetMaterials(
                asphaltMaterial,
                markingMaterial,
                sidewalkMaterial);

            Selection.activeGameObject = roadObject;
            isDrawing = true;
            hasPreviewPoint = false;

            SceneView.lastActiveSceneView?.Focus();
            SceneView.RepaintAll();
            Repaint();
        }

        private void FinishDrawing()
        {
            if (!isDrawing)
            {
                return;
            }

            if (activeRoad != null && activeRoad.ControlPoints.Count < 2)
            {
                GameObject invalidObject = activeRoad.gameObject;
                activeRoad = null;
                isDrawing = false;
                Undo.DestroyObjectImmediate(invalidObject);
                Debug.LogWarning("Yol oluşturmak için en az iki nokta gerekir.");
            }
            else
            {
                if (activeRoad != null)
                {
                    activeRoad.Rebuild();
                    EditorUtility.SetDirty(activeRoad);
                }

                activeRoad = null;
                isDrawing = false;
            }

            hasPreviewPoint = false;
            SceneView.RepaintAll();
            Repaint();
        }

        private void RemoveLastPoint()
        {
            if (!isDrawing || activeRoad == null)
            {
                return;
            }

            Undo.RecordObject(activeRoad, "Yol noktasını sil");
            activeRoad.RemoveLastPoint();
            EditorUtility.SetDirty(activeRoad);
            SceneView.RepaintAll();
            Repaint();
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (!isDrawing || activeRoad == null)
            {
                return;
            }

            Event currentEvent = Event.current;

            if (!currentEvent.alt)
            {
                HandleUtility.AddDefaultControl(
                    GUIUtility.GetControlID(FocusType.Passive));
            }

            hasPreviewPoint = TryGetMouseWorldPoint(
                currentEvent.mousePosition,
                out previewPoint);

            DrawSceneOverlay();

            if (hasPreviewPoint && activeRoad.ControlPoints.Count > 0)
            {
                Vector3 lastPoint = activeRoad.transform.TransformPoint(
                    activeRoad.ControlPoints[activeRoad.ControlPoints.Count - 1]);

                Handles.color = Color.cyan;
                Handles.DrawAAPolyLine(4f, lastPoint, previewPoint);
                Handles.SphereHandleCap(
                    0,
                    previewPoint,
                    Quaternion.identity,
                    0.6f,
                    EventType.Repaint);
            }

            if (currentEvent.type == EventType.MouseDown &&
                currentEvent.button == 0 &&
                !currentEvent.alt &&
                hasPreviewPoint)
            {
                Undo.RecordObject(activeRoad, "Yol noktası ekle");
                activeRoad.AddPointWorld(previewPoint);
                EditorUtility.SetDirty(activeRoad);
                currentEvent.Use();
                SceneView.RepaintAll();
                Repaint();
            }

            if (currentEvent.type == EventType.MouseDown &&
                currentEvent.button == 1 &&
                !currentEvent.alt)
            {
                RemoveLastPoint();
                currentEvent.Use();
            }

            if (currentEvent.type == EventType.KeyDown)
            {
                if (currentEvent.keyCode == KeyCode.Return ||
                    currentEvent.keyCode == KeyCode.KeypadEnter ||
                    currentEvent.keyCode == KeyCode.Escape)
                {
                    FinishDrawing();
                    currentEvent.Use();
                }
                else if (currentEvent.keyCode == KeyCode.Backspace ||
                         currentEvent.keyCode == KeyCode.Delete)
                {
                    RemoveLastPoint();
                    currentEvent.Use();
                }
            }

            sceneView.Repaint();
        }

        private bool TryGetMouseWorldPoint(Vector2 mousePosition, out Vector3 point)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(
                ray,
                10000f,
                ~0,
                QueryTriggerInteraction.Ignore);

            if (hits != null && hits.Length > 0)
            {
                System.Array.Sort(
                    hits,
                    delegate (RaycastHit a, RaycastHit b)
                    {
                        return a.distance.CompareTo(b.distance);
                    });

                for (int i = 0; i < hits.Length; i++)
                {
                    RoadPath hitRoad = hits[i].collider.GetComponentInParent<RoadPath>();

                    if (hitRoad == null || hitRoad == activeRoad)
                    {
                        point = hits[i].point;
                        return true;
                    }
                }
            }

            Plane groundPlane = new Plane(
                Vector3.up,
                new Vector3(0f, placementHeight, 0f));

            float enter;

            if (groundPlane.Raycast(ray, out enter))
            {
                point = ray.GetPoint(enter);
                return true;
            }

            point = Vector3.zero;
            return false;
        }

        private void DrawSceneOverlay()
        {
            Handles.BeginGUI();

            GUILayout.BeginArea(
                new Rect(12f, 12f, 310f, 105f),
                "Yol Çizimi",
                GUI.skin.window);

            GUILayout.Label("Sol tık: Nokta ekle");
            GUILayout.Label("Enter: Bitir   |   Backspace: Geri al");
            GUILayout.Label(
                activeRoad != null
                    ? "Nokta sayısı: " + activeRoad.ControlPoints.Count
                    : string.Empty);

            GUILayout.EndArea();
            Handles.EndGUI();
        }

        private static void EnsureDefaultMaterials()
        {
            if (AssetDatabase.LoadAssetAtPath<Material>(AsphaltPath) == null ||
                AssetDatabase.LoadAssetAtPath<Material>(MarkingPath) == null ||
                AssetDatabase.LoadAssetAtPath<Material>(SidewalkPath) == null)
            {
                CreateOrReplaceDefaultMaterials();
            }
        }

        private static void CreateOrReplaceDefaultMaterials()
        {
            EnsureFolder("Assets", "MiniCityRoadBuilder");
            EnsureFolder(PackageRoot, "Materials");

            Shader shader = FindCompatibleLitShader();

            CreateOrReplaceMaterial(
                AsphaltPath,
                shader,
                new Color(0.12f, 0.12f, 0.12f, 1f),
                0.18f);

            CreateOrReplaceMaterial(
                MarkingPath,
                shader,
                new Color(0.95f, 0.95f, 0.88f, 1f),
                0.1f);

            CreateOrReplaceMaterial(
                SidewalkPath,
                shader,
                new Color(0.45f, 0.45f, 0.45f, 1f),
                0.25f);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void CreateOrReplaceMaterial(
            string path,
            Shader shader,
            Color color,
            float smoothness)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.shader = shader;
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", smoothness);
            }

            EditorUtility.SetDirty(material);
        }

        private static Shader FindCompatibleLitShader()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");

            if (shader == null)
            {
                shader = Shader.Find("HDRP/Lit");
            }

            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            if (shader == null)
            {
                shader = Shader.Find("Diffuse");
            }

            return shader;
        }

        private static void EnsureFolder(string parent, string child)
        {
            string fullPath = parent + "/" + child;

            if (!AssetDatabase.IsValidFolder(fullPath))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private void LoadDefaultMaterials()
        {
            asphaltMaterial = AssetDatabase.LoadAssetAtPath<Material>(AsphaltPath);
            markingMaterial = AssetDatabase.LoadAssetAtPath<Material>(MarkingPath);
            sidewalkMaterial = AssetDatabase.LoadAssetAtPath<Material>(SidewalkPath);
        }
    }
}
