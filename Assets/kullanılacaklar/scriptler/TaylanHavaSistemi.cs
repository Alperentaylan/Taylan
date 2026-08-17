using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-9000)]
public sealed class TaylanHavaSistemi : MonoBehaviour
{
    public enum HavaTuru
    {
        Gunesli = 1,
        Yagmurlu = 2,
        Sisli = 3,
        Gece = 4,
        Kapali = 5
    }

    private struct HavaAyari
    {
        public Color sisRengi;
        public float sisYogunlugu;
        public Color ortamRengi;
        public Color gunesRengi;
        public float gunesSiddeti;
        public Vector3 gunesAcisi;
        public Color kameraRengi;
        public float yagmurMiktari;
        public float gokyuzuPozlamasi;
    }

    [Header("Hava Akisi")]
    [SerializeField] private HavaTuru baslangicHavasi = HavaTuru.Gunesli;
    [SerializeField] private bool otomatikDegisim = true;
    [Min(15f)] [SerializeField] private float havaSuresi = 90f;
    [Range(0.1f, 5f)] [SerializeField] private float gecisHizi = 0.75f;

    private HavaTuru aktifHava;
    private HavaAyari hedef;
    private Light gunes;
    private Camera aktifKamera;
    private ParticleSystem yagmur;
    private Material yagmurMateryali;
    private Texture2D yagmurDokusu;
    private float sonrakiHavaZamani;
    private float bildirimBitisZamani;
    private GUIStyle bilgiStili;
    private GUIStyle arkaPlanStili;
    private Texture2D arkaPlanDokusu;
    private Texture2D yagmurCizgiDokusu;
    private float yagmurEkranYogunlugu;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void OtomatikKur()
    {
        if (FindAnyObjectByType<TaylanHavaSistemi>() != null)
            return;

        GameObject sistem = new GameObject("Taylan - Hava Sistemi");
        DontDestroyOnLoad(sistem);
        sistem.AddComponent<TaylanHavaSistemi>();
    }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += SahneYuklendi;
        SahnedeGerekenleriBul();
        YagmuruHazirla();
        HavayiDegistir(baslangicHavasi, true);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= SahneYuklendi;

