using ImportadorMarcaciones.DataAccess;
using ImportadorMarcaciones.Models;
using ImportadorMarcaciones.Utils;
using Microsoft.Data.SqlClient;
using System.Globalization;
using System.Text;

namespace ImportadorMarcaciones.Services;

public sealed class MarcacionCsvImporter
{
    private const string FormatoFecha = "yyyy-MM-dd HH:mm:ss";

    private readonly SqlMarcacionRepository _repository;
    private readonly IReadOnlyDictionary<string, TrabajadorDb> _trabajadoresPorRut;

    public MarcacionCsvImporter(
        SqlMarcacionRepository repository,
        IReadOnlyDictionary<string, TrabajadorDb> trabajadoresPorRut)
    {
        _repository = repository;
        _trabajadoresPorRut = trabajadoresPorRut;
    }

    public async Task<ImportacionResultado> ImportarAsync(
        string rutaEntrada,
        string rutaRechazados,
        CancellationToken cancellationToken = default)
    {
        var rechazados = new List<RechazoCsv>();
        var marcacionesProcesadasEnArchivo = new HashSet<string>(StringComparer.Ordinal);

        var procesados = 0;
        var insertados = 0;
        var numeroLinea = 0;

        using var reader = new StreamReader(
            rutaEntrada,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true);

        var encabezado = await reader.ReadLineAsync(cancellationToken);
        numeroLinea++;
        ValidarEncabezado(encabezado);

        string? linea;
        while ((linea = await reader.ReadLineAsync(cancellationToken)) is not null)
        {
            numeroLinea++;

            if (string.IsNullOrWhiteSpace(linea))
                continue;

            procesados++;
            var campos = CsvSemicolonParser.ParsearLinea(linea);

            if (!TryCrearMarcacionBase(campos, numeroLinea, out var datosBase, out var rechazo))
            {
                rechazados.Add(rechazo!);
                continue;
            }

            if (!_trabajadoresPorRut.TryGetValue(datosBase!.Rut, out var trabajador))
            {
                rechazados.Add(CrearRechazo(
                    numeroLinea,
                    campos,
                    $"El RUT {datosBase.Rut} no existe en dbo.Trabajador."));
                continue;
            }

            if (!trabajador.Activo)
            {
                rechazados.Add(CrearRechazo(
                    numeroLinea,
                    campos,
                    $"El trabajador con RUT {datosBase.Rut} está inactivo."));
                continue;
            }

            var marcacion = new MarcacionImportar(
                trabajador.TrabajadorId,
                datosBase.Rut,
                datosBase.FechaHora,
                datosBase.Tipo,
                datosBase.Origen);

            var clave = CrearClaveDuplicado(marcacion);

            if (marcacionesProcesadasEnArchivo.Contains(clave))
            {
                rechazados.Add(CrearRechazo(
                    numeroLinea,
                    campos,
                    "Marcación duplicada dentro del archivo CSV."));
                continue;
            }

            try
            {
                if (await _repository.ExisteMarcacionAsync(
                        marcacion.TrabajadorId,
                        marcacion.FechaHora,
                        marcacion.Tipo,
                        cancellationToken))
                {
                    marcacionesProcesadasEnArchivo.Add(clave);

                    rechazados.Add(CrearRechazo(
                        numeroLinea,
                        campos,
                        "La marcación ya existe en la base de datos."));
                    continue;
                }

                await _repository.InsertarMarcacionAsync(marcacion, cancellationToken);
                marcacionesProcesadasEnArchivo.Add(clave);
                insertados++;
            }
            catch (SqlException ex)
            {
                rechazados.Add(CrearRechazo(
                    numeroLinea,
                    campos,
                    $"Error SQL {ex.Number}: {ex.Message}"));
            }
        }

        await EscribirRechazadosAsync(rutaRechazados, rechazados, cancellationToken);

        return new ImportacionResultado(
            procesados,
            insertados,
            rechazados.Count);
    }

