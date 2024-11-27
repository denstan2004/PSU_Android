using ModelMID;
using System.ComponentModel;
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
        
        public eStatus Status { get; set; }
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

    public class OrderWares
    {
        public int IdWorkplace { get; set; }
        public int CodePeriod { get; set; }
        public int CodeReceipt { get; set; }
        public int CodeWares { get; set; }
        public string NameWares { get; set; }
        public decimal Quantity { get; set; }
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
        private IEnumerable<OrderReceiptLink> receiptLinks( ReceiptWares receiptWares)
        {
            List<OrderReceiptLink> orderReceiptLinks = new List<OrderReceiptLink>();
            foreach (GW w in receiptWares.WaresLink)
            {
                if (true)//(w.IsSelected == true) //ПЕРЕРОБИТИ
                {
                    orderReceiptLinks.Add(new OrderReceiptLink(w,receiptWares));
                }
            }

            return orderReceiptLinks;
        }

    }

    public class OrderReceiptLink
    {
        public int IdWorkplace { get; set; }
        public string Name { get; set; }
        public int CodePeriod { get; set; }
        public int CodeReceipt { get; set; }
        public int CodeWares { get; set; }
        public decimal Quantity { get; set; }
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
            Quantity = 1m;//Доробити
            Sort = receiptWares.Sort;
        }
        public OrderReceiptLink() { }

    }


}
