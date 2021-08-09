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

        public IConfiguration Configuration { get; private set; }

        public MainWindow()
        {
            try
            {
                InitializeComponent();
                var builder = new ConfigurationBuilder()
               .SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("appsetting.json", optional: false, reloadOnChange: true);
                Configuration = builder.Build();
                READERIP =Configuration.GetConnectionString("ReaderIp");
                SYSTEMIP = Configuration.GetConnectionString("SystemIp");
                PORT = Configuration.GetConnectionString("Port");              
                //readProfile();
                startServer();
            }
            catch(Exception e)
            {
                MessageBox.Show(e.Message.ToString());
                e.Message.ToString();                    
            }
        }


        private  void startServer()
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
                    Console.WriteLine("Open!");
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
                    Console.WriteLine("Close!");
    
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
                    Console.WriteLine(exception);
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
                    Console.WriteLine(message);
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

            Console.WriteLine("Start listening");

            Console.ReadLine();
        }

        private  void initSetting()
        {
            try
            {
                // Assign a name to the reader. 
                // This will be used in tag reports. 
                reader.Name = "My Reader #1";

                // Connect to the reader.
                ConnectToReader();

                try
                {
                    Settings settings = reader.QueryDefaultSettings();


                    settings.AutoStart.Mode = AutoStartMode.Immediate;
                    settings.AutoStop.Mode = AutoStopMode.None;


                    settings.Report.IncludeFirstSeenTime = true;
                    settings.Report.IncludeLastSeenTime = true;
                    settings.Report.IncludeSeenCount = true;


                    settings.HoldReportsOnDisconnect = true;


                    settings.Keepalives.Enabled = true;
                    settings.Keepalives.PeriodInMs = 5000;

                    settings.Keepalives.EnableLinkMonitorMode = true;
                    settings.Keepalives.LinkDownThreshold = 5;


                    reader.KeepaliveReceived += OnKeepaliveReceived;

                    reader.ConnectionLost += OnConnectionLost;

                    settings.ReaderMode = ReaderMode.DenseReaderM4;

                    settings.SearchMode = SearchMode.DualTarget;

                    settings.Session = 3;

                    settings.TagPopulationEstimate = 32;

                    settings.Report.IncludeFastId = false;

                    settings.Antennas.DisableAll();

                    settings.Antennas.GetAntenna(1).IsEnabled = true;
                    settings.Antennas.GetAntenna(1).MaxTxPower = true;
                    settings.Antennas.GetAntenna(1).TxPowerInDbm = 31.5;
                    settings.Antennas.GetAntenna(1).MaxRxSensitivity = true;
                    settings.Antennas.GetAntenna(1).RxSensitivityInDbm = -80;

                    settings.Antennas.GetAntenna(9).IsEnabled = true;
                    settings.Antennas.GetAntenna(9).MaxTxPower = true;
                    settings.Antennas.GetAntenna(9).TxPowerInDbm = 31.5;
                    settings.Antennas.GetAntenna(9).MaxRxSensitivity = true;
                    settings.Antennas.GetAntenna(9).RxSensitivityInDbm = -80;

                    settings.Antennas.GetAntenna(17).IsEnabled = true;
                    settings.Antennas.GetAntenna(17).MaxTxPower = true;
                    settings.Antennas.GetAntenna(17).TxPowerInDbm = 31.5;
                    settings.Antennas.GetAntenna(17).MaxRxSensitivity = true;
                    settings.Antennas.GetAntenna(17).RxSensitivityInDbm = -80;

                    settings.Antennas.GetAntenna(25).IsEnabled = true;
                    settings.Antennas.GetAntenna(25).MaxTxPower = true;
                    settings.Antennas.GetAntenna(25).TxPowerInDbm = 31.5;
                    settings.Antennas.GetAntenna(25).MaxRxSensitivity = true;
                    settings.Antennas.GetAntenna(25).RxSensitivityInDbm = -80;

                    settings.Report.Mode = ReportMode.Individual;

                    settings.LowDutyCycle.IsEnabled = true;
                    settings.LowDutyCycle.EmptyFieldTimeoutInMs = 500;
                    settings.LowDutyCycle.FieldPingIntervalInMs = 200;
                    
                    settings.Filters.Mode = TagFilterMode.None;
                                        
                    //settings.ReducedPowerFrequenciesInMhz.Add(865.70);
                   
                   //settings.ReducedPowerFrequenciesInMhz.Add(866.30);
                  
                   //settings.ReducedPowerFrequenciesInMhz.Add(866.90);
                   
                   //settings.ReducedPowerFrequenciesInMhz.Add(867.50);

                    //switch ( dicProfile["Reader_Mode"])
                    //{
                    //    case "0":
                    //        settings.ReaderMode = ReaderMode.MaxThroughput;
                    //        break;
                    //    case "1":
                    //        settings.ReaderMode = ReaderMode.Hybrid;
                    //        break;
                    //    case "2":
                    //        settings.ReaderMode = ReaderMode.DenseReaderM4;
                    //        break;
                    //    case "3":
                    //        settings.ReaderMode = ReaderMode.DenseReaderM8;
                    //        break;
                    //    case "5":
                    //        settings.ReaderMode = ReaderMode.DenseReaderM4Two;
                    //        break;
                    //    case "1000":
                    //        settings.ReaderMode = ReaderMode.AutoSetDenseReader;
                    //        break;
                    //    case "1002":
                    //        settings.ReaderMode = ReaderMode.AutoSetDenseReaderDeepScan;
                    //        break;
                    //    case "1003":
                    //        settings.ReaderMode = ReaderMode.AutoSetStaticFast;
                    //        break;
                    //    case "1004":
                    //        settings.ReaderMode = ReaderMode.AutoSetStaticDRM;
                    //        break;
                    //    case "1005":
                    //        settings.ReaderMode = ReaderMode.AutoSetCustom;
                    //        break;

                    //    default:
                    //        break;
                    //}

                    //settings.Session = ushort.Parse( dicProfile["Session"]);
                    //settings.TagPopulationEstimate = ushort.Parse( dicProfile["Population"]);

                    //switch (int.Parse( dicProfile["Inventory_Mode"]))
                    //{
                    //    case 0:
                    //        settings.SearchMode = SearchMode.SingleTarget;
                    //        break;

                    //    case 1:
                    //        settings.SearchMode = SearchMode.DualTarget;
                    //        break;

                    //    case 2:
                    //        settings.SearchMode = SearchMode.TagFocus;
                    //        break;

                    //    case 3:
                    //        settings.SearchMode = SearchMode.SingleTargetReset;
                    //        break;

                    //    case 4:
                    //        settings.SearchMode = SearchMode.DualTargetBtoASelect;
                    //        break;

                    //    default:
                    //        break;
                    //}

                    //settings.Report.Mode = ReportMode.Individual;
                    //settings.Report.IncludeFastId = bool.Parse( dicProfile["FastID"]);
                    //switch ( dicProfile["MemoryBank"])
                    //{
                    //    case "EPC":
                    //        settings.Filters.TagFilter1.MemoryBank = MemoryBank.Epc;
                    //        settings.Filters.TagFilter1.BitPointer = BitPointers.Epc;
                    //        settings.Filters.TagFilter1.BitPointer = ushort.Parse( dicProfile["BitPointer"]);
                    //        settings.Filters.TagFilter1.BitCount = ushort.Parse( dicProfile["MaskLength"]);
                    //        break;

                    //    case "TID":
                    //        settings.Filters.TagFilter1.MemoryBank = MemoryBank.Tid;
                    //        settings.Filters.TagFilter1.BitPointer = ushort.Parse( dicProfile["BitPointer"]);
                    //        settings.Filters.TagFilter1.BitCount = ushort.Parse( dicProfile["MaskLength"]);
                    //        break;

                    //    case "Reserved":
                    //        settings.Filters.TagFilter1.MemoryBank = MemoryBank.Reserved;
                    //        settings.Filters.TagFilter1.BitPointer = ushort.Parse( dicProfile["BitPointer"]);
                    //        settings.Filters.TagFilter1.BitCount = ushort.Parse( dicProfile["MaskLength"]);

                    //        break;

                    //    case "User":
                    //        settings.Filters.TagFilter1.MemoryBank = MemoryBank.User;
                    //        settings.Filters.TagFilter1.BitPointer = ushort.Parse( dicProfile["BitPointer"]);
                    //        settings.Filters.TagFilter1.BitCount = ushort.Parse( dicProfile["MaskLength"]);

                    //        break;

                    //    default:
                    //        break;
                    //}

                    //if (bool.Parse( dicProfile["UseSpecifiedReq"]))
                    //{
                    //    if (bool.Parse( dicProfile["4"]))
                    //        settings.ReducedPowerFrequenciesInMhz.Add(865.70);
                    //    if (bool.Parse( dicProfile["7"]))
                    //        settings.ReducedPowerFrequenciesInMhz.Add(866.30);
                    //    if (bool.Parse( dicProfile["10"]))
                    //        settings.ReducedPowerFrequenciesInMhz.Add(866.90);
                    //    if (bool.Parse( dicProfile["13"]))
                    //        settings.ReducedPowerFrequenciesInMhz.Add(867.50);
                    //}

                    //settings.LowDutyCycle.EmptyFieldTimeoutInMs = ushort.Parse( dicProfile["EmptyFieldTimeout"]);
                    //settings.LowDutyCycle.FieldPingIntervalInMs = ushort.Parse( dicProfile["FieldPingInterval"]);
                    //settings.LowDutyCycle.IsEnabled = bool.Parse( dicProfile["LowDutyCycle"]);

                    //settings.Report.IncludeAntennaPortNumber = true;
                    //settings.Report.IncludePeakRssi = true;
                    //settings.Report.IncludePcBits = true;

                    reader.ApplySettings(settings);

                    //reader.SaveSettings();

                    reader.TagsReported += OnTagsReported;

                    Console.WriteLine("Press enter to exit.");
                    Console.ReadLine();

                    //reader.Stop();

                    // Disconnect from the reader.
                    //reader.Disconnect();


                }
                catch (Exception ex)
                {
                    ex.Message.ToString();
                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        lblScanLabel.Content = ex.Message.ToString();
                        lblScanLabel.Foreground = new SolidColorBrush(Colors.Red);
                    }));
                    
                }
           
            }
            catch (OctaneSdkException e)
            {
                // Handle Octane SDK errors.
                Console.WriteLine("Octane SDK exception: {0}", e.Message);
                this.Dispatcher.Invoke(new Action(() =>
                {
                    lblScanLabel.Content = e.Message.ToString();
                    lblScanLabel.Foreground = new SolidColorBrush(Colors.Red);
                }));

            }
            catch (Exception e)
            {
                // Handle other .NET errors.
                Console.WriteLine("Exception : {0}", e.Message);
                this.Dispatcher.Invoke(new Action(() =>
                {
                    lblScanLabel.Content = e.Message.ToString();
                    lblScanLabel.Foreground = new SolidColorBrush(Colors.Red);
                }));

            }
        }

        private void readProfile()
        {
            try
            {
                string filename = null;
                string[] ProfileFiles = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + @"ReaderSetting\");
                foreach (string p in ProfileFiles)
                {
                    string name = Path.GetFileNameWithoutExtension(p);
                    filename = p;
                }

                if (!File.Exists(filename))
                {
                    MessageBox.Show("Reader Profile Configuration File missing");
                    return;
                }

                string[] file = File.ReadAllLines(filename);
                dicProfile.Clear();
                foreach (string i in file)
                {
                    dicProfile.Add(i.Split('=')[0], i.Split('=')[1]);
                }
            }
            catch(Exception e)
            {
                e.Message.ToString();
            }
        }

        public  void ConnectToReader()
        {
            try
            {
                Console.WriteLine("Attempting to connect to {0} ({1}).", reader.Name, READERIP);
                reader.ConnectTimeout = 6000;
                reader.Connect(READERIP);
                Console.WriteLine("Successfully connected.");
            }
            catch (OctaneSdkException e)
            {
                Console.WriteLine("Failed to connect.");
                this.Dispatcher.Invoke(new Action(() =>
                {
                    lblScanLabel.Content = e.Message.ToString();
                    lblScanLabel.Foreground = new SolidColorBrush(Colors.Red);
                }));
            }
        }

        public  void OnConnectionLost(ImpinjReader reader)
        {
            Console.WriteLine("Connection lost : {0} ({1})", reader.Name, reader.Address);
            reader.Disconnect();
            ConnectToReader();
        }

        static void OnKeepaliveReceived(ImpinjReader reader)
        {
            // This event handler is called when a keepalive 
            // message is received from the reader.
            Console.WriteLine("Keepalive received from {0} ({1})", reader.Name, reader.Address);
        }

        private void OnTagsReported(ImpinjReader sender, TagReport report)
        {
            try
            {
                if (!action.Equals("stop"))
                {
                    if(report.Tags.Count>0)
                    {
                        Console.WriteLine(toAscii(report.Tags[0].Epc.ToHexString()));
                        string a = toAscii(report.Tags[0].Epc.ToHexString());
                        if(webSocketConnection!=null)
                        {
                            webSocketConnection.Send(report.Tags[0].Epc.ToHexString());
                        }                       
                        this.Dispatcher.Invoke(new Action(() =>
                        {
                            tagcount = tagcount + 1;
                            lblCount.Content = tagcount.ToString();
                            
                            if(!uniqueTags.Contains(a))
                            {
                                uniqueTags.Add(a);
                                lblTunnlCount.Content = uniqueTags.Count.ToString();
                            }

                            if (!lstTags.Items.Contains(a))
                            {
                                lstTags.Items.Add(a);
                                lblTunnlCount.Content = lstTags.Items.Count.ToString();
                            }
                        }));
                    }

                }
                
            }
            catch (Exception e)
            {
                e.Message.ToString();
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
                File.WriteAllLines(Environment.CurrentDirectory.ToString()+"/log.txt", lstTags.Items.OfType<string>().ToArray(), Encoding.UTF8);

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
            if(btnStart.Content.Equals("Start"))
            {
                initSetting();
                btnStart.Content = "Stop";
            }
            else
            {
                btnStart.Content = "Start";
                reader.Stop();
            }
           

        }
    }
}
