namespace ImportadorMarcaciones.Models;

public sealed record MarcacionImportar(
    int TrabajadorId,
    string Rut,
    DateTime FechaHora,
    string Tipo,
    string Origen);
