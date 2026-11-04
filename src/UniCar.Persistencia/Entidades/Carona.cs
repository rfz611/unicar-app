using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UniCar.Persistencia.Entidades;

public enum StatusCarona { Marcada, Cancelada }

/// <summary>Carona marcada: o motorista publica saída, destino, horário e vagas.</summary>
public class Carona
{
    [Key]
    public int Id { get; set; }

    // Um para muitos (lado "muitos"): a carona é de um motorista.
    [ForeignKey(nameof(Motorista))]
    public int MotoristaId { get; set; }
    public Usuario? Motorista { get; set; }

    // Um para muitos (lado "muitos"): a carona usa um veículo.
    [ForeignKey(nameof(Veiculo))]
    public int VeiculoId { get; set; }
    public Veiculo? Veiculo { get; set; }

    [Required(ErrorMessage = "Informe de onde a carona sai.")]
    [MaxLength(200, ErrorMessage = "A origem tem no máximo 200 letras.")]
    public string Origem { get; set; } = "";

    [Required(ErrorMessage = "Informe para onde a carona vai.")]
    [MaxLength(200, ErrorMessage = "O destino tem no máximo 200 letras.")]
    public string Destino { get; set; } = "";

    /// <summary>DateTime (e não DateTimeOffset): o SQLite não sabe ordenar DateTimeOffset.</summary>
    public DateTime SaidaEm { get; set; }

    [Range(1, 4, ErrorMessage = "A carona tem de 1 a 4 vagas.")]
    public int Vagas { get; set; } = 1;

    [Range(typeof(decimal), "0", "500", ErrorMessage = "O preço por vaga vai de R$ 0 a R$ 500.")]
    public decimal PrecoPorVaga { get; set; }

    public StatusCarona Status { get; set; } = StatusCarona.Marcada;

    // Um para muitos: a carona tem várias reservas (excluir a carona apaga as reservas, ver OnModelCreating).
    public List<Reserva> Reservas { get; set; } = new();

    // Muitos para muitos simples com RegraCarona: o EF cria a tabela de junção CaronaRegraCarona sozinho.
    public List<RegraCarona> Regras { get; set; } = new();

    /// <summary>Vagas ainda livres (não vai para o banco). Precisa das Reservas carregadas.</summary>
    [NotMapped]
    public int VagasLivres => Vagas - Reservas.Count(r => r.Status == StatusReserva.Confirmada);
}
