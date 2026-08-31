using AutoMapper;
using eCommerce.OrdersMicroservice.BusinessLogicLayer.DTO;
using eCommerce.OrdersMicroservice.BusinessLogicLayer.ServiceContracts;
using eCommerce.OrdersMicroservice.BusinessLogicLayer.Validators;
using eCommerce.OrdersMicroservice.DataAccessLayer.Entities;
using eCommerce.OrdersMicroservice.DataAccessLayer.RepositoryContracts;
using FluentValidation;
using FluentValidation.Results;
using MongoDB.Driver;

namespace eCommerce.OrdersMicroservice.BusinessLogicLayer.Services;

public class OrdersService : IOrdersService
{

    private readonly IValidator<OrderAddRequest> _orderAddRequestValidator;
    private readonly IValidator<OrderItemAddRequest> _orderItemAddRequestValidator;
    private readonly IValidator<OrderUpdateRequest> _orderUpdateRequestValidator;
    private readonly IValidator<OrderItemUpdateRequest> _orderItemUpdateRequestValidator;
    private readonly IMapper _mapper;
    private IOrdersRepository _ordersRepository;
    private UsersMicroserviceClient _userMicroserviceClient;
    private ProductsMicroserviceClient _productsMicroserviceClient;

    public OrdersService(IOrdersRepository ordersRepository, IMapper mapper, 
        IValidator<OrderAddRequest> orderAddRequestValidator,
        IValidator<OrderItemAddRequest> orderItemAddRequestValidator,
        IValidator<OrderUpdateRequest> orderUpdateRequestValidator,
        IValidator<OrderItemUpdateRequest> orderItemUpdateRequestValidator,
        UsersMicroserviceClient userMicroserviceClient,
        ProductsMicroserviceClient productsMicroserviceClient)
    {
        _orderAddRequestValidator = orderAddRequestValidator;
        _orderItemAddRequestValidator = orderItemAddRequestValidator;
        _orderItemUpdateRequestValidator = orderItemUpdateRequestValidator;
        _orderUpdateRequestValidator = orderUpdateRequestValidator;
        _mapper = mapper;
        _ordersRepository = ordersRepository;
        _userMicroserviceClient = userMicroserviceClient;
        _productsMicroserviceClient = productsMicroserviceClient;

    }
    public async Task<OrderResponse?> AddOrder(OrderAddRequest orderAddRequest)
    {
        if(orderAddRequest == null)
        {
            throw new ArgumentNullException(nameof(orderAddRequest));
        }

        // Validate order add req
        ValidationResult orderAddRequestValidationResult =  await _orderAddRequestValidator.ValidateAsync(orderAddRequest);

        if(!orderAddRequestValidationResult.IsValid)
        {
            string errors = string.Join(", ", orderAddRequestValidationResult.Errors.Select(e => e.ErrorMessage));

            throw new ArgumentException(errors);
        }

        List<ProductDTO?> products = new List<ProductDTO?>();

        foreach (OrderItemAddRequest orderItemAddRequest in orderAddRequest.OrderItems)
        {
            ValidationResult orderItemAddRequestValidationResult = await _orderItemAddRequestValidator.ValidateAsync(orderItemAddRequest);

            if(!orderAddRequestValidationResult.IsValid)
            {
                string errors = string.Join(", ", orderAddRequestValidationResult.Errors.Select(e => e.ErrorMessage));

                throw new ArgumentException(errors);
            }

            // Check if ProductID exists or not?
            ProductDTO? product = await _productsMicroserviceClient.GetProductByProductID(orderItemAddRequest.ProductID);

            if(product == null)
            {
                throw new ArgumentException("Invalid Product ID");
            }

            products.Add(product);


        }

        // We have to check for if UserID exists in Users microservice endpoint

        UserDTO? user = await _userMicroserviceClient.GetUserByUserID(orderAddRequest.UserId);

        if(user == null)
        {
            throw new ArgumentException("Invalid User ID");
        }



        // convert data from orderAddReq to order

        Order orderInput = _mapper.Map<Order>(orderAddRequest);

        foreach(OrderItem orderitem in orderInput.OrderItems)
        {
            orderitem.TotalPrice = orderitem.Quantity * orderitem.UnitPrice;
        }

        orderInput.TotalBill = orderInput.OrderItems.Sum(temp => temp.TotalPrice);

        Order? placedOrder = await _ordersRepository.AddOrder(orderInput);

        if(placedOrder == null)
        {
            return null;
        }
        OrderResponse addedOrderResponse = _mapper.Map<OrderResponse>(placedOrder);

        // Load ProductName and Category in OrderItem

        if (addedOrderResponse != null)
        {
            foreach (OrderItemResponse orderItemResponse in addedOrderResponse.OrderItems)
            {
                ProductDTO? productDTO = products.Where(temp => temp.ProductID == orderItemResponse.ProductID).FirstOrDefault();

                if (productDTO != null)
                {
                    _mapper.Map<ProductDTO, OrderItemResponse>(productDTO, orderItemResponse);
                }

            }
        }

        // Load UserPersonName and Email from Users Microservice
        if (addedOrderResponse != null)
        {
            if (user != null)
            {
                _mapper.Map<UserDTO, OrderResponse>(user, addedOrderResponse);
            }
        }

        return addedOrderResponse;
    }

