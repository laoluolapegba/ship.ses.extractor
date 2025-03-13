namespace Ship.Ses.Extractor.Domain.Orders
{
    public sealed record OrderId(Guid Value)
    {
        public static explicit operator Guid(OrderId orderId) => orderId.Value;
    }
}
