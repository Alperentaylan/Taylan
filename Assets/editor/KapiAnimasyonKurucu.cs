#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/*
 * Kullanım:
 * 1) Hierarchy'de karakteri seç.
 * 2) Tools > Kapı Sistemi > Seçili Karaktere Animasyonları Kur'a bas.
 *
 * Araç iki FBX'i proje içinde bulur, Root Motion import ayarlarını yapar
 * ve Animator Controller'daki doğru state'lere yerleştirir.
 */
public static class KapiAnimasyonKurucu
{
    private const string ICERI_FBX_ADI =
        "Opening Door Inwards";

    private const string DISARI_FBX_ADI =
        "Open Door Outwards";

    private const string ICERI_STATE_ADI =
        "kapıdan içeri girme";

    private const string DISARI_STATE_ADI =
        "kapıdan dışarı çıkma";

    [MenuItem(
        "Tools/Kapı Sistemi/Seçili Karaktere Animasyonları Kur"
    )]
    private static void AnimasyonlariKur()
    {
        GameObject seciliNesne = Selection.activeGameObject;

        if (seciliNesne == null)
        {
            HataGoster(
                "Önce Hierarchy'den karakteri seç."
            );
            return;
        }

        Animator animator =
            seciliNesne.GetComponent<Animator>();

        if (animator == null)
        {
            animator =
                seciliNesne.GetComponentInChildren<Animator>();
        }

        if (animator == null)
        {
            HataGoster(
                "Seçtiğin karakterde Animator bulunamadı."
            );
            return;
        }

        if (animator.avatar == null ||
            !animator.avatar.isHuman)
        {
            HataGoster(
                "Karakterin Animator Avatar alanında geçerli bir " +
                "Humanoid Avatar olmalı."
            );
            return;
        }

        AnimatorController controller =
            AnimatorControlleruBul(animator);

        if (controller == null)
        {
            HataGoster(
                "Karakterin Animator Controller alanında normal bir " +
                "Animator Controller bulunamadı."
            );
            return;
        }

        string iceriFbxYolu = FbxYolunuBul(ICERI_FBX_ADI);
        string disariFbxYolu = FbxYolunuBul(DISARI_FBX_ADI);

        if (string.IsNullOrEmpty(iceriFbxYolu) ||
            string.IsNullOrEmpty(disariFbxYolu))
        {
            HataGoster(
                "İki FBX de Assets klasöründe olmalı:\n\n" +
                "Opening Door Inwards.fbx\n" +
                "Open Door Outwards.fbx"
            );
            return;
        }

        try
        {
            EditorUtility.DisplayProgressBar(
                "Kapı animasyonları kuruluyor",
                "FBX Root Motion ayarları yapılıyor...",
                0.25f
            );

            FbxImportAyarlariniYap(
                iceriFbxYolu,
                animator.avatar
            );

            FbxImportAyarlariniYap(
                disariFbxYolu,
                animator.avatar
            );

            EditorUtility.DisplayProgressBar(
                "Kapı animasyonları kuruluyor",
                "Animator state'leri bağlanıyor...",
                0.75f
            );

            AnimationClip iceriKlibi =
                AnimasyonKlibiniYukle(iceriFbxYolu);

            AnimationClip disariKlibi =
                AnimasyonKlibiniYukle(disariFbxYolu);

            if (iceriKlibi == null || disariKlibi == null)
            {
                HataGoster(
                    "FBX dosyalarının içinden AnimationClip okunamadı."
                );
                return;
            }

            AnimatorStateMachine stateMachine =
                controller.layers[0].stateMachine;

            AnimatorState iceriState =
                StateBulVeyaOlustur(
                    stateMachine,
                    ICERI_STATE_ADI,
                    new Vector3(420f, 560f, 0f)
                );

            AnimatorState disariState =
                StateBulVeyaOlustur(
                    stateMachine,
                    DISARI_STATE_ADI,
                    new Vector3(420f, 630f, 0f)
                );

            StateAyarla(iceriState, iceriKlibi);
            StateAyarla(disariState, disariKlibi);

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Kapı sistemi hazır",
                "İki FBX doğru Root Motion ayarlarıyla içe aktarıldı.\n\n" +
                "Animator bağlantıları:\n" +
                "Opening Door Inwards -> kapıdan içeri girme\n" +
                "Open Door Outwards -> kapıdan dışarı çıkma\n\n" +
                "Kapı state'lerinin eski çıkış transition'ları temizlendi. " +
                "Geçişi artık KapiGirisSistemi.cs yönetiyor.",
                "Tamam"
            );
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    [MenuItem(
        "Tools/Kapı Sistemi/Seçili Karaktere Animasyonları Kur",
        true
    )]
    private static bool AnimasyonlariKurDogrula()
    {
        return !EditorApplication.isPlaying;
    }

    private static AnimatorController AnimatorControlleruBul(
        Animator animator)
    {
        RuntimeAnimatorController runtimeController =
            animator.runtimeAnimatorController;

        AnimatorOverrideController overrideController =
            runtimeController as AnimatorOverrideController;

        if (overrideController != null)
        {
            runtimeController =
                overrideController.runtimeAnimatorController;
        }

        return runtimeController as AnimatorController;
    }

    private static string FbxYolunuBul(string dosyaAdi)
    {
        string[] guidler = AssetDatabase.FindAssets(
            dosyaAdi + " t:Model"
        );

        for (int i = 0; i < guidler.Length; i++)
        {
            string yol =
                AssetDatabase.GUIDToAssetPath(guidler[i]);

            if (System.IO.Path.GetFileNameWithoutExtension(yol) ==
                dosyaAdi)
            {
                return yol;
            }
        }

        return null;
    }

    private static void FbxImportAyarlariniYap(
        string fbxYolu,
        Avatar karakterAvatari)
    {
        ModelImporter importer =
            AssetImporter.GetAtPath(fbxYolu) as ModelImporter;

        if (importer == null)
        {
            throw new System.InvalidOperationException(
                "ModelImporter bulunamadı: " + fbxYolu
            );
        }

        importer.importAnimation = true;
        importer.animationType =
            ModelImporterAnimationType.Human;

        importer.avatarSetup =
            ModelImporterAvatarSetup.CopyFromOther;

        importer.sourceAvatar = karakterAvatari;

        ModelImporterClipAnimation[] klipler =
            importer.clipAnimations;

        if (klipler == null || klipler.Length == 0)
        {
            klipler = importer.defaultClipAnimations;
        }

        for (int i = 0; i < klipler.Length; i++)
        {
            // Bunlar tek seferlik kapı animasyonlarıdır.
            klipler[i].loop = false;
            klipler[i].loopTime = false;
            klipler[i].loopPose = false;

            // Y ekseni ayakta sabit kalsın.
            klipler[i].lockRootHeightY = true;
            klipler[i].heightFromFeet = true;
            klipler[i].keepOriginalPositionY = false;

            // XZ kilitlenmez: gerçek ileri hareket Root Motion olur.
            klipler[i].lockRootPositionXZ = false;
            klipler[i].keepOriginalPositionXZ = false;

            // Kapıdan düz geçerken GameObject'in kendi ekseni sapmasın.
            klipler[i].lockRootRotation = true;
            klipler[i].keepOriginalOrientation = false;
        }

        importer.clipAnimations = klipler;
        importer.SaveAndReimport();
    }

    private static AnimationClip AnimasyonKlibiniYukle(
        string fbxYolu)
    {
        return AssetDatabase
            .LoadAllAssetsAtPath(fbxYolu)
            .OfType<AnimationClip>()
            .FirstOrDefault(
                klip => !klip.name.StartsWith("__preview__")
            );
    }

    private static AnimatorState StateBulVeyaOlustur(
        AnimatorStateMachine stateMachine,
        string stateAdi,
        Vector3 konum)
    {
        ChildAnimatorState[] stateListesi =
            stateMachine.states;

        for (int i = 0; i < stateListesi.Length; i++)
        {
            if (stateListesi[i].state.name == stateAdi)
            {
                return stateListesi[i].state;
            }
        }

        return stateMachine.AddState(stateAdi, konum);
    }

    private static void StateAyarla(
        AnimatorState state,
        AnimationClip klip)
    {
        state.motion = klip;
        state.speed = 1f;
        state.mirror = false;
        state.cycleOffset = 0f;
        state.writeDefaultValues = true;

        // State'in çıkışını runtime kapı kodu yapacak.
        AnimatorStateTransition[] transitionlar =
            state.transitions;

        for (int i = transitionlar.Length - 1; i >= 0; i--)
        {
            state.RemoveTransition(transitionlar[i]);
        }

        EditorUtility.SetDirty(state);
    }

    private static void HataGoster(string mesaj)
    {
        Debug.LogError("Kapı Animasyon Kurucu: " + mesaj);

        EditorUtility.DisplayDialog(
            "Kapı sistemi kurulamadı",
            mesaj,
            "Tamam"
        );
    }
}
#endif
