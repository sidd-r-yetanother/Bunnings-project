using Bunnings.Domain.Results;

namespace Bunnings.Application.Interface;

public interface IHotProductCalculator
{
    Task<HotProductsResult> Calculate(Stream ordersFile, Stream productsFile);
}