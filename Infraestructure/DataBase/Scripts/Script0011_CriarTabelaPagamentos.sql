-- Script0011: Criar tabela Pagamentos e adicionar campo Pago em Reservas

-- 1. Criar tabela Pagamentos
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Pagamentos')
BEGIN
    CREATE TABLE dbo.Pagamentos (
        Id              UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        ReservaId       UNIQUEIDENTIFIER NOT NULL,
        ValorPago       DECIMAL(10,2)    NOT NULL,
        Status          INT              NOT NULL DEFAULT 0,
        Metodo          NVARCHAR(50)     NOT NULL,
        DataPagamento   DATETIME2        NOT NULL,
        DataCriacao     DATETIME2        NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_Pagamentos_Reservas FOREIGN KEY (ReservaId)
            REFERENCES Reservas(Id)
    );
END

-- 2. Adicionar campo Pago em Reservas
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'Reservas' AND COLUMN_NAME = 'Pago'
)
BEGIN
    ALTER TABLE dbo.Reservas ADD Pago BIT NOT NULL DEFAULT 0;
END
