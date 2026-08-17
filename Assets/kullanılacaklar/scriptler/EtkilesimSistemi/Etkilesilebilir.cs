using UnityEngine;

public abstract class Etkilesilebilir : MonoBehaviour
{
    // Mesajdaki {E} yazısı ekranda renkli ve kalın gösterilir.
    public abstract string EtkilesimMesajiniAl();

    // Oyuncu E tuşuna bastığında çalışır.
    public abstract void Etkiles();

    // Oyuncu nesneye baktığında açılır, bakmayı bırakınca kapanır.
    public virtual void VurguyuAyarla(bool aktif)
    {
    }
}
