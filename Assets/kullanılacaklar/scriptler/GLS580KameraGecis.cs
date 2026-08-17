using System;
using System.Reflection;
using UnityEngine;

[DefaultExecutionOrder(32700)]
public class GLS580KameraSistemiV6 : MonoBehaviour
{
    [Header("BASIT SISTEM")]
    [Tooltip("Bos birakabilirsin. Script sahnedeki GLS580BasitSistem'i otomatik bulur.")]
    public MonoBehaviour basitSistem;

    [Header("KARAKTER")]
    [Tooltip("Ch31_nonPBR (1). Bos birakilirsa BasitSistem'den otomatik okunur.")]
    public Transform playerRoot;

    [Header("NORMAL OYUNDAKI 2 KARAKTER KAMERASI")]
    public Camera karakterKamera1;
    public Camera karakterKamera2;

    [Tooltip("Aractan tamamen indikten sonra ilk acilacak kamera.")]
    public Camera cikisAsilKarakterKamerasi;

    [Header("ARACTAKI 4 KAMERA - V SIRASI")]
    public Camera aracKamera1;      // yakin
    public Camera aracKamera2;      // orta
    public Camera aracKamera3;      // uzak
    public Camera aracGozKamerasi;  // ic

    public KeyCode kameraDegistirTusu = KeyCode.V;

    [Header("DIS KAMERA ORBIT")]
    [Tooltip("Bos birakilirsa BasitSistem'in driveRoot'u kullanilir.")]
    public Transform aracTakipRoot;

    [Tooltip("Bos birakilirsa aracTakipRoot + offset kullanilir.")]
    public Transform disKameraHedef;

    public Vector3 disKameraHedefOffset = new Vector3(0f, 1.35f, 0f);
    public float disKameraHassasiyet = 3.2f;
    public float disEnAsagiAci = -10f;
    public float disEnYukariAci = 60f;
    public float disTakipYumusakligi = 15f;

    [Tooltip("0 = kameranin Scene'deki mevcut uzakligini otomatik kullan.")]
    public float kamera1Mesafe = 0f;
    public float kamera2Mesafe = 0f;
    public float kamera3Mesafe = 0f;

    [Header("IC / GOZ KAMERASI")]
    [Tooltip("Direksiyon arkasinda goz hizasindaki Empty. Bos birakilirsa kameranin kendi pozisyonu korunur.")]
    public Transform icKameraPozisyonAnchor;

    public float icYatayLimit = 90f;
    public float icDikeyMin = -55f;
    public float icDikeyMax = 55f;
    public float icKameraHassasiyet = 2.5f;

    [Header("KOLTUKTA DUZ OTURMA - HIPS GEREKMIYOR")]
    public bool koltuktaDuzOtur = true;

    [Tooltip("Karakter ters bakarsa Y'yi 180 yap. Yamuksa genelde 0 yeterlidir.")]
    public Vector3 oturusRotasyonOffset = Vector3.zero;

    [Header("DEBUG")]
    [SerializeField] private bool playerInside;
    [SerializeField] private bool busy;
    [SerializeField] private int aktifAracKameraIndex = 0;
    [SerializeField] private string durum = "Karakter";

    private Type basitSistemType;
    private FieldInfo fieldPlayerInside;
    private FieldInfo fieldBusy;
    private FieldInfo fieldCarCamera;
    private FieldInfo fieldDriveRoot;
    private FieldInfo fieldPlayerRoot;

    private Camera basitSistemCarCamera;
    private Camera sonAktifKarakterKamera;

    private bool oncekiPlayerInside;
    private bool oncekiBusy;

    // dis orbit
    private float disYaw;
    private float disPitch;
    private bool disOrbitHazir;

    // ic kamera
    private float icYaw;
    private float icPitch;
    private float icBaslangicYawOffset;
    private float icBaslangicPitch;
    private bool icHazir;

    private Camera[] AracKameralari
    {
        get
        {
            return new Camera[]
            {
                aracKamera1,
                aracKamera2,
                aracKamera3,
                aracGozKamerasi
            };
        }
    }

