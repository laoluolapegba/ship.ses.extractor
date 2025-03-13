using FluentValidation;

namespace Ship.Ses.Extractor.Application.Order.GetOrder
{
    public class GetOrderQueryValidator : AbstractValidator<GetOrderQuery>, IValidator
    {
        public GetOrderQueryValidator()
        {
            RuleFor(x => x.OrderId).NotEmpty();
        }
    }
}
