using System.Collections.Generic;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/*
 * KULLANIM
 * 1) Hareket edecek tek kapağı veya çekmeceyi Hierarchy'de seç.
 * 2) Add Component > Etkilesimli Kapak Cekmece ekle.
 * 3) Hareket Türü'nü seç. Başka nesne/kamera/animasyon atamak gerekmez.
 *
 * Bütün component örnekleri aynı crosshair sistemini paylaşır. Crosshair'in
 * değdiği en yakın etkileşimli parça yeşil çerçeve alır ve E ile açılıp kapanır.
 */
[DisallowMultipleComponent]
[DefaultExecutionOrder(5000)]
public class EtkilesimliKapakCekmece : MonoBehaviour
{
    public enum HareketTuru
    {
        Kapak_Mentesesi_Solda,
        Kapak_Mentesesi_Sagda,
        Cekmece
    }

    public enum CekmeceEkseni
    {
        Otomatik,
        Yerel_X,
        Yerel_Z
    }

    [Header("Tek Seçmen Gereken Ayar")]
    [Tooltip(
        "Sol/sağ seçimi kapağın menteşesinin bulunduğu kenardır. " +
        "Çekmece için Çekmece seç.")]
    public HareketTuru hareketTuru =
        HareketTuru.Kapak_Mentesesi_Solda;

    [Header("Hareket - Genelde Değiştirme")]
    [Range(15f, 160f)]
    public float kapakAcilmaAcisi = 105f;

    [Range(0.05f, 2f)]
    public float cekmeceAcilmaMesafesi = 0.38f;

    public CekmeceEkseni cekmeceEkseni =
        CekmeceEkseni.Otomatik;

    [Range(0.1f, 2f)]
    public float hareketSuresi = 0.4f;

    [Range(0.25f, 4f)]
    public float hareketHizi = 1f;

    [Tooltip(
        "Açılma yönünü kameranın/oyuncunun bulunduğu tarafa otomatik seçer.")]
    public bool oyuncuyaDogruOtomatikAc = true;

    [Tooltip("Otomatik yön yanlış çıkarsa yalnız bunu işaretle.")]
    public bool yonuTersCevir;

    public bool baslangictaAcik;

    [Header("Çakışma Önleme")]
    [Tooltip(
        "Bu mobilyanın üst kökünde eski Animator varsa dönüşü geri " +
        "yazmaması için kapatır.")]
    public bool ustMobilyaAnimatorunuKapat = true;

    [Header("Bakış ve E Tuşu")]
    [Tooltip("Boş bırak. Aktif oyun kamerası otomatik bulunur.")]
    public Camera bakisKamerasi;

    public KeyCode etkilesimTusu = KeyCode.E;

    [Tooltip(
        "Yakınlık alanı değildir; crosshair ışınının azami erişimidir.")]
    [Range(5f, 200f)]
    public float bakisMenzili = 100f;

    public LayerMask bakisKatmanlari =
        Physics.DefaultRaycastLayers;

    [Header("Beyaz Crosshair")]
    [Tooltip("Ekran merkezinden yukarı piksel miktarı.")]
    [Range(-250f, 300f)]
    public float crosshairDikeyKaydirma = 65f;

    [Range(6f, 30f)]
    public float crosshairBoyutu = 12f;

    [Range(1f, 5f)]
    public float crosshairKalinligi = 2f;

    [Header("Bakılan Parçanın Yeşil Çerçevesi")]
    public Color vurguRengi =
        new Color(0.05f, 1f, 0.18f, 1f);

    [Range(0.003f, 0.05f)]
    public float vurguKalinligi = 0.012f;

    [Range(0.002f, 0.08f)]
    public float vurguBoslugu = 0.018f;

    [Range(1f, 8f)]
    public float vurguParlakligi = 4f;

    [Range(0f, 10f)]
    public float vurguNabizHizi = 4f;

    [Header("CANLI TEST - Play Modunda Bak")]
    [SerializeField]
    private string canliDurum = "Oyun henüz başlamadı";

    [SerializeField]
    private bool crosshairBuNesnede;

    [SerializeField]
    private bool acik;

    [SerializeField]
    private bool hareketEdiyor;

    [SerializeField]
    private Transform hareketEttirilenTransform;

    private static readonly List<EtkilesimliKapakCekmece>
        aktifNesneler = new List<EtkilesimliKapakCekmece>();

