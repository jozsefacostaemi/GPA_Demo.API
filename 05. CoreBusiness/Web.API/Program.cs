using Lib.MessageQueues.Functions.IRepositories;
using Lib.MessageQueues.Functions.Repositories.RabbitMQ;

//using Lib.MessageQueues.Functions.Repositories.RabbitMQ;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Notification.Lib;
using Web.Core.Business.API.Domain.Interfaces;
using Web.Core.Business.API.DTOs.Input;
using Web.Core.Business.API.Infraestructure.Persistence.Entities;
using Web.Core.Business.API.Infraestructure.Persistence.Repositories.Core;
using Web.Core.Business.API.Infraestructure.Persistence.Repositories.Login;
using Web.Core.Business.API.Infraestructure.Persistence.Repositories.Monitoring;
using Web.Core.Business.API.Infraestructure.Persistence.Repositories.Notifications;
using Web.Core.Business.API.Infraestructure.Persistence.Repositories.Queue;
using Web.Core.Business.API.Infraestructure.Persistence.Repositories.StateMachine;
using Web.Core.Business.API.Infraestructure.Persistence.Validators;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddSignalR();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins",
        builder => builder
            .WithOrigins("http://localhost:4200", "http://localhost:52528", "http://localhost:56681") 
            .AllowCredentials()  
            .AllowAnyHeader()
            .AllowAnyMethod());
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
builder.Services.AddScoped<IRabbitMQFunctions, RabbitMQFunctions>();
builder.Services.AddScoped<IQueueRepository, QueueRepository>();
builder.Services.Configure<RabbitMQSettingDTO>(builder.Configuration.GetSection("RabbitMQConf"));
builder.Services.AddScoped<RabbitMQConsumer>();
builder.Services.AddScoped<RabbitMQPublisher>();
#endregion
#region Repositories and functions core
builder.Services.AddScoped<GetMachineStateValidator>();
builder.Services.AddScoped<GetStatesRepository>();
builder.Services.AddScoped<IEmitMessagesRepository, EmitMessageRepository>();
builder.Services.AddScoped<IAttentionRepository, AttentionRepository>();
builder.Services.AddScoped<IGenericStatesRepository, GenericStatesRepository>();
builder.Services.AddScoped<IHealthCareStaffRepository, HealthCareStaffRepository>();
builder.Services.AddScoped<IPatientRepository, PatientRepository>();
builder.Services.AddScoped<ILoginRepository, LoginRepository>();
builder.Services.AddScoped<NotificationRepository>();
builder.Services.AddScoped<IMonitoringRepository, MonitoringRepository>();
builder.Services.AddScoped<EmitMessageRepository>();
#endregion

var app = builder.Build();
app.UseCors("AllowAllOrigins");
app.UseRouting();
app.UseAuthorization();
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "API EMI Demo v1");
    options.RoutePrefix = "swagger";
});

app.MapControllers();
app.MapHub<EventHub>("/eventhub");

app.Run();
