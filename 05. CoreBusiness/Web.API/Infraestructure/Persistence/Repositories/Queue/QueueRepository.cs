using Lib.MessageQueues.Functions.IRepositories;
using Microsoft.EntityFrameworkCore;
using Web.Core.Business.API.Domain.Interfaces;
using Web.Core.Business.API.Infraestructure.Persistence.Entities;

namespace Web.Core.Business.API.Infraestructure.Persistence.Repositories.Queue
{
    public class QueueRepository : IQueueRepository
    {
        #region Variables
        private readonly ApplicationDbContext _context;
        private readonly IRabbitMQFunctions _rabbitMQFunctions;
        #endregion

        #region Ctor
        public QueueRepository(ApplicationDbContext applicationDbContext, IRabbitMQFunctions rabbitMQFunctions)
        {
            _context = applicationDbContext;
            _rabbitMQFunctions = rabbitMQFunctions;
        }
        #endregion

        #region Public Methods 
        /* Función que genera la configuración de las colas con base a su parametrización */
        public async Task<bool> GeneratedConfigQueues()
        {
            var resultConfig = _context.ConfQueues.Where(x => x.Active == true).Select(x => new
            {
                CityName = x.City.Name,
                ProcessName = x.Process.Name,
                StateName = x.AttentionState.Name,
                x.NOrder,
                ConfQueueId = x.Id,
            }).ToList();
            var resultGeneratedQueues = await _context.GeneratedQueues.Where(x => x.Active == true).Select(x => x.ConfigQueueId).ToListAsync();

            if (resultConfig.Any())
            {
                foreach (var drResultConfig in resultConfig)
                {
                    if (!resultGeneratedQueues.Contains(drResultConfig.ConfQueueId))
                    {
                        var generatedQueue = GeneratedNameQueue(drResultConfig.CityName?.Trim(), drResultConfig?.ProcessName.Trim(), drResultConfig?.StateName.Trim(), (int)drResultConfig.NOrder);
                        if (!string.IsNullOrEmpty(generatedQueue))
                            await _context.AddAsync(new GeneratedQueue { Id = Guid.NewGuid(), Name = generatedQueue, Active = true, ConfigQueueId = drResultConfig.ConfQueueId });
                    }
                }
                await _context.SaveChangesAsync();
            }
            return true;
        }
        /* Función que crea las colas en el orquestador de mensajeria */
        public async Task<bool> CreatedQueues()
        {
            var resultGeneratedQueues = _context.GeneratedQueues.Include(x => x.ConfigQueue).Where(x => x.Active == true).ToList();
            foreach (var dr in resultGeneratedQueues)
            {
                await _rabbitMQFunctions.CreateQueueAsync(dr.Name,
                    dr.ConfigQueue.Durable,
                    dr.ConfigQueue.Exclusive,
                    dr.ConfigQueue.AutoDelete,
                    dr.ConfigQueue.MaxPriority,
                    dr.ConfigQueue.MessageLifeTime,
                    dr.ConfigQueue.QueueExpireTime,
                    dr.ConfigQueue.QueueMode,
                    dr.ConfigQueue.QueueDeadLetterExchange, 
                    dr.ConfigQueue.QueueDeadLetterExchangeRoutingKey);
            }
            return true;
        }
        /* Función que elimina todas las colas en el orquestador de mensajeria */
        public async Task<bool> DeleteQueues()
        {
            await _rabbitMQFunctions.DeleteQueues();
            return true;
        }
        #endregion

        #region Private Methods
        /* Función que genera el nombre de la cola */
        private string GeneratedNameQueue(string city, string process, string state, int order)
        {
            if (!string.IsNullOrEmpty(city) && !string.IsNullOrEmpty(process) && !string.IsNullOrEmpty(state))
                return $"{order}.{city}.{process}.{state}";
            if (!string.IsNullOrEmpty(process) && !string.IsNullOrEmpty(state))
                return $"{order}.{process}.{state}";
            if (!string.IsNullOrEmpty(state))
                return $"{order}.{process}.{state}";
            return string.Empty;
        }
        #endregion
    }
}
