using Bunnings.Application.Interface;
using Bunnings.Domain.Models;
using Newtonsoft.Json;

namespace Bunnings.Application.Implementation;

public class JsonFileProcessor : IJsonFileProcessor
{
    public async Task<List<T>> DeserializeListAsync<T>(Stream stream, CancellationToken ct)
    {
        if (stream is null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        if (!stream.CanRead)
        {
            throw new ArgumentException("Stream must be readable.", nameof(stream));
        }

        using var sr = new StreamReader(stream,leaveOpen: true);
        var json = await sr.ReadToEndAsync(ct);

        var data = JsonConvert.DeserializeObject<List<T>>(json);

        return data ?? throw new InvalidOperationException($"Invalid JSON. Expected a JSON array of {typeof(T).Name}.");
    }
}