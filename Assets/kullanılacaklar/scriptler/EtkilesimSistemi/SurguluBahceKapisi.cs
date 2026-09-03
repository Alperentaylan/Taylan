using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Yana kayan bahce kapisi icin sinematik etkilesim akisi.
/// Karakteri kapinin yanina hizalar, sol eli IK ile kapida tutar,
/// ondeki etkilesim kamerasina gecer ve kapiyi yavasca kaydirir.
/// </summary>
[DisallowMultipleComponent]
public sealed class SurguluBahceKapisi : MonoBehaviour
{
    public enum EtkilesimModu
    {
        YaklasincaOtomatik,
        EBasiliTut,
        SolaBasiliTut
    }

    [Header("TEMEL REFERANSLAR")]
    [SerializeField] private Transform hareketEdecekKapi;
    [SerializeField] private Transform oyuncuKoku;
    [SerializeField] private Animator oyuncuAnimatoru;
    [SerializeField] private Transform oyuncuHizalamaNoktasi;
    [SerializeField] private Transform solElTutmaNoktasi;
    [SerializeField] private Transform kameraNoktasi;

    [Header("ETKILESIM")]
    [SerializeField] private EtkilesimModu etkilesimModu =
        EtkilesimModu.YaklasincaOtomatik;
    [Min(0.5f)]
    [SerializeField] private float etkilesimMesafesi = 1.8f;
    [Min(0.05f)]
    [SerializeField] private float hizalamaSuresi = 0.55f;
    [Min(0f)]
    [SerializeField] private float eliYerlestirmeBeklemesi = 0.3f;
    [SerializeField] private bool yalnizcaBirKezAc = true;

    [Header("KAPININ KAYMASI")]
    [Tooltip("Kapinin kendi ekseninde acilma yonu. Varsayilan yerel sol (-X).")]
    [SerializeField] private Vector3 yerelAcilmaYonu = Vector3.left;
    [Min(0.1f)]
    [SerializeField] private float acilmaMesafesi = 3.2f;
    [Min(0.25f)]
    [SerializeField] private float acilmaSuresi = 4.2f;
    [Range(0f, 1f)]
    [SerializeField] private float oyuncuTakipOrani = 0.9f;

    [Header("SOL EL IK")]
    [Range(0f, 1f)]
    [SerializeField] private float solElPozisyonAgirligi = 1f;
    [Range(0f, 1f)]
    [SerializeField] private float solElRotasyonAgirligi = 0.35f;
    [Min(0.1f)]
    [SerializeField] private float ikGecisHizi = 5f;

    [Header("SINEMATIK KAMERA")]
    [Min(0.05f)]
    [SerializeField] private float kameraGecisSuresi = 0.65f;
    [Min(1f)]
    [SerializeField] private float kameraUzakligi = 3.1f;
    [SerializeField] private float kameraYuksekligi = 1.55f;
    [SerializeField] private float kameraYatayOfset = 0.2f;
    [SerializeField] private float bakisYuksekligi = 1.25f;
    [Range(25f, 85f)]
    [SerializeField] private float etkilesimKameraFov = 47f;

    [Header("EKRAN OKU")]
    [SerializeField] private Color okRengi =
        new Color(1f, 0.72f, 0.12f, 1f);
    [SerializeField] private Vector2 okEkranOfseti =
        new Vector2(-125f, 25f);
    [SerializeField] private Vector2 okBoyutu =
        new Vector2(150f, 76f);

    [Header("CANLI DURUM")]
    [SerializeField] private bool oyuncuMenzilde;
    [SerializeField] private bool etkilesimDevamEdiyor;
    [SerializeField, Range(0f, 1f)] private float acilmaOrani;

    private readonly List<DavranisDurumu> kapatilanDavranislar =
        new List<DavranisDurumu>();

