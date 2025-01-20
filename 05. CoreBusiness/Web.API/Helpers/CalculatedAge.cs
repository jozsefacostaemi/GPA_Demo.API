using Microsoft.AspNetCore.Mvc.RazorPages;
using NodaTime;
using Web.Core.Business.API.Response;

namespace Web.Core.Business.API.Helpers
{
    public static class CalculatedAge
    {
        /* Función estatica que calcula la edad del paciente */
        public static string YearsMonthsDays(DateTime birthDate)
        {
            var today = DateTime.Now;
            var birthDateTime = new LocalDate(birthDate.Year, birthDate.Month, birthDate.Day);
            var currentDateTime = new LocalDate(today.Year, today.Month, today.Day);
            var period = Period.Between(birthDateTime, currentDateTime);
            string yearsString = period.Years > 1 ? $"{period.Years} años" : (period.Years == 1 ? "1 año" : string.Empty);
            string monthsString = period.Months > 1 ? $"{period.Months} meses" : (period.Months == 1 ? "1 mes" : string.Empty);
            string daysString = period.Days > 1 ? $"{period.Days} días" : (period.Days == 1 ? "1 día" : string.Empty);
            string result = $"{yearsString} {monthsString} {daysString}".Trim();
            return result.Replace("  ", " ");
        }
    }
}
