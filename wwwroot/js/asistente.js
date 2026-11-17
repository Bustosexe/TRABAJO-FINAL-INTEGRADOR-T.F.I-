// Chat asistente con IA (ventana flotante). Envía la consulta por AJAX al servidor,
// que arma el contexto de stock y consulta al proveedor de IA: la API Key nunca
// llega al navegador. La conversación se guarda en sessionStorage para que no se
// pierda al cambiar de pantalla; se borra al cerrar la pestaña.
(function () {
    const raiz = document.getElementById('ssChat');
    if (!raiz) return;

    const CLAVE_HISTORIAL = 'ssChatHistorial';
    const CLAVE_ABIERTO = 'ssChatAbierto';
    const MAXIMO_GUARDADO = 30;      // mensajes que se conservan en pantalla
    const MAXIMO_ENVIADO = 6;        // turnos previos que se envían como contexto
    const ERROR_GENERICO = 'No se pudo completar la consulta. Recargue la página e intente nuevamente.';

    const boton = document.getElementById('ssChatBoton');
    const panel = document.getElementById('ssChatPanel');
    const lista = document.getElementById('ssChatMensajes');
    const sugerencias = document.getElementById('ssChatSugerencias');
    const formulario = document.getElementById('ssChatForm');
    const entrada = document.getElementById('ssChatEntrada');
    const enviar = document.getElementById('ssChatEnviar');
    const token = formulario.querySelector('input[name="__RequestVerificationToken"]').value;

    let historial = [];
    let esperando = false;

    // El almacenamiento del navegador puede no estar disponible: el chat funciona igual
    function leer(clave) {
        try { return sessionStorage.getItem(clave); } catch (e) { return null; }
    }
    function guardar(clave, valor) {
        try { sessionStorage.setItem(clave, valor); } catch (e) { /* sin almacenamiento */ }
    }

    function agregarBurbuja(rol, texto) {
        const burbuja = document.createElement('div');
        burbuja.className = 'ss-chat-burbuja ' + (rol === 'user' ? 'ss-chat-usuario' : 'ss-chat-asistente');
        // textContent: la respuesta de la IA se muestra como texto, nunca como HTML
        burbuja.textContent = texto;
        lista.appendChild(burbuja);
        lista.scrollTop = lista.scrollHeight;
        return burbuja;
    }

    function dibujar() {
        lista.innerHTML = '';
        if (historial.length === 0) {
            agregarBurbuja('assistant', 'Puedo responder consultas sobre el stock, los vencimientos y el uso del sistema.');
        }
        historial.forEach(function (m) { agregarBurbuja(m.rol, m.texto); });
        sugerencias.classList.toggle('d-none', historial.length > 0);
    }

    function abrir(abierto) {
        panel.classList.toggle('d-none', !abierto);
        boton.classList.toggle('d-none', abierto);
        boton.setAttribute('aria-expanded', abierto ? 'true' : 'false');
        guardar(CLAVE_ABIERTO, abierto ? '1' : '0');
        if (abierto) {
            lista.scrollTop = lista.scrollHeight;
            entrada.focus();
        }
    }

    async function consultar(texto) {
        texto = texto.trim();
        if (!texto || esperando) return;

        const previos = historial.slice(-MAXIMO_ENVIADO);
        historial.push({ rol: 'user', texto: texto });
        dibujar();

        esperando = true;
        entrada.value = '';
        entrada.disabled = true;
        enviar.disabled = true;
        const aviso = agregarBurbuja('assistant', 'Escribiendo...');
        aviso.classList.add('ss-chat-espera');

        let respuesta = null;
        let error = ERROR_GENERICO;
        try {
            const resultado = await fetch(raiz.dataset.url, {
                method: 'POST',
                headers: { 'RequestVerificationToken': token },
                body: new URLSearchParams({ mensaje: texto, historial: JSON.stringify(previos) })
            });
            if (resultado.ok) {
                const datos = await resultado.json();
                if (datos.ok) respuesta = datos.texto;
                else if (datos.error) error = datos.error;
            }
        } catch (e) {
            // se informa el error genérico
        }

        if (respuesta) {
            historial.push({ rol: 'assistant', texto: respuesta });
        } else {
            // La consulta fallida no queda en el historial que se envía a la IA
            historial.pop();
        }
        historial = historial.slice(-MAXIMO_GUARDADO);
        guardar(CLAVE_HISTORIAL, JSON.stringify(historial));
        dibujar();

        if (!respuesta) {
            agregarBurbuja('user', texto);
            agregarBurbuja('assistant', error).classList.add('ss-chat-error');
            sugerencias.classList.add('d-none');
        }

        esperando = false;
        entrada.disabled = false;
        enviar.disabled = false;
        entrada.focus();
    }

    boton.addEventListener('click', function () { abrir(true); });
    document.getElementById('ssChatCerrar').addEventListener('click', function () { abrir(false); });

    document.getElementById('ssChatNueva').addEventListener('click', function () {
        if (esperando) return;
        historial = [];
        guardar(CLAVE_HISTORIAL, '[]');
        dibujar();
        entrada.focus();
    });

    formulario.addEventListener('submit', function (evento) {
        evento.preventDefault();
        consultar(entrada.value);
    });

    sugerencias.querySelectorAll('[data-sugerencia]').forEach(function (opcion) {
        opcion.addEventListener('click', function () { consultar(opcion.dataset.sugerencia); });
    });

    // Estado inicial: se recupera la conversación de esta pestaña
    try {
        const guardado = JSON.parse(leer(CLAVE_HISTORIAL) || '[]');
        if (Array.isArray(guardado)) {
            historial = guardado.filter(function (m) {
                return m && (m.rol === 'user' || m.rol === 'assistant') && typeof m.texto === 'string';
            });
        }
    } catch (e) {
        historial = [];
    }
    dibujar();
    if (leer(CLAVE_ABIERTO) === '1') abrir(true);
})();
