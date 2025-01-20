using Microsoft.EntityFrameworkCore;
using Shared;
using Web.Core.Business.API.Domain.Interfaces;
using Web.Core.Business.API.Infraestructure.Persistence.Entities;
using Web.Core.Business.API.Response;

namespace Web.Core.Business.API.Infraestructure.Persistence.Repositories.Core
{
    public class GenericStatesRepository : IGenericStatesRepository
    {
        private readonly ApplicationDbContext _context;

        public GenericStatesRepository(ApplicationDbContext context) => _context = context;

        /* Función que consulta estado de atención */
        public Task<RequestResult> GetStatesAttention() => GetStatesAsync<AttentionState>(q => q);

        /* Función que consulta los estados del personal asistencial */
        public Task<RequestResult> GetStatesHealthCareStaff() => GetStatesAsync<PersonState>(q => q.Where(x => x.IsHealthCareStaff == true));

        /* Función que consulta los estados del proceso */
        public Task<RequestResult> GetStatesProcess() => GetStatesAsync<Processor>(q => q);

        /* Función que mapea la información genérica */
        public RequestResult GenericResponse(List<GenericResponse> result)
        {
            if (!result.Any())
                return RequestResult.SuccessResultNoRecords();
            return RequestResult.SuccessResult(result);
        }

        /* Función genérica para obtener y mapear estados */
        private async Task<RequestResult> GetStatesAsync<T>(Func<IQueryable<T>, IQueryable<T>> querySelector)
            where T : class
        {
            var result = await querySelector(_context.Set<T>())
                .Select(x => new GenericResponse { Id = EF.Property<Guid>(x, "Id"), Name = EF.Property<string?>(x, "Name"), Code = EF.Property<string?>(x, "Code") })
                .ToListAsync();

            return GenericResponse(result);
        }
    }
}