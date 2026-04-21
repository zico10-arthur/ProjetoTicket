-- =============================================
-- Script de Setup Completo — SoldOut Tickets
-- Cria todas as tabelas e dados iniciais
-- =============================================

-- Tabela de Perfis
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Perfis')
BEGIN
    CREATE TABLE Perfis (
        Id   UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        Nome VARCHAR(50)      NOT NULL
    );

    INSERT INTO Perfis (Id, Nome) VALUES
        ('A1A1A1A1-A1A1-A1A1-A1A1-A1A1A1A1A1A1', 'Admin'),
        ('B2B2B2B2-B2B2-B2B2-B2B2-B2B2B2B2B2B2', 'Vendedor'),
        ('C3C3C3C3-C3C3-C3C3-C3C3-C3C3C3C3C3C3', 'Comprador');
END

-- Tabela de Usuarios
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Usuarios')
BEGIN
    CREATE TABLE Usuarios (
        Cpf      VARCHAR(11)      NOT NULL PRIMARY KEY,
        Nome     VARCHAR(100)     NOT NULL,
        Email    VARCHAR(150)     NOT NULL UNIQUE,
        PerfilId UNIQUEIDENTIFIER NOT NULL,
        Senha    VARCHAR(100)     NOT NULL,
        CONSTRAINT FK_Usuarios_Perfis FOREIGN KEY (PerfilId) REFERENCES Perfis(Id)
    );
END

-- Tabela de Cupons
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Cupons')
BEGIN
    CREATE TABLE Cupons (
        Codigo               VARCHAR(49)    NOT NULL PRIMARY KEY,
        PorcentagemDesconto  INT            NOT NULL,
        ValorMinimo          DECIMAL(10, 2) NOT NULL,
        DataExpiracao        DATETIME       NULL,
        Ativo                BIT            NOT NULL DEFAULT 1
    );
END

-- Tabela de Eventos
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Eventos')
BEGIN
    CREATE TABLE dbo.Eventos (
        id             UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        Nome           VARCHAR(100)     NOT NULL,
        CapacidadeTotal INT             NOT NULL,
        DataEvento     DATETIME         NOT NULL,
        PrecoPadrao    DECIMAL(10, 2)   NOT NULL,
        VendedorCpf    NVARCHAR(11)     NOT NULL DEFAULT ''
    );
END

-- Tabela de Ingressos
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Ingressos')
BEGIN
    CREATE TABLE dbo.Ingressos (
        Id          UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        EventoId    UNIQUEIDENTIFIER NOT NULL,
        Preco       DECIMAL(10, 2)   NOT NULL,
        Posicao     VARCHAR(50)      NOT NULL,
        Setor       VARCHAR(20)      NOT NULL,
        Status      INT              NOT NULL DEFAULT 0,
        DataBloqueio DATETIME        NULL,
        CONSTRAINT FK_Ingressos_Eventos FOREIGN KEY (EventoId) REFERENCES dbo.Eventos(id)
    );
END

-- Tabela de Reservas
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Reservas')
BEGIN
    CREATE TABLE Reservas (
        Id             UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        UsuarioCpf     NVARCHAR(11)     NOT NULL,
        EventoId       UNIQUEIDENTIFIER NOT NULL,
        IngressoId     UNIQUEIDENTIFIER NOT NULL,
        CupomUtilizado NVARCHAR(49)     NULL,
        ValorFinalPago NUMERIC(10, 2)   NOT NULL,
        DataBloqueio   DATETIME         NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_Reservas_Usuarios FOREIGN KEY (UsuarioCpf) REFERENCES Usuarios(Cpf),
        CONSTRAINT FK_Reservas_Eventos  FOREIGN KEY (EventoId)   REFERENCES dbo.Eventos(id),
        CONSTRAINT FK_Reservas_Ingressos FOREIGN KEY (IngressoId) REFERENCES dbo.Ingressos(Id)
    );
END