    private void Awake()
    {
        EskiKameraScriptleriniKapat();

        BasitSistemiBulVeHazirla();

        if (cikisAsilKarakterKamerasi == null)
            cikisAsilKarakterKamerasi = karakterKamera1;

        Camera aktifKarakter = AktifKarakterKamerasiniBul();
        if (aktifKarakter != null)
            sonAktifKarakterKamera = aktifKarakter;

        // Oyun karakter modunda baslar.
        AracKameralariniKapat();
    }

    private void Start()
    {
        BasitSistemiBulVeHazirla();
        BasitDurumunuOku();

        DisOrbitBaslangiciniOku();
        IcKameraBaslangiciniOku();
    }

    private void Update()
    {
        BasitSistemiBulVeHazirla();
        BasitDurumunuOku();

        // Normal karakter modundayken hangi karakter kamerasi aktif onu hatirla.
        if (!playerInside && !busy)
        {
            Camera aktifKarakter = AktifKarakterKamerasiniBul();

            if (aktifKarakter != null)
                sonAktifKarakterKamera = aktifKarakter;
        }

        // V SADECE arac tamamen icindeyken calisir.
        if (playerInside && !busy)
        {
            if (Input.GetKeyDown(kameraDegistirTusu))
            {
                SonrakiAracKamerasinaGec();
            }

            KameraMouseInputunuOku();
        }

        oncekiPlayerInside = playerInside;
        oncekiBusy = busy;
    }

    private void LateUpdate()
    {
        BasitSistemiBulVeHazirla();
        BasitDurumunuOku();

        // ---------------------------------------------------------
        // 1) NORMAL OYUN
        // busy=false, playerInside=false
        // Sadece karakter kameralari.
        // ---------------------------------------------------------
        if (!playerInside && !busy)
        {
            durum = "Karakter";

            // Once karakter kamerasi garanti acik olsun.
            Camera hedefKarakter = AktifKarakterKamerasiniBul();

            if (hedefKarakter == null)
            {
                hedefKarakter =
                    cikisAsilKarakterKamerasi != null
                    ? cikisAsilKarakterKamerasi
                    : (sonAktifKarakterKamera != null
                        ? sonAktifKarakterKamera
                        : (karakterKamera1 != null ? karakterKamera1 : karakterKamera2));

                KameraAc(hedefKarakter);
            }

            if (hedefKarakter != null)
                sonAktifKarakterKamera = hedefKarakter;

            // SONRA arac kameralarini kapat.
            AracKameralariniKapat();

            // BasitSistem'in kendi kamerasi bizim atanmis Arac Kamera 1 ile AYNI ise,
            // onu burada zaten AracKameralariniKapat kapatmistir.
            if (!BasitKameraBizimAracKameralarimizdanBiriMi())
                KameraKapat(basitSistemCarCamera);

            if (hedefKarakter != null)
                SadeceBilinenKameralardaBuListenerAcik(hedefKarakter);

            return;
        }

        // ---------------------------------------------------------
        // 2) BINIS ANIMASYONU
        // busy=true, playerInside=false
        // KARAKTER KAMERASI animasyon bitene kadar acik.
        // No Display YOK.
        // ---------------------------------------------------------
        if (!playerInside && busy)
        {
            durum = "Biniyor - Karakter Kamerasi";

            Camera girisKamerasi =
                sonAktifKarakterKamera != null
                ? sonAktifKarakterKamera
                : (karakterKamera1 != null ? karakterKamera1 : karakterKamera2);

            // ONCE karakter kamerasi ac.
            KameraAc(girisKamerasi);

            // SONRA arac kameralarini kapat.
            AracKameralariniKapat();

            if (!BasitKameraBizimAracKameralarimizdanBiriMi())
                KameraKapat(basitSistemCarCamera);

            if (girisKamerasi != null)
                SadeceBilinenKameralardaBuListenerAcik(girisKamerasi);

            return;
        }

        // ---------------------------------------------------------
        // 3) ARAC ICINDE / INIS ANIMASYONU
        // playerInside=true.
        //
        // BasitSistem ExitRoutine boyunca playerInside=true tutuyor.
        // Bu yuzden inerken de SECILI ARAC KAMERASINDA kalir.
        // ---------------------------------------------------------
        if (playerInside)
        {
            durum = busy ? "Iniyor - Arac Kamerasi" : "Arac";

            GecerliAracKamerasiSeciliDegilseDuzelt();

            // ONCE secili arac kamerasini ac.
            SeciliAracKamerasiniGarantiEt();

            // SONRA karakter kameralarini kapat.
            KarakterKameralariniKapat();

            // BasitSistem'in otomatik kamerasini sadece secili kamera o degilse kapat.
            Camera secili = SeciliAracKamerasi();

            if (basitSistemCarCamera != null &&
                basitSistemCarCamera != secili)
            {
                KameraKapat(basitSistemCarCamera);
            }

            // Dýþ kameralar aracin etrafinda doner.
            DisKameralariGuncelle();

            // Ic kamera: -90/+90 ve momentum yok.
            if (aktifAracKameraIndex == 3)
                IcKamerayiGuncelle();

            // Sadece normal suruste oturusu duzelt.
            // Inis animasyonunda karaktere dokunma.
            if (!busy)
                KoltuktaDuzOturusuUygula();

            return;
        }
    }

