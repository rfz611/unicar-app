using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace UniCar.Persistencia.Entidades;

public enum StatusReserva { Confirmada, Cancelada }

/// <summary>
/// Entidade de junção do muitos para muitos entre Usuario (passageiro) e Carona. Além das duas FKs, guarda
/// dados próprios: preço, status e data. Uma reserva por passageiro em cada carona (índice único).
/// </summary>
[Index(nameof(CaronaId), nameof(PassageiroId), IsUnique = true)]
public class Reserva
{
    [Key]
    public int Id { get; set; }

    [ForeignKey(nameof(Carona))]
    public int CaronaId { get; set; }
    public Carona? Carona { get; set; }

    [ForeignKey(nameof(Passageiro))]
    public int PassageiroId { get; set; }
    public Usuario? Passageiro { get; set; }

    [Range(typeof(decimal), "0", "500", ErrorMessage = "O preço vai de R$ 0 a R$ 500.")]
    public decimal Preco { get; set; }

    public StatusReserva Status { get; set; } = StatusReserva.Confirmada;

    public DateTime CriadaEm { get; set; } = DateTime.Now;
}
