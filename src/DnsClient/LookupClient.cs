using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DnsClient
{
    public class LookupClient
    {
        private static readonly TimeSpan s_defaultTimeout = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan s_infiniteTimeout = System.Threading.Timeout.InfiniteTimeSpan;
        private static readonly TimeSpan s_maxTimeout = TimeSpan.FromMilliseconds(int.MaxValue);
        private static ushort _uniqueId = 0;
        private readonly DnsMessageHandler _messageHandler;
        private TimeSpan _timeout = s_defaultTimeout;

        /// <summary>
        /// Gets the list of configured name servers.
        /// </summary>
        public IReadOnlyCollection<DnsEndPoint> NameServers { get; }

        /// <summary>
        /// Gets or set a flag indicating if recursion should be enabled for DNS queries.
        /// </summary>
        public bool Recursion { get; set; } = true;

        /// <summary>
        /// Gets or sets number of tries to connect to one name server before trying the next one or throwing an exception.
        /// </summary>
        public int Retries { get; set; } = 5;

        /// <summary>
        /// Gets or sets a flag indicating if the <see cref="LookupClient"/> should throw an <see cref="DnsResponseException"/>
        /// if the returned result contains an error flag other than <see cref="DnsResponseCode.NoError"/>.
        /// (The default behavior is <c>False</c>).
        /// </summary>
        public bool ThrowDnsErrors { get; set; } = false;

        /// <summary>
        /// Gets or sets timeout in milliseconds.
        /// Timeout must be greater than zero and less than <see cref="int.MaxValue"/>.
        /// </summary>
        public TimeSpan Timeout
        {
            get { return _timeout; }
            set
            {
                if ((value <= TimeSpan.Zero || value > s_maxTimeout) && value != s_infiniteTimeout)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                _timeout = value;
            }
        }

        /// <summary>
        /// Gets or sets a flag indicating if the <see cref="LookupClient"/> should use caching or not.
        /// The TTL of cached results is defined by the name server's response.
        /// </summary>
        public bool UseCache { get; set; } = true;

        public LookupClient()
            : this(NameServer.ResolveNameServers().ToArray())
        {
        }

        public LookupClient(params DnsEndPoint[] nameServers)
            : this(new DnsUdpMessageHandler(), nameServers)
        {
        }

        public LookupClient(params IPEndPoint[] nameServers)
            : this(new DnsUdpMessageHandler(), nameServers.Select(p => new DnsEndPoint(p.Address.ToString(), p.Port)).ToArray())
        {
        }

        public LookupClient(params IPAddress[] nameServers)
            : this(
                  new DnsUdpMessageHandler(),
                  nameServers.Select(p => new DnsEndPoint(p.ToString(), NameServer.DefaultPort)).ToArray())
        {
        }

        public LookupClient(DnsMessageHandler messageInvoker, ICollection<DnsEndPoint> nameServers)
        {
            if (messageInvoker == null)
            {
                throw new ArgumentNullException(nameof(messageInvoker));
            }
            if (nameServers == null || nameServers.Count == 0)
            {
                throw new ArgumentException("At least one name server must be configured.", nameof(nameServers));
            }

            NameServers = nameServers.ToArray();
            _messageHandler = messageInvoker;
        }

        /// <summary>
        /// Translates the IPV4 or IPV6 address into an arpa address.
        /// </summary>
        /// <param name="ip">IP address to get the arpa address form</param>
        /// <returns>The mirrored IPV4 or IPV6 arpa address</returns>
        public static string GetArpaName(IPAddress ip)
        {
            var bytes = ip.GetAddressBytes();
            Array.Reverse(bytes);

            // check IP6
            if (ip.AddressFamily == AddressFamily.InterNetworkV6)
            {
                // reveresed bytes need to be split into 4 bit parts and separated by '.'
                var newBytes = bytes
                    .SelectMany(b => new[] { (b >> 0) & 0xf, (b >> 4) & 0xf })
                    .Aggregate(new StringBuilder(), (s, b) => s.Append(b.ToString("x")).Append(".")) + "ip6.arpa.";

                return newBytes;
            }
            else if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                // else IP4
                return string.Join(".", bytes) + ".in-addr.arpa.";
            }

            throw new InvalidOperationException("Not a valid IP4 or IP6 address.");
        }

        public Task<DnsResponseMessage> QueryAsync(string query, QueryType queryType)
            => QueryAsync(query, queryType, CancellationToken.None);

        public Task<DnsResponseMessage> QueryAsync(string query, QueryType queryType, CancellationToken cancellationToken)
            => QueryAsync(query, queryType, QueryClass.IN, cancellationToken);

        public Task<DnsResponseMessage> QueryAsync(string query, QueryType queryType, QueryClass queryClass)
            => QueryAsync(query, queryType, queryClass, CancellationToken.None);

        public Task<DnsResponseMessage> QueryAsync(string query, QueryType queryType, QueryClass queryClass, CancellationToken cancellationToken)
            => QueryAsync(cancellationToken, new DnsQuestion(query, queryType, queryClass));

        public Task<DnsResponseMessage> QueryAsync(params DnsQuestion[] questions)
            => QueryAsync(CancellationToken.None, questions);

        public Task<DnsResponseMessage> QueryAsync(CancellationToken cancellationToken, params DnsQuestion[] questions)
        {
            if (questions == null || questions.Length == 0)
            {
                throw new ArgumentNullException(nameof(questions));
            }

            var head = new DnsRequestHeader(GetNextUniqueId(), questions.Length, Recursion, DnsOpCode.Query);
            var request = new DnsRequestMessage(head, questions);

            return QueryAsync(request, CancellationToken.None);
        }

        public Task<DnsResponseMessage> QueryReverseAsync(IPAddress ipAddress)
            => QueryReverseAsync(ipAddress, CancellationToken.None);

        public Task<DnsResponseMessage> QueryReverseAsync(IPAddress ipAddress, CancellationToken cancellationToken)
        {
            if (ipAddress == null)
            {
                throw new ArgumentNullException(nameof(ipAddress));
            }

            var arpa = GetArpaName(ipAddress);
            var head = new DnsRequestHeader(GetNextUniqueId(), 1, Recursion, DnsOpCode.Query);
            var request = new DnsRequestMessage(head, new DnsQuestion(arpa, QueryType.PTR /*PTR*/, QueryClass.IN));

            return QueryAsync(request, cancellationToken);
        }

        private static ushort GetNextUniqueId()
        {
            if (_uniqueId == ushort.MaxValue || _uniqueId == 0)
            {
                _uniqueId = (ushort)(new Random()).Next(ushort.MaxValue / 2);
            }

            return _uniqueId++;
        }

        private async Task<DnsResponseMessage> QueryAsync(DnsRequestMessage request, CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            foreach (var server in NameServers)
            {
                var tries = 0;
                do
                {
                    tries++;
                    try
                    {
                        DnsResponseMessage response;
                        var resultTask = _messageHandler.QueryAsync(server, request, cancellationToken);
                        if (Timeout != s_infiniteTimeout)
                        {
                            response = await resultTask.TimeoutAfter(Timeout);
                        }

                        response = await resultTask;

                        if (ThrowDnsErrors && response.Header.ResponseCode != DnsResponseCode.NoError)
                        {
                            throw new DnsResponseException(response.Header.ResponseCode);
                        }

                        return response;
                    }
                    catch (TimeoutException)
                    {
                        // do nothing... transient if timeoutAfter timed out
                    }
                    catch (Exception ex) when (_messageHandler.IsTransientException(ex))
                    {
                    }
                    catch (Exception ex)
                    {
                        var agg = ex as AggregateException;
                        if (agg != null)
                        {
                            agg.Handle(e =>
                            {
                                if (e is TimeoutException) return true;
                                if (_messageHandler.IsTransientException(e)) return true;
                                return false;
                            });

                            throw new DnsResponseException("Unhandled exception", agg.InnerException);
                        }

                        throw new DnsResponseException("Unhandled exception", ex);
                    }
                    finally
                    {
                        // do cleanup stuff or logging?
                    }
                } while (tries <= Retries && !cancellationToken.IsCancellationRequested);
            }

            throw new DnsResponseException($"No connection could be established to any of the following name servers: {string.Join(", ", NameServers)}.");
        }
    }

    ////[EventSource(Name = "MichaCo-DnsClient")]
    ////public class DnsEventSource : EventSource
    ////{
    ////    public class Keywords
    ////    {
    ////        public const EventKeywords Default = (EventKeywords)0x0001;
    ////        public const EventKeywords Debug = (EventKeywords)0x0002;
    ////        public const EventKeywords EnterExit = (EventKeywords)0x0004;
    ////    }

    ////    private const string MissingMember = "(?)";
    ////    private const string NullInstance = "(null)";
    ////    private const string StaticMethodObject = "(static)";
    ////    private const string NoParameters = "";
    ////    private const int EnterEventId = 1;
    ////    private const int ExitEventId = 2;
    ////    private const int AssociateEventId = 3;
    ////    private const int InfoEventId = 4;
    ////    private const int ErrorEventId = 5;
    ////    private const int CriticalFailureEventId = 6;
    ////    private const int DumpArrayEventId = 7;
    ////    public static readonly DnsEventSource Log = new DnsEventSource();

    ////    public static new bool IsEnabled => Log.IsEnabled();

    ////    [NonEvent]
    ////    public static void Info(object thisOrContextObject, FormattableString formattableString = null, [CallerMemberName] string memberName = null)
    ////    {
    ////        if (IsEnabled) Log.Info(IdOf(thisOrContextObject), memberName, formattableString != null ? Format(formattableString) : NoParameters);
    ////    }

    ////    public static void Info(object thisOrContextObject, object message, [CallerMemberName] string memberName = null)
    ////    {
    ////        if (IsEnabled) Log.Info(IdOf(thisOrContextObject), memberName, Format(message).ToString());
    ////    }

    ////    [Event(InfoEventId, Level = EventLevel.Informational, Keywords = Keywords.Default)]
    ////    private void Info(string thisOrContextObject, string memberName, string message) =>
    ////        WriteEvent(InfoEventId, thisOrContextObject, memberName ?? MissingMember, message);

    ////    [NonEvent]
    ////    public static string IdOf(object value) => value != null ? value.GetType().Name + "#" + GetHashCode(value) : NullInstance;

    ////    [NonEvent]
    ////    public static int GetHashCode(object value) => value?.GetHashCode() ?? 0;

    ////    [NonEvent]
    ////    private static string Format(FormattableString s)
    ////    {
    ////        switch (s.ArgumentCount)
    ////        {
    ////            case 0: return s.Format;
    ////            case 1: return string.Format(s.Format, Format(s.GetArgument(0)));
    ////            case 2: return string.Format(s.Format, Format(s.GetArgument(0)), Format(s.GetArgument(1)));
    ////            case 3: return string.Format(s.Format, Format(s.GetArgument(0)), Format(s.GetArgument(1)), Format(s.GetArgument(2)));
    ////            default:
    ////                object[] args = s.GetArguments();
    ////                object[] formattedArgs = new object[args.Length];
    ////                for (int i = 0; i < args.Length; i++)
    ////                {
    ////                    formattedArgs[i] = Format(args[i]);
    ////                }
    ////                return string.Format(s.Format, formattedArgs);
    ////        }
    ////    }

    ////    [NonEvent]
    ////    public static object Format(object value)
    ////    {
    ////        // If it's null, return a known string for null values
    ////        if (value == null)
    ////        {
    ////            return NullInstance;
    ////        }

    ////        // Format arrays with their element type name and length
    ////        Array arr = value as Array;
    ////        if (arr != null)
    ////        {
    ////            return $"{arr.GetType().GetElementType()}[{((Array)value).Length}]";
    ////        }

    ////        // Format ICollections as the name and count
    ////        ICollection c = value as ICollection;
    ////        if (c != null)
    ////        {
    ////            return $"{c.GetType().Name}({c.Count})";
    ////        }

    ////        // Format SafeHandles as their type, hash code, and pointer value
    ////        SafeHandle handle = value as SafeHandle;
    ////        if (handle != null)
    ////        {
    ////            return $"{handle.GetType().Name}:{handle.GetHashCode()}(0x{handle.DangerousGetHandle():X})";
    ////        }

    ////        // Format IntPtrs as hex
    ////        if (value is IntPtr)
    ////        {
    ////            return $"0x{value:X}";
    ////        }

    ////        // If the string representation of the instance would just be its type name,
    ////        // use its id instead.
    ////        string toString = value.ToString();
    ////        if (toString == null || toString == value.GetType().FullName)
    ////        {
    ////            return IdOf(value);
    ////        }

    ////        // Otherwise, return the original object so that the caller does default formatting.
    ////        return value;
    ////    }
    ////}
}