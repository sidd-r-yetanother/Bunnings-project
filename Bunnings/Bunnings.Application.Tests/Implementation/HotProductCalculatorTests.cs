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
    private Mock<IOrderProcessor> _orderProcessorMock = null!;
    private HotProductCalculator _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _jsonFileProcessorMock = new Mock<IJsonFileProcessor>(MockBehavior.Strict);
        _orderProcessorMock = new Mock<IOrderProcessor>(MockBehavior.Strict);

        _sut = new HotProductCalculator(_jsonFileProcessorMock.Object, _orderProcessorMock.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _jsonFileProcessorMock.VerifyNoOtherCalls();
        _orderProcessorMock.VerifyNoOtherCalls();
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
        }.ToList();

        var orders = new List<Order>();

        var trackByDate = new Dictionary<DateOnly, Dictionary<string, int>>
        {
            [new DateOnly(2021, 7, 19)] = new() { ["P1"] = 3, ["P2"] = 2 },
            [new DateOnly(2021, 7, 20)] = new() { ["P1"] = 1, ["P4"] = 2, ["P5"] = 1 },
            [new DateOnly(2021, 7, 21)] = new() { ["P1"] = 1, ["P4"] = 1, ["P6"] = 1 },
        };

        _jsonFileProcessorMock
            .Setup(x => x.DeserializeListAsync<Order>(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        _jsonFileProcessorMock
            .Setup(x => x.DeserializeListAsync<Product>(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        _orderProcessorMock
            .Setup(x => x.ProcessOrders(It.IsAny<List<Order>>()))
            .Returns(trackByDate);

        using var ordersStream = new MemoryStream([1]);
        using var productsStream = new MemoryStream([2]);

        // Act
        var result = await _sut.Calculate(ordersStream, productsStream);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.DailyTop, Has.Count.EqualTo(3));

        AssertDailyTop(result.DailyTop[0], new DateOnly(2021, 7, 19), "Ezy Storage 37L Flexi Laundry Basket - White");
        AssertDailyTop(result.DailyTop[1], new DateOnly(2021, 7, 20), "Ozito 80W Soldering Iron");
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
        _orderProcessorMock.Verify(x => x.ProcessOrders(It.IsAny<List<Order>>()), Times.Once);
    }

    [Test]
    public async Task Calculate_WhenSalesEqual_SelectsAlphabeticallyByProductName()
    {
        // Arrange
        var products = new[]
        {
            new Product("P1", "Ezy Storage 37L Flexi Laundry Basket - White"),
            new Product("P2", "Aandleford Black Seaford Post Mounted Letterbox"),
        }.ToList();

        var orders = new List<Order>();

        var trackByDate = new Dictionary<DateOnly, Dictionary<string, int>>
        {
            [new DateOnly(2021, 7, 21)] = new() { ["P1"] = 1, ["P2"] = 1 }
        };

        _jsonFileProcessorMock
            .Setup(x => x.DeserializeListAsync<Order>(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        _jsonFileProcessorMock
            .Setup(x => x.DeserializeListAsync<Product>(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        _orderProcessorMock
            .Setup(x => x.ProcessOrders(It.IsAny<List<Order>>()))
            .Returns(trackByDate);

        using var ordersStream = new MemoryStream([1]);
        using var productsStream = new MemoryStream([2]);

        // Act
        var result = await _sut.Calculate(ordersStream, productsStream);

        // Assert
        Assert.That(result.DailyTop, Has.Count.EqualTo(1));
        AssertDailyTop(result.DailyTop.Single(), new DateOnly(2021, 7, 21), "Aandleford Black Seaford Post Mounted Letterbox");

        _jsonFileProcessorMock.Verify(x => x.DeserializeListAsync<Product>(It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Once);
        _jsonFileProcessorMock.Verify(x => x.DeserializeListAsync<Order>(It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Once);
        _orderProcessorMock.Verify(x => x.ProcessOrders(It.IsAny<List<Order>>()), Times.Once);
    }

    private static void AssertDailyTop(DailyTopProduct actual, DateOnly expectedDate, string expectedName)
    {
        Assert.Multiple(() =>
        {
            Assert.That(actual.Date, Is.EqualTo(expectedDate));
            Assert.That(actual.ProductName, Is.EqualTo(expectedName));
        });
    }
}
