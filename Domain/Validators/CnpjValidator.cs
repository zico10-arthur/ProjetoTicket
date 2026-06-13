using Domain.Exceptions;

namespace Domain.Validators;

/// <summary>
/// ST-01: Validador de CNPJ com dígitos verificadores oficiais.
/// </summary>
public static class CnpjValidator
{
    public static void Validar(string cnpj)
    {
        if (string.IsNullOrWhiteSpace(cnpj))
            throw new CnpjInvalido();

        var cnpjLimpo = cnpj
            .Replace(".", "")
            .Replace("-", "")
            .Replace("/", "")
            .Trim();

        if (cnpjLimpo.Length != 14)
            throw new CnpjInvalido();

        if (!cnpjLimpo.All(char.IsDigit))
            throw new CnpjInvalido();

        if (cnpjLimpo.Distinct().Count() == 1)
            throw new CnpjInvalido();

        if (!DigitosVerificadoresValidos(cnpjLimpo))
            throw new CnpjInvalido();
    }

    private static bool DigitosVerificadoresValidos(string cnpj)
    {
        int[] multiplicadores1 = { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        int[] multiplicadores2 = { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };

        int soma = 0;
        for (int i = 0; i < 12; i++)
            soma += (cnpj[i] - '0') * multiplicadores1[i];

        int resto = soma % 11;
        int digito1 = resto < 2 ? 0 : 11 - resto;

        soma = 0;
        for (int i = 0; i < 13; i++)
            soma += (cnpj[i] - '0') * multiplicadores2[i];

        resto = soma % 11;
        int digito2 = resto < 2 ? 0 : 11 - resto;

        return cnpj[12] - '0' == digito1 && cnpj[13] - '0' == digito2;
    }
}
