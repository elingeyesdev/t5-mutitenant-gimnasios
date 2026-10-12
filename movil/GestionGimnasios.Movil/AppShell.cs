using GestionGimnasios.Movil.Paginas.Cliente;
using GestionGimnasios.Movil.Paginas.Comun;
using GestionGimnasios.Movil.Paginas.Personal;
using GestionGimnasios.Movil.Servicios;

namespace GestionGimnasios.Movil;

/// <summary>
/// Navegación principal. Las pestañas se arman según el rol del usuario, igual que la app original
/// separaba la experiencia del cliente y la del personal.
/// </summary>
public class AppShell : Shell
{
    public AppShell()
    {
        Title = "Gestión Gimnasios";
        var barra = new TabBar();
        foreach (var (titulo, icono, pagina) in Pestanas())
            barra.Items.Add(new ShellContent { Title = titulo, Icon = icono, ContentTemplate = new DataTemplate(pagina), Route = pagina.Name });
        Items.Add(barra);
    }

    private static IEnumerable<(string, string, Type)> Pestanas()
    {
        var rol = Sesion.Rol;
        var nivel = Sesion.Nivel;
        if (rol == "CLIENTE")
        {
            yield return ("Inicio", "tab_inicio.png", typeof(InicioClientePage));
            yield return ("Gimnasios", "tab_gimnasios.png", typeof(SucursalesPage));
            yield return ("Reservas", "tab_reservas.png", typeof(MisReservasPage));
            yield return ("Entrenar", "tab_entrenar.png", typeof(MiRutinaPage));
        }
        else if (nivel is >= 4 and < 10)
        {
            // Recepcionista y gerente: control de acceso en la puerta y resumen de la sede.
            yield return ("Escáner", "tab_escaner.png", typeof(EscanerPage));
            yield return ("Resumen", "tab_inicio.png", typeof(ResumenPage));
            yield return ("Mensajes", "tab_mensajes.png", typeof(ConversacionesPage));
        }
        else if (nivel >= 10)
        {
            yield return ("Resumen", "tab_inicio.png", typeof(ResumenPage));
            yield return ("Mensajes", "tab_mensajes.png", typeof(ConversacionesPage));
        }
        else if (nivel == 3)
        {
            // Entrenador y nutricionista: asesorías, alumnos y citas.
            yield return ("Panel", "tab_inicio.png", typeof(PanelAsesorPage));
            if (rol == "NUTRICIONISTA") yield return ("Citas", "tab_reservas.png", typeof(CitasPage));
            yield return ("Mensajes", "tab_mensajes.png", typeof(ConversacionesPage));
        }
        else if (nivel == 2)
        {
            // Instructor y coordinador: clases del día y asistencia.
            yield return ("Agenda", "tab_reservas.png", typeof(AgendaPage));
            yield return ("Mensajes", "tab_mensajes.png", typeof(ConversacionesPage));
        }
        yield return ("Más", "tab_mas.png", typeof(MasPage));
    }
}
