using System;

namespace Chat
{
    [Serializable]
    public class Message
    {
        public enum PackType
        {
            Ping,
            Post,
            Command,
            Nick
            
        };

        public PackType PacketType { get; set; }
        public DateTime Time { get; private set; }
        public string Body { get; private set; }
        public string User { get; set; }

        public Message(string user, DateTime time, PackType type, string body = "")
        {
            Body = body;
            User = user;
            Time = time;
            PacketType = type;
        }
    }
}
