namespace ImportadorMarcaciones.Models;

public sealed record TrabajadorDb(
    int TrabajadorId,
    string Rut,
    bool Activo);