    // =============================================================
    // BASIT SISTEM STATE - HIPS / CONTROLLER GEREKMIYOR
    // =============================================================

    private void BasitSistemiBulVeHazirla()
    {
        if (basitSistem == null)
        {
            MonoBehaviour[] all =
                FindObjectsOfType<MonoBehaviour>(true);

            foreach (MonoBehaviour mb in all)
            {
                if (mb != null && mb.GetType().Name == "GLS580BasitSistem")
                {
                    basitSistem = mb;
                    break;
                }
            }
        }

        if (basitSistem == null)
            return;

        Type t = basitSistem.GetType();

        if (basitSistemType == t && fieldPlayerInside != null)
            return;

        basitSistemType = t;

        BindingFlags flags =
            BindingFlags.Instance |
            BindingFlags.NonPublic |
            BindingFlags.Public;

        fieldPlayerInside = t.GetField("playerInside", flags);
        fieldBusy = t.GetField("busy", flags);
        fieldCarCamera = t.GetField("carCamera", flags);
        fieldDriveRoot = t.GetField("driveRoot", flags);
        fieldPlayerRoot = t.GetField("playerRoot", flags);

        if (fieldCarCamera != null)
            basitSistemCarCamera =
                fieldCarCamera.GetValue(basitSistem) as Camera;

        if (aracTakipRoot == null && fieldDriveRoot != null)
            aracTakipRoot =
                fieldDriveRoot.GetValue(basitSistem) as Transform;

        if (playerRoot == null && fieldPlayerRoot != null)
            playerRoot =
                fieldPlayerRoot.GetValue(basitSistem) as Transform;
    }

    private void BasitDurumunuOku()
    {
        if (basitSistem == null)
        {
            playerInside = false;
            busy = false;
            return;
        }

        try
        {
            if (fieldPlayerInside != null)
                playerInside = (bool)fieldPlayerInside.GetValue(basitSistem);

            if (fieldBusy != null)
                busy = (bool)fieldBusy.GetValue(basitSistem);

            if (fieldCarCamera != null)
                basitSistemCarCamera =
                    fieldCarCamera.GetValue(basitSistem) as Camera;

            if (aracTakipRoot == null && fieldDriveRoot != null)
                aracTakipRoot =
                    fieldDriveRoot.GetValue(basitSistem) as Transform;
        }
        catch
        {
            // State gecici okunamazsa mevcut son degerleri koru.
        }
    }

    // =============================================================
    // V ILE KAMERA GECISI
    // =============================================================

