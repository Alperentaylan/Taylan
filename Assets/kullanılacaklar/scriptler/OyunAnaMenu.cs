using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// Bos bir Menu sahnesindeki GameObject'e eklenir.
/// Canvas, video/gorsel arka plan, karartma, Oyna butonu ve gecisi otomatik kurar.
/// </summary>
[DisallowMultipleComponent]
public sealed class OyunAnaMenu : MonoBehaviour
{
    [Header("OYUN")]
    [SerializeField] private string oyunBasligi = "KARİYER YOLCULUĞU";
    [SerializeField] private string altBaslik = "İNTERAKTİF PORTFÖY DENEYİMİ";
    [TextArea(2, 4)]
    [SerializeField]
    private string aciklama =
        "Şehri keşfet, geliştirilen sistemleri deneyimle ve projelerin arkasındaki yolculuğa katıl.";
    [Tooltip("Oyna butonunun acacagi sahnenin Build Settings'teki adi.")]
    [SerializeField] private string oyunSahnesi = "SampleScene";

    [Header("VIDEO ARKA PLAN")]
    [Tooltip("Project'e attigin MP4/WebM dosyasini buraya surukle.")]
    [SerializeField] private VideoClip arkaPlanVideosu;
    [Tooltip("Video kullanmak istemezsen StreamingAssets icindeki dosya adi. Ornek: menu.mp4")]
    [SerializeField] private string streamingAssetsVideoDosyasi = "";
    [Range(0f, 1f)]
    [SerializeField] private float videoSesSeviyesi = 0.08f;
    [SerializeField] private bool videoDongu = true;

    [Header("VIDEO YOKSA DONEN GORSELLER")]
    [Tooltip("Oyun ici ekran goruntulerini buraya sirayla koyabilirsin.")]
    [SerializeField] private Texture[] arkaPlanGorselleri;
    [Min(1f)][SerializeField] private float gorselBeklemeSuresi = 6f;
    [Min(0.1f)][SerializeField] private float gorselGecisSuresi = 1.2f;

    [Header("GORUNUM")]
    [Range(0f, 0.95f)]
    [Tooltip("Arka plani okunabilir yapmak icin siyah katman siddeti.")]
    [SerializeField] private float arkaPlanKarartma = 0.56f;
    [SerializeField] private Color vurguRengi = new Color(0.88f, 0.16f, 0.10f, 1f);
    [SerializeField] private Color butonRengi = new Color(0.92f, 0.92f, 0.92f, 1f);
    [SerializeField] private Color butonYaziRengi = new Color(0.06f, 0.06f, 0.07f, 1f);

    [Header("ALT BILGI")]
    [SerializeField] private string surumYazisi = "PORTFOLIO BUILD  •  2026";
    [SerializeField] private string ustMarkaYazisi = "INTERACTIVE CV  /  UNITY EXPERIENCE";

    private Canvas anaCanvas;
    private RawImage arkaPlanImage;
    private AspectRatioFitter arkaPlanAspect;
    private VideoPlayer videoPlayer;
    private AudioSource videoAudioSource;
    private RenderTexture videoRenderTexture;
    private CanvasGroup sahneGecisPerdesi;
    private Button oynaButonu;
    private Font menuFontu;
    private Coroutine gorselDongusu;
    private Texture2D beyazTexture;
    private Camera menuKamerasi;

