using System;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;

namespace Chat
{
    public static class TcpWorks
    {
        public const int DefaultPort = 50000;

        private static object ByteArrayToObject(byte[] arrBytes)
        {
            var memStream = new MemoryStream();
            var binForm = new BinaryFormatter();
            memStream.Write(arrBytes, 0, arrBytes.Length);
            memStream.Seek(0, SeekOrigin.Begin);
            var obj = binForm.Deserialize(memStream);
            memStream.Close();

            return obj;
        }

        private static byte[] ObjectToByteArray(object obj)
        {
            if (obj == null) return null;

            var formatter = new BinaryFormatter();
            var stream = new MemoryStream();

            formatter.Serialize(stream, obj);
            var arr = stream.ToArray();

            stream.Close();

            return arr;
        }

        public static object ReciveObjectOnce(TcpClient client)
        {
            var netStream = client.GetStream();
            Console.WriteLine("Recieving data");

            var lengthBuffer = new byte[sizeof(Int32)];
            netStream.Read(lengthBuffer, 0, sizeof(Int32));
            var messageSize = BitConverter.ToInt32(lengthBuffer, 0);

            var rcvBuffer = new byte[messageSize];
            netStream.Read(rcvBuffer, 0, rcvBuffer.Length);

            return ByteArrayToObject(rcvBuffer);
        }

        public static void SendObjectOnce(object obj, TcpClient client)
        {
            var netStream = client.GetStream();

            var dataBytes = ObjectToByteArray(obj);
            var dataLengthBytes = BitConverter.GetBytes(dataBytes.Length);

            netStream.Write(dataLengthBytes, 0, dataLengthBytes.Length);
            netStream.Write(dataBytes, 0, dataBytes.Length);
        }

        private static void SafeAsyncReadFromClient(TcpClient client, byte[] bytesData, int messageSize, AsyncCallback callback)
        {
            if (client == null)
            {
                Console.WriteLine("TcpClient to read is null, man.");
                return;
            }
            var netstream = client.GetStream();

            var ipProperties = IPGlobalProperties.GetIPGlobalProperties();
            var tcpConnections = ipProperties.GetActiveTcpConnections()
                .Where(
                    x =>
                        x.LocalEndPoint.Equals(client.Client.LocalEndPoint) &&
                        x.RemoteEndPoint.Equals(client.Client.RemoteEndPoint))
                .ToArray();

            if (tcpConnections.Length > 0 && tcpConnections.First().State == TcpState.Established)
            {
                netstream.BeginRead(bytesData, 0, messageSize, callback, null);
            }
            else
            {
                client.GetStream().Close();
                //_humans.Remove(_humans.First(human => human.Client == client));
            }
        }

        public static void ReciveMessagesLoop(TcpClient client, Action<object, TcpClient> dealWithRecieved)
        {
            var messageSize = sizeof(int);
            var bytesData = new byte[messageSize];
            var isBody = false;

            AsyncCallback callback = null;
            callback = delegate
            {
                if (!isBody)
                {
                    messageSize = BitConverter.ToInt32(bytesData, 0);
                    bytesData = new byte[messageSize];
                    isBody = true;
                }
                else
                {
                    var mess = ByteArrayToObject(bytesData);
                    dealWithRecieved(mess, client);

                    messageSize = sizeof(int);
                    bytesData = new byte[messageSize];
                    isBody = false;
                }
                
                SafeAsyncReadFromClient(client, bytesData, messageSize, callback);
            };

            
            SafeAsyncReadFromClient(client, bytesData, messageSize, callback);
        }
    }
}
