using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Doku gerektirmeyen, yumusak parlayan ve sola akan UGUI yon oku.
/// </summary>
[DisallowMultipleComponent]
public sealed class SurguluKapiYonOku : Graphic
{
    [SerializeField] private float kaymaMesafesi = 16f;
    [SerializeField] private float animasyonHizi = 2.2f;
    [SerializeField] private float nabizMiktari = 0.08f;

    private Vector2 temelKonum;
    private bool temelKonumHazir;

    public void EkranKonumunuAyarla(Vector3 ekranNoktasi, Vector2 ofset)
    {
        if (ekranNoktasi.z <= 0f)
        {
            canvasRenderer.SetAlpha(0f);
            return;
        }

        canvasRenderer.SetAlpha(1f);
        temelKonum = new Vector2(ekranNoktasi.x, ekranNoktasi.y) + ofset;
        temelKonum.x = Mathf.Clamp(temelKonum.x, 85f, Screen.width - 85f);
        temelKonum.y = Mathf.Clamp(temelKonum.y, 55f, Screen.height - 55f);
        temelKonumHazir = true;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        temelKonumHazir = false;
        canvasRenderer.SetAlpha(1f);
    }

    private void Update()
    {
        if (!temelKonumHazir)
            return;

        float zaman = Time.unscaledTime * animasyonHizi;
        float dalga = (Mathf.Sin(zaman * Mathf.PI * 2f) + 1f) * 0.5f;
        float solaAkis = Mathf.Repeat(zaman, 1f);

        rectTransform.position = temelKonum +
                                 Vector2.left * (solaAkis * kaymaMesafesi);

        float olcek = 1f + (dalga - 0.5f) * 2f * nabizMiktari;
        rectTransform.localScale = new Vector3(olcek, olcek, 1f);
        canvasRenderer.SetAlpha(Mathf.Lerp(0.72f, 1f, dalga));
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect alan = GetPixelAdjustedRect();

        Color32 golge = new Color(
            color.r,
            color.g,
            color.b,
            color.a * 0.18f
        );

        OkCiz(vh, alan, golge, 8f);
        OkCiz(vh, alan, color, 0f);
        VurguCiz(vh, alan);
    }

    private void OkCiz(VertexHelper vh, Rect alan, Color32 renk, float buyume)
    {
        float sol = alan.xMin + 8f - buyume;
        float sag = alan.xMax - 8f + buyume;
        float ortaY = alan.center.y;
        float govdeYari = Mathf.Max(7f, alan.height * 0.16f) + buyume * 0.25f;
        float basUzunlugu = alan.width * 0.34f + buyume;
        float basYari = alan.height * 0.43f + buyume;
        float boyunX = sol + basUzunlugu;

        DortgenEkle(
            vh,
            new Vector2(boyunX - 2f, ortaY - govdeYari),
            new Vector2(sag, ortaY + govdeYari),
            renk
        );

        UcgenEkle(
            vh,
            new Vector2(sol, ortaY),
            new Vector2(boyunX, ortaY + basYari),
            new Vector2(boyunX, ortaY - basYari),
            renk
        );
    }

    private void VurguCiz(VertexHelper vh, Rect alan)
    {
        Color32 vurgu = new Color(1f, 1f, 1f, color.a * 0.72f);
        float y = alan.center.y + alan.height * 0.1f;
        float x1 = alan.xMin + alan.width * 0.39f;
        float x2 = alan.xMax - alan.width * 0.11f;

        DortgenEkle(
            vh,
            new Vector2(x1, y),
            new Vector2(x2, y + Mathf.Max(2f, alan.height * 0.045f)),
            vurgu
        );
    }

    private static void DortgenEkle(
        VertexHelper vh,
        Vector2 min,
        Vector2 max,
        Color32 renk)
    {
        int baslangic = vh.currentVertCount;
        vh.AddVert(new Vector3(min.x, min.y), renk, Vector2.zero);
        vh.AddVert(new Vector3(min.x, max.y), renk, Vector2.up);
        vh.AddVert(new Vector3(max.x, max.y), renk, Vector2.one);
        vh.AddVert(new Vector3(max.x, min.y), renk, Vector2.right);
        vh.AddTriangle(baslangic, baslangic + 1, baslangic + 2);
        vh.AddTriangle(baslangic, baslangic + 2, baslangic + 3);
    }

    private static void UcgenEkle(
        VertexHelper vh,
        Vector2 a,
        Vector2 b,
        Vector2 c,
        Color32 renk)
    {
        int baslangic = vh.currentVertCount;
        vh.AddVert(new Vector3(a.x, a.y), renk, Vector2.zero);
        vh.AddVert(new Vector3(b.x, b.y), renk, Vector2.up);
        vh.AddVert(new Vector3(c.x, c.y), renk, Vector2.right);
        vh.AddTriangle(baslangic, baslangic + 1, baslangic + 2);
    }
}
