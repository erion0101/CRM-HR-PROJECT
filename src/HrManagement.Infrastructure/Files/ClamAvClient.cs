using System.Buffers.Binary;
using System.Net.Sockets;
using System.Text;
using HrManagement.Application.Common.Interfaces;
using HrManagement.Application.Common.Models;
using HrManagement.Infrastructure.Files.Options;
using Microsoft.Extensions.Options;

namespace HrManagement.Infrastructure.Files;

public sealed class ClamAvClient(IOptions<ClamAvOptions> options) : IDocumentScanner
{
    public async Task<ScanResult> ScanAsync(Stream content, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(options.Value.TimeoutSeconds));
        using var client = new TcpClient();
        await client.ConnectAsync(options.Value.Host, options.Value.Port, timeout.Token);
        await using var network = client.GetStream();
        await network.WriteAsync("zINSTREAM\0"u8.ToArray(), timeout.Token);
        var buffer = new byte[8192];
        while (true)
        {
            var read = await content.ReadAsync(buffer, timeout.Token);
            if (read == 0) break;
            var length = new byte[4];
            BinaryPrimitives.WriteInt32BigEndian(length, read);
            await network.WriteAsync(length, timeout.Token);
            await network.WriteAsync(buffer.AsMemory(0, read), timeout.Token);
        }
        await network.WriteAsync(new byte[4], timeout.Token);
        var responseBuffer = new byte[1024];
        var responseLength = await network.ReadAsync(responseBuffer, timeout.Token);
        var response = Encoding.UTF8.GetString(responseBuffer, 0, responseLength).Trim('\0', '\r', '\n');
        if (response.EndsWith("OK", StringComparison.Ordinal)) return new ScanResult(true, response);
        if (response.Contains("FOUND", StringComparison.Ordinal)) return new ScanResult(false, response);
        throw new IOException($"Unexpected ClamAV response: {response}");
    }
}
