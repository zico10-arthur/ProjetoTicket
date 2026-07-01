-- =============================================
-- Script0013: Migrar PK de Usuarios de Cpf para Id (Guid)
-- Spec 200 — Guid Id como PK de Usuarios
-- =============================================

-- PASSO 1: Remover TODAS as FKs que referenciam Usuarios — OBRIGATÓRIO antes de alterar a PK
DECLARE @fkDrop NVARCHAR(MAX) = '';
SELECT @fkDrop = @fkDrop + 'ALTER TABLE ' + QUOTENAME(OBJECT_NAME(fk.parent_object_id)) + ' DROP CONSTRAINT ' + QUOTENAME(fk.name) + '; '
FROM sys.foreign_keys fk
WHERE OBJECT_NAME(fk.referenced_object_id) = 'Usuarios';
IF LEN(@fkDrop) > 0
    EXEC(@fkDrop);

-- PASSO 2: Adicionar coluna Id à tabela Usuarios
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Usuarios' AND COLUMN_NAME = 'Id'
)
BEGIN
    ALTER TABLE Usuarios ADD Id UNIQUEIDENTIFIER NULL;
END

-- PASSO 3: Preencher Id com NEWID() via EXEC (forçar novo batch)
EXEC('UPDATE Usuarios SET Id = NEWID() WHERE Id IS NULL');

-- PASSO 4: Tornar Id NOT NULL via EXEC
EXEC('ALTER TABLE Usuarios ALTER COLUMN Id UNIQUEIDENTIFIER NOT NULL');

-- PASSO 5: Remover PK antiga baseada em Cpf
IF EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS 
    WHERE TABLE_NAME = 'Usuarios' AND CONSTRAINT_TYPE = 'PRIMARY KEY'
)
BEGIN
    DECLARE @pkName NVARCHAR(200);
    SELECT @pkName = CONSTRAINT_NAME 
    FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS 
    WHERE TABLE_NAME = 'Usuarios' AND CONSTRAINT_TYPE = 'PRIMARY KEY';
    EXEC('ALTER TABLE Usuarios DROP CONSTRAINT ' + @pkName);
END

-- PASSO 6: Criar nova PK em Id
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS 
    WHERE TABLE_NAME = 'Usuarios' AND CONSTRAINT_NAME = 'PK_Usuarios'
)
BEGIN
    ALTER TABLE Usuarios ADD CONSTRAINT PK_Usuarios PRIMARY KEY (Id);
END

-- PASSO 7: Tornar Cpf nullable
ALTER TABLE Usuarios ALTER COLUMN Cpf NVARCHAR(11) NULL;

-- PASSO 8: Criar filtered unique index em Cpf
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_Usuarios_Cpf')
BEGIN
    CREATE UNIQUE INDEX UQ_Usuarios_Cpf ON Usuarios(Cpf) WHERE Cpf IS NOT NULL;
END

-- PASSO 9: Adicionar colunas UsuarioId/VendedorId em Reservas
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Reservas' AND COLUMN_NAME = 'UsuarioId'
)
BEGIN
    ALTER TABLE Reservas ADD UsuarioId UNIQUEIDENTIFIER NULL;
END

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Reservas' AND COLUMN_NAME = 'VendedorId'
)
BEGIN
    ALTER TABLE Reservas ADD VendedorId UNIQUEIDENTIFIER NULL;
END

