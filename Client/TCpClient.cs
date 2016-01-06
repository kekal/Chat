using System;
using System.Linq;
using System.Net.Sockets;
using System.Windows;

namespace Chat
{
    public class TCPClient
    {
        public TCPClient(string server, string port)
        {
            int servPort;
            if (!int.TryParse(port, out servPort)) servPort = TcpWorks.DefaultPort;

            Client = new TcpClient(server, servPort);

            if (!PerformHandshake()) return;

            TcpWorks.ReciveMessagesLoop(Client, PerformRecivedMessage);
        }

        public TcpClient Client { get; private set; }

        private bool PerformHandshake()
        {
            TcpWorks.SendObjectOnce("handshake", Client);

            var answer = (string) TcpWorks.ReciveObjectOnce(Client);

            if (answer == "handshake") return true;

            MessageBox.Show("Handshake didn't performed.");
            return false;
        }

        private static void PerformRecivedMessage(object obj, TcpClient client)
        {
            var mess = (Message) obj;

            var dispatch = Application.Current.Dispatcher;

            switch (mess.PacketType)
            {
                case Message.PackType.Nick:
                    dispatch.BeginInvoke(new Action(() => MainWindow.Wm.ChangeNick(mess.Body)));
                    dispatch.BeginInvoke(new Action(() => MainWindow.Wm.AddTextToUi("You are now known as " + mess.Body)));
                    break;
                case Message.PackType.Post:
                    dispatch.BeginInvoke(new Action(() => MainWindow.Wm.AddPostToUi(mess)));
                    break;
                case Message.PackType.Ping:
                    break;
                case Message.PackType.Command:
                    var commandCollec = mess.Body.Split('%').ToList();
                    if (commandCollec[0] == "ROOM")
                    {
                        commandCollec.RemoveAt(0);
                        dispatch.BeginInvoke(new Action(() => MainWindow.Wm.ChangeRoom(commandCollec)));
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void Send(Message message)
        {
            try
            {
                TcpWorks.SendObjectOnce(message, Client);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }
    }
}