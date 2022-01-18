using System;
using System.Net;
using System.Net.Sockets;

namespace DnsClient.Tests
{
    internal abstract class UdpServer : IDisposable
    {
        // Response to query("example.com", QueryType.TXT);
        private static readonly byte[] s_txtResponse = new byte[] { 33, 155, 129, 128, 0, 1, 0, 2, 0, 0, 0, 1, 7, 101, 120, 97, 109, 112, 108, 101, 3, 99, 111, 109, 0, 0, 16, 0, 1, 192, 12, 0, 16, 0, 1, 0, 0, 63, 124, 0, 12, 11, 118, 61, 115, 112, 102, 49, 32, 45, 97, 108, 108, 192, 12, 0, 16, 0, 1, 0, 0, 63, 124, 0, 33, 32, 121, 120, 118, 121, 57, 109, 52, 98, 108, 114, 115, 119, 103, 114, 115, 122, 56, 110, 100, 106, 104, 52, 54, 55, 110, 50, 121, 55, 109, 103, 108, 50, 0, 0, 41, 2, 0, 0, 0, 0, 0, 0, 0 };

        protected readonly byte[] _txtResponse;
        protected readonly UdpClient _client;
        private volatile bool _disposed;

        public int RequestsCount { get; private set; }

        public UdpServer(IPEndPoint ipEndPoint)
        {
            _txtResponse = (byte[])s_txtResponse.Clone();

            _client = new UdpClient(ipEndPoint);

            _client.BeginReceive(DataReceived, null);
            _client.Client.ReceiveTimeout = 500;
            _client.Client.SendTimeout = 500;
        }

        private void DataReceived(IAsyncResult ar)
        {
            try
            {
                RequestsCount++;

                var receivedEndPoint = new IPEndPoint(IPAddress.Any, 0);
                var receivedBytes = _client.EndReceive(ar, ref receivedEndPoint);

                DataReceivedInternal(receivedEndPoint, receivedBytes);
            }
            catch
            {
                // Do nothing
            }
            finally
            {
                while (!_disposed)
                {
                    try
                    {
                        _client.BeginReceive(DataReceived, null);
                    }
                    catch
                    {
                        // Do nothing
                    }
                }
            }
        }

        protected virtual void DataReceivedInternal(IPEndPoint receivedEndPoint, byte[] receivedBytes)
        {
        }

        public void Dispose()
        {
#if !NET45
            _client.Dispose();
#else
            _client.Close();
#endif

            _disposed = true;
        }
    }

    internal sealed class UdpServerMistmatchXid : UdpServer
    {
        private readonly int _mistmatchResponses;

        public int MistmatchedResponsesCount { get; private set; }

        public UdpServerMistmatchXid(IPEndPoint ipEndPoint, int mistmatchResponses) :
            base(ipEndPoint)
        {
            _mistmatchResponses = mistmatchResponses;
        }

        protected override void DataReceivedInternal(IPEndPoint receivedEndPoint, byte[] receivedBytes)
        {
            // Set the xid
            _txtResponse[0] = receivedBytes[0];
            _txtResponse[1] = receivedBytes[1];

            if (MistmatchedResponsesCount < _mistmatchResponses)
            {
                // Change the xid
                _txtResponse[1]++;
                MistmatchedResponsesCount++;
            }

            _client.Send(_txtResponse, _txtResponse.Length, receivedEndPoint);
        }
    }

    internal sealed class UdpServerDuplicateResponses : UdpServer
    {
        private readonly int _totalResponsesCount;

        public UdpServerDuplicateResponses(IPEndPoint ipEndPoint, int duplicatesCount) :
            base(ipEndPoint)
        {
            _totalResponsesCount = duplicatesCount + 1;
        }

        protected override void DataReceivedInternal(IPEndPoint receivedEndPoint, byte[] receivedBytes)
        {
            // Set the xid
            _txtResponse[0] = receivedBytes[0];
            _txtResponse[1] = receivedBytes[1];

            for (int i = 0; i < _totalResponsesCount; i++)
            {
                _client.Send(_txtResponse, _txtResponse.Length, receivedEndPoint);
            }
        }
    }
}
