CREATE TABLE Cupons (
    Codigo NVARCHAR(49) NOT NULL,
    PorcentagemDesconto INT NOT NULL,
    ValorMinimo NUMERIC(10,2) NOT NULL,
    DataExpiracao DATETIME NOT NULL,
    Ativo BIT NOT NULL DEFAULT 1
);