    private static readonly RaycastHit[] bakisVuruslari =
        new RaycastHit[64];

    private static EtkilesimliKapakCekmece seciliNesne;
    private static int sonBakisKaresi = -1;
    private static GUIStyle mesajStili;

    private Renderer[] esasRendererlar;
    private Renderer[] vurguRendererlari;
    private Material vurguMateryali;
    private GameObject vurguKoku;
    private Bounds yerelSinir;

    private Vector3 kapaliYerelKonum;
    private Quaternion kapaliYerelRotasyon;
    private Vector3 acikYerelKonum;
    private Quaternion acikYerelRotasyon;

    private Vector3 hareketBaslangicKonumu;
    private Quaternion hareketBaslangicRotasyonu;
    private Vector3 hareketHedefKonumu;
    private Quaternion hareketHedefRotasyonu;
    private float hareketZamani;
    private bool sistemHazir;

    void Reset()
    {
        TuruNesneAdindanSec();
        bakisKatmanlari = Physics.DefaultRaycastLayers;
    }

    [ContextMenu("Türü Nesne Adından Otomatik Seç")]
    private void TuruNesneAdindanSec()
    {
        string nesneAdi = name.ToLowerInvariant();

        if (nesneAdi.Contains("drawer") ||
            nesneAdi.Contains("draw") ||
            nesneAdi.Contains("cekmec") ||
            nesneAdi.Contains("çekmec"))
        {
            hareketTuru = HareketTuru.Cekmece;
            return;
        }

        if (nesneAdi.Contains("right") ||
            nesneAdi.Contains("sag") ||
            nesneAdi.Contains("sağ") ||
            nesneAdi.EndsWith("_r") ||
            nesneAdi.EndsWith(" r"))
        {
            hareketTuru =
                HareketTuru.Kapak_Mentesesi_Sagda;
            return;
        }

        hareketTuru = HareketTuru.Kapak_Mentesesi_Solda;
    }

    void OnEnable()
    {
        if (!aktifNesneler.Contains(this))
        {
            aktifNesneler.Add(this);
        }
    }

    void Start()
    {
        SistemiHazirla();
    }

    private void SistemiHazirla()
    {
        CakisanAnimatoruKapat();

        esasRendererlar = GetComponentsInChildren<Renderer>(true);

        if (esasRendererlar == null || esasRendererlar.Length == 0)
        {
            canliDurum = "HATA: Bu nesnenin altında Renderer yok";
            Debug.LogError(
                name +
                ": Etkileşimli kapak/çekmece için Renderer bulunamadı.",
                this
            );
            return;
        }

        hareketEttirilenTransform = HareketTransforminiBul();
        kapaliYerelKonum = hareketEttirilenTransform.localPosition;
        kapaliYerelRotasyon = hareketEttirilenTransform.localRotation;

        if (!YerelSiniriHesapla(out yerelSinir))
        {
            canliDurum = "HATA: Nesne sınırları hesaplanamadı";
            return;
        }

        EtkilesimCollideriniHazirla();
        VurguCercevesiniOlustur();
        VurguyuGoster(false);

        sistemHazir = true;
        canliDurum = "HAZIR: Crosshair ile bu parçaya bak";

        if (baslangictaAcik)
        {
            AcikPozuHesapla(KamerayiBul());
            hareketEttirilenTransform.localPosition = acikYerelKonum;
            hareketEttirilenTransform.localRotation = acikYerelRotasyon;
            acik = true;
        }
        else
        {
            acik = false;
        }
    }

    private Transform HareketTransforminiBul()
    {
        Transform aday = transform.parent;

        if (aday == null)
        {
            return transform;
        }

        string adayAdi = aday.name.ToLowerInvariant();
        bool kapakTuru = hareketTuru != HareketTuru.Cekmece;

        bool uygunAd = kapakTuru
            ? adayAdi.Contains("door") ||
              adayAdi.Contains("kapak") ||
              adayAdi.Contains("hinge") ||
              adayAdi.Contains("mente")
            : adayAdi.Contains("drawer") ||
              adayAdi.Contains("draw") ||
              adayAdi.Contains("cekmec") ||
              adayAdi.Contains("çekmec");

        if (!uygunAd)
        {
            return transform;
        }

        Renderer[] adayRendererlari =
            aday.GetComponentsInChildren<Renderer>(true);

        // Parent yalnız bu hareketli parçayı taşıyorsa onu hazır pivot kabul et.
        // Böylece grp_doorsL/R döner, TVStand veya iki kapak birlikte dönmez.
        if (adayRendererlari.Length == esasRendererlar.Length)
        {
            return aday;
        }

        return transform;
    }

