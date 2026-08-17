using System.Collections;
using UnityEngine;

/*
 * Güvenli varsayılan olarak sağ eli erken uzatan Open Door Outwards
 * klibini kapının iki tarafından da kullanır. Karakterin kök hareketini
 * animasyon karesine göre kod yönetir; sağ el IK ile kapı koluna gider.
 */
[DisallowMultipleComponent]
public class KapiGirisSistemi : MonoBehaviour
{
    [Header("Oyuncu - Boş Bırakılabilir")]
    public Transform oyuncu;
    public Animator oyuncuAnimatoru;
    public MonoBehaviour hareketKodu;

    [Header("Etkileşim")]
    public KeyCode etkilesimTusu = KeyCode.E;
    [Tooltip(
        "Kapı yüzeyinden ölçülen yakın etkileşim mesafesi. " +
        "Komşu kapıların menzilleri karışmasın diye en fazla 0.65 olabilir.")]
    [Range(0.25f, 0.65f)]
    public float etkilesimMesafesi = 0.55f;
    public bool otomatikBaslat = false;

    [Header("İçeri / Dışarı Yönü")]
    [Tooltip(
        "Açıksa kapının mavi Z oku evin dışını gösteriyor kabul edilir. " +
        "Yanlış animasyon seçilirse yalnızca bu kutuyu ters çevir.")]
    public bool kapiForwardTarafiDisarisi = true;

    [Header("Karakter Animator State Adları")]
    public string animatorKatmani = "Base Layer";
    public string iceriGirisStateAdi = "kapıdan içeri girme";
    public string disariCikisStateAdi = "kapıdan dışarı çıkma";
    public string idleStateAdi = "Idle";
    [Tooltip(
        "Açık bırak. İki yönde de daha güvenli olan tek kapı klibi kullanılır.")]
    public bool tekAnimasyonKullan = true;
    public string tekKapiStateAdi = "kapıdan dışarı çıkma";
    [Range(0f, 0.25f)]
    public float animasyonGecisSuresi = 0.06f;

    [Header("Kapıya Hizalama")]
    [Tooltip("Opening Door Inwards klibinin ölçülen başlangıç uzaklığı.")]
    public float iceriBaslangicMesafesi = 0.72f;
    [Tooltip("Open Door Outwards klibinin ölçülen başlangıç uzaklığı.")]
    public float disariBaslangicMesafesi = 0.83f;
    public float kapiSonrasiGuvenliMesafe = 0.85f;
    [Range(0.05f, 0.5f)]
    public float hizalanmaSuresi = 0.18f;
    public bool gecisBoyuncaYuksekligiKoru = true;

    [Header("Kapı Kolu - Sağ El Hizalama")]
    [Tooltip(
        "Kapı kolu ayrı bir nesneyse buraya ver. Boşsa mesh üzerinden " +
        "menteşenin karşı kenarında otomatik bulunur.")]
    public Transform kapiKolu;
    [Range(0.60f, 0.95f)]
    public float otomatikKolKenarOrani = 0.84f;
    [Range(0.35f, 0.65f)]
    public float otomatikKolYukseklikOrani = 0.50f;
    [Tooltip("Animasyonda sağ elin gövde merkezinden yatay erişimi.")]
    [Range(0.15f, 0.45f)]
    public float sagElYatayUzakligi = 0.29f;
    [Range(0f, 0.35f)]
    public float maksimumGovdeYanalKaymasi = 0.20f;
    [Range(0f, 1f)]
    public float sagElIKGucu = 1f;

    [Header("Fiziksel Kapı")]
    [Tooltip("Kapı kolu soldaysa menteşe sağdadır.")]
    public bool menteseSagda = false;
    [Range(45f, 120f)]
    public float acilmaAcisi = 88f;
    [Tooltip(
        "Açık bırak. Kapı kanadı daima karakterin başladığı taraftan " +
        "uzağa açılır; böylece karakterin içinden geçmez.")]
    public bool oyuncudanUzagaAc = true;
    [Tooltip(
        "Oyuncudan uzağa aç kapalıysa kullanılacak sabit yön. " +
        "+1 veya -1 verilebilir.")]
    [Range(-1, 1)]
    public int sabitAcilmaYonu = 1;

    [Header("Geçiş Hareketi")]
    [Tooltip(
        "Açık bırak. FBX'in yana kayan Root Motion'u yerine karakteri " +
        "kapıdan düz ve kontrollü geçirir.")]
    public bool gecisiKodlaTasi = true;
    [Tooltip(
        "FBX'te Root Transform Position XZ yanlışlıkla Bake Into Pose " +
        "yapılmışsa karakteri animasyon karesine göre kod taşır.")]
    public bool rootMotionYoksaOtomatikTasi = true;

    [Header("CANLI TEST - Play Modunda Bak")]
    [SerializeField]
    private string canliDurum = "Oyun henüz başlamadı";
    [SerializeField]
    private string secilenAnimasyon = "-";
    [SerializeField]
    [Range(0f, 1f)]
    private float animasyonOrani;
    [SerializeField]
    [Range(0f, 1f)]
    private float kapiAciklikOrani;
    [SerializeField]
    private bool rootMotionCalisti;
    [SerializeField]
    private float oyuncuyaMesafe;

