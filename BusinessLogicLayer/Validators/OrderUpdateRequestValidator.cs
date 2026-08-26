using eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO;
using FluentValidation;

namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.Validators;


public class OrderUpdateRequestValidator : AbstractValidator<OrderUpdateRequest>
{
    public OrderUpdateRequestValidator()
    {
        
        RuleFor(temp => temp.OrderID).NotEmpty().WithErrorCode("OrderID can't be empty!");

        RuleFor(temp => temp.UserID).NotEmpty().WithErrorCode("USER id can't be empty!");

        RuleFor(temp => temp.OrderDate).NotEmpty().WithErrorCode("Order date can't be empty!");

        RuleFor(temp => temp.OrderItems).NotEmpty().WithErrorCode("At least 1 Order items required!");
    }
}