    private CharacterController oyuncuController;
    private bool controllerOncekiDurumu;
    private SurguluKapiSolElIK solElIK;
    private Camera kaynakKamera;
    private Camera etkilesimKamerasi;
    private Canvas okCanvas;
    private SurguluKapiYonOku yonOku;
    private Coroutine aktifAkis;
    private Vector3 kapininKapaliDunyaKonumu;
    private Vector3 oyuncununHizalanmisKonumu;
    private Quaternion oyuncununHizalanmisRotasyonu;
    private bool isWalkingParametresiVar;
    private bool oncekiIsWalking;
    private bool tamamlandi;
    private float oyuncuAramaZamani;

    private struct DavranisDurumu
    {
        public MonoBehaviour davranis;
        public bool aktifti;

        public DavranisDurumu(MonoBehaviour davranis, bool aktifti)
        {
            this.davranis = davranis;
            this.aktifti = aktifti;
        }
    }

    private void Awake()
    {
        if (hareketEdecekKapi == null)
            hareketEdecekKapi = transform;

        kapininKapaliDunyaKonumu = hareketEdecekKapi.position;
        ReferanslariTamamla();
        OkArayuzunuHazirla();
    }

    private void Update()
    {
        if (etkilesimDevamEdiyor ||
            (tamamlandi && yalnizcaBirKezAc))
        {
            return;
        }

        if (oyuncuKoku == null && Time.unscaledTime >= oyuncuAramaZamani)
        {
            oyuncuAramaZamani = Time.unscaledTime + 1f;
            ReferanslariTamamla();
        }

        if (oyuncuKoku == null)
            return;

        oyuncuMenzilde = OyuncuyaMesafe() <= etkilesimMesafesi;

        if (!oyuncuMenzilde)
            return;

        bool baslat = etkilesimModu == EtkilesimModu.YaklasincaOtomatik;

        if (etkilesimModu == EtkilesimModu.EBasiliTut)
            baslat = Input.GetKeyDown(KeyCode.E);
        else if (etkilesimModu == EtkilesimModu.SolaBasiliTut)
            baslat = SolaBasiliyorMu();

        if (baslat)
            aktifAkis = StartCoroutine(KapiAkisi());
    }

    private void LateUpdate()
    {
        if (!etkilesimDevamEdiyor || yonOku == null ||
            etkilesimKamerasi == null || oyuncuKoku == null)
        {
            return;
        }

        Vector3 ekranNoktasi = etkilesimKamerasi.WorldToScreenPoint(
            oyuncuKoku.position + Vector3.up * bakisYuksekligi
        );

        yonOku.EkranKonumunuAyarla(ekranNoktasi, okEkranOfseti);
    }

    private IEnumerator KapiAkisi()
    {
        etkilesimDevamEdiyor = true;
        oyuncuMenzilde = true;
        ReferanslariTamamla();

        if (oyuncuKoku == null || oyuncuAnimatoru == null ||
            oyuncuHizalamaNoktasi == null || solElTutmaNoktasi == null)
        {
            Debug.LogError(
                "SurguluBahceKapisi: Oyuncu, Animator veya kurulum noktalari eksik.",
                this
            );
            etkilesimDevamEdiyor = false;
            yield break;
        }

        KontrolleriKilitle();
        AnimatorYuruyusunuHazirla();
        yield return OyuncuyuHizala();

        SolElIKyiBaslat();
        EtkilesimKamerasiniHazirla();
        OkGoster(false);
        yield return KamerayiEtkilesimeGecir();

        OkGoster(true);
        if (eliYerlestirmeBeklemesi > 0f)
        {
            yield return new WaitForSecondsRealtime(
                eliYerlestirmeBeklemesi
            );
        }

        Vector3 oyuncuBaslangic = oyuncuKoku.position;
        Quaternion oyuncuRotasyonu = oyuncuKoku.rotation;
        Vector3 yerelYon = yerelAcilmaYonu.sqrMagnitude > 0.0001f
            ? yerelAcilmaYonu.normalized
            : Vector3.left;
        Vector3 dunyaYonu = hareketEdecekKapi.TransformDirection(
            yerelYon
        ).normalized;

        float sure = Mathf.Max(0.25f, acilmaSuresi);

        while (acilmaOrani < 1f)
        {
            bool ilerliyor = AcmaGirdisiAktif();
            AnimatorYuruyusunuAyarla(ilerliyor);

            if (ilerliyor)
            {
                acilmaOrani = Mathf.MoveTowards(
                    acilmaOrani,
                    1f,
                    Time.unscaledDeltaTime / sure
                );
            }

            float yumusakOran = Mathf.SmoothStep(0f, 1f, acilmaOrani);

            hareketEdecekKapi.position =
                kapininKapaliDunyaKonumu +
                dunyaYonu * (acilmaMesafesi * yumusakOran);

            oyuncuKoku.position =
                oyuncuBaslangic +
                dunyaYonu *
                (acilmaMesafesi * oyuncuTakipOrani * yumusakOran);
            oyuncuKoku.rotation = oyuncuRotasyonu;

            KameraHedefiniTakipEt();
            yield return null;
        }

        AnimatorYuruyusunuAyarla(false);
        tamamlandi = true;
        yield return new WaitForSecondsRealtime(0.25f);

        SolElIKyiKapat();
        OkGoster(false);
        yield return KamerayiGeriGetir();

        KontrolleriAc();
        etkilesimDevamEdiyor = false;
        aktifAkis = null;
    }

