
GLS580 - CROSSHAIR İLE ARACA BİNME VE GERÇEKÇİ SÜRÜŞ SİSTEMİ
==============================================================

DOSYALAR
--------
1) RealisticCarController.cs
2) CarDoorInteractable.cs
3) CarInteractionRaycaster.cs
4) ReverseBeep.wav

KONTROLLER
----------
F       : Şoför kapısına bakarken araca bin / araç dururken araçtan in
W       : Gaz
S       : İleri giderken fren, araç durunca geri vites
A / D   : Direksiyon
SPACE   : El freni

ÖNEMLİ: MODELİN KENDİ SCALE'İNİ BOZMA
--------------------------------------
Ekran görüntünde FBX'in scale değeri yaklaşık 0.42. Bunu zorla 1 yapma.

1. Hierarchy'de boş bir GameObject oluştur:
   GLS580_CarRoot

2. GLS580_CarRoot:
   Position = 0,0,0
   Rotation = 0,0,0
   Scale    = 1,1,1

3. Mercedes modelini GLS580_CarRoot'un altına koy.
   Modelin mevcut 0.42 ölçeği aynen kalabilir.

4. RealisticCarController ve Rigidbody'yi GLS580_CarRoot'a ekle.

ARABA FİZİĞİ KURULUMU
---------------------
GLS580_CarRoot üzerinde:

- Rigidbody:
  Mass = 2300
  Drag / Linear Damping = 0.02 civarı
  Angular Damping = 0.5 civarı
  Interpolate = Interpolate

- Gövde için 2-3 adet BoxCollider ekle.
  Rigidbody bulunan araçta büyük, non-convex MeshCollider kullanma.
  BoxCollider'ları gövdeye, ön kısma ve arka kısma oturt.

- RealisticCarController componentinin sağ üst üç nokta menüsüne bas:
  AUTO SETUP WHEEL COLLIDERS

Script şu isimleri otomatik bulur:
  Wheel_FL
  Wheel_FR
  Wheel_RL
  Wheel_RR

Oluşan WheelCollider objeleri:
  WC_FL
  WC_FR
  WC_RL
  WC_RR

Tekerler zemine fazla gömülürse:
- WheelCollider radius değerini çok az küçült.
- WheelCollider Y konumunu çok az yukarı al.
- Modelin teker meshlerini elle taşıma.

GERİ VİTES SESİ
---------------
1. GLS580_CarRoot altına boş obje oluştur:
   ReverseBeepAudio

2. AudioSource ekle:
   AudioClip = ReverseBeep.wav
   Loop = açık
   Play On Awake = kapalı
   Spatial Blend = 1
   Min Distance = 2
   Max Distance = 25

3. Bu AudioSource'u RealisticCarController içindeki
   Reverse Beep Source alanına sürükle.

GİRİŞ, KOLTUK VE ÇIKIŞ NOKTALARI
--------------------------------
GLS580_CarRoot altında üç boş obje oluştur:

1. EntryPoint
   Şoför kapısının dışında, karakterin animasyona başlayacağı yere koy.
   Mavi Z oku aracın/karakterin bakacağı yönü göstermeli.

2. SeatPoint
   Şoför koltuğunda, karakterin kalçasının geleceği yere koy.
   Rotasyonu aracın ileri yönüne baksın.

3. ExitPoint
   Şoför kapısının dışına koy.
   Araçtan inince karakter burada oluşur.

ARABA KAMERASI
--------------
GLS580_CarRoot altında:
  CarCameraRoot
    CarCamera

oluştur.

CarCameraRoot'u başlangıçta kapalı bırak.
CarCamera'yı aracın arkasında ve biraz yukarıda konumlandır.
Daha sonra ayrı bir takip kamerası eklenebilir; bu sistem yalnızca
kamera köklerini açıp kapatır.

KAPI ETKİLEŞİMİ
---------------
1. Door_FL objesine BoxCollider ekle.
   Collider sadece şoför kapısını kapsasın.
   Is Trigger açık veya kapalı olabilir.

2. GLS580_CarRoot'a CarDoorInteractable ekle.

Alanları şöyle doldur:

Car Controller             = GLS580_CarRoot üzerindeki RealisticCarController
Driver Door Pivot          = Door_FL
Driver Door Visual Root    = Door_FL

Player Root                = karakterin ana objesi
Player Animator            = karakter Animator'u
Player Character Controller= karakter CharacterController'ı
Player Rigidbody           = karakterde varsa Rigidbody

Player Scripts To Disable:
- KarakterHareketi
- BirinciUcuncuSahisKesin veya kullandığın normal kamera kontrol scripti
- Karakteri yürütüp döndüren diğer scriptler

CarInteractionRaycaster'ı bu listeye EKLEME.

Entry Point                = EntryPoint
Seat Point                 = SeatPoint
Exit Point                 = ExitPoint
Player Camera Root         = normal oyuncu kamera kökü
Car Camera Root            = CarCameraRoot

Door Open Euler:
Sol ön kapı yanlış yöne açılırsa Y değerini:
-68 yerine +68 yap.

ENTERINGCAR ANIMATOR AYARI
--------------------------
Animator > Parameters:
Trigger oluştur:
  EnteringCar

Animator'da:
Any State -> EnteringCar state transition oluştur.
Condition:
  EnteringCar trigger

EnteringCar clip/state adı da EnteringCar olmalı.

Animasyon Root Motion içeriyorsa:
- Use Root Motion During Enter açık kalsın.
- Karakter EntryPoint'ten başlayıp koltuğa doğru hareket eder.

Animasyon koltukta bitiyorsa:
- Freeze Final Animation Pose açık kalsın.
Script animasyonun son karesini dondurur ve karakter koltukta oturur.

Animasyon süresi kaç saniyeyse:
- Enter Animation Duration alanına aynı süreyi yaz.

CROSSHAIR RAYCAST
-----------------
Hierarchy'de, kamera objelerinin dışında boş obje oluştur:
  CarInteractionSystem

Üzerine CarInteractionRaycaster ekle.

View Camera:
- normal oyuncu kamerasını ver.

Interaction Distance:
- önerilen 2.5 - 3.0

Bu obje aktif kalmalı. PlayerCameraRoot'un altına koyma; çünkü araca
binince PlayerCameraRoot kapanacak.

YEŞİL KAPI VURGUSU
------------------
CarDoorInteractable, Door_FL meshinden otomatik yeşil dış kabuk oluşturur.
Crosshair kapıya bakarken ve mesafe uygunsa görünür.

Vurgu fazla kalınsa:
Outline Scale = 1.008 - 1.012

Vurgu az görünüyorsa:
Outline Scale = 1.02 - 1.03

URP shader yüzünden görünmezse:
CarDoorInteractable componentinin üç nokta menüsünden:
REBUILD GREEN OUTLINE

FİZİK DAVRANIŞI
---------------
- W'ye basılı tutuldukça gaz yavaşça yükselir.
- W bırakılınca araba süzülür ve yavaşça yavaşlar.
- İleri giderken S frendir.
- Araç durunca S geri vitese geçer.
- Geri viteste dit-dit sesi çalar.
- Space arka tekerlere güçlü el freni uygular.
- Hız arttıkça direksiyon açısı otomatik azalır.
- Dört teker de döner; ön tekerler ayrıca sağa-sola yönlenir.

INPUT ÇALIŞMAZSA
----------------
Edit > Project Settings > Player > Active Input Handling:
  Both
veya
  Input Manager (Old)

seçili olmalı.
