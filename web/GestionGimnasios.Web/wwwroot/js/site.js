// Confirmación para acciones destructivas: <form data-confirmar="¿Seguro?">
document.addEventListener('submit', (e) => {
  const mensaje = e.target.dataset?.confirmar;
  if (mensaje && !window.confirm(mensaje)) e.preventDefault();
});

// Cambio de tema claro u oscuro (HU-52). La preferencia se guarda en el navegador.
document.addEventListener('click', (e) => {
  if (!e.target.closest('[data-cambiar-tema]')) return;
  const raiz = document.documentElement;
  const tema = raiz.getAttribute('data-bs-theme') === 'dark' ? 'light' : 'dark';
  raiz.setAttribute('data-bs-theme', tema);
  try { localStorage.setItem('tema', tema); } catch { }
});

// Los reportes se imprimen siempre en tema claro, aunque el panel esté en oscuro.
let temaAntesDeImprimir = null;
window.addEventListener('beforeprint', () => {
  temaAntesDeImprimir = document.documentElement.getAttribute('data-bs-theme');
  document.documentElement.setAttribute('data-bs-theme', 'light');
});
window.addEventListener('afterprint', () => {
  if (temaAntesDeImprimir) document.documentElement.setAttribute('data-bs-theme', temaAntesDeImprimir);
});
