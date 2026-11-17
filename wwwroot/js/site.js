// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// Comportamiento común del sistema de control de stock.

// 1. Confirmación con SweetAlert2: cualquier formulario con el atributo
//    data-confirmar pide confirmación antes de enviarse (cerrar sesión,
//    dar de baja un registro, etc.).
document.addEventListener('submit', function (evento) {
    const formulario = evento.target;
    const mensaje = formulario.dataset.confirmar;
    if (!mensaje) return;

    evento.preventDefault();
    Swal.fire({
        title: formulario.dataset.titulo || 'Confirmar acción',
        text: mensaje,
        showCancelButton: true,
        confirmButtonText: formulario.dataset.si || 'Confirmar',
        cancelButtonText: 'Cancelar',
        confirmButtonColor: '#1F3A5F',
        cancelButtonColor: '#5B6F86',
        reverseButtons: true
    }).then(function (resultado) {
        // submit() directo no vuelve a disparar este evento
        if (resultado.isConfirmed) formulario.submit();
    });
});

// 2. Avisos de resultado: los controladores guardan el mensaje en
//    TempData["Exito"] o TempData["Error"] y el layout lo muestra aquí.
document.addEventListener('DOMContentLoaded', function () {
    const contenedor = document.getElementById('ss-mensajes');
    if (!contenedor) return;

    const mostrar = function (texto, clase) {
        if (!texto) return;
        Swal.fire({
            toast: true,
            position: 'top-end',
            title: texto,
            showConfirmButton: false,
            timer: 4000,
            timerProgressBar: true,
            customClass: { popup: clase }
        });
    };

    mostrar(contenedor.dataset.exito, 'ss-toast-exito');
    mostrar(contenedor.dataset.error, 'ss-toast-error');
});

// 3. Barra lateral en pantallas chicas: el botón Menú la abre y el velo, la tecla
//    Escape o elegir una opción la cierran. En pantallas grandes siempre está visible.
document.addEventListener('DOMContentLoaded', function () {
    const boton = document.getElementById('ssMenuBoton');
    const velo = document.getElementById('ssVelo');
    const lateral = document.getElementById('ssLateral');
    if (!boton || !lateral) return;

    const alternar = function (abrir) {
        document.body.classList.toggle('ss-lateral-abierto', abrir);
        boton.setAttribute('aria-expanded', abrir ? 'true' : 'false');
    };

    boton.addEventListener('click', function () {
        alternar(!document.body.classList.contains('ss-lateral-abierto'));
    });
    if (velo) velo.addEventListener('click', function () { alternar(false); });
    lateral.addEventListener('click', function (evento) {
        if (evento.target.closest('a')) alternar(false);
    });
    document.addEventListener('keydown', function (evento) {
        if (evento.key === 'Escape') alternar(false);
    });
});
