using System.Globalization;
using UniCar.Persistencia.Entidades;
using UniCar.Persistencia.Servicos;

namespace UniCar.Persistencia;

/// <summary>Leitura do teclado e escrita no console, com as mensagens em português.</summary>
public static class Tela
{
    public static void Titulo(string texto)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine($"== {texto} ==");
        Console.ResetColor();
    }

    public static void Ok(string texto) => Cor(ConsoleColor.Green, texto);

    public static void Erro(string texto) => Cor(ConsoleColor.Red, texto);

    public static void Erros(ErroUniCar erro)
    {
        foreach (var e in erro.Erros) Erro("  " + e);
    }

    private static void Cor(ConsoleColor cor, string texto)
    {
        Console.ForegroundColor = cor;
        Console.WriteLine(texto);
        Console.ResetColor();
    }

    public static string Texto(string pergunta, string? atual = null)
    {
        Console.Write(atual is null ? $"{pergunta}: " : $"{pergunta} [{atual}]: ");
        var lido = Console.ReadLine()?.Trim() ?? "";
        return lido.Length == 0 && atual is not null ? atual : lido;
    }

    public static int Numero(string pergunta, int? atual = null)
    {
        while (true)
        {
            var lido = Texto(pergunta, atual?.ToString());
            if (int.TryParse(lido, out var n)) return n;
            Erro("  Digite um número.");
        }
    }

    public static decimal Dinheiro(string pergunta, decimal? atual = null)
    {
        while (true)
        {
            var lido = Texto(pergunta, atual?.ToString("0.00")).Replace("R$", "").Trim();
            if (decimal.TryParse(lido, NumberStyles.Number, CultureInfo.GetCultureInfo("pt-BR"), out var v)) return v;
            Erro("  Digite um valor, por exemplo 9,50.");
        }
    }

    public static DateTime DataHora(string pergunta, DateTime? atual = null)
    {
        while (true)
        {
            var lido = Texto($"{pergunta} (dd/mm/aaaa hh:mm)", atual?.ToString("dd/MM/yyyy HH:mm"));
            if (DateTime.TryParseExact(lido, "dd/MM/yyyy HH:mm", CultureInfo.GetCultureInfo("pt-BR"), DateTimeStyles.None, out var d))
                return d;
            Erro("  Use o formato 05/11/2026 07:30.");
        }
    }

    public static string Brl(decimal valor) => valor.ToString("C", CultureInfo.GetCultureInfo("pt-BR"));

    public static string Linha(Carona c) =>
        $"#{c.Id} {c.SaidaEm:dd/MM HH:mm}  {c.Origem} → {c.Destino}  | {c.Motorista?.NomeCompleto} " +
        $"({c.Veiculo?.Modelo} {c.Veiculo?.Placa}) | {c.VagasLivres} de {c.Vagas} vagas livres | {Brl(c.PrecoPorVaga)}" +
        (c.Status == StatusCarona.Cancelada ? " | CANCELADA" : "") +
        (c.Regras.Count > 0 ? $"\n     Regras: {string.Join(", ", c.Regras.Select(r => r.Nome))}" : "");
}
