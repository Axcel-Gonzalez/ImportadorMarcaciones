--Index para optimizar la busqueda entre trabajador y marcacion
CREATE INDEX IX_Marcacion_TrabajadorId_FechaHora
ON dbo.Marcacion
(
    TrabajadorId,
    FechaHora
)
INCLUDE
(
    Tipo
);