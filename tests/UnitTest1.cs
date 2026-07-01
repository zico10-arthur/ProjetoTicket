namespace SoldOutTickets.Tests;

public class UnitTest1
{
    [Fact]
    public void Soma_DoisNumerosInteiros_DeveRetornarResultadoCorreto()
    {
        // Arrange
        var esperado = 3;

        // Act
        var resultado = 1 + 2;

        // Assert
        Assert.Equal(esperado, resultado);
    }
}
