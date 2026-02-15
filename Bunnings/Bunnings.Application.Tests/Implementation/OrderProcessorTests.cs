using Bunnings.Application.Implementation;
using Bunnings.Domain.Models;

namespace Bunnings.Application.Tests.Implementation;

[TestFixture]
public class OrderProcessorTests
{
    private OrderProcessor _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _sut = new OrderProcessor();
    }

    [Test]
    public void ProcessOrders_CountsOncePerDayCustomerProducts()
    {
        // Arrange
        var orders = new List<Order>
        {
            Completed("O1", "C1", "20/07/2021", "P1"),
            Completed("O2", "C1", "20/07/2021", "P1"), // same day+customer+product => should not increase
            Completed("O3", "C2", "20/07/2021", "P1"), // different customer => should count
        };

        // Act
        var result = _sut.ProcessOrders(orders);

        // Assert
        var date = new DateOnly(2021, 7, 20);
        Assert.Multiple(() =>
        {
            Assert.That(result.ContainsKey(date), Is.True);
            Assert.That(result[date]["P1"], Is.EqualTo(2));
        });
    }

    [Test]
    public void ProcessOrders_CancellationRemovesPreviouslyCountedPurchase()
    {
        // Arrange
        var orders = new List<Order>
        {
            Completed("O30", "C2", "19/07/2021", "P2"),
            Cancelled("O30", "20/07/2021"), // cancellation date doesn't matter; original order date does
        };

        // Act
        var result = _sut.ProcessOrders(orders);

        // Assert
        var date = new DateOnly(2021, 7, 19);
        Assert.Multiple(() =>
        {
            Assert.That(result.ContainsKey(date), Is.True);
            Assert.That(result[date].GetValueOrDefault("P2", 0), Is.EqualTo(0));
        });
    }

    [Test]
    public void ProcessOrders_CancellationWithoutOriginalOrder_IsIgnored()
    {
        // Arrange
        var orders = new List<Order>
        {
            Cancelled("MISSING", "20/07/2021"),
        };

        // Act
        var result = _sut.ProcessOrders(orders);

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void ProcessOrders_DistinctProducts_CountEachProductOnce()
    {
        // Arrange
        var orders = new List<Order>
        {
            Completed("O1", "C1", "21/07/2021", "P1", "P1", "P2"),
        };

        // Act
        var result = _sut.ProcessOrders(orders);

        // Assert
        var date = new DateOnly(2021, 7, 21);
        Assert.Multiple(() =>
        {
            Assert.That(result[date]["P1"], Is.EqualTo(1));
            Assert.That(result[date]["P2"], Is.EqualTo(1));
        });
    }

    private static Order Completed(string orderId, string customerId, string date, params string[] productIds) =>
        new()
        {
            OrderId = orderId,
            CustomerId = customerId,
            Date = date,
            Status = OrderStatus.Completed,
            Entries = productIds.Select(pid => new OrderLine
            {
                ProductId = pid,
                Quantity = 1
            }).ToList()
        };

    private static Order Cancelled(string orderId, string date) =>
        new()
        {
            OrderId = orderId,
            CustomerId = "IGNORED",
            Date = date,
            Status = OrderStatus.Canceled,
            Entries = []
        };
}
