using UniCar.Persistencia.Dados;
using UniCar.Persistencia.Entidades;
using UniCar.Persistencia.Servicos;

namespace UniCar.Persistencia;

/// <summary>
/// Modo automático (dotnet run -- --demo): passa pelo CRUD inteiro mostrando o SQL de cada passo.
/// No fim apaga o que criou, então dá para rodar quantas vezes quiser.
/// </summary>
public static class Demo
{
    public static void Rodar(UniCarContext db)
    {
        var usuarios = new UsuarioService(db);
        var veiculos = new VeiculoService(db);
        var caronas = new CaronaService(db);
        var reservas = new ReservaService(db);
        // e-mail e placa diferentes a cada execução, por causa dos índices únicos
        var n = Random.Shared.Next(100, 999);
        UniCarContext.MostrarSql = true;

        Passo("1. Criar o motorista (INSERT em Usuarios e em Preferencias, um para um)");
        var bruno = usuarios.Criar(new Usuario
        {
            NomeCompleto = "Bruno Lima", Email = $"bruno{n}@ufpr.br", Telefone = "(41) 99999-0001",
            Curso = "Engenharia de Software", Periodo = 7, UniversidadeId = 1,
        });
        Tela.Ok($"Usuário #{bruno.Id} criado.");

        Passo("2. Criar o veículo do motorista (INSERT em Veiculos)");
        var onix = veiculos.Criar(new Veiculo { MotoristaId = bruno.Id, Placa = $"BRU{n % 10}D{n % 100:00}", Modelo = "Chevrolet Onix", Cor = "Prata" });
        Tela.Ok($"Veículo #{onix.Id} criado, placa {onix.Placa}.");

        Passo("3. Publicar a carona com duas regras (INSERT em Caronas e na junção CaronaRegraCarona)");
        var carona = caronas.Criar(new Carona
        {
            MotoristaId = bruno.Id, VeiculoId = onix.Id,
            Origem = "Batel", Destino = "UFPR Centro Politécnico",
            SaidaEm = DateTime.Today.AddDays(1).AddHours(7).AddMinutes(30),
            Vagas = 3, PrecoPorVaga = 9.50m,
        }, new[] { 1, 2 });
        Tela.Ok($"Carona #{carona.Id} publicada.");

        Passo("4. Listar as caronas (SELECT com JOIN: Include de motorista, veículo, reservas e regras)");
        foreach (var c in caronas.Listar()) Console.WriteLine(Tela.Linha(c));

        Passo("5. Mudar o horário para 10 minutos depois (UPDATE em Caronas)");
        var paraEditar = caronas.BuscarPorId(carona.Id)!;
        paraEditar.SaidaEm = paraEditar.SaidaEm.AddMinutes(10);
        caronas.Atualizar(paraEditar);
        Tela.Ok($"Nova saída: {paraEditar.SaidaEm:dd/MM HH:mm}.");

        Passo("6. Criar a passageira e reservar uma vaga (INSERT em Usuarios e em Reservas)");
        var ana = usuarios.Criar(new Usuario
        {
            NomeCompleto = "Ana Ribeiro", Email = $"ana{n}@ufpr.br", Telefone = "(41) 99999-0002",
            Curso = "Direito", Periodo = 4, UniversidadeId = 1,
        });
        var reserva = reservas.Criar(carona.Id, ana.Id);
        Tela.Ok($"Reserva #{reserva.Id} confirmada por {Tela.Brl(reserva.Preco)}.");

        Passo("7. Regra: o motorista tenta reservar a própria carona");
        try
        {
            reservas.Criar(carona.Id, bruno.Id);
        }
        catch (ErroUniCar e)
        {
            Tela.Erros(e);
        }

        Passo("8. Regra: validação das anotações (usuário sem nome e com período 15)");
        try
        {
            usuarios.Criar(new Usuario { Email = "x@ufpr.br", Telefone = "41", Curso = "Direito", Periodo = 15, UniversidadeId = 1 });
        }
        catch (ErroUniCar e)
        {
            Tela.Erros(e);
        }

        Passo("9. Ver a carona com a reserva (SELECT com ThenInclude do passageiro)");
        var completa = caronas.BuscarPorId(carona.Id)!;
        Console.WriteLine(Tela.Linha(completa));
        foreach (var r in completa.Reservas) Console.WriteLine($"     Reserva #{r.Id}: {r.Passageiro?.NomeCompleto}, {Tela.Brl(r.Preco)}");

        Passo("10. Excluir a carona (DELETE em Caronas; as reservas vão junto, em cascata)");
        caronas.Excluir(carona.Id);
        Tela.Ok($"Carona excluída. Reservas que sobraram dela: {db.Reservas.Count(r => r.CaronaId == carona.Id)}.");

        Passo("11. Limpar o que o demo criou (DELETE do veículo e dos dois usuários)");
        veiculos.Excluir(onix.Id);
        usuarios.Excluir(ana.Id);
        usuarios.Excluir(bruno.Id);
        Tela.Ok("Pronto.");

        UniCarContext.MostrarSql = false;
        Menu.Resumo(db);
    }

    private static void Passo(string texto)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(texto);
        Console.ResetColor();
    }
}
