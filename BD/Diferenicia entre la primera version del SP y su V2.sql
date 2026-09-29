USE [ControlMarcaje]
GO
EXEC dbo.sp_calcula_horas_dia @EmpresaId = 1, @Fecha = '2026-09-14';
EXEC dbo.sp_calcula_horas_dia_V2 @EmpresaId = 1, @Fecha = '2026-09-14';