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
        private readonly IAttentionRepository _iattentionRepository;
        #endregion

        #region Ctor
        public QueueRepository(ApplicationDbContext applicationDbContext, IRabbitMQFunctions rabbitMQFunctions, IAttentionRepository iattentionRepository)
        {
            _context = applicationDbContext;
            _rabbitMQFunctions = rabbitMQFunctions;
            _iattentionRepository = iattentionRepository;
        }
        #endregion

        #region Public Methods 
        /* Función que genera la configuración de las colas con base a su parametrización */
        public async Task<bool> GeneratedConfigQueues()
        {
            Guid BusinessLineId = Guid.Parse("DD44C571-4FA5-4133-AECD-062834C93601");
            var businessLine = await _context.BusinessLines.Where(x => x.Active == true && x.Id.Equals(BusinessLineId)).Select(x => new { x.LevelQueueId, x.Code }).SingleOrDefaultAsync();
            if (businessLine != null)
            {
                await _iattentionRepository.ResetAttentionsAndPersonStatus();
                await DeleteProcessMessagesErroLog();
                await DeleteProcessMessages();
                await DeleteQueueGenerated();
                var getCodesForBusinessLine = await _context.BusinessLineLevelValueQueueConfigs
                     .Where(x => x.LevelQueueId.Equals(businessLine.LevelQueueId)).Select(x => x.Id).ToListAsync();
                if (getCodesForBusinessLine.Any())
                {
                    var resultConfig = _context.QueueConfs.Where(x => x.Active == true && getCodesForBusinessLine.Contains(x.BusinessLineLevelValueQueueConf.Id)).Select(x => new
                    {
                        PreNameQueue =
                        x.BusinessLineLevelValueQueueConf.CountryId != null ? $"{businessLine.Code}.{x.BusinessLineLevelValueQueueConf.Process.Name}.{x.BusinessLineLevelValueQueueConf.Country.Name}" :
                        x.BusinessLineLevelValueQueueConf.Department != null ? $"{businessLine.Code}.{x.BusinessLineLevelValueQueueConf.Process.Name}.{x.BusinessLineLevelValueQueueConf.Department.Name}" :
                        x.BusinessLineLevelValueQueueConf.CityId != null ? $"{businessLine.Code}.{x.BusinessLineLevelValueQueueConf.Process.Name}.{x.BusinessLineLevelValueQueueConf.City.Name}" : $"{businessLine.Code}.{x.BusinessLineLevelValueQueueConf.Process.Name}",
                        x.NOrder,
                        ConfQueueId = x.Id,
                    }).ToList();
                    var resultGeneratedQueues = await _context.GeneratedQueues.Where(x => x.Active == true).Select(x => x.QueueConfId).ToListAsync();
                    if (resultConfig.Any())
                    {
                        foreach (var drResultConfig in resultConfig)
                        {
                            if (!resultGeneratedQueues.Contains(drResultConfig.ConfQueueId))
                            {
                                var generatedQueue = GeneratedNameQueue(drResultConfig.PreNameQueue?.Trim(), (int)drResultConfig.NOrder);
                                if (!string.IsNullOrEmpty(generatedQueue))
                                    await _context.AddAsync(new GeneratedQueue { Id = Guid.NewGuid(), Name = generatedQueue, Active = true, QueueConfId = drResultConfig.ConfQueueId, CreatedAt = DateTime.Now });
                            }
                        }
                        await _context.SaveChangesAsync();
                    }
                }
                await DeleteQueues();
                await CreatedQueues();
            }
            return true;
        }
        /* Función que crea las colas en el orquestador de mensajeria */
        public async Task<bool> CreatedQueues()
        {
            var resultGeneratedQueues = _context.GeneratedQueues.Include(x => x.QueueConf).Where(x => x.Active == true).ToList();
            foreach (var dr in resultGeneratedQueues)
            {
                await _rabbitMQFunctions.CreateQueueAsync(dr.Name,
                    dr.QueueConf.Durable,
                    dr.QueueConf.Exclusive,
                    dr.QueueConf.AutoDelete,
                    dr.QueueConf.MaxPriority,
                    dr.QueueConf.MessageLifeTime,
                    dr.QueueConf.QueueExpireTime,
                    dr.QueueConf.QueueMode,
                    dr.QueueConf.QueueDeadLetterExchange,
                    dr.QueueConf.QueueDeadLetterExchangeRoutingKey);
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
        private string GeneratedNameQueue(string prename, int order)
        {
            if (!string.IsNullOrEmpty(prename))
                return $"{order}.{prename}";
            return string.Empty;
        }
        /* Función que elimina todas las colas generadas */
        private async Task DeleteQueueGenerated()
        {
            var deleteQueues = await _context.GeneratedQueues.ToListAsync();
            _context.GeneratedQueues.RemoveRange(deleteQueues);
            await _context.SaveChangesAsync();
        }
        /* Función que elimina los messageProcess */
        private async Task DeleteProcessMessages()
        {
            var processMessages = await _context.ProcessMessages.ToListAsync();
            _context.ProcessMessages.RemoveRange(processMessages);
            await _context.SaveChangesAsync();
        }

        /* Función que elimina los messageProcess */
        private async Task DeleteProcessMessagesErroLog()
        {
            var processMessageErrorLogs = await _context.ProcessMessageErrorLogs.ToListAsync();
            _context.ProcessMessageErrorLogs.RemoveRange(processMessageErrorLogs);
            await _context.SaveChangesAsync();
        }
        #endregion
    }
}
