using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace Chat
{
    public partial class MainWindow
    {
        public static MainWindow Wm;

        public string Username { get; private set; }
        public bool IsConnected { get; set; }
        public const uint Maxmesseges = 999;
        public TCPClient TcpClient;


        public MainWindow()
        {
            Wm = this;
            IsConnected = false;
            Width = SystemParameters.PrimaryScreenWidth - 400;
            Height = SystemParameters.PrimaryScreenHeight - 200;
            Username = "Username";

            InitializeComponent();
            //Closed += (e, o) => TcpClient.Client.Close();

        }

        internal void AddTextToUi(string text)
        {
            var mess = new Message("System", DateTime.Now, Message.PackType.Post, text);
            AddPostToUi(mess);
        }

        internal void AddPostToUi(Message mess)
        {
            var tb = new TextBlock
            {
                //Margin = new Thickness(0, -1, 0, -1),
                Background = new SolidColorBrush(Colors.LightGreen)
            };

            tb.Inlines.Add(new Run("[" + mess.Time.ToLongTimeString() + "]")
            {
                FontWeight = FontWeights.Bold
            });

            tb.Inlines.Add(new Run(" " + mess.User)
            {
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.Blue
            });

            tb.Inlines.Add(new Run(": "));

            tb.Inlines.Add(new Run(mess.Body));

            Wm.StackPanelMessages.Children.Add(tb);

            if (Wm.StackPanelMessages.Children.Count > Maxmesseges)
            {
                Wm.StackPanelMessages.Children.RemoveAt(0);
            }
        }

        private void TextBoxPostEdit_KeyDown(object sender, KeyEventArgs e)
        {
            if (!((Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) && Keyboard.IsKeyDown(Key.Enter))) return;

            var text = TextBoxPostEdit.Text;

            if (!ParseCommand(text) && IsConnected)
            {
                var mess = new Message(Username, DateTime.Now, Message.PackType.Post, text);
                Send(mess);
            }
            
            TextBoxPostEdit.Text = "";
        }

        private void Send(Message mess)
        {
            if (IsConnected && mess != null)
            {
                TcpClient.Send(mess);
            }
        }

        private bool ParseCommand(string text)
        {
            text = text.ToLower().Trim();
            if (text.IndexOf('/') != 0)
            {
                return false;
            }

            var command = text.Remove(0, 1).Split(' ');

            if (command[0] == "nick" && command.Length == 2)
            {
                RequestNick(command[1]);
            }
            else if (command[0] == "server"
                     && command.Length == 3
                     && Connect(command[1], command[2]))
            {
                ButtonConnect.Width = 0;
            }
            else
            {
                return false;
            }

            return true;
        }

        internal void RequestNick(string nick)
        {
            Send(new Message(Username, DateTime.Now, Message.PackType.Nick, nick));
        }

        internal void ChangeNick(string nick)
        {
            Username = nick;
        }

        private void ButtonConnect_Click(object sender, RoutedEventArgs e)
        {
            if (Connect("localhost", "50000"))
            {
                ((Button)sender).Width = 0;
            }
        }

        private bool Connect(string address, string port)
        {
            if (IsConnected)
            {
                AddTextToUi("Already connected");
                return false;
            }

            try
            {
                TcpClient = new TCPClient(address, port);
            }
            catch (Exception e)
            {
                MessageBox.Show("Connection cannot be established!!!111\n" + e.Message);
                return false;
            }

            if (TcpClient == null || TcpClient.Client == null) return false;
            IsConnected = true;
            AddTextToUi("Connected");

            return true;
        }


    }
}
