namespace Ship.Ses.Extractor.Domain.Orders.Exceptions
{
    public class MaximumQuantityExceededDomainException : DomainException
    {
        public MaximumQuantityExceededDomainException()
            : base("Maximum allowed quantity has been exceeded")
        {

        }
    }
}
