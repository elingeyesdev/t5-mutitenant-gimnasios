using System.Globalization;
using System.Text;
using System.Text.Json;
using GestionGimnasios.Web.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GestionGimnasios.Web.Data.Seed;

/// <summary>
/// Datos de prueba equivalentes al seeder del proyecto base (10 cadenas, 30 sucursales,
/// 206 usuarios, 85 ejercicios, máquinas, actividades y horarios). No borra nada: solo
/// carga datos si la base está vacía. Agrega planes y suscripciones, que el proyecto base
/// no creaba y sin los que ningún cliente podía reservar.
/// </summary>
public static class SeedData
{
    public const string PasswordPrueba = "Aaron123*";

    private static readonly string[] NombresCadena = ["Premier", "Megatlon", "Smart Fit", "Reyes", "Sport", "Bio Center", "Olimpia", "Fit Bull", "VIP", "Body Masters"];

    private static readonly (string Zona, string Direccion, decimal Lat, decimal Lng)[] Zonas =
    [
        ("Equipetrol", "Av. San Martin, 3er Anillo Interno", -17.7667m, -63.1970m),
        ("Centro", "Calle 24 de Septiembre 150", -17.7833m, -63.1821m),
        ("Urubo", "Av. Principal del Urubo", -17.7512m, -63.2201m),
        ("Plan 3000", "Rotonda del Plan 3000", -17.8335m, -63.1415m),
        ("Villa 1ro de Mayo", "Plaza Principal", -17.8011m, -63.1366m),
        ("Santos Dumont", "Av. Santos Dumont", -17.8172m, -63.1855m),
        ("Banzer", "Av. Banzer", -17.7441m, -63.1702m),
        ("Doble Via La Guardia", "Doble Via a La Guardia", -17.8089m, -63.2081m),
        ("Mutualista", "Av. Mutualista", -17.7655m, -63.1598m),
        ("Las Palmas", "Barrio Las Palmas", -17.7951m, -63.2005m),
    ];

    private static readonly (string Base, string Marca, int Cantidad, string Categoria)[] MaquinasBase =
    [
        ("Cinta de Correr", "Precor", 2, CategoriasMaquina.Cardio),
        ("Bicicleta Estatica", "Life Fitness", 2, CategoriasMaquina.Cardio),
        ("Eliptica", "Technogym", 1, CategoriasMaquina.Cardio),
        ("Press de Banca", "Hammer Strength", 1, CategoriasMaquina.TrenSuperior),
        ("Prensa de Piernas", "Cybex", 1, CategoriasMaquina.TrenInferior),
        ("Polea Cruzada", "Technogym", 1, CategoriasMaquina.Multiestacion),
        ("Smith Machine", "Precor", 1, CategoriasMaquina.Multiestacion),
        ("Hack Squat", "Hammer Strength", 1, CategoriasMaquina.TrenInferior),
        ("Remo Sentado", "Nautilus", 1, CategoriasMaquina.Espalda),
        ("Lat Pulldown", "Matrix", 1, CategoriasMaquina.Espalda),
    ];

    private static readonly (string Base, string Marca, int Cantidad, string Categoria)[] MaquinasExtra =
    [
        ("Extension de Cuadriceps", "Star Trac", 1, CategoriasMaquina.TrenInferior),
        ("Curl de Piernas", "Cybex", 1, CategoriasMaquina.TrenInferior),
        ("Pec Deck", "Life Fitness", 1, CategoriasMaquina.TrenSuperior),
        ("Press Militar", "Cybex", 1, CategoriasMaquina.TrenSuperior),
        ("Abductor Aductor", "Hoist", 1, CategoriasMaquina.TrenInferior),
    ];

