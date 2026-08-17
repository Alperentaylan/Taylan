using System;
using System.Reflection;
using UnityEngine;

[DefaultExecutionOrder(-20000)]
public class GLS580AracSesleri : MonoBehaviour
{
    [Header("ARAC / KARAKTER")]
    [Tooltip("Bos birakabilirsin. GLS580BasitSistem driveRoot'tan otomatik bulur.")]
    public Transform vehicleRoot;

    [Tooltip("Karakter root. Bos birakirsan Ch31_nonPBR (1) / Ch31_nonPBR isimlerinden bulmaya calisir.")]
    public Transform playerRoot;

    [Tooltip("Araca disaridan F basinca kapi sesi icin maksimum mesafe.")]
    public float kapiSesMesafesi = 6f;

    [Header("TUSLAR")]
    public KeyCode binInTus = KeyCode.F;
    public KeyCode kornaTus = KeyCode.E;
    public KeyCode frenTus = KeyCode.S;

    [Header("SES DOSYALARIN")]
    [Tooltip("Disaridan araca F ile binerken calacak kapi sesi.")]
    public AudioClip kapiBinisSesi;

    [Tooltip("Aractan F ile inerken calacak kapi sesi. Bos birakirsan binis sesini kullanir.")]
    public AudioClip kapiInisSesi;

    [Tooltip("E ile korna.")]
    public AudioClip kornaSesi;

    [Tooltip("S ile ileri giderken fren sesi.")]
    public AudioClip frenSesi;

    [Header("KAPI")]
    [Range(0f, 1f)]
    public float kapiSesSeviyesi = 0.9f;

    [Tooltip("Ayni F basiminda sesin iki kere tetiklenmesini engeller.")]
    public float kapiSesCooldown = 0.35f;

    [Header("KORNA")]
    [Tooltip("Aciksa E basili tutuldugu surece korna loop olur. Kapaliysa her E basiminda bir kere oynar.")]
    public bool kornaBasiliTut = true;

    [Range(0f, 1f)]
    public float kornaSesSeviyesi = 0.9f;

    [Header("FREN")]
    [Tooltip("Fren sesinin devreye girmesi icin arac en az bu hizda ileri gidiyor olmali.")]
    public float frenMinimumHizKmh = 4f;

    [Tooltip("Aciksa S basili tutuldugu surece fren sesi loop olur. Kapaliysa S'nin ilk basiminda bir kere oynar.")]
    public bool frenSesiLoop = true;

    [Range(0f, 1f)]
    public float frenSesSeviyesi = 0.75f;

    [Tooltip("Hiz arttikca fren sesi biraz yukselir.")]
    public bool frenSesiniHizaGoreAyarla = true;

    [Header("3D SES")]
    [Range(0f, 1f)]
    public float spatialBlend = 0.75f;

    public float minDistance = 2f;
    public float maxDistance = 45f;

    [Header("GLS580 BASIT SISTEM")]
    [Tooltip("Bos birakabilirsin. Otomatik bulur.")]
    public MonoBehaviour basitSistem;

    [Header("DEBUG")]
    [SerializeField] private bool playerInside;
    [SerializeField] private bool busy;
    [SerializeField] private float currentDriveSpeedMps;
    [SerializeField] private float currentSpeedKmh;
    [SerializeField] private bool kornaCalisiyor;
    [SerializeField] private bool frenSesiCalisiyor;

    private Type basitType;
    private FieldInfo fPlayerInside;
    private FieldInfo fBusy;
    private FieldInfo fDriveRoot;
    private FieldInfo fCurrentDriveSpeed;
    private FieldInfo fPlayerRoot;

    private AudioSource kapiSource;
    private AudioSource kornaSource;
    private AudioSource frenSource;

    private float lastDoorSoundTime = -999f;

    // currentDriveSpeed bulunamazsa fallback.
    private Vector3 lastVehiclePosition;
    private bool lastVehiclePositionReady;
    private float fallbackSignedSpeedMps;

    private void Awake()
    {
        OtomatikBul();
        AudioHazirla();
        FallbackHizBaslat();
    }

    private void Start()
    {
        OtomatikBul();
        AudioHazirla();
        FallbackHizBaslat();
    }

    private void Update()
    {
        OtomatikBul();
        DurumuOku();
        FallbackHiziGuncelle();

        KapiSesiniYonet();
        KornaYonet();
        FrenSesiniYonet();
    }

