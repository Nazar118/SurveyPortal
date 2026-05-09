$.ajaxSetup({
    beforeSend: function (xhr) {
        var token = localStorage.getItem("token");
        if (token) {
            xhr.setRequestHeader("Authorization", "Bearer " + token);
        }
    },
    error: function (jqXHR, textStatus, errorThrown) {
        if (jqXHR.status === 401) {
            localStorage.removeItem("token");
            window.location.replace("/Auth/Login");
        }
    }
});

const Toast = Swal.mixin({
    toast: true,
    position: 'top-end', // Sağ üst köşe
    showConfirmButton: false, // ONAY BUTONU YOK! (Modern hissiyat)
    timer: 3000, // 3 saniye sonra kendi kapanır
    timerProgressBar: true,
    didOpen: (toast) => {
        toast.addEventListener('mouseenter', Swal.stopTimer)
        toast.addEventListener('mouseleave', Swal.resumeTimer)
    }
});

function showSuccessToast(message) {
    Toast.fire({
        icon: 'success',
        title: message
    });
}

function showErrorToast(message) {
    Toast.fire({
        icon: 'error',
        title: message
    });
}

// 1. Projeyi Karanlık Moda sokacak GÜÇLENDİRİLMİŞ NİHAİ CSS örtüsü
const darkModeCss = `
    <style>
        /* Genel Arka Planlar */
        body.dark-mode, body.dark-mode .content-wrapper, body.dark-mode .page-body-wrapper, body.dark-mode .main-panel, body.dark-mode .container-scroller { background-color: #121212 !important; color: #e0e0e0 !important; transition: background-color 0.3s ease; }
        
        /* Kartlar ve Paneller */
        body.dark-mode .card, body.dark-mode .portal-sidebar, body.dark-mode .portal-content, body.dark-mode .auth-card, body.dark-mode .sidebar { background-color: #1e1e1e !important; color: #e0e0e0 !important; border-color: #333 !important; }
        
        /* Admin Üst Menü ve Sol Üst Logo Alanı */
        body.dark-mode .navbar.default-layout-navbar, body.dark-mode .navbar.default-layout-navbar .navbar-menu-wrapper, body.dark-mode .navbar.user-navbar { background-color: #1e1e1e !important; border-bottom: 1px solid #333 !important; }
        body.dark-mode .navbar .navbar-brand-wrapper { background-color: #1e1e1e !important; border-bottom: 1px solid #333 !important; }
        body.dark-mode .navbar .navbar-brand-wrapper .brand-logo h3 { color: #b66dff !important; }
        body.dark-mode .navbar .navbar-brand, body.dark-mode .sidebar .nav .nav-item .nav-link { color: #e0e0e0 !important; }
        body.dark-mode .sidebar .nav .nav-item .nav-link i.menu-icon { color: #b66dff !important; }
        
        /* 🔥 YENİ: Sol Menü Kenarlardaki Beyazlıkları Ezme (Sinsi Pseudo-elementler yok edildi) */
        body.dark-mode .sidebar { background: #1e1e1e !important; }
        body.dark-mode .sidebar .nav { background: transparent !important; }
        body.dark-mode .sidebar .nav .nav-item { background: transparent !important; border: none !important; }
        body.dark-mode .sidebar .nav .nav-item:hover, body.dark-mode .sidebar .nav .nav-item.active { background: transparent !important; }
        body.dark-mode .sidebar .nav .nav-item > .nav-link { background: transparent !important; }
        body.dark-mode .sidebar .nav .nav-item > .nav-link:hover, body.dark-mode .sidebar .nav .nav-item.active > .nav-link { background-color: #2c2c2c !important; color: #fff !important; }
        body.dark-mode .sidebar .nav .nav-item:hover::before, body.dark-mode .sidebar .nav .nav-item.active::before, body.dark-mode .sidebar .nav .nav-item::before, body.dark-mode .sidebar .nav .nav-item::after { display: none !important; background: transparent !important; }
        
        /* Metinler ve Başlıklar */
        body.dark-mode .text-dark, body.dark-mode .text-black, body.dark-mode .font-weight-bold { color: #e0e0e0 !important; }
        body.dark-mode .text-muted { color: #a0a0a0 !important; }
        body.dark-mode .page-title, body.dark-mode h1, body.dark-mode h2, body.dark-mode h3, body.dark-mode h4, body.dark-mode h5 { color: #e0e0e0 !important; }
        
        /* Açılır Menüler ve Formlar */
        body.dark-mode .bg-white, body.dark-mode .bg-light { background-color: #1e1e1e !important; border-color: #333 !important; color: #e0e0e0 !important; }
        body.dark-mode .form-control, body.dark-mode .form-select, body.dark-mode .input-group-text { background-color: #2c2c2c !important; color: #fff !important; border-color: #444 !important; }
        body.dark-mode .form-control:focus, body.dark-mode .form-select:focus { background-color: #333 !important; color: #fff !important; border-color: #b66dff !important; }
        body.dark-mode select option { background-color: #2c2c2c !important; color: #fff !important; }
        body.dark-mode .form-control::placeholder { color: #888 !important; }
        
        /* Tablolar */
        body.dark-mode .table { color: #e0e0e0 !important; background-color: #1e1e1e !important; border-color: #444 !important; }
        body.dark-mode .table tbody tr, body.dark-mode .table tbody tr:nth-child(odd), body.dark-mode .table tbody tr:nth-child(even), body.dark-mode .table-striped tbody tr:nth-of-type(odd), body.dark-mode .table-striped tbody tr:nth-of-type(even) { background-color: #1e1e1e !important; color: #e0e0e0 !important; box-shadow: inset 0 0 0 9999px #1e1e1e !important; }
        body.dark-mode .table td, body.dark-mode .table th { background-color: transparent !important; color: #e0e0e0 !important; border-color: #333 !important; box-shadow: none !important; }
        body.dark-mode .table thead th, body.dark-mode .table tr.bg-light th { background-color: #2c2c2c !important; border-bottom: 2px solid #444 !important; color: #e0e0e0 !important; }
        body.dark-mode .table tbody tr:hover, body.dark-mode .table tbody tr:hover td, body.dark-mode .table-hover tbody tr:hover, body.dark-mode .table-hover tbody tr:hover td { background-color: #2a2a2a !important; color: #ffffff !important; box-shadow: inset 0 0 0 9999px #2a2a2a !important; }
        
        /* Analiz / Sonuçlar Sayfası */
        body.dark-mode .nav-pills.bg-white { background-color: #1e1e1e !important; border: 1px solid #333 !important; }
        body.dark-mode .alert { background-color: #2c2c2c !important; border-color: #444 !important; color: #e0e0e0 !important; }
        body.dark-mode .alert .font-weight-bold, body.dark-mode .alert .text-dark { color: #e0e0e0 !important; }
        body.dark-mode .tab-content .position-relative.border { background-color: #252525 !important; border-color: #444 !important; }
        
        /* 🔥 YENİ: SweetAlert2 (Uyarı ve Girdi Pencereleri) Karanlık Mod Uyumu */
        body.dark-mode .swal2-popup { background-color: #1e1e1e !important; color: #e0e0e0 !important; border: 1px solid #444 !important; }
        body.dark-mode .swal2-title, body.dark-mode .swal2-html-container { color: #e0e0e0 !important; }
        body.dark-mode .swal2-input { background-color: #2c2c2c !important; color: #fff !important; border-color: #555 !important; }
        body.dark-mode .swal2-input:focus { border-color: #b66dff !important; box-shadow: 0 0 0 2px rgba(182,109,255,0.2) !important; }
        
        /* Diğer Detaylar */
        body.dark-mode .modal-content { background-color: #1e1e1e !important; color: #e0e0e0 !important; }
        body.dark-mode .footer { background-color: #1e1e1e !important; border-top: 1px solid #333 !important; }
        body.dark-mode .list-group-item { background-color: #1e1e1e !important; color: #e0e0e0 !important; border-color: #333 !important; }
        body.dark-mode .split-container .right-side { background-color: #121212 !important; }

        /* 🔥 YENİ: Anket Çözme Sayfası (Kullanıcı Tarafı) Şık/Seçenek Kutuları */
        body.dark-mode .form-check,
        body.dark-mode .form-check label,
        body.dark-mode .card-body .border,
        body.dark-mode label.w-100 {
            background-color: #2c2c2c !important;
            border-color: #444 !important;
            color: #e0e0e0 !important;
        }
        body.dark-mode .form-check:hover,
        body.dark-mode .card-body .border:hover,
        body.dark-mode label.w-100:hover {
            background-color: #383838 !important;
            border-color: #b66dff !important;
            cursor: pointer;
        }
        /* 🔥 YENİ: Profil Sayfası (Kullanıcı Paneli)  Beyaz Hover Çözümleri */
        body.dark-mode .nav-pills .nav-link:hover:not(.active) {
            background-color: #2a2a2a !important;
            color: #b66dff !important;
        }
        body.dark-mode .survey-list-item:hover {
            background-color: #2a2a2a !important;
            border-left-color: #b66dff !important;
        }
       
    </style>
`;
$("head").append(darkModeCss);

// 2. Kullanıcının önceki seçimini hatırla ve uygula
$(document).ready(function () {
    var isDark = localStorage.getItem("darkMode") === "true";
    if (isDark) {
        $("body").addClass("dark-mode");
    }
    updateDarkModeIcon(isDark); // Simgeleri güncelle
});

// 3. Geçiş Düğmesine (Aya/Güneşe) tıklandığında çalışacak kod
$(document).on("click", ".btn-dark-mode-toggle", function (e) {
    e.preventDefault();
    $("body").toggleClass("dark-mode");

    var isDark = $("body").hasClass("dark-mode");
    localStorage.setItem("darkMode", isDark); // Seçimi tarayıcıya kaydet

    updateDarkModeIcon(isDark);

});

// 4. Simgeleri Ay/Güneş olarak değiştiren fonksiyon
function updateDarkModeIcon(isDark) {
    if (isDark) {
        $(".darkModeIcon").removeClass("mdi-weather-night text-dark").addClass("mdi-white-balance-sunny text-warning");
    } else {
        $(".darkModeIcon").removeClass("mdi-white-balance-sunny text-warning").addClass("mdi-weather-night text-dark");
    }
}