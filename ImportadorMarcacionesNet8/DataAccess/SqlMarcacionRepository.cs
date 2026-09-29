using ImportadorMarcaciones.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ImportadorMarcaciones.DataAccess;

public sealed class SqlMarcacionRepository
{
    private readonly SqlConnection _connection;

    public SqlMarcacionRepository(SqlConnection connection)
    {
        _connection = connection;
    }

    public async Task<Dictionary<string, TrabajadorDb>> ObtenerTrabajadoresPorRutAsync(
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                TrabajadorId,
                Rut,
                Activo
            FROM dbo.Trabajador;
            """;

        await using var command = new SqlCommand(sql, _connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var trabajadores = new Dictionary<string, TrabajadorDb>(StringComparer.OrdinalIgnoreCase);

        while (await reader.ReadAsync(cancellationToken))
        {
            var trabajador = new TrabajadorDb(
                reader.GetInt32(0),
                reader.GetString(1).Trim().ToUpperInvariant(),
                reader.GetBoolean(2));

            trabajadores[trabajador.Rut] = trabajador;
        }

        return trabajadores;
    }

    public async Task<bool> ExisteMarcacionAsync(
        int trabajadorId,
        DateTime fechaHora,
        string tipo,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT CASE
                WHEN EXISTS
                (
                    SELECT 1
                    FROM dbo.Marcacion
                    WHERE TrabajadorId = @TrabajadorId
                      AND FechaHora = @FechaHora
                      AND Tipo = @Tipo
                )
                THEN CAST(1 AS BIT)
                ELSE CAST(0 AS BIT)
            END;
            """;

        await using var command = new SqlCommand(sql, _connection);
        command.Parameters.Add("@TrabajadorId", SqlDbType.Int).Value = trabajadorId;
        command.Parameters.Add("@FechaHora", SqlDbType.DateTime2).Value = fechaHora;
        command.Parameters.Add("@Tipo", SqlDbType.Char, 1).Value = tipo;

        var resultado = await command.ExecuteScalarAsync(cancellationToken);
        return resultado is bool existe && existe;
    }

    public async Task InsertarMarcacionAsync(
        MarcacionImportar marcacion,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.Marcacion
            (
                TrabajadorId,
                FechaHora,
                Tipo,
                Origen
            )
            VALUES
            (
                @TrabajadorId,
                @FechaHora,
                @Tipo,
                @Origen
            );
            """;

        await using var command = new SqlCommand(sql, _connection);
        command.Parameters.Add("@TrabajadorId", SqlDbType.Int).Value = marcacion.TrabajadorId;
        command.Parameters.Add("@FechaHora", SqlDbType.DateTime2).Value = marcacion.FechaHora;
        command.Parameters.Add("@Tipo", SqlDbType.Char, 1).Value = marcacion.Tipo;
        command.Parameters.Add("@Origen", SqlDbType.VarChar, 20).Value = marcacion.Origen;

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
