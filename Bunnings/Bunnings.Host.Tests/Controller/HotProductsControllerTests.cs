using Bunnings.Application.Controller;
using Bunnings.Application.Interface;
using Bunnings.Application.RequestDto;
using Bunnings.Domain.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace Bunnings.Host.Tests.Controller;

[TestFixture]
public class HotProductsControllerTests
{
    private Mock<ILogger<HotProductsController>> _logger = null!;
    private Mock<IHotProductCalculator> _calculator = null!;
    private HotProductsController _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _logger = new Mock<ILogger<HotProductsController>>();
        _calculator = new Mock<IHotProductCalculator>(MockBehavior.Strict);
        _sut = new HotProductsController(_logger.Object, _calculator.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _calculator.VerifyNoOtherCalls();
    }

    [Test]
    public async Task GetHotProduct_WhenCalculatorSucceeds_ReturnsOk()
    {
        // Arrange
        var expected = new HotProductsResult(
            DailyTop: [],
            TopLast3Days: new PeriodTopProduct(new DateOnly(2021, 7, 19), new DateOnly(2021, 7, 21), "Some product"));

        var request = new HotProductRequestDto
        {
            OrdersFile = CreateFormFile("orders.json", "{ }"),
            ProductsFile = CreateFormFile("products.json", "{ }"),
        };

        _calculator
            .Setup(x => x.Calculate(It.IsAny<Stream>(), It.IsAny<Stream>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _sut.GetHotProduct(request);

        // Assert
        Assert.That(result, Is.TypeOf<OkObjectResult>());
        var ok = (OkObjectResult)result;

        Assert.Multiple(() =>
        {
            Assert.That(ok.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
            Assert.That(ok.Value, Is.SameAs(expected));
        });

        _calculator.Verify(x => x.Calculate(It.IsAny<Stream>(), It.IsAny<Stream>()), Times.Once);
    }

    [Test]
    public async Task GetSizzlingHotProduct_WhenCalculatorThrows_ReturnsProblem_AndLogsError()
    {
        // Arrange
        var request = new HotProductRequestDto
        {
            OrdersFile = CreateFormFile("orders.json", "{ }"),
            ProductsFile = CreateFormFile("products.json", "{ }"),
        };

        var ex = new InvalidOperationException("exception");

        _calculator
            .Setup(x => x.Calculate(It.IsAny<Stream>(), It.IsAny<Stream>()))
            .ThrowsAsync(ex);

        // Act
        var result = await _sut.GetHotProduct(request);

        // Assert
        Assert.That(result, Is.TypeOf<ObjectResult>());
        var objectResult = (ObjectResult)result;

        Assert.Multiple(() =>
        {
            Assert.That(objectResult.StatusCode, Is.EqualTo(StatusCodes.Status500InternalServerError));
            Assert.That(objectResult.Value, Is.TypeOf<ProblemDetails>());
        });

        var problem = (ProblemDetails)objectResult.Value!;
        Assert.That(problem.Detail, Is.EqualTo("An error occurred while calculating hot products."));

        _calculator.Verify(x => x.Calculate(It.IsAny<Stream>(), It.IsAny<Stream>()), Times.Once);
    }

    private static IFormFile CreateFormFile(string fileName, string content)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(bytes);

        return new FormFile(stream, 0, bytes.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/json"
        };
    }
}
