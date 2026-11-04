using System.ComponentModel.DataAnnotations;

namespace UniCar.Persistencia.Entidades;

/// <summary>Combinado da carona, como "Não fumante" ou "Aceita pet".</summary>
public class RegraCarona
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "Informe o nome da regra.")]
    [MaxLength(60, ErrorMessage = "A regra tem no máximo 60 letras.")]
    public string Nome { get; set; } = "";

    // Muitos para muitos simples com Carona (coleção dos dois lados).
    public List<Carona> Caronas { get; set; } = new();
}
