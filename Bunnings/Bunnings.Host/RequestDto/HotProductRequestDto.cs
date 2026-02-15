namespace Bunnings.Application.RequestDto;

public class HotProductRequestDto
{
    public IFormFile ProductsFile { get; set; } = default!;

    public IFormFile OrdersFile { get; set; } = default!;
}