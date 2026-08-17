using UnityEngine;

/// <summary>
/// GTA tarzı mağaza kapısı — kararlı sürüm.
/// Rigidbody, HingeJoint, mesafe ve trigger kullanmaz.
/// CharacterController kapıya çarpıp yürümeye devam edince kapı,
/// verilen manuel menteşe noktasının etrafında dünya Y ekseninde döner.
/// </summary>
[DisallowMultipleComponent]
public sealed class GTAMagazaKapisiKararli : MonoBehaviour
{
    [Header("Kapı Kanatları")]
    [SerializeField] private Transform solKapi;
    [SerializeField] private Transform sagKapi;

    [Header("Manuel Menteşeler")]
    [Tooltip("Sol kapının dış sol dikey kenarındaki boş nesne.")]
    [SerializeField] private Transform solMentese;

    [Tooltip("Sağ kapının dış sağ dikey kenarındaki boş nesne.")]
    [SerializeField] private Transform sagMentese;

    [Header("Oyuncu")]
    [Tooltip("CharacterController bulunan oyuncu ana nesnesi.")]
    [SerializeField] private Transform karakter;

    [Header("Hareket")]
    [Min(1f)]
    [SerializeField] private float itmeGucu = 260f;

    [Range(45f, 150f)]
    [SerializeField] private float maksimumAcilmaAcisi = 115f;

    [Tooltip("0 yaparsan kapı açıldığı yerde kalır.")]
    [Min(0f)]
    [SerializeField] private float kapanmaYayi = 1.15f;

    [Tooltip("Düşük değer daha serbest sallanma sağlar.")]
    [Min(0f)]
    [SerializeField] private float sallanmaFreni = 2.2f;

    [Min(10f)]
    [SerializeField] private float maksimumDonusHizi = 190f;

    [Header("Collider")]
    [Min(0.03f)]
    [SerializeField] private float colliderKalinligi = 0.12f;

    private CharacterController characterController;

    private void Start()
    {
        if (solKapi == null ||
            sagKapi == null ||
            solMentese == null ||
            sagMentese == null)
        {
            Debug.LogError(
                "GTAMagazaKapisiKararli: Kapılar ve iki menteşe noktası doldurulmalı.",
                this);

            enabled = false;
            return;
        }

        characterController = CharacterControllerBul();

        if (characterController == null)
        {
            Debug.LogError(
                "GTAMagazaKapisiKararli: CharacterController bulunamadı.",
                this);

            enabled = false;
            return;
        }

        EskiSistemleriKapat();
        EskiFizigiKapat(solKapi);
        EskiFizigiKapat(sagKapi);

        KapiKanadiKararli solKanat = KanatKur(
            solKapi,
            solMentese.position,
            "GTA_SolKapi_KararliPivot");

        KapiKanadiKararli sagKanat = KanatKur(
            sagKapi,
            sagMentese.position,
            "GTA_SagKapi_KararliPivot");

        if (solKanat == null || sagKanat == null)
        {
            enabled = false;
            return;
        }

        characterController.detectCollisions = true;

        KarakterKapıIticiKararli itici =
            characterController.GetComponent<KarakterKapıIticiKararli>();

        if (itici == null)
        {
            itici =
                characterController.gameObject.AddComponent<KarakterKapıIticiKararli>();
        }

        itici.ItmeGucu = itmeGucu;

        Physics.IgnoreLayerCollision(
            characterController.gameObject.layer,
            0,
            false);

        Debug.Log(
            "Kararlı mağaza kapıları hazır. Kapılar yalnızca çarpınca açılır.",
            this);
    }

