using UnityEngine;

public class KameraTakip : MonoBehaviour
{
    [Header("Takip Edilecek Karakter")]
    public Transform hedef;

    [Header("Kamera Ayarlarý")]
    public float mesafe = 5f;
    public float yukseklik = 1.5f;
    public float fareHassasiyeti = 3f;
    public float takipYumusakligi = 15f;

    [Header("Dikey Kamera Sýnýrlarý")]
    public float enAsagiAci = -10f;
    public float enYukariAci = 60f;

    [Header("Merdiven Kamera Efekti")]

    // Her adýmda kameranýn ne kadar yükseleceði
    public float adimYukselmeMiktari = 0.08f;

    // Tik hareketinin ne kadar keskin olacaðý
    public float adimKeskinligi = 5f;

    // Efektin giriþ ve çýkýþ yumuþaklýðý
    public float adimYumusakligi = 15f;

    // Animasyon bir döngüde kaç adým içeriyor?
    public int donguBasinaAdimSayisi = 2;

    [Header("Duvar ve Çatý Kamera Kontrolü")]
    public LayerMask kameraEngelKatmanlari = ~0;

    // Küre þeklindeki kontrol kameranýn duvara girmesini engeller.
    [Range(0.05f, 0.5f)]
    public float kameraCarpismaYaricapi = 0.2f;

    // Kamera duvara tamamen yapýþmasýn.
    [Range(0.01f, 0.3f)]
    public float duvardanBosluk = 0.08f;

    // Çok dar yerlerde kameranýn karaktere yaklaþabileceði sýnýr.
    public float minimumKameraMesafesi = 0.4f;

    private float yatayAci;
    private float dikeyAci = 15f;

    private float mevcutAdimYuksekligi;
    private Vector3 yumusatilmisKameraKonumu;

    private Animator karakterAnimator;

    private int isOnStairsHash;
    private int isStairMovingHash;
    private int merdivenCikmaStateHash;

    private readonly RaycastHit[] kameraCarpismaSonuclari =
        new RaycastHit[24];

    void Start()
    {
        if (hedef != null)
        {
            yatayAci =
                hedef.eulerAngles.y;

            karakterAnimator =
                hedef.GetComponent<Animator>();

            if (karakterAnimator == null)
            {
                karakterAnimator =
                    hedef.GetComponentInChildren<Animator>();
            }
        }

        isOnStairsHash =
            Animator.StringToHash(
                "IsOnStairs"
            );

        isStairMovingHash =
            Animator.StringToHash(
                "IsStairMoving"
            );

        merdivenCikmaStateHash =
            Animator.StringToHash(
                "merdiven çýkma"
            );

        yumusatilmisKameraKonumu =
            transform.position;

        Cursor.lockState =
            CursorLockMode.Locked;

        Cursor.visible =
            false;
    }

    void Update()
    {
        if (Cursor.lockState ==
            CursorLockMode.Locked)
        {
            yatayAci +=
                Input.GetAxis("Mouse X") *
                fareHassasiyeti;

            dikeyAci -=
                Input.GetAxis("Mouse Y") *
                fareHassasiyeti;

            dikeyAci =
                Mathf.Clamp(
                    dikeyAci,
                    enAsagiAci,
                    enYukariAci
                );
        }

        // ESC ile fareyi serbest býrak
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState =
                CursorLockMode.None;

            Cursor.visible =
                true;
        }

