using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-10000)]
public sealed class TaylanGoruntuKalitesi : MonoBehaviour
{
    private const float YenilemeAraligi = 2f;
    private float sonrakiKameraYenilemesi;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void OtomatikKur()
    {
        if (FindAnyObjectByType<TaylanGoruntuKalitesi>() != null)
            return;

        GameObject sistem = new GameObject("Taylan - Goruntu Kalitesi");
        DontDestroyOnLoad(sistem);
        sistem.AddComponent<TaylanGoruntuKalitesi>();
    }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        KaliteyiUygula();
        SceneManager.sceneLoaded += SahneYuklendi;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= SahneYuklendi;
    }

    private void Update()
    {
        if (Time.unscaledTime < sonrakiKameraYenilemesi)
            return;

        sonrakiKameraYenilemesi = Time.unscaledTime + YenilemeAraligi;
        KameralariUygula();
    }

    private void SahneYuklendi(Scene sahne, LoadSceneMode mod)
    {
        KaliteyiUygula();
    }

    private static void KaliteyiUygula()
    {
        int pcKalite = Array.FindIndex(
            QualitySettings.names,
            ad => string.Equals(ad, "PC", StringComparison.OrdinalIgnoreCase)
        );

        if (pcKalite < 0 && QualitySettings.names.Length > 0)
            pcKalite = QualitySettings.names.Length - 1;

        if (pcKalite >= 0 && QualitySettings.GetQualityLevel() != pcKalite)
            QualitySettings.SetQualityLevel(pcKalite, true);

        QualitySettings.pixelLightCount = 4;
        QualitySettings.shadows = ShadowQuality.All;
        QualitySettings.shadowResolution = ShadowResolution.VeryHigh;
        QualitySettings.shadowProjection = ShadowProjection.StableFit;
        QualitySettings.shadowDistance = 100f;
        QualitySettings.shadowCascades = 4;
        QualitySettings.shadowNearPlaneOffset = 2f;
        QualitySettings.skinWeights = SkinWeights.Unlimited;
        QualitySettings.globalTextureMipmapLimit = 0;
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
        QualitySettings.antiAliasing = 4;
        QualitySettings.softParticles = true;
        QualitySettings.softVegetation = true;
        QualitySettings.realtimeReflectionProbes = true;
        QualitySettings.lodBias = 2f;
        QualitySettings.maximumLODLevel = 0;
        QualitySettings.vSyncCount = 0;

        Application.targetFrameRate = 60;
        ScalableBufferManager.ResizeBuffers(1f, 1f);

        RenderPipelineAsset aktifBoru = GraphicsSettings.currentRenderPipeline;
        if (aktifBoru != null)
        {
            OzellikAyarla(aktifBoru, "renderScale", 1f);
            OzellikAyarla(aktifBoru, "msaaSampleCount", 4);
            OzellikAyarla(aktifBoru, "shadowDistance", 100f);
            OzellikAyarla(aktifBoru, "mainLightShadowmapResolution", 4096);
            OzellikAyarla(aktifBoru, "additionalLightsShadowmapResolution", 2048);
            OzellikAyarla(aktifBoru, "supportsHDR", true);
            OzellikAyarla(aktifBoru, "supportsCameraDepthTexture", true);
            OzellikAyarla(aktifBoru, "supportsCameraOpaqueTexture", true);
        }

        KameralariUygula();
    }

    private static void KameralariUygula()
    {
        Camera[] kameralar = Camera.allCameras;
        for (int i = 0; i < kameralar.Length; i++)
        {
            Camera kamera = kameralar[i];
            if (kamera == null)
                continue;

            kamera.allowHDR = true;
            kamera.allowMSAA = true;
        }
    }

    private static void OzellikAyarla(object hedef, string ad, object deger)
    {
        try
        {
            PropertyInfo ozellik = hedef.GetType().GetProperty(
                ad,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );

            if (ozellik == null || !ozellik.CanWrite)
                return;

            object donusturulmusDeger = deger;
            Type hedefTur = ozellik.PropertyType;

            if (hedefTur.IsEnum)
                donusturulmusDeger = Enum.ToObject(hedefTur, deger);
            else if (deger != null && !hedefTur.IsInstanceOfType(deger))
                donusturulmusDeger = Convert.ChangeType(deger, hedefTur);

            ozellik.SetValue(hedef, donusturulmusDeger);
        }
        catch (Exception)
        {
            // URP surumleri arasinda adi degisen ayarlar sessizce atlanir.
        }
    }
}
