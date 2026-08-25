using eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.Validators;

public class OrderAddRequestValidator : AbstractValidator<OrderAddRequest>
{
    public OrderAddRequestValidator()
    {
        RuleFor(temp => temp.UserId).NotEmpty().WithErrorCode("User ID can't be empty!");

        RuleFor(temp => temp.OrderDate).NotEmpty().WithErrorCode("Orderdate can't be empty!");

        RuleFor(temp => temp.OrderItems).NotEmpty().WithErrorCode("At least 1 Order items required!");
    }
}
