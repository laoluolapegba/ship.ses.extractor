namespace Ship.Ses.Extractor.Domain.Orders
{
    public sealed record Money(decimal Amount, string Currency = "USD");
}
