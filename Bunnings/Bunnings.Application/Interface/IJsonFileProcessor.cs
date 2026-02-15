using Bunnings.Domain.Models;

namespace Bunnings.Application.Interface;

public interface IJsonFileProcessor
{
    Task<List<T>> DeserializeListAsync<T>(Stream stream, CancellationToken ct = default);
}