using Microsoft.EntityFrameworkCore;
using UniCar.Persistencia.Dados;
using UniCar.Persistencia.Entidades;

namespace UniCar.Persistencia.Servicos;

/// <summary>CRUD de veículos. Cada veículo pertence a um motorista (um para muitos).</summary>
public class VeiculoService(UniCarContext db)
{
    public Veiculo Criar(Veiculo veiculo)
    {
        veiculo.Placa = veiculo.Placa.Trim().ToUpperInvariant();
        Validacao.Validar(veiculo);
        if (db.Usuarios.Find(veiculo.MotoristaId) is null) throw new ErroUniCar("Motorista não encontrado.");
        if (db.Veiculos.Any(v => v.Placa == veiculo.Placa)) throw new ErroUniCar("Essa placa já está cadastrada.");

        db.Veiculos.Add(veiculo);
        db.SaveChanges();
        return veiculo;
    }

    public List<Veiculo> Listar() =>
        db.Veiculos.Include(v => v.Motorista).OrderBy(v => v.Placa).ToList();

    public Veiculo? BuscarPorId(int id) =>
        db.Veiculos.Include(v => v.Motorista).FirstOrDefault(v => v.Id == id);

    public Veiculo Atualizar(Veiculo veiculo)
    {
        veiculo.Placa = veiculo.Placa.Trim().ToUpperInvariant();
        Validacao.Validar(veiculo);
        if (db.Veiculos.Any(v => v.Placa == veiculo.Placa && v.Id != veiculo.Id))
            throw new ErroUniCar("Essa placa já está cadastrada.");

        db.Veiculos.Update(veiculo);
        db.SaveChanges();
        return veiculo;
    }

    public void Excluir(int id)
    {
        var veiculo = db.Veiculos.Find(id) ?? throw new ErroUniCar("Veículo não encontrado.");
        if (db.Caronas.Any(c => c.VeiculoId == id))
            throw new ErroUniCar("Esse veículo está em caronas publicadas. Exclua as caronas antes.");

        db.Veiculos.Remove(veiculo);
        db.SaveChanges();
    }
}
