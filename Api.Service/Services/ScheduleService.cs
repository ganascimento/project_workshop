using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Api.Domain.Dtos.Schedule;
using Api.Domain.Interfaces.Repositories;
using Api.Domain.Interfaces.Services;
using AutoMapper;
using System.Linq;
using Api.Domain.Entities;
using Api.Service.Helpers.interfaces;

namespace Api.Service.Services
{
    public class ScheduleService : IScheduleService
    {
        private readonly IScheduleRepository _scheduleRepository;
        private readonly IServiceRepository _serviceRepository;
        private readonly IIdentityService _identityService;
        private readonly IMapper _mapper;

        public ScheduleService(
            IScheduleRepository scheduleRepository,
            IMapper mapper,
            IServiceRepository serviceRepository,
            IIdentityService identityService
        )
        {
            _scheduleRepository = scheduleRepository;
            _mapper = mapper;
            _serviceRepository = serviceRepository;
            _identityService = identityService;
        }

        public async Task<IEnumerable<ScheduleDto>> GetToday()
        {
            var schedules = await _scheduleRepository.SelectPeriodAsync(
                _identityService.GetWorkshopId(),
                DateTime.Now.Date,
                DateTime.Now.Date.AddDays(1).AddSeconds(-1));
            return await MapSchedules(schedules);
            
        }

        public async Task<IEnumerable<ScheduleDto>> GetPeriod()
        {
            var schedules = await _scheduleRepository.SelectPeriodAsync(
                _identityService.GetWorkshopId(),
                DateTime.Now.Date,
                GetNextValidDay().AddDays(1).AddSeconds(-1));
            return await MapSchedules(schedules);
        }

        public async Task<IEnumerable<ScheduleDto>> GetPeriod(DateTime startDate, DateTime endDate)
        {
            var schedules = await _scheduleRepository.SelectPeriodAsync(
                _identityService.GetWorkshopId(),
                startDate,
                endDate.AddDays(1).AddSeconds(-1));
            return await MapSchedules(schedules);
        }

        public async Task<IEnumerable<ScheduleDtoAvailableWorkLoad>> GetAvailableWorkLoad()
        {
            var services = await _serviceRepository.SelectAsync();
            var schedules = await _scheduleRepository.SelectPeriodAsync(
                _identityService.GetWorkshopId(),
                DateTime.Now.Date,
                GetNextValidDay().AddDays(1).AddSeconds(-1));

            var data = new List<ScheduleDtoAvailableWorkLoad>();
            var dictionary = CreateDictionary();

            foreach (var schedule in schedules) {
                dictionary[schedule.Date.Date] -= services.FirstOrDefault(x => x.Id == schedule.ServiceId).WorkUnits;
            }

            foreach(var key in dictionary.Keys) {
                data.Add(new ScheduleDtoAvailableWorkLoad{
                    Date = key,
                    AvailableWorkLoad = dictionary[key]
                });
            }

            return data;
        }

        public async Task<ScheduleDtoCreateResult> Create(ScheduleDtoCreate dto)
        {
            await ValidToCreate(dto);
            var schedule = _mapper.Map<ScheduleEntity>(dto);
            schedule.WorkshopId = _identityService.GetWorkshopId();
            schedule = await _scheduleRepository.InsertAsync(schedule);

            return _mapper.Map<ScheduleDtoCreateResult>(schedule);
        }

        public async Task<bool> Remove(int id)
        {
            return await _scheduleRepository.DeleteAsync(id);
        }

        private async Task ValidToCreate(ScheduleDtoCreate dto) {
            if (IsWeekend(dto.Date))
                throw new Exception("Invalid day");

            var schedulesToday = await _scheduleRepository.SelectPeriodAsync(
                _identityService.GetWorkshopId(),
                dto.Date.Date,
                dto.Date.Date.AddDays(1).AddSeconds(-1));
            var services = await _serviceRepository.SelectAsync();
            var serviceWorkUnits = services.FirstOrDefault(x => x.Id == dto.ServiceId).WorkUnits;
            int workLoad = schedulesToday.Aggregate(0, (acc, x) => acc + services.FirstOrDefault(s => s.Id == x.ServiceId).WorkUnits) + serviceWorkUnits;

            if ((workLoad > 10 && !IsMoreWork(dto.Date)) || (workLoad > 13 && IsMoreWork(dto.Date)))
                throw new Exception("Workload exceeded");
        }

        private async Task<IEnumerable<ScheduleDto>> MapSchedules(IEnumerable<ScheduleEntity> entity) {
            var schedulesDto = entity.Select(schedule => _mapper.Map<ScheduleDto>(schedule)).ToList();
            var services = await _serviceRepository.SelectAsync();

            schedulesDto.ForEach(schedule => {
                schedule.ServiceName = services.FirstOrDefault(x => x.Id == schedule.ServiceId).Name;
            });

            return schedulesDto;
        }

        private Dictionary<DateTime, int> CreateDictionary() {
            var dictionary = new Dictionary<DateTime, int>();
            var date = DateTime.Now.Date;
            int count = 0;
            do {
                if (!IsWeekend(date)) {
                    count++;
                    var workload = IsMoreWork(date) ? 13 : 10;
                    dictionary.Add(date, workload);
                }
                date = date.AddDays(1);
            } while (count != 6);

            return dictionary;
        }

        private DateTime GetNextValidDay()
        {
            var date = DateTime.Now.Date;
            int count = 0;
            do {
                date = date.AddDays(1);
                if (!IsWeekend(date)) count++;
            } while (count != 5);

            return date;
        }

        private bool IsWeekend(DateTime date)
        {
            return date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;
        }

        private bool IsMoreWork(DateTime date)
        {
            return date.DayOfWeek == DayOfWeek.Thursday || date.DayOfWeek == DayOfWeek.Friday;
        }
    }
}
