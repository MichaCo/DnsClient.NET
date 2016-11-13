using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Net.NetworkInformation;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;

/*
 */
namespace DnsClient
{
    public class DnsLookupOptions
    {
        public DnsLookupOptions()
            : this(DnsLookup.GetDnsServers())
        {
        }

        public DnsLookupOptions(params IPEndPoint[] dnsServerEndpoints)
        {
            if (dnsServerEndpoints == null || dnsServerEndpoints.Length == 0)
            {
                throw new ArgumentException("No DNS Server endpoint specified.", nameof(dnsServerEndpoints));
            }

            DnsServers = dnsServerEndpoints;
        }

        /// <summary>
        /// Gets or sets timeout in milliseconds.
        /// </summary>
        public int Timeout { get; set; } = 1000;

        /// <summary>
        /// Gets or sets number of retries before giving up.
        /// </summary>
        public int Retries { get; set; } = 3;

        /// <summary>
        /// Gets or set recursion for doing queries.
        /// </summary>
        public bool Recursion { get; set; } = true;

        /// <summary>
        /// Gets or sets protocol to use.
        /// </summary>
        public TransportType TransportType { get; set; } = TransportType.Udp;

        /// <summary>
        /// Gets list of DNS servers to use.
        /// </summary>
        public IReadOnlyCollection<IPEndPoint> DnsServers { get; }

        /// <summary>
        /// Gets or sets a flag indicating if the <see cref="DnsLookup"/> should use caching or not.
        /// </summary>
        public bool UseCache { get; set; } = true;
    }

    /// <summary>
    /// Resolver is the main class to do DNS query lookups
    /// </summary>
    public class DnsLookup
    {
        /// <summary>
        /// Default DNS port
        /// </summary>
        public const int DefaultPort = 53;

        private static readonly Response TimeoutResponse = new Response("Connection timed out, no servers could be reached.");
        private ushort _uniqueId = (ushort)(new Random()).Next();
        private readonly ConcurrentDictionary<string, Response> _responseCache = new ConcurrentDictionary<string, Response>();
        private readonly ILogger<DnsLookup> _logger = null;
        private readonly ILoggerFactory _loggerFactory = null;
        private readonly DnsLookupOptions _options;

        /// <summary>
        /// Creates a new instance of <see cref="DnsLookup"/>.
        /// </summary>
        /// <param name="loggerFactory">A logger factory.</param>
        /// <param name="options">The configuration options.</param>
        public DnsLookup(ILoggerFactory loggerFactory, DnsLookupOptions options)
            : this(options)
        {
            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<DnsLookup>();
        }

        /// <summary>
        /// Creates a new instance of <see cref="DnsLookup"/>.
        /// </summary>
        /// <param name="options">The configuration options.</param>
        public DnsLookup(DnsLookupOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _options = options;
        }

        public DnsLookup()
            : this(new DnsLookupOptions())
        {
        }

        private bool IsLogging
        {
            get
            {
                return _logger != null;
            }
        }

        /// <summary>
        /// Clear the resolver cache
        /// </summary>
        public void ClearCache()
        {
            lock (_responseCache)
            {
                _responseCache.Clear();
            }
        }

        private Response SearchInCache(Question question)
        {
            if (!_options.UseCache)
            {
                return null;
            }

            string key = GetCacheKey(question);
            Response response = null;
            if (!_responseCache.TryGetValue(key, out response))
            {
                return null;
            }

            int timeLived = (int)((DateTime.Now.Ticks - response.TimeStamp.Ticks) / TimeSpan.TicksPerSecond);
            foreach (ResourceRecord record in response.ResourceRecords)
            {
                // The TTL property calculates its actual time to live
                if (record.SetTimeToLive(timeLived) == 0)
                {
                    return null; // out of date
                }
            }

            return response;
        }

        private void AddToCache(Response response)
        {
            if (!_options.UseCache)
            {
                return;
            }

            // No question, no caching
            if (response.Questions.Count == 0)
            {
                return;
            }

            // Only cached non-error responses
            if (response.Header.ResponseCode != RCode.NoError)
            {
                return;
            }

            var question = response.Questions.First();
            var key = GetCacheKey(question);

            if (_responseCache.ContainsKey(key))
            {
                Response existing;
                _responseCache.TryRemove(key, out existing);
            }

            _responseCache.TryAdd(key, response);
        }

