-- Script para agregar columna CodigoEquipo a la tabla Equipo
USE LynnusPeruDB;
GO

-- Verificar si la columna ya existe
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Equipo') AND name = 'codigo_equipo')
BEGIN
    -- Agregar la columna
    ALTER TABLE Equipo ADD codigo_equipo NVARCHAR(50) NOT NULL DEFAULT '';
    
    -- Actualizar registros existentes con códigos únicos
    UPDATE Equipo 
    SET codigo_equipo = 'EQ-' + CAST(equipo_id AS VARCHAR(10)) + '-' + FORMAT(GETDATE(), 'yyyyMMdd')
    WHERE codigo_equipo = '';
    
    PRINT 'Columna codigo_equipo agregada exitosamente.';
END
ELSE
BEGIN
    PRINT 'La columna codigo_equipo ya existe.';
END
GO