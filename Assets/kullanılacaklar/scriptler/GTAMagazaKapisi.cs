using UnityEngine;

/// <summary>
/// GTA mağaza kapısı:
/// - Yaklaşınca otomatik açılmaz.
/// - Trigger veya mesafe kontrolü kullanmaz.
/// - CharacterController kapıya gerçekten çarpınca kapıyı fiziksel olarak iter.
/// - Kapı çok hafiftir, iki tarafa serbestçe açılır.
/// - HingeJoint kullanmaz.
/// </summary>
[DisallowMultipleComponent]
public sealed class GTAMagazaKapisi : MonoBehaviour
{
    [Header("Kapı Kanatları")]
    [SerializeField] private Transform solKapi;
    [SerializeField] private Transform sagKapi;

    [Header("Oyuncu")]
    [Tooltip("CharacterController bulunan oyuncu ana nesnesini ver.")]
    [SerializeField] private Transform karakter;

    [Header("Serbest Kapı Fiziği")]
    [Min(0.05f)]
    [SerializeField] private float kapiKutlesi = 0.20f;

    [Min(0.1f)]
    [SerializeField] private float karakterItmeGucu = 14f;

    [Range(45f, 150f)]
    [SerializeField] private float maksimumAcilmaAcisi = 120f;

    [Tooltip("0 = kapı açıldığı yerde kalır. 0.08 çok hafif geri dönüş verir.")]
    [Min(0f)]
    [SerializeField] private float geriDonusGucu = 0.08f;

    [Tooltip("Düşük değer kapının daha serbest sallanmasını sağlar.")]
    [Min(0f)]
    [SerializeField] private float sallanmaFreni = 0.20f;

    [Header("Çarpışma")]
    [Min(0.05f)]
    [SerializeField] private float colliderKalinligi = 0.12f;

    private CharacterController characterController;

    private void Start()
    {
        if (solKapi == null || sagKapi == null)
        {
            Debug.LogError(
                "GTAMagazaKapisi: Sol Kapı ve Sağ Kapı alanlarını doldur.",
                this);

            enabled = false;
            return;
        }

        characterController = CharacterControllerBul();

        if (characterController == null)
        {
            Debug.LogError(
                "GTAMagazaKapisi: CharacterController bulunamadı. " +
                "Karakter alanına oyuncunun doğru ana nesnesini ver.",
                this);

            enabled = false;
            return;
        }

        EskiKapiSistemleriniKapat();
        KanatlardakiEskiFizigiKapat(solKapi);
        KanatlardakiEskiFizigiKapat(sagKapi);

        Bounds solBounds;
        Bounds sagBounds;

        if (!DunyaBoundsBul(solKapi, out solBounds) ||
            !DunyaBoundsBul(sagKapi, out sagBounds))
        {
            Debug.LogError(
                "GTAMagazaKapisi: Kapı nesnelerinde Renderer bulunamadı.",
                this);

            enabled = false;
            return;
        }

        Bounds toplamBounds = solBounds;
        toplamBounds.Encapsulate(sagBounds);

        bool genislikXEkseninde =
            toplamBounds.size.x >= toplamBounds.size.z;

        FizikselKapiOlustur(
            solKapi,
            solBounds,
            toplamBounds.center,
            genislikXEkseninde,
            true,
            "GTA_SolKapi_Pivot");

        FizikselKapiOlustur(
            sagKapi,
            sagBounds,
            toplamBounds.center,
            genislikXEkseninde,
            false,
            "GTA_SagKapi_Pivot");

        characterController.detectCollisions = true;

        GTAKarakterKapiItici itici =
            characterController.GetComponent<GTAKarakterKapiItici>();

        if (itici == null)
        {
            itici =
                characterController.gameObject.AddComponent<GTAKarakterKapiItici>();
        }

        itici.ItmeGucu = karakterItmeGucu;

        // Oyuncu katmanıyla kapıların Default katmanının çarpışmasını açık tut.
        Physics.IgnoreLayerCollision(
            characterController.gameObject.layer,
            0,
            false);

        Debug.Log(
            "GTA mağaza kapıları hazır: yalnızca fiziksel temasta açılır.",
            this);
    }

