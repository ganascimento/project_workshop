using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Api.Domain.Entities;
using Api.Domain.Interfaces.Repositories;
using Api.Infra.Context;
using Api.Infra.Repositories.Base;
using Microsoft.EntityFrameworkCore;

namespace Api.Infra.Repositories
{
    public class ScheduleRepository : Repository<ScheduleEntity>, IScheduleRepository
    {
        private DbSet<ScheduleEntity> _dataset;

        public ScheduleRepository(DataContext context) : base(context)
        {
            _dataset = context.Set<ScheduleEntity>();
        }

        public async Task<IEnumerable<ScheduleEntity>> SelectPeriodAsync(int workshopId, DateTime date)
        {
            return await _dataset.Where(p => p.WorkshopId.Equals(workshopId) && p.Date.Equals(date)).ToListAsync();
        }

        public async Task<IEnumerable<ScheduleEntity>> SelectPeriodAsync(int workshopId, DateTime startDate, DateTime endDate)
        {
            var schedules = await _dataset.Where(p => p.WorkshopId.Equals(workshopId) && p.Date >= startDate && p.Date <= endDate).ToListAsync();
            return schedules.OrderBy(x => x.Date).ToList();
        }
    }
}