        private string GetCacheKey(Question question)
        {
            return question.QClass + "-" + question.QType + "-" + question.QName;
        }

#if XPLAT
        private async Task<Response> UdpRequest(Request request)
        {
            var sw = Stopwatch.StartNew();

            // RFC1035 max. size of a UDP datagram is 512 bytes
            var responseMessage = new ArraySegment<byte>(new byte[512]);

            for (int attempts = 0; attempts < _options.Retries; attempts++)
            {
                for (int indexServer = 0; indexServer < _options.DnsServers.Count; indexServer++)
                {
                    using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
                    {
                        var dnsServer = _options.DnsServers.ElementAt(indexServer);

                        try
                        {
                            var sendData = new ArraySegment<byte>(request.Data, 0, request.Data.Length);
                            var sendTimer = Task.Delay(_options.Timeout);
                            var sendTask = socket.SendToAsync(sendData, SocketFlags.None, dnsServer);
                            await Task.WhenAny(sendTask, sendTimer);

                            if (!sendTask.IsCompleted)
                            {
                                if (IsLogging)
                                {
                                    _logger.LogWarning("Exceeded timeout of {0}ms connecting to nameserver '{1}'.", _options.Timeout, dnsServer);
                                }

                                continue;
                            }

                            if (IsLogging)
                            {
                                this._logger.LogDebug($"Sending ({request.Data.Length}) bytes in {sw.ElapsedMilliseconds} ms.");
                                sw.Restart();
                            }

                            var receiveTimer = Task.Delay(_options.Timeout);
                            var receiveTask = socket.ReceiveAsync(responseMessage, SocketFlags.None);
                            await Task.WhenAny(receiveTask, receiveTimer);

                            if (!receiveTask.IsCompleted)
                            {
                                if (IsLogging)
                                {
                                    _logger.LogWarning("Exceeded timeout of {0}ms receiving a message from nameserver '{1}'.", _options.Timeout, dnsServer);
                                }

                                continue;
                            }

                            int intReceived = receiveTask.Result;

                            if (IsLogging)
                            {
                                this._logger.LogDebug($"Received ({intReceived}) bytes in {sw.ElapsedMilliseconds} ms.");
                                sw.Restart();
                            }

                            Response response = new Response(_loggerFactory, dnsServer, responseMessage.ToArray());
                            AddToCache(response);
                            return response;
                        }
                        catch (SocketException ex)
                        {
                            //TODO remove servers which throw not supported protocol exceptions
                            if (IsLogging)
                            {
                                _logger.LogWarning(0, ex, "Socket error occurred with nameserver {0}.", dnsServer);
                            }
                        }
                        catch (AggregateException aggEx)
                        {
                            aggEx.Handle(ex =>
                            {
                                if (ex.GetType() == typeof(SocketException))
                                {
                                    if (IsLogging)
                                    {
                                        _logger.LogWarning(0, ex, "Connection to nameserver {0} failed.", dnsServer);
                                    }

                                    return true;
                                }

                                return false;
                            });
                        }
                        finally
                        {
                            _uniqueId++;
                        }
                    }
                }
            }

            if (IsLogging)
            {
                _logger.LogError("Could not send or receive message from any configured nameserver.");
            }

            return TimeoutResponse;
        }

#else
        private Response UdpRequest(Request request)
        {
            var sw = Stopwatch.StartNew();

            // RFC1035 max. size of a UDP datagram is 512 bytes
            byte[] responseMessage = new byte[512];

            for (int attempt = 0; attempt < _options.Retries; attempt++)
            {
                for (int indexServer = 0; indexServer < _options.DnsServers.Count; indexServer++)
                {
                    using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
                    {
                        socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, _options.Timeout);
                        var dnsServer = _options.DnsServers.ElementAt(indexServer);

                        try
                        {
                            socket.SendTo(request.Data, dnsServer);
                            if (IsLogging)
                            {
                                this._logger.LogDebug($"Sending ({request.Data.Length}) bytes in {sw.ElapsedMilliseconds} ms.");
                                sw.Restart();
                            }

                            int intReceived = socket.Receive(responseMessage);
                            if (IsLogging)
                            {
                                this._logger.LogDebug($"Received ({intReceived}) bytes in {sw.ElapsedMilliseconds} ms.");
                                sw.Restart();
                            }

                            byte[] data = new byte[intReceived];
                            Array.Copy(responseMessage, data, intReceived);
                            Response response = new Response(_loggerFactory, dnsServer, data);
                            AddToCache(response);
                            return response;
                        }
                        catch (SocketException)
                        {
                            //TODO remove servers which throw not supported protocol exceptions
                            if (IsLogging)
                            {
                                _logger.LogWarning("Connection to nameserver {0} failed.", dnsServer);
                            }
                        }
                        finally
                        {
                            _uniqueId++;
                        }
                    }
                }
            }

