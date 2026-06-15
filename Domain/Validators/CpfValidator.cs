using Domain.Exceptions;

namespace Domain.Validators;

/// <summary>
/// Validador estático de CPF reutilizável em todo o Domain.
/// </summary>
public static class CpfValidator
{
    /// <summary>
    /// Valida se o CPF é válido (11 dígitos, não todos iguais, dígitos verificadores).
    /// Lança CpfVazio ou CpfInvalido em caso de erro.
    /// </summary>
    public static void Validar(string cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf))
            throw new CpfVazio();

        var apenasDigitos = new string(cpf.Where(char.IsDigit).ToArray());

        if (apenasDigitos.Length != 11)
            throw new CpfInvalido();

        if (apenasDigitos.Distinct().Count() == 1)
            throw new CpfInvalido();

        if (!DigitosVerificadoresSaoValidos(apenasDigitos))
            throw new CpfInvalido();
    }

    private static bool DigitosVerificadoresSaoValidos(string cpf)
    {
        int soma = 0;
        for (int i = 0; i < 9; i++)
            soma += (cpf[i] - '0') * (10 - i);

        int resto = soma % 11;
        int digito1 = resto < 2 ? 0 : 11 - resto;

        if (cpf[9] - '0' != digito1)
            return false;

        soma = 0;
        for (int i = 0; i < 10; i++)
            soma += (cpf[i] - '0') * (11 - i);

        resto = soma % 11;
        int digito2 = resto < 2 ? 0 : 11 - resto;

        return cpf[10] - '0' == digito2;
    }
}
