using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace UniCar.Persistencia.Entidades;

/// <summary>Carro do motorista.</summary>
[Index(nameof(Placa), IsUnique = true)]
public class Veiculo
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "Informe a placa.")]
    [RegularExpression(@"^[A-Z]{3}-?[0-9][A-Z0-9][0-9]{2}$", ErrorMessage = "Placa inválida. Ex.: ABC1D23 ou ABC1234.")]
    [MaxLength(8)]
    public string Placa { get; set; } = "";

    [Required(ErrorMessage = "Informe o modelo do carro.")]
    [MaxLength(60, ErrorMessage = "O modelo tem no máximo 60 letras.")]
    public string Modelo { get; set; } = "";

    [Required(ErrorMessage = "Informe a cor do carro.")]
    [MaxLength(30, ErrorMessage = "A cor tem no máximo 30 letras.")]
    public string Cor { get; set; } = "";

    // Um para muitos (lado "muitos"): o veículo pertence a um motorista.
    [ForeignKey(nameof(Motorista))]
    public int MotoristaId { get; set; }
    public Usuario? Motorista { get; set; }

    // Um para muitos: o veículo é usado em várias caronas.
    public List<Carona> Caronas { get; set; } = new();
}
