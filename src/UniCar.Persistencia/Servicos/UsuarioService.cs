using Microsoft.EntityFrameworkCore;
using UniCar.Persistencia.Dados;
using UniCar.Persistencia.Entidades;

namespace UniCar.Persistencia.Servicos;

/// <summary>CRUD de usuários. Ao cadastrar, cria junto a linha de preferências (um para um).</summary>
public class UsuarioService(UniCarContext db)
{
    public Usuario Criar(Usuario usuario)
    {
        usuario.Email = usuario.Email.Trim().ToLowerInvariant();
        Validacao.Validar(usuario);
        if (db.Universidades.Find(usuario.UniversidadeId) is null)
            throw new ErroUniCar("Escolha uma faculdade da lista.");
        if (db.Usuarios.Any(u => u.Email == usuario.Email))
            throw new ErroUniCar("Já existe um usuário com esse e-mail.");

        usuario.Preferencias ??= new PreferenciasUsuario();
        db.Usuarios.Add(usuario);
        db.SaveChanges();
        return usuario;
    }

    public List<Usuario> Listar() =>
        db.Usuarios
            .Include(u => u.Universidade)
            .Include(u => u.Preferencias)
            .OrderBy(u => u.NomeCompleto)
            .ToList();

    public Usuario? BuscarPorId(int id) =>
        db.Usuarios
            .Include(u => u.Universidade)
            .Include(u => u.Preferencias)
            .Include(u => u.Veiculos)
            .FirstOrDefault(u => u.Id == id);

    public Usuario Atualizar(Usuario usuario)
    {
        usuario.Email = usuario.Email.Trim().ToLowerInvariant();
        Validacao.Validar(usuario);
        if (db.Usuarios.Any(u => u.Email == usuario.Email && u.Id != usuario.Id))
            throw new ErroUniCar("Já existe um usuário com esse e-mail.");

        db.Usuarios.Update(usuario);
        db.SaveChanges();
        return usuario;
    }

    /// <summary>Apaga o usuário, as preferências e os veículos (cascata). Quem tem caronas publicadas precisa excluí-las antes.</summary>
    public void Excluir(int id)
    {
        var usuario = db.Usuarios.Find(id) ?? throw new ErroUniCar("Usuário não encontrado.");
        if (db.Caronas.Any(c => c.MotoristaId == id))
            throw new ErroUniCar("Esse usuário tem caronas publicadas. Exclua as caronas antes.");

        // Passageiro → Reservas é Restrict no banco: as reservas dele saem primeiro.
        db.Reservas.RemoveRange(db.Reservas.Where(r => r.PassageiroId == id));
        db.Usuarios.Remove(usuario);
        db.SaveChanges();
    }
}
