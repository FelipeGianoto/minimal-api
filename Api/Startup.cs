using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MinimalApi;
using MinimalApi.Dominio.DTOs;
using MinimalApi.Dominio.Entidades;
using MinimalApi.Dominio.Enums;
using MinimalApi.Dominio.Interfaces;
using MinimalApi.Dominio.ModelViews;
using MinimalApi.Dominio.Servicos;
using MinimalApi.DTOs;
using MinimalApi.Infraestrutura.Db;

namespace MinimalApi.Startup;

public class Startup
{
    public IConfiguration Configuration { get; set; } = default!;
    private string key;

    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
        key = Configuration.GetSection("Jwt")!.ToString() ?? "";
    }

    public void ConfigureServices(IServiceCollection services)
    {

        services.AddAuthentication(option => {
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

        services.AddAuthorization();

        services.AddScoped<IAdministradorServico, AdministradorServico>();
        services.AddScoped<IVeiculosServico, VeiculoServico>();

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options => {
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

        services.AddDbContext<DbContexto>(options => {
            options.UseMySql(
                Configuration.GetConnectionString("Mysql"),
                ServerVersion.AutoDetect(Configuration.GetConnectionString("Mysql"))
            );
        });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {

        app.UseSwagger();
        app.UseSwaggerUI();

        app.UseAuthentication();
        app.UseRouting();
        app.UseAuthorization();

        app.UseEndpoints(endpoints => {
            #region Home
            endpoints.MapGet("/", () => Results.Json(new Home())).WithTags("Home");
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
                    new Claim(ClaimTypes.Role, administrador.Perfil),
                    new Claim("Perfil", administrador.Perfil)
                };

                var token = new JwtSecurityToken(
                    claims: claims,
                    expires: DateTime.Now.AddDays(1),
                    signingCredentials: credential
                );

                return new JwtSecurityTokenHandler().WriteToken(token);
            }

            endpoints.MapPost("/administradores/login", ([FromBody] LoginDto loginDto, IAdministradorServico administradorServico) => {
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

            endpoints.MapPost("/administradores", ([FromBody] AdministradorDto administradorDto, IAdministradorServico administradorServico) => {
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
            })
            .RequireAuthorization()
            .RequireAuthorization(new AuthorizeAttribute {Roles = "Adm"})
            .WithTags("Administradores");

            endpoints.MapGet("/administradores", ([FromQuery] int? pagina, IAdministradorServico administradorServico) => {
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
            })
            .RequireAuthorization()
            .RequireAuthorization(new AuthorizeAttribute {Roles = "Adm"})
            .WithTags("Administradores");

            endpoints.MapGet("/administradores/{id}", ([FromRoute] int id, IAdministradorServico administradorServico) => {
                var adm = administradorServico.BuscaPorId(id);
                if(adm == null) return Results.NotFound();
                return Results.Ok(new AdministradorModelView{
                        Id = adm.Id, 
                        Email = adm.Email, 
                        Perfil = adm.Perfil
                    });
            })
            .RequireAuthorization()
            .RequireAuthorization(new AuthorizeAttribute {Roles = "Adm"})
            .WithTags("Administradores");

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

            endpoints.MapPost("/veiculos", ([FromBody] VeiculoDto veiculoDto, IVeiculosServico veiculoServico) => {
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
            })
            .RequireAuthorization()
            .RequireAuthorization(new AuthorizeAttribute {Roles = "Adm,Editor"})
            .WithTags("Veiculos");


            endpoints.MapGet("/veiculos", (IVeiculosServico veiculoServico) => {
                var veiculo = veiculoServico.Todos();
                return Results.Ok(veiculo);
            }).RequireAuthorization().WithTags("Veiculos");


            endpoints.MapGet("/veiculos/{id}", ([FromRoute] int id, IVeiculosServico veiculoServico) => {
                var veiculo = veiculoServico.BuscaPorId(id);
                if(veiculo == null) return Results.NotFound();
                return Results.Ok(veiculo);
            }).RequireAuthorization().WithTags("Veiculos");


            endpoints.MapPut("/veiculos/{id}", ([FromRoute] int id, [FromBody] VeiculoDto veiculoDto, IVeiculosServico veiculoServico) => {

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

            endpoints.MapDelete("/veiculos/{id}", ([FromRoute] int id, IVeiculosServico veiculoServico) => {
                var veiculo = veiculoServico.BuscaPorId(id);
                if(veiculo == null) return Results.NotFound();

                veiculoServico.Apagar(veiculo);
                return Results.NoContent();
            }).RequireAuthorization().WithTags("Veiculos");
            #endregion
        });
    }

}