    private IEnumerator OyuncuyuHizala()
    {
        Vector3 baslangicKonumu = oyuncuKoku.position;
        Quaternion baslangicRotasyonu = oyuncuKoku.rotation;
        oyuncununHizalanmisKonumu = oyuncuHizalamaNoktasi.position;
        oyuncununHizalanmisRotasyonu = oyuncuHizalamaNoktasi.rotation;

        float gecen = 0f;
        float sure = Mathf.Max(0.05f, hizalamaSuresi);

        while (gecen < sure)
        {
            gecen += Time.unscaledDeltaTime;
            float oran = Mathf.SmoothStep(0f, 1f, gecen / sure);

            oyuncuKoku.position = Vector3.Lerp(
                baslangicKonumu,
                oyuncununHizalanmisKonumu,
                oran
            );
            oyuncuKoku.rotation = Quaternion.Slerp(
                baslangicRotasyonu,
                oyuncununHizalanmisRotasyonu,
                oran
            );

            yield return null;
        }

        oyuncuKoku.SetPositionAndRotation(
            oyuncununHizalanmisKonumu,
            oyuncununHizalanmisRotasyonu
        );
    }

    private void EtkilesimKamerasiniHazirla()
    {
        kaynakKamera = Camera.main;

        if (kaynakKamera == null)
        {
            Camera[] kameralar = Camera.allCameras;
            if (kameralar.Length > 0)
                kaynakKamera = kameralar[0];
        }

        if (etkilesimKamerasi == null)
        {
            GameObject kameraObjesi = new GameObject(
                "SurguluKapi_EtkilesimKamerasi"
            );
            kameraObjesi.transform.SetParent(transform, true);
            etkilesimKamerasi = kameraObjesi.AddComponent<Camera>();
        }

        if (kaynakKamera != null)
        {
            etkilesimKamerasi.CopyFrom(kaynakKamera);
            etkilesimKamerasi.transform.SetPositionAndRotation(
                kaynakKamera.transform.position,
                kaynakKamera.transform.rotation
            );
            etkilesimKamerasi.depth = kaynakKamera.depth + 50f;
        }

        etkilesimKamerasi.targetTexture = null;
        etkilesimKamerasi.fieldOfView = etkilesimKameraFov;
        etkilesimKamerasi.tag = "Untagged";
        etkilesimKamerasi.enabled = true;
    }

