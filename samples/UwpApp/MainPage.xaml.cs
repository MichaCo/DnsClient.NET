using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using DnsClient;
using Windows.Networking.Connectivity;
using Windows.UI;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace UwpApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private LookupClient _client;

        public MainPage()
        {
            this.InitializeComponent();
            
            _client = new LookupClient();
            _client.EnableAuditTrail = true;
            _client.ThrowDnsErrors = false;
            _client.UseCache = false;
        }
        
        private async void txtQuery_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                var query = (sender as TextBox).Text;
                await RunQuery(query ?? "");
            }
        }

        private async void cmdQuery_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            var query = txtQuery.Text;
            await RunQuery(query ?? "");
        }

        private async Task RunQuery(string query)
        {
            this.txtQuery.IsEnabled = false;
            try
            {
                var result = await _client.QueryAsync(query, QueryType.ANY);
                
                this.txtOutput.Text = result.AuditTrail;
            }
            catch (Exception ex)
            {
                this.txtOutput.Text = ex.InnerException?.ToString() ?? ex.ToString();
            }
            finally
            {
                this.txtQuery.IsEnabled = true;
            }
        }
        
        private void cmdAddresses_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            var interfaces = Interop.IpHlpApi.SystemNetworkInterface.GetAllNetworkInterfaces();

            var info = Interop.IpHlpApi.FixedNetworkInformation.GetFixedNetworkInformation();

            this.txtOutput.Text = $@"
{string.Join("\r\n ", interfaces.Select(p=>p.Name + "\t" + p.NetworkInterfaceType + "\t" + p.GetPhysicalAddress() + "\t" + p.OperationalStatus))}

{info.DomainName}
{info.HostName}
{string.Join(", ", info.DnsAddresses.Select(p=>p.ToString()))}
";
        }
    }
    
    internal static class IPAddressParserStatics
    {
        public const int IPv4AddressBytes = 4;
        public const int IPv6AddressBytes = 16;
    }
}