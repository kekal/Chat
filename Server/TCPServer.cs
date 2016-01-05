using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Chat
{
    public static class Server
    {
        private static TcpServer _server;

        private static void Main()
        {
            _server = new TcpServer("50000");
        }
    }

    public class TcpServer
    {
        private static readonly ManualResetEvent NewMessegeEvent = new ManualResetEvent(false);
        private readonly TcpListener _listener;
        private static LinkedList<Human> _humans;
        private static ConcurrentQueue<Message> _messages;

        internal TcpServer(string port)
        {
            int servPort;
            
            _messages = new ConcurrentQueue<Message>();
            _humans = new LinkedList<Human>();

            if (!int.TryParse(port, out servPort)) servPort = TcpWorks.DefaultPort;

            try
            {
                _listener = new TcpListener(IPAddress.Any, servPort);
                _listener.Start();
            }
            catch (SocketException se)
            {
                Console.WriteLine(se.ErrorCode + ": " + se.Message);
                Environment.Exit(se.ErrorCode);
            }

            var listenThread = new Thread(WaitForClient);
            listenThread.Start();


            var broadcastThread = new Thread(BroadcastMessages);
            broadcastThread.Start();
        }

        private void BroadcastMessages()
        {
            while (true)
            {
                NewMessegeEvent.Reset();

                Message mess;
                _messages.TryDequeue(out mess);
                if (mess != null)
                {
                    foreach (var human in _humans)
                    {
                        TcpWorks.SendObjectOnce(mess, human.Client);
                    }
                }
                NewMessegeEvent.WaitOne();
            }
        }

        private void WaitForClient()
        {
            _listener.BeginAcceptTcpClient(LetClientJoin, null);
            Console.WriteLine("Start waiting for incoming connection");
        }

        private void LetClientJoin(IAsyncResult ar)
        {
            Console.WriteLine("Handling new client");
            var client = _listener.EndAcceptTcpClient(ar);
            ReplyToHandshake(client);

            var nick = GenerateNick();

            _humans.AddLast(new Human(client, nick));
            Console.WriteLine("New client added");

            TcpWorks.SendObjectOnce(new Message(nick, DateTime.Now, Message.PackType.Nick, nick), client);
            Console.WriteLine("New client name \"" + nick + "\" sended");
            UpdateRoomBroadcast();

            TcpWorks.ReciveMessagesLoop(client, DealWithRecieved);
            Console.WriteLine("Now waiting for " + nick + " messages.");

            WaitForClient();
        }

        private void ReplyToHandshake(TcpClient client)
        {
            string handshake;

            try
            {
                handshake = (string)TcpWorks.ReciveObjectOnce(client);
                Console.WriteLine("Handshake recived");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return;
            }

            TcpWorks.SendObjectOnce(handshake, client);
            Console.WriteLine("Handshake replyed");

        }

        private void DealWithRecieved(object obj, TcpClient client)
        {
            var mess = (Message)obj;
            if (mess == null)
            {
                Console.WriteLine("Null object have been recived");
                return;
            }

            var human = FindHuman(client);
            if (human == null)
            {
                Console.WriteLine("Requester is unknown");
                return;
            }

            if (mess.PacketType == Message.PackType.Nick)
            {
                TryChangeNick(mess.Body, human, client);
            }

            if (mess.PacketType == Message.PackType.Post)
            {
                if (mess.User != human.Nick) ForceChangeNick(human.Nick, human);

                
                Console.WriteLine(mess.Time.ToLongTimeString() + ' ' + mess.User + ' ' + mess.Body);
                AddMessageToQueue(mess);
            }
        }

        private void AddMessageToQueue(Message mess)
        {
            _messages.Enqueue(mess);
            NewMessegeEvent.Set();
        }

        private void TryChangeNick(string nick, Human human, TcpClient client)
        {
            if (_humans.Any(hum => hum.Nick == nick))
            {
//                AddMessageToQueue(new Message("System",
//                    DateTime.Now,
//                    Message.PackType.Post,
//                    "Nick is already occupied"));
                #region 
                TcpWorks.SendObjectOnce(new Message("System",
                    DateTime.Now,
                    Message.PackType.Post,
                    "Nick is already occupied"),
                    client);
                #endregion
            }
            else
            {
                human.Nick = nick;
//                AddMessageToQueue(new Message("System",
//                    DateTime.Now,
//                    Message.PackType.Nick,
//                    nick));
                #region 
                TcpWorks.SendObjectOnce(new Message("System",
                    DateTime.Now,
                    Message.PackType.Nick,
                    nick),
                    client);
                #endregion
                UpdateRoomBroadcast();
            }
        }

        private void UpdateRoomBroadcast()
        {
            var nicks = string.Join("%", _humans.Select(human1 => human1.Nick));
            AddMessageToQueue(new Message("System", DateTime.Now, Message.PackType.Command, "ROOM%" + nicks));
            //_messages.Enqueue(new Message("System", DateTime.Now, Message.PackType.Command, "ROOM%" + nicks));
            //NewMessegeEvent.Set();
        }

        private void ForceChangeNick(string nick, Human human)
        {
            human.Nick = nick;
            AddMessageToQueue(new Message("System",
                DateTime.Now,
                Message.PackType.Nick,
                nick));
            UpdateRoomBroadcast();
        }

        private string GenerateNick()
        {
            for (var i = 0; i < int.MaxValue; i++)
            {
                var claimantNick = "Users" + i;
                if (_humans.All(human => human.Nick != claimantNick))
                {
                    Console.WriteLine("New client named as " + claimantNick);
                    return claimantNick;
                }
            }
            return null;
        }

        private Human FindHuman(TcpClient client)
        {
            var temp = _humans.FirstOrDefault(human => human.Client == client);
            return temp;
        }
    }


}

