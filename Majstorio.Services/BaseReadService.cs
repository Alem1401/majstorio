using Majstorio.Model.Responses;
using Majstorio.Model.SearchObjects;
using Majstorio.Services.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Linq.Dynamic.Core;
using MapsterMapper;

namespace Majstorio.Services
{
    public abstract class BaseReadService<TEntity, TResponse, TSearch> : IBaseReadService<TResponse, TSearch>
        where TEntity : class
        where TSearch : BaseSearchObject
    {
        protected readonly IMapper _mapper;
        protected readonly MajstorioDbContext _dbContext;

        protected BaseReadService(IMapper mapper, MajstorioDbContext dbContext)
        {
            _mapper = mapper;
            _dbContext = dbContext;
        }


   
        protected abstract IEnumerable<TEntity> ApplyFilters(IEnumerable<TEntity> query, TSearch? search);

        public virtual async Task<PageResult<TResponse>> GetAllAsync(TSearch? search = null)
        {
            IEnumerable<TEntity> query = this._dbContext.Set<TEntity>();

            query = await IncludeRelatedEntitiesAsync(search, query.AsQueryable());
            query = ApplyFilters(query, search);

            int? totalCount = null;

            if (search.IncludeTotalCount ?? false)
            {
                totalCount = query.Count();
            }

            if (!string.IsNullOrWhiteSpace(search.SortBy))
            {
                
                query = query.AsQueryable().OrderBy(search.SortBy);
            }

            if (search.Page.HasValue)
            {
                query = query.Skip((search.Page.Value - 1) * search.PageSize.Value);
            }

            if (search.PageSize.HasValue)
            {
                query = query.Take(search.PageSize.Value);
            }

            var list = query.Select(item => _mapper.Map<TResponse>(item)).ToList();

            var pageResult = new PageResult<TResponse>
            {
                Items = list,
                TotalCount = totalCount
            };

            return await Task.FromResult(pageResult);
        }

        protected virtual async Task<IQueryable<TEntity>> IncludeRelatedEntitiesAsync(TSearch? search, IQueryable<TEntity> query = null)
        {
            
            return query;
        }


        public virtual async Task<TResponse> GetByIdAsync(int id)
        {
            var entity = this._dbContext.Set<TEntity>().Find(id);
            if (entity == null)
            {
                throw new KeyNotFoundException($"{typeof(TEntity).Name} with id {id} not found.");
            }

            return await Task.FromResult(_mapper.Map<TResponse>(entity));
        }
    }
}
