# 📝 Anket Portalı (Survey Portal)

> 🚧 **Durum: Aktif Geliştirme Aşamasında**
> *Bu proje şu anda geliştirilme sürecindedir ve yeni özellikler (anket oluşturma, raporlama vb.) eklenmeye devam etmektedir.*

Kullanıcıların dinamik anketler oluşturabildiği ve katılım sağlayabildiği modern bir anket yönetim sistemidir. Sistem, arka planda veri güvenliğini sağlayan bir API ve son kullanıcıya hitap eden bir MVC arayüzünden oluşur.

## 📸 Ekran Görüntüleri

### 💻 Yönetim Paneli (Anket Oluşturma)
![Admin Panel](images/resim1.png)

### 📝 Kullanıcı Ekranı (Anket Doldurma)
![Survey Screen](images/resim2.png)

## 🏗️ Proje Katmanları

- **`SurveyPortal.API/` (Backend):** Sistemin beyni. Veritabanı işlemleri, güvenlik politikaları ve dış dünyaya sunulan RESTful servisler bu katmanda yer alır.
- **`SurveyPortal.MVC/` (Frontend):** Son kullanıcıların anketleri doldurduğu ve yöneticilerin anketleri oluşturup yönettiği, API ile haberleşen kullanıcı arayüzü.

## 🛠️ Kullanılan Teknolojiler

- **Backend:** C#, ASP.NET Core Web API
- **Frontend:** ASP.NET Core MVC, HTML, CSS, JavaScript, Bootstrap
- **Veritabanı & ORM:** Microsoft SQL Server, Entity Framework Core

## ⚙️ Kurulum (Geliştiriciler İçin)

Projeyi kendi ortamınızda test etmek için şu adımları izleyebilirsiniz:

1. Repoyu klonlayın:
```bash
git clone [https://github.com/Nazar118/SurveyPortal.git](https://github.com/Nazar118/SurveyPortal.git)