    void LateUpdate()
    {
        HareketiGuncelle();

        if (sonBakisKaresi == Time.frameCount)
        {
            return;
        }

        sonBakisKaresi = Time.frameCount;
        GenelBakisiGuncelle();
    }

    private static void GenelBakisiGuncelle()
    {
        EtkilesimliKapakCekmece lider = LideriBul();

        if (lider == null)
        {
            SecimiDegistir(null);
            return;
        }

        Camera kamera = lider.KamerayiBul();

        if (kamera == null)
        {
            lider.canliDurum = "HATA: Aktif kamera bulunamadı";
            SecimiDegistir(null);
            return;
        }

        Vector3 ekranNoktasi = lider.CrosshairEkranNoktasiniAl();
        Ray isin = kamera.ScreenPointToRay(ekranNoktasi);

        int vurusSayisi = Physics.RaycastNonAlloc(
            isin,
            bakisVuruslari,
            lider.bakisMenzili,
            lider.bakisKatmanlari,
            QueryTriggerInteraction.Collide
        );

        EtkilesimliKapakCekmece yeniSecim = null;
        float enYakinMesafe = float.PositiveInfinity;

        for (int i = 0; i < vurusSayisi; i++)
        {
            Collider vuranCollider = bakisVuruslari[i].collider;

            if (vuranCollider == null)
            {
                continue;
            }

            EtkilesimliKapakCekmece aday =
                vuranCollider.GetComponentInParent<
                    EtkilesimliKapakCekmece>();

            if (aday == null ||
                !aday.isActiveAndEnabled ||
                !aday.sistemHazir)
            {
                continue;
            }

            float mesafe = bakisVuruslari[i].distance;

            if (mesafe < enYakinMesafe)
            {
                enYakinMesafe = mesafe;
                yeniSecim = aday;
            }
        }

        SecimiDegistir(yeniSecim);

        if (seciliNesne == null)
        {
            return;
        }

        seciliNesne.VurguParlakliginiGuncelle();
        seciliNesne.canliDurum = seciliNesne.hareketEdiyor
            ? "BEKLE: Parça hareket ediyor"
            : seciliNesne.acik
                ? "HAZIR: E ile kapat"
                : "HAZIR: E ile aç";

        if (!seciliNesne.hareketEdiyor &&
            seciliNesne.EtkilesimTusunaBasildi())
        {
            seciliNesne.AcKapat();
        }
    }

    private static EtkilesimliKapakCekmece LideriBul()
    {
        for (int i = aktifNesneler.Count - 1; i >= 0; i--)
        {
            EtkilesimliKapakCekmece nesne = aktifNesneler[i];

            if (nesne == null || !nesne.isActiveAndEnabled)
            {
                aktifNesneler.RemoveAt(i);
            }
        }

        return aktifNesneler.Count > 0
            ? aktifNesneler[0]
            : null;
    }

    private static void SecimiDegistir(
        EtkilesimliKapakCekmece yeniSecim)
    {
        if (seciliNesne == yeniSecim)
        {
            return;
        }

        if (seciliNesne != null)
        {
            seciliNesne.crosshairBuNesnede = false;
            seciliNesne.VurguyuGoster(false);

            if (!seciliNesne.hareketEdiyor)
            {
                seciliNesne.canliDurum =
                    "HAZIR: Crosshair ile bu parçaya bak";
            }
        }

        seciliNesne = yeniSecim;

        if (seciliNesne != null)
        {
            seciliNesne.crosshairBuNesnede = true;
            seciliNesne.VurguyuGoster(true);
        }
    }

    private void AcKapat()
    {
        if (!sistemHazir || hareketEdiyor)
        {
            return;
        }

        bool acilacak = !acik;

        if (acilacak)
        {
            AcikPozuHesapla(KamerayiBul());
        }

        hareketBaslangicKonumu =
            hareketEttirilenTransform.localPosition;
        hareketBaslangicRotasyonu =
            hareketEttirilenTransform.localRotation;

        hareketHedefKonumu = acilacak
            ? acikYerelKonum
            : kapaliYerelKonum;
        hareketHedefRotasyonu = acilacak
            ? acikYerelRotasyon
            : kapaliYerelRotasyon;

        acik = acilacak;
        hareketZamani = 0f;
        hareketEdiyor = true;
    }

