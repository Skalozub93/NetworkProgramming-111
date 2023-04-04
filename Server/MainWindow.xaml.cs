using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Server
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Socket? listenSocket; // слушающий сокет - постоянно активный при вкл сервере
        public MainWindow()
        {
            InitializeComponent();
        }

        private void StartServer_Click(object sender, RoutedEventArgs e)
        {
                IPEndPoint endpoint; 
            try
            {
                IPAddress ip =
                    IPAddress.Parse(
                        serverIp.Text);
                int port =
                    Convert.ToInt32(
                        serverPort.Text);
                endpoint =
                    new(ip, port);
            }
            catch
            {
                MessageBox.Show("Check start network parameters");
                return;
            }
            listenSocket = new(
                AddressFamily.InterNetwork,
                SocketType.Stream,
                ProtocolType.Tcp);
            new Thread(StartServerMethod).Start(endpoint);
        }

        private void StartServerMethod(object? param)
        {
            if (listenSocket is null) return;
            IPEndPoint? endpoint = param as IPEndPoint;
            if(endpoint is null) return;

            try
            {
                listenSocket.Bind(endpoint);
                listenSocket.Listen(100);
                Dispatcher.Invoke(() =>            // Добавляем  к логам информацию
                    serverLogs.Text +=             // о старте сервера. Используем Dispatcher
                    "Server started\n");
                byte[] buf = new byte[1024];
                while(true)
                {
                    Socket socket =
                        listenSocket.Accept();
                    // Начинаем прием данных
                    StringBuilder sb = new StringBuilder();
                    do
                    {
                        int n = 
                            socket.Receive(buf);
                        sb.Append(
                            System.Text.Encoding.UTF8
                            .GetString(buf, 0, n));
                    } while (socket.Available > 0);


                    String str = sb.ToString();    // собираем все фрагменты в одну строку
                    Dispatcher.Invoke(() =>        // Добавляем полученные данные к логам
                    serverLogs.Text +=             // сервера. Используем Dispatcher
                    str + "\n");                   // для доступа к UI

                    // Отправляем клиенту ответ = отчет о получении сообщения
                    str = "Received at" +          // В обратном порядке - 
                        DateTime.Now;              // сначала строка
                    socket.Send(                   // затем переводим в байты
                        Encoding.UTF8              // по заданной кодировке
                        .GetBytes(str));           // и отправялем в сокет


                   
                    socket.Shutdown(               // Закрываем соединение - 
                        SocketShutdown.Both);      // отключаем сокет с уведомлениемм клиента
                    socket.Close();                // Освобождаем  ресурс
                }
            }
            catch(Exception ex)
            {
                Dispatcher.Invoke(() =>            // Логируем исключение
                    serverLogs.Text +=             // и уведомляем об остановке
                    "Server stopped "              // сервера
                    + ex.Message + "\n");
            }
           
        }

        private void StopServer_Click(object sender, RoutedEventArgs e)
        {
            // Остановить бесконечный цикл можно только выбросом исключения
            listenSocket?.Close();
        }
    }
}
