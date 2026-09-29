namespace ImportadorMarcaciones.Models;

public sealed record ImportacionResultado(
    int Procesados,
    int Insertados,
    int Rechazados);
