#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/*
 * Bu dosyayı Assets/Editor klasörüne koy.
 * Unity üst menüsünde Tools > Box Collider Kolay Kurulum menüsü oluşur.
 */
public static class TopluBoxColliderAraci
{
    private const string MENU_KOK =
        "Tools/Box Collider Kolay Kurulum/" +
        "Seçili Her Eşyaya Tek Otomatik Collider";

    private const string MENU_MESH =
        "Tools/Box Collider Kolay Kurulum/" +
        "Seçili Köklerin Tüm Meshlerine Otomatik Collider";

    [MenuItem(MENU_KOK, false, 100)]
    private static void SeciliKoklereTekColliderEkle()
    {
        Transform[] secimler = Selection.transforms;

        if (secimler == null || secimler.Length == 0)
        {
            EditorUtility.DisplayDialog(
                "Box Collider Kolay Kurulum",
                "Önce Hierarchy'den bir veya daha fazla eşya seç.",
                "Tamam"
            );
            return;
        }

        int undoGrubu = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName(
            "Seçili eşyalara otomatik Box Collider ekle"
        );

        int eklendi = 0;
        int guncellendi = 0;
        int atlandi = 0;

        HashSet<GameObject> islenenler = new HashSet<GameObject>();

        for (int i = 0; i < secimler.Length; i++)
        {
            Transform kok = secimler[i];

            if (kok == null ||
                EditorUtility.IsPersistent(kok.gameObject) ||
                !islenenler.Add(kok.gameObject))
            {
                continue;
            }

            Bounds yerelSinir;

            if (!AltRendererSiniriniHesapla(kok, out yerelSinir))
            {
                atlandi++;
                continue;
            }

            Collider varOlanCollider = kok.GetComponent<Collider>();

            if (varOlanCollider != null &&
                !(varOlanCollider is BoxCollider))
            {
                atlandi++;
                continue;
            }

            BoxCollider kutu = varOlanCollider as BoxCollider;

            if (kutu == null)
            {
                kutu = Undo.AddComponent<BoxCollider>(kok.gameObject);
                eklendi++;
            }
            else
            {
                Undo.RecordObject(kutu, "Box Collider ölçüsünü güncelle");
                guncellendi++;
            }

            kutu.center = yerelSinir.center;
            kutu.size = GuvenliBoyut(yerelSinir.size);
            kutu.isTrigger = false;

            EditorUtility.SetDirty(kutu);
            EditorSceneManager.MarkSceneDirty(kok.gameObject.scene);
        }

        Undo.CollapseUndoOperations(undoGrubu);

        SonucuGoster(
            "Her seçili eşya için tek collider tamamlandı.",
            eklendi,
            guncellendi,
            atlandi
        );
    }

    [MenuItem(MENU_MESH, false, 101)]
    private static void SeciliKoklerinMeshlerineColliderEkle()
    {
        Transform[] secimler = Selection.transforms;

        if (secimler == null || secimler.Length == 0)
        {
            EditorUtility.DisplayDialog(
                "Box Collider Kolay Kurulum",
                "Önce Hierarchy'den bir ana kök veya eşyalar seç.",
                "Tamam"
            );
            return;
        }

        int undoGrubu = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName(
            "Seçili köklerin meshlerine Box Collider ekle"
        );

        int eklendi = 0;
        int guncellendi = 0;
        int atlandi = 0;

        HashSet<GameObject> islenenler = new HashSet<GameObject>();

        for (int i = 0; i < secimler.Length; i++)
        {
            Transform kok = secimler[i];

            if (kok == null || EditorUtility.IsPersistent(kok.gameObject))
            {
                continue;
            }

            MeshFilter[] meshFiltreleri =
                kok.GetComponentsInChildren<MeshFilter>(true);

            for (int m = 0; m < meshFiltreleri.Length; m++)
            {
                MeshFilter meshFiltresi = meshFiltreleri[m];

                if (meshFiltresi == null ||
                    meshFiltresi.sharedMesh == null ||
                    !islenenler.Add(meshFiltresi.gameObject))
                {
                    continue;
                }

                Collider varOlanCollider =
                    meshFiltresi.GetComponent<Collider>();

                if (varOlanCollider != null &&
                    !(varOlanCollider is BoxCollider))
                {
                    atlandi++;
                    continue;
                }

                BoxCollider kutu = varOlanCollider as BoxCollider;

                if (kutu == null)
                {
                    kutu = Undo.AddComponent<BoxCollider>(
                        meshFiltresi.gameObject
                    );
                    eklendi++;
                }
                else
                {
                    Undo.RecordObject(
                        kutu,
                        "Box Collider ölçüsünü güncelle"
                    );
                    guncellendi++;
                }

                Bounds meshSiniri = meshFiltresi.sharedMesh.bounds;
                kutu.center = meshSiniri.center;
                kutu.size = GuvenliBoyut(meshSiniri.size);
                kutu.isTrigger = false;

                EditorUtility.SetDirty(kutu);
                EditorSceneManager.MarkSceneDirty(
                    meshFiltresi.gameObject.scene
                );
            }

            SkinnedMeshRenderer[] skinnedRendererlar =
                kok.GetComponentsInChildren<SkinnedMeshRenderer>(true);

            for (int s = 0; s < skinnedRendererlar.Length; s++)
            {
                SkinnedMeshRenderer renderer = skinnedRendererlar[s];

                if (renderer == null ||
                    renderer.sharedMesh == null ||
                    !islenenler.Add(renderer.gameObject))
                {
                    continue;
                }

                Collider varOlanCollider =
                    renderer.GetComponent<Collider>();

                if (varOlanCollider != null &&
                    !(varOlanCollider is BoxCollider))
                {
                    atlandi++;
                    continue;
                }

                BoxCollider kutu = varOlanCollider as BoxCollider;

                if (kutu == null)
                {
                    kutu = Undo.AddComponent<BoxCollider>(
                        renderer.gameObject
                    );
                    eklendi++;
                }
                else
                {
                    Undo.RecordObject(
                        kutu,
                        "Box Collider ölçüsünü güncelle"
                    );
                    guncellendi++;
                }

                Bounds meshSiniri = renderer.localBounds;
                kutu.center = meshSiniri.center;
                kutu.size = GuvenliBoyut(meshSiniri.size);
                kutu.isTrigger = false;

                EditorUtility.SetDirty(kutu);
                EditorSceneManager.MarkSceneDirty(renderer.gameObject.scene);
            }
        }

        Undo.CollapseUndoOperations(undoGrubu);

        SonucuGoster(
            "Seçili köklerin altındaki mesh collider'ları tamamlandı.",
            eklendi,
            guncellendi,
            atlandi
        );
    }

