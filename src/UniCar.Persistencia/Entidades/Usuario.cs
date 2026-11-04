using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace UniCar.Persistencia.Entidades;

/// <summary>Universitário. Todo mundo é passageiro; quem tem veículo também pode oferecer caronas.</summary>
[Index(nameof(Email), IsUnique = true)]
public class Usuario
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "Informe o nome completo.")]
    [MaxLength(120, ErrorMessage = "O nome tem no máximo 120 letras.")]
    public string NomeCompleto { get; set; } = "";

    [Required(ErrorMessage = "Informe o e-mail da faculdade.")]
    [EmailAddress(ErrorMessage = "E-mail inválido.")]
    [MaxLength(160, ErrorMessage = "O e-mail tem no máximo 160 letras.")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Informe o celular.")]
    [MaxLength(20, ErrorMessage = "O celular tem no máximo 20 caracteres.")]
    public string Telefone { get; set; } = "";

    [Required(ErrorMessage = "Informe o curso.")]
    [MaxLength(80, ErrorMessage = "O curso tem no máximo 80 letras.")]
    public string Curso { get; set; } = "";

    [Range(1, 12, ErrorMessage = "O período vai de 1 a 12.")]
    public int Periodo { get; set; } = 1;

    // Um para muitos (lado "muitos"): a FK aponta para a universidade.
    [ForeignKey(nameof(Universidade))]
    public int UniversidadeId { get; set; }
    public Universidade? Universidade { get; set; }

    // Um para um: cada usuário tem uma linha de preferências (PreferenciasUsuario.UsuarioId é PK e FK).
    public PreferenciasUsuario? Preferencias { get; set; }

    // Um para muitos: o motorista tem vários veículos.
    [InverseProperty(nameof(Veiculo.Motorista))]
    public List<Veiculo> Veiculos { get; set; } = new();

    // Um para muitos: o motorista oferece várias caronas.
    [InverseProperty(nameof(Carona.Motorista))]
    public List<Carona> CaronasOferecidas { get; set; } = new();

    // Muitos para muitos com Carona, pela entidade de junção Reserva (lado do passageiro).
    [InverseProperty(nameof(Reserva.Passageiro))]
    public List<Reserva> Reservas { get; set; } = new();
}
