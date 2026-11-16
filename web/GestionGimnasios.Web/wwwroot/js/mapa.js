// Mapas de sucursales con Leaflet y OpenStreetMap.
//  - [data-selector-ubicacion]: el usuario marca la sucursal en el mapa y el formulario guarda
//    las coordenadas en campos ocultos; nunca tiene que escribir latitud ni longitud.
//  - [data-mapa-sucursales]: todas las sucursales con su marcador.
(function () {
  'use strict';

  const SANTA_CRUZ = [-17.7833, -63.1821];
  const NOMINATIM = 'https://nominatim.openstreetmap.org';

  function capaBase() {
    return L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
      maxZoom: 19,
      attribution: '&copy; colaboradores de <a href="https://www.openstreetmap.org/copyright" target="_blank" rel="noopener">OpenStreetMap</a>',
    });
  }

  function numero(texto) {
    const n = parseFloat(String(texto ?? '').replace(',', '.'));
    return Number.isFinite(n) ? n : null;
  }

  function selectorUbicacion(raiz) {
    const lat = raiz.querySelector('[data-latitud]');
    const lng = raiz.querySelector('[data-longitud]');
    const estado = raiz.querySelector('[data-estado]');
    const buscarTexto = raiz.querySelector('[data-buscar-texto]');
    const buscarBoton = raiz.querySelector('[data-buscar]');
    const direccion = document.querySelector(raiz.dataset.direccion);
    const ciudad = document.querySelector(raiz.dataset.ciudad);

    const la = numero(lat.value), lo = numero(lng.value);
    const marcada = la !== null && lo !== null && !(la === 0 && lo === 0);
    const mapa = L.map(raiz.querySelector('[data-mapa]')).setView(marcada ? [la, lo] : SANTA_CRUZ, marcada ? 17 : 13);
    capaBase().addTo(mapa);

    let marcador = null;

    function avisar(texto, tipo) {
      estado.textContent = texto;
      estado.className = 'form-hint mt-2' + (tipo ? ' text-' + tipo : '');
    }

    function marcar(punto, completar) {
      if (!marcador) {
        marcador = L.marker(punto, { draggable: true }).addTo(mapa);
        marcador.on('dragend', () => marcar(marcador.getLatLng(), true));
      } else {
        marcador.setLatLng(punto);
      }
      lat.value = punto.lat.toFixed(6);
      lng.value = punto.lng.toFixed(6);
      avisar('Ubicación marcada. Si no es exacta, arrastra el marcador o haz clic en otro punto.', 'success');
      if (completar) completarDireccion(punto);
    }

    // Si la dirección está vacía, se sugiere la del punto marcado (el usuario puede corregirla).
    async function completarDireccion(punto) {
      if (!direccion || direccion.value.trim()) return;
      try {
        const r = await fetch(`${NOMINATIM}/reverse?format=jsonv2&accept-language=es&zoom=18&lat=${punto.lat}&lon=${punto.lng}`);
        if (!r.ok) return;
        const d = await r.json();
        const a = d.address || {};
        const calle = [a.road, a.house_number].filter(Boolean).join(' ');
        const zona = a.neighbourhood || a.suburb || '';
        const texto = [calle, zona].filter(Boolean).join(', ');
        if (texto && !direccion.value.trim()) direccion.value = texto;
        const lugar = a.city || a.town || a.village;
        if (ciudad && lugar && !ciudad.value.trim()) ciudad.value = lugar;
      } catch { /* sin conexión: el usuario escribe la dirección */ }
    }

    async function buscar() {
      const texto = buscarTexto.value.trim();
      if (!texto) { buscarTexto.focus(); return; }
      avisar('Buscando…');
      const consulta = ciudad && ciudad.value.trim() ? `${texto}, ${ciudad.value.trim()}` : texto;
      try {
        const r = await fetch(`${NOMINATIM}/search?format=jsonv2&limit=1&countrycodes=bo&accept-language=es&q=${encodeURIComponent(consulta)}`);
        const lista = r.ok ? await r.json() : [];
        if (!lista.length) {
          avisar('No encontramos ese lugar. Acerca el mapa y haz clic donde está la sucursal.', 'warning');
          return;
        }
        const punto = L.latLng(parseFloat(lista[0].lat), parseFloat(lista[0].lon));
        mapa.setView(punto, 17);
        marcar(punto, false);
        if (direccion && !direccion.value.trim()) direccion.value = texto;
      } catch {
        avisar('No se pudo buscar en este momento. Haz clic en el mapa para marcar la sucursal.', 'warning');
      }
    }

    if (marcada) marcar(L.latLng(la, lo), false);
    else avisar('Busca la dirección o haz clic en el mapa donde está la sucursal.');

    mapa.on('click', (e) => marcar(e.latlng, true));
    buscarBoton.addEventListener('click', buscar);
    buscarTexto.addEventListener('keydown', (e) => {
      if (e.key === 'Enter') { e.preventDefault(); buscar(); }
    });

    // Sin punto marcado no se envía: se avisa junto al mapa.
    raiz.closest('form')?.addEventListener('submit', (e) => {
      if (lat.value && lng.value) return;
      e.preventDefault();
      avisar('Falta marcar la ubicación de la sucursal en el mapa.', 'danger');
      raiz.scrollIntoView({ behavior: 'smooth', block: 'center' });
    });

    setTimeout(() => mapa.invalidateSize(), 200);
  }

  function mapaSucursales(contenedor) {
    const sucursales = JSON.parse(contenedor.dataset.mapaSucursales || '[]');
    const mapa = L.map(contenedor).setView(SANTA_CRUZ, 12);
    capaBase().addTo(mapa);
    const puntos = [];
    for (const s of sucursales) {
      const popup = document.createElement('div');
      const nombre = document.createElement('div');
      nombre.className = 'fw-bold';
      nombre.textContent = s.nombre;
      const detalle = document.createElement('div');
      detalle.textContent = `${s.cadena} · ${s.direccion}`;
      const estado = document.createElement('div');
      estado.className = s.abierta ? 'text-success' : 'text-secondary';
      estado.textContent = s.abierta ? 'Abierta ahora' : 'Cerrada ahora';
      popup.append(nombre, detalle, estado);
      if (s.editar) {
        const enlace = document.createElement('a');
        enlace.href = s.editar;
        enlace.textContent = 'Ver o editar';
        popup.append(enlace);
      }
      L.marker([s.latitud, s.longitud]).addTo(mapa).bindPopup(popup);
      puntos.push([s.latitud, s.longitud]);
    }
    if (puntos.length) mapa.fitBounds(puntos, { padding: [40, 40], maxZoom: 15 });
    setTimeout(() => mapa.invalidateSize(), 200);
  }

  document.querySelectorAll('[data-selector-ubicacion]').forEach(selectorUbicacion);
  document.querySelectorAll('[data-mapa-sucursales]').forEach(mapaSucursales);
})();