    private enum KapiAnimasyonTipi
    {
        IceriGiris,
        DisariCikis
    }

    private const float EnBuyukEtkilesimMesafesi = 0.65f;
    private const float EnBuyukMerkezUzakligi = 1.10f;

    private static KapiGirisSistemi aktifKapi;
    private static KapiGirisSistemi seciliKapi;
    private static int secimKaresi = -1;

    private CharacterController oyuncuController;
    private KapiSagElIK sagElIK;
    private Collider[] kapiColliderlari;
    private bool[] colliderlarinOncekiDurumu;

    private Transform mentesePivoti;
    private Quaternion kapaliPivotRotasyonu;
    private Vector3 kapaliKapiForwardu;
    private Vector3 kapaliKapiMerkezi;
    private Bounds kapaliKapiYerelSiniri;
    private bool kapiColliderlariGeciciOlarakKapali;

    private bool oyuncuYakinda;
    private bool islemDevamEdiyor;
    private bool kontrollerKilitli;

    private bool hareketKoduOncedenAcikti;
    private bool controllerOncedenAcikti;
    private bool rootMotionOncedenAcikti;
    private float animatorOncekiHizi = 1f;

    private GUIStyle mesajStili;

    void Start()
    {
        etkilesimMesafesi = GecerliEtkilesimMesafesi();
        OyuncuyuBul();
        KapiKolunuBul();
        MentesePivotunuHazirla();
        KapiColliderlariniBul();
    }

    void Update()
    {
        SecimiBuKareIcinSifirla();
        oyuncuYakinda = false;

        if (islemDevamEdiyor ||
            (aktifKapi != null && aktifKapi != this))
        {
            return;
        }

        if (oyuncu == null)
        {
            OyuncuyuBul();
        }

        if (oyuncu == null || mentesePivoti == null)
        {
            return;
        }

        oyuncuyaMesafe = OyuncuyaGercekMesafeyiHesapla();

        if (oyuncuyaMesafe <= GecerliEtkilesimMesafesi())
        {
            EnYakinKapiAdayiOl();
        }
    }

    /*
     * Bütün kapıların Update'i bittikten sonra çalışır. Böylece E tuşunu
     * ve sol üstteki mesajı aynı karede yalnızca en yakın tek kapı alır.
     */
    void LateUpdate()
    {
        if (islemDevamEdiyor ||
            (aktifKapi != null && aktifKapi != this))
        {
            oyuncuYakinda = false;
            return;
        }

        oyuncuYakinda =
            seciliKapi == this &&
            oyuncu != null &&
            oyuncuyaMesafe <= GecerliEtkilesimMesafesi();

        canliDurum = oyuncuYakinda
            ? "HAZIR: En yakın kapı - E tuşuna bas"
            : "UZAK: Kapının dibine yaklaş";

        if (!oyuncuYakinda)
        {
            return;
        }

        bool baslatmaIstegi =
            otomatikBaslat || Input.GetKeyDown(etkilesimTusu);

        if (baslatmaIstegi)
        {
            StartCoroutine(KapiAkisi());
        }
    }

    private void SecimiBuKareIcinSifirla()
    {
        if (secimKaresi == Time.frameCount)
        {
            return;
        }

        secimKaresi = Time.frameCount;
        seciliKapi = null;
    }

    private void EnYakinKapiAdayiOl()
    {
        if (seciliKapi == null)
        {
            seciliKapi = this;
            return;
        }

        /*
         * Eşit mesafede o karede ilk bulunan kapı seçili kalır.
         * Böylece tek kapı kuralı korunur ve Unity sürümüne bağlı
         * GetInstanceID / GetEntityId API'lerine ihtiyaç duyulmaz.
         */
        if (oyuncuyaMesafe < seciliKapi.oyuncuyaMesafe)
        {
            seciliKapi = this;
        }
    }

    private float GecerliEtkilesimMesafesi()
    {
        return Mathf.Clamp(
            etkilesimMesafesi,
            0.25f,
            EnBuyukEtkilesimMesafesi
        );
    }

    private void OyuncuyuBul()
    {
        Transform oncekiOyuncu = oyuncu;
        CharacterController aktifController =
            AktifOyuncuControlleriniBul();

        if (aktifController != null)
        {
            oyuncuController = aktifController;
            oyuncu = aktifController.transform;
        }
        else if (oyuncu != null)
        {
            oyuncuController =
                oyuncu.GetComponent<CharacterController>();
        }

        if (oncekiOyuncu != oyuncu)
        {
            oyuncuAnimatoru = null;
            hareketKodu = null;
            sagElIK = null;
        }

        if (oyuncu == null)
        {
            canliDurum =
                "HATA: CharacterController taşıyan oyuncu bulunamadı";
            return;
        }

        if (oyuncuAnimatoru == null ||
            !oyuncuAnimatoru.transform.IsChildOf(oyuncu) &&
            oyuncuAnimatoru.transform != oyuncu)
        {
            oyuncuAnimatoru = oyuncu.GetComponent<Animator>();

            if (oyuncuAnimatoru == null)
            {
                oyuncuAnimatoru =
                    oyuncu.GetComponentInChildren<Animator>();
            }
        }

        if (hareketKodu == null)
        {
            hareketKodu =
                oyuncu.GetComponent("KarakterHareketi")
                as MonoBehaviour;
        }

        if (oyuncuAnimatoru != null)
        {
            sagElIK =
                oyuncuAnimatoru.GetComponent<KapiSagElIK>();

            if (sagElIK == null)
            {
                sagElIK =
                    oyuncuAnimatoru.gameObject
                        .AddComponent<KapiSagElIK>();
            }
        }
    }

