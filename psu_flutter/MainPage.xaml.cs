using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OrderTrack.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
            var config = LoadConfiguration();
            var ipAddress = config.ip;
            var port = config.port;
            _socketClient = new SocketClient(ipAddress, port);
            Orders = new ObservableCollection<Order>();
            BindingContext = this; // Встановлюємо BindingContext
            _timer = new System.Timers.Timer(10000);
            _timer.AutoReset = true;
            _timer.Elapsed += OnTimerElapsed;
            _timer.Start();
        }

        #region Fields

        public string IP;
        public bool MenuVisibility { get; set; }
        public bool IsOrdersEmpty
        {
            get { return !Orders.Any(); }
        }
        private readonly SocketClient _socketClient;
        private System.Timers.Timer _timer;
        public ObservableCollection<Order> Orders { get; set; }
        #endregion

        #region appsettings Methods
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


        public void UpdateIpAddress(string newIp)
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
                        socketSection["IpAddress"] = newIp;

                        // Серіалізуємо об'єкт назад у JSON із відформатованим виглядом
                        var updatedJson = jsonObj.ToString(Newtonsoft.Json.Formatting.Indented);

                        // Записуємо оновлений JSON у файл
                        File.WriteAllText(filePath, updatedJson);

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

        private async void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await SendGetAllOrdersRequest();
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
                            Orders.Add(order); // Масове додавання
                        }
                    });
                    OnPropertyChanged(nameof (IsOrdersEmpty));
                }
            }
            catch (Exception ex)
            {

            }
        }



        protected override void OnDisappearing()
        {
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
            try
            {
                if (sender is Button button && button.CommandParameter is Order currentOrder)
                {
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
                    if (currentOrder.Status == eStatus.Waiting)
                    {
                        currentOrder.Status = eStatus.Preparing;
                    }
                    else if (currentOrder.Status == eStatus.Preparing)
                    {
                        currentOrder.Status = eStatus.Ready;
                    }
                    OnPropertyChanged(nameof(Orders));
                    UpdateModel updateModel = new UpdateModel(currentOrder.Status, currentOrder.Id);
                    var command = new
                    {
                        Command = "ChangeOrderState",
                        Data = updateModel
                    };


                    OnPropertyChanged(nameof(Orders));
                    string jsonRequest = JsonConvert.SerializeObject(command);
                    string response = await _socketClient.SendMessageAsync(jsonRequest);
                    Status<Order> order = JsonConvert.DeserializeObject<Status<Order>>(response, settings);

                    if (order.State == 0)
                    {
                        var search = Orders.FirstOrDefault(item => item.Id == currentOrder.Id);
                        if (search != null)
                        {
                            if (currentOrder.Status == eStatus.Ready)
                            {
                                Orders.Remove(search);
                            }
                            else
                            {
                                search.Status = currentOrder.Status;
                            }
                        }
                        OnPropertyChanged(nameof(Orders));
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
                UpdateModel updateModel = new UpdateModel(eStatus.Waiting, currentOrder.Id);
                var command = new
                {
                    Command = "ChangeOrderState",
                    Data = updateModel
                };
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
                string jsonRequest = JsonConvert.SerializeObject(command);
                string response = await _socketClient.SendMessageAsync(jsonRequest);
                Status<Order> order = JsonConvert.DeserializeObject<Status<Order>>(response, settings);

                if (order.State == 0)
                {
                    var search = Orders.FirstOrDefault(item => item.Id == currentOrder.Id);
                    if (search != null)
                    {
                        search.Status = currentOrder.Status;
                        OnPropertyChanged(nameof(search));

                    }
                    OnPropertyChanged(nameof(Orders));
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
            UpdateIpAddress(IP);
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

        private void OnIpChange(string enteredText)
        {
            IP = enteredText; // Зберігаємо введений IP
        }

        #endregion

        #region SocketPart
        public class SocketClient
        {
            public string _serverAddress;
            public int _port;

            public SocketClient(string serverAddress, int port)
            {
                _serverAddress = serverAddress;
                _port = port;
            }

            public async Task<string> SendMessageAsync(string message)
            {
                try
                {
                    using var client = new TcpClient();
                    await client.ConnectAsync(_serverAddress, _port);

                    using var stream = client.GetStream();
                    byte[] data = Encoding.UTF8.GetBytes(message);
                    await stream.WriteAsync(data, 0, data.Length);

                    return await ReceiveStreamAsync(stream);
                }
                catch (Exception ex)
                {
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
        } // <-- Закриваюча дужка для SocketClient

      
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




