using Ship.Ses.Extractor.Domain.Customers.Exceptions;
using Ship.Ses.Extractor.Domain.Orders;

namespace Ship.Ses.Extractor.Domain.Customers
{
    public sealed record Address
    {
        public string Street { get; private set; }
        public string HouseNumber { get; private set; }
        public string FlatNumber { get; private set; }
        public string Country { get; private set; }
        public string PostalCode { get; private set; }

        public Address(string street, string houseNumber, string flatNumber, string country, string postalCode)
        {
            if (string.IsNullOrWhiteSpace(street) || street.Length > CustomerConstants.Customer.StreetMaxLength)
                throw new InvalidAddressDomainException(nameof(street));
            if (string.IsNullOrWhiteSpace(postalCode) || postalCode.Length > CustomerConstants.Customer.PostalCodeMaxLength)
                throw new InvalidAddressDomainException(nameof(postalCode));

            Street = street;
            HouseNumber = houseNumber;
            FlatNumber = flatNumber;
            Country = country;
            PostalCode = postalCode;
        }
    }
}
