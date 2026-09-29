/* ============================================================
   CREACIÓN DE LOGIN Y USUARIO PARA IMPORTADOR DE MARCACIONES
   Usar únicamente datos ficticios/locales en la prueba técnica.
   ============================================================ */

USE [master];
GO

/* 1. Crear Login a nivel de instancia */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.server_principals
    WHERE name = 'usr_importador_marcaciones'
)
BEGIN
    CREATE LOGIN [usr_importador_marcaciones]
    WITH PASSWORD = 'Clave_Importador2026!',
         CHECK_POLICY = ON,
         CHECK_EXPIRATION = OFF;
END
GO


/* ============================================================
   2. Cambiar por el nombre REAL de tu BD local
   ============================================================ */

USE [ControlMarcaje];
GO


/* 3. Crear usuario dentro de la base de datos */
IF NOT EXISTS
(
    SELECT 1
    FROM sys.database_principals
    WHERE name = 'usr_importador_marcaciones'
)
BEGIN
    CREATE USER [usr_importador_marcaciones]
    FOR LOGIN [usr_importador_marcaciones];
END
GO


/* ============================================================
   4. PERMISOS MÍNIMOS NECESARIOS
   ============================================================ */

/* Necesario para buscar TrabajadorId, RUT y Activo */
GRANT SELECT
ON dbo.Trabajador
TO [usr_importador_marcaciones];
GO


/* Necesario para verificar si una marcación ya existe */
GRANT SELECT
ON dbo.Marcacion
TO [usr_importador_marcaciones];
GO


/* Necesario para insertar las marcaciones válidas */
GRANT INSERT
ON dbo.Marcacion
TO [usr_importador_marcaciones];
GO


PRINT 'Usuario usr_importador_marcaciones creado correctamente.';
GO