    private KapiKanadiKararli KanatKur(
        Transform kapiGorseli,
        Vector3 menteseDunyaKonumu,
        string pivotAdi)
    {
        Bounds dunyaBounds;

        if (!DunyaBoundsBul(kapiGorseli, out dunyaBounds))
        {
            Debug.LogError(
                kapiGorseli.name + " için Renderer bounds bulunamadı.",
                this);

            return null;
        }

        GameObject pivotObjesi =
            new GameObject(pivotAdi);

        Transform pivot =
            pivotObjesi.transform;

        // Kritik nokta:
        // Pivotun rotasyonu her zaman dünya eksenleriyle aynı ve ölçeği 1.
        // İthal modelin Rotation 90 / Scale 100 değerleri pivota aktarılmaz.
        pivot.position = menteseDunyaKonumu;
        pivot.rotation = Quaternion.identity;
        pivot.localScale = Vector3.one;
        pivotObjesi.layer = 0;

        // Görselin dünya konumu değişmez.
        kapiGorseli.SetParent(pivot, true);

        Vector3 merkezdenMentese =
            dunyaBounds.center - menteseDunyaKonumu;

        merkezdenMentese.y = 0f;

        if (merkezdenMentese.sqrMagnitude < 0.001f)
        {
            Debug.LogError(
                pivotAdi + ": Menteşe kapının merkezinde. " +
                "Boş nesneyi kapının dış dikey kenarına taşı.",
                this);

            Destroy(pivotObjesi);
            return null;
        }

        Vector3 genislikYonu =
            merkezdenMentese.normalized;

        Vector3 normalYonu =
            Vector3.Cross(Vector3.up, genislikYonu).normalized;

        // Collider ayrı bir çocuk nesnede tutulur.
        // X ekseni menteşeden kapı merkezine, Z ekseni kapı kalınlığına bakar.
        GameObject colliderObjesi =
            new GameObject("GTA_KapiCollider");

        Transform colliderTransform =
            colliderObjesi.transform;

        colliderTransform.SetParent(pivot, true);
        colliderTransform.position = dunyaBounds.center;
        colliderTransform.rotation =
            Quaternion.LookRotation(normalYonu, Vector3.up);
        colliderTransform.localScale = Vector3.one;
        colliderObjesi.layer = 0;

        BoxCollider box =
            colliderObjesi.AddComponent<BoxCollider>();

        float genislik =
            Mathf.Max(merkezdenMentese.magnitude * 2f, 0.2f);

        float yukseklik =
            Mathf.Max(dunyaBounds.size.y * 0.96f, 0.5f);

        box.center = Vector3.zero;
        box.size = new Vector3(
            genislik * 0.94f,
            yukseklik,
            colliderKalinligi);

        box.isTrigger = false;

        KapiKanadiKararli kanat =
            pivotObjesi.AddComponent<KapiKanadiKararli>();

        kanat.Ayarla(
            maksimumAcilmaAcisi,
            kapanmaYayi,
            sallanmaFreni,
            maksimumDonusHizi);

        return kanat;
    }

    private CharacterController CharacterControllerBul()
    {
        CharacterController sonuc = null;

        if (karakter != null)
        {
            sonuc =
                karakter.GetComponent<CharacterController>();

            if (sonuc == null)
                sonuc =
                    karakter.GetComponentInParent<CharacterController>();

            if (sonuc == null)
                sonuc =
                    karakter.GetComponentInChildren<CharacterController>(true);
        }

        if (sonuc != null)
            return sonuc;

#if UNITY_6000_0_OR_NEWER
        CharacterController[] controllerlar =
            Object.FindObjectsByType<CharacterController>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
#else
        CharacterController[] controllerlar =
            Object.FindObjectsOfType<CharacterController>();
#endif

        foreach (CharacterController controller in controllerlar)
        {
            if (controller != null &&
                controller.enabled &&
                controller.CompareTag("Player"))
            {
                return controller;
            }
        }

        foreach (CharacterController controller in controllerlar)
        {
            if (controller != null && controller.enabled)
                return controller;
        }

        return null;
    }

    private static bool DunyaBoundsBul(
        Transform hedef,
        out Bounds sonuc)
    {
        Renderer[] rendererlar =
            hedef.GetComponentsInChildren<Renderer>(true);

        bool bulundu = false;
        sonuc = new Bounds();

        foreach (Renderer renderer in rendererlar)
        {
            if (renderer == null || !renderer.enabled)
                continue;

            if (!bulundu)
            {
                sonuc = renderer.bounds;
                bulundu = true;
            }
            else
            {
                sonuc.Encapsulate(renderer.bounds);
            }
        }

        return bulundu;
    }

    private static void EskiFizigiKapat(
        Transform hedef)
    {
        Collider[] colliderlar =
            hedef.GetComponentsInChildren<Collider>(true);

        foreach (Collider collider in colliderlar)
        {
            if (collider != null)
                collider.enabled = false;
        }

        Rigidbody[] bodyler =
            hedef.GetComponentsInChildren<Rigidbody>(true);

        foreach (Rigidbody body in bodyler)
        {
            if (body == null)
                continue;

            body.isKinematic = true;
            body.detectCollisions = false;
        }
    }

