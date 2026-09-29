using ImportadorMarcaciones.DataAccess;
using ImportadorMarcaciones.Services;
using Microsoft.Data.SqlClient;

try
{
    var rutaEntrada = ObtenerArgumento(args, "--archivo")
        ?? Path.Combine(AppContext.BaseDirectory, "Data", "marcaciones.csv");

    rutaEntrada = Path.GetFullPath(rutaEntrada);

    var rutaRechazados = ObtenerArgumento(args, "--rechazados")
        ?? Path.Combine(
            Path.GetDirectoryName(rutaEntrada) ?? AppContext.BaseDirectory,
            "rechazado.csv");

    rutaRechazados = Path.GetFullPath(rutaRechazados);

    if (!File.Exists(rutaEntrada))
        throw new FileNotFoundException("No se encontró el archivo CSV de entrada.", rutaEntrada);

    var connectionString = ConstruirConnectionStringDesdeVariablesDeEntorno();

    await using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync();

    var repository = new SqlMarcacionRepository(connection);
    var trabajadores = await repository.ObtenerTrabajadoresPorRutAsync();

    var importer = new MarcacionCsvImporter(repository, trabajadores);
    var resultado = await importer.ImportarAsync(rutaEntrada, rutaRechazados);

    Console.WriteLine("Importación finalizada correctamente.");
    Console.WriteLine($"Archivo de entrada : {rutaEntrada}");
    Console.WriteLine($"Filas procesadas    : {resultado.Procesados}");
    Console.WriteLine($"Filas insertadas    : {resultado.Insertados}");
    Console.WriteLine($"Filas rechazadas    : {resultado.Rechazados}");
    Console.WriteLine($"Archivo de rechazo  : {rutaRechazados}");

    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error fatal: {ex.Message}");
    return 1;
}

static string? ObtenerArgumento(string[] args, string nombre)
{
    for (var i = 0; i < args.Length; i++)
    {
        if (args[i].Equals(nombre, StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            return args[i + 1];

        var prefijo = nombre + "=";
        if (args[i].StartsWith(prefijo, StringComparison.OrdinalIgnoreCase))
            return args[i][prefijo.Length..];
    }

    return null;
}

static string ConstruirConnectionStringDesdeVariablesDeEntorno()
{
    var servidor = ObtenerVariableObligatoria("DB_SERVER");
    var baseDatos = ObtenerVariableObligatoria("DB_DATABASE");
    var usarSeguridadIntegrada = ObtenerBooleano("DB_INTEGRATED_SECURITY", false);

    var builder = new SqlConnectionStringBuilder
    {
        DataSource = servidor,
        InitialCatalog = baseDatos,
        Encrypt = ObtenerBooleano("DB_ENCRYPT", true),
        TrustServerCertificate = ObtenerBooleano("DB_TRUST_SERVER_CERTIFICATE", true),
        ConnectTimeout = 15,
        ApplicationName = "ImportadorMarcacionesNet8"
    };

    if (usarSeguridadIntegrada)
    {
        builder.IntegratedSecurity = true;
    }
    else
    {
        builder.UserID = ObtenerVariableObligatoria("DB_USER");
        builder.Password = ObtenerVariableObligatoria("DB_PASSWORD");
    }

    return builder.ConnectionString;
}

static string ObtenerVariableObligatoria(string nombre)
{
    var valor = Environment.GetEnvironmentVariable(nombre);

    if (string.IsNullOrWhiteSpace(valor))
        throw new InvalidOperationException($"Falta definir la variable de entorno {nombre}.");

    return valor;
}

static bool ObtenerBooleano(string nombre, bool valorPredeterminado)
{
    var valor = Environment.GetEnvironmentVariable(nombre);
    return bool.TryParse(valor, out var resultado)
        ? resultado
        : valorPredeterminado;
}
