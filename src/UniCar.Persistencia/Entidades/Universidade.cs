using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace UniCar.Persistencia.Entidades;

/// <summary>Faculdade de onde vem o e-mail do universitário (ex.: UFPR, ufpr.br).</summary>
[Index(nameof(Sigla), IsUnique = true)]
public class Universidade
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "Informe o nome da faculdade.")]
    [MaxLength(120, ErrorMessage = "O nome da faculdade tem no máximo 120 letras.")]
    public string Nome { get; set; } = "";

    [Required(ErrorMessage = "Informe a sigla da faculdade.")]
    [MaxLength(20, ErrorMessage = "A sigla tem no máximo 20 letras.")]
    public string Sigla { get; set; } = "";

    /// <summary>Final do e-mail institucional, ex.: up.edu.br.</summary>
    [MaxLength(60)]
    public string? DominioEmail { get; set; }

    // Um para muitos: uma universidade tem vários usuários (Usuario.UniversidadeId é a FK).
    public List<Usuario> Usuarios { get; set; } = new();
}
