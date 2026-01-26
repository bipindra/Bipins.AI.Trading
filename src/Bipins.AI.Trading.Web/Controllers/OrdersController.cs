using Bipins.AI.Trading.Domain.Entities;
using Bipins.AI.Trading.Application.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bipins.AI.Trading.Web.Controllers;

[Authorize]
public class OrdersController : Controller
{
    private readonly IOrderRepository _orderRepository;
    private readonly IFillRepository _fillRepository;
    private readonly ILogger<OrdersController> _logger;
    
    public OrdersController(
        IOrderRepository orderRepository,
        IFillRepository fillRepository,
        ILogger<OrdersController> logger)
    {
        _orderRepository = orderRepository;
        _fillRepository = fillRepository;
        _logger = logger;
    }
    
    public async Task<IActionResult> Index(
        OrderStatus? status = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        var orders = await _orderRepository.GetOrdersAsync(status, from, to, cancellationToken);
        return View(orders);
    }
    
    public async Task<IActionResult> Fills(
        string? orderId = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        var fills = await _fillRepository.GetFillsAsync(orderId, from, to, cancellationToken);
        return View(fills);
    }
}