    private void HareketiGuncelle()
    {
        if (!hareketEdiyor)
        {
            return;
        }

        float gercekSure = hareketSuresi /
            Mathf.Max(0.01f, hareketHizi);

        hareketZamani += Time.deltaTime;

        float oran = Mathf.Clamp01(
            hareketZamani / Mathf.Max(0.01f, gercekSure)
        );

        float yumusakOran = oran * oran * (3f - 2f * oran);

        hareketEttirilenTransform.localPosition = Vector3.Lerp(
            hareketBaslangicKonumu,
            hareketHedefKonumu,
            yumusakOran
        );

        hareketEttirilenTransform.localRotation = Quaternion.Slerp(
            hareketBaslangicRotasyonu,
            hareketHedefRotasyonu,
            yumusakOran
        );

        if (oran >= 1f)
        {
            hareketEttirilenTransform.localPosition =
                hareketHedefKonumu;
            hareketEttirilenTransform.localRotation =
                hareketHedefRotasyonu;
            hareketEdiyor = false;
        }
    }

    private void AcikPozuHesapla(Camera kamera)
    {
        hareketEttirilenTransform.localPosition = kapaliYerelKonum;
        hareketEttirilenTransform.localRotation = kapaliYerelRotasyon;

        if (hareketTuru == HareketTuru.Cekmece)
        {
            CekmeceAcikPozunuHesapla(kamera);
        }
        else
        {
            KapakAcikPozunuHesapla(kamera);
        }
    }

    private void KapakAcikPozunuHesapla(Camera kamera)
    {
        bool menteseSolda =
            hareketTuru == HareketTuru.Kapak_Mentesesi_Solda;

        Vector3 menteseDunya = hareketEttirilenTransform.position;
        Vector3 merkezDunya =
            transform.TransformPoint(yerelSinir.center);
        Vector3 eksenDunya =
            hareketEttirilenTransform.up.normalized;

        float varsayilanIsaret = menteseSolda ? -1f : 1f;
        float isaret = varsayilanIsaret;

        if (oyuncuyaDogruOtomatikAc && kamera != null)
        {
            Quaternion artiDonus = Quaternion.AngleAxis(
                Mathf.Abs(kapakAcilmaAcisi),
                eksenDunya
            );
            Quaternion eksiDonus = Quaternion.AngleAxis(
                -Mathf.Abs(kapakAcilmaAcisi),
                eksenDunya
            );

            Vector3 artiMerkez = menteseDunya +
                artiDonus * (merkezDunya - menteseDunya);
            Vector3 eksiMerkez = menteseDunya +
                eksiDonus * (merkezDunya - menteseDunya);

            float artiUzaklik =
                (kamera.transform.position - artiMerkez).sqrMagnitude;
            float eksiUzaklik =
                (kamera.transform.position - eksiMerkez).sqrMagnitude;

            isaret = artiUzaklik <= eksiUzaklik ? 1f : -1f;
        }

        if (yonuTersCevir)
        {
            isaret *= -1f;
        }

        Quaternion dunyaDonus = Quaternion.AngleAxis(
            isaret * Mathf.Abs(kapakAcilmaAcisi),
            eksenDunya
        );

        // Kapakta konum kesinlikle değişmez. Modelde grp_doorsL/R gibi
        // bir menteşe grubu varsa o grup; yoksa parçanın kendi pivotu döner.
        Vector3 hedefDunyaKonum =
            hareketEttirilenTransform.position;
        Quaternion hedefDunyaRotasyon =
            dunyaDonus * hareketEttirilenTransform.rotation;

        DunyaPozunuYereleCevir(
            hedefDunyaKonum,
            hedefDunyaRotasyon,
            out acikYerelKonum,
            out acikYerelRotasyon
        );
    }

