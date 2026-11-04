using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UniCar.Persistencia.Entidades;

/// <summary>Tela de Configurações do app. Uma linha por usuário.</summary>
public class PreferenciasUsuario
{
    // Um para um: a mesma coluna é a chave primária desta tabela e a chave estrangeira para Usuario.
    [Key]
    [ForeignKey(nameof(Usuario))]
    public int UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    public bool TemaEscuro { get; set; } = true;
    public bool AvisosDeCarona { get; set; } = true;
    public bool MostrarCurso { get; set; } = true;
}
