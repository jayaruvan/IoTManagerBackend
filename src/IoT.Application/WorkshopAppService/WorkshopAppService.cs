﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Text;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using IoT.Application.WorkshopAppService.DTO;
using IoT.Core;
using IoT.Core.Cities;
using IoT.Core.Factories;
using IoT.Core.Workshops;
using L._52ABP.Application.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
//加入权限需要加入的库
using IoTManager.Authorization;
using Abp.Authorization;

namespace IoT.Application.WorkshopAppService
{
    public class WorkshopAppService:ApplicationService,IWorkshopAppService
    {
        private readonly IWorkshopRepository _workshopRepository;
        private readonly IFactoryRepository _factoryRepository;
        private readonly ICityRepository _cityRepository;
        private readonly IWorkshopManager _workshopManager;

        public WorkshopAppService(IWorkshopRepository workshopRepository,
            IFactoryRepository factoryRepository,
            ICityRepository cityRepository,
            IWorkshopManager workshopManager)
        {
            _workshopRepository = workshopRepository;
            _factoryRepository = factoryRepository;
            _cityRepository = cityRepository;
            _workshopManager = workshopManager;
        }

        [AbpAuthorize(PermissionNames.Pages_Workshop_View)]
        public WorkshopDto Get(EntityDto<int> input)
        {
            var query = _workshopRepository.GetAllIncluding(w => w.Factory)
                .Include(w => w.Factory.City).Where(w => w.Id == input.Id);
            var entity = query.FirstOrDefault();
            if (entity.IsNullOrDeleted())
            {
                throw new ApplicationException("该设备不存在或已被删除");
            }
            return ObjectMapper.Map<WorkshopDto>(entity);
        }

        [AbpAuthorize(PermissionNames.Pages_Workshop_View)]
        public WorkshopDto GetByName(string workshopName)
        {
            var query = _workshopRepository.GetAllIncluding(w => w.Factory).Where(w =>w.WorkshopName .Contains(workshopName)).Where(f => f.IsDeleted == false).Include(w=>w.Factory.City);
            var entity = query.FirstOrDefault();
            if (entity.IsNullOrDeleted())
            {
                throw new ApplicationException("该workshop不存在或已被删除");
            }
            var result = ObjectMapper.Map<WorkshopDto>(entity);
            return result;
        }

        [AbpAuthorize(PermissionNames.Pages_Workshop_View)]
        public PagedResultDto<WorkshopDto> GetAll(PagedSortedAndFilteredInputDto input)
        {
            var query = _workshopRepository.GetAllIncluding(w=>w.Factory).Where(w=>w.IsDeleted==false)
                .Include(w=>w.Factory.City);
            var total = query.Count();
            var result = input.Sorting != null
                ? query.OrderBy(input.Sorting).AsNoTracking().PageBy(input).ToList()
                : query.PageBy(input).ToList();
            return new PagedResultDto<WorkshopDto>(total, ObjectMapper.Map<List<WorkshopDto>>(result));
        }

        [AbpAuthorize(PermissionNames.Pages_Workshop_View)]
        [HttpGet]
        public PagedResultDto<WorkshopDto> GetByCity(string CityName)
        {
            var cityQuery = _cityRepository.GetAll().Where(c => c.CityName == CityName).Where(g => g.IsDeleted == false);
            if (!cityQuery.Any())
            {
                throw new ApplicationException("城市不存在或已被删除");
            }
            var query = _workshopRepository.GetAll().Where(d => d.IsDeleted == false).Where(w => w.Factory.City.CityName == CityName)
               .Include(w => w.Factory)
               .Include(w => w.Factory.City);
            var total = query.Count();
            var result = query.ToList();
            return new PagedResultDto<WorkshopDto>(total, ObjectMapper.Map<List<WorkshopDto>>(result));
        }

        [AbpAuthorize(PermissionNames.Pages_Workshop_View)]
        [HttpGet]
        public PagedResultDto<WorkshopDto> GetByFactory(string FactoryName)
        {
            var factoryQuery = _factoryRepository.GetAll().Where(f => f.FactoryName == FactoryName).Where(g => g.IsDeleted == false);
            if (!factoryQuery.Any())
            {
                throw new ApplicationException("factory不存在或已被删除");
            }
            var query = _workshopRepository.GetAll().Where(d => d.IsDeleted == false).Where(w => w.Factory.FactoryName == FactoryName)
               .Include(w => w.Factory)
               .Include(w => w.Factory.City);
            var total = query.Count();
            var result = query.ToList();
            return new PagedResultDto<WorkshopDto>(total, ObjectMapper.Map<List<WorkshopDto>>(result));
        }

        [HttpGet]
        public long GetNumber()
        {
            var query = _workshopRepository.GetAll().Where(w => w.IsDeleted == false);
            return query.Count();
        }

        public WorkshopDto Create(CreateWorkshopDto input)
        {
            var workshopQuery = _workshopRepository.GetAll().Where(w => w.WorkshopName == input.WorkshopName);
            if ((workshopQuery.Any()) && (workshopQuery.FirstOrDefault().IsDeleted == true))
            {
                var entity_old = workshopQuery.FirstOrDefault();
                entity_old.IsDeleted = false;
                var result_old = _workshopRepository.Update(entity_old);
                CurrentUnitOfWork.SaveChanges();
                return ObjectMapper.Map<WorkshopDto>(result_old);
            }
            if (workshopQuery.Any())
            {
                throw new ApplicationException("WorkshopName重复");
            }

            var cityQuery = _cityRepository.GetAll().Where(c => c.CityName == input.CityName);
            var city = cityQuery.FirstOrDefault();
            if (city.IsNullOrDeleted())
            {
                throw new ApplicationException("City不存在或输入错误");
            }

            var factoryQuery = _factoryRepository.GetAll().Where(f => f.FactoryName == input.FactoryName);
            var factory = factoryQuery.FirstOrDefault();
            if (factory.IsNullOrDeleted())
            {
                throw new ApplicationException("Factory不存在或输入错误");
            }

            

            var entity = ObjectMapper.Map<Workshop>(input);
            entity.Factory = factory;
            var result = _workshopRepository.Insert(entity);
            CurrentUnitOfWork.SaveChanges();
            return ObjectMapper.Map<WorkshopDto>(result);
        }

        public WorkshopDto Update(CreateWorkshopDto input)
        {
            var cityQuery = _cityRepository.GetAll().Where(c => c.CityName == input.CityName);
            var city = cityQuery.FirstOrDefault();
            if (city.IsNullOrDeleted())
            {
                throw new ApplicationException("City不存在或输入错误");
            }

            var factoryQuery = _factoryRepository.GetAll().Where(f => f.FactoryName == input.FactoryName);
            var factory = factoryQuery.FirstOrDefault();
            if (factory.IsNullOrDeleted())
            {
                throw new ApplicationException("Factory不存在或输入错误");
            }

            var workshop = _workshopRepository.Get(input.Id);
            ObjectMapper.Map(input, workshop);
            workshop.Factory = factory;
            var result = _workshopRepository.Update(workshop);
            CurrentUnitOfWork.SaveChanges();
            return ObjectMapper.Map<WorkshopDto>(workshop);
        }

        public void Delete(EntityDto<int> input)
        {
            var entity = _workshopRepository.Get(input.Id);
            _workshopManager.Delete(entity);
        }

        [HttpDelete]
        public void BatchDelete(int[] inputs)
        {
            foreach (var input in inputs)
            {
                var entity = _workshopRepository.Get(input);
                _workshopManager.Delete(entity);
            }
        }
    }
}