    private void CekmeceAcikPozunuHesapla(Camera kamera)
    {
        Vector3 yerelEksen = CekmeceYerelEkseniniAl();
        Vector3 dunyaAdim = transform.TransformVector(
            yerelEksen.normalized * cekmeceAcilmaMesafesi
        );

        float isaret = 1f;

        if (oyuncuyaDogruOtomatikAc && kamera != null)
        {
            Vector3 merkezDunya =
                transform.TransformPoint(yerelSinir.center);
            Vector3 artiMerkez = merkezDunya + dunyaAdim;
            Vector3 eksiMerkez = merkezDunya - dunyaAdim;

            float artiUzaklik =
                (kamera.transform.position - artiMerkez).sqrMagnitude;
            float eksiUzaklik =
                (kamera.transform.position - eksiMerkez).sqrMagnitude;

            isaret = artiUzaklik <= eksiUzaklik ? 1f : -1f;
        }

        if (yonuTersCevir)
        {
            isaret *= -1f;
        }

        Vector3 hedefDunyaKonum =
            hareketEttirilenTransform.position + dunyaAdim * isaret;

        DunyaPozunuYereleCevir(
            hedefDunyaKonum,
            hareketEttirilenTransform.rotation,
            out acikYerelKonum,
            out acikYerelRotasyon
        );
    }

    private Vector3 CekmeceYerelEkseniniAl()
    {
        if (cekmeceEkseni == CekmeceEkseni.Yerel_X)
        {
            return Vector3.right;
        }

        if (cekmeceEkseni == CekmeceEkseni.Yerel_Z)
        {
            return Vector3.forward;
        }

        return yerelSinir.size.x < yerelSinir.size.z
            ? Vector3.right
            : Vector3.forward;
    }

    private void DunyaPozunuYereleCevir(
        Vector3 dunyaKonum,
        Quaternion dunyaRotasyon,
        out Vector3 yerelKonum,
        out Quaternion yerelRotasyon)
    {
        Transform ebeveyn = hareketEttirilenTransform.parent;

        if (ebeveyn == null)
        {
            yerelKonum = dunyaKonum;
            yerelRotasyon = dunyaRotasyon;
            return;
        }

        yerelKonum = ebeveyn.InverseTransformPoint(dunyaKonum);
        yerelRotasyon =
            Quaternion.Inverse(ebeveyn.rotation) * dunyaRotasyon;
    }

