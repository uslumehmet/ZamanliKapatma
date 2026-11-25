# Zamanlı Kapatma

![build](https://github.com/KULLANICI_ADI/ZamanliKapatma/actions/workflows/build.yml/badge.svg)

Windows bilgisayarınızı belirlediğiniz **süre sonra** ya da belirlediğiniz **saatte** kapatan, yeniden başlatan, uyku moduna alan veya oturumu kapatan küçük bir sistem tepsisi uygulaması.

Uygulama görev çubuğundaki bildirim alanında (**gizli simgeler `^`**) çalışır. Zamanlayıcıyı oradan tek tıkla devre dışı bırakabilir ya da yeniden etkinleştirebilirsiniz.

<!-- Ekran görüntüsü eklemek için: docs/ekran-goruntusu.png dosyasını ekleyin ve alttaki satırın yorumunu kaldırın. -->
<!-- ![Ekran görüntüsü](docs/ekran-goruntusu.png) -->

## Özellikler

- ⏱️ **Süre sonra** (örn. 1 saat 30 dakika sonra) veya **belirli saatte** (örn. 23:00) zamanlama
- 🔁 **Her gün tekrarla** (örn. her gece 01:00'de kapat, her sabah 04:00'te yeniden başlat)
- 🔌 Kapat · Yeniden başlat · Uyku · Oturumu kapat
- 🖱️ Tepsi menüsünden **devre dışı bırak / etkinleştir**, **hızlı zamanla** (15 dk – 3 saat) ve iptal
- ⚠️ İşlemden **60 saniye önce uyarı**: ertele, iptal et / bugünlük atla ya da hemen uygula
- 🎨 Durumu gösteren simge: gri (plan yok) · mavi (çalışıyor) · turuncu (devre dışı) · kırmızı (son dakika)
- 💾 Plan diske kaydedilir; uygulama yeniden açıldığında kaldığı yerden devam eder
- 🚀 İsteğe bağlı **Windows açılışında otomatik başlatma**
- 🛡️ Bilgisayar kapalıyken ya da uykudayken kaçırılan işlemler açılışta **çalıştırılmaz**, böylece yeniden başlatma döngüsü oluşmaz
- 🔐 Yönetici izni gerektirmez

## Kurulum

1. [Releases](../../releases) sayfasından `ZamanliKapatma.exe` dosyasını indirin.
2. Çalıştırın; kurulum gerekmez.

> İmzasız uygulamalarda Windows SmartScreen uyarısı çıkabilir: **Ek bilgi → Yine de çalıştır**.

**Simgeyi her zaman görünür yapmak için:** Ayarlar → Kişiselleştirme → Görev çubuğu → Diğer sistem tepsisi simgeleri → *Zamanlı Kapatma*'yı açın.

## Kullanım

| Ne yapmak istiyorsunuz? | Nasıl? |
|---|---|
| Hızlıca zamanlamak | Tepsi simgesine sağ tık → **Hızlı zamanla** → işlem → süre |
| Ayrıntılı zamanlamak | Simgeye sol tık (veya **Zamanla...**) → işlemi ve zamanı seçin → **Zamanla** |
| Geçici olarak durdurmak | Sağ tık → **Devre dışı bırak** (kalan süre korunur) |
| Yeniden başlatmak | Sağ tık → **Etkinleştir** |
| Tamamen iptal etmek | Sağ tık → **Planı iptal et** |

Pencereyi kapatmak uygulamayı kapatmaz; uygulama tepside çalışmaya devam eder. Tamamen çıkmak için: sağ tık → **Çıkış**.

## Kaynaktan derleme

Gereksinim: [.NET 10 SDK](https://dotnet.microsoft.com/download)

```powershell
# Çalıştır
dotnet run --project src/ZamanliKapatma

# Testleri çalıştır
dotnet test

# Tek dosyalık exe üret (publish/ZamanliKapatma.exe)
dotnet publish src/ZamanliKapatma -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -o publish
```

Yeni bir sürüm etiketi (`git tag v1.0.0 && git push --tags`) gönderildiğinde GitHub Actions exe'yi derleyip otomatik olarak **Releases** sayfasına ekler.

## Nasıl çalışır?

- **Kapatma, yeniden başlatma ve oturum kapatma** Windows'un yerleşik `shutdown.exe` aracıyla, **uyku** ise `Application.SetSuspendState` ile yapılır.
- **Zamanlama mantığı** (`Scheduler`) arayüzden bağımsız ayrı bir kütüphanededir. Şimdiki zaman metotlara parametre olarak verildiği için tüm senaryolar (uyarı, erteleme, kaçırılan işlem, her gün tekrar) birim testleriyle doğrulanır.
- **Plan** `%APPDATA%\ZamanliKapatma\state.json` dosyasında saklanır.
- **Otomatik başlatma** `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` kaydıyla yapılır.
- Aynı anda **tek kopya** çalışır; ikinci kez açılırsa mevcut pencere öne gelir.

## Proje yapısı

```
src/
  ZamanliKapatma.Core/    Zamanlama mantığı, metinler ve kayıt (arayüzden bağımsız)
  ZamanliKapatma/         WinForms sistem tepsisi uygulaması
tests/
  ZamanliKapatma.Tests/   xUnit birim testleri
```

## Lisans

[MIT](LICENSE)
