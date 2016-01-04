using System.Net.Sockets;

namespace Chat
{
    public class Human
    {
        public string Nick { get; internal set; }
        public TcpClient Client { get; private set; }

        public Human(TcpClient tcpClient, string nick)
        {
            Client = tcpClient;
            Nick = nick;
        }
    }
}
