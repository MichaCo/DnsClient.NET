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
 * Network Working Group                                     P. Mockapetris
 * Request for Comments: 1035                                           ISI
 *                                                            November 1987
 *
 *           DOMAIN NAMES - IMPLEMENTATION AND SPECIFICATION
 *
 */

namespace DnsClient
{
    /// <summary>
    /// Resolver is the main class to do DNS query lookups
    /// </summary>
    public class Resolver
    {
        /// <summary>
        /// Default DNS port
        /// </summary>
        public const int DefaultPort = 53;

        private static readonly Response TimeoutResponse = new Response("Connection timed out, no servers could be reached.");
        private ushort _uniqueId;
        private int _retries;
        private readonly List<IPEndPoint> _dnsServers = new List<IPEndPoint>();
        private readonly ConcurrentDictionary<string, Response> _responseCache = new ConcurrentDictionary<string, Response>();
        private readonly ILogger<Resolver> _logger = null;

        private bool IsLogging
        {
            get
            {
                return _logger != null;
            }
        }

        public Resolver(ILoggerFactory loggerFactory, params IPEndPoint[] dnsServers)
            : this(dnsServers)
        {
            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _logger = loggerFactory.CreateLogger<Resolver>();
        }

        /// <summary>
        /// Constructor of Resolver using DNS servers specified.
        /// </summary>
        /// <param name="dnsServers">Set of DNS servers</param>
        public Resolver(params IPEndPoint[] dnsServers)
        {
            if (dnsServers == null || dnsServers.Length == 0)
            {
                throw new ArgumentException("At least one dns server must be specified.", nameof(dnsServers));
            }

            _dnsServers.AddRange(dnsServers);
            _uniqueId = (ushort)(new Random()).Next();
            Retries = 3;
            UseCache = true;
        }

        /// <summary>
        /// Constructor of Resolver using DNS server and port specified.
        /// </summary>
        /// <param name="ServerIpAddress">DNS server to use</param>
        /// <param name="ServerPortNumber">DNS port to use</param>
        public Resolver(IPAddress ServerIpAddress, int ServerPortNumber)
            : this(new IPEndPoint(ServerIpAddress, ServerPortNumber))
        {
        }

        /// <summary>
        /// Constructor of Resolver using DNS address and port specified.
        /// </summary>
        /// <param name="ServerIpAddress">DNS server address to use</param>
        /// <param name="ServerPortNumber">DNS port to use</param>
        public Resolver(string ServerIpAddress, int ServerPortNumber)
            : this(IPAddress.Parse(ServerIpAddress), ServerPortNumber)
        {
        }

        /// <summary>
        /// Constructor of Resolver using DNS address.
        /// </summary>
        /// <param name="ServerIpAddress">DNS server address to use</param>
        public Resolver(string ServerIpAddress)
            : this(IPAddress.Parse(ServerIpAddress), DefaultPort)
        {
        }

        /// <summary>
        /// Resolver constructor, using DNS servers specified by Windows
        /// </summary>
        public Resolver()
            : this(GetDnsServers())
        {
        }

        /// <summary>
        /// Gets or sets timeout in milliseconds
        /// </summary>
        public int Timeout { get; set; } = 1000;

        /// <summary>
        /// Gets or sets number of retries before giving up
        /// </summary>
        public int Retries
        {
            get
            {
                return _retries;
            }
            set
            {
                if (value >= 1)
                {
                    _retries = value;
                }
            }
        }

        /// <summary>
        /// Gets or set recursion for doing queries
        /// </summary>
        public bool Recursion { get; set; } = true;

        /// <summary>
        /// Gets or sets protocol to use
        /// </summary>
        public TransportType TransportType { get; set; } = TransportType.Tcp;

        /// <summary>
        /// Gets or sets list of DNS servers to use
        /// </summary>
        public IPEndPoint[] DnsServers
        {
            get
            {
                return _dnsServers.ToArray();
            }
        }

        /// <summary>
        /// Gets first DNS server address or sets single DNS server to use
        /// </summary>
        public string DnsServer
        {
            get
            {
                return _dnsServers.FirstOrDefault()?.Address?.ToString();
            }
        }

        public bool UseCache { get; set; } = true;

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
            if (!UseCache)
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
            if (!UseCache)
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

