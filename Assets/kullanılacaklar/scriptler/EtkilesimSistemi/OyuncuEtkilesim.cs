using UnityEngine;

public class OyuncuEtkilesim : MonoBehaviour
{
    [Header("Etkileşim Kontrolü")]
    public Camera oyuncuKamerasi;
    public Transform oyuncuKoku;
    public KeyCode etkilesimTusu = KeyCode.E;
    public float etkilesimMesafesi = 2.5f;
    public LayerMask etkilesimKatmanlari = ~0;

    [Header("Sol Üst Mesaj")]
    public Vector2 mesajKonumu = new Vector2(20f, 20f);
    public Vector2 mesajBoyutu = new Vector2(480f, 54f);
    public int yaziBoyutu = 22;
    public Color eTusuRengi = new Color(1f, 0.75f, 0.1f, 1f);

    private Etkilesilebilir bakilanNesne;
    private GUIStyle mesajStili;
    private GUIStyle arkaPlanStili;
    private Texture2D arkaPlanDokusu;
    private readonly Collider[] yakindakiColliderlar =
        new Collider[64];

    void Start()
    {
        if (oyuncuKamerasi == null)
        {
            oyuncuKamerasi = Camera.main;
        }

        if (oyuncuKamerasi == null)
        {
            Debug.LogError(
                "OyuncuEtkilesim: Oyuncu Kamerası bulunamadı."
            );

            return;
        }

        if (oyuncuKoku == null)
        {
            KameraTakip kameraTakibi =
                oyuncuKamerasi.GetComponent<KameraTakip>();

            if (kameraTakibi != null)
            {
                oyuncuKoku = kameraTakibi.hedef;
            }
        }

        if (oyuncuKoku == null)
        {
            CharacterController bulunanKarakter =
                FindFirstObjectByType<CharacterController>();

            if (bulunanKarakter != null)
            {
                oyuncuKoku = bulunanKarakter.transform;
            }
        }
    }

    void Update()
    {
        BakilanNesneyiGuncelle();

        if (bakilanNesne != null &&
            Input.GetKeyDown(etkilesimTusu))
        {
            bakilanNesne.Etkiles();
        }
    }

    private void BakilanNesneyiGuncelle()
    {
        Etkilesilebilir yeniBakilanNesne =
            YakindakiEtkilesilebilirNesneyiBul();

        if (yeniBakilanNesne == bakilanNesne)
        {
            return;
        }

        if (bakilanNesne != null)
        {
            bakilanNesne.VurguyuAyarla(false);
        }

        bakilanNesne = yeniBakilanNesne;

        if (bakilanNesne != null)
        {
            bakilanNesne.VurguyuAyarla(true);
        }
    }

    private Etkilesilebilir YakindakiEtkilesilebilirNesneyiBul()
    {
        if (oyuncuKamerasi == null)
        {
            return null;
        }

        Vector3 taramaMerkezi = oyuncuKoku != null
            ? oyuncuKoku.position + Vector3.up
            : oyuncuKamerasi.transform.position;

        int colliderSayisi = Physics.OverlapSphereNonAlloc(
            taramaMerkezi,
            etkilesimMesafesi,
            yakindakiColliderlar,
            etkilesimKatmanlari,
            QueryTriggerInteraction.Ignore
        );

        Etkilesilebilir enUygunNesne = null;
        float enIyiPuan = float.PositiveInfinity;

        for (int i = 0; i < colliderSayisi; i++)
        {
            Collider bulunanCollider = yakindakiColliderlar[i];

            if (bulunanCollider == null)
            {
                continue;
            }

            Transform bulunanTransform =
                bulunanCollider.transform;

            bool oyuncununKendisi =
                oyuncuKoku != null &&
                (bulunanTransform == oyuncuKoku ||
                 bulunanTransform.IsChildOf(oyuncuKoku));

            if (oyuncununKendisi)
            {
                continue;
            }

            Etkilesilebilir etkilesilebilirNesne =
                bulunanCollider
                    .GetComponentInParent<Etkilesilebilir>();

            if (etkilesilebilirNesne == null)
            {
                continue;
            }

            Vector3 hedefNoktasi =
                bulunanCollider.bounds.center;

            Vector3 ekrandakiKonum =
                oyuncuKamerasi.WorldToViewportPoint(
                    hedefNoktasi
                );

            bool ekrandaMi =
                ekrandakiKonum.z > 0f &&
                ekrandakiKonum.x >= -0.05f &&
                ekrandakiKonum.x <= 1.05f &&
                ekrandakiKonum.y >= -0.05f &&
                ekrandakiKonum.y <= 1.05f;

            if (!ekrandaMi)
            {
                continue;
            }

            Vector3 enYakinNokta =
                bulunanCollider.ClosestPoint(taramaMerkezi);

            float oyuncuyaMesafe =
                Vector3.Distance(
                    taramaMerkezi,
                    enYakinNokta
                );

            Vector2 ekranMerkezindenUzaklik =
                new Vector2(
                    ekrandakiKonum.x - 0.5f,
                    ekrandakiKonum.y - 0.5f
                );

            float secimPuani =
                oyuncuyaMesafe +
                ekranMerkezindenUzaklik.sqrMagnitude * 2f;

            if (secimPuani < enIyiPuan)
            {
                enIyiPuan = secimPuani;
                enUygunNesne = etkilesilebilirNesne;
            }
        }

        return enUygunNesne;
    }

    void OnDisable()
    {
        if (bakilanNesne != null)
        {
            bakilanNesne.VurguyuAyarla(false);
            bakilanNesne = null;
        }
    }

    void OnGUI()
    {
        if (bakilanNesne == null)
        {
            return;
        }

        GUIStilleriniHazirla();

        Rect mesajAlani = new Rect(
            mesajKonumu.x,
            mesajKonumu.y,
            mesajBoyutu.x,
            mesajBoyutu.y
        );

        GUI.Box(
            mesajAlani,
            GUIContent.none,
            arkaPlanStili
        );

        string eRengi = ColorUtility.ToHtmlStringRGB(
            eTusuRengi
        );

        string mesaj =
            bakilanNesne
                .EtkilesimMesajiniAl()
                .Replace(
                    "{E}",
                    "<color=#" + eRengi + "><b>E</b></color>"
                );

        Rect yaziAlani = new Rect(
            mesajAlani.x + 16f,
            mesajAlani.y,
            mesajAlani.width - 32f,
            mesajAlani.height
        );

        GUI.Label(
            yaziAlani,
            mesaj,
            mesajStili
        );
    }

    private void GUIStilleriniHazirla()
    {
        if (mesajStili != null)
        {
            return;
        }

        mesajStili = new GUIStyle(GUI.skin.label);
        mesajStili.richText = true;
        mesajStili.fontSize = yaziBoyutu;
        mesajStili.normal.textColor = Color.white;
        mesajStili.alignment = TextAnchor.MiddleLeft;
        mesajStili.wordWrap = false;

        arkaPlanDokusu = new Texture2D(1, 1);
        arkaPlanDokusu.name = "Etkilesim Mesaji Arka Plani";
        arkaPlanDokusu.SetPixel(
            0,
            0,
            new Color(0f, 0f, 0f, 0.72f)
        );
        arkaPlanDokusu.Apply();

        arkaPlanStili = new GUIStyle(GUI.skin.box);
        arkaPlanStili.normal.background = arkaPlanDokusu;
    }

    void OnDestroy()
    {
        if (arkaPlanDokusu != null)
        {
            Destroy(arkaPlanDokusu);
        }
    }
}