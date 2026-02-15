using Bunnings.Application.Implementation;
using Bunnings.Application.Interface;
using Bunnings.Domain.Models;
using Bunnings.Domain.Results;
using Moq;

namespace Bunnings.Application.Tests.Implementation;

[TestFixture]
public class HotProductCalculatorTests
{
    private Mock<IJsonFileProcessor> _jsonFileProcessorMock = null!;
    private HotProductCalculator _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _jsonFileProcessorMock = new Mock<IJsonFileProcessor>();
        _sut = new HotProductCalculator(_jsonFileProcessorMock.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _jsonFileProcessorMock.VerifyNoOtherCalls();
    }

    [Test]
    public async Task Calculate_WithSampleInput_ProducesExpectedResult()
    {
        // Arrange 
        var products = new[]
        {
            new Product("P1", "Ezy Storage 37L Flexi Laundry Basket - White"),
            new Product("P2", "Aandleford Black Seaford Post Mounted Letterbox"),
            new Product("P3", "Coolaroo 5.4m Square Graphite Premium Shade Sail Kit"),
            new Product("P4", "Ozito 80W Soldering Iron"),
            new Product("P5", "Richgro 25L All Purpose Garden Soil Mix"),
            new Product("P6", "Arlec 160W Crystalline Solar Foldable Charging Kit"),
        };

        var orders = new[]
        {
            Completed("O10", "C1", "19/07/2021", "P1"),
            Completed("O20", "C2", "19/07/2021", "P1"),
            Completed("O30", "C2", "19/07/2021", "P2"),
            Completed("O31", "C3", "19/07/2021", "P2", "P1"),
            Completed("O32", "C32", "19/07/2021", "P2"),

            // cancel order
            Cancelled("O30", "20/07/2021"),

            Completed("O40", "C3", "20/07/2021", "P4"),
            Completed("O60", "C3", "20/07/2021", "P4", "P1"),
            Completed("O70", "C4", "20/07/2021", "P5"),
            Completed("O80", "C5", "20/07/2021", "P1"),
            Completed("O81", "C5", "20/07/2021", "P1"), // same customer+product+day, excluded

            Completed("O90", "C5", "21/07/2021", "P1"),
            Completed("O100", "C3", "21/07/2021", "P4", "P6"),
        };

        _jsonFileProcessorMock
            .Setup(x => x.DeserializeListAsync<Product>(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(products.ToList());

        _jsonFileProcessorMock
            .Setup(x => x.DeserializeListAsync<Order>(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders.ToList());

        using var ordersStream = new MemoryStream([1]);
        using var productsStream = new MemoryStream([2]);

        // Act
        var result = await _sut.Calculate(ordersStream, productsStream);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.DailyTop, Has.Count.EqualTo(3));

        AssertDailyTop(result.DailyTop[0], new DateOnly(2021, 7, 19), "Ezy Storage 37L Flexi Laundry Basket - White");
        AssertDailyTop(result.DailyTop[1], new DateOnly(2021, 7, 20), "Ezy Storage 37L Flexi Laundry Basket - White");
        AssertDailyTop(result.DailyTop[2], new DateOnly(2021, 7, 21), "Arlec 160W Crystalline Solar Foldable Charging Kit");
    
        Assert.Multiple(() =>
        {    
            Assert.That(result.TopLast3Days, Is.Not.Null);
            Assert.That(result.TopLast3Days.From, Is.EqualTo(new DateOnly(2021, 7, 19)));
            Assert.That(result.TopLast3Days.To, Is.EqualTo(new DateOnly(2021, 7, 21)));
            Assert.That(result.TopLast3Days.ProductName, Is.EqualTo("Ezy Storage 37L Flexi Laundry Basket - White"));
        });

        _jsonFileProcessorMock.Verify(x => x.DeserializeListAsync<Product>(It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Once);
        _jsonFileProcessorMock.Verify(x => x.DeserializeListAsync<Order>(It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Calculate_WhenSalesEqual_SelectsAlphabeticallyByProductName()
    {
        // Arrange
        var products = new[]
        {
            new Product("P1", "Ezy Storage 37L Flexi Laundry Basket - White"),
            new Product("P2", "Aandleford Black Seaford Post Mounted Letterbox"),
        };

        var orders = new[]
        {
            Completed("O1", "C1", "21/07/2021", "P1"),
            Completed("O2", "C2", "21/07/2021", "P2"),
        };

        _jsonFileProcessorMock
            .Setup(x => x.DeserializeListAsync<Product>(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(products.ToList());

        _jsonFileProcessorMock
            .Setup(x => x.DeserializeListAsync<Order>(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders.ToList());

        using var ordersStream = new MemoryStream([1]);
        using var productsStream = new MemoryStream([2]);

        // Act
        var result = await _sut.Calculate(ordersStream, productsStream);

        // Assert
        Assert.That(result.DailyTop, Has.Count.EqualTo(1));
        AssertDailyTop(result.DailyTop.Single(), new DateOnly(2021, 7, 21), "Aandleford Black Seaford Post Mounted Letterbox");
        
        _jsonFileProcessorMock.Verify(x => x.DeserializeListAsync<Product>(It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Once);
        _jsonFileProcessorMock.Verify(x => x.DeserializeListAsync<Order>(It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Calculate_DoesNotDoubleCountSameCustomerOrders()
    {
        // Arrange
        var products = new[]
        {
            new Product("P1", "Ezy Storage 37L Flexi Laundry Basket - White"),
        };

        var orders = new[]
        {
            Completed("O1", "C1", "20/07/2021", "P1"),
            Completed("O2", "C1", "20/07/2021", "P1"), // should be excluded
        };

        _jsonFileProcessorMock
            .Setup(x => x.DeserializeListAsync<Product>(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(products.ToList());

        _jsonFileProcessorMock
            .Setup(x => x.DeserializeListAsync<Order>(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders.ToList());
        
        using var ordersStream = new MemoryStream([1]);
        using var productsStream = new MemoryStream([2]);

        // Act
        var result = await _sut.Calculate(ordersStream, productsStream);

        // Assert
        Assert.That(result.DailyTop, Has.Count.EqualTo(1));
        AssertDailyTop(result.DailyTop.Single(), new DateOnly(2021, 7, 20), "Ezy Storage 37L Flexi Laundry Basket - White");
        
        _jsonFileProcessorMock.Verify(x => x.DeserializeListAsync<Product>(It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Once);
        _jsonFileProcessorMock.Verify(x => x.DeserializeListAsync<Order>(It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static void AssertDailyTop(DailyTopProduct actual, DateOnly expectedDate, string expectedName)
    {
        Assert.Multiple(() =>
        {
            Assert.That(actual.Date, Is.EqualTo(expectedDate));
            Assert.That(actual.ProductName, Is.EqualTo(expectedName));
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
