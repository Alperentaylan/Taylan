#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Secili kapiyi tek tikla calisir surgulu bahce kapisina donusturur.
/// Tools > Bahce Kapisi > Secili Kapiya Sistemi Kur
/// </summary>
public static class SurguluBahceKapisiKurucu
{
    private const string HIZALAMA_NOKTASI = "SurguluKapi_OyuncuNoktasi";
    private const string EL_NOKTASI = "SurguluKapi_SolElNoktasi";
    private const string KAMERA_NOKTASI = "SurguluKapi_KameraNoktasi";

    [MenuItem("Tools/Bahçe Kapısı/Seçili Kapıya Sistemi Kur")]
    private static void SistemiKur()
    {
        GameObject secili = Selection.activeGameObject;

        if (secili == null)
        {
            Hata("Önce Hierarchy'den hareket edecek kapı mesh'ini seç.");
            return;
        }

        Renderer kapiRenderer = secili.GetComponent<Renderer>();
        if (kapiRenderer == null)
            kapiRenderer = secili.GetComponentInChildren<Renderer>();

        if (kapiRenderer == null)
        {
            Hata("Seçili nesnede veya çocuklarında Renderer bulunamadı.");
            return;
        }

        Animator animator = OyuncuAnimatorunuBul(kapiRenderer.bounds.center);
        if (animator == null)
        {
            Hata(
                "Sahnede Humanoid Animator ve CharacterController içeren " +
                "bir oyuncu bulunamadı. Karakterin Avatar ayarını Humanoid yap."
            );
            return;
        }

        CharacterController controller =
            animator.GetComponentInParent<CharacterController>();
        Transform oyuncuKoku = controller != null
            ? controller.transform
            : animator.transform.root;

        SurguluBahceKapisi sistem =
            secili.GetComponent<SurguluBahceKapisi>();

        if (sistem == null)
            sistem = Undo.AddComponent<SurguluBahceKapisi>(secili);

        if (animator.GetComponent<SurguluKapiSolElIK>() == null)
            Undo.AddComponent<SurguluKapiSolElIK>(animator.gameObject);

        Transform hizalama = AltNoktaBulVeyaOlustur(
            secili.transform,
            HIZALAMA_NOKTASI
        );
        Transform elNoktasi = AltNoktaBulVeyaOlustur(
            secili.transform,
            EL_NOKTASI
        );
        Transform kameraNoktasi = AltNoktaBulVeyaOlustur(
            secili.transform,
            KAMERA_NOKTASI
        );

        Bounds sinir = kapiRenderer.bounds;
        Vector3 yerelUzunEksen = YerelUzunEkseniBul(
            secili.transform,
            kapiRenderer
        );
        Vector3 yerelAcilmaYonu = -yerelUzunEksen;
        Vector3 acilmaYonu = secili.transform.TransformDirection(
            yerelAcilmaYonu
        ).normalized;

        Vector3 kapiNormali = Vector3.Cross(Vector3.up, acilmaYonu).normalized;
        if (Vector3.Dot(
                oyuncuKoku.position - sinir.center,
                kapiNormali
            ) < 0f)
        {
            kapiNormali = -kapiNormali;
        }

        float yariUzunluk = YansitilmisYariBoy(sinir, acilmaYonu);
        float acilmaMesafesi = Mathf.Clamp(
            yariUzunluk * 1.85f,
            1.5f,
            6f
        );

        Transform solElKemigi = animator.GetBoneTransform(
            HumanBodyBones.LeftHand
        );
        float elYuksekligi = solElKemigi != null
            ? solElKemigi.position.y
            : sinir.min.y + 1.15f;

        Vector3 elKonumu =
            sinir.center -
            acilmaYonu * (yariUzunluk * 0.62f) +
            kapiNormali * 0.035f;
        elKonumu.y = elYuksekligi;

        Vector3 karakterIleri = Vector3.ProjectOnPlane(
            -kapiNormali + acilmaYonu * 0.2f,
            Vector3.up
        ).normalized;
        Quaternion karakterRotasyonu = Quaternion.LookRotation(
            karakterIleri,
            Vector3.up
        );
        Vector3 karakterSolu = karakterRotasyonu * Vector3.left;

        Vector3 hizalamaKonumu =
            elKonumu +
            kapiNormali * 0.62f -
            karakterSolu * 0.27f;
        hizalamaKonumu.y = oyuncuKoku.position.y;

        Vector3 kameraKonumu =
            hizalamaKonumu +
            karakterIleri * 3.1f +
            (karakterRotasyonu * Vector3.right) * 0.2f +
            Vector3.up * 1.55f;

        Undo.RecordObject(hizalama, "Sürgülü kapı oyuncu noktasını ayarla");
        hizalama.SetPositionAndRotation(
            hizalamaKonumu,
            karakterRotasyonu
        );

        Undo.RecordObject(elNoktasi, "Sürgülü kapı el noktasını ayarla");
        elNoktasi.position = elKonumu;
        elNoktasi.rotation = Quaternion.LookRotation(
            kapiNormali,
            Vector3.up
        ) * Quaternion.Euler(0f, 0f, 90f);

        Undo.RecordObject(kameraNoktasi, "Sürgülü kapı kamera noktasını ayarla");
        kameraNoktasi.position = kameraKonumu;
        kameraNoktasi.rotation = Quaternion.LookRotation(
            hizalamaKonumu + Vector3.up * 1.25f - kameraKonumu,
            Vector3.up
        );

        Collider collider = secili.GetComponent<Collider>();
        if (collider == null)
            ColliderEkle(secili, kapiRenderer);

        IKPassiniAc(animator);

        Undo.RecordObject(sistem, "Sürgülü bahçe kapısını kur");
        sistem.EditorKurulumunuAyarla(
            secili.transform,
            oyuncuKoku,
            animator,
            hizalama,
            elNoktasi,
            kameraNoktasi,
            yerelAcilmaYonu,
            acilmaMesafesi
        );

        EditorUtility.SetDirty(sistem);
        EditorUtility.SetDirty(animator);
        EditorSceneManager.MarkSceneDirty(secili.scene);
        Selection.activeGameObject = secili;

        EditorUtility.DisplayDialog(
            "Sürgülü bahçe kapısı hazır",
            "Sistem seçili kapıya kuruldu.\n\n" +
            "Turuncu çizgi kapının açılacağı yönü gösterir. " +
            "Mavi küre oyuncunun yaklaşma alanıdır.\n\n" +
            "Play'e basıp kapıya yaklaş. Varsayılan davranış: " +
            "yaklaşınca otomatik, sol el teması, önden kamera ve sola kayan ok.",
            "Tamam"
        );
    }

