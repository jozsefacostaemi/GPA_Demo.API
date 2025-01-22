using Lib.MessageQueues.Functions.IRepositories;
using Lib.MessageQueues.Functions.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Web.Core.Business.API.Domain.Interfaces;
using Web.Core.Business.API.Infraestructure.Persistence.Entities;
using Web.Core.Business.API.Infraestructure.Persistence.Repositories.Core;
using Web.Core.Business.API.Infraestructure.Persistence.Repositories.Queue;
using Web.Core.Business.API.Infraestructure.Persistence.Repositories.StateMachine;
using Web.Core.Business.API.Infraestructure.Persistence.Validators;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins", builder =>
        builder.WithOrigins("http://localhost:4200") // Permitir solo desde localhost:4200
               .AllowAnyMethod()   // Permitir cualquier m�todo (GET, POST, PUT, DELETE, etc.)
               .AllowAnyHeader()); // Permitir cualquier encabezado
});

builder.Services.AddControllers();

// Se agrega servicio de Swagger
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "API EMI Demo", Version = "v1" });
});
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer")));
#region DbContext
builder.Services.AddScoped<ApplicationDbContext>();
#endregion
#region Messaging Functions
builder.Services.AddScoped<MessagingFunctionsFactory>();
builder.Services.AddScoped<IQueueRepository, QueueRepository>();
builder.Services.AddScoped<IRabbitMQFunctions, RabbitMQFunctions>();
builder.Services.AddScoped<IKafkaFunctions, KafkaFunctions>();
#endregion

#region Repositories and functions core
builder.Services.AddScoped<GetMachineStateValidator>();
builder.Services.AddScoped<GetStatesRepository>();
builder.Services.AddScoped<IEmitMessagesRepository, EmitMessageRepository>();
builder.Services.AddScoped<IAttentionRepository, AttentionRepository>();
builder.Services.AddScoped<IGenericStatesRepository, GenericStatesRepository>();
builder.Services.AddScoped<IHealthCareStaffRepository, HealthCareStaffRepository>();
builder.Services.AddScoped<IPatientRepository, PatientRepository>();


#endregion

var app = builder.Build();
app.UseCors("AllowAllOrigins"); // Habilitar CORS para todos los or�genes permitidos
app.UseRouting();
app.UseAuthorization();
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "API EMI Demo v1");
    options.RoutePrefix = "swagger";
});

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

app.Run();
