namespace UniCar.Api.Domain;

/// <summary>Faculdade parceira. Só e-mails dos domínios dela podem se cadastrar.</summary>
public class University
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string ShortName { get; set; }
    public List<UniversityDomain> Domains { get; set; } = new();
    public List<Campus> Campuses { get; set; } = new();
}

/// <summary>Domínio de e-mail aceito, por exemplo "ufpr.br". Subdomínios também valem (alunos.utfpr.edu.br).</summary>
public class UniversityDomain
{
    public int Id { get; set; }
    public int UniversityId { get; set; }
    public required string Domain { get; set; }
}

public class Campus
{
    public int Id { get; set; }
    public int UniversityId { get; set; }
    public required string Name { get; set; }
    public required string Address { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}
