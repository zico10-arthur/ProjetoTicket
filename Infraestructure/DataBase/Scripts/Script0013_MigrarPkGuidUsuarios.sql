-- =============================================
-- Script0013: Migrar PK de Usuarios de Cpf para Id (Guid)
-- Spec 200 — Guid Id como PK de Usuarios
-- =============================================
-- IMPORTANTE: Este script é DESTRUTIVO.
-- Faça backup do banco antes de executar.
-- As colunas UsuarioCpf, VendedorCpf serão REMOVIDAS permanentemente.
-- =============================================

BEGIN TRANSACTION;

BEGIN TRY
    PRINT '=== Iniciando Script0013: Migrar PK Cpf → Guid Id ===';

    -- =============================================
    -- PASSO 1: Adicionar coluna Id à tabela Usuarios
    -- =============================================
    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
        WHERE TABLE_NAME = 'Usuarios' AND COLUMN_NAME = 'Id'
    )
    BEGIN
        PRINT 'Passo 1: Adicionando coluna Id...';
        ALTER TABLE Usuarios ADD Id UNIQUEIDENTIFIER NULL;
    END

    -- =============================================
    -- PASSO 2: Preencher Id com NEWID() para registros existentes
    -- =============================================
    PRINT 'Passo 2: Preenchendo Id com NEWID()...';
    UPDATE Usuarios SET Id = NEWID() WHERE Id IS NULL;

    -- =============================================
    -- PASSO 3: Tornar Id NOT NULL
    -- =============================================
    PRINT 'Passo 3: Tornando Id NOT NULL...';
    ALTER TABLE Usuarios ALTER COLUMN Id UNIQUEIDENTIFIER NOT NULL;

    -- =============================================
    -- PASSO 4: Remover PK antiga baseada em Cpf
    -- =============================================
    PRINT 'Passo 4: Removendo PK antiga (Cpf)...';
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

    -- =============================================
    -- PASSO 5: Criar nova PK em Id
    -- =============================================
    PRINT 'Passo 5: Criando nova PK em Id...';
    ALTER TABLE Usuarios ADD CONSTRAINT PK_Usuarios PRIMARY KEY (Id);

    -- =============================================
    -- PASSO 6: Tornar Cpf nullable
    -- =============================================
    PRINT 'Passo 6: Tornando Cpf nullable...';
    ALTER TABLE Usuarios ALTER COLUMN Cpf NVARCHAR(11) NULL;

    -- =============================================
    -- PASSO 7: Criar filtered unique index em Cpf
    -- =============================================
    PRINT 'Passo 7: Criando filtered unique index em Cpf...';
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_Usuarios_Cpf')
    BEGIN
        CREATE UNIQUE INDEX UQ_Usuarios_Cpf ON Usuarios(Cpf) WHERE Cpf IS NOT NULL;
    END

    -- =============================================
    -- PASSO 8: Remover FKs antigas de Reservas e Eventos
    -- =============================================
    PRINT 'Passo 8: Removendo FKs antigas...';
    
    -- FK_Reservas_Usuarios (UsuarioCpf → Usuarios.Cpf)
    IF EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS 
        WHERE TABLE_NAME = 'Reservas' AND CONSTRAINT_NAME = 'FK_Reservas_Usuarios'
    )
    BEGIN
        ALTER TABLE Reservas DROP CONSTRAINT FK_Reservas_Usuarios;
    END

    -- FK_Reservas_Usuarios_Vendedor (VendedorCpf → Usuarios.Cpf)
    IF EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS 
        WHERE TABLE_NAME = 'Reservas' AND CONSTRAINT_NAME = 'FK_Reservas_Usuarios_Vendedor'
    )
    BEGIN
        ALTER TABLE Reservas DROP CONSTRAINT FK_Reservas_Usuarios_Vendedor;
    END

    -- FK_Eventos_Vendedor (se existir)
    IF EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS 
        WHERE TABLE_NAME = 'Eventos' AND CONSTRAINT_NAME = 'FK_Eventos_Vendedor'
    )
    BEGIN
        ALTER TABLE Eventos DROP CONSTRAINT FK_Eventos_Vendedor;
    END

    -- =============================================
    -- PASSO 9: Adicionar colunas UsuarioId/VendedorId em Reservas
    -- =============================================
    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
        WHERE TABLE_NAME = 'Reservas' AND COLUMN_NAME = 'UsuarioId'
    )
    BEGIN
        PRINT 'Passo 9: Adicionando UsuarioId/VendedorId em Reservas...';
        
        ALTER TABLE Reservas ADD UsuarioId UNIQUEIDENTIFIER NULL;
        ALTER TABLE Reservas ADD VendedorId UNIQUEIDENTIFIER NULL;

        -- Migrar dados: UsuarioCpf → UsuarioId
        UPDATE r 
        SET r.UsuarioId = u.Id
        FROM Reservas r 
        INNER JOIN Usuarios u ON r.UsuarioCpf = u.Cpf;

        -- Migrar dados: VendedorCpf → VendedorId (pode ser vazio)
        UPDATE r 
        SET r.VendedorId = u.Id
        FROM Reservas r 
        INNER JOIN Usuarios u ON r.VendedorCpf = u.Cpf
        WHERE r.VendedorCpf IS NOT NULL AND r.VendedorCpf != '';

        -- Tornar UsuarioId NOT NULL
        ALTER TABLE Reservas ALTER COLUMN UsuarioId UNIQUEIDENTIFIER NOT NULL;
    END

    -- =============================================
    -- PASSO 10: Adicionar coluna VendedorId em Eventos
    -- =============================================
    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
        WHERE TABLE_NAME = 'Eventos' AND COLUMN_NAME = 'VendedorId'
    )
    BEGIN
        PRINT 'Passo 10: Adicionando VendedorId em Eventos...';
        
        ALTER TABLE dbo.Eventos ADD VendedorId UNIQUEIDENTIFIER NULL;

        -- Migrar dados: VendedorCpf → VendedorId
        UPDATE e 
        SET e.VendedorId = u.Id
        FROM dbo.Eventos e 
        INNER JOIN Usuarios u ON e.VendedorCpf = u.Cpf
        WHERE e.VendedorCpf IS NOT NULL AND e.VendedorCpf != '';
    END

    -- =============================================
    -- PASSO 11: Criar novas FKs
    -- =============================================
    PRINT 'Passo 11: Criando novas FKs...';

    -- FK_Reservas_Usuarios (UsuarioId → Usuarios.Id)
    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS 
        WHERE TABLE_NAME = 'Reservas' AND CONSTRAINT_NAME = 'FK_Reservas_Usuarios'
    )
    BEGIN
        ALTER TABLE Reservas ADD CONSTRAINT FK_Reservas_Usuarios 
            FOREIGN KEY (UsuarioId) REFERENCES Usuarios(Id);
    END

    -- FK_Reservas_Vendedor (VendedorId → Usuarios.Id)
    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS 
        WHERE TABLE_NAME = 'Reservas' AND CONSTRAINT_NAME = 'FK_Reservas_Vendedor'
    )
    BEGIN
        ALTER TABLE Reservas ADD CONSTRAINT FK_Reservas_Vendedor 
            FOREIGN KEY (VendedorId) REFERENCES Usuarios(Id);
    END

    -- FK_Eventos_Vendedor (VendedorId → Usuarios.Id)
    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS 
        WHERE TABLE_NAME = 'Eventos' AND CONSTRAINT_NAME = 'FK_Eventos_Vendedor'
    )
    BEGIN
        ALTER TABLE dbo.Eventos ADD CONSTRAINT FK_Eventos_Vendedor 
            FOREIGN KEY (VendedorId) REFERENCES Usuarios(Id);
    END

    -- =============================================
    -- PASSO 12: Criar índices para performance
    -- =============================================
    PRINT 'Passo 12: Criando índices...';

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Reservas_UsuarioId')
        CREATE INDEX IX_Reservas_UsuarioId ON Reservas(UsuarioId);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Reservas_VendedorId')
        CREATE INDEX IX_Reservas_VendedorId ON Reservas(VendedorId);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Eventos_VendedorId')
        CREATE INDEX IX_Eventos_VendedorId ON dbo.Eventos(VendedorId);

    -- =============================================
    -- PASSO 13: Remover colunas antigas
    -- =============================================
    PRINT 'Passo 13: Removendo colunas antigas...';

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

    -- =============================================
    -- PASSO 14: Remover índice antigo (se existir)
    -- =============================================
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Reservas_VendedorCpf')
        DROP INDEX IX_Reservas_VendedorCpf ON Reservas;

    PRINT '=== Script0013 concluído com sucesso! ===';
    
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    THROW;
END CATCH
