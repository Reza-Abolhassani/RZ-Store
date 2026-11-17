using Asp.Versioning;

using AutoMapper;
using AutoMapper.QueryableExtensions;

using Data.Repositories;

using Entities;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace WebFramework.Api
{
    [ApiVersion("1")]
    public class CrudController<TDto, TSelectDto, TEntity, TKey> : BaseController
        where TDto : BaseDto<TDto, TEntity, TKey>, new()
        where TSelectDto : BaseDto<TSelectDto, TEntity, TKey>, new()
        where TEntity : BaseEntity<TKey>, new()
    {
        protected readonly IRepository<TEntity> Repository;
        protected readonly IMapper Mapper;

        public CrudController(IRepository<TEntity> repository, IMapper mapper)
        {
            Repository = repository;
            Mapper = mapper;
        }

        [HttpGet("[Action]")]
        public virtual async Task<ApiResult<PagedResult<TSelectDto>>> GetAll(int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 50 : Math.Min(pageSize, 100);

            var query = Repository.TableNoTracking
                .OrderByDescending(x => x.CreationDateTime)
                .ProjectTo<TSelectDto>(Mapper.ConfigurationProvider);

            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return Ok(new PagedResult<TSelectDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            });
        }

        [HttpGet("[Action]")]
        public virtual async Task<ApiResult<TSelectDto>> GetById(TKey id, CancellationToken cancellationToken)
        {
            var dto = await Repository.TableNoTracking.ProjectTo<TSelectDto>(Mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(p => p.Id.Equals(id), cancellationToken);

            if (dto == null)
                return NotFound();

            return dto;
        }

        [HttpPost("[Action]")]
        public virtual async Task<ApiResult> Create(TDto dto, CancellationToken cancellationToken)
        {
            var model = dto.ToEntity(Mapper);

            await Repository.AddAsync(model, cancellationToken);

            return Ok();
        }

        [HttpPut("{id}")]
        public virtual async Task<ApiResult> Update(TKey id, TDto dto, CancellationToken cancellationToken)
        {
            if (!id.Equals(dto.Id))
                return BadRequest("Route id and payload id do not match.");

            var model = await Repository.GetByIdAsync(cancellationToken, dto.Id);
            if (model == null)
                return NotFound();

            model = dto.ToEntity(Mapper, model);

            await Repository.UpdateAsync(model, cancellationToken);

            return Ok();
        }

        [HttpDelete("{id}")]
        public virtual async Task<ApiResult> Delete(TKey id, CancellationToken cancellationToken)
        {
            var model = await Repository.GetByIdAsync(cancellationToken, id);
            if (model == null)
                return NotFound();

            await Repository.DeleteAsync(model, cancellationToken);

            return Ok();
        }
    }

    public class CrudController<TDto, TSelectDto, TEntity> : CrudController<TDto, TSelectDto, TEntity, Guid>
        where TDto : BaseDto<TDto, TEntity, Guid>, new()
        where TSelectDto : BaseDto<TSelectDto, TEntity, Guid>, new()
        where TEntity : BaseEntity<Guid>, new()
    {
        public CrudController(IRepository<TEntity> repository, IMapper mapper)
            : base(repository, mapper)
        {
        }
    }

    public class CrudController<TDto, TEntity> : CrudController<TDto, TDto, TEntity, Guid>
        where TDto : BaseDto<TDto, TEntity, Guid>, new()
        where TEntity : BaseEntity<Guid>, new()
    {
        public CrudController(IRepository<TEntity> repository, IMapper mapper)
            : base(repository, mapper)
        {
        }
    }
}
