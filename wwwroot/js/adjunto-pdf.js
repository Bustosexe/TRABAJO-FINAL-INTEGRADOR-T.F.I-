// Validación del remito en el navegador (el servidor la repite): solo PDF de hasta 5 MB.
document.addEventListener('change', function (evento) {
    const entrada = evento.target;
    if (!entrada.matches || !entrada.matches('input[data-adjunto-pdf]')) return;

    const archivo = entrada.files[0];
    if (!archivo) return;

    const esPdf = archivo.type === 'application/pdf' || archivo.name.toLowerCase().endsWith('.pdf');
    let motivo = null;
    if (!esPdf) motivo = 'El remito debe ser un archivo PDF.';
    else if (archivo.size > 5 * 1024 * 1024) motivo = 'El remito no puede superar los 5 MB.';

    if (motivo) {
        entrada.value = '';
        Swal.fire({ title: 'Archivo no válido', text: motivo, confirmButtonColor: '#1F3A5F' });
    }
});
