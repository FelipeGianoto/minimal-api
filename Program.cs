using MinimalApi.Infraestrutura.Db;
using MinimalApi.DTOs;
using Microsoft.EntityFrameworkCore;
using MinimalApi.Dominio.Interfaces;
using MinimalApi.Dominio.Servicos;
using Microsoft.AspNetCore.Mvc;
using MinimalApi.Dominio.ModelViews;
using MinimalApi.Dominio.DTOs;
using MinimalApi.Dominio.Entidades;
using MinimalApi.Dominio.Enums;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.OpenApi.Models;

#region Builder
var builder = WebApplication.CreateBuilder(args);

var key = builder.Configuration.GetSection("Jwt").ToString();
if(string.IsNullOrEmpty(key)) key = "123456";

builder.Services.AddAuthentication(option => {
    option.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    option.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(option => {
    option.TokenValidationParameters = new TokenValidationParameters{
        ValidateLifetime = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
        ValidateIssuer = false,
        ValidateAudience = false,
    };
});

builder.Services.AddAuthorization();

builder.Services.AddScoped<IAdministradorServico, AdministradorServico>();
builder.Services.AddScoped<IVeiculosServico, VeiculoServico>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => {
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Insira o token Jwt"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme{
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                } 
            },
            new string[] {}
        }
    });
});

builder.Services.AddDbContext<DbContexto>(options => {
    options.UseMySql(
        builder.Configuration.GetConnectionString("Mysql"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("Mysql"))
    );
});

var app = builder.Build();
#endregion

#region Home
app.MapGet("/", () => Results.Json(new Home())).WithTags("Home");
#endregion

#region Administradores
string GerarTokenJwt(Administrador administrador)
{
    if(string.IsNullOrEmpty(key)) return string.Empty;
        
    var securityKey  = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
    var credential = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

    var claims = new List<Claim>()
    {
        new Claim("Email", administrador.Email),
        new Claim("Perfil", administrador.Perfil)
    };

    var token = new JwtSecurityToken(
        claims: claims,
        expires: DateTime.Now.AddDays(1),
        signingCredentials: credential
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
}

app.MapPost("/administradores/login", ([FromBody] LoginDto loginDto, IAdministradorServico administradorServico) => {
    var adm = administradorServico.Login(loginDto);
    if(adm != null)
    {
        string token = GerarTokenJwt(adm);

        return Results.Ok(new AdministradorLogado
        {
            Email = adm.Email,
            Perfil = adm.Perfil,
            Token = token
        });
    }
    else
        return Results.Unauthorized();
}).WithTags("Administradores");

app.MapPost("/administradores", ([FromBody] AdministradorDto administradorDto, IAdministradorServico administradorServico) => {
    var validacao = new ErrosDeValidacao{
        Mensagens = new List<string>()
    };

    if(string.IsNullOrEmpty(administradorDto.Email))
        validacao.Mensagens.Add("Email nao pode ser vazio!");
    if(string.IsNullOrEmpty(administradorDto.Senha))
        validacao.Mensagens.Add("Senha nao pode ser vazia!");
    if(administradorDto.Perfil == null)
        validacao.Mensagens.Add("Perfil nao pode ser vazio!");

    if(validacao.Mensagens.Count > 0)
        return Results.BadRequest(validacao);

    var adm = new Administrador
    {
        Email = administradorDto.Email,
        Senha = administradorDto.Senha,
        Perfil = administradorDto.Perfil.ToString() ?? PerfilEnum.Editor.ToString(),
    };

    administradorServico.Incluir(adm);

    return Results.Created($"/administrador/{adm.Id}", new AdministradorModelView{
            Id = adm.Id, 
            Email = adm.Email, 
            Perfil = adm.Perfil
        });
}).RequireAuthorization().WithTags("Administradores");

app.MapGet("/administradores", ([FromQuery] int? pagina, IAdministradorServico administradorServico) => {
    var adms = new List<AdministradorModelView>();
    var administradores = administradorServico.Todos(pagina);
    foreach (var adm in administradores)
    {
        adms.Add(new AdministradorModelView{
            Id = adm.Id, 
            Email = adm.Email, 
            Perfil = adm.Perfil
        });
    }
    return Results.Ok(adms);
}).RequireAuthorization().WithTags("Administradores");

app.MapGet("/administradores/{id}", ([FromRoute] int id, IAdministradorServico administradorServico) => {
    var adm = administradorServico.BuscaPorId(id);
    if(adm == null) return Results.NotFound();
    return Results.Ok(new AdministradorModelView{
            Id = adm.Id, 
            Email = adm.Email, 
            Perfil = adm.Perfil
        });
}).RequireAuthorization().WithTags("Administradores");

#endregion

#region Veiculos
ErrosDeValidacao validaDto(VeiculoDto veiculoDto)
{
    var validacao = new ErrosDeValidacao{
        Mensagens = new List<string>()
    };

    if(string.IsNullOrEmpty(veiculoDto.Nome))
        validacao.Mensagens.Add("O nome nao pode ser vazio");

    if(string.IsNullOrEmpty(veiculoDto.Marca))
        validacao.Mensagens.Add("A marca nao ficar em branco");

    if(veiculoDto.Ano < 1950)
        validacao.Mensagens.Add("Veiculo muito antigo, aceito somente anos superiores a 1950");

    return validacao;
}

app.MapPost("/veiculos", ([FromBody] VeiculoDto veiculoDto, IVeiculosServico veiculoServico) => {
    var validacao = validaDto(veiculoDto);
    if(validacao.Mensagens.Count > 0)
        return Results.BadRequest(validacao);

    var veiculo = new Veiculo
    {
        Nome = veiculoDto.Nome,
        Marca = veiculoDto.Marca,
        Ano = veiculoDto.Ano,
    };
    veiculoServico.Incluir(veiculo);

    return Results.Created($"/veiculo/{veiculo.Id}", veiculo);
}).RequireAuthorization().WithTags("Veiculos");


app.MapGet("/veiculos", (IVeiculosServico veiculoServico) => {
    var veiculo = veiculoServico.Todos();
    return Results.Ok(veiculo);
}).RequireAuthorization().WithTags("Veiculos");


app.MapGet("/veiculos/{id}", ([FromRoute] int id, IVeiculosServico veiculoServico) => {
    var veiculo = veiculoServico.BuscaPorId(id);
    if(veiculo == null) return Results.NotFound();
    return Results.Ok(veiculo);
}).RequireAuthorization().WithTags("Veiculos");


app.MapPut("/veiculos/{id}", ([FromRoute] int id, [FromBody] VeiculoDto veiculoDto, IVeiculosServico veiculoServico) => {

    var veiculo = veiculoServico.BuscaPorId(id);
    if(veiculo == null) 
        return Results.NotFound();

    var validacao = validaDto(veiculoDto);
    if(validacao.Mensagens.Count > 0)
        return Results.BadRequest(validacao);

    veiculo.Nome = veiculoDto.Nome;
    veiculo.Marca = veiculoDto.Marca;
    veiculo.Ano = veiculoDto.Ano;

    veiculoServico.Atualizar(veiculo);
    return Results.Ok(veiculo);
}).RequireAuthorization().WithTags("Veiculos");

app.MapDelete("/veiculos/{id}", ([FromRoute] int id, IVeiculosServico veiculoServico) => {
    var veiculo = veiculoServico.BuscaPorId(id);
    if(veiculo == null) return Results.NotFound();

    veiculoServico.Apagar(veiculo);
    return Results.NoContent();
}).RequireAuthorization().WithTags("Veiculos");
#endregion

#region App
app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.Run();
#endregion
