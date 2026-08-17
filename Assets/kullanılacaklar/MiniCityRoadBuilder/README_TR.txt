MINI CITY ROAD BUILDER — KURULUM VE KULLANIM
=============================================

UYUMLULUK
---------
Unity 2021.3 LTS ve daha yeni sürümler için hazırlanmıştır.
Built-in Render Pipeline, URP ve HDRP için uygun materyal shader'ını otomatik bulmayı dener.

KURULUM
-------
1. ZIP dosyasını aç.
2. İçindeki "MiniCityRoadBuilder" klasörünü Unity projenin "Assets" klasörüne taşı.
3. Unity'ye dön ve scriptlerin derlenmesini bekle.
4. Üst menüden:
   Tools > Mini City > Yol Çizici
   penceresini aç.

YOL ÇİZME
---------
1. Yol Türü seç:
   - Sokak
   - Ana Cadde
   - Çevre Yolu
   - Rampa
2. Çevre yolu tam halka olacaksa "Kapalı Döngü" seçeneğini aç.
3. "YENİ YOL ÇİZMEYE BAŞLA" butonuna bas.
4. Scene ekranına sol tıklayarak yol noktalarını koy.
5. Viraj yapmak için dönüş boyunca birkaç nokta yerleştir.
6. Enter ile yolu bitir.

KONTROLLER
----------
Sol tık                  : Yeni yol noktası
Enter / Escape           : Yolu bitir
Backspace / Delete       : Son noktayı sil
Sağ tık                  : Son noktayı sil
Alt + fare               : Scene kamerasını hareket ettir

YOLU SONRADAN DÜZENLEME
-----------------------
Hierarchy'den yolu seç.
Scene ekranındaki mavi P1, P2, P3... tutamaçlarını taşı.
Yol ve viraj otomatik yeniden oluşur.

ÇEVRE YOLU İÇİN ÖNERİ
---------------------
- Yol türü: Çevre Yolu
- Kapalı Döngü: Açık
- Haritanın etrafına 8-16 nokta koy.
- Keskin köşe yerine dönüşlere 2-3 ara nokta ekle.
- Noktaları geniş aralıkla yerleştir.
- Inspector'da Samples Per Segment değerini 14-20 yaparsan viraj daha yumuşak olur.

RAMPA / KÖPRÜ GİRİŞİ
--------------------
Rampa türünü seç.
Noktaları yerleştirdikten sonra yolu seç.
Scene ekranındaki nokta tutamaçlarını yukarı kaldırarak yükselen veya alçalan yol oluştur.

ÖNEMLİ
------
Bu ilk sürüm yol çizme, viraj, kapalı çevre yolu, şerit, kaldırım ve collider üretir.
Otomatik kavşak birleştirme, trafik ışıkları, yol silme aracı ve trafik düğümleri sonraki aşamadır.
