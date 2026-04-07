# 📰 Haber Portalı - Akıllı Backend API

## 🚀 Proje Amacı
Haber toplama, yapay zeka ile içerik üretim ve veri yönetim süreçlerini otomatize eden kapsamlı bir API çözümüdür. Proje, sadece veri sunmakla kalmaz, aynı zamanda veritabanı seviyesinde akıllı temizlik ve güvenlik protokollerini işletir.

## ✨ Temel Özellikler & İş Mantığı
* **🤖 AI & RSS Integration:** RSS kaynaklarından çekilen haberlerin LLM (Yapay Zeka) modelleri ile otomatik olarak yeniden yazılması.
* **🛡️ Onay Mekanizması:** Kaydedilen her içerik `OnayDurumu: 0` olarak başlar. Kalite kontrolü sonrası editör tarafından yayına alınır.
* **🧹 Akıllı Veri Temizliği (Smart Cleaning):** Çekilen haberlerde içerik boşluğu (Null) veya veri bozulması tespit edildiğinde, sistem bu kayıtları veritabanından otomatik olarak temizler.
* **🔐 JWT & Role Management:** Microsoft Identity ile Admin, Moderator ve User rolleri üzerinden tam kapsamlı yetkilendirme.
* **📑 Swagger/OpenAPI:** Tüm içerik operasyonlarının (Ekle/Sil/Onayla) görsel ve hızlı bir şekilde test edilebildiği dökümantasyon arayüzü.

## 🛠️ Kullanılan Teknolojiler
* **⚙️ .NET 8** (ASP.NET Core Web API)
* **🗃️ Entity Framework Core** (SQLite Provider)
* **🔑 Microsoft.AspNetCore.Identity & JWT**
* **📑 Swashbuckle / Swagger**

## 🧑‍💻 Kullanım Rehberi (Swagger ile Yönetim)
1. `/api/Auth/login` endpoint'i üzerinden Admin bilgilerinizle giriş yapın.
2. Dönen **JWT Token**'ı kopyalayın.
3. Sağ üstteki **Authorize** butonuna tıklayıp `Bearer <token>` yazarak giriş yapın.
4. Artık korumalı olan haber onaylama, kategori silme veya RSS tetikleme işlemlerini gerçekleştirebilirsiniz.

## ⚙️ Kurulum
1. `dotnet restore`
2. `dotnet ef database update`
3. `dotnet run`
