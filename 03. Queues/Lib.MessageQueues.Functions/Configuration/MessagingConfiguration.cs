using Newtonsoft.Json;
using System;
using System.IO;

public static class MessagingConfiguration
{
    public static MessagingSettings Settings { get; private set; }

    static MessagingConfiguration()
    {
        // Obtener la ruta del archivo settings.json en el directorio de salida
        string configFilePath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");

        if (File.Exists(configFilePath))
        {
            // Leer el contenido del archivo settings.json
            var json = File.ReadAllText(configFilePath);

            // Deserializar el JSON a la clase MessagingSettings
            Settings = JsonConvert.DeserializeObject<MessagingSettings>(json);
        }
        else
        {
            // Configuración por defecto si el archivo no se encuentra
            Settings = new MessagingSettings
            {
                MessagingSystem = "RabbitMQ" // Valor por defecto
            };
        }
    }
}

public class MessagingSettings
{
    public string MessagingSystem { get; set; }
}
