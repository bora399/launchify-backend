# Launchify incelemesi — 16 Ekim 2026

İncelenenler: .NET backend, Next.js frontend ve canlı ana sayfanın HTTP yanıtı.
Canlı ana sayfa HTTP 200 döndü. Görsel tarayıcı aracı ortam izni nedeniyle
başlatılamadı; mobil görünüm ve giriş sonrası canlı akışlar görsel olarak doğrulanmadı.
Üretim API'sinde yetki açığı denemesi veya veri değiştiren istek yapılmadı.

## Ürünün odağı

Mevcut ürün: AI ile dört şablondan landing page oluşturma, metin düzenleme,
paylaşma, bekleme listesi ve toplam ziyaret takibi. Ana sayfadaki
tam SaaS/.NET altyapısı oluşturma iddiaları mevcut işlevlerle uyuşmuyordu.

## Uygulanan ilk düzeltmeler

- Frontend: ana sayfa vaatleri mevcut landing page işlevleriyle hizalandı.
- Frontend: sayfa render sırasında gönderilen ikinci ziyaret isteği kaldırıldı.
  Ziyaret kaydı yalnız istemci bileşeninden gönderiliyor; aynı bileşenin tekrarlanan
  effect çağrıları istek başlamadan engelleniyor. Bu, benzersiz ziyaretçi ölçümü değildir.
- Backend: Firestore kredi düşümü ve iadesi transaction içine alındı.
- Backend: kredi yokken proje sayısına göre otomatik kredi verme kaldırıldı.
  Kredi artık sunucudaki mevcut bakiyeden harcanıyor; eski tutarsız hesaplar için
  ayrı, kontrollü bir veri onarımı gerekebilir.
- Backend: demo bağlantıları yalnız HTTP/HTTPS kabul ediyor.
- Backend: oluşturma hatalarında sağlayıcının iç hata metni kullanıcıya dönmüyor.
- Backend: gerçek command handler ve validator üzerinden regresyon testleri eklendi.

## P0 — Yeni özelliklerden önce

1. Kimlik doğrulama ve sahiplik kontrolü.
   Program.cs içinde authentication kurulumu ve UseAuthentication yok.
   FirebaseAuthService mevcut fakat servis olarak bağlanmamış.
   Create komutu UserId'yi body'den, delete userId'yi query'den alıyor.
   Update işleminde sahiplik kontrolü yok. User ve waitlist okuma uçları da korunmuyor.
   Frontend özel fetch çağrıları Firebase ID token göndermiyor.
   Çözüm: istemcide Authorization: Bearer ID_TOKEN, sunucuda Firebase doğrulaması,
   claim'den UID çıkarma ve her özel işlemde sahiplik denetimi.
   Public landing page, ziyaret ve bekleme listesine katılım uçları açık kalabilir.
   Güvenlik geçişi iki projede birlikte uygulanmalı ve anonim/başka kullanıcı testleriyle doğrulanmalı.

2. AI maliyet kontrolü.
   ai-assist anonim erişime açık ve oluşturmadaki limit ona uygulanmıyor.
   Mevcut limit anahtarı istemcinin değiştirebildiği X-User-Id ve forwarded header'larına dayanıyor.
   Doğrulanmış UID başına limit, istek uzunluğu/mod doğrulaması ve maliyet kotası eklenmeli.
   Forwarded header güveni yalnız dağıtım ortamının güvenilir proxy'leriyle sınırlandırılmalı.

3. HTML dışa aktarmada sahte başarı.
   app/utils/exportHtml.ts içindeki formlar sadece alert gösterip sıfırlanıyor,
   kayıt API'sine gönderilmiyor. Kullanıcı kayıt topladığını sanabilir.
   Gerçek API entegrasyonu/CORS yapılandırması veya açıkça görsel önizleme olarak
   sunulan pasif form gerekli. Export edilen metinler HTML bağlamında kaçışlanmalı.

## P1 — Veri doğruluğu

- Aynı ürün adından aynı slug üretiliyor; Firestore'da atomik benzersiz slug rezervasyonu eklenmeli.
  create, dashboard, login, about, sss gibi uygulama yolları da rezerve edilmeli.