    private void FizikselKapiOlustur(
        Transform kapiGorseli,
        Bounds dunyaBounds,
        Vector3 ikiKapininMerkezi,
        bool genislikXEkseninde,
        bool solKanat,
        string pivotAdi)
    {
        Vector3 pivotKonumu = dunyaBounds.center;

        // Menteşe, iki kapının birleştiği orta kenarda değil dış kenarda olur.
        if (genislikXEkseninde)
        {
            pivotKonumu.x =
                solKanat
                    ? dunyaBounds.min.x
                    : dunyaBounds.max.x;
        }
        else
        {
            pivotKonumu.z =
                solKanat
                    ? dunyaBounds.min.z
                    : dunyaBounds.max.z;
        }

        GameObject pivotObjesi =
            new GameObject(pivotAdi);

        Transform pivot = pivotObjesi.transform;

        pivot.position = pivotKonumu;
        pivot.rotation = Quaternion.identity;
        pivot.localScale = Vector3.one;
        pivotObjesi.layer = 0;

        // Görselin dünya konumu değişmeden yeni fizik pivotuna bağlanır.
        kapiGorseli.SetParent(pivot, true);

        BoxCollider box =
            pivotObjesi.AddComponent<BoxCollider>();

        box.center =
            dunyaBounds.center - pivotKonumu;

        Vector3 boyut = dunyaBounds.size;

        // Çerçeveye sıkışmaması için çok az küçült.
        boyut.y = Mathf.Max(boyut.y * 0.96f, 0.5f);

        if (genislikXEkseninde)
        {
            boyut.x = Mathf.Max(boyut.x * 0.94f, 0.15f);
            boyut.z = Mathf.Max(boyut.z, colliderKalinligi);
        }
        else
        {
            boyut.z = Mathf.Max(boyut.z * 0.94f, 0.15f);
            boyut.x = Mathf.Max(boyut.x, colliderKalinligi);
        }

        box.size = boyut;
        box.isTrigger = false;

        Rigidbody body =
            pivotObjesi.AddComponent<Rigidbody>();

        body.mass = Mathf.Max(0.05f, kapiKutlesi);
        body.useGravity = false;
        body.isKinematic = false;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode =
            CollisionDetectionMode.ContinuousSpeculative;

        // Kapı yerinden hareket edemez; yalnızca dünya Y ekseninde döner.
        body.constraints =
            RigidbodyConstraints.FreezePositionX |
            RigidbodyConstraints.FreezePositionY |
            RigidbodyConstraints.FreezePositionZ |
            RigidbodyConstraints.FreezeRotationX |
            RigidbodyConstraints.FreezeRotationZ;

#if UNITY_6000_0_OR_NEWER
        body.linearDamping = 0f;
        body.angularDamping = sallanmaFreni;
#else
        body.drag = 0f;
        body.angularDrag = sallanmaFreni;
#endif

        body.maxAngularVelocity = 30f;

        // Rigidbody'nin dönüş merkezi kapının ortası değil, menteşe pivotu olur.
        body.centerOfMass = Vector3.zero;

        GTASerbestKapiKanadi kanat =
            pivotObjesi.AddComponent<GTASerbestKapiKanadi>();

        kanat.Ayarla(
            maksimumAcilmaAcisi,
            geriDonusGucu);
    }

    private CharacterController CharacterControllerBul()
    {
        CharacterController sonuc = null;

        if (karakter != null)
        {
            sonuc =
                karakter.GetComponent<CharacterController>();

            if (sonuc == null)
            {
                sonuc =
                    karakter.GetComponentInParent<CharacterController>();
            }

            if (sonuc == null)
            {
                sonuc =
                    karakter.GetComponentInChildren<CharacterController>(true);
            }
        }

        if (sonuc != null)
            return sonuc;

#if UNITY_6000_0_OR_NEWER
        CharacterController[] tumControllerlar =
            Object.FindObjectsByType<CharacterController>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
#else
        CharacterController[] tumControllerlar =
            Object.FindObjectsOfType<CharacterController>();
#endif

        foreach (CharacterController controller in tumControllerlar)
        {
            if (controller != null &&
                controller.enabled &&
                controller.CompareTag("Player"))
            {
                return controller;
            }
        }

        foreach (CharacterController controller in tumControllerlar)
        {
            if (controller != null && controller.enabled)
                return controller;
        }

        return null;
    }

