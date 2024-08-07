using MinimalApi.Dominio.Enums;

namespace MinimalApi.Dominio.DTOs;

public class AdministradorDto
{
    public string Email { get; set;} = default!;
    public string Senha { get; set;} = default!;
    public PerfilEnum? Perfil { get; set;} = default!;
}