using System;
using System.Net.Sockets;
using System.Windows;

namespace Chat
{
    public class TCPClient
    {
        public TcpClient Client { get; private set; }

        public TCPClient(string server, string port)
        {
            int servPort;
            if (!int.TryParse(port, out servPort)) servPort = TcpWorks.DefaultPort;

            try
            {
                Client = new TcpClient(server, servPort);
            }
            catch (Exception e)
            {
                MessageBox.Show("С сетью что-то не так.\n"+e.Message);
                if (Client != null) Client.Close();
                return;
            }

            if (!PerformHandshake()) return;

            TcpWorks.ReciveMessagesLoop(Client, PerformRecivedMessage);
        }

        private bool  PerformHandshake()
        {
            TcpWorks.SendObjectOnce("handshake", Client);

            var answer = (string) TcpWorks.ReciveObjectOnce(Client);

            if (answer == "handshake") return true;

            MessageBox.Show("Handshake didn't performed.");
            return false;
        }

        private static void PerformRecivedMessage(object obj, TcpClient client)
        {
            var mess = (Message)obj;

            var dispatch = Application.Current.Dispatcher;

            switch (mess.PacketType)
            {
                case Message.PackType.Nick:
                    dispatch.BeginInvoke(new Action(() => MainWindow.Wm.ChangeNick(mess.Body)));
                    dispatch.BeginInvoke(new Action(() => MainWindow.Wm.AddTextToUi("You are now known as " + mess.Body)));
                    break;
                case Message.PackType.Post:
                    dispatch.BeginInvoke(new Action(() => MainWindow.Wm.AddPostToUi((Message)mess)));
                    break;
                case Message.PackType.Ping:
                    break;
                case Message.PackType.Command:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void Send(Message message)
        {
            TcpWorks.SendObjectOnce(message, Client);
        }
    }
}
