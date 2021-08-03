using Impinj.OctaneSdk;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using WatsonWebsocket;
using System.Configuration;
using Fleck;
using System.Threading;

namespace TunnelReaderApp
{
    class Program
    {
        static string loAZ = "abcdefghijklmnopqrstuvwxyz";
        static string symbols = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`" + loAZ + "{|}~";
        static string READERIP = "";  // NEED to set to your speedway!
        static string SYSTEMIP = "";
        static string PORT = "";
        static string action = "";
        static ImpinjReader reader = new ImpinjReader();
        static IWebSocketConnection webSocketConnection;

        static void Main(string[] args)
        {
            READERIP = ConfigurationManager.AppSettings["readerip"];
            SYSTEMIP = ConfigurationManager.AppSettings["systemip"];
            PORT = ConfigurationManager.AppSettings["port"];
            startServer();         
        }

        static void startServer()
        {
            var server = new WebSocketServer("ws://"+ SYSTEMIP+":"+PORT);
            string clientAddress = "";
            server.Start(socket =>
            {
                webSocketConnection = socket;
                socket.OnOpen = () =>
                {
                    //clientPort = socket.ConnectionInfo.ClientPort;
                    clientAddress = socket.ConnectionInfo.ClientIpAddress;
                    Console.WriteLine("Open!");
                };
                socket.OnClose = () =>
                {
                    Console.WriteLine("Close!");
                    reader.Stop();
                    reader.Disconnect();
                    socket.Send("Scan stopped");
                };
                socket.OnError = exception =>
                {
                    Console.WriteLine(exception);
                    reader.Stop();
                    reader.Disconnect();
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
                            socket.Send("Scan Started");
                            initSetting();
                        }
                        else
                        {
                            //reader.Stop();
                            //reader.Disconnect();
                            socket.Send("Scan stopped");
                        }
                        
                    }
                    catch(Exception e)
                    {
                        e.Message.ToString();
                        socket.Send(e.Message.ToString());
                    }
                   
                };
            });
        
            Console.WriteLine("Start listening");

            Console.ReadLine();
        }

        static void initSetting()
        {
            try
            {
                // Assign a name to the reader. 
                // This will be used in tag reports. 
                reader.Name = "My Reader #1";

                // Connect to the reader.
                ConnectToReader();

                // Get the default settings.
                // We'll use these as a starting point
                // and then modify the settings we're 
                // interested in.
                Settings settings = reader.QueryDefaultSettings();

                // Start the reader as soon as it's configured.
                // This will allow it to run without a client connected.
                settings.AutoStart.Mode = AutoStartMode.Immediate;
                settings.AutoStop.Mode = AutoStopMode.None;

                // Use Advanced GPO to set GPO #1 
                // when an client (LLRP) connection is present.
                // settings.Gpos.GetGpo(1).Mode = GpoMode.LLRPConnectionStatus;

                // Tell the reader to include the timestamp in all tag reports.
                settings.Report.IncludeFirstSeenTime = true;
                settings.Report.IncludeLastSeenTime = true;
                settings.Report.IncludeSeenCount = true;

                // If this application disconnects from the 
                // reader, hold all tag reports and events.
                settings.HoldReportsOnDisconnect = true;

                // Enable keepalives.
                settings.Keepalives.Enabled = true;
                settings.Keepalives.PeriodInMs = 5000;

                // Enable link monitor mode.
                // If our application fails to reply to
                // five consecutive keepalive messages,
                // the reader will close the network connection.
                settings.Keepalives.EnableLinkMonitorMode = true;
                settings.Keepalives.LinkDownThreshold = 5;

                // Assign an event handler that will be called
                // when keepalive messages are received.
                //reader.KeepaliveReceived += OnKeepaliveReceived;

                // Assign an event handler that will be called
                // if the reader stops sending keepalives.
                //reader.ConnectionLost += OnConnectionLost;

                // Apply the newly modified settings.
                reader.ApplySettings(settings);

                // Save the settings to the reader's 
                // non-volatile memory. This will
                // allow the settings to persist
                // through a power cycle.
                reader.SaveSettings();

                // Assign the TagsReported event handler.
                // This specifies which method to call
                // when tags reports are available.
                reader.TagsReported += OnTagsReported;

                // Wait for the user to press enter.
                Console.WriteLine("Press enter to exit.");
                Console.ReadLine();

                // Stop reading.
                reader.Stop();

                // Disconnect from the reader.
                reader.Disconnect();
            }
            catch (OctaneSdkException e)
            {
                // Handle Octane SDK errors.
                Console.WriteLine("Octane SDK exception: {0}", e.Message);
            }
            catch (Exception e)
            {
                // Handle other .NET errors.
                Console.WriteLine("Exception : {0}", e.Message);
            }
        }

        static void ConnectToReader()
        {
            try
            {
                Console.WriteLine("Attempting to connect to {0} ({1}).",reader.Name, READERIP);             
                reader.ConnectTimeout = 6000;              
                reader.Connect(READERIP);
                Console.WriteLine("Successfully connected.");
            }
            catch (OctaneSdkException e)
            {
                Console.WriteLine("Failed to connect.");
                throw e;
            }
        }
        
         static void OnConnectionLost(ImpinjReader reader)
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

        static async void OnTagsReported(ImpinjReader sender, TagReport report)
        {
           try
            {
                if (action.Equals("start"))
                {
                    foreach (Tag tag in report)
                    {                   
                        Console.WriteLine(toAscii(tag.Epc.ToHexString()));
                        webSocketConnection.Send(toAscii(tag.Epc.ToHexString())); 
                    }
                }
            }
            catch(Exception e)
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

    }

}
