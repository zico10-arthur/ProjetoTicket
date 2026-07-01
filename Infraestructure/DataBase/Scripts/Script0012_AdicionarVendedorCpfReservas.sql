-- Script0012: Adicionar VendedorCpf à tabela Reservas
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Reservas' AND COLUMN_NAME = 'VendedorCpf'
)
BEGIN
    ALTER TABLE Reservas ADD VendedorCpf NVARCHAR(11) NOT NULL DEFAULT '';

    ALTER TABLE Reservas ADD CONSTRAINT FK_Reservas_Usuarios_Vendedor
        FOREIGN KEY (VendedorCpf) REFERENCES Usuarios(Cpf);

    CREATE INDEX IX_Reservas_VendedorCpf ON Reservas(VendedorCpf);
END
