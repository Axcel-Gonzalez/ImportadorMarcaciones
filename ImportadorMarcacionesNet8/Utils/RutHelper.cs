using System.Globalization;

namespace ImportadorMarcaciones.Utils;

public static class RutHelper
{
    public static bool TryNormalizarYValidar(string? valor, out string rutCanonico, out string error)
    {
        rutCanonico = string.Empty;
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(valor))
        {
            error = "RUT vacío.";
            return false;
        }

        var limpio = valor
            .Trim()
            .Replace(".", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .ToUpperInvariant();

        var partes = limpio.Split('-');
        if (partes.Length != 2 || string.IsNullOrWhiteSpace(partes[0]) || partes[1].Length != 1)
        {
            error = "Formato de RUT inválido. Se espera cuerpo-DV, por ejemplo 12345678-5.";
            return false;
        }

        if (!int.TryParse(partes[0], NumberStyles.None, CultureInfo.InvariantCulture, out var cuerpo) || cuerpo <= 0)
        {
            error = "El cuerpo del RUT debe ser numérico y mayor que cero.";
            return false;
        }

        var dvInformado = char.ToUpperInvariant(partes[1][0]);
        if (!(char.IsDigit(dvInformado) || dvInformado == 'K'))
        {
            error = "Dígito verificador de RUT inválido.";
            return false;
        }

        var dvCalculado = CalcularDv(partes[0]);
        if (dvInformado != dvCalculado)
        {
            error = $"Dígito verificador incorrecto. Para el RUT informado corresponde {dvCalculado}.";
            return false;
        }

        // Conserva el cuerpo sin puntos y evita ceros a la izquierda innecesarios.
        rutCanonico = $"{cuerpo}-{dvInformado}";
        return true;
    }

    private static char CalcularDv(string cuerpo)
    {
        var suma = 0;
        var factor = 2;

        for (var i = cuerpo.Length - 1; i >= 0; i--)
        {
            suma += (cuerpo[i] - '0') * factor;
            factor = factor == 7 ? 2 : factor + 1;
        }

        var resultado = 11 - (suma % 11);

        return resultado switch
        {
            11 => '0',
            10 => 'K',
            _ => (char)('0' + resultado)
        };
    }
}