    private static bool AltRendererSiniriniHesapla(
        Transform kok,
        out Bounds yerelSinir)
    {
        yerelSinir = new Bounds(Vector3.zero, Vector3.zero);
        bool ilkNokta = true;

        Renderer[] rendererlar =
            kok.GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < rendererlar.Length; i++)
        {
            Renderer renderer = rendererlar[i];

            if (renderer == null ||
                (!(renderer is MeshRenderer) &&
                 !(renderer is SkinnedMeshRenderer)))
            {
                continue;
            }

            Bounds dunyaSiniri = renderer.bounds;
            Vector3 min = dunyaSiniri.min;
            Vector3 max = dunyaSiniri.max;

            for (int x = 0; x <= 1; x++)
            {
                for (int y = 0; y <= 1; y++)
                {
                    for (int z = 0; z <= 1; z++)
                    {
                        Vector3 dunyaNoktasi = new Vector3(
                            x == 0 ? min.x : max.x,
                            y == 0 ? min.y : max.y,
                            z == 0 ? min.z : max.z
                        );
                        Vector3 yerelNokta =
                            kok.InverseTransformPoint(dunyaNoktasi);

                        if (ilkNokta)
                        {
                            yerelSinir = new Bounds(
                                yerelNokta,
                                Vector3.zero
                            );
                            ilkNokta = false;
                        }
                        else
                        {
                            yerelSinir.Encapsulate(yerelNokta);
                        }
                    }
                }
            }
        }

        return !ilkNokta &&
            yerelSinir.size.sqrMagnitude > 0.000001f;
    }

    private static Vector3 GuvenliBoyut(Vector3 boyut)
    {
        return new Vector3(
            Mathf.Max(0.01f, boyut.x),
            Mathf.Max(0.01f, boyut.y),
            Mathf.Max(0.01f, boyut.z)
        );
    }

    private static void SonucuGoster(
        string baslik,
        int eklendi,
        int guncellendi,
        int atlandi)
    {
        Debug.Log(
            "BOX COLLIDER KURULUMU: " + baslik +
            " Eklenen: " + eklendi +
            ", Güncellenen: " + guncellendi +
            ", Atlanan: " + atlandi +
            ". İşlemi geri almak için Ctrl+Z."
        );

        EditorUtility.DisplayDialog(
            "Box Collider Kolay Kurulum",
            baslik + "\n\n" +
            "Eklenen: " + eklendi + "\n" +
            "Güncellenen: " + guncellendi + "\n" +
            "Atlanan (başka collider veya mesh yok): " + atlandi +
            "\n\nGeri almak için Ctrl+Z.",
            "Tamam"
        );
    }

    [MenuItem(MENU_KOK, true)]
    private static bool KokMenuAktifMi()
    {
        return MenuAktifMi();
    }

    [MenuItem(MENU_MESH, true)]
    private static bool MeshMenuAktifMi()
    {
        return MenuAktifMi();
    }

    private static bool MenuAktifMi()
    {
        return !EditorApplication.isPlayingOrWillChangePlaymode &&
            Selection.transforms != null &&
            Selection.transforms.Length > 0;
    }
}
#endif