    private void GecerliAracKamerasiSeciliDegilseDuzelt()
    {
        Camera[] cams = AracKameralari;

        if (aktifAracKameraIndex >= 0 &&
            aktifAracKameraIndex < cams.Length &&
            cams[aktifAracKameraIndex] != null)
        {
            return;
        }

        aktifAracKameraIndex = -1;

        for (int i = 0; i < cams.Length; i++)
        {
            if (cams[i] != null)
            {
                aktifAracKameraIndex = i;
                return;
            }
        }
    }

    private void SonrakiAracKamerasinaGec()
    {
        Camera[] cams = AracKameralari;

        GecerliAracKamerasiSeciliDegilseDuzelt();

        int start = aktifAracKameraIndex;

        for (int step = 1; step <= cams.Length; step++)
        {
            int index = (start + step + cams.Length) % cams.Length;

            if (cams[index] != null)
            {
                aktifAracKameraIndex = index;
                SeciliAracKamerasiniGarantiEt();

                Debug.Log(
                    "GLS580 Kamera V6: V -> " +
                    (index + 1) +
                    "/4 : " +
                    cams[index].name,
                    this);

                return;
            }
        }
    }

    private Camera SeciliAracKamerasi()
    {
        Camera[] cams = AracKameralari;

        if (aktifAracKameraIndex < 0 ||
            aktifAracKameraIndex >= cams.Length)
            return null;

        return cams[aktifAracKameraIndex];
    }

    private void SeciliAracKamerasiniGarantiEt()
    {
        Camera[] cams = AracKameralari;

        GecerliAracKamerasiSeciliDegilseDuzelt();

        Camera secili = SeciliAracKamerasi();

        if (secili == null)
            return;

        // ONCE seciliyi ac.
        KameraAc(secili);

        // SONRA digerlerini kapat.
        for (int i = 0; i < cams.Length; i++)
        {
            if (cams[i] != null && cams[i] != secili)
                KameraKapat(cams[i]);
        }

        SadeceBilinenKameralardaBuListenerAcik(secili);
    }

    // =============================================================
    // DIS ORBIT
    // =============================================================

    private Transform TakipRoot()
    {
        if (aracTakipRoot != null)
            return aracTakipRoot;

        return transform;
    }

    private Vector3 DisHedefPozisyonu()
    {
        if (disKameraHedef != null)
            return disKameraHedef.position;

        Transform root = TakipRoot();
        return root.TransformPoint(disKameraHedefOffset);
    }

    private void DisOrbitBaslangiciniOku()
    {
        Transform root = TakipRoot();
        if (root == null)
            return;

        Vector3 target = DisHedefPozisyonu();

        if (kamera1Mesafe <= 0.01f && aracKamera1 != null)
            kamera1Mesafe =
                Mathf.Max(0.5f, Vector3.Distance(aracKamera1.transform.position, target));

        if (kamera2Mesafe <= 0.01f && aracKamera2 != null)
            kamera2Mesafe =
                Mathf.Max(0.5f, Vector3.Distance(aracKamera2.transform.position, target));

        if (kamera3Mesafe <= 0.01f && aracKamera3 != null)
            kamera3Mesafe =
                Mathf.Max(0.5f, Vector3.Distance(aracKamera3.transform.position, target));

        Camera referans =
            aracKamera1 != null ? aracKamera1 :
            (aracKamera2 != null ? aracKamera2 : aracKamera3);

        if (referans != null)
        {
            Vector3 dir =
                target - referans.transform.position;

            if (dir.sqrMagnitude > 0.001f)
            {
                Quaternion look =
                    Quaternion.LookRotation(dir.normalized, Vector3.up);

                disYaw =
                    Mathf.DeltaAngle(root.eulerAngles.y, look.eulerAngles.y);

                disPitch =
                    Mathf.Clamp(
                        Mathf.DeltaAngle(0f, look.eulerAngles.x),
                        disEnAsagiAci,
                        disEnYukariAci);
            }
        }

        if (kamera1Mesafe <= 0.01f) kamera1Mesafe = 4.5f;
        if (kamera2Mesafe <= 0.01f) kamera2Mesafe = 6.5f;
        if (kamera3Mesafe <= 0.01f) kamera3Mesafe = 9f;

        disOrbitHazir = true;
    }

