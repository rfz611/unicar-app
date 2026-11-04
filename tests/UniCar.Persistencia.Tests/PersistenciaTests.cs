using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using UniCar.Persistencia.Dados;
using UniCar.Persistencia.Entidades;
using UniCar.Persistencia.Servicos;
using Xunit;

namespace UniCar.Persistencia.Tests;

/// <summary>
/// Cada teste usa um banco SQLite novo, só na memória. A conexão fica aberta o teste inteiro:
/// no SQLite em memória, fechar a conexão apaga o banco.
/// </summary>
public sealed class PersistenciaTests : IDisposable
{
    private readonly SqliteConnection conexao = new("DataSource=:memory:");

    public PersistenciaTests()
    {
        conexao.Open();
        using var db = NovoContexto();
        db.Database.Migrate();
    }

    public void Dispose() => conexao.Dispose();

    /// <summary>Contexto novo na mesma conexão: lê do banco, não do que o contexto anterior guardou na memória.</summary>
    private UniCarContext NovoContexto() =>
        new(new DbContextOptionsBuilder<UniCarContext>().UseSqlite(conexao).Options);

    private static Usuario NovoUsuario(string nome, string email) => new()
    {
        NomeCompleto = nome, Email = email, Telefone = "(41) 99999-0000", Curso = "Direito", Periodo = 3, UniversidadeId = 1,
    };

    /// <summary>Motorista com carro e uma carona de 2 vagas com as regras 1 e 3.</summary>
    private (int MotoristaId, int CaronaId) CriarCarona()
    {
        using var db = NovoContexto();
        var motorista = new UsuarioService(db).Criar(NovoUsuario("Bruno Lima", "bruno@ufpr.br"));
        var carro = new VeiculoService(db).Criar(new Veiculo { MotoristaId = motorista.Id, Placa = "bru1d23", Modelo = "Onix", Cor = "Prata" });
        var carona = new CaronaService(db).Criar(new Carona
        {
            MotoristaId = motorista.Id, VeiculoId = carro.Id, Origem = "Batel", Destino = "UFPR Politécnico",
            SaidaEm = new DateTime(2026, 11, 5, 7, 30, 0), Vagas = 2, PrecoPorVaga = 9.5m,
        }, new[] { 1, 3 });
        return (motorista.Id, carona.Id);
    }

    [Fact]
    public void Cria_e_le_carona_com_motorista_e_regras()
    {
        var (_, caronaId) = CriarCarona();

        using var db = NovoContexto();
        var carona = new CaronaService(db).BuscarPorId(caronaId);

        Assert.NotNull(carona);
        Assert.Equal("Bruno Lima", carona.Motorista!.NomeCompleto);
        Assert.Equal("BRU1D23", carona.Veiculo!.Placa);
        Assert.Equal(new[] { "Aceita pet", "Não fumante" }, carona.Regras.Select(r => r.Nome).OrderBy(n => n));
        Assert.Equal(2, carona.VagasLivres);
        // o um para um foi criado junto com o usuário
        Assert.NotNull(db.Preferencias.Find(carona.MotoristaId));
    }

    [Fact]
    public void Atualiza_horario_e_vagas()
    {
        var (_, caronaId) = CriarCarona();

        using (var db = NovoContexto())
        {
            var servico = new CaronaService(db);
            var carona = servico.BuscarPorId(caronaId)!;
            carona.SaidaEm = carona.SaidaEm.AddMinutes(10);
            carona.Vagas = 4;
            servico.Atualizar(carona);
        }

        using var leitura = NovoContexto();
        var salva = leitura.Caronas.Find(caronaId)!;
        Assert.Equal(new DateTime(2026, 11, 5, 7, 40, 0), salva.SaidaEm);
        Assert.Equal(4, salva.Vagas);
    }

    [Fact]
    public void Excluir_carona_apaga_as_reservas_dela()
    {
        var (_, caronaId) = CriarCarona();
        using (var db = NovoContexto())
        {
            var ana = new UsuarioService(db).Criar(NovoUsuario("Ana Ribeiro", "ana@ufpr.br"));
            new ReservaService(db).Criar(caronaId, ana.Id);
        }

        using (var db = NovoContexto())
        {
            Assert.Equal(1, db.Reservas.Count(r => r.CaronaId == caronaId));
            new CaronaService(db).Excluir(caronaId);
        }

        using var leitura = NovoContexto();
        Assert.Null(leitura.Caronas.Find(caronaId));
        Assert.Equal(0, leitura.Reservas.Count());
        // os usuários continuam
        Assert.Equal(2, leitura.Usuarios.Count());
    }

    [Fact]
    public void Indice_unico_nao_deixa_dois_usuarios_com_o_mesmo_email()
    {
        using (var db = NovoContexto())
        {
            db.Usuarios.Add(NovoUsuario("Ana Ribeiro", "ana@ufpr.br"));
            db.SaveChanges();
        }

        // direto no banco: o índice único recusa
        using (var db = NovoContexto())
        {
            db.Usuarios.Add(NovoUsuario("Outra Ana", "ana@ufpr.br"));
            var erro = Assert.Throws<DbUpdateException>(() => db.SaveChanges());
            Assert.Contains("UNIQUE", erro.InnerException!.Message);
        }

        // pelo serviço: mensagem em português
        using (var db = NovoContexto())
        {
            var erro = Assert.Throws<ErroUniCar>(() => new UsuarioService(db).Criar(NovoUsuario("Outra Ana", "ANA@ufpr.br")));
            Assert.Equal("Já existe um usuário com esse e-mail.", erro.Message);
        }
    }

    [Fact]
    public void Required_barra_usuario_sem_nome()
    {
        using var db = NovoContexto();
        var erro = Assert.Throws<ErroUniCar>(() => new UsuarioService(db).Criar(NovoUsuario("", "sem.nome@ufpr.br")));

        Assert.Contains("Informe o nome completo.", erro.Erros);
        Assert.Equal(0, db.Usuarios.Count());
    }

    [Fact]
    public void Motorista_nao_reserva_a_propria_carona_e_nao_passa_das_vagas()
    {
        var (motoristaId, caronaId) = CriarCarona();
        using var db = NovoContexto();
        var usuarios = new UsuarioService(db);
        var reservas = new ReservaService(db);

        var propria = Assert.Throws<ErroUniCar>(() => reservas.Criar(caronaId, motoristaId));
        Assert.Equal("Você não pode reservar a sua própria carona.", propria.Message);

        reservas.Criar(caronaId, usuarios.Criar(NovoUsuario("Ana Ribeiro", "ana@ufpr.br")).Id);
        reservas.Criar(caronaId, usuarios.Criar(NovoUsuario("Carla Souza", "carla@ufpr.br")).Id);
        var lotada = Assert.Throws<ErroUniCar>(() => reservas.Criar(caronaId, usuarios.Criar(NovoUsuario("Diego Martins", "diego@ufpr.br")).Id));
        Assert.Equal("As vagas dessa carona acabaram.", lotada.Message);
    }

    [Fact]
    public void Vagas_fora_de_1_a_4_sao_recusadas()
    {
        var (_, caronaId) = CriarCarona();
        using var db = NovoContexto();
        var servico = new CaronaService(db);
        var carona = servico.BuscarPorId(caronaId)!;
        carona.Vagas = 5;

        var erro = Assert.Throws<ErroUniCar>(() => servico.Atualizar(carona));
        Assert.Contains("A carona tem de 1 a 4 vagas.", erro.Erros);
    }
}