    public async Task<bool> DeleteOrder(Guid orderID)
    {
        FilterDefinition<Order> filter = Builders<Order>.Filter.Eq(temp => temp.OrderID, orderID);

        Order? existingOrder = await _ordersRepository.GetOrderByCondition(filter);

        if(existingOrder == null)
        {
            return false;
        }

        bool isDeleted = await _ordersRepository.DeleteOrder(orderID);

        return isDeleted;
    }

    public async Task<OrderResponse?> GetOrderByCondition(FilterDefinition<Order> filter)
    {
        Order? order = await _ordersRepository.GetOrderByCondition(filter);

        if (order == null)
        {
            return null;
        }

        OrderResponse orderResponse = _mapper.Map<OrderResponse>(order);

        // I have not added validation of product id here, incase later will add: Done

        //Load ProductName and Category in OrderItem
        if (orderResponse != null)
        {
            foreach (OrderItemResponse orderItemResponse in orderResponse.OrderItems)
            {
                ProductDTO? productDTO = await _productsMicroserviceClient.GetProductByProductID(orderItemResponse.ProductID);

                if (productDTO == null)
                    continue;

                _mapper.Map<ProductDTO, OrderItemResponse>(productDTO, orderItemResponse);
            }
        }

        //Load UserPersonName and Email from Users Microservice
        if (orderResponse != null)
        {
            UserDTO? user = await _userMicroserviceClient.GetUserByUserID(orderResponse.UserID);
            if (user != null)
            {
                _mapper.Map<UserDTO, OrderResponse>(user, orderResponse);
            }
        }


        return orderResponse;
    }

    public async Task<List<OrderResponse?>> GetOrdersByCondition(FilterDefinition<Order> filter)
    {
        IEnumerable<Order?> orders = await _ordersRepository.GetOrdersByCondition(filter);

        IEnumerable<OrderResponse?> orderResponses = _mapper.Map<IEnumerable<OrderResponse>>(orders);

        foreach (OrderResponse? orderResponse in orderResponses)
        {
            if (orderResponse == null)
            {
                continue;
            }

            foreach (OrderItemResponse orderItemResponse in orderResponse.OrderItems)
            {
                ProductDTO? productDTO = await _productsMicroserviceClient.GetProductByProductID(orderItemResponse.ProductID);

                if (productDTO != null)
                {
                    _mapper.Map<ProductDTO, OrderItemResponse>(productDTO, orderItemResponse);
                }

            }

            // Loading users PersonName and Email from Users Microservice

            UserDTO? user = await _userMicroserviceClient.GetUserByUserID(orderResponse.UserID);

            if (user != null)
            {
                _mapper.Map<UserDTO, OrderResponse>(user, orderResponse);
            }

        }

        return orderResponses.ToList();
    }

