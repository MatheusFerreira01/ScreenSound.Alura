using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ScreenSound.API.Endpoints;
using ScreenSound.Banco;
using ScreenSound.Modelos;
using ScreenSound.Shared.Dados.Modelos;
using ScreenSound.Shared.Modelos.Modelos;
using System.Data.SqlTypes;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ScreenSoundContext>((options) => {
    options
            .UseSqlServer(builder.Configuration["ConnectionStrings:ScreenSoundDB"])
            .UseLazyLoadingProxies();
});

builder.Services
    .AddIdentityApiEndpoints<PessoaComAcesso>()
    .AddEntityFrameworkStores<ScreenSoundContext>();

builder.Services.AddAuthentication();
builder.Services.AddAuthorization();


builder.Services.AddTransient<DAL<PessoaComAcesso>>();
builder.Services.AddTransient<DAL<Artista>>();
builder.Services.AddTransient<DAL<Musica>>();
builder.Services.AddTransient<DAL<Genero>>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options => options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);

builder.Services.AddCors(options => options.AddPolicy(
    "wasm", policy => policy.WithOrigins(new string[] {
        builder.Configuration["BackendUrl"] ?? "https://localhost:7089",
        builder.Configuration["FrontendUrl"] ?? "https://localhost:7015"
         })
         .AllowAnyMethod()
         .SetIsOriginAllowed(_ => true)
         .AllowAnyHeader()
         .AllowCredentials())
);

builder.Services.Configure<IdentityOptions>(options =>
{
    options.Password.RequireNonAlphanumeric = false;
});


var app = builder.Build();

app.UseCors("wasm");

app.UseAuthentication();
app.UseAuthorization();

app.UseStaticFiles();


app.AddEndPointsArtistas();
app.AddEndPointsMusicas();
app.AddEndPointGeneros();

app.MapGroup("auth").MapIdentityApi<PessoaComAcesso>().WithTags("Autorização");

app.MapPost("auth/logout", async ([FromServices] SignInManager<PessoaComAcesso> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.Ok();
}).RequireAuthorization().WithTags("Autorização");


app.MapPost("auth/create", async ([FromServices] UserManager<PessoaComAcesso> userManager, RegisterRequest registerRequest) =>
{
    var user = new PessoaComAcesso { UserName = registerRequest.Email, Email = registerRequest.Email };
    var result = await userManager.CreateAsync(user, registerRequest.Password);
    if (result.Succeeded)
    {
        return Results.Ok("User registered successfully");
    }
    
    return Results.BadRequest(result.Errors);
}).AllowAnonymous().WithTags("Autorização");

app.UseSwagger();
app.UseSwaggerUI();

app.Run();
