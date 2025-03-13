using Ship.Ses.Extractor.Application.Order.BrowseOrders;
using Ship.Ses.Extractor.Application.Order.GetOrder;

namespace Ship.Ses.Extractor.Application.Shared
{
    public interface IOrderReadService
    {
        IQueryable<T> ExecuteSqlQueryAsync<T>(string sql, object[] parameters, CancellationToken cancellationToken) where T : class;

        Task<BrowseOrdersDto> BrowseOrders(Guid customerId, PaginationParameters paginationParameters, CancellationToken cancellationToken);
        Task<OrderDto> GetOrderById(Guid orderId, CancellationToken cancellationToken);
    }
}