    // ============================================================
    // KAPI - F
    // ============================================================
    private void KapiSesiniYonet()
    {
        if (!Input.GetKeyDown(binInTus))
            return;

        if (Time.time - lastDoorSoundTime < kapiSesCooldown)
            return;

        if (kapiSource == null)
            return;

        AudioClip clip = null;

        if (playerInside)
        {
            // Aractan inis.
            clip =
                kapiInisSesi != null
                    ? kapiInisSesi
                    : kapiBinisSesi;
        }
        else
        {
            // Araca binis: uzaktan F basinca ses gelmesin.
            if (!OyuncuAracaYakin())
                return;

            clip = kapiBinisSesi;
        }

        if (clip == null)
            return;

        lastDoorSoundTime = Time.time;

        kapiSource.PlayOneShot(
            clip,
            kapiSesSeviyesi);
    }

    private bool OyuncuAracaYakin()
    {
        if (vehicleRoot == null)
            return false;

        if (playerRoot == null)
        {
            // Player bulunamazsa BasitSistem zaten F'yi sadece yakinlikta kabul ediyor olabilir.
            // Ama yanlis yerde ses cikarmamak icin false donuyoruz.
            return false;
        }

        float distance =
            Vector3.Distance(
                playerRoot.position,
                vehicleRoot.position);

        return distance <= kapiSesMesafesi;
    }

    // ============================================================
    // KORNA - E
    // ============================================================
    private void KornaYonet()
    {
        bool allowed =
            playerInside &&
            !busy &&
            kornaSesi != null &&
            kornaSource != null;

        if (!allowed)
        {
            KornaDurdur();
            return;
        }

        if (kornaBasiliTut)
        {
            if (Input.GetKey(kornaTus))
            {
                if (kornaSource.clip != kornaSesi)
                    kornaSource.clip = kornaSesi;

                kornaSource.loop = true;
                kornaSource.volume = kornaSesSeviyesi;

                if (!kornaSource.isPlaying)
                    kornaSource.Play();

                kornaCalisiyor = true;
            }
            else
            {
                KornaDurdur();
            }
        }
        else
        {
            if (Input.GetKeyDown(kornaTus))
            {
                kornaSource.PlayOneShot(
                    kornaSesi,
                    kornaSesSeviyesi);
            }

            kornaCalisiyor = false;
        }
    }

    private void KornaDurdur()
    {
        if (kornaSource != null &&
            kornaSource.isPlaying &&
            kornaSource.loop)
        {
            kornaSource.Stop();
        }

        kornaCalisiyor = false;
    }

    // ============================================================
    // FREN - S
    // ============================================================
    private void FrenSesiniYonet()
    {
        float signedSpeed =
            GercekSignedSpeedMps();

        currentDriveSpeedMps = signedSpeed;
        currentSpeedKmh = Mathf.Abs(signedSpeed) * 3.6f;

        // S geri viteste de kullanildigi icin SADECE ILERI giderken fren sesi.
        bool ileriGidiyor =
            signedSpeed >
            (frenMinimumHizKmh / 3.6f);

        bool allowed =
            playerInside &&
            !busy &&
            ileriGidiyor &&
            frenSesi != null &&
            frenSource != null;

        if (!allowed)
        {
            FrenSesiDurdur();
            return;
        }

        if (frenSesiLoop)
        {
            if (Input.GetKey(frenTus))
            {
                if (frenSource.clip != frenSesi)
                    frenSource.clip = frenSesi;

                frenSource.loop = true;

                float volume =
                    frenSesSeviyesi;

                if (frenSesiniHizaGoreAyarla)
                {
                    float speedFactor =
                        Mathf.InverseLerp(
                            frenMinimumHizKmh,
                            100f,
                            currentSpeedKmh);

                    volume *=
                        Mathf.Lerp(
                            0.55f,
                            1f,
                            speedFactor);
                }

                frenSource.volume = volume;

                if (!frenSource.isPlaying)
                    frenSource.Play();

                frenSesiCalisiyor = true;
            }
            else
            {
                FrenSesiDurdur();
            }
        }
        else
        {
            if (Input.GetKeyDown(frenTus))
            {
                frenSource.PlayOneShot(
                    frenSesi,
                    frenSesSeviyesi);
            }

            frenSesiCalisiyor = false;
        }
    }

    private void FrenSesiDurdur()
    {
        if (frenSource != null &&
            frenSource.isPlaying &&
            frenSource.loop)
        {
            frenSource.Stop();
        }

        frenSesiCalisiyor = false;
    }

    // ============================================================
    // AUDIO SOURCE
    // ============================================================
    private void AudioHazirla()
    {
        if (vehicleRoot == null)
            return;

        if (kapiSource == null)
        {
            kapiSource =
                YeniAudioSource(
                    "_GLS580_KapiAudio");
        }

        if (kornaSource == null)
        {
            kornaSource =
                YeniAudioSource(
                    "_GLS580_KornaAudio");
        }

        if (frenSource == null)
        {
            frenSource =
                YeniAudioSource(
                    "_GLS580_FrenAudio");
        }
    }

