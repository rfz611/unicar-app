using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using UniCar.Persistencia.Entidades;

namespace UniCar.Persistencia.Dados;

/// <summary>
/// Banco do UniCar em SQLite. Sem opções, usa o arquivo unicar.db da pasta onde o programa roda.
/// Os testes passam as opções prontas (SQLite em memória).
/// </summary>
public class UniCarContext : DbContext
{
    public const string ConexaoPadrao = "Data Source=unicar.db";

    /// <summary>Opção "Mostrar o SQL" do menu: liga e desliga o log dos comandos enviados ao banco.</summary>
    public static bool MostrarSql { get; set; }

    public UniCarContext() { }

    public UniCarContext(DbContextOptions<UniCarContext> options) : base(options) { }

    public DbSet<Universidade> Universidades => Set<Universidade>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<PreferenciasUsuario> Preferencias => Set<PreferenciasUsuario>();
    public DbSet<Veiculo> Veiculos => Set<Veiculo>();
    public DbSet<Carona> Caronas => Set<Carona>();
    public DbSet<Reserva> Reservas => Set<Reserva>();
    public DbSet<RegraCarona> Regras => Set<RegraCarona>();

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        if (!options.IsConfigured) options.UseSqlite(ConexaoPadrao);

        // Só os comandos SQL (INSERT, SELECT, UPDATE, DELETE), e só quando a opção está ligada.
        options.LogTo(
            mensagem => { if (MostrarSql) Escrever(mensagem); },
            new[] { DbLoggerCategory.Database.Command.Name },
            LogLevel.Information,
            // sem data e categoria em cada linha: fica só o comando, mais fácil de ler no vídeo
            DbContextLoggerOptions.None);
    }

    private static void Escrever(string mensagem)
    {
        var cor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine(mensagem.Trim());
        Console.ForegroundColor = cor;
    }

    /// <summary>
    /// Fluent API só onde a anotação não resolve: o que acontece ao excluir e os dados iniciais.
    /// O resto (chaves, FKs, índices, tamanhos) está nas anotações das entidades.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Carona → Reservas (um para muitos): excluir a carona apaga as reservas dela.
        modelBuilder.Entity<Reserva>()
            .HasOne(r => r.Carona)
            .WithMany(c => c.Reservas)
            .HasForeignKey(r => r.CaronaId)
            .OnDelete(DeleteBehavior.Cascade);

        // Passageiro → Reservas: Restrict. Com cascata aqui, excluir um usuário apagaria reservas por dois
        // caminhos (direto e pela carona), e o EF recusa caminhos múltiplos de cascata.
        modelBuilder.Entity<Reserva>()
            .HasOne(r => r.Passageiro)
            .WithMany(u => u.Reservas)
            .HasForeignKey(r => r.PassageiroId)
            .OnDelete(DeleteBehavior.Restrict);

        // Motorista → Caronas: Restrict, pelo mesmo motivo. Para excluir o usuário, exclua antes as caronas dele.
        modelBuilder.Entity<Carona>()
            .HasOne(c => c.Motorista)
            .WithMany(u => u.CaronasOferecidas)
            .HasForeignKey(c => c.MotoristaId)
            .OnDelete(DeleteBehavior.Restrict);

        // Veículo → Caronas: Restrict. Um carro com caronas marcadas não some sem aviso.
        modelBuilder.Entity<Carona>()
            .HasOne(c => c.Veiculo)
            .WithMany(v => v.Caronas)
            .HasForeignKey(c => c.VeiculoId)
            .OnDelete(DeleteBehavior.Restrict);

        // Universidade → Usuarios: Restrict, apagar uma faculdade não apaga os alunos.
        modelBuilder.Entity<Usuario>()
            .HasOne(u => u.Universidade)
            .WithMany(x => x.Usuarios)
            .HasForeignKey(u => u.UniversidadeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Universidade>().HasData(
            new Universidade { Id = 1, Nome = "Universidade Federal do Paraná", Sigla = "UFPR", DominioEmail = "ufpr.br" },
            new Universidade { Id = 2, Nome = "Pontifícia Universidade Católica do Paraná", Sigla = "PUCPR", DominioEmail = "pucpr.edu.br" },
            new Universidade { Id = 3, Nome = "Universidade Tecnológica Federal do Paraná", Sigla = "UTFPR", DominioEmail = "utfpr.edu.br" },
            new Universidade { Id = 4, Nome = "Universidade Positivo", Sigla = "Positivo", DominioEmail = "up.edu.br" },
            new Universidade { Id = 5, Nome = "FAE Centro Universitário", Sigla = "FAE", DominioEmail = "fae.edu.br" });

        modelBuilder.Entity<RegraCarona>().HasData(
            new RegraCarona { Id = 1, Nome = "Não fumante" },
            new RegraCarona { Id = 2, Nome = "Música ambiente" },
            new RegraCarona { Id = 3, Nome = "Aceita pet" });
    }
}
