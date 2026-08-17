using UnityEditor;
using UnityEngine;

public static class TopluMeshColliderEkle
{
    [MenuItem("Tools/Collider/Seçili Modele Mesh Collider Ekle")]
    private static void ColliderEkle()
    {
        GameObject[] seciliNesneler = Selection.gameObjects;

        if (seciliNesneler.Length == 0)
        {
            Debug.LogWarning("Önce Hierarchy'den bir model seç.");
            return;
        }

        int eklenenColliderSayisi = 0;

        foreach (GameObject anaNesne in seciliNesneler)
        {
            MeshFilter[] meshFilterlar =
                anaNesne.GetComponentsInChildren<MeshFilter>(true);

            foreach (MeshFilter meshFilter in meshFilterlar)
            {
                if (meshFilter.sharedMesh == null)
                    continue;

                MeshCollider meshCollider =
                    meshFilter.GetComponent<MeshCollider>();

                if (meshCollider == null)
                {
                    meshCollider =
                        Undo.AddComponent<MeshCollider>(
                            meshFilter.gameObject
                        );

                    eklenenColliderSayisi++;
                }

                meshCollider.sharedMesh = meshFilter.sharedMesh;

                // Sabit binalarda kapalý olmalý.
                meshCollider.convex = false;

                // Karakter collider'ýn içinden geçmesin.
                meshCollider.isTrigger = false;

                EditorUtility.SetDirty(meshCollider);
            }

            GameObjectUtility.SetStaticEditorFlags(
                anaNesne,
                StaticEditorFlags.BatchingStatic |
                StaticEditorFlags.OccluderStatic |
                StaticEditorFlags.OccludeeStatic
            );
        }

        Debug.Log(
            "Ýþlem tamamlandý. Eklenen Mesh Collider: " +
            eklenenColliderSayisi
        );
    }

    [MenuItem("Tools/Collider/Seçili Modelden Mesh Colliderlarý Sil")]
    private static void ColliderlariSil()
    {
        GameObject[] seciliNesneler = Selection.gameObjects;

        if (seciliNesneler.Length == 0)
        {
            Debug.LogWarning("Önce Hierarchy'den bir model seç.");
            return;
        }

        int silinenColliderSayisi = 0;

        foreach (GameObject anaNesne in seciliNesneler)
        {
            MeshCollider[] colliderlar =
                anaNesne.GetComponentsInChildren<MeshCollider>(true);

            foreach (MeshCollider collider in colliderlar)
            {
                Undo.DestroyObjectImmediate(collider);
                silinenColliderSayisi++;
            }
        }

        Debug.Log(
            "Silinen Mesh Collider: " +
            silinenColliderSayisi
        );
    }
}