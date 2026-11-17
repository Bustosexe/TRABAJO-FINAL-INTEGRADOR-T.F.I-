// Desplegable de provincia y localidad. La provincia es una lista fija; al elegirla se
// piden al servidor sus localidades oficiales y se ofrecen para buscar escribiendo. Solo se
// aceptan localidades de la lista (el servidor repite la validación). Si no se puede cargar
// la lista, el campo queda libre y se avisa.
(function () {
    const formulario = document.querySelector('form[data-ubicacion]');
    if (!formulario) return;

    const provincia = formulario.querySelector('[data-ubicacion-provincia]');
    const localidad = formulario.querySelector('[data-ubicacion-localidad]');
    if (!provincia || !localidad) return;

    const url = formulario.dataset.url;
    const MENSAJE_INVALIDA = 'Elija una localidad del listado desplegable.';

    // Lista de sugerencias nativa del navegador (datalist) y texto de ayuda bajo el campo
    const sugerencias = document.createElement('datalist');
    sugerencias.id = 'listaLocalidades';
    localidad.setAttribute('list', sugerencias.id);
    localidad.setAttribute('autocomplete', 'off');
    localidad.parentNode.appendChild(sugerencias);

    const ayuda = document.createElement('div');
    ayuda.className = 'form-text';
    localidad.insertAdjacentElement('afterend', ayuda);

    const aviso = formulario.querySelector('[data-valmsg-for="' + localidad.name + '"]');

    let localidades = [];
    let modoLibre = false;
    let contador = 0;
    let avisoPropio = false;

    function buscar(valor) {
        const texto = valor.trim().toLowerCase();
        return localidades.find(function (l) { return l.toLowerCase() === texto; });
    }

    function limpiarAviso() {
        if (!avisoPropio || !aviso) return;
        aviso.textContent = '';
        aviso.classList.remove('field-validation-error');
        aviso.classList.add('field-validation-valid');
        localidad.classList.remove('input-validation-error');
        avisoPropio = false;
    }

    // Devuelve verdadero si la localidad escrita es válida (o si no se puede comprobar)
    function validar() {
        const valor = localidad.value.trim();
        if (modoLibre || localidades.length === 0 || valor === '') { limpiarAviso(); return true; }

        const oficial = buscar(valor);
        if (oficial) {
            localidad.value = oficial;   // escritura oficial (mayúsculas y acentos)
            limpiarAviso();
            return true;
        }

        if (aviso) {
            aviso.textContent = MENSAJE_INVALIDA;
            aviso.classList.remove('field-validation-valid');
            aviso.classList.add('field-validation-error');
            localidad.classList.add('input-validation-error');
            avisoPropio = true;
        }
        return false;
    }

    async function cargar() {
        const nombre = provincia.value;
        const turno = ++contador;   // descarta respuestas de una provincia que ya se cambió

        sugerencias.innerHTML = '';
        localidades = [];
        modoLibre = false;
        limpiarAviso();

        if (!nombre) {
            ayuda.textContent = 'Elija primero la provincia.';
            return;
        }
        ayuda.textContent = 'Cargando localidades...';

        let datos = null;
        try {
            const respuesta = await fetch(url + '?provincia=' + encodeURIComponent(nombre));
            if (respuesta.ok) datos = await respuesta.json();
        } catch (e) {
            // se informa más abajo
        }
        if (turno !== contador) return;

        if (datos && datos.ok && datos.localidades.length > 0) {
            localidades = datos.localidades;
            const fragmento = document.createDocumentFragment();
            localidades.forEach(function (l) {
                const opcion = document.createElement('option');
                opcion.value = l;
                fragmento.appendChild(opcion);
            });
            sugerencias.appendChild(fragmento);
            ayuda.textContent = 'Escriba para buscar y elija una localidad del listado.';
            if (localidad.value.trim() !== '') validar();
        } else {
            modoLibre = true;
            ayuda.textContent = 'No se pudo cargar el listado oficial de localidades. Escríbala manualmente.';
        }
    }

    provincia.addEventListener('change', function () {
        localidad.value = '';
        cargar();
    });
    localidad.addEventListener('change', validar);
    localidad.addEventListener('blur', validar);

    // Antes de enviar se comprueba la localidad (el servidor la vuelve a validar)
    formulario.addEventListener('submit', function (evento) {
        if (!validar()) {
            evento.preventDefault();
            evento.stopImmediatePropagation();
            localidad.focus();
        }
    }, true);

    cargar();
})();
