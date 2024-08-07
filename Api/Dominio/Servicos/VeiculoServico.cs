using Microsoft.EntityFrameworkCore;
using MinimalApi.Dominio.Entidades;
using MinimalApi.Dominio.Interfaces;
using MinimalApi.Infraestrutura.Db;

namespace MinimalApi.Dominio.Servicos;

public class VeiculoServico : IVeiculosServico
{
    private readonly DbContexto _dbContexto;
    public VeiculoServico(DbContexto dbContexto)
    {
        _dbContexto = dbContexto;
    }
    public void Apagar(Veiculo veiculo)
    {
        _dbContexto.Veiculos.Remove(veiculo);
        _dbContexto.SaveChanges();
    }

    public void Atualizar(Veiculo veiculo)
    {
         _dbContexto.Veiculos.Update(veiculo);
         _dbContexto.SaveChanges();
    }

    public void Incluir(Veiculo veiculo)
    {
        _dbContexto.Veiculos.Add(veiculo);
        _dbContexto.SaveChanges();
    }

    public Veiculo? BuscaPorId(int id)
    {
        return _dbContexto.Veiculos.Where(v => v.Id == id).FirstOrDefault();
    }

    public List<Veiculo> Todos(int pagina = 1, string? nome = null, string? marca = null)
    {
        var query = _dbContexto.Veiculos.AsQueryable();
        if (!string.IsNullOrEmpty(nome))
        {
            query = query.Where(v => v.Nome.Contains(nome, StringComparison.CurrentCultureIgnoreCase));
        }

        int itensPorPagina = 10;

        query = query.Skip((pagina - 1) * itensPorPagina).Take(itensPorPagina);

        return query.ToList();
    }
}