using ModelMID;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Utils;

namespace OrderTrack.Models
{
    public class Order : INotifyPropertyChanged
    {
        public bool IsButtonVisible 
           {
            get{
                return this.Status==eStatus.Preparing ? true : false;
            }
           }
        private int _OrderNumber;

        public event PropertyChangedEventHandler? PropertyChanged;

        public int OrderNumber { get => Id < 100 ? Id : Id % 100; set => _OrderNumber = value; }


        private eStatus _status;
        public eStatus Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged();
                   
                }
            }
        }
        [JsonIgnore]
        public string StatusText
        {
            get
            {
                return Status switch
                {
                    eStatus.Waiting => "Почати готувати",
                    eStatus.Preparing => "Закінчити готувати",
                    eStatus.Ready => "Готово!",
                    _ => "Невідомий статус"
                };
            }
        }
        public string TranslatedStatus { get => Status.GetDescription(); }
        public string StatusIcon { get => $"Images/{Status}.png"; }
        public Order()
        {
            DateCreate = DateTime.Now;
        }
        public int Id { get; set; }
        public int IdWorkplace { get; set; }
        public int CodePeriod { get; set; }
        public int CodeReceipt { get; set; }
        public IEnumerable<OrderWares> Wares { get; set; }
        public DateTime DateCreate { get; set; }
        public DateTime DateStart { get; set; }
        public DateTime DateEnd { get; set; }
        public string Type { get; set; }
        public string JSON { get; set; }
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
   
    public enum eStatus
    {
        [Description("Очікує")]
        Waiting,
        [Description("Готується")]
        Preparing,
        [Description("Готово!")]
        Ready,
    }

    public class OrderWares : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string _nameWares;
        public string NameWares
        {
            get => _nameWares;
            set
            {
                if (_nameWares != value)
                {
                    _nameWares = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(NameQuantity)); // Повідомляємо про зміну NameQuantity
                }
            }
        }

        private decimal _quantity;
        public decimal Quantity
        {
            get => _quantity;
            set
            {
                if (_quantity != value)
                {
                    _quantity = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(NameQuantity)); // Повідомляємо про зміну NameQuantity
                }
            }
        }

        public string NameQuantity => $"{NameWares} {Quantity.ToString().Trim(',')[0]} шт.";

        public int IdWorkplace { get; set; }
        public int CodePeriod { get; set; }
        public int CodeReceipt { get; set; }
        public int CodeWares { get; set; }
        public int Sort { get; set; }
        public DateTime DateCreate { get; set; }
        public int UserCreate { get; set; }
        public IEnumerable<OrderReceiptLink> ReceiptLinks { get; set; }

        public OrderWares(ReceiptWares receiptWares)
        {
            this.CodeWares = receiptWares.CodeWares;
            this.NameWares = receiptWares.NameWares;
            this.UserCreate = receiptWares.UserCreate;
            this.CodePeriod = receiptWares.CodePeriod;
            this.CodeReceipt = receiptWares.CodeReceipt;
            this.IdWorkplace = receiptWares.IdWorkplace;
            this.Sort = receiptWares.Sort;
            this.Quantity = receiptWares.Quantity;
            this.DateCreate = DateTime.Now;
            this.UserCreate = receiptWares.UserCreate;
            this.ReceiptLinks = receiptLinks(receiptWares);
        }

        public OrderWares() { }

        private IEnumerable<OrderReceiptLink> receiptLinks(ReceiptWares receiptWares)
        {
            List<OrderReceiptLink> orderReceiptLinks = new List<OrderReceiptLink>();
            foreach (GW w in receiptWares.WaresLink)
            {
                if (true) // ПЕРЕРОБИТИ умову
                {
                    orderReceiptLinks.Add(new OrderReceiptLink(w, receiptWares));
                }
            }

            return orderReceiptLinks;
        }
    }

    public class OrderReceiptLink : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string _name;
        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(NameQuantity)); // Повідомляємо про зміну NameQuantity
                }
            }
        }

        private decimal _quantity;
        public decimal Quantity
        {
            get => _quantity;
            set
            {
                if (_quantity != value)
                {
                    _quantity = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(NameQuantity)); // Повідомляємо про зміну NameQuantity
                }
            }
        }

        public string NameQuantity => $"{Name} {Quantity} шт.";

        public int IdWorkplace { get; set; }
        public int CodePeriod { get; set; }
        public int CodeReceipt { get; set; }
        public int CodeWares { get; set; }
        public int CodeWaresTo { get; set; }
        public int Sort { get; set; }

        public OrderReceiptLink(GW waresLink, ReceiptWares receiptWares)
        {
            IdWorkplace = receiptWares.IdWorkplace;
            CodePeriod = receiptWares.CodePeriod;
            CodeReceipt = receiptWares.CodeReceipt;
            CodeWaresTo = receiptWares.CodeWares;
            CodeWares = waresLink.Code;
            Name = waresLink.Name;
            Quantity = 1m; // Доробити
            Sort = receiptWares.Sort;
        }

        public OrderReceiptLink() { }
    }


}