            for (int intAttempts = 0; intAttempts < _retries; intAttempts++)
            {
                for (int intDnsServer = 0; intDnsServer < _dnsServers.Count; intDnsServer++)
                {
                    Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

                    try
                    {
                        var sendData = new ArraySegment<byte>(request.Data, 0, request.Data.Length);
                        var timerTask = Task.Delay(Timeout);
                        var sendTask = socket.SendToAsync(sendData, SocketFlags.None, _dnsServers[intDnsServer]);
                        await Task.WhenAny(sendTask, timerTask);
                        if (!sendTask.IsCompleted)
                        {
                            throw new SocketException();
                        }

                        if (IsLogging)
                        {
                            this._logger.LogDebug($"Sending ({request.Data.Length}) bytes in {sw.ElapsedMilliseconds} ms.");
                            sw.Restart();
                        }

                        var receiveTimer = Task.Delay(Timeout);
                        var receiveTask = socket.ReceiveAsync(responseMessage, SocketFlags.None);
                        await Task.WhenAny(receiveTask, receiveTimer);
                        if (!receiveTask.IsCompleted)
                        {
                            throw new SocketException();
                        }

                        int intReceived = receiveTask.Result;

                        if (IsLogging)
                        {
                            this._logger.LogDebug($"Received ({intReceived}) bytes in {sw.ElapsedMilliseconds} ms.");
                            sw.Restart();
                        }

                        Response response = new Response(_dnsServers[intDnsServer], responseMessage.ToArray());
                        AddToCache(response);
                        return response;
                    }
                    catch (SocketException)
                    {
                        if (IsLogging)
                        {
                            _logger.LogWarning("Connection to nameserver {0} failed", _dnsServers[intDnsServer]);
                        }

                        continue;
                    }
                    finally
                    {
                        _uniqueId++;

                        // close the socket
                        socket.Dispose();
                    }
                }
            }

            return TimeoutResponse;
        }

#else
        private Response UdpRequest(Request request)
        {
            var sw = Stopwatch.StartNew();

            // RFC1035 max. size of a UDP datagram is 512 bytes
            byte[] responseMessage = new byte[512];

            for (int intAttempts = 0; intAttempts < _retries; intAttempts++)
            {
                for (int intDnsServer = 0; intDnsServer < _dnsServers.Count; intDnsServer++)
                {
                    Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, Timeout);

                    try
                    {
                        socket.SendTo(request.Data, _dnsServers[intDnsServer]);
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
                        Response response = new Response(_dnsServers[intDnsServer], data);
                        AddToCache(response);
                        return response;
                    }
                    catch (SocketException)
                    {
                        if (IsLogging)
                        {
                            _logger.LogWarning("Connection to nameserver {0} failed.", _dnsServers[intDnsServer]);
                        }

                        continue;
                    }
                    finally
                    {
                        _uniqueId++;

                        // close the socket
                        socket.Dispose();
                    }
                }
            }

            return TimeoutResponse;
        }
