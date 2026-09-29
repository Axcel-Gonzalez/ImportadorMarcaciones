--Index para optimizar la busqueda entre el turno y el trabajador
CREATE INDEX IX_TrabajadorTurno_Fecha_TrabajadorId
ON dbo.TrabajadorTurno
(
    Fecha,
    TrabajadorId
)
INCLUDE
(
    TurnoId
);