    private static bool DunyaBoundsBul(
        Transform hedef,
        out Bounds bounds)
    {
        Renderer[] rendererlar =
            hedef.GetComponentsInChildren<Renderer>(true);

        bool bulundu = false;
        bounds = new Bounds();

        foreach (Renderer renderer in rendererlar)
        {
            if (renderer == null || !renderer.enabled)
                continue;

            if (!bulundu)
            {
                bounds = renderer.bounds;
                bulundu = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return bulundu;
    }

    private static void KanatlardakiEskiFizigiKapat(
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

    private static void EskiKapiSistemleriniKapat()
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
                davranis is GTAMagazaKapisi ||
                davranis is GTAKarakterKapiItici ||
                davranis is GTASerbestKapiKanadi)
            {
                continue;
            }

            string tipAdi =
                davranis.GetType().Name;

            if (tipAdi.Contains("GtaDoorPairController") ||
                tipAdi.Contains("StablePushDoor") ||
                tipAdi.Contains("GtaSwingDoor") ||
                tipAdi.Contains("GtaPushDoor") ||
                tipAdi.Contains("OtomatikMentese"))
            {
                davranis.enabled = false;
            }
        }

        // Önceki denemelerden kalan görünmez collider'ları kapatır.
        foreach (Collider collider in colliderlar)
        {
            if (collider == null)
                continue;

            string tamYol =
                TransformYolu(collider.transform);

            if (collider.gameObject.name == "SolidDoorCollider" ||
                collider.gameObject.name == "KapiCollider" ||
                tamYol.Contains("GTA_DoorSystem_V4") ||
                tamYol.Contains("GTA_StableDoorPivots_V3") ||
                tamYol.Contains("GTA_DoorPivots") ||
                tamYol.Contains("DoorSingle"))
            {
                collider.enabled = false;
            }
        }
    }

    private static string TransformYolu(
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
/// Yalnızca CharacterController bir kapı collider'ına gerçekten çarptığında çalışır.
/// Mesafe veya trigger kontrolü içermez.
/// </summary>
internal sealed class GTAKarakterKapiItici : MonoBehaviour
{
    public float ItmeGucu { get; set; } = 14f;

    private void OnControllerColliderHit(
        ControllerColliderHit hit)
    {
        if (hit == null || hit.collider == null)
            return;

        Rigidbody body =
            hit.collider.attachedRigidbody;

        if (body == null || body.isKinematic)
            return;

        if (body.GetComponent<GTASerbestKapiKanadi>() == null)
            return;

        Vector3 hareketYonu =
            hit.moveDirection;

        hareketYonu.y = 0f;

        if (hareketYonu.sqrMagnitude < 0.0001f)
            return;

        body.AddForceAtPosition(
            hareketYonu.normalized * ItmeGucu,
            hit.point,
            ForceMode.Force);
    }
}


/// <summary>
/// Kapıya açı sınırı ve çok hafif geri dönüş uygular.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
internal sealed class GTASerbestKapiKanadi : MonoBehaviour
{
    private Rigidbody body;
    private Quaternion kapaliRotasyon;
    private float maksimumAci = 120f;
    private float geriDonus = 0.08f;

    public void Ayarla(
        float yeniMaksimumAci,
        float yeniGeriDonus)
    {
        maksimumAci =
            Mathf.Clamp(yeniMaksimumAci, 45f, 150f);

        geriDonus =
            Mathf.Max(0f, yeniGeriDonus);

        kapaliRotasyon =
            transform.rotation;
    }

    private void Awake()
    {
        body =
            GetComponent<Rigidbody>();

        kapaliRotasyon =
            transform.rotation;
    }

    private void FixedUpdate()
    {
        if (body == null)
            return;

        float kapaliY =
            kapaliRotasyon.eulerAngles.y;

        float mevcutAci =
            Mathf.DeltaAngle(
                kapaliY,
                transform.eulerAngles.y);

        if (geriDonus > 0f)
        {
            body.AddTorque(
                Vector3.up * (-mevcutAci * geriDonus),
                ForceMode.Acceleration);
        }

        if (mevcutAci > maksimumAci ||
            mevcutAci < -maksimumAci)
        {
            float sinirliAci =
                Mathf.Clamp(
                    mevcutAci,
                    -maksimumAci,
                    maksimumAci);

            body.MoveRotation(
                Quaternion.Euler(
                    0f,
                    kapaliY + sinirliAci,
                    0f));

            Vector3 acisalHiz =
                body.angularVelocity;

            if ((mevcutAci > maksimumAci && acisalHiz.y > 0f) ||
                (mevcutAci < -maksimumAci && acisalHiz.y < 0f))
            {
                acisalHiz.y = 0f;
                body.angularVelocity = acisalHiz;
            }
        }
    }
}