    private IEnumerator KamerayiEtkilesimeGecir()
    {
        Vector3 ilkKonum = etkilesimKamerasi.transform.position;
        Quaternion ilkRotasyon = etkilesimKamerasi.transform.rotation;
        float gecen = 0f;
        float sure = Mathf.Max(0.05f, kameraGecisSuresi);

        while (gecen < sure)
        {
            gecen += Time.unscaledDeltaTime;
            float oran = Mathf.SmoothStep(0f, 1f, gecen / sure);
            KameraPozunuHesapla(out Vector3 hedefKonum, out Quaternion hedefRotasyon);

            etkilesimKamerasi.transform.SetPositionAndRotation(
                Vector3.Lerp(ilkKonum, hedefKonum, oran),
                Quaternion.Slerp(ilkRotasyon, hedefRotasyon, oran)
            );
            yield return null;
        }

        KameraHedefiniTakipEt();
    }

    private IEnumerator KamerayiGeriGetir()
    {
        if (etkilesimKamerasi == null)
            yield break;

        Vector3 ilkKonum = etkilesimKamerasi.transform.position;
        Quaternion ilkRotasyon = etkilesimKamerasi.transform.rotation;
        float ilkFov = etkilesimKamerasi.fieldOfView;
        float gecen = 0f;
        float sure = Mathf.Max(0.05f, kameraGecisSuresi);

        while (gecen < sure && kaynakKamera != null)
        {
            gecen += Time.unscaledDeltaTime;
            float oran = Mathf.SmoothStep(0f, 1f, gecen / sure);

            etkilesimKamerasi.transform.SetPositionAndRotation(
                Vector3.Lerp(
                    ilkKonum,
                    kaynakKamera.transform.position,
                    oran
                ),
                Quaternion.Slerp(
                    ilkRotasyon,
                    kaynakKamera.transform.rotation,
                    oran
                )
            );
            etkilesimKamerasi.fieldOfView = Mathf.Lerp(
                ilkFov,
                kaynakKamera.fieldOfView,
                oran
            );
            yield return null;
        }

        etkilesimKamerasi.enabled = false;
    }

    private void KameraHedefiniTakipEt()
    {
        if (etkilesimKamerasi == null)
            return;

        KameraPozunuHesapla(out Vector3 konum, out Quaternion rotasyon);
        etkilesimKamerasi.transform.SetPositionAndRotation(konum, rotasyon);
    }

    private void KameraPozunuHesapla(
        out Vector3 konum,
        out Quaternion rotasyon)
    {
        Vector3 bakisNoktasi =
            oyuncuKoku.position + Vector3.up * bakisYuksekligi;

        if (kameraNoktasi != null)
        {
            konum = kameraNoktasi.position;
        }
        else
        {
            konum =
                oyuncuKoku.position +
                oyuncuKoku.forward * kameraUzakligi +
                oyuncuKoku.right * kameraYatayOfset +
                Vector3.up * kameraYuksekligi;
        }

        Vector3 bakisYonu = bakisNoktasi - konum;
        rotasyon = bakisYonu.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(bakisYonu.normalized, Vector3.up)
            : oyuncuKoku.rotation;
    }

    private void SolElIKyiBaslat()
    {
        solElIK = oyuncuAnimatoru.GetComponent<SurguluKapiSolElIK>();

        if (solElIK == null)
            solElIK = oyuncuAnimatoru.gameObject.AddComponent<SurguluKapiSolElIK>();

        solElIK.HedefiAyarla(
            solElTutmaNoktasi,
            solElPozisyonAgirligi,
            solElRotasyonAgirligi,
            ikGecisHizi
        );
    }

    private void SolElIKyiKapat()
    {
        if (solElIK != null)
            solElIK.Kapat();
    }

