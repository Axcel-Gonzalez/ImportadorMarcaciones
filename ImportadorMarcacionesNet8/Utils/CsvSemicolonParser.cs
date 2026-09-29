using System.Text;

namespace ImportadorMarcaciones.Utils;

public static class CsvSemicolonParser
{
    public static string[] ParsearLinea(string linea)
    {
        var campos = new List<string>();
        var actual = new StringBuilder();
        var entreComillas = false;

        for (var i = 0; i < linea.Length; i++)
        {
            var c = linea[i];

            if (c == '"')
            {
                if (entreComillas && i + 1 < linea.Length && linea[i + 1] == '"')
                {
                    actual.Append('"');
                    i++;
                }
                else
                {
                    entreComillas = !entreComillas;
                }

                continue;
            }

            if (c == ';' && !entreComillas)
            {
                campos.Add(actual.ToString());
                actual.Clear();
                continue;
            }

            actual.Append(c);
        }

        campos.Add(actual.ToString());
        return campos.ToArray();
    }

    public static string Escapar(string? valor)
    {
        valor ??= string.Empty;

        if (valor.Contains(';') || valor.Contains('"') || valor.Contains('\r') || valor.Contains('\n'))
            return $"\"{valor.Replace("\"", "\"\"")}\"";

        return valor;
    }
}
