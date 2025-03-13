
namespace Ship.Ses.Extractor.Domain.Orders
{
    public interface IOrderRepository
    {
        Task AddAsync(Order order, CancellationToken cancellationToken = default);
        Task<Order> GetOrderById(OrderId orderId, CancellationToken cancellationToken = default);
        Task<decimal> GetTotalSpentInLast31DaysAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}