    private void KontrolleriKilitle()
    {
        kapatilanDavranislar.Clear();

        MonoBehaviour[] davranislar =
            oyuncuKoku.GetComponentsInChildren<MonoBehaviour>(true);

        for (int i = 0; i < davranislar.Length; i++)
        {
            MonoBehaviour davranis = davranislar[i];
            if (davranis == null || davranis == this)
                continue;

            string tipAdi = davranis.GetType().Name;
            bool kontrolKodu =
                tipAdi == "KarakterHareketi" ||
                tipAdi == "BirinciUcuncuSahisKesin" ||
                tipAdi == "OyuncuEtkilesim";

            if (!kontrolKodu)
                continue;

            kapatilanDavranislar.Add(
                new DavranisDurumu(davranis, davranis.enabled)
            );
            davranis.enabled = false;
        }

        oyuncuController = oyuncuKoku.GetComponent<CharacterController>();
        if (oyuncuController == null)
        {
            oyuncuController =
                oyuncuKoku.GetComponentInChildren<CharacterController>();
        }

        if (oyuncuController != null)
        {
            controllerOncekiDurumu = oyuncuController.enabled;
            oyuncuController.enabled = false;
        }
    }

    private void KontrolleriAc()
    {
        AnimatorYuruyusunuGeriYukle();

        if (oyuncuController != null)
            oyuncuController.enabled = controllerOncekiDurumu;

        for (int i = 0; i < kapatilanDavranislar.Count; i++)
        {
            DavranisDurumu durum = kapatilanDavranislar[i];
            if (durum.davranis != null)
                durum.davranis.enabled = durum.aktifti;
        }

        kapatilanDavranislar.Clear();
    }

    private void AnimatorYuruyusunuHazirla()
    {
        isWalkingParametresiVar = false;
        if (oyuncuAnimatoru == null)
            return;

        AnimatorControllerParameter[] parametreler =
            oyuncuAnimatoru.parameters;

        for (int i = 0; i < parametreler.Length; i++)
        {
            if (parametreler[i].name == "isWalking" &&
                parametreler[i].type == AnimatorControllerParameterType.Bool)
            {
                isWalkingParametresiVar = true;
                oncekiIsWalking = oyuncuAnimatoru.GetBool("isWalking");
                break;
            }
        }

        AnimatorYuruyusunuAyarla(false);
    }

    private void AnimatorYuruyusunuAyarla(bool yuruyor)
    {
        if (oyuncuAnimatoru != null && isWalkingParametresiVar)
            oyuncuAnimatoru.SetBool("isWalking", yuruyor);
    }

    private void AnimatorYuruyusunuGeriYukle()
    {
        if (oyuncuAnimatoru != null && isWalkingParametresiVar)
            oyuncuAnimatoru.SetBool("isWalking", oncekiIsWalking);
    }

    private void ReferanslariTamamla()
    {
        if (oyuncuAnimatoru != null && oyuncuKoku == null)
        {
            CharacterController controller =
                oyuncuAnimatoru.GetComponentInParent<CharacterController>();
            oyuncuKoku = controller != null
                ? controller.transform
                : oyuncuAnimatoru.transform.root;
        }

        if (oyuncuKoku == null)
        {
            Animator[] animatorler = FindObjectsByType<Animator>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

            for (int i = 0; i < animatorler.Length; i++)
            {
                Animator aday = animatorler[i];
                if (aday == null || aday.avatar == null ||
                    !aday.avatar.isHuman)
                {
                    continue;
                }

                CharacterController controller =
                    aday.GetComponentInParent<CharacterController>();

                if (controller != null)
                {
                    oyuncuAnimatoru = aday;
                    oyuncuKoku = controller.transform;
                    break;
                }
            }
        }

        if (oyuncuAnimatoru == null && oyuncuKoku != null)
        {
            oyuncuAnimatoru = oyuncuKoku.GetComponent<Animator>();
            if (oyuncuAnimatoru == null)
            {
                oyuncuAnimatoru =
                    oyuncuKoku.GetComponentInChildren<Animator>(true);
            }
        }
    }

    private float OyuncuyaMesafe()
    {
        Vector3 hedef = oyuncuHizalamaNoktasi != null
            ? oyuncuHizalamaNoktasi.position
            : hareketEdecekKapi.position;

        Vector3 fark = oyuncuKoku.position - hedef;
        fark.y = 0f;
        return fark.magnitude;
    }

