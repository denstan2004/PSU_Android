using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OrderTrack.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Timers;
using System.Windows.Input;
using Utils;


namespace psu_flutter
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
            NavigationPage.SetHasNavigationBar(this, false);
            EnsureAppSettingsFileExists();

            var config = LoadConfiguration();
            var ipAddress = config.ip;
            var port = config.port;

            _socketClient = new SocketClient(ipAddress, port);
            _socketClient.OnMessageReceived += HandleServerMessage; // Підписуємося на подію
            _ = _socketClient.ConnectAsync(); // Асинхронне підключення

            Orders = new ObservableCollection<Order>();
            BindingContext = this; // Встановлюємо BindingContext


            _timer = new System.Timers.Timer(45000);
            _timer.AutoReset = true;
            _timer.Elapsed += OnTimerElapsed;
            _timer.Start();
        }

        #region Fields

        public string IP;
        public string Port;
        public bool IsNotConnected 
        {
            get
            {
                return !_socketClient.IsConnected;
      
            }
               
                }
        public bool MenuVisibility { get; set; }
        public bool IsOrdersEmpty
        {
            get { return !Orders.Any() && !IsNotConnected; }
        }
        private  SocketClient _socketClient;
        private System.Timers.Timer _timer;
        public ObservableCollection<Order> Orders { get; set; }
        #endregion

        #region appsettings Methods
        private void EnsureAppSettingsFileExists()
        {
            var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "appsettings.json");

            if (!File.Exists(filePath))
            {
                try
                {
                    // Стандартний JSON для файлу налаштувань
                    var defaultConfig = new
                    {
                        Socket = new
                        {
                            IpAddress = "192.168.88.248", // Стандартна IP-адреса
                            PortAPI = 8080          // Стандартний порт
                        }
                    };

                    // Серіалізуємо у формат JSON із форматуванням
                    var json = Newtonsoft.Json.JsonConvert.SerializeObject(defaultConfig, Formatting.Indented);

                    // Записуємо JSON у файл
                    File.WriteAllText(filePath, json);

                    Console.WriteLine($"Файл налаштувань створено за замовчуванням: {filePath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Помилка створення файлу налаштувань: {ex.Message}");
                }
            }
        }
        public Config LoadConfiguration()
        {
            var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "appsettings.json");

            if (File.Exists(filePath))
            {
                try
                {
                    // Зчитуємо вміст JSON-файлу
                    var json = File.ReadAllText(filePath);

                    // Парсимо JSON у JObject
                    var jsonObj = JObject.Parse(json);

                    // Дістаємо секцію "Socket"
                    var socketSection = jsonObj["Socket"];

                    if (socketSection != null)
                    {
                        // Створюємо об'єкт Config і заповнюємо його значеннями
                        var config = new Config
                        {
                            ip = socketSection["IpAddress"]?.ToString(),
                            port = socketSection["PortAPI"]?.ToObject<int>() ?? 0 // Перетворення в int
                        };

                        return config;
                    }
                    else
                    {
                        Console.WriteLine("Секція 'Socket' не знайдена в конфігурації.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Помилка при зчитуванні конфігурації: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("Файл конфігурації не знайдений.");
            }

            // Якщо файл не знайдений або помилка, повертаємо порожню конфігурацію
            return new Config();
        }


        public void UpdateIpAddress(string newIp ,string newPort)
        {
            var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "appsettings.json");

            if (File.Exists(filePath))
            {
                try
                {
                    // Зчитуємо існуючий JSON-файл
                    var json = File.ReadAllText(filePath);

                    // Парсимо JSON у JObject
                    var jsonObj = JObject.Parse(json);

                    // Оновлюємо секцію "Socket" з новим IP
                    var socketSection = jsonObj["Socket"];
                    if (socketSection != null)
                    {
                        socketSection["PortAPI"] =newPort;
                        socketSection["IpAddress"] = newIp;

                        // Серіалізуємо об'єкт назад у JSON із відформатованим виглядом
                        var updatedJson = jsonObj.ToString(Newtonsoft.Json.Formatting.Indented);

                        // Записуємо оновлений JSON у файл
                        File.WriteAllText(filePath, updatedJson);
                        _socketClient = new SocketClient(newIp, Int32.Parse(newPort));
                        Console.WriteLine("IP-адресу успішно оновлено.");
                    }
                    else
                    {
                        Console.WriteLine("Секція 'Socket' не знайдена у файлі конфігурації.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Помилка при оновленні IP-адреси: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("Файл конфігурації не знайдений.");
            }
            LoadConfiguration();
        }

        #endregion

        #region Timer
        private void HandleServerMessage(string message)
        {
            SendGetAllOrdersRequest();
        }
        private async void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {

                await SendGetAllOrdersRequest();
                OnPropertyChanged(nameof(IsNotConnected));

            });
        }

        private async Task SendGetAllOrdersRequest()
        {
            try
            {
                var command = new
                {
                    Command = "GetActiveOrders"
                };

                string jsonRequest = System.Text.Json.JsonSerializer.Serialize(command);
                var timer = new Stopwatch();
                timer.Start();
              
                string response = await _socketClient.SendMessageAsync(jsonRequest);
               
                string pDateFormatString = null;
                JsonSerializerSettings JsonSettings = new JsonSerializerSettings()
                {
                    DateFormatString = "dd.MM.yyyy",
                    FloatParseHandling = FloatParseHandling.Decimal,
                    NullValueHandling = NullValueHandling.Ignore,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                };

                JsonSerializerSettings settings = pDateFormatString == null
                    ? JsonSettings
                    : new JsonSerializerSettings()
                    {
                        DateFormatString = pDateFormatString,
                        FloatParseHandling = FloatParseHandling.Decimal,
                        NullValueHandling = NullValueHandling.Ignore,
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                    };

                // Десеріалізація виконується у фоновому потоці
                var status = JsonConvert.DeserializeObject<Status<IEnumerable<Order>>>(response, settings);

                // Оновлюємо ObservableCollection через проміжну колекцію   
                if (status?.Data != null)
                { 
                    var ordersList = status.Data.ToList();
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        Orders.Clear(); // Масове очищення
                        foreach (var order in ordersList)
                        {
                            // Збираємо всі товари, які потрібно видалити
                            var waresToRemove = new List<OrderWares>();

                            foreach (var ware in order.Wares)
                            {
                                if (ware.ReceiptLinks.Any())
                                {
                                    foreach (var link in ware.ReceiptLinks)
                                    {
                                        foreach (var ware2 in order.Wares)
                                        {
                                            if (link.CodeWares == ware2.CodeWares)
                                            {
                                                waresToRemove.Add(ware2);
                                            }
                                        }
                                    }
                                }
                            }

                            // Видаляємо товари після завершення ітерації
                            order.Wares = order.Wares.Except(waresToRemove).ToList();

                            Orders.Add(order);

                        }   
                    });
                    timer.Stop();
                    TimeSpan timeTaken = timer.Elapsed;
                    OnPropertyChanged(nameof (IsOrdersEmpty));
                }
            }
            catch (Exception ex)
            {

            }
        }



        protected override void OnDisappearing()
        {
            _socketClient.Disconnect();

            base.OnDisappearing();

            // Зупиняємо таймер, коли сторінка закривається
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Dispose();
            }
        }
        #endregion 

        #region UiPart
        private async void Button_Clicked(object sender, EventArgs e)
        {
            var timer = new Stopwatch();
            timer.Start();
           
            try
            {
                if (sender is Button button && button.CommandParameter is Order currentOrder)
                {
                    
                    if (currentOrder.Status == eStatus.Waiting)
                    {
                        currentOrder.Status = eStatus.Preparing;
                        var order = Orders.FirstOrDefault(e => e.Id == currentOrder.Id);
                        if (order != null)
                        {
                            order.Status = currentOrder.Status;
                        }
                    }
                    else if (currentOrder.Status == eStatus.Preparing)
                    {
                        Orders = new ObservableCollection<Order>(Orders.Where(e => e.Id != currentOrder.Id));

                         currentOrder.Status = eStatus.Ready;
                         
                    }
                   


                   /* MainThread.BeginInvokeOnMainThread(() =>
                    {
                        OnPropertyChanged(nameof(Orders));
                    });*/
                    timer.Stop();
                    TimeSpan timeTaken = timer.Elapsed;
                    string pDateFormatString = null;
                    JsonSerializerSettings settings = new JsonSerializerSettings()
                    {
                        DateFormatString = "dd.MM.yyyy",
                        FloatParseHandling = FloatParseHandling.Decimal,
                        NullValueHandling = NullValueHandling.Ignore,
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                    };
                
                          UpdateModel updateModel = new UpdateModel(currentOrder.Status, currentOrder.Id);
                    
                    var command = new
                    {
                        Command = "ChangeOrderState",
                        Data = updateModel
                    };


                  //  OnPropertyChanged(nameof(Orders));
                    string jsonRequest = JsonConvert.SerializeObject(command);
                    string response = await _socketClient.SendMessageAsync(jsonRequest);
                    Status<Order> returnOrder = JsonConvert.DeserializeObject<Status<Order>>(response, settings);

                    if (returnOrder.State == 0)
                    {
                        SendGetAllOrdersRequest();
                       
                    }
                   
                }
            }
            catch (Exception ex)
            {
                Console.Write(ex.ToString());
            }
        }

        private async void Button_Clicked_1(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is Order currentOrder)
            {
                var timer = new Stopwatch();
                timer.Start();
                if (currentOrder.Status == eStatus.Preparing)
                {
                    currentOrder.Status = eStatus.Waiting;
                    var order = Orders.FirstOrDefault(e => e.Id == currentOrder.Id);
                    if (order != null)
                    {
                        order.Status = currentOrder.Status;
                    }
                }
              /*  MainThread.BeginInvokeOnMainThread(() =>
                {
                    OnPropertyChanged(nameof(Orders));
                });*/
                timer.Stop();
                TimeSpan timeTaken = timer.Elapsed;
                UpdateModel updateModel = new UpdateModel(eStatus.Waiting, currentOrder.Id);
                var command = new
                {
                    Command = "ChangeOrderState",
                    Data = updateModel
                };
                string pDateFormatString = null;

                JsonSerializerSettings settings = new JsonSerializerSettings()
                {
                    DateFormatString = "dd.MM.yyyy",
                    FloatParseHandling = FloatParseHandling.Decimal,
                    NullValueHandling = NullValueHandling.Ignore,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                };
                
                string jsonRequest = JsonConvert.SerializeObject(command);
                string response = await _socketClient.SendMessageAsync(jsonRequest);
                Status<Order> returnoOrder = JsonConvert.DeserializeObject<Status<Order>>(response, settings);

                if (returnoOrder.State == 0)
                {
                    SendGetAllOrdersRequest();

                  /*  var search = Orders.FirstOrDefault(item => item.Id == currentOrder.Id);
                    if (search != null)
                    {
                        search.Status = currentOrder.Status;
                        OnPropertyChanged(nameof(search));

                    }
                    OnPropertyChanged(nameof(Orders));*/
                }

            }


        }
        
        private void Button_Clicked_2(object sender, EventArgs e)
        {
            MenuVisibility = !MenuVisibility;
            OnPropertyChanged(nameof(MenuVisibility));
        }
        private void Button_Clicked_3(object sender, EventArgs e)
        {
            UpdateIpAddress(IP,Port);
            MenuVisibility = !MenuVisibility;
            OnPropertyChanged(nameof(MenuVisibility));
        }
        private void OnEntryCompleted(object sender, EventArgs e)
        {
            if (sender is Entry entry)
            {
                string enteredText = entry.Text;
                OnIpChange(enteredText); 
            }
        }
        private void OnEntryCompleted1(object sender, EventArgs e)
        {
            if (sender is Entry entry)
            {
                string enteredText = entry.Text;
                OnPortChange(enteredText);
            }
        }

        private void OnIpChange(string enteredText)
        {
            IP = enteredText; // Зберігаємо введений IP
        }
        private void OnPortChange(string enteredText)
        {
            Port = enteredText; // Зберігаємо введений IP
        }


        #endregion

        #region SocketPart
        public class SocketClient
        {
            private string _serverAddress;
            private int _port;
            public bool IsConnected {  get; private set; }
            private TcpClient _client;
            private CancellationTokenSource _cancellationTokenSource;
            public event Action<string> OnMessageReceived; // Подія для обробки отриманих повідомлень

            public SocketClient(string serverAddress, int port)
            {
                _serverAddress = serverAddress;
                _port = port;
                _cancellationTokenSource = new CancellationTokenSource();
            }
            public async Task ConnectAsync()
            {
                try
                {
                    _client = new TcpClient();
                    await _client.ConnectAsync(_serverAddress, _port);
                    Console.WriteLine("Клієнт підключений до сервера.");
                    IsConnected = true;
                    _ = ListenForMessagesAsync(_cancellationTokenSource.Token); // Запускаємо слухання
                }
                catch (Exception ex)
                {
                    IsConnected = false;
                    Console.WriteLine($"Помилка підключення до сервера: {ex.Message}");
                }
            }

            public async Task<string> SendMessageAsync(string message)
            {
                try
                {
                

                    using var client = new TcpClient();
                    await client.ConnectAsync(_serverAddress, _port);
                    IsConnected = true;
                    
                    using var stream = client.GetStream();
                    byte[] data = Encoding.UTF8.GetBytes(message);
                    await stream.WriteAsync(data, 0, data.Length);

                    return await ReceiveStreamAsync(stream);
                  
                }
                catch (Exception ex)
                {
                    IsConnected = false;

                    return $"Error: {ex.Message}";
                }
            }


            private async Task<string> ReceiveStreamAsync(NetworkStream stream)
            {
                var buffer = new byte[4096];
                var sb = new StringBuilder();

                while (true)
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                    {
                        break;
                    }

                    sb.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));

                    if (IsJsonComplete(sb.ToString()))
                    {
                        break;
                    }
                }

                return sb.ToString();
            }


            private async Task ListenForMessagesAsync(CancellationToken cancellationToken)
            {
                try
                {
                    using var stream = _client.GetStream();
                    var buffer = new byte[4096];

                    while (!cancellationToken.IsCancellationRequested)
                    {
                        if (stream.DataAvailable)
                        {
                            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                            if (bytesRead > 0)
                            {
                                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                                OnMessageReceived?.Invoke(message); // Викликаємо подію для обробки
                                Console.WriteLine($"Отримано повідомлення: {message}");
                            }
                        }
                        await Task.Delay(100, cancellationToken); // Невелика затримка для економії ресурсів
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Помилка при отриманні повідомлень: {ex.Message}");
                }
            }

            private bool IsJsonComplete(string json)
            {
                try
                {
                    JToken.Parse(json);
                    return true;
                }
                catch (JsonReaderException)
                {
                    return false;
                }
            }

            public void Disconnect()
            {
                _cancellationTokenSource.Cancel();
                _client?.Close();
                Console.WriteLine("Клієнт відключений.");
            }
        }



    }
    #endregion
    public class Config
    {
        public int port { get; set; }
        public string ip
        {
            get; set;
        }
    }

}




