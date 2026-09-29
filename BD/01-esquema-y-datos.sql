-- Prueba técnica Genera · Kit 01: esquema y datos FICTICIOS (Control de Asistencia simplificado)
-- Requiere SQL Server 2016 o superior. Ejecutar completo en una base de datos vacía (ej. PruebaGenera).
SET NOCOUNT ON;

IF OBJECT_ID('dbo.Marcacion','U')       IS NOT NULL DROP TABLE dbo.Marcacion;
IF OBJECT_ID('dbo.TrabajadorTurno','U') IS NOT NULL DROP TABLE dbo.TrabajadorTurno;
IF OBJECT_ID('dbo.Trabajador','U')      IS NOT NULL DROP TABLE dbo.Trabajador;
IF OBJECT_ID('dbo.Turno','U')           IS NOT NULL DROP TABLE dbo.Turno;
IF OBJECT_ID('dbo.Empresa','U')         IS NOT NULL DROP TABLE dbo.Empresa;

CREATE TABLE dbo.Empresa (
    EmpresaId INT           NOT NULL CONSTRAINT PK_Empresa PRIMARY KEY,
    Nombre    NVARCHAR(100) NOT NULL
);

CREATE TABLE dbo.Turno (
    TurnoId         INT          NOT NULL CONSTRAINT PK_Turno PRIMARY KEY,
    Nombre          NVARCHAR(50) NOT NULL,
    HoraEntrada     TIME(0)      NOT NULL,
    HoraSalida      TIME(0)      NOT NULL,
    CruzaMedianoche BIT          NOT NULL   -- 1 = la salida ocurre el día siguiente
);

CREATE TABLE dbo.Trabajador (
    TrabajadorId INT           NOT NULL CONSTRAINT PK_Trabajador PRIMARY KEY,
    EmpresaId    INT           NOT NULL CONSTRAINT FK_Trabajador_Empresa REFERENCES dbo.Empresa(EmpresaId),
    Rut          VARCHAR(12)   NOT NULL,   -- formato canónico: sin puntos, con guion, DV en mayúscula. Ej: 12345678-5
    Nombre       NVARCHAR(100) NOT NULL,
    Activo       BIT           NOT NULL,
    CONSTRAINT UQ_Trabajador_Rut UNIQUE (Rut)
);

CREATE TABLE dbo.TrabajadorTurno (           -- turno asignado a cada trabajador por día
    TrabajadorId INT  NOT NULL CONSTRAINT FK_TT_Trabajador REFERENCES dbo.Trabajador(TrabajadorId),
    Fecha        DATE NOT NULL,
    TurnoId      INT  NOT NULL CONSTRAINT FK_TT_Turno REFERENCES dbo.Turno(TurnoId),
    CONSTRAINT PK_TrabajadorTurno PRIMARY KEY (TrabajadorId, Fecha)
);

CREATE TABLE dbo.Marcacion (
    MarcacionId  BIGINT       IDENTITY(1,1) NOT NULL CONSTRAINT PK_Marcacion PRIMARY KEY,
    TrabajadorId INT          NOT NULL CONSTRAINT FK_Marcacion_Trabajador REFERENCES dbo.Trabajador(TrabajadorId),
    FechaHora    DATETIME2(0) NOT NULL,
    Tipo         CHAR(1)      NOT NULL CONSTRAINT CK_Marcacion_Tipo CHECK (Tipo IN ('E','S')),  -- E = entrada, S = salida
    Origen       VARCHAR(20)  NOT NULL
);
-- Nota: la tabla Marcacion solo tiene su clave primaria. No hay más índices a propósito.

INSERT dbo.Empresa (EmpresaId, Nombre) VALUES
(1, N'Distribuidora Andes Ficticia SpA'),
(2, N'Servicios Cordillera Ficticia Ltda.');

INSERT dbo.Turno (TurnoId, Nombre, HoraEntrada, HoraSalida, CruzaMedianoche) VALUES
(1, N'Diurno',   '08:00:00', '17:00:00', 0),
(2, N'Nocturno', '22:00:00', '06:00:00', 1);

INSERT dbo.Trabajador (TrabajadorId, EmpresaId, Rut, Nombre, Activo) VALUES
(1,1,N'12345678-5',N'Ana Riquelme Soto',1),
(2,1,N'9876543-3',N'Bruno Salas Peña',1),
(3,1,N'15678234-3',N'Carla Núñez Rojas',1),
(4,1,N'11223344-K',N'Diego Paredes Lara',1),
(5,1,N'17890123-0',N'Elena Vidal Castro',1),
(6,1,N'13579246-2',N'Felipe Ortiz Mora',1),
(7,1,N'10203040-0',N'Gonzalo Mena Ruiz',0),
(8,2,N'14141414-3',N'Helena Toro Díaz',1);

INSERT dbo.TrabajadorTurno (TrabajadorId, Fecha, TurnoId) VALUES
(1,'2026-09-14',1),(1,'2026-09-15',1),(1,'2026-09-16',1),(2,'2026-09-14',1),(2,'2026-09-15',1),(2,'2026-09-16',1),(3,'2026-09-14',1),(3,'2026-09-15',1),(3,'2026-09-16',1),(4,'2026-09-14',1),(4,'2026-09-15',1),(4,'2026-09-16',1),(5,'2026-09-14',2),(5,'2026-09-15',2),(5,'2026-09-16',2),(6,'2026-09-14',2),(6,'2026-09-15',2),(6,'2026-09-16',2),(8,'2026-09-14',1),(8,'2026-09-15',1),(8,'2026-09-16',1);

INSERT dbo.Marcacion (TrabajadorId, FechaHora, Tipo, Origen) VALUES
(1,'2026-09-14 08:01:00','E',N'RELOJ'),
(1,'2026-09-14 17:00:00','S',N'RELOJ'),
(1,'2026-09-15 07:58:00','E',N'RELOJ'),
(1,'2026-09-15 17:03:00','S',N'RELOJ'),
(2,'2026-09-15 08:20:00','E',N'RELOJ'),
(2,'2026-09-15 17:00:00','S',N'RELOJ'),
(3,'2026-09-14 08:00:00','E',N'RELOJ'),
(3,'2026-09-14 17:00:00','S',N'RELOJ'),
(4,'2026-09-15 08:00:00','E',N'RELOJ'),
(4,'2026-09-15 08:00:40','E',N'RELOJ'),
(4,'2026-09-15 17:00:00','S',N'RELOJ'),
(5,'2026-09-14 22:00:00','E',N'RELOJ'),
(5,'2026-09-15 06:00:00','S',N'RELOJ'),
(5,'2026-09-15 21:55:00','E',N'RELOJ'),
(5,'2026-09-16 06:02:00','S',N'RELOJ'),
(6,'2026-09-15 22:10:00','E',N'RELOJ'),
(6,'2026-09-16 05:58:00','S',N'RELOJ'),
(7,'2026-09-15 08:00:00','E',N'RELOJ'),
(7,'2026-09-15 17:00:00','S',N'RELOJ'),
(8,'2026-09-15 08:00:00','E',N'RELOJ'),
(8,'2026-09-15 17:00:00','S',N'RELOJ');

DECLARE @t INT = (SELECT COUNT(*) FROM dbo.Trabajador), @m INT = (SELECT COUNT(*) FROM dbo.Marcacion);
PRINT 'Kit 01 cargado: ' + CAST(@t AS VARCHAR(10)) + ' trabajadores, ' + CAST(@m AS VARCHAR(10)) + ' marcaciones.';