    private static void EskiSistemleriKapat()
    {
#if UNITY_6000_0_OR_NEWER
        MonoBehaviour[] davranislar =
            Object.FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

        Collider[] colliderlar =
            Object.FindObjectsByType<Collider>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
#else
        MonoBehaviour[] davranislar =
            Object.FindObjectsOfType<MonoBehaviour>(true);

        Collider[] colliderlar =
            Object.FindObjectsOfType<Collider>(true);
#endif

        foreach (MonoBehaviour davranis in davranislar)
        {
            if (davranis == null ||
                davranis is GTAMagazaKapisiKararli ||
                davranis is KarakterKapıIticiKararli ||
                davranis is KapiKanadiKararli)
            {
                continue;
            }

            string tipAdi =
                davranis.GetType().Name;

            if (tipAdi.Contains("GTAMagazaKapisi") ||
                tipAdi.Contains("GtaDoor") ||
                tipAdi.Contains("StablePushDoor") ||
                tipAdi.Contains("GtaSwingDoor") ||
                tipAdi.Contains("GtaPushDoor") ||
                tipAdi.Contains("OtomatikMentese"))
            {
                davranis.enabled = false;
            }
        }

        foreach (Collider collider in colliderlar)
        {
            if (collider == null)
                continue;

            string yol =
                TamYol(collider.transform);

            if (collider.gameObject.name.Contains("GTA_KapiCollider") ||
                collider.gameObject.name == "SolidDoorCollider" ||
                collider.gameObject.name == "KapiCollider" ||
                yol.Contains("GTA_DoorSystem") ||
                yol.Contains("GTA_StableDoorPivots") ||
                yol.Contains("GTA_DoorPivots") ||
                yol.Contains("DoorSingle"))
            {
                collider.enabled = false;
            }
        }
    }

    private static string TamYol(
        Transform hedef)
    {
        string yol = hedef.name;
        Transform ust = hedef.parent;

        while (ust != null)
        {
            yol = ust.name + "/" + yol;
            ust = ust.parent;
        }

        return yol;
    }
}


/// <summary>
/// CharacterController'ın gerçek kapı temasını yakalar.
/// Yakınlık veya trigger kullanılmaz.
/// </summary>
internal sealed class KarakterKapıIticiKararli : MonoBehaviour
{
    public float ItmeGucu { get; set; } = 260f;

    private void OnControllerColliderHit(
        ControllerColliderHit hit)
    {
        if (hit == null || hit.collider == null)
            return;

        KapiKanadiKararli kanat =
            hit.collider.GetComponentInParent<KapiKanadiKararli>();

        if (kanat == null)
            return;

        Vector3 hareketYonu =
            hit.moveDirection;

        hareketYonu.y = 0f;

        if (hareketYonu.sqrMagnitude < 0.0001f)
            return;

        kanat.It(
            hit.point,
            hareketYonu.normalized,
            ItmeGucu);
    }
}


/// <summary>
/// Kapı kanadını dünya Y ekseninde, manuel menteşe noktasından döndürür.
/// </summary>
internal sealed class KapiKanadiKararli : MonoBehaviour
{
    private float maksimumAci = 115f;
    private float yay = 1.15f;
    private float fren = 2.2f;
    private float maksimumHiz = 190f;

    private float aci;
    private float acisalHiz;

    public void Ayarla(
        float yeniMaksimumAci,
        float yeniYay,
        float yeniFren,
        float yeniMaksimumHiz)
    {
        maksimumAci =
            Mathf.Clamp(yeniMaksimumAci, 45f, 150f);

        yay =
            Mathf.Max(0f, yeniYay);

        fren =
            Mathf.Max(0f, yeniFren);

        maksimumHiz =
            Mathf.Max(10f, yeniMaksimumHiz);
    }

    public void It(
        Vector3 temasNoktasi,
        Vector3 hareketYonu,
        float itmeGucu)
    {
        Vector3 kaldirac =
            temasNoktasi - transform.position;

        kaldirac.y = 0f;
        hareketYonu.y = 0f;

        if (kaldirac.sqrMagnitude < 0.0001f ||
            hareketYonu.sqrMagnitude < 0.0001f)
        {
            return;
        }

        float donusYonu =
            Vector3.Cross(
                kaldirac.normalized,
                hareketYonu.normalized).y;

        if (Mathf.Abs(donusYonu) < 0.01f)
            return;

        float kaldiracOrani =
            Mathf.Clamp(kaldirac.magnitude, 0.2f, 2f);

        acisalHiz +=
            donusYonu *
            itmeGucu *
            kaldiracOrani *
            Time.deltaTime;

        acisalHiz =
            Mathf.Clamp(
                acisalHiz,
                -maksimumHiz,
                maksimumHiz);
    }

    private void Update()
    {
        float dt =
            Time.deltaTime;

        // Kapalı konuma geri çeken yay.
        acisalHiz +=
            (-aci * yay) * dt;

        // Sallanmayı yumuşatır.
        acisalHiz *=
            1f / (1f + fren * dt);

        aci +=
            acisalHiz * dt;

        if (aci > maksimumAci)
        {
            aci = maksimumAci;

            if (acisalHiz > 0f)
                acisalHiz *= -0.12f;
        }
        else if (aci < -maksimumAci)
        {
            aci = -maksimumAci;

            if (acisalHiz < 0f)
                acisalHiz *= -0.12f;
        }

        // Pivot identity rotation ile oluşturulduğu için
        // yalnızca dünya Y ekseninde düzgün döner.
        transform.rotation =
            Quaternion.Euler(0f, aci, 0f);
    }
}
