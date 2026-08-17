using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class GLS580DireksiyonAyirV5_KonumSabit : MonoBehaviour
{
    [Header("KAYNAK")]
    [Tooltip("Hierarchy'deki Static_Body objesinin MeshFilter'i.")]
    public MeshFilter sourceMesh;

    [Header("DIREKSIYON MERKEZI")]
    [Tooltip("Direksiyonun TAM gobegindeki SteeringCenterMarker.")]
    public Transform steeringCenterMarker;

    [Header("SECIM KUTUSU")]
    [Tooltip("Kutu sadece direksiyonun tamamini kapsasin.")]
    public Vector3 selectionSize = new Vector3(0.52f, 0.52f, 0.18f);

    [Range(0f, 0.05f)]
    public float tolerance = 0.01f;

    [Header("OLUSACAK")]
    public string steeringRootName = "SteeringWheel";
    public string steeringMeshName = "SteeringWheel_Mesh";
    public string leftGripName = "LeftHandGrip";
    public string rightGripName = "RightHandGrip";

#if UNITY_EDITOR

    private void OnDrawGizmosSelected()
    {
        if (steeringCenterMarker == null)
            return;

        Matrix4x4 old = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(
            steeringCenterMarker.position,
            steeringCenterMarker.rotation,
            Vector3.one);

        Gizmos.DrawWireCube(Vector3.zero, selectionSize);
        Gizmos.matrix = old;
    }

    [ContextMenu("DIREKSIYONU AYIR - KONUMU SABIT TUT")]
    public void Ayir()
    {
        if (sourceMesh == null)
        {
            Debug.LogError("V5: Source Mesh bos. Static_Body'yi ver.", this);
            return;
        }

        if (steeringCenterMarker == null)
        {
            Debug.LogError("V5: Steering Center Marker bos.", this);
            return;
        }

        if (sourceMesh.sharedMesh == null)
        {
            Debug.LogError("V5: Source Mesh'te mesh yok.", sourceMesh);
            return;
        }

        if (!ReadWriteAc())
            return;

        Mesh src = sourceMesh.sharedMesh;

        if (!src.isReadable)
        {
            Debug.LogError("V5: Mesh okunamiyor.", sourceMesh);
            return;
        }

        Vector3[] vertices = src.vertices;

        // Triangle'lari marker kutusuna gore sec.
        List<List<int>> selectedPerSubmesh = new List<List<int>>();
        List<List<int>> bodyPerSubmesh = new List<List<int>>();

        HashSet<int> selectedVertices = new HashSet<int>();
        int selectedTriangleCount = 0;

        for (int s = 0; s < src.subMeshCount; s++)
        {
            int[] tris = src.GetTriangles(s);

            List<int> steeringTris = new List<int>();
            List<int> bodyTris = new List<int>();

            for (int i = 0; i + 2 < tris.Length; i += 3)
            {
                int a = tris[i];
                int b = tris[i + 1];
                int c = tris[i + 2];

                Vector3 wa = sourceMesh.transform.TransformPoint(vertices[a]);
                Vector3 wb = sourceMesh.transform.TransformPoint(vertices[b]);
                Vector3 wc = sourceMesh.transform.TransformPoint(vertices[c]);

                Vector3 centerWorld = (wa + wb + wc) / 3f;
                Vector3 markerLocal =
                    steeringCenterMarker.InverseTransformPoint(centerWorld);

                if (KutuIcinde(markerLocal))
                {
                    steeringTris.Add(a);
                    steeringTris.Add(b);
                    steeringTris.Add(c);

                    selectedVertices.Add(a);
                    selectedVertices.Add(b);
                    selectedVertices.Add(c);

                    selectedTriangleCount++;
                }
                else
                {
                    bodyTris.Add(a);
                    bodyTris.Add(b);
                    bodyTris.Add(c);
                }
            }

            selectedPerSubmesh.Add(steeringTris);
            bodyPerSubmesh.Add(bodyTris);
        }

        if (selectedTriangleCount < 50)
        {
            Debug.LogError(
                "V5: Kutuda yeterli direksiyon geometrisi yok. Triangle = " +
                selectedTriangleCount +
                ". Kutuyu biraz buyut.",
                this);
            return;
        }

        // ==========================================================
        // BODY MESH
        // ==========================================================
        Mesh bodyMesh = Instantiate(src);
        bodyMesh.name = src.name + "_NO_STEERING_V5";

        for (int s = 0; s < src.subMeshCount; s++)
            bodyMesh.SetTriangles(bodyPerSubmesh[s], s, false);

        bodyMesh.RecalculateBounds();

        // ==========================================================
        // STEERING MESH
        //
        // EN ONEMLI FARK:
        // Vertex koordinatlarini HIC DEGISTIRMIYORUZ.
        // Yani direksiyon mesh'i Static_Body ile ayni local koordinatlarda kalir.
        // SteeringWheel pivot parent'i marker'da olacak.
        // Altindaki SteeringWheel_Mesh child'i -pivotLocal kadar geri kaydirilir.
        // Boylece ilk karede dunya konumu %100 ayni kalir.
        // ==========================================================
        Mesh steeringMesh = Instantiate(src);
        steeringMesh.name = "GLS580_STEERING_WHEEL_V5";

        for (int s = 0; s < src.subMeshCount; s++)
            steeringMesh.SetTriangles(selectedPerSubmesh[s], s, false);

        steeringMesh.RecalculateBounds();

        const string folder = "Assets/GLS580_Generated";

        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets", "GLS580_Generated");

        string bodyPath = AssetDatabase.GenerateUniqueAssetPath(
            folder + "/" + bodyMesh.name + ".asset");

        string wheelPath = AssetDatabase.GenerateUniqueAssetPath(
            folder + "/" + steeringMesh.name + ".asset");

        AssetDatabase.CreateAsset(bodyMesh, bodyPath);
        AssetDatabase.CreateAsset(steeringMesh, wheelPath);
        AssetDatabase.SaveAssets();

        Undo.RecordObject(sourceMesh, "GLS580 Steering V5");
        sourceMesh.sharedMesh = bodyMesh;

        Transform old = sourceMesh.transform.Find(steeringRootName);
        if (old != null)
            Undo.DestroyObjectImmediate(old.gameObject);

        // Marker'in sourceMesh LOCAL pozisyonu.
        Vector3 pivotLocal =
            sourceMesh.transform.InverseTransformPoint(
                steeringCenterMarker.position);

        // ROOT = sadece pivot.
        GameObject root = new GameObject(steeringRootName);
        Undo.RegisterCreatedObjectUndo(root, "Create SteeringWheel V5");

        root.transform.SetParent(sourceMesh.transform, false);
        root.transform.localPosition = pivotLocal;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;

        // MESH CHILD:
        // Source mesh vertexleri sourceMesh local koordinatinda.
        // Root pivotLocal kadar ileri geldigi icin child'i -pivotLocal geri al.
        GameObject meshGO = new GameObject(steeringMeshName);
        meshGO.transform.SetParent(root.transform, false);
        meshGO.transform.localPosition = -pivotLocal;
        meshGO.transform.localRotation = Quaternion.identity;
        meshGO.transform.localScale = Vector3.one;

        MeshFilter mf = meshGO.AddComponent<MeshFilter>();
        mf.sharedMesh = steeringMesh;

        MeshRenderer mr = meshGO.AddComponent<MeshRenderer>();

        MeshRenderer sourceRenderer =
            sourceMesh.GetComponent<MeshRenderer>();

        if (sourceRenderer != null)
        {
            mr.sharedMaterials = sourceRenderer.sharedMaterials;
            mr.shadowCastingMode = sourceRenderer.shadowCastingMode;
            mr.receiveShadows = sourceRenderer.receiveShadows;
            mr.lightProbeUsage = sourceRenderer.lightProbeUsage;
            mr.reflectionProbeUsage = sourceRenderer.reflectionProbeUsage;
        }

        // Grip objeleri ROOT'un child'i.
        // World'de direksiyonun 9 ve 3 tarafina yaklasik yerlestiriyoruz.
        Bounds selectedWorldBounds = CalculateSelectedWorldBounds(
            vertices,
            selectedVertices);

        Vector3 gripCenterWorld = steeringCenterMarker.position;

        Vector3 leftWorld = new Vector3(
            selectedWorldBounds.min.x,
            gripCenterWorld.y,
            gripCenterWorld.z);

        Vector3 rightWorld = new Vector3(
            selectedWorldBounds.max.x,
            gripCenterWorld.y,
            gripCenterWorld.z);

        GameObject left = new GameObject(leftGripName);
        left.transform.SetParent(root.transform, false);
        left.transform.position = leftWorld;
        left.transform.rotation = steeringCenterMarker.rotation;

        GameObject right = new GameObject(rightGripName);
        right.transform.SetParent(root.transform, false);
        right.transform.position = rightWorld;
        right.transform.rotation = steeringCenterMarker.rotation;

        Selection.activeGameObject = root;

        Debug.Log(
            "V5 TAMAM: Direksiyon ayrildi ve eski dunya konumu korunuyor. " +
            "SteeringWheel ROOT pivot olarak marker'da, gorunen mesh SteeringWheel_Mesh child'inda. " +
            "Triangle = " + selectedTriangleCount,
            root);
    }

    private Bounds CalculateSelectedWorldBounds(
        Vector3[] srcVertices,
        HashSet<int> selectedVertices)
    {
        bool first = true;
        Bounds b = new Bounds();

        foreach (int i in selectedVertices)
        {
            Vector3 w =
                sourceMesh.transform.TransformPoint(srcVertices[i]);

            if (first)
            {
                b = new Bounds(w, Vector3.zero);
                first = false;
            }
            else
            {
                b.Encapsulate(w);
            }
        }

        return b;
    }

    private bool KutuIcinde(Vector3 p)
    {
        Vector3 half =
            selectionSize * 0.5f +
            Vector3.one * tolerance;

        return
            Mathf.Abs(p.x) <= half.x &&
            Mathf.Abs(p.y) <= half.y &&
            Mathf.Abs(p.z) <= half.z;
    }

    private bool ReadWriteAc()
    {
        Mesh current = sourceMesh.sharedMesh;
        string path = AssetDatabase.GetAssetPath(current);

        if (string.IsNullOrEmpty(path))
            return current.isReadable;

        ModelImporter importer =
            AssetImporter.GetAtPath(path) as ModelImporter;

        if (importer == null)
            return current.isReadable;

        if (!importer.isReadable)
        {
            importer.isReadable = true;
            importer.SaveAndReimport();
        }

        return sourceMesh.sharedMesh != null &&
               sourceMesh.sharedMesh.isReadable;
    }
#endif
}