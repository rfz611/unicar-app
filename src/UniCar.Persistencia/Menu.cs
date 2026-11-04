using Microsoft.EntityFrameworkCore;
using UniCar.Persistencia.Dados;
using UniCar.Persistencia.Entidades;
using UniCar.Persistencia.Servicos;

namespace UniCar.Persistencia;

/// <summary>Menu do console: um submenu por entidade, mais "Mostrar o SQL" e "Resumo do banco".</summary>
public class Menu(UniCarContext db)
{
    private readonly UsuarioService usuarios = new(db);
    private readonly VeiculoService veiculos = new(db);
    private readonly CaronaService caronas = new(db);
    private readonly ReservaService reservas = new(db);

    public void Rodar()
    {
        while (true)
        {
            Tela.Titulo("UniCar — persistência com EF Core e SQLite");
            Console.WriteLine("1. Usuários");
            Console.WriteLine("2. Veículos");
            Console.WriteLine("3. Caronas");
            Console.WriteLine("4. Reservas");
            Console.WriteLine($"5. Mostrar o SQL ({(UniCarContext.MostrarSql ? "ligado" : "desligado")})");
            Console.WriteLine("6. Resumo do banco");
            Console.WriteLine("0. Sair");
            switch (Tela.Texto("Escolha"))
            {
                case "1": MenuUsuarios(); break;
                case "2": MenuVeiculos(); break;
                case "3": MenuCaronas(); break;
                case "4": MenuReservas(); break;
                case "5":
                    UniCarContext.MostrarSql = !UniCarContext.MostrarSql;
                    Tela.Ok(UniCarContext.MostrarSql ? "Os comandos SQL vão aparecer na tela." : "SQL escondido.");
                    break;
                case "6": Resumo(); break;
                case "0": return;
                default: Tela.Erro("Opção inválida."); break;
            }
        }
    }

    /// <summary>Roda uma ação e mostra o erro em português, sem derrubar o programa.</summary>
    private void Tentar(Action acao)
    {
        try
        {
            acao();
        }
        catch (ErroUniCar e)
        {
            Tela.Erros(e);
            db.ChangeTracker.Clear();
        }
        catch (DbUpdateException e)
        {
            Tela.Erro("  O banco recusou a operação: " + (e.InnerException?.Message ?? e.Message));
            db.ChangeTracker.Clear();
        }
    }

    // ---------- usuários ----------

    private void MenuUsuarios()
    {
        Tela.Titulo("Usuários");
        Console.WriteLine("1. Cadastrar  2. Listar  3. Editar  4. Excluir  0. Voltar");
        switch (Tela.Texto("Escolha"))
        {
            case "1": Tentar(CadastrarUsuario); break;
            case "2": ListarUsuarios(); break;
            case "3": Tentar(EditarUsuario); break;
            case "4": Tentar(() => { usuarios.Excluir(Tela.Numero("Id do usuário")); Tela.Ok("Usuário excluído."); }); break;
        }
    }

    private void CadastrarUsuario()
    {
        var unis = db.Universidades.OrderBy(u => u.Id).ToList();
        foreach (var u in unis) Console.WriteLine($"  {u.Id}. {u.Sigla} ({u.DominioEmail})");
        var usuario = new Usuario
        {
            UniversidadeId = Tela.Numero("Faculdade"),
            NomeCompleto = Tela.Texto("Nome completo"),
            Email = Tela.Texto("E-mail da faculdade"),
            Telefone = Tela.Texto("Celular"),
            Curso = Tela.Texto("Curso"),
            Periodo = Tela.Numero("Período (1 a 12)"),
        };
        usuarios.Criar(usuario);
        Tela.Ok($"Usuário #{usuario.Id} cadastrado, com as preferências padrão.");
    }

    private void ListarUsuarios()
    {
        var lista = usuarios.Listar();
        if (lista.Count == 0) Console.WriteLine("Nenhum usuário ainda.");
        foreach (var u in lista)
            Console.WriteLine($"#{u.Id} {u.NomeCompleto} | {u.Email} | {u.Curso}, {u.Periodo}º período | {u.Universidade?.Sigla}" +
                              $" | tema {(u.Preferencias?.TemaEscuro == false ? "claro" : "escuro")}");
    }

    private void EditarUsuario()
    {
        var u = usuarios.BuscarPorId(Tela.Numero("Id do usuário")) ?? throw new ErroUniCar("Usuário não encontrado.");
        Console.WriteLine("Enter mantém o valor entre colchetes.");
        u.NomeCompleto = Tela.Texto("Nome completo", u.NomeCompleto);
        u.Email = Tela.Texto("E-mail", u.Email);
        u.Telefone = Tela.Texto("Celular", u.Telefone);
        u.Curso = Tela.Texto("Curso", u.Curso);
        u.Periodo = Tela.Numero("Período", u.Periodo);
        usuarios.Atualizar(u);
        Tela.Ok("Usuário atualizado.");
    }

    // ---------- veículos ----------

