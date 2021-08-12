using Fleck;
using Impinj.OctaneSdk;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Path = System.IO.Path;
using System.Configuration;
using System.Diagnostics;
using System.Net;

namespace TunnelRedaerWpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        static string loAZ = "abcdefghijklmnopqrstuvwxyz";
        static string symbols = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`" + loAZ + "{|}~";
        static string READERIP = "";  // NEED to set to your speedway!
        static string SYSTEMIP = "";
        static string PORT = "";
        static string action = "";
        static ImpinjReader reader = new ImpinjReader();
        static IWebSocketConnection webSocketConnection;
        public static Dictionary<string, string> dicProfile = new Dictionary<string, string>();
        int tagcount = 0;
        List<string> uniqueTags = new List<string>();
        Stopwatch watch = new Stopwatch();
        

        public IConfiguration Configuration { get; private set; }

        public MainWindow()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("TAG COUNT");
                InitializeComponent();
                var builder = new ConfigurationBuilder()
               .SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("appsetting.json", optional: false, reloadOnChange: true);
                Configuration = builder.Build();
                READERIP = Configuration.GetConnectionString("ReaderIp");
                SYSTEMIP = GetIPAddress();
                lblIp.Content = SYSTEMIP;
                PORT = Configuration.GetConnectionString("Port");
                //readProfile();
                startServer();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message.ToString());
                e.Message.ToString();
            }
        }

       

        public string GetIPAddress()
        {
            string IPAddress="";
            IPHostEntry Host = default(IPHostEntry);
            string Hostname = null;
            Hostname = System.Environment.MachineName;
            Host = Dns.GetHostEntry(Hostname);
            foreach (IPAddress IP in Host.AddressList)
            {
                if (IP.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    IPAddress = Convert.ToString(IP);
                }
            }
            return IPAddress;
        }

        private void startServer()
        {
            var server = new WebSocketServer("ws://" + SYSTEMIP + ":" + PORT);
            string clientAddress = "";
            server.Start(socket =>
            {
                webSocketConnection = socket;
                socket.OnOpen = () =>
                {
                    //clientPort = socket.ConnectionInfo.ClientPort;
                    clientAddress = socket.ConnectionInfo.ClientIpAddress;
                    System.Diagnostics.Debug.WriteLine("Open!");
                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        tagcount = 0;
                        lstTags.Items.Clear();
                        lblScanLabel.Content = "Client Connected";
                        lblTunnlCount.Content = "0";
                        lblCount.Content = "0";
                        lblScanLabel.Foreground = new SolidColorBrush(Colors.YellowGreen);
                    }));

                };
                socket.OnClose = () =>
                {
                    System.Diagnostics.Debug.WriteLine("Close!");

                    this.Dispatcher.Invoke(new Action(() =>
                    {

                        reader.Stop();
                        reader.Disconnect();
                        lblScanLabel.Content = "Client DisConnected";
                        //lblTunnlCount.Content = "0";
                        //lblCount.Content = "0";
                        //tagcount = 0;
                        //lstTags.Items.Clear();
                        lblScanLabel.Foreground = new SolidColorBrush(Colors.Red);
                    }));
                    socket.Send("Scan stopped");
                };
                socket.OnError = exception =>
                {
                    System.Diagnostics.Debug.WriteLine(exception);
                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        reader.Stop();
                        reader.Disconnect();
                        lblScanLabel.Content = "Client DisConnected";
                        lblScanLabel.Foreground = new SolidColorBrush(Colors.Red);
                    }));
                    socket.Send("Scan stopped");
                };

                socket.OnMessage = message =>
                {
                    System.Diagnostics.Debug.WriteLine(message);
                    action = message;
                    try
                    {
                        if (message.Equals("start"))
                        {
                            this.Dispatcher.Invoke(new Action(() =>
                            {
                                tagcount = 0;
                                lblTunnlCount.Content = "0";
                                lblCount.Content = "0";
                                lblScanLabel.Content = "Scan Started";
                                lblScanLabel.Foreground = new SolidColorBrush(Colors.Green);
                                socket.Send("Scan Started");
                            }));

                            initSetting();
                        }
                        else
                        {

                            this.Dispatcher.Invoke(new Action(() =>
                            {

                                lblScanLabel.Content = "Scan stopped";
                                /*lblTunnlCount.Content = "0";
                                lblCount.Content = "0";
                                tagcount = 0;
                                lstTags.Items.Clear();*/
                                lblScanLabel.Foreground = new SolidColorBrush(Colors.Red);
                                reader.Stop();
                                reader.Disconnect();
                                socket.Send("Scan stopped");
                            }));
                        }

                    }
                    catch (Exception e)
                    {
                        e.Message.ToString();
                        MessageBox.Show(e.Message.ToString());
                        //socket.Send(e.Message.ToString());
                    }

                };
            });

            System.Diagnostics.Debug.WriteLine("Start listening");

            Console.ReadLine();
        }

        private void initSetting()
        {

            // Assign a name to the reader. 
            // This will be used in tag reports. 
            reader.Name = "My Reader #1";

            // Connect to the reader.
           // if (!reader.IsConnected)
            {
                ConnectToReader();
                Settings settings = reader.QueryDefaultSettings();

                settings.AutoStart.Mode = AutoStartMode.Immediate;
                settings.AutoStart.GpiPortNumber = 0;
                settings.AutoStart.GpiLevel = false;
                settings.AutoStart.FirstDelayInMs = 0;
                settings.AutoStart.PeriodInMs = 0;
                settings.AutoStart.UtcTimestamp = 0;

                settings.AutoStop.Mode = AutoStopMode.None;
                settings.AutoStop.DurationInMs = 0;
                settings.AutoStop.GpiPortNumber = 0;
                settings.AutoStop.GpiLevel = false;
                settings.AutoStop.Timeout = 0;

                settings.ReaderMode = ReaderMode.MaxThroughput;
                settings.RfMode = 1002;
                settings.SearchMode = SearchMode.DualTarget;

                settings.Session = 3;
                settings.TagPopulationEstimate = 32;

                settings.LowDutyCycle.IsEnabled = false;
                settings.LowDutyCycle.EmptyFieldTimeoutInMs = 500;
                settings.LowDutyCycle.FieldPingIntervalInMs = 200;

                settings.Filters.Mode = TagFilterMode.None;

                settings.TruncatedReply.IsEnabled = false;
                settings.TruncatedReply.Gen2v2TagsOnly = true;
                settings.TruncatedReply.EpcLengthInWords = 0;
                settings.TruncatedReply.BitPointer = 0;


                settings.Report.IncludeAntennaPortNumber = true;
                settings.Report.IncludeChannel = true;
                settings.Report.IncludeFirstSeenTime = true;
                settings.Report.IncludeLastSeenTime = false;
                settings.Report.IncludePeakRssi = true;
                settings.Report.IncludeSeenCount = false;
                settings.Report.IncludeFastId = false;
                settings.Report.IncludePhaseAngle = false;
                settings.Report.IncludeDopplerFrequency = false;
                settings.Report.IncludeGpsCoordinates = false;
                settings.Report.IncludePcBits = false;
                settings.Report.IncludeCrc = false;
                settings.Report.Mode = ReportMode.Individual;

                for (ushort i = 1; i <= 32; i++)
                {
                    settings.Antennas.GetAntenna(i).IsEnabled = true;
                    settings.Antennas.GetAntenna(i).PortNumber = i;
                    settings.Antennas.GetAntenna(i).PortName = "Antenna Port " + i;
                    settings.Antennas.GetAntenna(i).TxPowerInDbm = 30;
                    settings.Antennas.GetAntenna(i).RxSensitivityInDbm = -80;
                    settings.Antennas.GetAntenna(i).MaxRxSensitivity = false;
                    settings.Antennas.GetAntenna(i).MaxTxPower = false;

                }

                settings.Keepalives.Enabled = false;
                settings.Keepalives.PeriodInMs = 0;
                settings.Keepalives.EnableLinkMonitorMode = false;
                settings.Keepalives.LinkDownThreshold = 0;


                settings.HoldReportsOnDisconnect = false;

                settings.SpatialConfig.Mode = SpatialMode.Inventory;

                settings.SpatialConfig.Placement.HeightCm = 400;
                settings.SpatialConfig.Placement.FacilityXLocationCm = 0;
                settings.SpatialConfig.Placement.FacilityYLocationCm = 0;
                settings.SpatialConfig.Placement.OrientationDegrees = 0;

                settings.SpatialConfig.Location.ComputeWindowSeconds = 10;
                settings.SpatialConfig.Location.TagAgeIntervalSeconds = 20;
                settings.SpatialConfig.Location.UpdateIntervalSeconds = 5;
                settings.SpatialConfig.Location.UpdateReportEnabled = true;
                settings.SpatialConfig.Location.EntryReportEnabled = true;
                settings.SpatialConfig.Location.ExitReportEnabled = true;
                settings.SpatialConfig.Location.DiagnosticReportEnabled = false;
                settings.SpatialConfig.Location.MaxTxPower = false;
                settings.SpatialConfig.Location.TxPowerInDbm = 30;

                settings.SpatialConfig.Direction.TagAgeIntervalSeconds = 20;
                settings.SpatialConfig.Direction.UpdateIntervalSeconds = 5;
                settings.SpatialConfig.Direction.UpdateReportEnabled = true;
                settings.SpatialConfig.Direction.EntryReportEnabled = true;
                settings.SpatialConfig.Direction.ExitReportEnabled = true;
                settings.SpatialConfig.Direction.FieldOfView = DirectionFieldOfView.Wide;
                settings.SpatialConfig.Direction.Mode = DirectionMode.HighPerformance;
                settings.SpatialConfig.Direction.TagPopulationLimit = 0;
                settings.SpatialConfig.Direction.DiagnosticReportEnabled = false;
                settings.SpatialConfig.Direction.MaxTxPower = false;
                settings.SpatialConfig.Direction.TxPowerInDbm = 30;

                reader.ApplySettings(settings);

                reader.KeepaliveReceived += OnKeepaliveReceived;
                reader.ConnectionLost += OnConnectionLost;
                reader.TagsReported += OnTagsReported;


            }



        }

        

        public void ConnectToReader()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Attempting to connect to {0} ({1})." + reader.Name + READERIP);
                reader.ConnectTimeout = 6000;
                reader.Connect(READERIP);
                System.Diagnostics.Debug.WriteLine("Successfully connected.");
            }
            catch (OctaneSdkException e)
            {
                System.Diagnostics.Debug.WriteLine("Failed to connect.");
                this.Dispatcher.Invoke(new Action(() =>
                {
                    lblScanLabel.Content = e.Message.ToString();
                    lblScanLabel.Foreground = new SolidColorBrush(Colors.Red);
                }));
            }
        }

        public void OnConnectionLost(ImpinjReader reader)
        {
            System.Diagnostics.Debug.WriteLine("Connection lost : {0} ({1})" + reader.Name + reader.Address);
            reader.Disconnect();
            ConnectToReader();
        }

        static void OnKeepaliveReceived(ImpinjReader reader)
        {
            System.Diagnostics.Debug.WriteLine("Keepalive received from {0} ({1})" + reader.Name + reader.Address);
        }

        private void OnTagsReported(ImpinjReader sender, TagReport report)
        {
            if (!action.Equals("stop"))
            {
              
                if (report.Tags.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine("TAG COUNT" + report.Tags.Count());
                    System.Diagnostics.Debug.WriteLine(toAscii(report.Tags[0].Epc.ToHexString()));
                    string a = report.Tags[0].Epc.ToHexString();
                    tagcount = tagcount + 1;
                    if (!uniqueTags.Contains(a))
                    {
                        uniqueTags.Add(a);

                    }
                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        TimeSpan timeSpan = watch.Elapsed;
                        lblTunnlSeconds.Content = timeSpan.Seconds.ToString();
                        lblCount.Content = tagcount.ToString();
                        lblTunnlCount.Content = uniqueTags.Count.ToString();
                        if (!lstTags.Items.Contains(a))
                        {
                            lstTags.Items.Add(a);

                            if (webSocketConnection != null)
                            {
                                webSocketConnection.Send(a);
                            }
                        }
                    }));
                    

                }

            }

        }

        public static string toAscii(string valueStr)
        {
            valueStr = valueStr.ToLower();
            var hex = "0123456789abcdef";
            var text = "";
            int i = 0;

            for (i = 0; i < valueStr.Length; i = i + 2)
            {
                char char1 = valueStr[i];
                if (char1 == ':')
                {
                    i++;
                    char1 = valueStr[i];
                }
                var char2 = valueStr[i + 1];
                var num1 = hex.IndexOf(char1);
                var num2 = hex.IndexOf(char2);
                var value = num1 << 4;
                value = value | num2;

                int valueInt = int.Parse(value.ToString());
                var symbolIndex = valueInt - 32;
                //var ch = '?';
                var ch = ' ';
                if (symbolIndex >= 0 && value <= 126)
                {
                    ch = symbols[symbolIndex];
                }
                text += ch;
            }
            return text.ToString().Trim();

        }

        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                File.WriteAllLines(Environment.CurrentDirectory.ToString() + "/log.txt", lstTags.Items.OfType<string>().ToArray(), Encoding.UTF8);

            }
            catch (Exception ex)
            {
                ex.Message.ToString();
            }
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            lblTunnlCount.Content = "0";
            lblCount.Content = "0";
            tagcount = 0;
            lstTags.Items.Clear();
            uniqueTags.Clear();
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            if (btnStart.Content.Equals("Start"))
            {
                initSetting();
                watch.Start();
                btnStart.Content = "Stop";
            }
            else
            {
                watch.Stop();
                btnStart.Content = "Start";
                reader.Stop();
            }


        }
    }
}
