using FluentValidation;

namespace Example.Validation;

public class Order
{
    public string Product { get; set; }

    public int Quantity { get; set; }

    public string Email { get; set; }
}

public class OrderValidator : AbstractValidator<Order>
{
    public OrderValidator()
    {
        RuleFor(x => x.Product).NotEmpty();
        RuleFor(x => x.Quantity).InclusiveBetween(1, 100);
        RuleFor(x => x.Email).EmailAddress();
    }
}
