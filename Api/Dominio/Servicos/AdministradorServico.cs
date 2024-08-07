using MinimalApi.Dominio.DTOs;
using MinimalApi.Dominio.Entidades;
using MinimalApi.Dominio.Interfaces;
using MinimalApi.DTOs;
using MinimalApi.Infraestrutura.Db;

namespace MinimalApi.Dominio.Servicos;

public class AdministradorServico : IAdministradorServico
{
    private readonly DbContexto _dbContexto;

    public AdministradorServico(DbContexto db)
    {
        _dbContexto = db;
    }

    public Administrador? BuscaPorId(int id)
    {
        return _dbContexto.Administradores.Where(a => a.Id == id).FirstOrDefault();
    }

    public Administrador Incluir(Administrador administrador)
    {
        _dbContexto.Administradores.Add(administrador);
        _dbContexto.SaveChanges();
        return administrador;
    }

    public Administrador? Login(LoginDto loginDto)
    {
        var adm = _dbContexto.Administradores.Where(adm => adm.Email == loginDto.Email && adm.Senha == loginDto.Senha).FirstOrDefault();
        return adm;
    }

    public List<Administrador> Todos(int? pagina)
    {
        var query = _dbContexto.Administradores.AsQueryable();

        int itensPorPagina = 10;

        if(pagina != null)
            query = query.Skip(((int)pagina - 1) * itensPorPagina).Take(itensPorPagina);

        return query.ToList();
    }
}