namespace Web.Core.Business.API.Helpers
{
    public class GetColor
    {
        private static readonly List<string> backgroundColors = new List<string>
        {
            "rgba(255, 99, 132, 0.2)",
            "rgba(255, 159, 64, 0.2)",
            "rgba(255, 205, 86, 0.2)",
            "rgba(75, 192, 192, 0.2)",
            "rgba(54, 162, 235, 0.2)",
            "rgba(153, 102, 255, 0.2)",
            "rgba(201, 203, 207, 0.2)",
            // Nuevas combinaciones pastel
            "rgba(255, 182, 193, 0.2)",
            "rgba(255, 229, 180, 0.2)",
            "rgba(173, 216, 230, 0.2)",
            "rgba(255, 228, 225, 0.2)",
            "rgba(221, 160, 221, 0.2)",
            "rgba(152, 251, 152, 0.2)",
            "rgba(255, 240, 245, 0.2)",
            "rgba(255, 99, 160, 0.2)",
            "rgba(204, 204, 255, 0.2)",
            "rgba(250, 240, 230, 0.2)",
            "rgba(255, 222, 173, 0.2)",
            "rgba(245, 245, 220, 0.2)",
            "rgba(230, 230, 250, 0.2)",
            "rgba(240, 230, 140, 0.2)",
            "rgba(255, 228, 196, 0.2)",
            "rgba(186, 85, 211, 0.2)",
            "rgba(204, 255, 255, 0.2)",
            "rgba(240, 128, 128, 0.2)",
            "rgba(173, 216, 230, 0.2)",
            "rgba(255, 239, 191, 0.2)"
        };

        private static readonly List<string> borderColors = new List<string>
        {
            "rgb(255, 99, 132)",
            "rgb(255, 159, 64)",
            "rgb(255, 205, 86)",
            "rgb(75, 192, 192)",
            "rgb(54, 162, 235)",
            "rgb(153, 102, 255)",
            "rgb(201, 203, 207)",
            // Nuevas combinaciones pastel
            "rgb(255, 182, 193)",
            "rgb(255, 229, 180)",
            "rgb(173, 216, 230)",
            "rgb(255, 228, 225)",
            "rgb(221, 160, 221)",
            "rgb(152, 251, 152)",
            "rgb(255, 240, 245)",
            "rgb(255, 99, 160)",
            "rgb(204, 204, 255)",
            "rgb(250, 240, 230)",
            "rgb(255, 222, 173)",
            "rgb(245, 245, 220)",
            "rgb(230, 230, 250)",
            "rgb(240, 230, 140)",
            "rgb(255, 228, 196)",
            "rgb(186, 85, 211)",
            "rgb(204, 255, 255)",
            "rgb(240, 128, 128)",
            "rgb(173, 216, 230)",
            "rgb(255, 239, 191)"
        };


        // Función para obtener un par de colores aleatorios
        public static (string backgroundColor, string borderColor) GetRandomColorPair()
        {
            Random random = new Random();
            int index = random.Next(backgroundColors.Count); 
            return (backgroundColors[index], borderColors[index]);
        }
    }
}