    private static bool TryCrearMarcacionBase(
        string[] campos,
        int numeroLinea,
        out DatosBase? datos,
        out RechazoCsv? rechazo)
    {
        datos = null;
        rechazo = null;

        if (campos.Length != 4)
        {
            rechazo = CrearRechazo(
                numeroLinea,
                campos,
                $"Cantidad de columnas inválida. Se esperaban 4 y se recibieron {campos.Length}.");
            return false;
        }

        var rutTexto = campos[0].Trim();
        var fechaTexto = campos[1].Trim();
        var tipo = campos[2].Trim().ToUpperInvariant();
        var origen = campos[3].Trim();

        if (!RutHelper.TryNormalizarYValidar(rutTexto, out var rutCanonico, out var errorRut))
        {
            rechazo = CrearRechazo(numeroLinea, campos, errorRut);
            return false;
        }

        if (!DateTime.TryParseExact(
                fechaTexto,
                FormatoFecha,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var fechaHora))
        {
            rechazo = CrearRechazo(
                numeroLinea,
                campos,
                $"fecha_hora inválida. Se espera el formato {FormatoFecha}.");
            return false;
        }

        if (tipo is not ("E" or "S"))
        {
            rechazo = CrearRechazo(
                numeroLinea,
                campos,
                "tipo inválido. Solo se permite E (entrada) o S (salida).");
            return false;
        }

        if (string.IsNullOrWhiteSpace(origen))
        {
            rechazo = CrearRechazo(numeroLinea, campos, "origen es obligatorio.");
            return false;
        }

        if (origen.Length > 20)
        {
            rechazo = CrearRechazo(
                numeroLinea,
                campos,
                "origen no puede superar los 20 caracteres permitidos por dbo.Marcacion.Origen.");
            return false;
        }

        datos = new DatosBase(rutCanonico, fechaHora, tipo, origen);
        return true;
    }

    private static void ValidarEncabezado(string? encabezado)
    {
        if (string.IsNullOrWhiteSpace(encabezado))
            throw new InvalidDataException("El CSV está vacío o no tiene encabezado.");

        var columnas = CsvSemicolonParser.ParsearLinea(encabezado)
            .Select(c => c.Trim())
            .ToArray();

        var esperadas = new[] { "rut", "fecha_hora", "tipo", "origen" };

        if (columnas.Length != esperadas.Length ||
            !columnas.SequenceEqual(esperadas, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(
                "Encabezado inválido. Se esperaba: rut;fecha_hora;tipo;origen");
        }
    }

    private static string CrearClaveDuplicado(MarcacionImportar marcacion)
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{marcacion.TrabajadorId}|{marcacion.FechaHora:yyyyMMddHHmmss}|{marcacion.Tipo}");
    }

    private static RechazoCsv CrearRechazo(
        int numeroLinea,
        string[] campos,
        string motivo)
    {
        return new RechazoCsv(
            numeroLinea,
            campos.ElementAtOrDefault(0) ?? string.Empty,
            campos.ElementAtOrDefault(1) ?? string.Empty,
            campos.ElementAtOrDefault(2) ?? string.Empty,
            campos.ElementAtOrDefault(3) ?? string.Empty,
            motivo);
    }

    private static async Task EscribirRechazadosAsync(
        string ruta,
        IReadOnlyCollection<RechazoCsv> rechazados,
        CancellationToken cancellationToken)
    {
        var directorio = Path.GetDirectoryName(ruta);
        if (!string.IsNullOrWhiteSpace(directorio))
            Directory.CreateDirectory(directorio);

        await using var writer = new StreamWriter(
            ruta,
            append: false,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

        await writer.WriteLineAsync(
            "numero_linea;rut;fecha_hora;tipo;origen;motivo".AsMemory(),
            cancellationToken);

        foreach (var rechazo in rechazados)
        {
            var valores = new[]
            {
                rechazo.NumeroLinea.ToString(CultureInfo.InvariantCulture),
                rechazo.Rut,
                rechazo.FechaHora,
                rechazo.Tipo,
                rechazo.Origen,
                rechazo.Motivo
            };

            var linea = string.Join(';', valores.Select(CsvSemicolonParser.Escapar));
            await writer.WriteLineAsync(linea.AsMemory(), cancellationToken);
        }
    }

    private sealed record DatosBase(
        string Rut,
        DateTime FechaHora,
        string Tipo,
        string Origen);
}