    private void Awake()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        menuFontu = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        MenuKamerasiniKur();
        ArayuzuKur();
        EventSystemKur();
        ArkaPlaniBaslat();
    }

    private void ArayuzuKur()
    {
        GameObject canvasObjesi = new GameObject("AnaMenu_Canvas");
        canvasObjesi.transform.SetParent(transform, false);

        anaCanvas = canvasObjesi.AddComponent<Canvas>();
        anaCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        anaCanvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasObjesi.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasObjesi.AddComponent<GraphicRaycaster>();

        GameObject arkaPlan = UIObjesi("ArkaPlan", canvasObjesi.transform);
        RectTransform arkaRect = arkaPlan.GetComponent<RectTransform>();
        TamEkranYap(arkaRect);

        arkaPlanImage = arkaPlan.AddComponent<RawImage>();
        arkaPlanImage.color = Color.white;
        arkaPlanImage.raycastTarget = false;

        arkaPlanAspect = arkaPlan.AddComponent<AspectRatioFitter>();
        arkaPlanAspect.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        arkaPlanAspect.aspectRatio = 16f / 9f;

        DekoratifArkaPlaniKur(canvasObjesi.transform);

        // Video/gorselin uzerindeki sinematik karartma.
        Image karartma = ResimOlustur(
            "Sinematik_Karartma",
            canvasObjesi.transform,
            new Color(0f, 0f, 0f, arkaPlanKarartma));
        TamEkranYap(karartma.rectTransform);
        karartma.raycastTarget = false;

        // Sol tarafta baslik okunurlugunu artiran ek panel.
        Image solGolge = ResimOlustur(
            "Sol_Golge",
            canvasObjesi.transform,
            new Color(0f, 0f, 0f, 0.28f));
        RectTransform solGolgeRect = solGolge.rectTransform;
        solGolgeRect.anchorMin = new Vector2(0f, 0f);
        solGolgeRect.anchorMax = new Vector2(0.53f, 1f);
        solGolgeRect.offsetMin = Vector2.zero;
        solGolgeRect.offsetMax = Vector2.zero;
        solGolge.raycastTarget = false;

        UstMarkayiKur(canvasObjesi.transform);

        // Ust/alt sinema bantlari.
        BantOlustur(canvasObjesi.transform, true);
        BantOlustur(canvasObjesi.transform, false);

        GameObject icerik = UIObjesi("Menu_Icerik", canvasObjesi.transform);
        RectTransform icerikRect = icerik.GetComponent<RectTransform>();
        icerikRect.anchorMin = new Vector2(0f, 0f);
        icerikRect.anchorMax = new Vector2(0f, 0f);
        icerikRect.pivot = new Vector2(0f, 0f);
        icerikRect.anchoredPosition = new Vector2(118f, 128f);
        icerikRect.sizeDelta = new Vector2(800f, 470f);

        Image vurguCizgisi = ResimOlustur("Vurgu_Cizgisi", icerik.transform, vurguRengi);
        RectTransform vurguRect = vurguCizgisi.rectTransform;
        vurguRect.anchorMin = new Vector2(0f, 1f);
        vurguRect.anchorMax = new Vector2(0f, 1f);
        vurguRect.pivot = new Vector2(0f, 1f);
        vurguRect.anchoredPosition = new Vector2(2f, -3f);
        vurguRect.sizeDelta = new Vector2(82f, 7f);

        Text altBaslikText = YaziOlustur(
            "Alt_Baslik",
            icerik.transform,
            altBaslik,
            20,
            FontStyle.Bold,
            new Color(1f, 1f, 1f, 0.72f));
        RectTransform altRect = altBaslikText.rectTransform;
        altRect.anchorMin = new Vector2(0f, 1f);
        altRect.anchorMax = new Vector2(1f, 1f);
        altRect.pivot = new Vector2(0f, 1f);
        altRect.anchoredPosition = new Vector2(0f, -35f);
        altRect.sizeDelta = new Vector2(780f, 40f);
        altBaslikText.characterSpacingUyumlu(2);

        Text baslikText = YaziOlustur(
            "Oyun_Basligi",
            icerik.transform,
            oyunBasligi,
            68,
            FontStyle.Bold,
            Color.white);
        RectTransform baslikRect = baslikText.rectTransform;
        baslikRect.anchorMin = new Vector2(0f, 1f);
        baslikRect.anchorMax = new Vector2(1f, 1f);
        baslikRect.pivot = new Vector2(0f, 1f);
        baslikRect.anchoredPosition = new Vector2(-3f, -74f);
        baslikRect.sizeDelta = new Vector2(790f, 180f);
        baslikText.resizeTextForBestFit = true;
        baslikText.resizeTextMinSize = 42;
        baslikText.resizeTextMaxSize = 68;

        Text aciklamaText = YaziOlustur(
            "Aciklama",
            icerik.transform,
            aciklama,
            19,
            FontStyle.Normal,
            new Color(1f, 1f, 1f, 0.68f));
        RectTransform aciklamaRect = aciklamaText.rectTransform;
        aciklamaRect.anchorMin = new Vector2(0f, 1f);
        aciklamaRect.anchorMax = new Vector2(1f, 1f);
        aciklamaRect.pivot = new Vector2(0f, 1f);
        aciklamaRect.anchoredPosition = new Vector2(1f, -244f);
        aciklamaRect.sizeDelta = new Vector2(650f, 72f);
        aciklamaText.lineSpacing = 1.2f;

        oynaButonu = ButonOlustur(icerik.transform);

        SagBilgiKartiniKur(canvasObjesi.transform);

        Text ipucu = YaziOlustur(
            "Kontrol_Ipucu",
            icerik.transform,
            "OYUNU BAŞLATMAK İÇİN TIKLA",
            14,
            FontStyle.Normal,
            new Color(1f, 1f, 1f, 0.48f));
        RectTransform ipucuRect = ipucu.rectTransform;
        ipucuRect.anchorMin = new Vector2(0f, 0f);
        ipucuRect.anchorMax = new Vector2(0f, 0f);
        ipucuRect.pivot = new Vector2(0f, 0f);
        ipucuRect.anchoredPosition = new Vector2(3f, 13f);
        ipucuRect.sizeDelta = new Vector2(500f, 28f);

        Text surum = YaziOlustur(
            "Surum",
            canvasObjesi.transform,
            surumYazisi,
            13,
            FontStyle.Normal,
            new Color(1f, 1f, 1f, 0.42f));
        RectTransform surumRect = surum.rectTransform;
        surumRect.anchorMin = new Vector2(1f, 0f);
        surumRect.anchorMax = new Vector2(1f, 0f);
        surumRect.pivot = new Vector2(1f, 0f);
        surumRect.anchoredPosition = new Vector2(-52f, 35f);
        surumRect.sizeDelta = new Vector2(500f, 30f);
        surum.alignment = TextAnchor.MiddleRight;

        Image gecisPerdesi = ResimOlustur(
            "Sahne_Gecis_Perdesi",
            canvasObjesi.transform,
            Color.black);
        TamEkranYap(gecisPerdesi.rectTransform);
        gecisPerdesi.transform.SetAsLastSibling();
        sahneGecisPerdesi = gecisPerdesi.gameObject.AddComponent<CanvasGroup>();
        sahneGecisPerdesi.alpha = 0f;
        sahneGecisPerdesi.blocksRaycasts = false;
    }

    private Button ButonOlustur(Transform parent)
    {
        GameObject butonObjesi = UIObjesi("Oyna_Butonu", parent);
        RectTransform rect = butonObjesi.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0f, 0f);
        rect.anchoredPosition = new Vector2(0f, 48f);
        rect.sizeDelta = new Vector2(338f, 82f);

        Image zemin = butonObjesi.AddComponent<Image>();
        zemin.sprite = DuzRenkSprite();
        zemin.color = butonRengi;

        Button button = butonObjesi.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(OyunuBaslat);

        Text yazi = YaziOlustur(
            "Yazi",
            butonObjesi.transform,
            "OYNA     →",
            25,
            FontStyle.Bold,
            butonYaziRengi);
        TamEkranYap(yazi.rectTransform);
        yazi.alignment = TextAnchor.MiddleCenter;

        MenuButonEfekti efekt = butonObjesi.AddComponent<MenuButonEfekti>();
        efekt.Ayarla(zemin, yazi, butonRengi, vurguRengi, butonYaziRengi);

        return button;
    }

    private void MenuKamerasiniKur()
    {
        Camera mevcut = FindFirstObjectByType<Camera>();
        if (mevcut != null && mevcut.enabled && mevcut.gameObject.activeInHierarchy)
        {
            menuKamerasi = mevcut;
            return;
        }

        GameObject kameraObjesi = new GameObject("AnaMenu_Kamera");
        kameraObjesi.transform.SetParent(transform, false);
        menuKamerasi = kameraObjesi.AddComponent<Camera>();
        menuKamerasi.clearFlags = CameraClearFlags.SolidColor;
        menuKamerasi.backgroundColor = new Color(0.012f, 0.016f, 0.027f, 1f);
        menuKamerasi.cullingMask = 0;
        menuKamerasi.depth = -100f;
        menuKamerasi.orthographic = true;
        kameraObjesi.tag = "MainCamera";
    }

    private void DekoratifArkaPlaniKur(Transform parent)
    {
        // Video eklenmemisken de bos siyah ekran yerine premium koyu zemin.
        bool medyaVar =
            arkaPlanVideosu != null ||
            !string.IsNullOrWhiteSpace(streamingAssetsVideoDosyasi) ||
            (arkaPlanGorselleri != null && arkaPlanGorselleri.Length > 0);

        Image taban = ResimOlustur(
            "Tasarim_Taban",
            parent,
            new Color(0.018f, 0.024f, 0.043f, medyaVar ? 0.16f : 0.94f));
        TamEkranYap(taban.rectTransform);
        taban.raycastTarget = false;

        // Ince teknik grid.
        for (int i = 1; i < 12; i++)
        {
            Image dikey = ResimOlustur(
                "Grid_Dikey_" + i,
                parent,
                new Color(1f, 1f, 1f, 0.026f));
            RectTransform rect = dikey.rectTransform;
            float x = i / 12f;
            rect.anchorMin = new Vector2(x, 0f);
            rect.anchorMax = new Vector2(x, 1f);
            rect.sizeDelta = new Vector2(1f, 0f);
            rect.anchoredPosition = Vector2.zero;
            dikey.raycastTarget = false;
        }

        for (int i = 1; i < 7; i++)
        {
            Image yatay = ResimOlustur(
                "Grid_Yatay_" + i,
                parent,
                new Color(1f, 1f, 1f, 0.022f));
            RectTransform rect = yatay.rectTransform;
            float y = i / 7f;
            rect.anchorMin = new Vector2(0f, y);
            rect.anchorMax = new Vector2(1f, y);
            rect.sizeDelta = new Vector2(0f, 1f);
            rect.anchoredPosition = Vector2.zero;
            yatay.raycastTarget = false;
        }

        DekorPanelOlustur(
            parent,
            "Hareketli_Kirmizi",
            new Vector2(1540f, 820f),
            new Vector2(780f, 240f),
            -17f,
            new Color(vurguRengi.r, vurguRengi.g, vurguRengi.b, 0.11f),
            new Vector2(34f, 18f),
            0.34f);

        DekorPanelOlustur(
            parent,
            "Hareketli_Mavi",
            new Vector2(1370f, 290f),
            new Vector2(900f, 330f),
            -17f,
            new Color(0.12f, 0.32f, 0.55f, 0.08f),
            new Vector2(-26f, 24f),
            0.25f);

        DekorPanelOlustur(
            parent,
            "Hareketli_Cizgi",
            new Vector2(1180f, 620f),
            new Vector2(1100f, 3f),
            -17f,
            new Color(1f, 1f, 1f, 0.12f),
            new Vector2(42f, 10f),
            0.42f);
    }

    private void DekorPanelOlustur(
        Transform parent,
        string ad,
        Vector2 konum,
        Vector2 boyut,
        float aci,
        Color renk,
        Vector2 hareket,
        float hiz)
    {
        Image panel = ResimOlustur(ad, parent, renk);
        RectTransform rect = panel.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = konum;
        rect.sizeDelta = boyut;
        rect.localRotation = Quaternion.Euler(0f, 0f, aci);
        panel.raycastTarget = false;

        MenuArkaPlanHareketi animator = panel.gameObject.AddComponent<MenuArkaPlanHareketi>();
        animator.Ayarla(konum, hareket, hiz);
    }

    private void UstMarkayiKur(Transform parent)
    {
        Text marka = YaziOlustur(
            "Ust_Marka",
            parent,
            ustMarkaYazisi,
            14,
            FontStyle.Bold,
            new Color(1f, 1f, 1f, 0.58f));
        RectTransform rect = marka.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(56f, -39f);
        rect.sizeDelta = new Vector2(620f, 30f);

        Image nokta = ResimOlustur("Canli_Nokta", parent, vurguRengi);
        RectTransform noktaRect = nokta.rectTransform;
        noktaRect.anchorMin = new Vector2(0f, 1f);
        noktaRect.anchorMax = new Vector2(0f, 1f);
        noktaRect.pivot = new Vector2(0f, 1f);
        noktaRect.anchoredPosition = new Vector2(35f, -45f);
        noktaRect.sizeDelta = new Vector2(8f, 8f);
    }

    private void SagBilgiKartiniKur(Transform parent)
    {
        GameObject kart = UIObjesi("Deneyim_Karti", parent);
        RectTransform kartRect = kart.GetComponent<RectTransform>();
        kartRect.anchorMin = new Vector2(1f, 0.5f);
        kartRect.anchorMax = new Vector2(1f, 0.5f);
        kartRect.pivot = new Vector2(1f, 0.5f);
        kartRect.anchoredPosition = new Vector2(-105f, 15f);
        kartRect.sizeDelta = new Vector2(410f, 390f);

        Image kartZemin = kart.AddComponent<Image>();
        kartZemin.sprite = DuzRenkSprite();
        kartZemin.color = new Color(0.015f, 0.018f, 0.028f, 0.56f);
        kartZemin.raycastTarget = false;

        Image ustCizgi = ResimOlustur("Kart_Vurgu", kart.transform, vurguRengi);
        RectTransform ustRect = ustCizgi.rectTransform;
        ustRect.anchorMin = new Vector2(0f, 1f);
        ustRect.anchorMax = new Vector2(1f, 1f);
        ustRect.pivot = new Vector2(0.5f, 1f);
        ustRect.sizeDelta = new Vector2(0f, 3f);
        ustRect.anchoredPosition = Vector2.zero;

        Text baslik = YaziOlustur(
            "Kart_Baslik",
            kart.transform,
            "DENEYİM İÇERİĞİ",
            15,
            FontStyle.Bold,
            new Color(1f, 1f, 1f, 0.54f));
        RectTransform baslikRect = baslik.rectTransform;
        baslikRect.anchorMin = new Vector2(0f, 1f);
        baslikRect.anchorMax = new Vector2(1f, 1f);
        baslikRect.pivot = new Vector2(0f, 1f);
        baslikRect.anchoredPosition = new Vector2(30f, -27f);
        baslikRect.sizeDelta = new Vector2(-60f, 28f);

        string[] numaralar = { "01", "02", "03", "04" };
        string[] satirlar =
        {
            "AÇIK DÜNYAYI KEŞFET",
            "ARAÇ SİSTEMLERİNİ DENE",
            "KARAKTERLE ETKİLEŞİME GEÇ",
            "GELİŞTİRME YOLCULUĞUNU GÖR"
        };

        for (int i = 0; i < satirlar.Length; i++)
        {
            float y = -84f - i * 67f;

            Text no = YaziOlustur(
                "Kart_No_" + i,
                kart.transform,
                numaralar[i],
                14,
                FontStyle.Bold,
                vurguRengi);
            RectTransform noRect = no.rectTransform;
            noRect.anchorMin = new Vector2(0f, 1f);
            noRect.anchorMax = new Vector2(0f, 1f);
            noRect.pivot = new Vector2(0f, 1f);
            noRect.anchoredPosition = new Vector2(30f, y);
            noRect.sizeDelta = new Vector2(42f, 30f);

            Text satir = YaziOlustur(
                "Kart_Satir_" + i,
                kart.transform,
                satirlar[i],
                16,
                FontStyle.Bold,
                new Color(1f, 1f, 1f, 0.86f));
            RectTransform satirRect = satir.rectTransform;
            satirRect.anchorMin = new Vector2(0f, 1f);
            satirRect.anchorMax = new Vector2(1f, 1f);
            satirRect.pivot = new Vector2(0f, 1f);
            satirRect.anchoredPosition = new Vector2(78f, y);
            satirRect.sizeDelta = new Vector2(-105f, 32f);

            Image ayirici = ResimOlustur(
                "Kart_Ayirici_" + i,
                kart.transform,
                new Color(1f, 1f, 1f, 0.075f));
            RectTransform ayiriciRect = ayirici.rectTransform;
            ayiriciRect.anchorMin = new Vector2(0f, 1f);
            ayiriciRect.anchorMax = new Vector2(1f, 1f);
            ayiriciRect.pivot = new Vector2(0.5f, 1f);
            ayiriciRect.anchoredPosition = new Vector2(0f, y - 39f);
            ayiriciRect.sizeDelta = new Vector2(-60f, 1f);
        }
    }

    private void ArkaPlaniBaslat()
    {
        bool videoVar =
            arkaPlanVideosu != null ||
            !string.IsNullOrWhiteSpace(streamingAssetsVideoDosyasi);

        if (videoVar)
        {
            VideoBaslat();
            return;
        }

        if (arkaPlanGorselleri != null && arkaPlanGorselleri.Length > 0)
        {
            arkaPlanImage.texture = arkaPlanGorselleri[0];
            GorselOraniniAyarla(arkaPlanGorselleri[0]);
            gorselDongusu = StartCoroutine(GorselleriDondur());
            return;
        }

        arkaPlanImage.texture = DuzRenkTexture();
        arkaPlanImage.color = new Color(0.055f, 0.06f, 0.075f, 1f);
    }

    private void VideoBaslat()
    {
        videoRenderTexture = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32);
        videoRenderTexture.name = "AnaMenu_Video_RenderTexture";
        videoRenderTexture.Create();
        arkaPlanImage.texture = videoRenderTexture;

        videoPlayer = gameObject.AddComponent<VideoPlayer>();
        videoPlayer.playOnAwake = false;
        videoPlayer.waitForFirstFrame = true;
        videoPlayer.skipOnDrop = true;
        videoPlayer.isLooping = videoDongu;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = videoRenderTexture;

        if (arkaPlanVideosu != null)
        {
            videoPlayer.source = VideoSource.VideoClip;
            videoPlayer.clip = arkaPlanVideosu;
        }
        else
        {
            videoPlayer.source = VideoSource.Url;
            videoPlayer.url = System.IO.Path.Combine(
                Application.streamingAssetsPath,
                streamingAssetsVideoDosyasi);
        }

        videoAudioSource = gameObject.AddComponent<AudioSource>();
        videoAudioSource.playOnAwake = false;
        videoAudioSource.loop = videoDongu;
        videoAudioSource.volume = videoSesSeviyesi;

        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        videoPlayer.EnableAudioTrack(0, true);
        videoPlayer.SetTargetAudioSource(0, videoAudioSource);

        videoPlayer.prepareCompleted += VideoHazir;
        videoPlayer.errorReceived += VideoHatasi;
        videoPlayer.Prepare();
    }

    private void VideoHazir(VideoPlayer player)
    {
        if (player.width > 0 && player.height > 0)
            arkaPlanAspect.aspectRatio = (float)player.width / player.height;

        player.Play();
    }

    private void VideoHatasi(VideoPlayer player, string hata)
    {
        Debug.LogWarning("Ana menu videosu acilamadi: " + hata, this);
    }

    private IEnumerator GorselleriDondur()
    {
        int index = 0;
        Color normalRenk = Color.white;

        while (arkaPlanGorselleri != null && arkaPlanGorselleri.Length > 0)
        {
            yield return new WaitForSecondsRealtime(gorselBeklemeSuresi);

            float sure = 0f;
            while (sure < gorselGecisSuresi)
            {
                sure += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(sure / gorselGecisSuresi);
                arkaPlanImage.color = new Color(1f, 1f, 1f, 1f - t);
                yield return null;
            }

            index = (index + 1) % arkaPlanGorselleri.Length;
            arkaPlanImage.texture = arkaPlanGorselleri[index];
            GorselOraniniAyarla(arkaPlanGorselleri[index]);

            sure = 0f;
            while (sure < gorselGecisSuresi)
            {
                sure += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(sure / gorselGecisSuresi);
                arkaPlanImage.color = new Color(1f, 1f, 1f, t);
                yield return null;
            }

            arkaPlanImage.color = normalRenk;
        }
    }

    public void OyunuBaslat()
    {
        if (oynaButonu != null)
            oynaButonu.interactable = false;

        StartCoroutine(OyunaGec());
    }

    private IEnumerator OyunaGec()
    {
        sahneGecisPerdesi.blocksRaycasts = true;

        float sure = 0f;
        const float kararmaSuresi = 0.65f;
        while (sure < kararmaSuresi)
        {
            sure += Time.unscaledDeltaTime;
            sahneGecisPerdesi.alpha = Mathf.Clamp01(sure / kararmaSuresi);
            yield return null;
        }

        if (!Application.CanStreamedLevelBeLoaded(oyunSahnesi))
        {
            Debug.LogError(
                "Ana menu: '" + oyunSahnesi +
                "' sahnesi bulunamadi. Sahneyi File > Build Profiles > Scene List'e ekle.",
                this);
            sahneGecisPerdesi.alpha = 0f;
            sahneGecisPerdesi.blocksRaycasts = false;
            if (oynaButonu != null) oynaButonu.interactable = true;
            yield break;
        }

        AsyncOperation yukleme = SceneManager.LoadSceneAsync(oyunSahnesi);
        while (yukleme != null && !yukleme.isDone)
            yield return null;
    }

    private void EventSystemKur()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
            return;

        GameObject eventObjesi = new GameObject("EventSystem");
        eventObjesi.transform.SetParent(transform, false);
        eventObjesi.AddComponent<EventSystem>();
        eventObjesi.AddComponent<StandaloneInputModule>();
    }

    private void BantOlustur(Transform parent, bool ust)
    {
        Image bant = ResimOlustur(
            ust ? "Ust_Sinema_Bandi" : "Alt_Sinema_Bandi",
            parent,
            new Color(0f, 0f, 0f, 0.92f));
        RectTransform rect = bant.rectTransform;
        rect.anchorMin = ust ? new Vector2(0f, 1f) : new Vector2(0f, 0f);
        rect.anchorMax = ust ? new Vector2(1f, 1f) : new Vector2(1f, 0f);
        rect.pivot = ust ? new Vector2(0.5f, 1f) : new Vector2(0.5f, 0f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(0f, 24f);
        bant.raycastTarget = false;
    }

    private Text YaziOlustur(
        string ad,
        Transform parent,
        string icerik,
        int punto,
        FontStyle stil,
        Color renk)
    {
        GameObject obje = UIObjesi(ad, parent);
        Text text = obje.AddComponent<Text>();
        text.text = icerik;
        text.font = menuFontu;
        text.fontSize = punto;
        text.fontStyle = stil;
        text.color = renk;
        text.alignment = TextAnchor.MiddleLeft;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }

    private Image ResimOlustur(string ad, Transform parent, Color renk)
    {
        GameObject obje = UIObjesi(ad, parent);
        Image image = obje.AddComponent<Image>();
        image.sprite = DuzRenkSprite();
        image.color = renk;
        return image;
    }

    private GameObject UIObjesi(string ad, Transform parent)
    {
        GameObject obje = new GameObject(ad, typeof(RectTransform));
        obje.transform.SetParent(parent, false);
        return obje;
    }

    private static void TamEkranYap(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private Sprite DuzRenkSprite()
    {
        if (beyazTexture == null)
        {
            beyazTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            beyazTexture.name = "AnaMenu_DuzRenk";
            beyazTexture.SetPixel(0, 0, Color.white);
            beyazTexture.Apply();
        }
        return Sprite.Create(beyazTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
    }

    private Texture DuzRenkTexture()
    {
        DuzRenkSprite();
        return beyazTexture;
    }

    private void GorselOraniniAyarla(Texture texture)
    {
        if (texture != null && texture.height > 0)
            arkaPlanAspect.aspectRatio = (float)texture.width / texture.height;
    }

    private void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= VideoHazir;
            videoPlayer.errorReceived -= VideoHatasi;
        }

        if (videoRenderTexture != null)
        {
            videoRenderTexture.Release();
            Destroy(videoRenderTexture);
        }

        if (beyazTexture != null)
            Destroy(beyazTexture);
    }
}