    private CharacterController AktifOyuncuControlleriniBul()
    {
        CharacterController[] adaylar =
            FindObjectsByType<CharacterController>(
                FindObjectsSortMode.None
            );

        CharacterController enIyiAday = null;
        float enIyiPuan = float.PositiveInfinity;
        Camera anaKamera = Camera.main;

        for (int i = 0; i < adaylar.Length; i++)
        {
            CharacterController aday = adaylar[i];

            if (aday == null ||
                !aday.enabled ||
                !aday.gameObject.activeInHierarchy)
            {
                continue;
            }

            MonoBehaviour adayHareketKodu =
                aday.GetComponent("KarakterHareketi")
                as MonoBehaviour;

            float puan =
                adayHareketKodu != null && adayHareketKodu.enabled
                    ? 0f
                    : 10000f;

            if (anaKamera != null)
            {
                puan += (
                    aday.transform.position -
                    anaKamera.transform.position
                ).sqrMagnitude;
            }
            else if (aday.transform == oyuncu)
            {
                puan -= 100f;
            }

            if (puan < enIyiPuan)
            {
                enIyiPuan = puan;
                enIyiAday = aday;
            }
        }

        return enIyiAday;
    }

    private void MentesePivotunuHazirla()
    {
        if (mentesePivoti != null)
        {
            return;
        }

        MeshFilter meshFiltresi = GetComponent<MeshFilter>();

        if (meshFiltresi == null ||
            meshFiltresi.sharedMesh == null)
        {
            canliDurum =
                "HATA: Script, Mesh Filter bulunan kapı nesnesinde olmalı";
            Debug.LogError(name + ": Kapıda Mesh Filter bulunamadı.", this);
            return;
        }

        Bounds yerelSinir = meshFiltresi.sharedMesh.bounds;
        kapaliKapiYerelSiniri = yerelSinir;

        kapaliKapiForwardu = transform.forward.normalized;
        kapaliKapiMerkezi =
            transform.TransformPoint(yerelSinir.center);

        float menteseX = menteseSagda
            ? yerelSinir.max.x
            : yerelSinir.min.x;

        Vector3 menteseYerelNoktasi = new Vector3(
            menteseX,
            yerelSinir.center.y,
            yerelSinir.center.z
        );

        Vector3 menteseDunyaNoktasi =
            transform.TransformPoint(menteseYerelNoktasi);

        Transform eskiParent = transform.parent;

        GameObject pivotNesnesi =
            new GameObject(name + " - Otomatik Menteşe");

        mentesePivoti = pivotNesnesi.transform;
        mentesePivoti.SetParent(eskiParent, true);
        mentesePivoti.position = menteseDunyaNoktasi;
        mentesePivoti.rotation = transform.rotation;
        mentesePivoti.localScale = Vector3.one;

        transform.SetParent(mentesePivoti, true);
        kapaliPivotRotasyonu = mentesePivoti.localRotation;
    }

