-- Prueba técnica Genera · Kit 03 (OPCIONAL): volumen para medir rendimiento
-- Agrega 200 trabajadores ficticios a la empresa 3 y ~292.000 marcaciones (2 por día, 2 años).
-- Tarda menos de 1 minuto en un equipo normal. Solo sirve para comparar el plan de ejecución antes y después.
-- Ejecutar UNA sola vez sobre kit 01 recién cargado: inserta la Empresa 3 y los TrabajadorId 1001 a 1200; una segunda ejecución falla por clave primaria. Los RUT de este kit tienen DV fijo '-0' (aquí no se validan).
SET NOCOUNT ON;

INSERT dbo.Empresa (EmpresaId, Nombre) VALUES (3, N'Volumen Ficticio SA');

;WITH n AS (SELECT TOP (200) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS i FROM sys.all_objects)
INSERT dbo.Trabajador (TrabajadorId, EmpresaId, Rut, Nombre, Activo)
SELECT 1000 + i, 3, CAST(20000000 + i AS VARCHAR(10)) + '-0', N'Trabajador Volumen ' + CAST(i AS NVARCHAR(5)), 1 FROM n;

;WITH d AS (SELECT TOP (730) DATEADD(DAY, -ROW_NUMBER() OVER (ORDER BY (SELECT NULL)), '2026-09-15') AS Fecha FROM sys.all_objects)
INSERT dbo.TrabajadorTurno (TrabajadorId, Fecha, TurnoId)
SELECT t.TrabajadorId, d.Fecha, 1 FROM dbo.Trabajador t CROSS JOIN d WHERE t.EmpresaId = 3;

INSERT dbo.Marcacion (TrabajadorId, FechaHora, Tipo, Origen)
SELECT tt.TrabajadorId, DATEADD(MINUTE, 480 + (tt.TrabajadorId % 7), CAST(tt.Fecha AS DATETIME2(0))), 'E', 'CARGA'
FROM dbo.TrabajadorTurno tt JOIN dbo.Trabajador t ON t.TrabajadorId = tt.TrabajadorId WHERE t.EmpresaId = 3
UNION ALL
SELECT tt.TrabajadorId, DATEADD(MINUTE, 1020 + (tt.TrabajadorId % 5), CAST(tt.Fecha AS DATETIME2(0))), 'S', 'CARGA'
FROM dbo.TrabajadorTurno tt JOIN dbo.Trabajador t ON t.TrabajadorId = tt.TrabajadorId WHERE t.EmpresaId = 3;

DECLARE @m INT = (SELECT COUNT(*) FROM dbo.Marcacion);
PRINT 'Kit 03 cargado. Marcaciones totales: ' + CAST(@m AS VARCHAR(10));
-- Medir: SET STATISTICS IO ON; EXEC dbo.sp_calcula_horas_dia @EmpresaId = 1, @Fecha = '2026-09-15';