            if (IsLogging)
            {
                _logger.LogError("Could not send or receive message from any configured nameserver.");
            }

            return TimeoutResponse;
        }
#endif

        private async Task<Response> TcpRequest(Request request)
        {
            var sw = Stopwatch.StartNew();

            byte[] responseMessage = new byte[512];

            for (int attempt = 0; attempt < _options.Retries; attempt++)
            {
                for (int indexServer = 0; indexServer < _options.DnsServers.Count; indexServer++)
                {
                    TcpClient tcpClient = new TcpClient();
                    tcpClient.ReceiveTimeout = _options.Timeout;
                    var dnsServer = _options.DnsServers.ElementAt(indexServer);

                    try
                    {
                        var connectTimer = Task.Delay(_options.Timeout);
                        var connectTask = tcpClient.ConnectAsync(dnsServer.Address, dnsServer.Port);
                        await Task.WhenAny(connectTask, connectTimer);
                        if (!connectTask.IsCompleted || !tcpClient.Connected)
                        {
                            if (IsLogging)
                            {
                                _logger.LogWarning("Connection to nameserver {0} failed.", dnsServer);
                            }

                            continue;
                        }

                        var bs = new BufferedStream(tcpClient.GetStream());

                        var data = request.Data;
                        bs.WriteByte((byte)((data.Length >> 8) & 0xff));
                        bs.WriteByte((byte)(data.Length & 0xff));
                        await bs.WriteAsync(data, 0, data.Length);
                        await bs.FlushAsync();

                        Response transferResponse = new Response();
                        int intSoa = 0;
                        int intMessageSize = 0;

                        if (IsLogging)
                        {
                            this._logger.LogDebug($"Sending ({request.Data.Length + 2}) bytes in {sw.ElapsedMilliseconds} ms.");
                            sw.Restart();
                        }

                        while (true)
                        {
                            int intLength = bs.ReadByte() << 8 | bs.ReadByte();
                            if (intLength <= 0)
                            {
                                if (IsLogging)
                                {
                                    _logger.LogWarning("Connection to nameserver {0} failed.", dnsServer);
                                }

                                break;
                            }

                            intMessageSize += intLength;

                            data = new byte[intLength];
                            bs.Read(data, 0, intLength);
                            Response response = new Response(_loggerFactory, dnsServer, data);

                            if (IsLogging)
                            {
                                this._logger.LogDebug($"Received ({intLength + 2}) bytes in {sw.ElapsedMilliseconds} ms.");
                                sw.Restart();
                            }

                            if (response.Header.ResponseCode != RCode.NoError)
                            {
                                return response;
                            }

                            if (response.Questions.First().QType != QType.AXFR)
                            {
                                AddToCache(response);
                                return response;
                            }

                            // Zone transfer!!
                            if (transferResponse.Questions.Count == 0)
                            {
                                transferResponse = new Response(response);
                            }
                            else
                            {
                                transferResponse = Response.Concat(transferResponse, response);
                            }

                            if (response.Answers.First().Type == TypeValue.SOA)
                            {
                                intSoa++;
                            }

                            if (intSoa == 2)
                            {
                                transferResponse.Header.QuestionCount = (ushort)transferResponse.Questions.Count;
                                transferResponse.Header.AnswerCount = (ushort)transferResponse.Answers.Count;
                                transferResponse.Header.NameServerCount = (ushort)transferResponse.Authorities.Count;
                                transferResponse.Header.AdditionalCount = (ushort)transferResponse.Additionals.Count;
                                transferResponse.MessageSize = intMessageSize;
                                return transferResponse;
                            }
                        }
                    }
                    catch (SocketException ex)
                    {
                        if (IsLogging)
                        {
                            _logger.LogWarning(0, ex, "Connection to nameserver {0} failed.", dnsServer);
                        }
                    }
                    catch (NotSupportedException ex)
                    {
                        //TODO remove servers which throw not supported protocol exceptions
                        if (IsLogging)
                        {
                            _logger.LogWarning(0, ex, "Connection to nameserver {0} failed.", dnsServer);
                        }
                    }
                    catch (AggregateException aggEx)
                    {
                        aggEx.Handle(ex =>
                        {
                            if (ex.GetType() == typeof(SocketException) || ex.GetType() == typeof(NotSupportedException))
                            {
                                //TODO remove servers which throw not supported protocol exceptions
                                if (IsLogging)
                                {
                                    _logger.LogWarning(0, ex, "Connection to nameserver {0} failed.", dnsServer);
                                }

                                return true;
                            }

                            return false;
                        });
                    }
                    finally
                    {
                        _uniqueId++;

                        // close the socket
#if XPLAT
                        tcpClient.Dispose();
#else
                        tcpClient.Close();
#endif
                    }
                }
            }

            if (IsLogging)
            {
                _logger.LogError("Could not send or receive message from any configured nameserver.");
            }

            return TimeoutResponse;
        }

