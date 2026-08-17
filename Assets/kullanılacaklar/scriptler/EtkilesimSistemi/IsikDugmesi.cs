using UnityEngine;

public class IsikDugmesi : Etkilesilebilir
{
    [Header("Düğmenin Kontrol Ettiği Işıklar")]
    public Light[] kontrolEdilenIsiklar;

    [Header("İsteğe Bağlı Düğme Animasyonu")]
    public Animator dugmeAnimatoru;
    public string isikAcmaTriggeri = "IsikAc";
    public string isikKapatmaTriggeri = "IsikKapat";

    [Header("Bakınca Parlama")]
    public Renderer[] vurgulanacakRendererlar;
    public Color vurguRengi = new Color(1f, 0.65f, 0.1f, 1f);
    [Range(0.1f, 5f)]
    public float materyalParlamaGucu = 2f;
    [Range(0f, 5f)]
    public float cevreIsigiGucu = 1.4f;
    [Range(0.1f, 3f)]
    public float cevreIsigiMenzili = 0.7f;
    public Vector3 cevreIsigiYerelKonumu =
        new Vector3(0f, 0f, -0.08f);

    private bool isiklarAcik;
    private Light bakisVurguIsigi;
    private Material[][] rendererMateryalleri;
    private Color[][] normalEmissionRenkleri;
    private bool[][] normalEmissionDurumlari;

    void Start()
    {
        IsiklarinMevcutDurumunuBul();
        VurguMateryalleriniHazirla();
        CevreVurguIsiginiHazirla();
        VurguyuAyarla(false);
    }

    public override string EtkilesimMesajiniAl()
    {
        return isiklarAcik
            ? "Işıkları söndürmek için {E} tuşuna basın"
            : "Işıkları yakmak için {E} tuşuna basın";
    }

    public override void Etkiles()
    {
        isiklarAcik = !isiklarAcik;

        if (kontrolEdilenIsiklar != null)
        {
            foreach (Light kontrolEdilenIsik in kontrolEdilenIsiklar)
            {
                if (kontrolEdilenIsik != null)
                {
                    kontrolEdilenIsik.enabled = isiklarAcik;
                }
            }
        }

        DugmeAnimasyonunuOynat();
    }

    public override void VurguyuAyarla(bool aktif)
    {
        if (bakisVurguIsigi != null)
        {
            bakisVurguIsigi.enabled = aktif;
        }

        if (rendererMateryalleri == null)
        {
            return;
        }

        for (int rendererIndex = 0;
             rendererIndex < rendererMateryalleri.Length;
             rendererIndex++)
        {
            Material[] materyaller =
                rendererMateryalleri[rendererIndex];

            for (int materyalIndex = 0;
                 materyalIndex < materyaller.Length;
                 materyalIndex++)
            {
                Material materyal = materyaller[materyalIndex];

                if (materyal == null ||
                    !materyal.HasProperty("_EmissionColor"))
                {
                    continue;
                }

                if (aktif)
                {
                    materyal.EnableKeyword("_EMISSION");
                    materyal.SetColor(
                        "_EmissionColor",
                        normalEmissionRenkleri
                            [rendererIndex][materyalIndex] +
                        vurguRengi * materyalParlamaGucu
                    );
                }
                else
                {
                    materyal.SetColor(
                        "_EmissionColor",
                        normalEmissionRenkleri
                            [rendererIndex][materyalIndex]
                    );

                    if (!normalEmissionDurumlari
                        [rendererIndex][materyalIndex])
                    {
                        materyal.DisableKeyword("_EMISSION");
                    }
                }
            }
        }
    }

    private void IsiklarinMevcutDurumunuBul()
    {
        isiklarAcik = false;

        if (kontrolEdilenIsiklar == null)
        {
            return;
        }

        foreach (Light kontrolEdilenIsik in kontrolEdilenIsiklar)
        {
            if (kontrolEdilenIsik != null)
            {
                isiklarAcik = kontrolEdilenIsik.enabled;
                return;
            }
        }
    }

    private void VurguMateryalleriniHazirla()
    {
        if (vurgulanacakRendererlar == null ||
            vurgulanacakRendererlar.Length == 0)
        {
            vurgulanacakRendererlar =
                GetComponentsInChildren<Renderer>(true);
        }

        rendererMateryalleri =
            new Material[vurgulanacakRendererlar.Length][];

        normalEmissionRenkleri =
            new Color[vurgulanacakRendererlar.Length][];

        normalEmissionDurumlari =
            new bool[vurgulanacakRendererlar.Length][];

        for (int rendererIndex = 0;
             rendererIndex < vurgulanacakRendererlar.Length;
             rendererIndex++)
        {
            Renderer vurguRendereri =
                vurgulanacakRendererlar[rendererIndex];

            Material[] materyaller = vurguRendereri != null
                ? vurguRendereri.materials
                : new Material[0];

            rendererMateryalleri[rendererIndex] = materyaller;
            normalEmissionRenkleri[rendererIndex] =
                new Color[materyaller.Length];
            normalEmissionDurumlari[rendererIndex] =
                new bool[materyaller.Length];

            for (int materyalIndex = 0;
                 materyalIndex < materyaller.Length;
                 materyalIndex++)
            {
                Material materyal = materyaller[materyalIndex];

                if (materyal != null &&
                    materyal.HasProperty("_EmissionColor"))
                {
                    normalEmissionRenkleri
                        [rendererIndex][materyalIndex] =
                        materyal.GetColor("_EmissionColor");

                    normalEmissionDurumlari
                        [rendererIndex][materyalIndex] =
                        materyal.IsKeywordEnabled("_EMISSION");
                }
            }
        }
    }

    private void CevreVurguIsiginiHazirla()
    {
        GameObject vurguIsigiNesnesi =
            new GameObject("Bakış Vurgu Işığı");

        vurguIsigiNesnesi.transform.SetParent(
            transform,
            false
        );

        vurguIsigiNesnesi.transform.localPosition =
            cevreIsigiYerelKonumu;

        bakisVurguIsigi =
            vurguIsigiNesnesi.AddComponent<Light>();

        bakisVurguIsigi.type = LightType.Point;
        bakisVurguIsigi.color = vurguRengi;
        bakisVurguIsigi.intensity = cevreIsigiGucu;
        bakisVurguIsigi.range = cevreIsigiMenzili;
        bakisVurguIsigi.shadows = LightShadows.None;
        bakisVurguIsigi.enabled = false;
    }

    private void DugmeAnimasyonunuOynat()
    {
        if (dugmeAnimatoru == null)
        {
            return;
        }

        string triggerAdi = isiklarAcik
            ? isikAcmaTriggeri
            : isikKapatmaTriggeri;

        if (string.IsNullOrWhiteSpace(triggerAdi))
        {
            return;
        }

        foreach (
            AnimatorControllerParameter parametre
            in dugmeAnimatoru.parameters)
        {
            if (parametre.type ==
                    AnimatorControllerParameterType.Trigger &&
                parametre.name == triggerAdi)
            {
                dugmeAnimatoru.SetTrigger(triggerAdi);
                return;
            }
        }

        Debug.LogWarning(
            "Işık düğmesi Animator'ında '" +
            triggerAdi +
            "' Trigger parametresi bulunamadı."
        );
    }
}
