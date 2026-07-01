-- Script0014: Adicionar coluna Reembolsada na tabela Reservas
-- Spec 40: Comprador Cancela Reserva com Reembolso

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'Reservas' AND COLUMN_NAME = 'Reembolsada'
)
BEGIN
    ALTER TABLE dbo.Reservas ADD Reembolsada BIT NOT NULL DEFAULT 0;
END