        if (yagmurMateryali != null)
            Destroy(yagmurMateryali);
        if (yagmurDokusu != null)
            Destroy(yagmurDokusu);
        if (arkaPlanDokusu != null)
            Destroy(arkaPlanDokusu);
        if (yagmurCizgiDokusu != null)
            Destroy(yagmurCizgiDokusu);
    }

    private void SahneYuklendi(Scene sahne, LoadSceneMode mod)
    {
        SahnedeGerekenleriBul();
    }

    private void Update()
    {
        KlavyeKontrolu();

        if (otomatikDegisim && Time.unscaledTime >= sonrakiHavaZamani)
        {
            int siradaki = ((int)aktifHava % 5) + 1;
            HavayiDegistir((HavaTuru)siradaki);
        }

        if (aktifKamera == null || !aktifKamera.isActiveAndEnabled)
            aktifKamera = Camera.main;

        GecisiUygula(Time.deltaTime);
        YagmuruKameradaTut();
    }

    private void KlavyeKontrolu()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) HavayiDegistir(HavaTuru.Gunesli);
        if (Input.GetKeyDown(KeyCode.Alpha2)) HavayiDegistir(HavaTuru.Yagmurlu);
        if (Input.GetKeyDown(KeyCode.Alpha3)) HavayiDegistir(HavaTuru.Sisli);
        if (Input.GetKeyDown(KeyCode.Alpha4)) HavayiDegistir(HavaTuru.Gece);
        if (Input.GetKeyDown(KeyCode.Alpha5)) HavayiDegistir(HavaTuru.Kapali);

        if (Input.GetKeyDown(KeyCode.F6))
        {
            otomatikDegisim = !otomatikDegisim;
            sonrakiHavaZamani = Time.unscaledTime + havaSuresi;
            bildirimBitisZamani = Time.unscaledTime + 4f;
        }
    }

    public void HavayiDegistir(HavaTuru yeniHava, bool aninda = false)
    {
        aktifHava = yeniHava;
        hedef = AyarlariAl(yeniHava);
        sonrakiHavaZamani = Time.unscaledTime + havaSuresi;
        bildirimBitisZamani = Time.unscaledTime + 4f;

        // WebGL'de ilk karelerde particle shader'i gec derlenebiliyor. Hava
        // yagmura alindigi anda hazir bir damla grubu baslatmak, oyuncunun
        // yagmuru hemen gormesini garanti eder.
        if (yeniHava == HavaTuru.Yagmurlu && yagmur != null)
        {
            if (!yagmur.isPlaying)
                yagmur.Play(true);
            yagmur.Emit(320);
        }

        if (aninda)
            GecisiUygula(1000f);
    }

    private void SahnedeGerekenleriBul()
    {
        aktifKamera = Camera.main;

        Light[] isiklar = FindObjectsByType<Light>();
        gunes = null;

        for (int i = 0; i < isiklar.Length; i++)
        {
            if (isiklar[i] != null && isiklar[i].type == LightType.Directional)
            {
                gunes = isiklar[i];
                break;
            }
        }

        if (gunes == null)
        {
            GameObject yeniGunes = new GameObject("Hava Sistemi - Gunes");
            gunes = yeniGunes.AddComponent<Light>();
            gunes.type = LightType.Directional;
            gunes.shadows = LightShadows.Soft;
        }
    }

    private void YagmuruHazirla()
    {
        if (yagmur != null)
            return;

        GameObject yagmurObjesi = new GameObject("Hava Sistemi - Yagmur");
        yagmurObjesi.transform.SetParent(transform, false);
        yagmur = yagmurObjesi.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule ana = yagmur.main;
        ana.loop = true;
        ana.playOnAwake = true;
        ana.simulationSpace = ParticleSystemSimulationSpace.World;
        ana.startLifetime = new ParticleSystem.MinMaxCurve(1.6f, 2.2f);
        ana.startSpeed = 0f;
        ana.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.13f);
        ana.startColor = new Color(0.72f, 0.86f, 1f, 0.9f);
        ana.maxParticles = 6000;

        ParticleSystem.ShapeModule sekil = yagmur.shape;
        sekil.enabled = true;
        sekil.shapeType = ParticleSystemShapeType.Box;
        sekil.scale = new Vector3(45f, 1f, 45f);

        ParticleSystem.EmissionModule salim = yagmur.emission;
        salim.rateOverTime = 0f;

        ParticleSystem.VelocityOverLifetimeModule hiz = yagmur.velocityOverLifetime;
        hiz.enabled = true;
        hiz.space = ParticleSystemSimulationSpace.World;
        hiz.x = new ParticleSystem.MinMaxCurve(-1.5f, -0.5f);
        hiz.y = -26f;

        ParticleSystemRenderer cizici = yagmur.GetComponent<ParticleSystemRenderer>();
        cizici.renderMode = ParticleSystemRenderMode.Stretch;
        cizici.velocityScale = 0.12f;
        cizici.lengthScale = 10f;
        cizici.sortingOrder = 100;
        cizici.minParticleSize = 0.004f;
        cizici.maxParticleSize = 0.08f;

        // Sprites/Default WebGL build'lerinde alfa karisimiyla en kararli
        // calisan secenektir. URP particle shader'i bulunursa yedek olarak kalir.
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Particles/Standard Unlit");

        if (shader != null)
        {
            yagmurMateryali = new Material(shader);
            yagmurMateryali.name = "Hava Sistemi - Yagmur Materyali";
            yagmurDokusu = YagmurDokusuOlustur();

            if (yagmurMateryali.HasProperty("_BaseMap"))
                yagmurMateryali.SetTexture("_BaseMap", yagmurDokusu);
            if (yagmurMateryali.HasProperty("_MainTex"))
                yagmurMateryali.SetTexture("_MainTex", yagmurDokusu);

            cizici.material = yagmurMateryali;
        }

        yagmur.Play();
    }

    private static Texture2D YagmurDokusuOlustur()
    {
        Texture2D doku = new Texture2D(4, 32, TextureFormat.RGBA32, false);
        doku.name = "Hava Sistemi - Yagmur Dokusu";
        doku.wrapMode = TextureWrapMode.Clamp;
        doku.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < doku.height; y++)
        {
            float dikey = Mathf.Sin((y / 31f) * Mathf.PI);
            for (int x = 0; x < doku.width; x++)
            {
                float yatay = 1f - Mathf.Abs(x - 1.5f) / 1.5f;
                float alfa = Mathf.Clamp01(dikey * yatay) * 0.75f;
                doku.SetPixel(x, y, new Color(0.75f, 0.88f, 1f, alfa));
            }
        }

        doku.Apply();
        return doku;
    }

    private void YagmuruKameradaTut()
    {
        if (yagmur == null || aktifKamera == null)
            return;

        Vector3 konum = aktifKamera.transform.position;
        konum.y += 18f;
        yagmur.transform.position = konum;
    }

    private void GecisiUygula(float deltaTime)
    {
        float t = 1f - Mathf.Exp(-Mathf.Max(0.01f, gecisHizi) * deltaTime);

        RenderSettings.fog = hedef.sisYogunlugu > 0.0001f || RenderSettings.fogDensity > 0.0001f;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, hedef.sisRengi, t);
        RenderSettings.fogDensity = Mathf.Lerp(RenderSettings.fogDensity, hedef.sisYogunlugu, t);
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = Color.Lerp(RenderSettings.ambientLight, hedef.ortamRengi, t);
        RenderSettings.ambientIntensity = Mathf.Lerp(RenderSettings.ambientIntensity, 1f, t);

        if (gunes != null)
        {
            gunes.color = Color.Lerp(gunes.color, hedef.gunesRengi, t);
            gunes.intensity = Mathf.Lerp(gunes.intensity, hedef.gunesSiddeti, t);
            gunes.shadows = LightShadows.Soft;
            gunes.shadowStrength = Mathf.Lerp(gunes.shadowStrength, aktifHava == HavaTuru.Kapali ? 0.55f : 0.85f, t);
            gunes.transform.rotation = Quaternion.Slerp(
                gunes.transform.rotation,
                Quaternion.Euler(hedef.gunesAcisi),
                t
            );
        }

        if (aktifKamera != null)
            aktifKamera.backgroundColor = Color.Lerp(aktifKamera.backgroundColor, hedef.kameraRengi, t);

        if (RenderSettings.skybox != null)
        {
            if (RenderSettings.skybox.HasProperty("_Exposure"))
            {
                float mevcut = RenderSettings.skybox.GetFloat("_Exposure");
                RenderSettings.skybox.SetFloat("_Exposure", Mathf.Lerp(mevcut, hedef.gokyuzuPozlamasi, t));
            }

            if (RenderSettings.skybox.HasProperty("_Tint"))
            {
                Color tint = Color.Lerp(hedef.kameraRengi, Color.white, 0.35f);
                RenderSettings.skybox.SetColor("_Tint", tint);
            }
        }

        if (yagmur != null)
        {
            ParticleSystem.EmissionModule salim = yagmur.emission;
            float mevcut = salim.rateOverTime.constant;
            salim.rateOverTime = Mathf.Lerp(mevcut, hedef.yagmurMiktari, t);
        }

        float ekranHedefi = Mathf.Clamp01(hedef.yagmurMiktari / 1450f);
        yagmurEkranYogunlugu = Mathf.Lerp(yagmurEkranYogunlugu, ekranHedefi, t);
    }

    private static HavaAyari AyarlariAl(HavaTuru hava)
    {
        switch (hava)
        {
            case HavaTuru.Yagmurlu:
                return new HavaAyari
                {
                    sisRengi = new Color(0.36f, 0.42f, 0.48f),
                    sisYogunlugu = 0.008f,
                    ortamRengi = new Color(0.34f, 0.39f, 0.46f),
                    gunesRengi = new Color(0.65f, 0.72f, 0.82f),
                    gunesSiddeti = 0.55f,
                    gunesAcisi = new Vector3(42f, -25f, 0f),
                    kameraRengi = new Color(0.26f, 0.32f, 0.4f),
                    yagmurMiktari = 1450f,
                    gokyuzuPozlamasi = 0.55f
                };

            case HavaTuru.Sisli:
                return new HavaAyari
                {
                    sisRengi = new Color(0.62f, 0.66f, 0.68f),
                    sisYogunlugu = 0.026f,
                    ortamRengi = new Color(0.48f, 0.51f, 0.52f),
                    gunesRengi = new Color(0.78f, 0.8f, 0.78f),
                    gunesSiddeti = 0.38f,
                    gunesAcisi = new Vector3(32f, -45f, 0f),
                    kameraRengi = new Color(0.56f, 0.6f, 0.62f),
                    yagmurMiktari = 0f,
                    gokyuzuPozlamasi = 0.65f
                };

            case HavaTuru.Gece:
                return new HavaAyari
                {
                    sisRengi = new Color(0.025f, 0.04f, 0.085f),
                    sisYogunlugu = 0.006f,
                    ortamRengi = new Color(0.045f, 0.07f, 0.14f),
                    gunesRengi = new Color(0.3f, 0.42f, 0.7f),
                    gunesSiddeti = 0.18f,
                    gunesAcisi = new Vector3(18f, 145f, 0f),
                    kameraRengi = new Color(0.015f, 0.025f, 0.06f),
                    yagmurMiktari = 0f,
                    gokyuzuPozlamasi = 0.12f
                };

            case HavaTuru.Kapali:
                return new HavaAyari
                {
                    sisRengi = new Color(0.46f, 0.5f, 0.54f),
                    sisYogunlugu = 0.005f,
                    ortamRengi = new Color(0.42f, 0.45f, 0.49f),
                    gunesRengi = new Color(0.72f, 0.76f, 0.82f),
                    gunesSiddeti = 0.62f,
                    gunesAcisi = new Vector3(48f, -35f, 0f),
                    kameraRengi = new Color(0.36f, 0.41f, 0.47f),
                    yagmurMiktari = 0f,
                    gokyuzuPozlamasi = 0.7f
                };

            default:
                return new HavaAyari
                {
                    sisRengi = new Color(0.62f, 0.78f, 0.92f),
                    sisYogunlugu = 0.001f,
                    ortamRengi = new Color(0.72f, 0.76f, 0.82f),
                    gunesRengi = new Color(1f, 0.93f, 0.78f),
                    gunesSiddeti = 1.25f,
                    gunesAcisi = new Vector3(48f, -35f, 0f),
                    kameraRengi = new Color(0.45f, 0.7f, 0.95f),
                    yagmurMiktari = 0f,
                    gokyuzuPozlamasi = 1.15f
                };
        }
    }

    private void OnGUI()
    {
        YagmurPerdesiniCiz();

        if (Time.unscaledTime > bildirimBitisZamani)
            return;

        GUIStilleriniHazirla();

        float genislik = 360f;
        Rect alan = new Rect(Screen.width - genislik - 18f, 18f, genislik, 58f);
        GUI.Box(alan, GUIContent.none, arkaPlanStili);
        GUI.Label(
            new Rect(alan.x + 14f, alan.y + 7f, alan.width - 28f, alan.height - 14f),
            "Hava: " + HavaAdiniAl(aktifHava) +
            "\n1-5: hava sec  |  F6: otomatik " + (otomatikDegisim ? "acik" : "kapali"),
            bilgiStili
        );
    }

    private void YagmurPerdesiniCiz()
    {
        if (yagmurEkranYogunlugu < 0.015f || Event.current.type != EventType.Repaint)
            return;

        if (yagmurCizgiDokusu == null)
        {
            yagmurCizgiDokusu = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            yagmurCizgiDokusu.name = "Hava Sistemi - Ekran Yagmuru";
            yagmurCizgiDokusu.SetPixel(0, 0, Color.white);
            yagmurCizgiDokusu.Apply();
        }

        Matrix4x4 oncekiMatris = GUI.matrix;
        Color oncekiRenk = GUI.color;
        GUIUtility.RotateAroundPivot(-7f, new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));

        float zaman = Time.unscaledTime * 920f;
        int damlaSayisi = Mathf.RoundToInt(Mathf.Lerp(55f, 185f, yagmurEkranYogunlugu));
        for (int i = 0; i < damlaSayisi; i++)
        {
            float x = Mathf.Repeat(i * 83.71f + (i % 9) * 19.3f, Screen.width + 180f) - 90f;
            float y = Mathf.Repeat(i * 47.37f + zaman * (0.82f + (i % 7) * 0.035f), Screen.height + 180f) - 90f;
            float uzunluk = 22f + (i % 8) * 6f;
            float genislik = 1f + (i % 3) * 0.45f;
            float alfa = (0.2f + (i % 5) * 0.055f) * yagmurEkranYogunlugu;
            GUI.color = new Color(0.7f, 0.86f, 1f, alfa);
            GUI.DrawTexture(new Rect(x, y, genislik, uzunluk), yagmurCizgiDokusu);
        }

        GUI.color = oncekiRenk;
        GUI.matrix = oncekiMatris;
    }

    private void GUIStilleriniHazirla()
    {
        if (bilgiStili != null)
            return;

        bilgiStili = new GUIStyle(GUI.skin.label);
        bilgiStili.fontSize = 16;
        bilgiStili.normal.textColor = Color.white;
        bilgiStili.alignment = TextAnchor.MiddleLeft;

        arkaPlanDokusu = new Texture2D(1, 1);
        arkaPlanDokusu.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.72f));
        arkaPlanDokusu.Apply();

        arkaPlanStili = new GUIStyle(GUI.skin.box);
        arkaPlanStili.normal.background = arkaPlanDokusu;
    }

    private static string HavaAdiniAl(HavaTuru hava)
    {
        switch (hava)
        {
            case HavaTuru.Yagmurlu: return "Yagmurlu";
            case HavaTuru.Sisli: return "Sisli";
            case HavaTuru.Gece: return "Gece";
            case HavaTuru.Kapali: return "Kapali / Bulutlu";
            default: return "Gunesli";
        }
    }
}
