var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "Hello!");

app.MapPost("/login", (MinimalApi.DTOs.LoginDto loginDto) => {
    if(loginDto.Email == "adm@teste.com" && loginDto.Senha == "123456")
        return Results.Ok("Login com sucesso!");
    else
        return Results.Unauthorized();
});

app.Run();

