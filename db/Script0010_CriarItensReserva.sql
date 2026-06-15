-- Script0010: Criar tabela ItensReserva e adaptar Reservas para multi-item

-- 1. Criar tabela ItensReserva
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ItensReserva')
BEGIN
    CREATE TABLE dbo.ItensReserva (
        Id              UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        ReservaId       UNIQUEIDENTIFIER NOT NULL,
        CpfParticipante VARCHAR(14)      NOT NULL,
        IngressoId      UNIQUEIDENTIFIER NOT NULL,
        PrecoUnitario   DECIMAL(10,2)    NOT NULL,
        Reembolsado     BIT              NOT NULL DEFAULT 0,
        CONSTRAINT FK_ItensReserva_Reservas FOREIGN KEY (ReservaId)
            REFERENCES Reservas(Id),
        CONSTRAINT FK_ItensReserva_Ingressos FOREIGN KEY (IngressoId)
            REFERENCES Ingressos(Id)
    );
END

-- 2. Tornar IngressoId nullable em Reservas (será substituído por ItensReserva)
--    Mantemos a coluna para compatibilidade com dados existentes
IF EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Reservas' AND COLUMN_NAME = 'IngressoId' AND IS_NULLABLE = 'NO'
)
BEGIN
    ALTER TABLE Reservas ALTER COLUMN IngressoId UNIQUEIDENTIFIER NULL;
END