        /// <summary>
        /// Do Query on specified DNS servers
        /// </summary>
        /// <param name="name">Name to query</param>
        /// <param name="qtype">Question type</param>
        /// <param name="qclass">Class type</param>
        /// <returns>Response of the query</returns>
        public async Task<Response> QueryAsync(string name, QType qtype, QClass qclass)
        {
            Question question = new Question(name, qtype, qclass);
            Response response = SearchInCache(question);
            if (response != null)
            {
                return response;
            }

            var header = new Header(_uniqueId, OPCode.Query, 1, _options.Recursion);
            Request request = new Request(header, question);
            return await GetResponseAsync(request);
        }

        /// <summary>
        /// Do an QClass=IN Query on specified DNS servers
        /// </summary>
        /// <param name="name">Name to query</param>
        /// <param name="qtype">Question type</param>
        /// <returns>Response of the query</returns>
        public async Task<Response> QueryAsync(string name, QType qtype)
        {
            var question = new Question(name, qtype, QClass.IN);
            var response = SearchInCache(question);
            if (response != null)
            {
                return response;
            }

            var header = new Header(_uniqueId, OPCode.Query, 1, _options.Recursion);
            Request request = new Request(header, question);
            return await GetResponseAsync(request);
        }

        private async Task<Response> GetResponseAsync(Request request)
        {
            if (_options.TransportType == TransportType.Udp)
            {
                if (IsLogging)
                {
                    _logger.LogDebug("Sending Udp request.");
                }

#if XPLAT
                return await UdpRequest(request);
#else
                return UdpRequest(request);
#endif
            }

            if (_options.TransportType == TransportType.Tcp)
            {
                if (IsLogging)
                {
                    _logger.LogDebug("Sending Tcp request.");
                }

                return await TcpRequest(request);
            }

            return new Response("Unknown TransportType"); ;
        }

        /// <summary>
        /// Gets a list of default DNS servers used on the machine.
        /// </summary>
        /// <returns></returns>
        public static IPEndPoint[] GetDnsServers()
        {
            List<IPEndPoint> list = new List<IPEndPoint>();

            // TODO: check filter loopback adapters and such? Getting unsupported exceptions when running a query against those ip6 DNS addresses.
            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface n in adapters)
            {
                if (n.OperationalStatus == OperationalStatus.Up)
                {
                    IPInterfaceProperties ipProps = n.GetIPProperties();
                    foreach (IPAddress ipAddr in ipProps.DnsAddresses)
                    {
                        IPEndPoint entry = new IPEndPoint(ipAddr, DefaultPort);
                        if (!list.Contains(entry))
                        {
                            list.Add(entry);
                        }
                    }

                }
            }

