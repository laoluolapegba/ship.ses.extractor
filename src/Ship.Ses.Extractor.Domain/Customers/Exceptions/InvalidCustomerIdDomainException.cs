namespace Ship.Ses.Extractor.Domain.Customers.Exceptions
{
    public class InvalidCustomerIdDomainException : DomainException
    {
        public InvalidCustomerIdDomainException(Guid id)
            : base($"The provided GUID '{id}' is not a valid Customer ID.")
        {

        }
    }
}
