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