    private AudioSource YeniAudioSource(
        string objectName)
    {
        GameObject go =
            new GameObject(objectName);

        go.transform.SetParent(
            vehicleRoot,
            false);

        AudioSource source =
            go.AddComponent<AudioSource>();

        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = spatialBlend;
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;

        return source;
    }

    // ============================================================
    // SPEED
    // ============================================================
    private float GercekSignedSpeedMps()
    {
        if (basitSistem != null &&
            fCurrentDriveSpeed != null)
        {
            try
            {
                return Convert.ToSingle(
                    fCurrentDriveSpeed.GetValue(
                        basitSistem));
            }
            catch { }
        }

        return fallbackSignedSpeedMps;
    }

    private void FallbackHizBaslat()
    {
        if (vehicleRoot == null)
            return;

        lastVehiclePosition =
            vehicleRoot.position;

        lastVehiclePositionReady = true;
    }

    private void FallbackHiziGuncelle()
    {
        if (vehicleRoot == null)
            return;

        if (!lastVehiclePositionReady)
        {
            FallbackHizBaslat();
            return;
        }

        float dt =
            Mathf.Max(
                Time.deltaTime,
                0.0001f);

        Vector3 delta =
            vehicleRoot.position -
            lastVehiclePosition;

        lastVehiclePosition =
            vehicleRoot.position;

        Vector3 velocity =
            delta / dt;

        fallbackSignedSpeedMps =
            Vector3.Dot(
                velocity,
                vehicleRoot.forward);
    }

    // ============================================================
    // BASIT SYSTEM
    // ============================================================
    private void OtomatikBul()
    {
        if (basitSistem == null)
        {
            MonoBehaviour[] all =
                FindObjectsOfType<MonoBehaviour>(
                    true);

            foreach (MonoBehaviour mb in all)
            {
                if (mb != null &&
                    mb.GetType().Name ==
                    "GLS580BasitSistem")
                {
                    basitSistem = mb;
                    break;
                }
            }
        }

        if (basitSistem != null)
        {
            Type currentType =
                basitSistem.GetType();

            if (basitType != currentType)
            {
                basitType =
                    currentType;

                BindingFlags flags =
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic;

                fPlayerInside =
                    currentType.GetField(
                        "playerInside",
                        flags);

                fBusy =
                    currentType.GetField(
                        "busy",
                        flags);

                fDriveRoot =
                    currentType.GetField(
                        "driveRoot",
                        flags);

                fCurrentDriveSpeed =
                    currentType.GetField(
                        "currentDriveSpeed",
                        flags);

                // Olası player root alan adlari.
                fPlayerRoot =
                    currentType.GetField(
                        "playerRoot",
                        flags);

                if (fPlayerRoot == null)
                    fPlayerRoot =
                        currentType.GetField(
                            "player",
                            flags);
            }

            if (fDriveRoot != null)
            {
                try
                {
                    Transform root =
                        fDriveRoot.GetValue(
                            basitSistem)
                        as Transform;

                    if (root != null)
                        vehicleRoot = root;
                }
                catch { }
            }

            if (playerRoot == null &&
                fPlayerRoot != null)
            {
                try
                {
                    Transform p =
                        fPlayerRoot.GetValue(
                            basitSistem)
                        as Transform;

                    if (p != null)
                        playerRoot = p;
                }
                catch { }
            }
        }

        if (vehicleRoot == null)
        {
            GameObject root =
                GameObject.Find(
                    "_GLS580_DriveRoot_V2");

            if (root != null)
                vehicleRoot =
                    root.transform;
        }

        if (playerRoot == null)
        {
            GameObject p =
                GameObject.Find(
                    "Ch31_nonPBR (1)");

            if (p == null)
                p =
                    GameObject.Find(
                        "Ch31_nonPBR");

            if (p != null)
                playerRoot =
                    p.transform;
        }

        AudioHazirla();
    }

    private void DurumuOku()
    {
        if (basitSistem == null)
        {
            playerInside = false;
            busy = false;
            return;
        }

        try
        {
            playerInside =
                fPlayerInside != null
                    ? (bool)fPlayerInside.GetValue(
                        basitSistem)
                    : false;

            busy =
                fBusy != null
                    ? (bool)fBusy.GetValue(
                        basitSistem)
                    : false;
        }
        catch
        {
            playerInside = false;
            busy = false;
        }
    }

    private void OnDisable()
    {
        KornaDurdur();
        FrenSesiDurdur();
    }
}
