﻿namespace Ship.Ses.Extractor.Domain.Orders
{
    public record Discount(decimal Amount, DiscountType Type);

    public enum DiscountType
    {
        TotalSpentMoneyInLast31Days = 1,
        OrderBasedAmount = 2
    }
}
