using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace Chat
{
    public partial class MainWindow
    {
        private const uint Maxmesseges = 999;
        public static MainWindow Wm;
        private TCPClient _tcpClient;


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

        private string Username { get; set; }
        private bool IsConnected { get; set; }

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
            if (
                !((Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) &&
                  Keyboard.IsKeyDown(Key.Enter))) return;

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
                _tcpClient.Send(mess);
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
                     && command.Length == 3)
            {
                ButtonConnect_Click(ButtonConnect, null);
            }
            else
            {
                return false;
            }

            return true;
        }

        private void RequestNick(string nick)
        {
            Send(new Message(Username, DateTime.Now, Message.PackType.Nick, nick));
        }

        internal void ChangeNick(string nick)
        {
            Username = nick;
        }

        private void ButtonConnect_Click(object sender, RoutedEventArgs e)
        {
            if (IsConnected)
            {
                AddTextToUi("Already connected");
            }
            var temp = new Thread(delegate()
            {
                try
                {
                    _tcpClient = new TCPClient("localhost", TcpWorks.DefaultPort.ToString());
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Connection cannot be established!!!111\n" + ex.Message);
                    return;
                }

                Application.Current.Dispatcher.BeginInvoke(new Action(() => ((Button) sender).Width = 0));
                IsConnected = true;
                Application.Current.Dispatcher.BeginInvoke(new Action(() => AddTextToUi("Connected")));
                
            });
            temp.Start();
        }

        public void ChangeRoom(IEnumerable<string> commandCollec)
        {
            Wm.StackPanelUserList.Children.Clear();
            foreach (var nick in commandCollec)
            {
                var tb = new TextBlock
                {
                    Background = new SolidColorBrush(Colors.Bisque)
                };

                tb.Inlines.Add(new Run(nick)
                {
                    FontWeight = FontWeights.ExtraBold
                });

                Wm.StackPanelUserList.Children.Add(tb);
            }
        }
    }
}