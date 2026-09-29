namespace ImportadorMarcaciones.Models;

public sealed record RechazoCsv(
    int NumeroLinea,
    string Rut,
    string FechaHora,
    string Tipo,
    string Origen,
    string Motivo);