    private void KameraMouseInputunuOku()
    {
        if (aktifAracKameraIndex >= 0 &&
            aktifAracKameraIndex <= 2)
        {
            disYaw +=
                Input.GetAxis("Mouse X") *
                disKameraHassasiyet;

            disPitch -=
                Input.GetAxis("Mouse Y") *
                disKameraHassasiyet;

            disPitch =
                Mathf.Clamp(
                    disPitch,
                    disEnAsagiAci,
                    disEnYukariAci);
        }
        else if (aktifAracKameraIndex == 3)
        {
            icYaw +=
                Input.GetAxis("Mouse X") *
                icKameraHassasiyet;

            icPitch -=
                Input.GetAxis("Mouse Y") *
                icKameraHassasiyet;

            icYaw =
                Mathf.Clamp(
                    icYaw,
                    -icYatayLimit,
                    icYatayLimit);

            icPitch =
                Mathf.Clamp(
                    icPitch,
                    icDikeyMin,
                    icDikeyMax);
        }
    }

    private void DisKameralariGuncelle()
    {
        if (!disOrbitHazir)
            DisOrbitBaslangiciniOku();

        Vector3 target = DisHedefPozisyonu();

        OrbitUygula(aracKamera1, kamera1Mesafe, target);
        OrbitUygula(aracKamera2, kamera2Mesafe, target);
        OrbitUygula(aracKamera3, kamera3Mesafe, target);
    }

    private void OrbitUygula(
        Camera cam,
        float mesafe,
        Vector3 target)
    {
        if (cam == null)
            return;

        Transform root = TakipRoot();

        float worldYaw =
            root.eulerAngles.y + disYaw;

        Quaternion orbit =
            Quaternion.Euler(
                disPitch,
                worldYaw,
                0f);

        Vector3 hedefPoz =
            target -
            orbit * Vector3.forward *
            Mathf.Max(0.5f, mesafe);

        if (disTakipYumusakligi <= 0f)
        {
            cam.transform.position = hedefPoz;
        }
        else
        {
            float t =
                1f -
                Mathf.Exp(
                    -disTakipYumusakligi *
                    Time.deltaTime);

            cam.transform.position =
                Vector3.Lerp(
                    cam.transform.position,
                    hedefPoz,
                    t);
        }

        Vector3 look =
            target - cam.transform.position;

        if (look.sqrMagnitude > 0.0001f)
        {
            cam.transform.rotation =
                Quaternion.LookRotation(
                    look.normalized,
                    Vector3.up);
        }
    }

    // =============================================================
    // IC KAMERA - 90 / -90, MOMENTUM YOK
    // =============================================================

    private void IcKameraBaslangiciniOku()
    {
        if (aracGozKamerasi == null)
            return;

        Transform root = TakipRoot();

        icBaslangicYawOffset =
            Mathf.DeltaAngle(
                root.eulerAngles.y,
                aracGozKamerasi.transform.eulerAngles.y);

        icBaslangicPitch =
            Mathf.DeltaAngle(
                0f,
                aracGozKamerasi.transform.eulerAngles.x);

        icYaw = 0f;
        icPitch = 0f;
        icHazir = true;
    }

    private void IcKamerayiGuncelle()
    {
        if (aracGozKamerasi == null)
            return;

        if (!icHazir)
            IcKameraBaslangiciniOku();

        Transform root = TakipRoot();

        if (icKameraPozisyonAnchor != null)
        {
            // Anlik pozisyon: SmoothDamp / momentum yok.
            aracGozKamerasi.transform.position =
                icKameraPozisyonAnchor.position;
        }

        float yaw =
            root.eulerAngles.y +
            icBaslangicYawOffset +
            Mathf.Clamp(
                icYaw,
                -icYatayLimit,
                icYatayLimit);

        float pitch =
            Mathf.Clamp(
                icBaslangicPitch + icPitch,
                icDikeyMin,
                icDikeyMax);

        // Anlik rotasyon: momentum yok.
        // Aracin pitch/roll salinimini kameraya tasimiyoruz.
        aracGozKamerasi.transform.rotation =
            Quaternion.Euler(
                pitch,
                yaw,
                0f);
    }