    [MenuItem(
        "Tools/Bahçe Kapısı/Seçili Kapıya Sistemi Kur",
        true
    )]
    private static bool SistemiKurDogrula()
    {
        return !EditorApplication.isPlaying && Selection.activeGameObject != null;
    }

    private static Animator OyuncuAnimatorunuBul(Vector3 kapiKonumu)
    {
        Animator[] animatorler = Object.FindObjectsByType<Animator>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        Animator enIyi = null;
        float enKisaMesafe = float.PositiveInfinity;

        for (int i = 0; i < animatorler.Length; i++)
        {
            Animator aday = animatorler[i];
            if (aday == null || aday.avatar == null ||
                !aday.avatar.isValid || !aday.avatar.isHuman)
            {
                continue;
            }

            CharacterController controller =
                aday.GetComponentInParent<CharacterController>();
            if (controller == null)
                continue;

            float mesafe = (controller.transform.position - kapiKonumu).sqrMagnitude;
            if (mesafe < enKisaMesafe)
            {
                enKisaMesafe = mesafe;
                enIyi = aday;
            }
        }

        return enIyi;
    }

    private static Transform AltNoktaBulVeyaOlustur(
        Transform ebeveyn,
        string ad)
    {
        Transform mevcut = ebeveyn.Find(ad);
        if (mevcut != null)
            return mevcut;

        GameObject yeni = new GameObject(ad);
        Undo.RegisterCreatedObjectUndo(yeni, ad + " oluştur");
        yeni.transform.SetParent(ebeveyn, false);
        return yeni.transform;
    }