-- Migrar dados Reservas via EXEC
EXEC('
    UPDATE r 
    SET r.UsuarioId = u.Id
    FROM Reservas r 
    INNER JOIN Usuarios u ON r.UsuarioCpf = u.Cpf
    WHERE r.UsuarioId IS NULL
');

EXEC('
    UPDATE r 
    SET r.VendedorId = u.Id
    FROM Reservas r 
    INNER JOIN Usuarios u ON r.VendedorCpf = u.Cpf
    WHERE r.VendedorCpf IS NOT NULL AND r.VendedorCpf != '''' AND r.VendedorId IS NULL
');

-- Tornar UsuarioId NOT NULL via EXEC
EXEC('ALTER TABLE Reservas ALTER COLUMN UsuarioId UNIQUEIDENTIFIER NOT NULL');

-- PASSO 10: Adicionar coluna VendedorId em Eventos
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Eventos' AND COLUMN_NAME = 'VendedorId'
)
BEGIN
    ALTER TABLE dbo.Eventos ADD VendedorId UNIQUEIDENTIFIER NULL;
END

-- Migrar dados Eventos via EXEC
EXEC('
    UPDATE e 
    SET e.VendedorId = u.Id
    FROM dbo.Eventos e 
    INNER JOIN Usuarios u ON e.VendedorCpf = u.Cpf
    WHERE e.VendedorCpf IS NOT NULL AND e.VendedorCpf != '''' AND e.VendedorId IS NULL
');

-- PASSO 11: Criar novas FKs
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS 
    WHERE TABLE_NAME = 'Reservas' AND CONSTRAINT_NAME = 'FK_Reservas_Usuarios'
)
BEGIN
    ALTER TABLE Reservas ADD CONSTRAINT FK_Reservas_Usuarios 
        FOREIGN KEY (UsuarioId) REFERENCES Usuarios(Id);
END

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS 
    WHERE TABLE_NAME = 'Reservas' AND CONSTRAINT_NAME = 'FK_Reservas_Vendedor'
)
BEGIN
    ALTER TABLE Reservas ADD CONSTRAINT FK_Reservas_Vendedor 
        FOREIGN KEY (VendedorId) REFERENCES Usuarios(Id);
END

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS 
    WHERE TABLE_NAME = 'Eventos' AND CONSTRAINT_NAME = 'FK_Eventos_Vendedor'
)
BEGIN
    ALTER TABLE dbo.Eventos ADD CONSTRAINT FK_Eventos_Vendedor 
        FOREIGN KEY (VendedorId) REFERENCES Usuarios(Id);
END

-- PASSO 12: Criar índices para performance
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Reservas_UsuarioId')
    CREATE INDEX IX_Reservas_UsuarioId ON Reservas(UsuarioId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Reservas_VendedorId')
    CREATE INDEX IX_Reservas_VendedorId ON Reservas(VendedorId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Eventos_VendedorId')
    CREATE INDEX IX_Eventos_VendedorId ON dbo.Eventos(VendedorId);

-- PASSO 13: Remover DEFAULT constraints antes de dropar colunas antigas
DECLARE @dfDrop NVARCHAR(MAX) = '';
SELECT @dfDrop = @dfDrop + 'ALTER TABLE ' + QUOTENAME(t.name) + ' DROP CONSTRAINT ' + QUOTENAME(dc.name) + '; '
FROM sys.default_constraints dc
INNER JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
INNER JOIN sys.tables t ON c.object_id = t.object_id
WHERE (t.name = 'Reservas' AND c.name IN ('UsuarioCpf', 'VendedorCpf'))
   OR (t.name = 'Eventos' AND c.name = 'VendedorCpf');
IF LEN(@dfDrop) > 0
    EXEC(@dfDrop);

-- PASSO 14: Remover índice antigo antes de dropar coluna
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Reservas_VendedorCpf')
    DROP INDEX IX_Reservas_VendedorCpf ON Reservas;

-- PASSO 15: Dropar colunas antigas
IF EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Reservas' AND COLUMN_NAME = 'UsuarioCpf'
)
    ALTER TABLE Reservas DROP COLUMN UsuarioCpf;

IF EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Reservas' AND COLUMN_NAME = 'VendedorCpf'
)
    ALTER TABLE Reservas DROP COLUMN VendedorCpf;

IF EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Eventos' AND COLUMN_NAME = 'VendedorCpf'
)
    ALTER TABLE dbo.Eventos DROP COLUMN VendedorCpf;
