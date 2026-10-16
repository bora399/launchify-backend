# Oturum ve kredi güvenliği — 17 Ekim 2026

## Uygulanan davranış

- Özel uçlar Firebase Admin SDK ile ID token doğrular.
- Create ve user sync işlemleri UID'yi doğrulanmış claim'den alır.
  Sync e-postası da token claim'inden gelir; body ile başka hesap adına işlem yapılamaz.
- Kullanıcı/proje listeleri, update, delete ve waitlist okuma için sahiplik kontrolü vardır.
- AI asistanı giriş ve doğrulanmış kullanıcıya göre mevcut saatlik limite tabidir.
- SignalR bağlantısı oturum hazır olduğunda token ile başlar. Üretim logları yalnız doğrulanmış
  kullanıcıya gönderilir; istemcinin verdiği connectionId başka bir hesaba log yönlendiremez.
- Public sayfa, ping, ziyaret ve waitlist katılımı giriş gerektirmez.
- Yeni hesap 3 kredi alır. Sync transaction'ı mevcut bakiyeyi sıfırlamaz.
- Kredi harcama/iade ve sayfa silme+iade transaction'larla korunur.
  Aynı sayfanın ikinci kez silinmesi tekrar kredi üretmez; iade üst sınırı 3'tür.

## Dağıtım

Frontend ve backend değişiklikleri birlikte kullanılmalı.
Mevcut FIREBASE_JSON veya firebase-key.json/GOOGLE_APPLICATION_CREDENTIALS kurulumu kullanılır;
Firebase:ProjectId istemcinin Firebase projesi ile aynı olmalıdır.
Yeni bir gizli anahtar ya da veritabanı şema geçişi gerektirmez.

Frontend özel isteklerde Firebase getIdToken() ile Authorization: Bearer başlığı gönderir.
Token saklama/yenileme Firebase istemci SDK'sında kalır.

## Kontroller

- WebAPI derlemesi başarılı.
- 32 backend testi geçti; yeni HTTP testleri gerçek Kestrel üzerinden authentication handler
  ve controller'ları çalıştırır. Firebase/veritabanı servisleri test çiftleriyle değiştirilir.
- Anonim/geçersiz token, sahte UID, başka kullanıcı erişimi, gerçek sahibin erişimi ve
  public sayfa/kayıt akışları kapsandı.
- Frontend TypeScript ve webpack üretim derlemesi başarılı.
- Ana sayfa masaüstü/390 piksel mobil görünümü ve mobil menü → giriş geçişi tarayıcıda kontrol edildi.
- Mevcut middleware/Edge ve metadataBase uyarıları devam ediyor.
- Mevcut ESLint yapılandırmasının önceki incelemede saptanan sürüm/import sorunu bu değişiklikte ele alınmadı.
- Gerçek Firebase hesabıyla uçtan uca giriş ve Firestore emulator entegrasyon testi yapılmadı.

## Kaynak

Firebase ID token doğrulama: https://firebase.google.com/docs/auth/admin/verify-id-tokens

Önceki PROJECT_REVIEW.md ilk incelemenin kaydıdır.
Oradaki kimlik doğrulama, sync ve delete yarış bulguları bu değişikliklerle ele alındı.
Slug tekilliği, analytics batch tutarlılığı ve HTML export formları ayrı işlerdir.