/// <summary>Oyna butonunun mouse hover/basma animasyonu.</summary>
internal sealed class MenuButonEfekti : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    private Image zemin;
    private Text yazi;
    private Color normalRenk;
    private Color hoverRenk;
    private Color yaziRengi;
    private Vector3 hedefScale = Vector3.one;

    public void Ayarla(Image image, Text text, Color normal, Color hover, Color textColor)
    {
        zemin = image;
        yazi = text;
        normalRenk = normal;
        hoverRenk = hover;
        yaziRengi = textColor;
    }

    private void Update()
    {
        transform.localScale = Vector3.Lerp(
            transform.localScale,
            hedefScale,
            1f - Mathf.Exp(-14f * Time.unscaledDeltaTime));
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hedefScale = Vector3.one * 1.035f;
        if (zemin != null) zemin.color = hoverRenk;
        if (yazi != null) yazi.color = Color.white;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hedefScale = Vector3.one;
        if (zemin != null) zemin.color = normalRenk;
        if (yazi != null) yazi.color = yaziRengi;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        hedefScale = Vector3.one * 0.98f;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        hedefScale = Vector3.one * 1.035f;
    }
}

// Legacy UI Text'te harf araligi alani yoktur. Bu yardimci metod
// cagrinin niyetini korur ve farkli Unity surumlerinde derleme bozmaz.
internal static class MenuTextUyumluluk
{
    public static void characterSpacingUyumlu(this Text text, int deger) { }
}

/// <summary>Menu arka planindaki dekoratif panellere yavas sinematik hareket verir.</summary>
internal sealed class MenuArkaPlanHareketi : MonoBehaviour
{
    private RectTransform rect;
    private Vector2 baslangic;
    private Vector2 hareket;
    private float hiz;
    private float faz;

    public void Ayarla(Vector2 ilkKonum, Vector2 hareketMiktari, float hareketHizi)
    {
        rect = transform as RectTransform;
        baslangic = ilkKonum;
        hareket = hareketMiktari;
        hiz = hareketHizi;
        faz = Random.Range(0f, 6.28f);
    }

    private void Update()
    {
        if (rect == null)
            rect = transform as RectTransform;

        if (rect == null)
            return;

        float dalga = Mathf.Sin(Time.unscaledTime * hiz + faz);
        float ikinciDalga = Mathf.Cos(Time.unscaledTime * hiz * 0.73f + faz);
        rect.anchoredPosition = baslangic +
            new Vector2(hareket.x * dalga, hareket.y * ikinciDalga);
    }
}
