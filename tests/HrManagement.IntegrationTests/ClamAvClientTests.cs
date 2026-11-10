using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Text;
using HrManagement.Infrastructure.Files;
using HrManagement.Infrastructure.Files.Options;
using Microsoft.Extensions.Options;
using Xunit;

namespace HrManagement.IntegrationTests;

public sealed class ClamAvClientTests
{
    [Theory]
    [InlineData("stream: OK\0", true)]
    [InlineData("stream: Eicar-Test-Signature FOUND\0", false)]
    public async Task Parses_clean_and_infected_responses(string response, bool expectedClean)
    {
        var (port, server) = StartServer(response, respond: true);
        var client = new ClamAvClient(Options.Create(new ClamAvOptions { Host = "127.0.0.1", Port = port, TimeoutSeconds = 3 }));
        await using var content = new MemoryStream("test-content"u8.ToArray());
        var result = await client.ScanAsync(content, CancellationToken.None);
        Assert.Equal(expectedClean, result.IsClean);
        await server;
    }

    [Fact]
    public async Task Throws_when_clamav_is_unavailable()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port; listener.Stop();
        var client = new ClamAvClient(Options.Create(new ClamAvOptions { Host = "127.0.0.1", Port = port, TimeoutSeconds = 1 }));
        await Assert.ThrowsAnyAsync<SocketException>(() => client.ScanAsync(new MemoryStream("x"u8.ToArray()), CancellationToken.None));
    }

    [Fact]
    public async Task Times_out_when_clamav_does_not_respond()
    {
        var (port, server) = StartServer(string.Empty, respond: false);
        var client = new ClamAvClient(Options.Create(new ClamAvOptions { Host = "127.0.0.1", Port = port, TimeoutSeconds = 1 }));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.ScanAsync(new MemoryStream("x"u8.ToArray()), CancellationToken.None));
        await server;
    }

    private static (int Port, Task Server) StartServer(string response, bool respond)
    {
        var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var server = Task.Run(async () =>
        {
            try
            {
                using var socket = await listener.AcceptTcpClientAsync();
                await using var stream = socket.GetStream();
                var command = new byte[10]; await stream.ReadExactlyAsync(command);
                while (true)
                {
                    var lengthBytes = new byte[4]; await stream.ReadExactlyAsync(lengthBytes);
                    var length = BinaryPrimitives.ReadInt32BigEndian(lengthBytes);
                    if (length == 0) break;
                    var buffer = new byte[length]; await stream.ReadExactlyAsync(buffer);
                }
                if (respond) await stream.WriteAsync(Encoding.UTF8.GetBytes(response));
                else await Task.Delay(TimeSpan.FromSeconds(2));
            }
            finally { listener.Stop(); }
        });
        return (port, server);
    }
}
