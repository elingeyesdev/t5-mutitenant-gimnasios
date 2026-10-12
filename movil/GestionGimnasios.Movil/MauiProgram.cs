using Microsoft.Extensions.Logging;

namespace GestionGimnasios.Movil;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		// Fechas y textos del sistema en español de Bolivia, sin importar el idioma del celular.
		var cultura = new System.Globalization.CultureInfo("es-BO");
		System.Globalization.CultureInfo.DefaultThreadCurrentCulture = cultura;
		System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = cultura;

		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
