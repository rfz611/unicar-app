using System.ComponentModel.DataAnnotations;

namespace UniCar.Persistencia.Servicos;

/// <summary>Erro que o usuário pode corrigir (dado inválido ou regra da carona). As mensagens já estão em português.</summary>
public class ErroUniCar : Exception
{
    public IReadOnlyList<string> Erros { get; }

    public ErroUniCar(params string[] erros) : base(string.Join(" ", erros)) => Erros = erros;
}

public static class Validacao
{
    /// <summary>
    /// Confere as Data Annotations ([Required], [MaxLength], [Range], [EmailAddress]...) antes de salvar.
    /// O EF não faz isso sozinho: sem esta chamada, um texto vazio iria para o banco.
    /// </summary>
    public static void Validar(object entidade)
    {
        var resultados = new List<ValidationResult>();
        var contexto = new ValidationContext(entidade);
        try
        {
            Validator.ValidateObject(entidade, contexto, validateAllProperties: true);
        }
        catch (ValidationException)
        {
            // ValidateObject para no primeiro erro; TryValidateObject junta todos para mostrar de uma vez.
            Validator.TryValidateObject(entidade, contexto, resultados, validateAllProperties: true);
            throw new ErroUniCar(resultados.Select(r => r.ErrorMessage ?? "Dado inválido.").ToArray());
        }
    }
}