    private bool AcmaGirdisiAktif()
    {
        switch (etkilesimModu)
        {
            case EtkilesimModu.EBasiliTut:
                return Input.GetKey(KeyCode.E);
            case EtkilesimModu.SolaBasiliTut:
                return SolaBasiliyorMu();
            default:
                return true;
        }
    }

    private static bool SolaBasiliyorMu()
    {
        return Input.GetKey(KeyCode.A) ||
               Input.GetKey(KeyCode.LeftArrow) ||
               Input.GetAxisRaw("Horizontal") < -0.25f;
    }

    private void OkArayuzunuHazirla()
    {
        if (okCanvas != null)
            return;

        GameObject canvasObjesi = new GameObject(
            "SurguluKapi_YonOkuCanvas"
        );
        canvasObjesi.transform.SetParent(transform, false);

        okCanvas = canvasObjesi.AddComponent<Canvas>();
        okCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        okCanvas.sortingOrder = 500;

        CanvasScaler scaler = canvasObjesi.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject okObjesi = new GameObject("SolaDogruOk");
        okObjesi.transform.SetParent(canvasObjesi.transform, false);
        yonOku = okObjesi.AddComponent<SurguluKapiYonOku>();
        yonOku.raycastTarget = false;
        yonOku.color = okRengi;

        RectTransform rect = yonOku.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = okBoyutu;

        OkGoster(false);
    }

    private void OkGoster(bool goster)
    {
        if (yonOku != null)
            yonOku.gameObject.SetActive(goster);
    }

    private void OnDisable()
    {
        if (aktifAkis != null)
        {
            StopCoroutine(aktifAkis);
            aktifAkis = null;
        }

        SolElIKyiKapat();
        OkGoster(false);

        if (etkilesimKamerasi != null)
            etkilesimKamerasi.enabled = false;

        if (etkilesimDevamEdiyor)
            KontrolleriAc();

        etkilesimDevamEdiyor = false;
    }

    private void OnDrawGizmosSelected()
    {
        Transform kapi = hareketEdecekKapi != null
            ? hareketEdecekKapi
            : transform;

        Vector3 yerelYon = yerelAcilmaYonu.sqrMagnitude > 0.0001f
            ? yerelAcilmaYonu.normalized
            : Vector3.left;
        Vector3 dunyaYonu = kapi.TransformDirection(yerelYon).normalized;
        Vector3 kapaliKonum = kapi.position;
        Vector3 acikKonum = kapaliKonum + dunyaYonu * acilmaMesafesi;

        Gizmos.color = new Color(1f, 0.7f, 0.1f, 0.9f);
        Gizmos.DrawLine(kapaliKonum, acikKonum);
        Gizmos.DrawWireSphere(acikKonum, 0.14f);

        if (oyuncuHizalamaNoktasi != null)
        {
            Gizmos.color = new Color(0.15f, 0.85f, 1f, 0.85f);
            Gizmos.DrawWireSphere(
                oyuncuHizalamaNoktasi.position,
                etkilesimMesafesi
            );
            Gizmos.DrawRay(
                oyuncuHizalamaNoktasi.position,
                oyuncuHizalamaNoktasi.forward
            );
        }
    }

#if UNITY_EDITOR
    public void EditorKurulumunuAyarla(
        Transform kapi,
        Transform oyuncu,
        Animator animator,
        Transform hizalamaNoktasi,
        Transform elNoktasi,
        Transform kameraHedefi,
        Vector3 hesaplananYerelAcilmaYonu,
        float hesaplananAcilmaMesafesi)
    {
        hareketEdecekKapi = kapi;
        oyuncuKoku = oyuncu;
        oyuncuAnimatoru = animator;
        oyuncuHizalamaNoktasi = hizalamaNoktasi;
        solElTutmaNoktasi = elNoktasi;
        kameraNoktasi = kameraHedefi;
        yerelAcilmaYonu = hesaplananYerelAcilmaYonu.normalized;
        acilmaMesafesi = Mathf.Max(0.5f, hesaplananAcilmaMesafesi);
        kapininKapaliDunyaKonumu = kapi.position;
    }
#endif
}