    private static Vector3 YerelUzunEkseniBul(
        Transform kapi,
        Renderer kapiRenderer)
    {
        MeshFilter meshFilter = kapi.GetComponent<MeshFilter>();

        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            Vector3 boyut = Vector3.Scale(
                meshFilter.sharedMesh.bounds.size,
                Mutlak(kapi.lossyScale)
            );

            Vector3[] eksenler =
            {
                Vector3.right,
                Vector3.up,
                Vector3.forward
            };

            float[] uzunluklar = { boyut.x, boyut.y, boyut.z };
            float enIyiSkor = -1f;
            Vector3 enIyiEksen = Vector3.right;

            for (int i = 0; i < eksenler.Length; i++)
            {
                Vector3 dunyaEkseni =
                    kapi.TransformDirection(eksenler[i]).normalized;
                float yataylik = 1f - Mathf.Abs(
                    Vector3.Dot(dunyaEkseni, Vector3.up)
                );
                float skor = uzunluklar[i] * yataylik * yataylik;

                if (skor > enIyiSkor)
                {
                    enIyiSkor = skor;
                    enIyiEksen = eksenler[i];
                }
            }

            return enIyiEksen;
        }

        Vector3[] adaylar =
        {
            Vector3.right,
            Vector3.up,
            Vector3.forward
        };
        float enBuyuk = -1f;
        Vector3 sonuc = Vector3.right;

        for (int i = 0; i < adaylar.Length; i++)
        {
            Vector3 dunya = kapi.TransformDirection(adaylar[i]).normalized;
            float yataylik = 1f - Mathf.Abs(Vector3.Dot(dunya, Vector3.up));
            float skor = YansitilmisYariBoy(kapiRenderer.bounds, dunya) * yataylik;

            if (skor > enBuyuk)
            {
                enBuyuk = skor;
                sonuc = adaylar[i];
            }
        }

        return sonuc;
    }

    private static float YansitilmisYariBoy(Bounds sinir, Vector3 yon)
    {
        Vector3 mutlakYon = Mutlak(yon.normalized);
        return Vector3.Dot(sinir.extents, mutlakYon);
    }

    private static Vector3 Mutlak(Vector3 deger)
    {
        return new Vector3(
            Mathf.Abs(deger.x),
            Mathf.Abs(deger.y),
            Mathf.Abs(deger.z)
        );
    }

    private static void ColliderEkle(GameObject kapi, Renderer renderer)
    {
        MeshFilter meshFilter = kapi.GetComponent<MeshFilter>();
        BoxCollider collider = Undo.AddComponent<BoxCollider>(kapi);

        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            collider.center = meshFilter.sharedMesh.bounds.center;
            collider.size = meshFilter.sharedMesh.bounds.size;
            return;
        }

        collider.center = kapi.transform.InverseTransformPoint(
            renderer.bounds.center
        );

        Vector3 yerelBoyut = kapi.transform.InverseTransformVector(
            renderer.bounds.size
        );
        collider.size = Mutlak(yerelBoyut);
    }

    private static void IKPassiniAc(Animator animator)
    {
        RuntimeAnimatorController runtimeController =
            animator.runtimeAnimatorController;

        AnimatorOverrideController overrideController =
            runtimeController as AnimatorOverrideController;

        if (overrideController != null)
            runtimeController = overrideController.runtimeAnimatorController;

        AnimatorController controller = runtimeController as AnimatorController;
        if (controller == null || controller.layers.Length == 0)
            return;

        AnimatorControllerLayer[] katmanlar = controller.layers;
        if (katmanlar[0].iKPass)
            return;

        Undo.RecordObject(controller, "Kapı sol el IK Pass aç");
        katmanlar[0].iKPass = true;
        controller.layers = katmanlar;
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
    }

    private static void Hata(string mesaj)
    {
        Debug.LogError("Sürgülü Bahçe Kapısı Kurucu: " + mesaj);
        EditorUtility.DisplayDialog(
            "Sürgülü kapı kurulamadı",
            mesaj,
            "Tamam"
        );
    }
}
#endif