        // Sol týklamayla kamerayý tekrar kilitle
        if (Input.GetMouseButtonDown(0))
        {
            Cursor.lockState =
                CursorLockMode.Locked;

            Cursor.visible =
                false;
        }
    }

    void LateUpdate()
    {
        if (hedef == null)
            return;

        MerdivenAdimEfektiniHesapla();

        Quaternion kameraAcisi =
            Quaternion.Euler(
                dikeyAci,
                yatayAci,
                0f
            );

        /*
         * Kameranýn normalde bakacaðý nokta.
         * Merdiven efekti ayrý olarak eklenecek.
         */
        Vector3 normalBakisNoktasi =
            hedef.position +
            Vector3.up * yukseklik;

        Vector3 normalKameraKonumu =
            normalBakisNoktasi -
            kameraAcisi *
            Vector3.forward *
            mesafe;

        /*
         * SADECE EKLENEN BÖLÜM:
         * Karakter ile normal kamera konumu arasýnda duvar/çatý varsa
         * kamerayý engelin karakter tarafýna çeker.
         */
        bool kameraEngellendiMi;

        Vector3 guvenliKameraKonumu =
            GuvenliKameraKonumunuBul(
                normalBakisNoktasi,
                normalKameraKonumu,
                out kameraEngellendiMi
            );

        /*
         * Karakter takibini yumuþatýr.
         * Adým efektini bu deðere karýþtýrmýyoruz;
         * böylece týk hareketi kaybolmuyor.
         */
        float takipOrani =
            1f -
            Mathf.Exp(
                -takipYumusakligi *
                Time.deltaTime
            );

        if (kameraEngellendiMi)
        {
            /*
             * Duvar araya girdiði kare kamerayý anýnda içeri alýr.
             * Böylece yumuþatma yüzünden kamera birkaç kare dýþarýda
             * kalýp duvarýn arkasýný göstermez.
             */
            yumusatilmisKameraKonumu =
                guvenliKameraKonumu;
        }
        else
        {
            yumusatilmisKameraKonumu =
                Vector3.Lerp(
                    yumusatilmisKameraKonumu,
                    guvenliKameraKonumu,
                    takipOrani
                );
        }

        Vector3 adimOfseti =
            Vector3.up *
            mevcutAdimYuksekligi;

        transform.position =
            yumusatilmisKameraKonumu +
            adimOfseti;

        transform.LookAt(
            normalBakisNoktasi +
            adimOfseti
        );
    }

    private Vector3 GuvenliKameraKonumunuBul(
        Vector3 bakisNoktasi,
        Vector3 normalKameraKonumu,
        out bool kameraEngellendiMi)
    {
        Vector3 kamerayaDogruYon =
            normalKameraKonumu -
            bakisNoktasi;

        float istenenMesafe =
            kamerayaDogruYon.magnitude;

        kameraEngellendiMi = false;

        if (istenenMesafe <= 0.001f)
        {
            return normalKameraKonumu;
        }

        kamerayaDogruYon.Normalize();

        int carpismaSayisi =
            Physics.SphereCastNonAlloc(
                bakisNoktasi,
                kameraCarpismaYaricapi,
                kamerayaDogruYon,
                kameraCarpismaSonuclari,
                istenenMesafe,
                kameraEngelKatmanlari,
                QueryTriggerInteraction.Ignore
            );

        float enYakinEngelMesafesi =
            istenenMesafe;

        for (int i = 0; i < carpismaSayisi; i++)
        {
            Collider carpisilanCollider =
                kameraCarpismaSonuclari[i].collider;

            if (carpisilanCollider == null)
            {
                continue;
            }

            Transform carpisilanNesne =
                carpisilanCollider.transform;

            // Karakterin kendi colliderlarýný kamera engeli sayma.
            bool karakterinKendisiMi =
                carpisilanNesne == hedef ||
                carpisilanNesne.IsChildOf(hedef);

            if (karakterinKendisiMi)
            {
                continue;
            }

            float carpismaMesafesi =
                kameraCarpismaSonuclari[i].distance;

            if (carpismaMesafesi <
                enYakinEngelMesafesi)
            {
                enYakinEngelMesafesi =
                    carpismaMesafesi;

                kameraEngellendiMi = true;
            }
        }

        if (!kameraEngellendiMi)
        {
            return normalKameraKonumu;
        }

        float guvenliMesafe =
            Mathf.Clamp(
                enYakinEngelMesafesi -
                duvardanBosluk,
                minimumKameraMesafesi,
                istenenMesafe
            );

        return bakisNoktasi +
               kamerayaDogruYon *
               guvenliMesafe;
    }

    private void MerdivenAdimEfektiniHesapla()
    {
        float hedefAdimYuksekligi = 0f;

        if (karakterAnimator != null)
        {
            bool merdivendeMi =
                karakterAnimator.GetBool(
                    isOnStairsHash
                );

            bool merdivendeHareketEdiyorMu =
                karakterAnimator.GetBool(
                    isStairMovingHash
                );

            AnimatorStateInfo mevcutDurum =
                karakterAnimator
                    .GetCurrentAnimatorStateInfo(0);

            bool merdivenAnimasyonuOynuyorMu =
                mevcutDurum.shortNameHash ==
                merdivenCikmaStateHash;

            if (merdivendeMi &&
                merdivendeHareketEdiyorMu &&
                merdivenAnimasyonuOynuyorMu)
            {
                /*
                 * Animasyonun 0 ile 1 arasýndaki
                 * mevcut döngü konumu.
                 */
                float animasyonKonumu =
                    mevcutDurum.normalizedTime %
                    1f;

                /*
                 * Her animasyon döngüsünde iki tepe
                 * oluþturur. Her tepe bir adýmý temsil eder.
                 */
                float adimDalgasi =
                    Mathf.Abs(
                        Mathf.Sin(
                            animasyonKonumu *
                            Mathf.PI *
                            donguBasinaAdimSayisi
                        )
                    );

                /*
                 * Dalganýn daha yuvarlak deðil,
                 * kýsa bir "týk" gibi görünmesini saðlar.
                 */
                float keskinAdim =
                    Mathf.Pow(
                        adimDalgasi,
                        adimKeskinligi
                    );

                hedefAdimYuksekligi =
                    keskinAdim *
                    adimYukselmeMiktari;
            }
        }

        float yumusamaOrani =
            1f -
            Mathf.Exp(
                -adimYumusakligi *
                Time.deltaTime
            );

        mevcutAdimYuksekligi =
            Mathf.Lerp(
                mevcutAdimYuksekligi,
                hedefAdimYuksekligi,
                yumusamaOrani
            );
    }
}