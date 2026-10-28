namespace GestionGimnasios.Web.Services;

public static class Registro
{
    /// <summary>Servicios de negocio compartidos por la API y el panel.</summary>
    public static IServiceCollection AddServiciosDeNegocio(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(configuration.GetSection("Correo").Get<OpcionesCorreo>() ?? new OpcionesCorreo());
        services.AddScoped<Alcances>();
        services.AddScoped<Almacenamiento>();
        services.AddScoped<DatosPersonales>();
        services.AddScoped<CuentaService>();
        services.AddScoped<UsuariosService>();
        services.AddScoped<RolesService>();
        services.AddScoped<SedesService>();
        services.AddScoped<MaquinasService>();
        services.AddScoped<ActividadesService>();
        services.AddScoped<EjerciciosService>();
        services.AddScoped<RutinasService>();
        services.AddScoped<EntrenamientoService>();
        services.AddScoped<SuscripcionesService>();
        services.AddScoped<NotificacionesService>();
        services.AddScoped<ReservasService>();
        services.AddScoped<ListaEsperaService>();
        services.AddScoped<AccesosService>();
        services.AddScoped<BeneficiosService>();
        services.AddScoped<PersonalService>();
        services.AddScoped<ReportesService>();
        services.AddScoped<MensajesService>();
        services.AddScoped<VisitasService>();
        if (configuration.GetValue("TareasProgramadas:Habilitadas", true))
            services.AddHostedService<TareasProgramadas>();
        return services;
    }
}
