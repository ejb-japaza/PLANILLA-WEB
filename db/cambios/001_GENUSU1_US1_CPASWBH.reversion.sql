-- Reversión de 001: solo si se necesita deshacer y la columna aún no tiene datos.

IF COL_LENGTH('dbo.GENUSU1', 'US1_CPASWBH') IS NOT NULL
    ALTER TABLE dbo.GENUSU1 DROP COLUMN US1_CPASWBH;
GO