    private Camera KamerayiBul()
    {
        if (bakisKamerasi != null &&
            bakisKamerasi.enabled &&
            bakisKamerasi.gameObject.activeInHierarchy)
        {
            return bakisKamerasi;
        }

        bakisKamerasi = Camera.main;

        if (bakisKamerasi != null)
        {
            return bakisKamerasi;
        }

        Camera[] kameralar =
            FindObjectsByType<Camera>(FindObjectsSortMode.None);

        float enYuksekDerinlik = float.NegativeInfinity;

        for (int i = 0; i < kameralar.Length; i++)
        {
            Camera aday = kameralar[i];

            if (aday == null ||
                !aday.enabled ||
                !aday.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (aday.depth >= enYuksekDerinlik)
            {
                enYuksekDerinlik = aday.depth;
                bakisKamerasi = aday;
            }
        }

        return bakisKamerasi;
    }

    private Vector3 CrosshairEkranNoktasiniAl()
    {
        return new Vector3(
            Screen.width * 0.5f,
            Screen.height * 0.5f + crosshairDikeyKaydirma,
            0f
        );
    }

    private bool EtkilesimTusunaBasildi()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard klavye = Keyboard.current;

        if (klavye != null)
        {
            Key yeniTus;

            if (System.Enum.TryParse(
                    etkilesimTusu.ToString(),
                    true,
                    out yeniTus) &&
                klavye[yeniTus].wasPressedThisFrame)
            {
                return true;
            }
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(etkilesimTusu);
#else
        return false;
#endif
    }

    private bool YerelSiniriHesapla(out Bounds sonuc)
    {
        sonuc = new Bounds(Vector3.zero, Vector3.zero);
        bool ilkNokta = true;

        for (int i = 0; i < esasRendererlar.Length; i++)
        {
            Renderer renderer = esasRendererlar[i];

            if (renderer == null ||
                (vurguKoku != null &&
                 renderer.transform.IsChildOf(vurguKoku.transform)))
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
                            transform.InverseTransformPoint(dunyaNoktasi);

                        if (ilkNokta)
                        {
                            sonuc = new Bounds(yerelNokta, Vector3.zero);
                            ilkNokta = false;
                        }
                        else
                        {
                            sonuc.Encapsulate(yerelNokta);
                        }
                    }
                }
            }
        }

        return !ilkNokta && sonuc.size.sqrMagnitude > 0.000001f;
    }

    private void EtkilesimCollideriniHazirla()
    {
        Collider kokCollider = GetComponent<Collider>();

        if (kokCollider != null)
        {
            return;
        }

        BoxCollider kutu = gameObject.AddComponent<BoxCollider>();
        kutu.center = yerelSinir.center;
        kutu.size = new Vector3(
            Mathf.Max(0.02f, yerelSinir.size.x),
            Mathf.Max(0.02f, yerelSinir.size.y),
            Mathf.Max(0.02f, yerelSinir.size.z)
        );
        kutu.isTrigger = true;
    }

    private void CakisanAnimatoruKapat()
    {
        if (!ustMobilyaAnimatorunuKapat)
        {
            return;
        }

        Animator animator = GetComponentInParent<Animator>();

        if (animator != null)
        {
            animator.enabled = false;
        }

        Animation legacyAnimation = GetComponentInParent<Animation>();

        if (legacyAnimation != null)
        {
            legacyAnimation.enabled = false;
        }
    }

    private void VurguCercevesiniOlustur()
    {
        vurguMateryali = VurguMateryaliOlustur();

        if (vurguMateryali == null)
        {
            return;
        }

        vurguKoku = new GameObject("__BakisVurgusu");
        vurguKoku.transform.SetParent(transform, false);
        vurguKoku.transform.localPosition = Vector3.zero;
        vurguKoku.transform.localRotation = Quaternion.identity;
        vurguKoku.transform.localScale = Vector3.one;

        List<Renderer> parcalar = new List<Renderer>(12);

        Vector3 min = yerelSinir.min - Vector3.one * vurguBoslugu;
        Vector3 max = yerelSinir.max + Vector3.one * vurguBoslugu;
        Vector3 merkez = (min + max) * 0.5f;
        Vector3 boyut = max - min;
        float k = Mathf.Max(0.003f, vurguKalinligi);

        for (int y = 0; y <= 1; y++)
        {
            for (int z = 0; z <= 1; z++)
            {
                parcalar.Add(VurguCubuguOlustur(
                    new Vector3(
                        merkez.x,
                        y == 0 ? min.y : max.y,
                        z == 0 ? min.z : max.z
                    ),
                    new Vector3(boyut.x + k, k, k)
                ));
            }
        }

        for (int x = 0; x <= 1; x++)
        {
            for (int z = 0; z <= 1; z++)
            {
                parcalar.Add(VurguCubuguOlustur(
                    new Vector3(
                        x == 0 ? min.x : max.x,
                        merkez.y,
                        z == 0 ? min.z : max.z
                    ),
                    new Vector3(k, boyut.y + k, k)
                ));
            }
        }

        for (int x = 0; x <= 1; x++)
        {
            for (int y = 0; y <= 1; y++)
            {
                parcalar.Add(VurguCubuguOlustur(
                    new Vector3(
                        x == 0 ? min.x : max.x,
                        y == 0 ? min.y : max.y,
                        merkez.z
                    ),
                    new Vector3(k, k, boyut.z + k)
                ));
            }
        }

        vurguRendererlari = parcalar.ToArray();
    }

    private Renderer VurguCubuguOlustur(
        Vector3 yerelKonum,
        Vector3 yerelBoyut)
    {
        GameObject cubuk =
            GameObject.CreatePrimitive(PrimitiveType.Cube);

        cubuk.name = "Yeşil Vurgu";
        cubuk.transform.SetParent(vurguKoku.transform, false);
        cubuk.transform.localPosition = yerelKonum;
        cubuk.transform.localRotation = Quaternion.identity;
        cubuk.transform.localScale = yerelBoyut;

        int ignoreRaycast = LayerMask.NameToLayer("Ignore Raycast");

        if (ignoreRaycast >= 0)
        {
            cubuk.layer = ignoreRaycast;
        }

        Collider collider = cubuk.GetComponent<Collider>();

        if (collider != null)
        {
            Destroy(collider);
        }

        Renderer renderer = cubuk.GetComponent<Renderer>();
        renderer.sharedMaterial = vurguMateryali;
        renderer.shadowCastingMode =
            UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.enabled = false;
        return renderer;
    }

    private Material VurguMateryaliOlustur()
    {
        Shader shader = Shader.Find(
            "Universal Render Pipeline/Unlit"
        );

        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        if (shader == null)
        {
            Debug.LogError(
                name + ": Yeşil vurgu için uygun Shader bulunamadı.",
                this
            );
            return null;
        }

        Material materyal = new Material(shader);
        materyal.name = name + " - Bakış Vurgusu";
        vurguMateryali = materyal;
        VurguRenginiUygula(vurguParlakligi);
        return materyal;
    }

    private void VurguyuGoster(bool goster)
    {
        if (vurguRendererlari == null)
        {
            return;
        }

        for (int i = 0; i < vurguRendererlari.Length; i++)
        {
            if (vurguRendererlari[i] != null)
            {
                vurguRendererlari[i].enabled = goster;
            }
        }
    }

    private void VurguParlakliginiGuncelle()
    {
        float nabiz = 0.88f +
            Mathf.Sin(Time.unscaledTime * vurguNabizHizi) * 0.12f;

        VurguRenginiUygula(vurguParlakligi * nabiz);
    }

    private void VurguRenginiUygula(float carpan)
    {
        if (vurguMateryali == null)
        {
            return;
        }

        Color renk = vurguRengi * Mathf.Max(1f, carpan);
        renk.a = 1f;

        if (vurguMateryali.HasProperty("_BaseColor"))
        {
            vurguMateryali.SetColor("_BaseColor", renk);
        }

        if (vurguMateryali.HasProperty("_Color"))
        {
            vurguMateryali.SetColor("_Color", renk);
        }

        if (vurguMateryali.HasProperty("_EmissionColor"))
        {
            vurguMateryali.EnableKeyword("_EMISSION");
            vurguMateryali.SetColor("_EmissionColor", renk);
        }
    }

    void OnGUI()
    {
        if (LideriBul() != this)
        {
            return;
        }

        CrosshairCiz();

        if (seciliNesne == null)
        {
            return;
        }

        if (mesajStili == null)
        {
            mesajStili = new GUIStyle(GUI.skin.label);
            mesajStili.richText = true;
            mesajStili.fontSize = 20;
            mesajStili.normal.textColor = Color.white;
            mesajStili.alignment = TextAnchor.MiddleLeft;
        }

        GUI.Box(
            new Rect(20f, 20f, 480f, 50f),
            GUIContent.none
        );

        string eylem = seciliNesne.acik
            ? "kapatmak"
            : "açmak";

        string mesaj = seciliNesne.name + " " + eylem +
            " için <color=#FFC01A><b>" +
            seciliNesne.etkilesimTusu +
            "</b></color> tuşuna bas";

        GUI.Label(
            new Rect(34f, 20f, 450f, 50f),
            mesaj,
            mesajStili
        );
    }

    private void CrosshairCiz()
    {
        Vector3 ekranNoktasi = CrosshairEkranNoktasiniAl();
        float merkezX = ekranNoktasi.x;
        float merkezY = Screen.height - ekranNoktasi.y;

        Color oncekiRenk = GUI.color;
        GUI.color = Color.white;

        GUI.DrawTexture(
            new Rect(
                merkezX - crosshairBoyutu * 0.5f,
                merkezY - crosshairKalinligi * 0.5f,
                crosshairBoyutu,
                crosshairKalinligi
            ),
            Texture2D.whiteTexture
        );

        GUI.DrawTexture(
            new Rect(
                merkezX - crosshairKalinligi * 0.5f,
                merkezY - crosshairBoyutu * 0.5f,
                crosshairKalinligi,
                crosshairBoyutu
            ),
            Texture2D.whiteTexture
        );

        GUI.color = oncekiRenk;
    }

    void OnDisable()
    {
        aktifNesneler.Remove(this);

        if (seciliNesne == this)
        {
            SecimiDegistir(null);
        }

        VurguyuGoster(false);
    }

    void OnDestroy()
    {
        aktifNesneler.Remove(this);

        if (vurguMateryali != null)
        {
            Destroy(vurguMateryali);
        }
    }

    void OnValidate()
    {
        kapakAcilmaAcisi =
            Mathf.Clamp(kapakAcilmaAcisi, 15f, 160f);
        cekmeceAcilmaMesafesi =
            Mathf.Clamp(cekmeceAcilmaMesafesi, 0.05f, 2f);
        hareketSuresi = Mathf.Clamp(hareketSuresi, 0.1f, 2f);
        hareketHizi = Mathf.Clamp(hareketHizi, 0.25f, 4f);
        bakisMenzili = Mathf.Clamp(bakisMenzili, 5f, 200f);
        crosshairBoyutu = Mathf.Clamp(crosshairBoyutu, 6f, 30f);
        crosshairKalinligi =
            Mathf.Clamp(crosshairKalinligi, 1f, 5f);
    }
}