    private static readonly string[] Nombres =
    [
        "Carlos","Maria","Jose","Ana","Luis","Sofia","Jorge","Lucia","Miguel","Laura",
        "Pedro","Carmen","Raul","Elena","Fernando","Paula","Diego","Marta","Javier","Sara",
        "Andres","Beatriz","Alejandro","Isabel","Manuel","Cristina","Daniel","Teresa","David","Rosa",
        "Adrian","Julia","Mario","Patricia","Alberto","Alba","Victor","Silvia","Marcos","Natalia",
        "Oscar","Irene","Ruben","Lorena","Ivan","Angela","Gabriel","Belen","Hugo","Rocio",
    ];

    private static readonly string[] Apellidos =
    [
        "Gomez","Rodriguez","Fernandez","Lopez","Martinez","Sanchez","Perez","Martin","Garcia","Ruiz",
        "Diaz","Suarez","Romero","Alvarez","Torres","Navarro","Gutierrez","Molina","Blanco","Castro",
        "Ortiz","Rubio","Marin","Sanz","Iglesias","Vazquez","Ramos","Gil","Serrano","Mendez",
        "Cabrera","Medina","Rojas","Flores","Vargas","Rios","Castillo","Pena","Guzman","Leon",
        "Herrera","Cortes","Mora","Arias","Vega","Cruz","Mendoza","Soto","Campos","Delgado",
    ];

    private static readonly (string Nombre, string Descripcion, bool Libre, int? Duracion)[] ServiciosLibres =
    [
        ("Musculacion Libre", "Acceso libre a maquinas", true, null),
        ("Zona Cardio", "Acceso a cintas y elipticas", true, null),
        ("Open Box", "Uso libre funcional", true, null),
        ("Area de Estiramiento", "Elongacion y movilidad", true, null),
    ];

    private static readonly (string Nombre, string Descripcion, bool Libre, int? Duracion)[] ServiciosConHorario =
    [
        ("Zumba", "Baile fitness", false, 60),
        ("Yoga", "Yoga fluido", false, 60),
        ("Pilates", "Pilates core", false, 45),
        ("Spinning", "Bicicleta estatica", false, 45),
        ("CrossFit", "Entrenamiento funcional", false, 60),
        ("Body Pump", "Clase con barras", false, 60),
        ("HIIT", "Intervalos alta intensidad", false, 30),
    ];

