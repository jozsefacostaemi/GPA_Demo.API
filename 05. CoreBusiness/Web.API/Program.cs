using Lib.MessageQueues.Functions.IRepositories;
using Lib.MessageQueues.Functions.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Web.Core.Business.API.Domain.Interfaces;
using Web.Core.Business.API.Infraestructure.Persistence.Entities;
using Web.Core.Business.API.Infraestructure.Persistence.Repositories.EmitMessage;
using Web.Core.Business.API.Infraestructure.Persistence.Repositories.Queue;
using Web.Core.Business.API.Infraestructure.Persistence.Repositories.StateMachine;
using Web.Core.Business.API.Infraestructure.Persistence.Validators;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// Se agrega servicio de Swagger
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "API EMI Demo", Version = "v1" });
});
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer")));
builder.Services.AddScoped<ApplicationDbContext>();
//builder.Services.AddScoped<IMessagingFunctions>();
builder.Services.AddScoped<MessagingFunctionsFactory>();
builder.Services.AddScoped<IQueueRepository, QueueRepository>();
builder.Services.AddScoped<IRabbitMQFunctions, RabbitMQFunctions>();
builder.Services.AddScoped<IKafkaFunctions, KafkaFunctions>();
builder.Services.AddScoped<GetMachineStateValidator>();
builder.Services.AddScoped<GetStatesRepository>();
builder.Services.AddScoped<IEmitMessagesRepository, EmitMessageRepository>();
var app = builder.Build();
app.UseAuthorization();
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "API EMI Demo v1");
    options.RoutePrefix = "swagger";
});

app.MapControllers();

app.Run();