#endif

        private async Task<Response> TcpRequest(Request request)
        {
            var sw = Stopwatch.StartNew();

            byte[] responseMessage = new byte[512];

            for (int intAttempts = 0; intAttempts < _retries; intAttempts++)
            {
                for (int intDnsServer = 0; intDnsServer < _dnsServers.Count; intDnsServer++)
                {
                    TcpClient tcpClient = new TcpClient();
                    tcpClient.ReceiveTimeout = Timeout;

                    try
                    {
                        var connectTimer = Task.Delay(Timeout);
                        var connectTask = tcpClient.ConnectAsync(_dnsServers[intDnsServer].Address, _dnsServers[intDnsServer].Port);
                        await Task.WhenAny(connectTask, connectTimer);
                        if (!connectTask.IsCompleted || !tcpClient.Connected)
                        {
#if XPLAT
                            tcpClient.Dispose();
#else
                            tcpClient.Close();
#endif
                            if (IsLogging)
                            {
                                _logger.LogWarning("Connection to nameserver {0} failed", _dnsServers[intDnsServer]);
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
#if XPLAT
                                tcpClient.Dispose();
#else
                                tcpClient.Close();
#endif
                                if (IsLogging)
                                {
                                    _logger.LogWarning("Connection to nameserver {0} failed", _dnsServers[intDnsServer]);
                                }

                                throw new SocketException(); // next try
                            }

                            intMessageSize += intLength;

                            data = new byte[intLength];
                            bs.Read(data, 0, intLength);
                            Response response = new Response(_dnsServers[intDnsServer], data);

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
                            _logger.LogWarning(0, ex, "Connection to nameserver {0} failed", _dnsServers[intDnsServer]);
                        }
                        continue; // next try
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

            var header = new Header(_uniqueId, OPCode.Query, 1, Recursion);
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

            var header = new Header(_uniqueId, OPCode.Query, 1, Recursion);
            Request request = new Request(header, question);
            return await GetResponseAsync(request);
        }

        private async Task<Response> GetResponseAsync(Request request)
        {
            if (TransportType == TransportType.Udp)
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

            if (TransportType == TransportType.Tcp)
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

            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface n in adapters)
            {
                if (n.OperationalStatus == OperationalStatus.Up)
                {
                    IPInterfaceProperties ipProps = n.GetIPProperties();
                    // thanks to Jon Webster on May 20, 2008
                    foreach (IPAddress ipAddr in ipProps.DnsAddresses)
                    {
                        IPEndPoint entry = new IPEndPoint(ipAddr, DefaultPort);
                        if (!list.Contains(entry))
                            list.Add(entry);
                    }

                }
            }
            return list.ToArray();
        }

        public async Task<IPAddress[]> GetHostAddresses(string hostNameOrAddress)
        {
            IPHostEntry entry = await GetHostEntryAsync(hostNameOrAddress);
            return entry.AddressList;
        }

        /// <summary>
        ///		Creates an System.Net.IPHostEntry instance from the specified System.Net.IPAddress.
        /// </summary>
        /// <param name="ip">An System.Net.IPAddress.</param>
        /// <returns>An System.Net.IPHostEntry.</returns>
        public async Task<IPHostEntry> GetHostByAddress(IPAddress ip)
        {
            return await GetHostEntryAsync(ip);
        }

        /// <summary>
        ///		Creates an System.Net.IPHostEntry instance from an IP address.
        /// </summary>
        /// <param name="address">An IP address.</param>
        /// <returns>An System.Net.IPHostEntry instance.</returns>
        public async Task<IPHostEntry> GetHostByAddress(string address)
        {
            return await GetHostEntryAsync(address);
        }

        /// <summary>
        ///		Gets the DNS information for the specified DNS host name.
        /// </summary>
        /// <param name="hostName">The DNS name of the host</param>
        /// <returns>An System.Net.IPHostEntry object that contains host information for the address specified in hostName.</returns>
        public async Task<IPHostEntry> GetHostByNameAsync(string hostName)
        {
            return await GetHostEntryAsync(hostName);
        }

        /// <summary>
        ///		Resolves a host name or IP address to an System.Net.IPHostEntry instance.
        /// </summary>
        /// <param name="hostName">A DNS-style host name or IP address.</param>
        /// <returns></returns>
        //[Obsolete("no problem",false)]
        public async Task<IPHostEntry> ResolveAsync(string hostName)
        {
            return await GetHostEntryAsync(hostName);
        }

        /// <summary>
        ///		Resolves an IP address to an System.Net.IPHostEntry instance.
        /// </summary>
        /// <param name="ip">An IP address.</param>
        /// <returns>
        ///		An System.Net.IPHostEntry instance that contains address information about
        ///		the host specified in address.
        ///</returns>
        public async Task<IPHostEntry> GetHostEntryAsync(IPAddress ip)
        {
            Response response = await QueryAsync(GetArpaFromIp(ip), QType.PTR, QClass.IN);
            if (response.RecordsPTR.Count > 0)
            {
                return await GetHostEntryAsync(response.RecordsPTR.First().PTRDName);
            }
            else
            {
                return new IPHostEntry();
            }
        }

        /// <summary>
        ///		Resolves a host name or IP address to an System.Net.IPHostEntry instance.
        /// </summary>
        /// <param name="hostNameOrAddress">The host name or IP address to resolve.</param>
        /// <returns>
        ///		An System.Net.IPHostEntry instance that contains address information about
        ///		the host specified in hostNameOrAddress. 
        ///</returns>
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

            var entry = new IPHostEntry();
            entry.HostName = hostName;

            var response = await QueryAsync(hostName, QType.A, QClass.IN);

            // fill AddressList and aliases
            var addressList = new List<IPAddress>();
            var aliases = new List<string>();
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