    public static async Task CargarAsync(AppDbContext db, string rutaContenido, ILogger logger)
    {
        if (await db.Roles.AnyAsync())
        {
            logger.LogInformation("Datos de prueba ya cargados; no se modifica la base.");
            return;
        }

        var azar = new Random(2026);
        await using var tx = await db.Database.BeginTransactionAsync();

        // ── Roles (mismos niveles que el proyecto base) ─────────────────
        var roles = new[]
        {
            new Rol { Nombre = Roles.SuperAdmin, Nivel = 10, Descripcion = "Administrador Global", EsDeSistema = true },
            new Rol { Nombre = Roles.Gerente, Nivel = 5, Descripcion = "Gerente de Marca", EsDeSistema = true },
            new Rol { Nombre = Roles.Recepcionista, Nivel = 4, Descripcion = "Recepcionista", EsDeSistema = true },
            new Rol { Nombre = Roles.Entrenador, Nivel = 3, Descripcion = "Entrenador Personal", EsDeSistema = true },
            new Rol { Nombre = Roles.Nutricionista, Nivel = 3, Descripcion = "Nutricionista", EsDeSistema = true },
            new Rol { Nombre = Roles.Instructor, Nivel = 2, Descripcion = "Instructor", EsDeSistema = true },
            new Rol { Nombre = Roles.Coordinador, Nivel = 2, Descripcion = "Coordinador de Sede" },
            new Rol { Nombre = Roles.Cliente, Nivel = 1, Descripcion = "Cliente", EsDeSistema = true },
            new Rol { Nombre = Roles.Limpieza, Nivel = 1, Descripcion = "Limpieza" },
        };
        db.Roles.AddRange(roles);
        await db.SaveChangesAsync();
        Rol RolPor(string nombre) => roles.First(r => r.Nombre == nombre);

        // ── Cadenas, sucursales, ubicaciones, máquinas y horarios ───────
        var cadenas = NombresCadena.Select(n => new Cadena { Nombre = n, Descripcion = "Cadena matriz" }).ToList();
        db.Cadenas.AddRange(cadenas);
        await db.SaveChangesAsync();

        var sucursales = new List<Sucursal>();
        var indice = 0;
        foreach (var cadena in cadenas)
        {
            for (var i = 0; i < 3; i++)
            {
                var zona = Zonas[(indice + i) % Zonas.Length];
                var sucursal = new Sucursal
                {
                    CadenaId = cadena.Id,
                    Nombre = $"{cadena.Nombre} {zona.Zona}",
                    Descripcion = $"Sucursal {zona.Zona}",
                    CapacidadMaxima = 100 + azar.Next(200),
                    Ubicacion = new UbicacionSucursal
                    {
                        Direccion = zona.Direccion,
                        Ciudad = "Santa Cruz",
                        Latitud = zona.Lat + (decimal)(azar.NextDouble() * 0.005 - 0.0025),
                        Longitud = zona.Lng + (decimal)(azar.NextDouble() * 0.005 - 0.0025),
                    },
                };

                var extras = azar.Next(MaquinasExtra.Length);
                foreach (var m in MaquinasBase.Concat(MaquinasExtra.Take(extras)))
                    for (var u = 0; u < m.Cantidad; u++)
                        sucursal.Maquinas.Add(new Maquina
                        {
                            CadenaId = cadena.Id,
                            Nombre = $"{m.Base} {m.Marca}{(m.Cantidad > 1 ? $" {"ABCD"[u]}" : "")}",
                            Estado = azar.NextDouble() < 0.80 ? EstadosMaquina.Disponible : EstadosMaquina.Mantenimiento,
                            Categoria = m.Categoria,
                        });

                foreach (var dia in Dias.Cortos)
                {
                    var (abre, cierra, feriado) = dia switch
                    {
                        "SAB" => (new TimeOnly(7, 0), new TimeOnly(18, 0), false),
                        // En el proyecto base el índice avanza de 3 en 3, así que todas las sucursales cierran el domingo.
                        "DOM" => (new TimeOnly(8, 0), new TimeOnly(13, 0), indice % 3 == 0),
                        _ => (new TimeOnly(6, 0), new TimeOnly(22, 0), false),
                    };
                    sucursal.Horarios.Add(new HorarioSucursal { DiaSemana = dia, Abre = abre, Cierra = cierra, EsFeriado = feriado });
                }
                sucursales.Add(sucursal);
            }
            indice += 3;
        }
        db.Sucursales.AddRange(sucursales);
        await db.SaveChangesAsync();

        // ── Usuarios (misma secuencia de nombres y correos que el proyecto base) ─
        var hash = new PasswordHasher<Usuario>().HashPassword(new Usuario(), PasswordPrueba);
        var correos = new HashSet<string>();
        var n = 0;
        (string Nombre, string Apellido) Siguiente()
        {
            var r = (Nombres[n % Nombres.Length], Apellidos[(n * 7 + 3) % Apellidos.Length]);
            n++;
            return r;
        }
        string Correo(string nombre, string apellido)
        {
            var baseCorreo = $"{Limpiar(nombre)}.{Limpiar(apellido)}";
            var correo = $"{baseCorreo}@gmail.com";
            for (var c = 1; correos.Contains(correo); c++) correo = $"{baseCorreo}{c}@gmail.com";
            correos.Add(correo);
            return correo;
        }
        Usuario Crear(string nombre, string apellido, string rol, int? cadenaId, int? sucursalId, bool conTelefono, string? correo = null)
        {
            var u = new Usuario
            {
                Email = correo ?? Correo(nombre, apellido),
                PasswordHash = hash,
                Perfil = new PerfilUsuario
                {
                    Nombres = nombre,
                    Apellidos = apellido,
                    Telefono = conTelefono ? $"+5917{azar.Next(1000000, 9999999)}" : null,
                    Ci = azar.Next(1000000, 9999999).ToString(),
                    Genero = n % 2 == 0 ? "Masculino" : "Femenino",
                },
            };
            if (rol == Roles.Cliente)
            {
                u.Roles.Add(new UsuarioRol { RolId = RolPor(rol).Id });
                if (cadenaId is not null)
                    u.Membresias.Add(new Membresia { CadenaId = cadenaId.Value, SucursalId = sucursalId });
            }
            else
            {
                u.Roles.Add(new UsuarioRol { RolId = RolPor(rol).Id, CadenaId = cadenaId, SucursalId = sucursalId });
            }
            db.Usuarios.Add(u);
            return u;
        }

        Crear("Super", "Admin", Roles.SuperAdmin, null, null, false, "admin@gymsync.com");
        foreach (var cadena in cadenas)
        {
            var (nom, ape) = Siguiente();
            Crear(nom, ape, Roles.Gerente, cadena.Id, null, true);
        }

        var instructores = new Dictionary<Sucursal, List<Usuario>>();
        foreach (var s in sucursales)
        {
            var (rn, ra) = Siguiente();
            Crear(rn, ra, Roles.Recepcionista, s.CadenaId, s.Id, false);
            for (var j = 0; j < 2; j++)
            {
                var (en, ea) = Siguiente();
                Crear(en, ea, Roles.Entrenador, s.CadenaId, s.Id, true);
            }
            instructores[s] = [];
            for (var j = 0; j < 2; j++)
            {
                var (inn, ina) = Siguiente();
                instructores[s].Add(Crear(inn, ina, Roles.Instructor, s.CadenaId, s.Id, true));
            }
        }

        var clientes = new List<(Usuario Usuario, Sucursal Sucursal)>();
        for (var i = 0; i < 45; i++)
        {
            var s = sucursales[i % sucursales.Count];
            var (cn, ca) = Siguiente();
            clientes.Add((Crear(cn, ca, Roles.Cliente, s.CadenaId, s.Id, false), s));
        }
        await db.SaveChangesAsync();

        // ── Actividades y horarios de clases ────────────────────────────
        string[][] gruposDias = [["LUNES", "MIERCOLES", "VIERNES"], ["MARTES", "JUEVES", "SABADO"]];
        int[] horasInicio = [7, 9, 14, 18];
        foreach (var s in sucursales)
        {
            var libre = ServiciosLibres[azar.Next(ServiciosLibres.Length)];
            var conHorario = ServiciosConHorario[azar.Next(ServiciosConHorario.Length)];
            var otros = ServiciosConHorario.Where(x => x.Nombre != conHorario.Nombre).ToArray();
            var definiciones = new[] { libre, conHorario, otros[azar.Next(otros.Length)] };

            for (var si = 0; si < definiciones.Length; si++)
            {
                var d = definiciones[si];
                var actividad = new Actividad
                {
                    CadenaId = s.CadenaId,
                    SucursalId = s.Id,
                    Nombre = d.Nombre,
                    Descripcion = d.Descripcion,
                    DuracionMin = d.Duracion,
                    AccesoLibre = d.Libre,
                };
                if (!d.Libre && instructores[s].Count > 0)
                {
                    var inicio = new TimeOnly(horasInicio[si % horasInicio.Length], 0);
                    foreach (var dia in gruposDias[si % gruposDias.Length])
                        actividad.Horarios.Add(new HorarioActividad
                        {
                            CadenaId = s.CadenaId,
                            InstructorId = instructores[s][si % instructores[s].Count].Id,
                            DiaSemana = dia,
                            Inicio = inicio,
                            Fin = inicio.AddMinutes(d.Duracion!.Value),
                            CupoMaximo = 15,
                        });
                }
                db.Actividades.Add(actividad);
            }
        }

        // ── Catálogo de ejercicios (85, igual que el proyecto base) ─────
        var json = await File.ReadAllTextAsync(Path.Combine(rutaContenido, "Data", "Seed", "ejercicios.json"));
        var ejercicios = JsonSerializer.Deserialize<List<Ejercicio>>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        db.Ejercicios.AddRange(ejercicios);

        // ── Planes y suscripciones (no existían en los datos del proyecto base) ─
        var hoy = DateOnly.FromDateTime(DateTime.Today);
        var planes = new List<PlanSuscripcion>();
        foreach (var c in cadenas)
        {
            planes.Add(new PlanSuscripcion { CadenaId = c.Id, Nombre = "Plan Básico", Descripcion = "Acceso a la sucursal principal", PrecioMensual = 150, MaxSucursales = 1, Beneficios = ["Sala de musculación", "Zona cardio"] });
            planes.Add(new PlanSuscripcion { CadenaId = c.Id, Nombre = "Plan Full", Descripcion = "Acceso a todas las sucursales de la cadena y clases", PrecioMensual = 250, Beneficios = ["Todas las sucursales", "Clases grupales", "Asesoría de entrenador"] });
        }
        db.Planes.AddRange(planes);
        await db.SaveChangesAsync();

        for (var i = 0; i < clientes.Count; i++)
        {
            if (i % 3 == 2) continue; // un tercio de los clientes queda sin suscripción, para probar ese caso
            var (cliente, s) = clientes[i];
            var plan = planes.First(p => p.CadenaId == s.CadenaId && p.Nombre == (i % 2 == 0 ? "Plan Full" : "Plan Básico"));
            var suscripcion = new Suscripcion
            {
                UsuarioId = cliente.Id,
                PlanId = plan.Id,
                CadenaId = s.CadenaId,
                SucursalPrincipalId = s.Id,
                Inicio = hoy.AddDays(-10),
                Fin = hoy.AddDays(20),
            };
            suscripcion.Pagos.Add(new PagoSuscripcion { Monto = plan.PrecioMensual, Metodo = "EFECTIVO" });
            db.Suscripciones.Add(suscripcion);
        }

        db.Plantillas.AddRange(
            new PlantillaNotificacion { Tipo = "RECORDATORIO_RESERVA", Titulo = "Tu clase empieza pronto", Cuerpo = "Tu reserva de {actividad} empieza a las {hora}." },
            new PlantillaNotificacion { Tipo = "RESERVA_CANCELADA", Titulo = "Reserva cancelada", Cuerpo = "Tu reserva para {actividad} ha sido cancelada." },
            new PlantillaNotificacion { Tipo = "ALERTA_INACTIVIDAD", Titulo = "Alumnos inactivos", Cuerpo = "Tienes {cantidad} alumno(s) sin entrenar según su meta." });

        await db.SaveChangesAsync();
        await tx.CommitAsync();

        logger.LogInformation(
            "Datos de prueba cargados: {Cadenas} cadenas, {Sucursales} sucursales, {Usuarios} usuarios, {Ejercicios} ejercicios, {Maquinas} máquinas, {Actividades} actividades, {Horarios} horarios.",
            await db.Cadenas.CountAsync(), await db.Sucursales.CountAsync(), await db.Usuarios.CountAsync(),
            await db.Ejercicios.CountAsync(), await db.Maquinas.IgnoreQueryFilters().CountAsync(),
            await db.Actividades.IgnoreQueryFilters().CountAsync(), await db.HorariosActividad.IgnoreQueryFilters().CountAsync());
    }

    /// <summary>Igual que cleanString del proyecto base: minúsculas, sin tildes ni símbolos.</summary>
    private static string Limpiar(string texto)
    {
        var sb = new StringBuilder();
        foreach (var c in texto.ToLowerInvariant().Normalize(NormalizationForm.FormD))
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark && char.IsAsciiLetterOrDigit(c))
                sb.Append(c);
        return sb.ToString();
    }
}
