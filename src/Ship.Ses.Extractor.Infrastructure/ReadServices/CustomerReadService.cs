using Ship.Ses.Extractor.Application.Customer.Shared;
using Ship.Ses.Extractor.Infrastructure.Exceptions;
using Ship.Ses.Extractor.Infrastructure.Persistance.MsSql;
using Microsoft.EntityFrameworkCore;

namespace Ship.Ses.Extractor.Infrastructure
{

    public class CustomerReadService : Application.Shared.ICustomerReadService
    {
        private readonly AppDbContext _dbContext;

        public CustomerReadService(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public IQueryable<T> ExecuteSqlQueryAsync<T>(string sql, object[] parameters, CancellationToken cancellationToken) where T : class
        {
            return _dbContext.Set<T>()
                .FromSqlRaw(sql, parameters)
                .AsNoTracking();
        }

        public async Task<CustomerDto> GetCustomerById(Guid customerId, CancellationToken cancellationToken)
        {
            var customer = await _dbContext.Customers
                .TagWithCallSite()
                .AsSplitQuery()
                .Where(o => (Guid)o.CustomerId == customerId)
                .FirstOrDefaultAsync(cancellationToken);

            if (customer is null)
            {
                throw new NotFoundException(customerId);
            }

            return customer.ToDto();
        }

    }
}
