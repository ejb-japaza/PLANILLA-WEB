-- 001 - Columna de contraseña web hasheada en GENUSU1
-- Aprobación: P16 (DECISIONES_Y_PREGUNTAS_GERENCIA.md)
-- Aditivo e idempotente: puede ejecutarse más de una vez.
-- Pendiente (P12): replicar en las funciones fCam_ de modGENPLA03 del VB6.
-- Ejecutar conectado a la base del cliente (desarrollo: EJBBENNY).

IF COL_LENGTH('dbo.GENUSU1', 'US1_CPASWBH') IS NULL
    ALTER TABLE dbo.GENUSU1 ADD US1_CPASWBH VARCHAR(256) NULL;
GO

SELECT name, TYPE_NAME(user_type_id) AS tipo, max_length, is_nullable
FROM sys.columns
WHERE object_id = OBJECT_ID('dbo.GENUSU1') AND name = 'US1_CPASWBH';
