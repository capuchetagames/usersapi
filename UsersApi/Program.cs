using System.Text.Json.Serialization;
using Amazon.DynamoDBv2;
using Core.Entity;
using Core.Models;
using Core.Repository;
using DynamoDb.Logging;
using DynamoDb.Services;
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

//add logs in a file for new Relic

// Log.Logger = new LoggerConfiguration()
//     .Enrich.FromLogContext()
//     .Enrich.WithNewRelicLogsInContext() // método do pacote
//     .WriteTo.File(
//         path: "logs/app.log.json",
//         formatter: new NewRelicFormatter(),
//         rollingInterval: RollingInterval.Day)
//     .CreateLogger();
//
// builder.Host.UseSerilog();


builder.Services.AddDynamoDb(builder.Configuration);

var serviceProvider = builder.Services.BuildServiceProvider();
var dynamoClient    = serviceProvider.GetRequiredService<IAmazonDynamoDB>();
var logTableName    = builder.Configuration["DynamoDb:LogTableName"];


builder.Logging
    .ClearProviders()                      
    .AddConsole()                          
    .AddDynamoDbLogger(dynamoClient, logTableName, LogLevel.Information);


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

app.UseLogMiddleware();

//app.UseMiddleware<CorrelationMiddleware>();
app.UseDynamoLogging();


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



Console.WriteLine("UsersApi Up and Running!");

app.Run();