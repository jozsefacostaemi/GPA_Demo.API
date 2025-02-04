namespace Web.Core.Business.API.Middleware
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;
    using System.IO;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Web.Core.Business.API.Enums;
    using Web.Core.Business.API.Infraestructure.Persistence.Repositories.Notifications;

    public class SignalRAfterResponseMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public SignalRAfterResponseMiddleware(RequestDelegate next, IServiceScopeFactory serviceScopeFactory)
        {
            _next = next;
            _serviceScopeFactory = serviceScopeFactory;
        }

        // Lista de endpoints en los que queremos ejecutar SignalR
        private readonly HashSet<string> _signalREndpoints = new()
        {
            "/EmitMessages/EmitAttention",
            "/EmitMessages/AssignAttention",
            "/EmitMessages/StartAttention",
            "/EmitMessages/FinishAttention",
            "/EmitMessages/CancelAttention",
            "/auth/LogIn",
        };

        public async Task Invoke(HttpContext context)
        {
            var originalResponseBody = context.Response.Body;

            using (var memoryStream = new MemoryStream())
            {
                context.Response.Body = memoryStream;

                await _next(context); // Ejecuta la lógica del controlador

                // Captura la respuesta del controlador
                memoryStream.Seek(0, SeekOrigin.Begin);
                var responseBody = await new StreamReader(memoryStream).ReadToEndAsync();
                memoryStream.Seek(0, SeekOrigin.Begin);

                // Copia la respuesta al cuerpo original
                await memoryStream.CopyToAsync(originalResponseBody);
                context.Response.Body = originalResponseBody;

                // Envía la respuesta al cliente
                await context.Response.Body.FlushAsync();

                // Verificar si la petición es desde un endpoint permitido
                if (_signalREndpoints.Contains(context.Request.Path.Value))
                {
                    // Intentar deserializar la respuesta como JSON
                    object? payloadData = null;

                    if (!string.IsNullOrWhiteSpace(responseBody))
                    {
                        try
                        {
                            using (JsonDocument doc = JsonDocument.Parse(responseBody))
                            {
                                if (doc.RootElement.TryGetProperty("data", out JsonElement dataElement))
                                {
                                    payloadData = JsonSerializer.Deserialize<object>(dataElement.GetRawText());
                                }
                            }
                        }
                        catch (JsonException)
                        {
                            // Si la respuesta no es JSON válido, no se pasa payload
                        }
                    }

                    // Crear un nuevo scope para obtener el servicio scoped y enviar SignalR
                    using (var scope = _serviceScopeFactory.CreateScope())
                    {
                        var notificationRepository = scope.ServiceProvider.GetRequiredService<NotificationRepository>();
                        await notificationRepository.SendBroadcastAsync(NotificationEventCodeEnum.AttentionMessage, payloadData);
                    }
                }
            }
        }

    }

}
