
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