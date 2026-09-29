-- Prueba técnica Genera · Kit 02: procedimiento a revisar
-- Contrato de salida (NO cambiar nombres ni tipos de columna):
--   TrabajadorId INT · Rut · Nombre · Turno · Entrada DATETIME2 · Salida DATETIME2
--   HorasTrabajadas DECIMAL(5,2) (0.00 si falta entrada o salida) · AtrasoMin INT (0 si no hay atraso) · Estado ('Completo' | 'Sin salida' | 'Ausente')
-- Debe listar a TODOS los trabajadores activos de la empresa que tengan turno asignado ese día, incluidos los ausentes.
-- Prueba: EXEC dbo.sp_calcula_horas_dia @EmpresaId = 1, @Fecha = '2026-09-15';

IF OBJECT_ID('dbo.sp_calcula_horas_dia','P') IS NOT NULL DROP PROCEDURE dbo.sp_calcula_horas_dia;
GO
CREATE PROCEDURE dbo.sp_calcula_horas_dia
    @EmpresaId INT,
    @Fecha     DATE
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        t.TrabajadorId,
        t.Rut,
        t.Nombre,
        tu.Nombre AS Turno,
        MIN(CASE WHEN m.Tipo = 'E' THEN m.FechaHora END) AS Entrada,
        MAX(CASE WHEN m.Tipo = 'S' THEN m.FechaHora END) AS Salida,
        CAST(DATEDIFF(MINUTE,
                      MIN(CASE WHEN m.Tipo = 'E' THEN m.FechaHora END),
                      MAX(CASE WHEN m.Tipo = 'S' THEN m.FechaHora END)) / 60.0 AS DECIMAL(5,2)) AS HorasTrabajadas,
        DATEDIFF(MINUTE, tu.HoraEntrada, CAST(MIN(CASE WHEN m.Tipo = 'E' THEN m.FechaHora END) AS TIME)) AS AtrasoMin,
        CASE
            WHEN MIN(CASE WHEN m.Tipo = 'E' THEN m.FechaHora END) IS NULL THEN 'Ausente'
            WHEN MAX(CASE WHEN m.Tipo = 'S' THEN m.FechaHora END) IS NULL THEN 'Sin salida'
            ELSE 'Completo'
        END AS Estado
    FROM dbo.Trabajador t
    INNER JOIN dbo.TrabajadorTurno tt ON tt.TrabajadorId = t.TrabajadorId AND tt.Fecha = @Fecha
    INNER JOIN dbo.Turno tu           ON tu.TurnoId = tt.TurnoId
    LEFT  JOIN dbo.Marcacion m        ON m.TrabajadorId = t.TrabajadorId
    WHERE t.EmpresaId = @EmpresaId
      AND t.Activo = 1
      AND CONVERT(VARCHAR(10), m.FechaHora, 112) = CONVERT(VARCHAR(10), @Fecha, 112)
    GROUP BY t.TrabajadorId, t.Rut, t.Nombre, tu.Nombre, tu.HoraEntrada
    ORDER BY t.Nombre;
END
GO