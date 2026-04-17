using System.Text.Json.Serialization;
using Core.Entity;
using Core.Models;
using Core.Repository;
using FluentValidation;
using Infrastructure.Repository;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using NewRelic.LogEnrichers.Serilog;
using Serilog;
using UsersApi.Configs;
using UsersApi.Middlewares;
using UsersApi.Service;
using UsersApi.Service.Validator;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithNewRelicLogsInContext() // método do pacote
    .WriteTo.File(
        path: "logs/app.log.json",
        formatter: new NewRelicFormatter(),
        rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

 
//Pegando as variaveis do k8s
// var host = Environment.GetEnvironmentVariable("DB_HOST");
// var db = Environment.GetEnvironmentVariable("DB_NAME");
// var user = Environment.GetEnvironmentVariable("DB_USER");
// var pass = Environment.GetEnvironmentVariable("DB_PASSWORD");

builder.Services.AddControllers();
builder.Services.AddValidatorsFromAssemblyContaining<UserValidator>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerDocumentation();


builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));


builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();

builder.Services.AddMemoryCache();
builder.Services.AddTransient<ICacheService, MemCacheService>();

builder.Services.AddTransient<ICorrelationIdService, CorrelationIdService>();


builder.Services.AddScoped(typeof(IBaseLogger<>), typeof(BaseLogger<>));

builder.AddJwtAuthentication();
builder.Services.AddPolicyAuthorization();

builder.Services.AddHealthChecks();

builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("RabbitMq"));

builder.Services.AddSingleton<IRabbitMqService>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<RabbitMqSettings>>().Value;
    return new RabbitMqService(settings);
});




var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.ApplyMigrations();
    
    app.UseSwagger();
    app.UseSwaggerUI();
    
    app.UseReDoc(c =>
    {
        c.DocumentTitle = "REDOC API Documentation";
        c.SpecUrl = "/swagger/v1/swagger.json";
    });
}

//app.UseHttpsRedirection();

app.MapHealthChecks("/health");

app.UseAuthorization();

app.MapControllers();

app.UseLogMiddleware();

Console.WriteLine("UsersApi Up and Running!");

app.Run();