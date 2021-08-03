using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using WatsonWebsocket;

namespace ClientApp
{
    class Program
    {
        static void Main(string[] args)
        {
            WatsonWsClient client = new WatsonWsClient("localhost",4000, true | false);
            client.ServerConnected += ServerConnected;
            client.ServerDisconnected += ServerDisconnected;
            client.MessageReceived += MessageReceived;
            client.Start();
            Console.WriteLine("Client Started");
            Console.ReadLine();
        }



        static void MessageReceived(object sender, MessageReceivedEventArgs args)
        {
            Console.WriteLine("Message from server: " + Encoding.UTF8.GetString(args.Data));
        }

        static void ServerConnected(object sender, EventArgs args)
        {
            Console.WriteLine("Server connected");
        }

        static void ServerDisconnected(object sender, EventArgs args)
        {
            Console.WriteLine("Server disconnected");
        }
    }
}
