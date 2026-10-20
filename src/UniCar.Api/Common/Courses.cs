namespace UniCar.Api.Common;

/// <summary>Lista fechada de cursos, para ninguém inventar nome de curso.</summary>
public static class Courses
{
    public static readonly IReadOnlyList<string> All = new[]
    {
        "Administração", "Agronomia", "Arquitetura e Urbanismo", "Biomedicina", "Ciência da Computação",
        "Ciências Contábeis", "Design", "Direito", "Economia", "Educação Física", "Enfermagem",
        "Engenharia Civil", "Engenharia de Computação", "Engenharia de Produção", "Engenharia de Software",
        "Engenharia Elétrica", "Engenharia Mecânica", "Engenharia Química", "Farmácia", "Fisioterapia",
        "Jornalismo", "Medicina", "Medicina Veterinária", "Nutrição", "Odontologia", "Pedagogia",
        "Psicologia", "Publicidade e Propaganda", "Relações Internacionais", "Sistemas de Informação"
    };

    public static bool Exists(string? name) => name is not null && All.Contains(name);
}
