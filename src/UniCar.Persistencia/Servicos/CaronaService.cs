using Microsoft.EntityFrameworkCore;
using UniCar.Persistencia.Dados;
using UniCar.Persistencia.Entidades;

namespace UniCar.Persistencia.Servicos;

/// <summary>CRUD de caronas marcadas, com as regras (muitos para muitos) e as reservas (um para muitos).</summary>
public class CaronaService(UniCarContext db)
{
    public Carona Criar(Carona carona, IEnumerable<int>? regraIds = null)
    {
        Validacao.Validar(carona);
        var veiculo = db.Veiculos.Find(carona.VeiculoId) ?? throw new ErroUniCar("Veículo não encontrado.");
        if (veiculo.MotoristaId != carona.MotoristaId)
            throw new ErroUniCar("Esse veículo não é desse motorista.");

        // Muitos para muitos: basta colocar as regras na coleção; o EF grava a tabela de junção.
        var ids = regraIds?.ToList() ?? new List<int>();
        carona.Regras = db.Regras.Where(r => ids.Contains(r.Id)).ToList();

        db.Caronas.Add(carona);
        db.SaveChanges();
        return carona;
    }

    public List<Carona> Listar() =>
        db.Caronas
            .Include(c => c.Motorista)
            .Include(c => c.Veiculo)
            .Include(c => c.Reservas)
            .Include(c => c.Regras)
            .OrderBy(c => c.SaidaEm)
            .ToList();

    public Carona? BuscarPorId(int id) =>
        db.Caronas
            .Include(c => c.Motorista)
            .Include(c => c.Veiculo)
            .Include(c => c.Regras)
            .Include(c => c.Reservas).ThenInclude(r => r.Passageiro)
            .FirstOrDefault(c => c.Id == id);

    public Carona Atualizar(Carona carona)
    {
        Validacao.Validar(carona);
        var reservadas = db.Reservas.Count(r => r.CaronaId == carona.Id && r.Status == StatusReserva.Confirmada);
        if (carona.Vagas < reservadas)
            throw new ErroUniCar($"Essa carona já tem {reservadas} reservas. Não dá para deixar menos vagas que isso.");

        db.Caronas.Update(carona);
        db.SaveChanges();
        return carona;
    }

    /// <summary>Excluir a carona apaga as reservas dela (DeleteBehavior.Cascade no OnModelCreating).</summary>
    public void Excluir(int id)
    {
        var carona = db.Caronas.Include(c => c.Reservas).FirstOrDefault(c => c.Id == id)
            ?? throw new ErroUniCar("Carona não encontrada.");
        db.Caronas.Remove(carona);
        db.SaveChanges();
    }
}