    // =============================================================
    // KOLTUKTA DUZ OTURMA - PLAYER HIPS YOK
    // =============================================================

    private void KoltuktaDuzOturusuUygula()
    {
        if (!koltuktaDuzOtur || playerRoot == null)
            return;

        Transform root = TakipRoot();
        if (root == null)
            return;

        // Pozisyona dokunmuyoruz.
        // BasitSistem koltuk pozisyonunu / kalca kilidini aynen yonetsin.
        // Biz sadece yamuk rotation'i en son duzeltiyoruz.
        Quaternion duzAracYonu =
            Quaternion.Euler(
                0f,
                root.eulerAngles.y,
                0f);

        playerRoot.rotation =
            duzAracYonu *
            Quaternion.Euler(oturusRotasyonOffset);
    }

    // =============================================================
    // ESKI KAMERA SCRIPTLERINI OTOMATIK KAPAT
    // =============================================================

    private void EskiKameraScriptleriniKapat()
    {
        string[] eskiTipler =
        {
            "GLS580KameraGecis",
            "GLS580KameraGecisV2",
            "GLS580KameraSistemiV3",
            "GLS580KameraSistemiV4",
            "GLS580KameraKoltukV5"
        };

        MonoBehaviour[] all =
            FindObjectsOfType<MonoBehaviour>(true);

        foreach (MonoBehaviour mb in all)
        {
            if (mb == null || mb == this)
                continue;

            string ad = mb.GetType().Name;

            foreach (string eski in eskiTipler)
            {
                if (ad == eski)
                {
                    mb.enabled = false;

                    Debug.Log(
                        "GLS580 Kamera V6: Eski kamera scripti otomatik kapatildi -> " +
                        ad,
                        mb);

                    break;
                }
            }
        }
    }

    // =============================================================
    // CAMERA HELPERS
    // =============================================================

    private Camera AktifKarakterKamerasiniBul()
    {
        if (karakterKamera1 != null &&
            karakterKamera1.gameObject.activeInHierarchy &&
            karakterKamera1.enabled)
        {
            return karakterKamera1;
        }

        if (karakterKamera2 != null &&
            karakterKamera2.gameObject.activeInHierarchy &&
            karakterKamera2.enabled)
        {
            return karakterKamera2;
        }

        return null;
    }

    private bool BasitKameraBizimAracKameralarimizdanBiriMi()
    {
        if (basitSistemCarCamera == null)
            return false;

        foreach (Camera cam in AracKameralari)
        {
            if (cam == basitSistemCarCamera)
                return true;
        }

        return false;
    }

    private void AracKameralariniKapat()
    {
        foreach (Camera cam in AracKameralari)
            KameraKapat(cam);
    }

    private void KarakterKameralariniKapat()
    {
        KameraKapat(karakterKamera1);
        KameraKapat(karakterKamera2);
    }

    private static void KameraAc(Camera cam)
    {
        if (cam == null)
            return;

        if (!cam.gameObject.activeSelf)
            cam.gameObject.SetActive(true);

        cam.enabled = true;
    }

    private static void KameraKapat(Camera cam)
    {
        if (cam == null)
            return;

        cam.enabled = false;

        AudioListener listener =
            cam.GetComponent<AudioListener>();

        if (listener != null)
            listener.enabled = false;

        if (cam.gameObject.activeSelf)
            cam.gameObject.SetActive(false);
    }

    private void SadeceBilinenKameralardaBuListenerAcik(Camera aktif)
    {
        Camera[] bilinen =
        {
            karakterKamera1,
            karakterKamera2,
            aracKamera1,
            aracKamera2,
            aracKamera3,
            aracGozKamerasi,
            basitSistemCarCamera
        };

        foreach (Camera cam in bilinen)
        {
            if (cam == null)
                continue;

            AudioListener listener =
                cam.GetComponent<AudioListener>();

            if (listener != null)
                listener.enabled = (cam == aktif);
        }
    }
}