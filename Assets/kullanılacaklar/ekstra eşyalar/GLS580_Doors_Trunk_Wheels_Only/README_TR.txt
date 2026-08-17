MERCEDES GLS 580 — KAPILAR, BAGAJ VE TEKERLEKLER AYRI

Bu sürüm SOURCE_Original.fbx dosyasından yeniden üretildi.
Gövde yeniden modellenmedi; tampon, cam, tavan ve materyallere dokunulmadı.

Ayrı objeler:
- Door_FL
- Door_FR
- Door_RL
- Door_RR
- Trunk
- Wheel_FL
- Wheel_FR
- Wheel_RL
- Wheel_RR
- Geri kalan her şey: Static_Body

Tekerlek pivotları kendi göbek merkezlerindedir. Bu nedenle Wheel_FL, Wheel_FR,
Wheel_RL ve Wheel_RR objeleri local X ekseninde döndürülebilir.

UNITY KURULUMU
1. Eski Doors_Trunk_Only modelini sahneden kaldır.
2. Mercedes_GLS580_Doors_Trunk_Wheels_Only.fbx dosyasını Assets içine koy.
3. FBX'i sahneye sürükle.
4. GLS580DoorTrunkWheelController.cs dosyasını Assets içine koy.
5. Scripti arabanın en üst objesine ekle. Parçaları isimlerinden otomatik bulur.
6. Eski GLS580DoorTrunkController componenti varsa kaldır; iki script aynı anda kapıları yönetmesin.

KLAVYE TESTİ
1 = sol ön kapı
2 = sağ ön kapı
3 = sol arka kapı
4 = sağ arka kapı
5 = bagaj
W / S = tekerlekleri ileri / geri döndürme
A / D = ön tekerlekleri sağa / sola çevirme

Sürüş sistemi bağlandığında demoKeyboardControls kapatılabilir.
Script içerisindeki SetSteering(angle) ve RotateWheelsFromDistance(distance)
metotları araç sürüş kodundan çağrılabilir.

Not: Unity eksen dönüşümünden dolayı tekerlek dönüşü ters görünürse,
AddWheelSpin veya RotateWheelsFromDistance metoduna gönderilen değerin işaretini ters çevir.