- SyncUserAsync: Get + Set yarışı hesap kredilerini sıfırlayabilir; transaction/create-if-absent gerekli.
- Delete: eşzamanlı silmeler birden çok kredi iadesi üretebilir.
  Sayfa silme ve kredi iadesi tek transaction olmalı.
- AnalyticsWorker: ToArray + Clear arasında gelen ziyaretler kaybolabilir.
  Repository yazma hatalarını yuttuğu için worker'ın retry mekanizması devreye girmeyebilir.
  Atomik batch alma, başarısız batch retry ve kapanışta flush gerekli.
- Sayfa oluşturma isteği tekrarlarında idempotency anahtarı kullanılmalı.
- Waitlist: e-posta biçimi/uzunluğu, geçerli sayfa kontrolü, atomik tekillik,
  bot koruması ve rate limit eklenmeli.
- Faqs entity'de var fakat kayıt/okuma ve public DTO boyunca taşınmıyor.
- IsActive alanı public sayfa erişiminde uygulanmıyor.
- Slug normalize edilerek cache anahtarı hazırlanıyor, sorguya orijinal slug gidiyor.
- Update ve ai-assist iç hata metinlerini hala dışarı verebiliyor.

## P1 — Frontend deneyimi

- Ana sayfaya dört şablonun gerçek önizlemesini ve örnek bir yayınlanmış sayfayı ekle.
- Create akışını ürün bilgisi → şablon/önizleme → oluştur/paylaş olarak sadeleştir.
- Editor önizlemelerindeki bekleme listesi formları gerçek kayıt oluşturmamalı.
- alert yerine form yanında durum mesajları; erişilebilir modal odağı ve klavye desteği ekle.
- Bekleme listesi için arama, tarih filtresi, sayfalama ve güvenli CSV export ekle.
- localStorage cache ayrıştırma hatalarını yakala; oturum çıkışında hassas cache'i temizle.
- Bulunmayan slug için gerçek notFound/404, backend hatası için ayrı tekrar deneme görünümü kullan.
- Proje başına title, description ve paylaşım görseli ekle.
- API URL yönetimini tek modüle, proje ve AI veri tiplerini ortak TypeScript tiplerine taşı.
  Çoklu camelCase/PascalCase fallback ve any kullanımını azalt.
- Paketlerde next ve eslint-config-next sürümleri farklı ana sürümlerde; uyumluluk kontrolü yap.
- Ana sayfanın tekrarlanan font importlarını merkezi font yönetimine taşı.

## Sonraki ürün özellikleri

1. Taslak/yayın durumu, mobil/masaüstü önizleme, güvenli silme ve kredi açıklaması.
2. Düzenlenebilir özellikler, FAQ, demo linki ve iletişim bilgisi.
3. Günlük ziyaret ve kayıt grafikleri; toplam hit ile benzersiz ziyaretçiyi ayıran metrikler.
4. Özel alan adı ve e-posta bildirimleri; temel akışlar güvenilir hale geldikten sonra.

## Şimdilik çıkar/sadeleştir

- Tam SaaS veya backend ürettiğini ima eden vaatler.
- Gerçekte kayıt yapmayan export formunun başarı mesajları.
- Ana sayfa gibi kullanıcı yüzeylerinde gereksiz teknoloji/mimari anlatımı.
- Kullanılmayan DTO'lar, eski MasalPages namespace'i ve yalnız mock'u test eden testler.
- Özel alan adı henüz yokken middleware'deki gelecek özellik karmaşıklığı değerlendirilmeli.

## Sınırlar

Bu ilk değişiklikler bütün yetkilendirme veya veri tutarlılığı sorunlarını çözmez.
Firestore transaction davranışı emulator/entegrasyon testiyle ayrıca doğrulanmalı.
Canlıya dağıtım yapılmadı.

## Çalıştırılan kontroller

- dotnet test --no-restore: 14 test geçti, 0 başarısız.
- npx tsc --noEmit: başarılı.
- İki projede git diff --check: başarılı.
- Değişen frontend dosyaları için ESLint: mevcut konfigürasyon yüklenemedi.
  eslint-config-next/core-web-vitals importu bulunamıyor; kurulu eslint-config-next
  14 sürümü ve Next 16 uyumu ayrıca düzeltilmeli.
- Firestore emulator testi, üretim build'i ve görsel tarayıcı testi yapılmadı.