            return list.ToArray();
        }

        public async Task<IPAddress[]> GetHostAddressesAsync(string hostNameOrAddress)
        {
            IPHostEntry entry = await GetHostEntryAsync(hostNameOrAddress);
            return entry.AddressList;
        }

        /// <summary>
        ///		Creates an System.Net.IPHostEntry instance from the specified System.Net.IPAddress.
        /// </summary>
        /// <param name="ip">An System.Net.IPAddress.</param>
        /// <returns>An System.Net.IPHostEntry.</returns>
        public async Task<IPHostEntry> GetHostEntryAsync(IPAddress ip)
        {
            var arpa = GetArpaFromIp(ip);
            if (IsLogging)
            {
                _logger.LogDebug("Query GetHostEntryAsync for ip '{0}' => '{1}'.", ip.ToString(), arpa);
            }

            Response response = await QueryAsync(arpa, QType.PTR, QClass.IN);
            if (response.RecordsPTR.Count > 0)
            {
                return await GetHostEntryAsync(response.RecordsPTR.First().PTRDName);
            }
            else
            {
                if (IsLogging)
                {
                    _logger.LogDebug("Query GetHostEntryAsync for ip '{0}' did not return any result.", ip.ToString());
                }

                // on linux, reverse /PTR query might not return any value
                // the dotnet Dns util then returns a host entry with the IP address as host name. We'll do the same.
                return new IPHostEntry()
                {
                    AddressList = new IPAddress[] { ip },
                    HostName = ip.ToString()
                };
            }
        }

        /// <summary>
        ///		Resolves a host name or IP address to an System.Net.IPHostEntry instance.
        /// </summary>
        /// <param name="hostNameOrAddress">A DNS-style host name or IP address.</param>
        /// <returns></returns>
        public async Task<IPHostEntry> GetHostEntryAsync(string hostNameOrAddress)
        {
            IPAddress iPAddress;
            if (IPAddress.TryParse(hostNameOrAddress, out iPAddress))
            {
                return await GetHostEntryAsync(iPAddress);
            }
            else
            {
                return await GetHostEntryByNameAsync(hostNameOrAddress);
            }
        }

        private async Task<IPHostEntry> GetHostEntryByNameAsync(string hostName)
        {
            if (string.IsNullOrWhiteSpace(hostName))
            {
                throw new ArgumentNullException(nameof(hostName));
            }
            if (IsLogging)
            {
                _logger.LogDebug("Query GetHostEntryByNameAsync for host name '{0}'.", hostName);
            }

            var entry = new IPHostEntry();
            entry.HostName = hostName;

            var response = await QueryAsync(hostName, QType.A, QClass.IN);

            // fill AddressList and aliases
            var addressList = new List<IPAddress>();
            var aliases = new List<string>();

            if (IsLogging)
            {
                _logger.LogDebug("Query GetHostEntryByNameAsync for host name '{0}' found {1} answers.", hostName, response.Answers.Count);
            }

            foreach (ResourceRecord answer in response.Answers)
            {
                if (answer.Type == TypeValue.A)
                {
                    addressList.Add(IPAddress.Parse((answer.Record.ToString())));
                    entry.HostName = answer.Name;
                }
                else
                {
                    if (answer.Type == TypeValue.CNAME)
                    {
                        aliases.Add(answer.Name);
                    }
                }
            }

            entry.AddressList = addressList.ToArray();
            entry.Aliases = aliases.ToArray();

            if (entry.HostName.EndsWith("."))
            {
                entry.HostName = entry.HostName.Substring(0, entry.HostName.Length - 1);
            }

            return entry;
        }

        /// <summary>
        /// Translates the IPV4 or IPV6 address into an arpa address
        /// </summary>
        /// <param name="ip">IP address to get the arpa address form</param>
        /// <returns>The 'mirrored' IPV4 or IPV6 arpa address</returns>
        public static string GetArpaFromIp(IPAddress ip)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("in-addr.arpa.");
                foreach (byte b in ip.GetAddressBytes())
                {
                    sb.Insert(0, string.Format("{0}.", b));
                }

                return sb.ToString();
            }
            if (ip.AddressFamily == AddressFamily.InterNetworkV6)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("ip6.arpa.");
                foreach (byte b in ip.GetAddressBytes())
                {
                    sb.Insert(0, string.Format("{0:x}.", (b >> 4) & 0xf));
                    sb.Insert(0, string.Format("{0:x}.", (b >> 0) & 0xf));
                }

                return sb.ToString();
            }

            return "?";
        }

        public static string GetArpaFromEnum(string strEnum)
        {
            StringBuilder sb = new StringBuilder();
            string Number = System.Text.RegularExpressions.Regex.Replace(strEnum, "[^0-9]", "");
            sb.Append("e164.arpa.");
            foreach (char c in Number)
            {
                sb.Insert(0, string.Format("{0}.", c));
            }

            return sb.ToString();
        }
    }
}