

using eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO;
using FluentValidation;

namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.Validators;

public class OrderItemUpdateRequestValidator : AbstractValidator<OrderItemUpdateRequest>
{
    public OrderItemUpdateRequestValidator()
    {

        RuleFor(temp => temp.ProductID).NotEmpty().WithErrorCode("OrderID can't be empty!");

        RuleFor(temp => temp.UnitPrice).NotEmpty().WithErrorCode("unit price can't be empty!");

        RuleFor(temp => temp.Quantity).NotEmpty().WithErrorCode("Order date can't be empty!");

    }
}

