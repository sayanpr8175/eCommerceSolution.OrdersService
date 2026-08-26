using eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO;
using FluentValidation;


namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.Validators;

public class OrderItemAddRequestValidator : AbstractValidator<OrderItemAddRequest>
{
    public OrderItemAddRequestValidator()
    {
        RuleFor(temp => temp.ProductID).NotEmpty().WithErrorCode("Product ID can't be empty!");

        RuleFor(temp => temp.UnitPrice).NotEmpty().WithErrorCode("UnitPrice can't be empty!")
            .GreaterThan(0).WithErrorCode("It can be zero or below zero");

        RuleFor(temp => temp.Quantity).NotEmpty().WithErrorCode("Quantity can not be empty!")
            .GreaterThan(0).WithErrorCode("Quantity must be greater than zero!");
    }
}