    public async Task<List<OrderResponse?>> GetOrders()
    {
        IEnumerable<Order?> orders = await _ordersRepository.GetOrders();

        IEnumerable<OrderResponse?> orderResponses = _mapper.Map<IEnumerable<OrderResponse>>(orders);

        foreach(OrderResponse? orderResponse in orderResponses)
        {
            if(orderResponse == null)
            {
                continue;
            }

            foreach (OrderItemResponse orderItemResponse in orderResponse.OrderItems)
            {
                ProductDTO? productDTO = await _productsMicroserviceClient.GetProductByProductID(orderItemResponse.ProductID);

                if(productDTO != null)
                {
                    _mapper.Map<ProductDTO, OrderItemResponse>(productDTO, orderItemResponse);
                }
                
            }

            // Loading users PersonName and Email from Users Microservice

            UserDTO? user = await _userMicroserviceClient.GetUserByUserID(orderResponse.UserID);

            if(user!=null)
            {
                _mapper.Map<UserDTO, OrderResponse>(user, orderResponse);
            }

        }


        return orderResponses.ToList();
    }


    public async Task<OrderResponse?> UpdateOrder(OrderUpdateRequest orderUpdateRequest)
    {
        if (orderUpdateRequest == null)
        {
            throw new ArgumentNullException(nameof(orderUpdateRequest));
        }


        //Validate OrderUpdateRequest
        ValidationResult orderUpdateRequestValidationResult = await _orderUpdateRequestValidator.ValidateAsync(orderUpdateRequest);
        if (!orderUpdateRequestValidationResult.IsValid)
        {
            string errors = string.Join(", ", orderUpdateRequestValidationResult.Errors.Select(temp => temp.ErrorMessage));
            throw new ArgumentException(errors);
        }

        List<ProductDTO> products = new List<ProductDTO>();

        //Validate order items
        foreach (OrderItemUpdateRequest orderItemUpdateRequest in orderUpdateRequest.OrderItems)
        {
            ValidationResult orderItemUpdateRequestValidationResult = await _orderItemUpdateRequestValidator.ValidateAsync(orderItemUpdateRequest);

            if (!orderItemUpdateRequestValidationResult.IsValid)
            {
                string errors = string.Join(", ", orderItemUpdateRequestValidationResult.Errors.Select(temp => temp.ErrorMessage));
                throw new ArgumentException(errors);
            }

            ProductDTO? product = await _productsMicroserviceClient.GetProductByProductID(orderItemUpdateRequest.ProductID);

            if (product == null)
            {
                throw new ArgumentException("Invalid Product ID");
            }

            products.Add(product);

        }

        //checking if UserID exists in Users microservice

        UserDTO? user = await _userMicroserviceClient.GetUserByUserID(orderUpdateRequest.UserID);

        if (user == null)
        {
            throw new ArgumentException("Invalid User ID");
        }

        Order orderInput = _mapper.Map<Order>(orderUpdateRequest);

        
        foreach (OrderItem orderItem in orderInput.OrderItems)
        {
            orderItem.TotalPrice = orderItem.Quantity * orderItem.UnitPrice;
        }
        orderInput.TotalBill = orderInput.OrderItems.Sum(temp => temp.TotalPrice);


        
        Order? updatedOrder = await _ordersRepository.UpdateOrder(orderInput);

        if (updatedOrder == null)
        {
            return null;
        }

        OrderResponse updatedOrderResponse = _mapper.Map<OrderResponse>(updatedOrder);

        // Load ProductName and Category in OrderItem

        if (updatedOrderResponse != null)
        {
            foreach (OrderItemResponse orderItemResponse in updatedOrderResponse.OrderItems)
            {
                ProductDTO? productDTO = products.Where(temp => temp.ProductID == orderItemResponse.ProductID).FirstOrDefault();

                if (productDTO != null)
                {
                    _mapper.Map<ProductDTO, OrderItemResponse>(productDTO, orderItemResponse);
                }

            }
        }

        //Load UserPersonName and Email from Users Microservice
        if (updatedOrderResponse != null)
        {
            if (user != null)
            {
                _mapper.Map<UserDTO, OrderResponse>(user, updatedOrderResponse);
            }
        }


        return updatedOrderResponse;
    }
}