    private void KapiKolunuBul()
    {
        if (kapiKolu != null)
        {
            return;
        }

        Transform[] cocuklar =
            GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < cocuklar.Length; i++)
        {
            if (cocuklar[i] == transform)
            {
                continue;
            }

            string ad = cocuklar[i].name.ToLowerInvariant();

            if (ad.Contains("handle") ||
                ad.Contains("knob") ||
                ad.Contains("doorhandle") ||
                ad.Contains("kapı kol") ||
                ad.Contains("kapi kol"))
            {
                kapiKolu = cocuklar[i];
                return;
            }
        }
    }

    private Vector3 KapiKoluDunyaNoktasiniHesapla(
        float oyuncununTarafi)
    {
        if (kapiKolu != null)
        {
            Renderer kolRendereri =
                kapiKolu.GetComponent<Renderer>();

            return kolRendereri != null
                ? kolRendereri.bounds.center
                : kapiKolu.position;
        }

        float kolKenariX = menteseSagda
            ? kapaliKapiYerelSiniri.min.x
            : kapaliKapiYerelSiniri.max.x;

        Vector3 yerelKolNoktasi = new Vector3(
            Mathf.Lerp(
                kapaliKapiYerelSiniri.center.x,
                kolKenariX,
                otomatikKolKenarOrani
            ),
            Mathf.Lerp(
                kapaliKapiYerelSiniri.min.y,
                kapaliKapiYerelSiniri.max.y,
                otomatikKolYukseklikOrani
            ),
            oyuncununTarafi >= 0f
                ? kapaliKapiYerelSiniri.max.z
                : kapaliKapiYerelSiniri.min.z
        );

        return transform.TransformPoint(yerelKolNoktasi);
    }

    private float GuvenliAcilmaTarafiniBul(
        Vector3 oyuncuBaslangicNoktasi)
    {
        if (!oyuncudanUzagaAc)
        {
            return sabitAcilmaYonu >= 0 ? 1f : -1f;
        }

        Vector3 artiMerkez =
            AcilmisKapiMerkeziniHesapla(1f);
        Vector3 eksiMerkez =
            AcilmisKapiMerkeziniHesapla(-1f);

        Vector3 artiFarki =
            artiMerkez - oyuncuBaslangicNoktasi;
        Vector3 eksiFarki =
            eksiMerkez - oyuncuBaslangicNoktasi;

        artiFarki.y = 0f;
        eksiFarki.y = 0f;

        return artiFarki.sqrMagnitude >= eksiFarki.sqrMagnitude
            ? 1f
            : -1f;
    }

    private Vector3 AcilmisKapiMerkeziniHesapla(float yon)
    {
        Quaternion oncekiRotasyon =
            mentesePivoti.localRotation;

        mentesePivoti.localRotation =
            kapaliPivotRotasyonu *
            Quaternion.Euler(0f, acilmaAcisi * yon, 0f);

        Vector3 acilmisMerkez =
            transform.TransformPoint(
                kapaliKapiYerelSiniri.center
            );

        mentesePivoti.localRotation = oncekiRotasyon;
        return acilmisMerkez;
    }

    private void KapiColliderlariniBul()
    {
        kapiColliderlari = GetComponentsInChildren<Collider>(true);
        colliderlarinOncekiDurumu =
            new bool[kapiColliderlari.Length];
    }

    private float OyuncuyaGercekMesafeyiHesapla()
    {
        Vector3 oyuncuNoktasi =
            oyuncu.position + Vector3.up * 0.9f;

        /*
         * MeshCollider.ClosestPoint bazı kapı modellerinde uzaktaki
         * oyuncuyu collider'ın içindeymiş gibi görüp 0 döndürüyor.
         * Önce kapı merkezine kesin bir üst sınır koyuyoruz. Böylece
         * odanın öbür ucundaki bir kapı hiçbir koşulda aday olamaz.
         */
        Vector3 merkezFarki =
            oyuncu.position - kapaliKapiMerkezi;
        merkezFarki.y = 0f;

        float merkezUzakligi = merkezFarki.magnitude;

        if (merkezUzakligi > EnBuyukMerkezUzakligi)
        {
            return merkezUzakligi;
        }

        /*
         * Collider yerine kapı mesh'inin yerel Bounds kutusundaki en
         * yakın noktayı bul. Bu hesap her kapının kendi Transform'una
         * göre yapıldığı için komşu kapıların menzilleri karışmaz.
         */
        Vector3 oyuncuYerelNoktasi =
            transform.InverseTransformPoint(oyuncuNoktasi);

        Vector3 enYakinYerelNokta =
            kapaliKapiYerelSiniri.ClosestPoint(oyuncuYerelNoktasi);

        Vector3 kapiNoktasi =
            transform.TransformPoint(enYakinYerelNokta);

        float uzaklik =
            Vector3.Distance(oyuncuNoktasi, kapiNoktasi);

        /*
         * Inspector'daki değer karakter merkezinden değil, karakterin
         * fiziksel kapsülünün dışından kapı yüzeyine kalan boşluktur.
         * Bu sayede 0.25 gerçekten kapıya çok yakın bir mesafedir.
         */
        if (oyuncuController != null)
        {
            Vector3 olcek = oyuncuController.transform.lossyScale;
            float yatayOlcek = Mathf.Max(
                Mathf.Abs(olcek.x),
                Mathf.Abs(olcek.z)
            );

            uzaklik -= oyuncuController.radius * yatayOlcek;
        }

        return Mathf.Max(0f, uzaklik);
    }

    private IEnumerator KapiAkisi()
    {
        if (aktifKapi != null || islemDevamEdiyor)
        {
            yield break;
        }

        if (oyuncu == null ||
            oyuncuAnimatoru == null ||
            mentesePivoti == null)
        {
            canliDurum =
                "HATA: Oyuncu, Animator veya menteşe bulunamadı";
            yield break;
        }

        aktifKapi = this;
        islemDevamEdiyor = true;
        oyuncuYakinda = false;
        rootMotionCalisti = false;
        animasyonOrani = 0f;
        kapiAciklikOrani = 0f;

        float oyuncununTarafi = Mathf.Sign(
            Vector3.Dot(
                oyuncu.position - kapaliKapiMerkezi,
                kapaliKapiForwardu
            )
        );

        if (Mathf.Approximately(oyuncununTarafi, 0f))
        {
            oyuncununTarafi = 1f;
        }

        bool oyuncuDisarida = kapiForwardTarafiDisarisi
            ? oyuncununTarafi > 0f
            : oyuncununTarafi < 0f;

        KapiAnimasyonTipi yonAnimasyonTipi = oyuncuDisarida
            ? KapiAnimasyonTipi.IceriGiris
            : KapiAnimasyonTipi.DisariCikis;

        KapiAnimasyonTipi animasyonTipi = tekAnimasyonKullan
            ? KapiAnimasyonTipi.DisariCikis
            : yonAnimasyonTipi;

        string stateAdi = tekAnimasyonKullan
            ? tekKapiStateAdi
            : animasyonTipi == KapiAnimasyonTipi.IceriGiris
                ? iceriGirisStateAdi
                : disariCikisStateAdi;

        secilenAnimasyon = stateAdi;
        canliDurum = "HAZIRLANIYOR: " + stateAdi;

        int katman = oyuncuAnimatoru.GetLayerIndex(animatorKatmani);

        if (katman < 0)
        {
            katman = 0;
        }

        int stateHash = Animator.StringToHash(
            TamStateYolu(stateAdi)
        );

        if (!oyuncuAnimatoru.HasState(katman, stateHash))
        {
            canliDurum =
                "HATA: Animator state bulunamadı: " +
                TamStateYolu(stateAdi);

            Debug.LogError(canliDurum, oyuncuAnimatoru);
            IslemSonunuTemizle();
            yield break;
        }

        float baslangicMesafesi = animasyonTipi ==
                                  KapiAnimasyonTipi.IceriGiris
            ? iceriBaslangicMesafesi
            : disariBaslangicMesafesi;

        Vector3 gecisYonu =
            -kapaliKapiForwardu * oyuncununTarafi;
        gecisYonu.y = 0f;
        gecisYonu.Normalize();

        Quaternion hedefOyuncuRotasyonu =
            Quaternion.LookRotation(
                gecisYonu,
                Vector3.up
            );

        Vector3 kolNoktasi =
            KapiKoluDunyaNoktasiniHesapla(oyuncununTarafi);

        Vector3 karakterSagYonu =
            hedefOyuncuRotasyonu * Vector3.right;

        Vector3 kapiSagYonu = transform.right.normalized;

        Vector3 elHizaliGovdeNoktasi =
            kolNoktasi -
            karakterSagYonu * sagElYatayUzakligi;

        float govdeYanalKaymasi = Vector3.Dot(
            elHizaliGovdeNoktasi - kapaliKapiMerkezi,
            kapiSagYonu
        );

        govdeYanalKaymasi = Mathf.Clamp(
            govdeYanalKaymasi,
            -maksimumGovdeYanalKaymasi,
            maksimumGovdeYanalKaymasi
        );

        Vector3 gecisMerkezi =
            kapaliKapiMerkezi +
            kapiSagYonu * govdeYanalKaymasi;

        Vector3 baslangicNoktasi =
            gecisMerkezi -
            gecisYonu * baslangicMesafesi;

        Vector3 bitisNoktasi =
            gecisMerkezi +
            gecisYonu * kapiSonrasiGuvenliMesafe;

        baslangicNoktasi.y = oyuncu.position.y;
        bitisNoktasi.y = oyuncu.position.y;

        float acilmaTarafi =
            GuvenliAcilmaTarafiniBul(baslangicNoktasi);

        Quaternion acikPivotRotasyonu =
            kapaliPivotRotasyonu *
            Quaternion.Euler(
                0f,
                acilmaAcisi * acilmaTarafi,
                0f
            );

        KontrolleriKilitle();
        KapiColliderlariniAyarla(false);

        yield return OyuncuyuHizala(
            baslangicNoktasi,
            hedefOyuncuRotasyonu
        );

        float sabitYukseklik = oyuncu.position.y;
        Vector3 animasyonBaslangicPozisyonu = oyuncu.position;

        AnimatorBoolunuKapat("isWalking");
        AnimatorBoolunuKapat("isRunning");
        AnimatorBoolunuKapat("IsFalling");
        AnimatorBoolunuKapat("IsCrouching");
        AnimatorBoolunuKapat("IsCrouchWalking");
        AnimatorBoolunuKapat("IsOnStairs");
        AnimatorBoolunuKapat("IsStairMoving");
        AnimatorBoolunuKapat("IsStairDescending");

        if (sagElIK != null)
        {
            sagElIK.Kapat();
        }

        oyuncuAnimatoru.speed = 1f;
        oyuncuAnimatoru.applyRootMotion = !gecisiKodlaTasi;
        oyuncuAnimatoru.CrossFadeInFixedTime(
            stateHash,
            animasyonGecisSuresi,
            katman,
            0f
        );

        bool stateBasladi = false;
        float stateBeklemeSuresi = 1f;

        while (stateBeklemeSuresi > 0f)
        {
            AnimatorStateInfo beklenenState;

            if (HedefStateBilgisiniAl(
                    katman,
                    stateHash,
                    out beklenenState))
            {
                stateBasladi = true;
                break;
            }

            stateBeklemeSuresi -= Time.deltaTime;
            yield return null;
        }

        if (!stateBasladi)
        {
            canliDurum =
                "HATA: Animator state'e giremedi: " + stateAdi;
            Debug.LogError(canliDurum, oyuncuAnimatoru);
            IslemSonunuTemizle();
            yield break;
        }

        canliDurum = "OYNATILIYOR: " + stateAdi;

        bool rootMotionKontrolEdildi = gecisiKodlaTasi;
        bool yedekTasimaAktif = gecisiKodlaTasi;
        float guvenlikSayaci = 8f;

        while (guvenlikSayaci > 0f)
        {
            AnimatorStateInfo stateBilgisi;

            if (!HedefStateBilgisiniAl(
                    katman,
                    stateHash,
                    out stateBilgisi))
            {
                // State çıkış geçişine girdiyse son kare olarak tamamla.
                if (animasyonOrani >= 0.90f)
                {
                    animasyonOrani = 1f;
                    break;
                }

                canliDurum =
                    "UYARI: Kapı animasyonu başka state tarafından kesildi";
                break;
            }

            animasyonOrani = Mathf.Clamp01(
                stateBilgisi.normalizedTime
            );

            kapiAciklikOrani = KapiAcikliginiHesapla(
                animasyonTipi,
                animasyonOrani
            );

            mentesePivoti.localRotation =
                Quaternion.Slerp(
                    kapaliPivotRotasyonu,
                    acikPivotRotasyonu,
                    kapiAciklikOrani
                );

            if (sagElIK != null)
            {
                float elAgirligi =
                    SagElIKAgirliginiHesapla(animasyonOrani) *
                    sagElIKGucu;

                sagElIK.HedefiAyarla(
                    KapiKoluDunyaNoktasiniHesapla(
                        oyuncununTarafi
                    ),
                    elAgirligi,
                    elAgirligi * 0.35f
                );
            }

            if (!rootMotionKontrolEdildi &&
                animasyonOrani >= 0.20f)
            {
                Vector3 hareket =
                    oyuncu.position - animasyonBaslangicPozisyonu;

                hareket.y = 0f;

                float ileriIlerleme = Vector3.Dot(
                    hareket,
                    gecisYonu.normalized
                );

                rootMotionCalisti = ileriIlerleme > 0.04f;
                yedekTasimaAktif =
                    rootMotionYoksaOtomatikTasi &&
                    !rootMotionCalisti;

                rootMotionKontrolEdildi = true;

                if (yedekTasimaAktif)
                {
                    oyuncu.position = animasyonBaslangicPozisyonu;
                    Debug.LogWarning(
                        name +
                        ": FBX Root Motion ilerletmedi. " +
                        "Animasyon karesine bağlı güvenli taşıma açıldı.",
                        this
                    );
                }
            }

            if (yedekTasimaAktif)
            {
                float gecisOrani =
                    YedekGecisOraniniHesapla(
                        animasyonTipi,
                        animasyonOrani
                    );

                oyuncu.position = Vector3.Lerp(
                    animasyonBaslangicPozisyonu,
                    bitisNoktasi,
                    gecisOrani
                );

                oyuncu.rotation = hedefOyuncuRotasyonu;
            }
            else if (gecisBoyuncaYuksekligiKoru)
            {
                Vector3 konum = oyuncu.position;
                konum.y = sabitYukseklik;
                oyuncu.position = konum;
            }

            if (animasyonOrani >= 0.995f)
            {
                break;
            }

            guvenlikSayaci -= Time.deltaTime;
            yield return null;
        }

        // Kapı mutlaka tam kapalı kalır; yarım açıyla takılmaz.
        animasyonOrani = 1f;
        kapiAciklikOrani = 0f;
        mentesePivoti.localRotation = kapaliPivotRotasyonu;

        /*
         * Root Motion yanlış içe aktarılmış olsa bile kapı collider'ını
         * açmadan önce karakteri kesin olarak kapının öteki tarafına al.
         */
        float kalanTaraf = Vector3.Dot(
            oyuncu.position - kapaliKapiMerkezi,
            kapaliKapiForwardu
        ) * oyuncununTarafi;

        if (kalanTaraf > -0.25f)
        {
            oyuncu.position = bitisNoktasi;
            oyuncu.rotation = hedefOyuncuRotasyonu;
        }

        if (sagElIK != null)
        {
            sagElIK.Kapat();
        }

        IdleAnimasyonunaDon(katman);
        KapiColliderlariniAyarla(true);
        KontrolleriAc();

        islemDevamEdiyor = false;
        aktifKapi = null;
        canliDurum =
            "TAMAMLANDI: Karakter geçti ve kapı kapandı";
    }

    /*
     * FBX dosyalarından ölçülen özel zamanlama:
     *
     * İçeri (4.45 sn): el kapıya yaklaşık %30'da gider, karakter
     * %45'ten sonra geçer, arkasından %76-%96 arasında kapatır.
     *
     * Tek klip (3.15 sn): sağ el kola gider; kapı %14-%38 arasında
     * açılır, karakter %40-%76'da geçer ve %80-%98'de kapanır.
     */
    private float KapiAcikliginiHesapla(
        KapiAnimasyonTipi tip,
        float oran)
    {
        float acmaBaslangici;
        float acmaBitisi;
        float kapamaBaslangici;
        float kapamaBitisi;

        if (tip == KapiAnimasyonTipi.IceriGiris)
        {
            acmaBaslangici = 0.30f;
            acmaBitisi = 0.55f;
            kapamaBaslangici = 0.76f;
            kapamaBitisi = 0.96f;
        }
        else
        {
            acmaBaslangici = 0.14f;
            acmaBitisi = 0.38f;
            kapamaBaslangici = 0.80f;
            kapamaBitisi = 0.98f;
        }

        if (oran <= acmaBaslangici)
        {
            return 0f;
        }

        if (oran < acmaBitisi)
        {
            return Yumusa(
                Mathf.InverseLerp(
                    acmaBaslangici,
                    acmaBitisi,
                    oran
                )
            );
        }

        if (oran <= kapamaBaslangici)
        {
            return 1f;
        }

        if (oran < kapamaBitisi)
        {
            return 1f - Yumusa(
                Mathf.InverseLerp(
                    kapamaBaslangici,
                    kapamaBitisi,
                    oran
                )
            );
        }

        return 0f;
    }

    private float YedekGecisOraniniHesapla(
        KapiAnimasyonTipi tip,
        float oran)
    {
        float gecisBaslangici =
            tip == KapiAnimasyonTipi.IceriGiris
                ? 0.44f
                : 0.40f;

        float gecisBitisi =
            tip == KapiAnimasyonTipi.IceriGiris
                ? 0.82f
                : 0.76f;

        return Yumusa(
            Mathf.InverseLerp(
                gecisBaslangici,
                gecisBitisi,
                oran
            )
        );
    }

    private float SagElIKAgirliginiHesapla(float oran)
    {
        if (oran <= 0.05f)
        {
            return 0f;
        }

        if (oran < 0.12f)
        {
            return Yumusa(
                Mathf.InverseLerp(0.05f, 0.12f, oran)
            );
        }

        if (oran <= 0.28f)
        {
            return 1f;
        }

        if (oran < 0.46f)
        {
            return 1f - Yumusa(
                Mathf.InverseLerp(0.28f, 0.46f, oran)
            );
        }

        return 0f;
    }

    private float Yumusa(float oran)
    {
        oran = Mathf.Clamp01(oran);
        return oran * oran * (3f - 2f * oran);
    }

    private bool HedefStateBilgisiniAl(
        int katman,
        int hedefHash,
        out AnimatorStateInfo stateBilgisi)
    {
        AnimatorStateInfo mevcut =
            oyuncuAnimatoru.GetCurrentAnimatorStateInfo(katman);

        if (mevcut.fullPathHash == hedefHash)
        {
            stateBilgisi = mevcut;
            return true;
        }

        if (oyuncuAnimatoru.IsInTransition(katman))
        {
            AnimatorStateInfo sonraki =
                oyuncuAnimatoru.GetNextAnimatorStateInfo(katman);

            if (sonraki.fullPathHash == hedefHash)
            {
                stateBilgisi = sonraki;
                return true;
            }
        }

        stateBilgisi = mevcut;
        return false;
    }

    private IEnumerator OyuncuyuHizala(
        Vector3 hedefKonum,
        Quaternion hedefRotasyon)
    {
        Vector3 ilkKonum = oyuncu.position;
        Quaternion ilkRotasyon = oyuncu.rotation;
        float gecenSure = 0f;

        while (gecenSure < hizalanmaSuresi)
        {
            float oran = Yumusa(
                gecenSure /
                Mathf.Max(0.01f, hizalanmaSuresi)
            );

            oyuncu.position = Vector3.Lerp(
                ilkKonum,
                hedefKonum,
                oran
            );

            oyuncu.rotation = Quaternion.Slerp(
                ilkRotasyon,
                hedefRotasyon,
                oran
            );

            gecenSure += Time.deltaTime;
            yield return null;
        }

        oyuncu.position = hedefKonum;
        oyuncu.rotation = hedefRotasyon;
    }

    private void KontrolleriKilitle()
    {
        if (kontrollerKilitli)
        {
            return;
        }

        if (hareketKodu != null)
        {
            hareketKoduOncedenAcikti = hareketKodu.enabled;
            hareketKodu.enabled = false;
        }

        if (oyuncuController != null)
        {
            controllerOncedenAcikti = oyuncuController.enabled;
            oyuncuController.enabled = false;
        }

        if (oyuncuAnimatoru != null)
        {
            rootMotionOncedenAcikti =
                oyuncuAnimatoru.applyRootMotion;

            animatorOncekiHizi = oyuncuAnimatoru.speed;
        }

        kontrollerKilitli = true;
    }

    private void KontrolleriAc()
    {
        if (!kontrollerKilitli)
        {
            return;
        }

        if (oyuncuAnimatoru != null)
        {
            oyuncuAnimatoru.applyRootMotion =
                rootMotionOncedenAcikti;

            oyuncuAnimatoru.speed =
                animatorOncekiHizi > 0.01f
                    ? animatorOncekiHizi
                    : 1f;
        }

        if (oyuncuController != null)
        {
            oyuncuController.enabled =
                controllerOncedenAcikti;
        }

        if (hareketKodu != null)
        {
            hareketKodu.enabled =
                hareketKoduOncedenAcikti;
        }

        kontrollerKilitli = false;
    }

    private void KapiColliderlariniAyarla(bool ac)
    {
        if (kapiColliderlari == null)
        {
            return;
        }

        if (ac && !kapiColliderlariGeciciOlarakKapali)
        {
            return;
        }

        for (int i = 0; i < kapiColliderlari.Length; i++)
        {
            if (kapiColliderlari[i] == null)
            {
                continue;
            }

            if (!ac)
            {
                colliderlarinOncekiDurumu[i] =
                    kapiColliderlari[i].enabled;

                kapiColliderlari[i].enabled = false;
            }
            else
            {
                kapiColliderlari[i].enabled =
                    colliderlarinOncekiDurumu[i];
            }
        }

        kapiColliderlariGeciciOlarakKapali = !ac;
    }

    private void IdleAnimasyonunaDon(int katman)
    {
        if (oyuncuAnimatoru == null)
        {
            return;
        }

        int idleHash = Animator.StringToHash(
            TamStateYolu(idleStateAdi)
        );

        if (oyuncuAnimatoru.HasState(katman, idleHash))
        {
            AnimatorBoolunuKapat("isWalking");
            AnimatorBoolunuKapat("isRunning");
            AnimatorBoolunuKapat("IsFalling");
            AnimatorBoolunuKapat("IsStairMoving");
            AnimatorBoolunuKapat("IsCrouchWalking");

            oyuncuAnimatoru.speed = 1f;
            oyuncuAnimatoru.Play(
                idleHash,
                katman,
                0f
            );

            /*
             * Aynı karede Idle pose'unu uygula. Böylece kapı state'inin
             * son karesinde donup hareket kodunu kilitlemesi mümkün olmaz.
             */
            oyuncuAnimatoru.Update(0f);
        }
    }

    private string TamStateYolu(string stateAdi)
    {
        if (stateAdi.Contains("."))
        {
            return stateAdi;
        }

        return animatorKatmani + "." + stateAdi;
    }

    private void AnimatorBoolunuKapat(string parametreAdi)
    {
        if (oyuncuAnimatoru == null)
        {
            return;
        }

        AnimatorControllerParameter[] parametreler =
            oyuncuAnimatoru.parameters;

        for (int i = 0; i < parametreler.Length; i++)
        {
            if (parametreler[i].name == parametreAdi &&
                parametreler[i].type ==
                    AnimatorControllerParameterType.Bool)
            {
                oyuncuAnimatoru.SetBool(parametreAdi, false);
                return;
            }
        }
    }

    private void IslemSonunuTemizle()
    {
        if (mentesePivoti != null)
        {
            mentesePivoti.localRotation = kapaliPivotRotasyonu;
        }

        kapiAciklikOrani = 0f;

        if (sagElIK != null)
        {
            sagElIK.Kapat();
        }

        if (oyuncuAnimatoru != null)
        {
            int katman =
                oyuncuAnimatoru.GetLayerIndex(animatorKatmani);

            IdleAnimasyonunaDon(katman >= 0 ? katman : 0);
        }

        KapiColliderlariniAyarla(true);
        KontrolleriAc();
        islemDevamEdiyor = false;

        if (aktifKapi == this)
        {
            aktifKapi = null;
        }
    }

    void OnGUI()
    {
        if (seciliKapi != this ||
            !oyuncuYakinda ||
            islemDevamEdiyor)
        {
            return;
        }

        if (mesajStili == null)
        {
            mesajStili = new GUIStyle(GUI.skin.label);
            mesajStili.richText = true;
            mesajStili.fontSize = 22;
            mesajStili.normal.textColor = Color.white;
            mesajStili.alignment = TextAnchor.MiddleLeft;
        }

        GUI.Box(
            new Rect(20f, 20f, 520f, 54f),
            GUIContent.none
        );

        string mesaj = otomatikBaslat
            ? "Kapı geçişi başlıyor..."
            : "Kapıdan geçmek için " +
              "<color=#FFC01A><b>E</b></color> tuşuna basın";

        GUI.Label(
            new Rect(36f, 20f, 490f, 54f),
            mesaj,
            mesajStili
        );
    }

    void OnDisable()
    {
        oyuncuYakinda = false;

        if (seciliKapi == this)
        {
            seciliKapi = null;
        }

        if (!islemDevamEdiyor && !kontrollerKilitli)
        {
            return;
        }

        StopAllCoroutines();
        IslemSonunuTemizle();
    }

    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(
            kapaliKapiMerkezi,
            kapaliKapiMerkezi + kapaliKapiForwardu
        );

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(
            kapaliKapiMerkezi,
            GecerliEtkilesimMesafesi()
        );
    }

    void OnValidate()
    {
        etkilesimMesafesi = GecerliEtkilesimMesafesi();
    }
}