    private void MenuVeiculos()
    {
        Tela.Titulo("Veículos");
        Console.WriteLine("1. Cadastrar  2. Listar  3. Excluir  0. Voltar");
        switch (Tela.Texto("Escolha"))
        {
            case "1":
                Tentar(() =>
                {
                    var v = new Veiculo
                    {
                        MotoristaId = Tela.Numero("Id do motorista"),
                        Placa = Tela.Texto("Placa (ex.: ABC1D23)"),
                        Modelo = Tela.Texto("Modelo"),
                        Cor = Tela.Texto("Cor"),
                    };
                    veiculos.Criar(v);
                    Tela.Ok($"Veículo #{v.Id} cadastrado.");
                });
                break;
            case "2":
                var lista = veiculos.Listar();
                if (lista.Count == 0) Console.WriteLine("Nenhum veículo ainda.");
                foreach (var v in lista) Console.WriteLine($"#{v.Id} {v.Placa} | {v.Modelo} {v.Cor} | motorista: {v.Motorista?.NomeCompleto}");
                break;
            case "3": Tentar(() => { veiculos.Excluir(Tela.Numero("Id do veículo")); Tela.Ok("Veículo excluído."); }); break;
        }
    }

    // ---------- caronas ----------

    private void MenuCaronas()
    {
        Tela.Titulo("Caronas");
        Console.WriteLine("1. Publicar  2. Listar  3. Editar horário e vagas  4. Excluir  0. Voltar");
        switch (Tela.Texto("Escolha"))
        {
            case "1": Tentar(PublicarCarona); break;
            case "2": ListarCaronas(); break;
            case "3": Tentar(EditarCarona); break;
            case "4":
                Tentar(() =>
                {
                    caronas.Excluir(Tela.Numero("Id da carona"));
                    Tela.Ok("Carona excluída, junto com as reservas dela.");
                });
                break;
        }
    }

    private void PublicarCarona()
    {
        var motoristaId = Tela.Numero("Id do motorista");
        var carros = db.Veiculos.Where(v => v.MotoristaId == motoristaId).ToList();
        if (carros.Count == 0) throw new ErroUniCar("Esse usuário não tem veículo. Cadastre um em Veículos.");
        foreach (var v in carros) Console.WriteLine($"  {v.Id}. {v.Modelo} {v.Cor} ({v.Placa})");
        var veiculoId = Tela.Numero("Veículo");

        var carona = new Carona
        {
            MotoristaId = motoristaId,
            VeiculoId = veiculoId,
            Origem = Tela.Texto("Saindo de"),
            Destino = Tela.Texto("Indo para"),
            SaidaEm = Tela.DataHora("Saída"),
            Vagas = Tela.Numero("Vagas (1 a 4)"),
            PrecoPorVaga = Tela.Dinheiro("Preço por vaga (R$)"),
        };

        foreach (var r in db.Regras.OrderBy(r => r.Id)) Console.WriteLine($"  {r.Id}. {r.Nome}");
        var regras = Tela.Texto("Regras (números separados por vírgula, Enter para nenhuma)")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => int.TryParse(x, out var n) ? n : 0)
            .Where(n => n > 0);

        caronas.Criar(carona, regras);
        Tela.Ok($"Carona #{carona.Id} publicada.");
    }

    private void ListarCaronas()
    {
        var lista = caronas.Listar();
        if (lista.Count == 0) Console.WriteLine("Nenhuma carona ainda.");
        foreach (var c in lista) Console.WriteLine(Tela.Linha(c));
    }

    private void EditarCarona()
    {
        var c = caronas.BuscarPorId(Tela.Numero("Id da carona")) ?? throw new ErroUniCar("Carona não encontrada.");
        Console.WriteLine("Enter mantém o valor entre colchetes.");
        c.SaidaEm = Tela.DataHora("Saída", c.SaidaEm);
        c.Vagas = Tela.Numero("Vagas", c.Vagas);
        caronas.Atualizar(c);
        Tela.Ok("Carona atualizada.");
    }

    // ---------- reservas ----------

    private void MenuReservas()
    {
        Tela.Titulo("Reservas");
        Console.WriteLine("1. Reservar vaga  2. Listar por carona  3. Cancelar  0. Voltar");
        switch (Tela.Texto("Escolha"))
        {
            case "1":
                Tentar(() =>
                {
                    var r = reservas.Criar(Tela.Numero("Id da carona"), Tela.Numero("Id do passageiro"));
                    Tela.Ok($"Reserva #{r.Id} confirmada por {Tela.Brl(r.Preco)}.");
                });
                break;
            case "2":
                var lista = reservas.ListarPorCarona(Tela.Numero("Id da carona"));
                if (lista.Count == 0) Console.WriteLine("Nenhuma reserva nessa carona.");
                foreach (var r in lista)
                    Console.WriteLine($"#{r.Id} {r.Passageiro?.NomeCompleto} | {Tela.Brl(r.Preco)} | {r.Status} | {r.CriadaEm:dd/MM HH:mm}");
                break;
            case "3": Tentar(() => { reservas.Cancelar(Tela.Numero("Id da reserva")); Tela.Ok("Reserva cancelada, vaga liberada."); }); break;
        }
    }

    // ---------- resumo ----------

    public static void Resumo(UniCarContext db)
    {
        Tela.Titulo("Resumo do banco");
        Console.WriteLine($"Universidades ....... {db.Universidades.Count()}");
        Console.WriteLine($"Usuarios ............ {db.Usuarios.Count()}");
        Console.WriteLine($"Preferencias ........ {db.Preferencias.Count()}");
        Console.WriteLine($"Veiculos ............ {db.Veiculos.Count()}");
        Console.WriteLine($"Caronas ............. {db.Caronas.Count()}");
        Console.WriteLine($"Reservas ............ {db.Reservas.Count()}");
        Console.WriteLine($"Regras .............. {db.Regras.Count()}");
        Console.WriteLine($"CaronaRegraCarona ... {db.Caronas.SelectMany(c => c.Regras).Count()}  (junção criada pelo EF)");
    }

    private void Resumo() => Resumo(db);
}
