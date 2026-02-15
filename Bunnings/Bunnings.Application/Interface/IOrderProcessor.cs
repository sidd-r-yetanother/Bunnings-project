using Bunnings.Domain.Models;

namespace Bunnings.Application.Interface;

public interface IOrderProcessor
{
    public Dictionary<DateOnly, Dictionary<string, int>> ProcessOrders(List<Order> orders);
}