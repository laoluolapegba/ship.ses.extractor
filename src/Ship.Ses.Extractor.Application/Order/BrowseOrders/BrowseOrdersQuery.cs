using Ship.Ses.Extractor.Application.Shared;

namespace Ship.Ses.Extractor.Application.Order.BrowseOrders
{
    public sealed record BrowseOrdersQuery(Guid CustomerId, PaginationParameters PaginationParameters);
}
