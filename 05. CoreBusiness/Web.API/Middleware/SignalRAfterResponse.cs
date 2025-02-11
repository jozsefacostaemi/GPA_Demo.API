using System.Text.Json;
using Web.Core.Business.API.Enums;
using Web.Core.Business.API.Infraestructure.Persistence.Repositories.Notifications;

namespace Web.Core.Business.API.Middleware
{
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

                await _next(context);

                memoryStream.Seek(0, SeekOrigin.Begin);
                var responseBody = await new StreamReader(memoryStream).ReadToEndAsync();
                memoryStream.Seek(0, SeekOrigin.Begin);

                await memoryStream.CopyToAsync(originalResponseBody);
                context.Response.Body = originalResponseBody;

                await context.Response.Body.FlushAsync();

                if (_signalREndpoints.Contains(context.Request.Path.Value))
                {
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
                        }
                    }

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
