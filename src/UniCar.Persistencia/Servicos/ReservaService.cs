using Microsoft.EntityFrameworkCore;
using UniCar.Persistencia.Dados;
using UniCar.Persistencia.Entidades;

namespace UniCar.Persistencia.Servicos;

/// <summary>
/// CRUD de reservas: a entidade de junção entre passageiro e carona. Aqui ficam as regras
/// "ninguém reserva a própria carona" e "sem reserva acima das vagas".
/// </summary>
public class ReservaService(UniCarContext db)
{
    /// <summary>Reserva uma vaga. O preço vem da carona, nunca de quem reserva.</summary>
    public Reserva Criar(int caronaId, int passageiroId)
    {
        var carona = db.Caronas.Include(c => c.Reservas).FirstOrDefault(c => c.Id == caronaId)
            ?? throw new ErroUniCar("Carona não encontrada.");
        if (db.Usuarios.Find(passageiroId) is null) throw new ErroUniCar("Passageiro não encontrado.");
        if (carona.MotoristaId == passageiroId) throw new ErroUniCar("Você não pode reservar a sua própria carona.");
        if (carona.Status != StatusCarona.Marcada) throw new ErroUniCar("Essa carona foi cancelada.");

        var existente = carona.Reservas.FirstOrDefault(r => r.PassageiroId == passageiroId);
        if (existente?.Status == StatusReserva.Confirmada) throw new ErroUniCar("Você já tem uma vaga nessa carona.");
        if (carona.VagasLivres <= 0) throw new ErroUniCar("As vagas dessa carona acabaram.");

        // O índice único (CaronaId, PassageiroId) permite uma linha por pessoa: quem cancelou e volta reaproveita a dela.
        var reserva = existente ?? new Reserva { CaronaId = caronaId, PassageiroId = passageiroId };
        reserva.Preco = carona.PrecoPorVaga;
        reserva.Status = StatusReserva.Confirmada;
        reserva.CriadaEm = DateTime.Now;
        Validacao.Validar(reserva);

        if (existente is null) db.Reservas.Add(reserva);
        db.SaveChanges();
        return reserva;
    }

    public List<Reserva> Listar() =>
        db.Reservas
            .Include(r => r.Passageiro)
            .Include(r => r.Carona).ThenInclude(c => c!.Motorista)
            .OrderBy(r => r.CriadaEm)
            .ToList();

    public List<Reserva> ListarPorCarona(int caronaId) =>
        db.Reservas
            .Include(r => r.Passageiro)
            .Where(r => r.CaronaId == caronaId)
            .OrderBy(r => r.CriadaEm)
            .ToList();

    public Reserva? BuscarPorId(int id) =>
        db.Reservas
            .Include(r => r.Passageiro)
            .Include(r => r.Carona).ThenInclude(c => c!.Motorista)
            .FirstOrDefault(r => r.Id == id);

    public Reserva Atualizar(Reserva reserva)
    {
        Validacao.Validar(reserva);
        db.Reservas.Update(reserva);
        db.SaveChanges();
        return reserva;
    }

    /// <summary>Cancelar mantém a linha (histórico) e libera a vaga.</summary>
    public Reserva Cancelar(int id)
    {
        var reserva = db.Reservas.Find(id) ?? throw new ErroUniCar("Reserva não encontrada.");
        if (reserva.Status == StatusReserva.Cancelada) throw new ErroUniCar("Essa reserva já estava cancelada.");
        reserva.Status = StatusReserva.Cancelada;
        db.SaveChanges();
        return reserva;
    }

    public void Excluir(int id)
    {
        var reserva = db.Reservas.Find(id) ?? throw new ErroUniCar("Reserva não encontrada.");
        db.Reservas.Remove(reserva);
        db.SaveChanges();
    }
}
