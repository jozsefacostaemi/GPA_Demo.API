namespace Shared
{
    public class RequestResult : IRequestResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public dynamic? Data { get; set; }
        public string Module { get; set; }

        // Constructor por defecto
        public RequestResult() { }

        // Constructor con parámetros
        public RequestResult(dynamic? data, bool success, string message, bool isWarning = false)
        {
            Data = data;
            Success = success;
            Message = message;
        }

        // Metodo estatico para devolver consulta realizada con éxito
        public static RequestResult SuccessResult(dynamic? data = null, string message = "Registros consultados con éxito", string module = "")
        {
            return new RequestResult(data, true, message, false);
        }

        // Metodo estatico para devolver registro creado con éxito
        public static RequestResult SuccessRecord(dynamic? data = null, string message = "Registro creado con éxito", string module = "")
        {
            return new RequestResult(data, true, message, false);
        }

        // Metodo estatico para devolver registro modificado con éxito
        public static RequestResult SuccessUpdate(dynamic? data = null, string message = "Registro modificado con éxito", string module = "")
        {
            return new RequestResult(data, true, message, false);
        }


        // Método estático para devolver un registro eliminado
        public static RequestResult SuccessOperation(string message = "Operación exitosa", string module = "")
        {
            return new RequestResult
            {
                Success = true,
                Message = message,
                Module = module
            };
        }

        // Método estático para devolver error de consulta
        public static RequestResult ErrorResult(string message = "Ocurrió un error al consultar la información", string module = "")
        {
            return new RequestResult
            {
                Success = false,
                Message = message,
                Module = module
            };
        }

        // Método estático para devolver error de guardado
        public static RequestResult ErrorRecord(string message = "Ocurrió un error al crear el registro", string module = "")
        {
            return new RequestResult
            {
                Success = false,
                Message = message,
                Module = module
            };
        }

        // Metodo estatico para devolver consulta realizada con éxito
        public static RequestResult SuccessResultNoRecords(string message = "No se encontraron registros", string module = "")
        {
            return new RequestResult(null, true, message, false);
        }

        // Método estático para devolver un registro eliminado
        public static RequestResult SuccessDelete(string message = "Registro eliminado con éxito", string module = "")
        {
            return new RequestResult
            {
                Success = true,
                Message = message,
                Module = module
            };
        }
    }

    public interface IRequestResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public dynamic? Data { get; set; }
        public string Module { get; set; }
    }
}
