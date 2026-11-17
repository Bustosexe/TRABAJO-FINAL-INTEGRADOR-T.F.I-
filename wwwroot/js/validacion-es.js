// Validación numérica con formato argentino: coma decimal y punto de miles.
// jQuery Validation solo acepta punto decimal, por lo que "10,50" sería rechazado.
(function ($) {
    if (!$ || !$.validator) return;

    const aNumero = function (valor) {
        return parseFloat(String(valor).replace(/\./g, '').replace(',', '.'));
    };

    $.validator.methods.number = function (valor, elemento) {
        return this.optional(elemento) ||
            /^-?(?:\d+|\d{1,3}(?:\.\d{3})+)(?:,\d+)?$/.test(valor);
    };

    $.validator.methods.range = function (valor, elemento, parametros) {
        const numero = aNumero(valor);
        return this.optional(elemento) || (numero >= parametros[0] && numero <= parametros[1]);
    };
})(window.jQuery);

// Campos de coordenadas: acepta el punto decimal que copia Google Maps y lo convierte a coma
document.addEventListener('change', function (evento) {
    if (!evento.target.matches('[data-coordenada]')) return;
    const valor = evento.target.value.trim();
    if (/^-?\d+\.\d+$/.test(valor)) evento.target.value = valor.replace('.', ',');
});