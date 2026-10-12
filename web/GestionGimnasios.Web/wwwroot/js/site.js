// Confirmación para acciones destructivas: <form data-confirmar="¿Seguro?">
document.addEventListener('submit', (e) => {
  const mensaje = e.target.dataset?.confirmar;
  if (mensaje && !window.confirm(mensaje)) e.preventDefault();
});
