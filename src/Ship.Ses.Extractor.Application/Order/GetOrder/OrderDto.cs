﻿using Ship.Ses.Extractor.Domain.Orders;

namespace Ship.Ses.Extractor.Application.Order.GetOrder
{
    public sealed record OrderDto(Guid OrderId, List<OrderItemDto> OrderItems, decimal TotalAmount, string Currency);

    public sealed record OrderItemDto(Guid OrderItemId, string productName, decimal price, uint quantity);

    public static class OrderMapper
    {
        public static OrderDto ToDto(this Ship.Ses.Extractor.Domain.Orders.Order order)
        {
            return new OrderDto(order.OrderId.Value, order.OrderItems.ToDto(), order.TotalAmount.Amount, order.TotalAmount.Currency);
        }
        public static List<OrderItemDto> ToDto(this IReadOnlyCollection<OrderItem> orderItems)
        {
            return orderItems.Select(x => new OrderItemDto(x.OrderItemId.Value, x.ProductName, x.Price.Amount, x.Quantity)).ToList();
        }
    }
}
