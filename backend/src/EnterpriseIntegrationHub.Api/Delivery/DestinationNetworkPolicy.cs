using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Options;

namespace EnterpriseIntegrationHub.Api.Delivery;

public sealed class DestinationNetworkPolicy
{
    private readonly DestinationHttpOptions _options;
    private readonly Uri[] _origins;

    public DestinationNetworkPolicy(IOptions<DestinationHttpOptions> options, IHostEnvironment environment)
    {
        _options = options.Value;
        if ((_options.AllowLoopback || _options.AllowInsecureHttp) &&
            !environment.IsDevelopment() && !environment.IsEnvironment("Testing"))
            throw new InvalidOperationException("Unsafe destination options are only available in Development or Testing.");

        _origins = _options.AllowedOrigins.Select(origin =>
        {
            if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri) || !IsValidUri(uri) ||
                uri.AbsolutePath != "/" || uri.Query.Length != 0 ||
                (uri.Scheme == Uri.UriSchemeHttp && !_options.AllowInsecureHttp))
                throw new InvalidOperationException("Destination origin configuration is invalid.");
            return uri;
        }).ToArray();
    }

    public bool IsAllowedDestination(string address, out Uri? uri)
    {
        if (!Uri.TryCreate(address, UriKind.Absolute, out var candidate) || !IsValidUri(candidate) ||
            (candidate.Scheme == Uri.UriSchemeHttp && !_options.AllowInsecureHttp) ||
            !_origins.Any(origin => SameOrigin(origin, candidate)))
        {
            uri = null;
            return false;
        }
        uri = candidate;
        return true;
    }

    private static bool IsValidUri(Uri uri) =>
        (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp) &&
        uri.UserInfo.Length == 0 && uri.Fragment.Length == 0 && uri.Query.Length == 0 &&
        !string.IsNullOrEmpty(uri.Host);

    private static bool SameOrigin(Uri left, Uri right) =>
        left.Scheme == right.Scheme && left.Port == right.Port &&
        string.Equals(left.IdnHost, right.IdnHost, StringComparison.OrdinalIgnoreCase);

    public async ValueTask<Stream> ConnectAsync(SocketsHttpConnectionContext context, CancellationToken cancellationToken)
    {
        var host = context.DnsEndPoint.Host;
        if (!_origins.Any(origin => origin.Port == context.DnsEndPoint.Port &&
            string.Equals(origin.IdnHost.Trim('[', ']'), host.Trim('[', ']'), StringComparison.OrdinalIgnoreCase)))
            throw new DestinationPolicyException();

        var isLiteral = IPAddress.TryParse(host.Trim('[', ']'), out var literal);
        var addresses = isLiteral ? [literal!] : await Dns.GetHostAddressesAsync(host, cancellationToken);
        if (addresses.Length == 0 || addresses.Any(address => !IsAllowedAddress(address, isLiteral)))
            throw new DestinationPolicyException();

        SocketException? lastError = null;
        foreach (var address in addresses)
        {
            var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                await socket.ConnectAsync(new IPEndPoint(address, context.DnsEndPoint.Port), cancellationToken);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch (SocketException exception)
            {
                lastError = exception;
                socket.Dispose();
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        }
        throw new HttpRequestException("Destination connection failed.", lastError);
    }

    public bool IsAllowedAddress(IPAddress address, bool literalHost)
    {
        if (address.IsIPv4MappedToIPv6)
            address = address.MapToIPv4();
        if (IPAddress.IsLoopback(address))
            return _options.AllowLoopback && literalHost;
        var bytes = address.GetAddressBytes();
        if (address.AddressFamily == AddressFamily.InterNetwork)
            return bytes[0] != 0 && bytes[0] != 10 && bytes[0] != 127 && bytes[0] < 224 &&
                !(bytes[0] == 100 && bytes[1] >= 64 && bytes[1] <= 127) &&
                !(bytes[0] == 169 && bytes[1] == 254) &&
                !(bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) &&
                !(bytes[0] == 192 && (bytes[1] == 168 || (bytes[1] == 0 && (bytes[2] == 0 || bytes[2] == 2)) ||
                    (bytes[1] == 88 && bytes[2] == 99))) &&
                !(bytes[0] == 198 && (bytes[1] == 18 || bytes[1] == 19 || (bytes[1] == 51 && bytes[2] == 100))) &&
                !(bytes[0] == 203 && bytes[1] == 0 && bytes[2] == 113);
        return address.AddressFamily == AddressFamily.InterNetworkV6 &&
            (bytes[0] & 0xe0) == 0x20 &&
            !(bytes[0] == 0x20 && bytes[1] == 0x01 && (bytes[2] & 0xfe) == 0) &&
            !(bytes[0] == 0x20 && bytes[1] == 0x01 && bytes[2] == 0x0d && bytes[3] == 0xb8) &&
            !(bytes[0] == 0x20 && bytes[1] == 0x02) &&
            !(bytes[0] == 0x3f && bytes[1] == 0xff && (bytes[2] & 0xf0) == 0);
    }

    private sealed class DestinationPolicyException : IOException
    {
        public DestinationPolicyException() : base("Destination address is blocked.") { }
    }

    public static bool IsPolicyFailure(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
            if (current is DestinationPolicyException)
                return true;
        return false;
    }
}
