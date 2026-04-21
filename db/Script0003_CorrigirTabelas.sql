-- Corrige tabela Usuarios: adiciona PK, UNIQUE e aumenta coluna Senha
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Usuarios')
BEGIN
    -- Aumenta Senha para 100 se ainda for menor
    IF EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
        WHERE TABLE_NAME = 'Usuarios' AND COLUMN_NAME = 'Senha' AND CHARACTER_MAXIMUM_LENGTH < 100
    )
    BEGIN
        ALTER TABLE Usuarios ALTER COLUMN Senha VARCHAR(100) NOT NULL;
    END

    -- Aumenta Nome para 100 se ainda for menor
    IF EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
        WHERE TABLE_NAME = 'Usuarios' AND COLUMN_NAME = 'Nome' AND CHARACTER_MAXIMUM_LENGTH < 100
    )
    BEGIN
        ALTER TABLE Usuarios ALTER COLUMN Nome VARCHAR(100) NOT NULL;
    END

    -- Adiciona PRIMARY KEY no Cpf se não existir
    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS 
        WHERE TABLE_NAME = 'Usuarios' AND CONSTRAINT_TYPE = 'PRIMARY KEY'
    )
    BEGIN
        ALTER TABLE Usuarios ADD CONSTRAINT PK_Usuarios PRIMARY KEY (Cpf);
    END

    -- Adiciona UNIQUE no Email se não existir
    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS 
        WHERE TABLE_NAME = 'Usuarios' AND CONSTRAINT_NAME = 'UQ_Usuarios_Email'
    )
    BEGIN
        ALTER TABLE Usuarios ADD CONSTRAINT UQ_Usuarios_Email UNIQUE (Email);
    END
END

-- Corrige tabela Cupons: adiciona PK no Codigo se não existir
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Cupons')
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS 
        WHERE TABLE_NAME = 'Cupons' AND CONSTRAINT_TYPE = 'PRIMARY KEY'
    )
    BEGIN
        ALTER TABLE Cupons ADD CONSTRAINT PK_Cupons PRIMARY KEY (Codigo);
    END
END

-- Cria tabela Perfis se não existir (usada no JOIN do BuscarCpf)
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Perfis')
BEGIN
    CREATE TABLE Perfis (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        Nome VARCHAR(50) NOT NULL
    );

    INSERT INTO Perfis (Id, Nome) VALUES
        ('A1A1A1A1-A1A1-A1A1-A1A1-A1A1A1A1A1A1', 'Admin'),
        ('B2B2B2B2-B2B2-B2B2-B2B2-B2B2B2B2B2B2', 'Vendedor'),
        ('C3C3C3C3-C3C3-C3C3-C3C3-C3C3C3C3C3C3', 'Comprador');
END
