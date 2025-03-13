using FluentValidation;

namespace Ship.Ses.Extractor.Application.Customer.GetCustomer
{
    public class GetCustomerQueryValidator : AbstractValidator<GetCustomerQuery>, IValidator
    {
        public GetCustomerQueryValidator()
        {
            RuleFor(x => x.CustomerId).NotEmpty();
        }
    }
}
