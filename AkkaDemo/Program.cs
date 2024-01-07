using Akka.Actor;

namespace AkkaDemo
{
    public class Product
    {
        public Product(string name, int price)
        {
            Name = name;
            Price = price;
        }

        public string Name { get; set; }
        public int Price { get; set; }
    }

    public class InprogressProduct
    {
        public InprogressProduct(int tableNumber, string productName)
        {
            TableNumber = tableNumber;
            ProductName = productName;
        }

        public int TableNumber { get; set; }
        public string ProductName { get; set; }
    }

    public class Bill
    {
        public Bill(int tableNumber, Product product, DateTime time)
        {
            TableNumber = tableNumber;
            Product = product;
            Time = time;
        }

        public int TableNumber { get; set; }
        public Product Product { get; set; }
        public DateTime Time { get; set; }
    }

    public class CreateOrderMessage
    {
        public CreateOrderMessage(int tableNumber, Product product)
        {
            TableNumber = tableNumber;
            Product = product;
        }

        public int TableNumber { get; private set; }
        public Product Product { get; private set; }
    }

    public class ProcessProductMessage
    {
        public ProcessProductMessage(int tableNumber, string productName)
        {
            TableNumber = tableNumber;
            ProductName = productName;
        }

        public int TableNumber { get; private set; }
        public string ProductName { get; private set; }
    }

    public class ProductCompletedMessage
    {
        public ProductCompletedMessage(int tableNumber)
        {
            TableNumber = tableNumber;
        }

        public int TableNumber { get; private set; }
    }

    public class CreateBillMessage
    {
        public CreateBillMessage(int tableNumber, Product product)
        {
            TableNumber = tableNumber;
            Product = product;
        }

        public int TableNumber { get; private set; }
        public Product Product { get; private set; }
    }

    public class DisplayInprogressProductsMessage { }
    public class DisplayAllBillsMessage { }


    public class OrderActor : ReceiveActor
    {
        public OrderActor()
        {
            Receive<CreateOrderMessage>(msg =>
            {
                var productProcessingActor = Context.ActorSelection("/user/ProductProcessingActor");
                var billingActor = Context.ActorSelection("/user/BillingActor");

                productProcessingActor.Tell(new ProcessProductMessage(msg.TableNumber, msg.Product.Name));
                billingActor.Tell(new CreateBillMessage(msg.TableNumber, msg.Product));
            });
        }
    }

    public class BillingActor : ReceiveActor
    {
        private List<Bill> bills = new List<Bill>() { };

        public BillingActor()
        {
            Receive<CreateBillMessage>(msg =>
            {
                var bill = new Bill(msg.TableNumber, msg.Product, DateTime.Now);
                bills.Add(bill);
                Console.WriteLine("{0,10}   {1,-15}{2,7}   {3,-15}", "Table", "Product", "Price", "Time");
                Console.WriteLine("------------------------------------------------------------------");
                DisplayBill(bill);
            });

            Receive<DisplayAllBillsMessage>(msg =>
            {
                Console.WriteLine("{0,10}   {1,-15}{2,7}   {3,-15}", "Table", "Product", "Price", "Time");
                Console.WriteLine("------------------------------------------------------------------");
                bills.ForEach(bill =>
                {
                    DisplayBill(bill);
                });
            });
        }

        private void DisplayBill(Bill bill)
        {
            Console.WriteLine("{0,10}   {1,-15}{2,7}   {3,-15}", bill.TableNumber, bill.Product.Name, bill.Product.Price, bill.Time.ToString());
        }
    }

    public class ProductProcessingActor : ReceiveActor
    {
        private List<InprogressProduct> inProgressProducts = new List<InprogressProduct>() { };

        public ProductProcessingActor()
        {
            Receive<ProcessProductMessage>(msg =>
            {
                inProgressProducts.Add(new InprogressProduct(msg.TableNumber, msg.ProductName));
            });

            Receive<ProductCompletedMessage>(msg =>
            {
                InprogressProduct removeProduct = inProgressProducts.SingleOrDefault(p => p.TableNumber == msg.TableNumber);
                if (removeProduct != null)
                {
                    inProgressProducts.Remove(removeProduct);
                }
                else
                {
                    Console.WriteLine("[Complete product] Table number not found!");
                }
                
            });

            Receive<DisplayInprogressProductsMessage>(msg =>
            {
                Console.WriteLine("{0,10}   {1,-20}", "Table", "Product");
                Console.WriteLine("------------------------------------------------------------------");
                inProgressProducts.ForEach(p =>
                {
                    Console.WriteLine("{0,10}   {1,-20}", p.TableNumber, p.ProductName);
                });

            });
        }
    }

    public class ManagerActor : ReceiveActor
    {
        ActorSelection orderActor;
        ActorSelection billingActor;
        ActorSelection productProcessingActor;
        public ManagerActor()
        {
            orderActor = Context.ActorSelection("/user/OrderActor");
            billingActor = Context.ActorSelection("/user/BillingActor");
            productProcessingActor = Context.ActorSelection("/user/ProductProcessingActor");

            Receive<CreateOrderMessage>(msg =>
            {
                orderActor.Tell(msg);
            });

            Receive<DisplayAllBillsMessage>(msg =>
            {
                billingActor.Tell(msg);
            });

            Receive<ProductCompletedMessage>(msg =>
            {
                productProcessingActor.Tell(msg);
            });

            Receive<DisplayInprogressProductsMessage>(msg =>
            {
                productProcessingActor.Tell(msg);
            });
        }
    }

    class Program
    {
        static void Main()
        {
            var system = ActorSystem.Create("CafeSystem");
            IActorRef managerActor = system.ActorOf(Props.Create(() => new ManagerActor()), "ManagerActor");

            IActorRef orderActor = system.ActorOf(Props.Create(() => new OrderActor()), "OrderActor");
            IActorRef billingActor = system.ActorOf(Props.Create(() => new BillingActor()), "BillingActor");
            IActorRef productProcessingActor = system.ActorOf(Props.Create(() => new ProductProcessingActor()), "ProductProcessingActor");

            Console.ReadLine();
            managerActor.Tell(new CreateOrderMessage(1, new Product("Black Coffee", 20)));
            Console.ReadLine();
            managerActor.Tell(new CreateOrderMessage(2, new Product("White Coffee", 30)));
            Console.ReadLine();
            managerActor.Tell(new CreateOrderMessage(3, new Product("Green Tea", 25)));
            Console.ReadLine();

            managerActor.Tell(new DisplayInprogressProductsMessage());
            Console.ReadLine();
            managerActor.Tell(new ProductCompletedMessage(2));
            managerActor.Tell(new DisplayInprogressProductsMessage());
            Console.ReadLine();
            managerActor.Tell(new ProductCompletedMessage(1));
            managerActor.Tell(new DisplayInprogressProductsMessage());
            Console.ReadLine();

            managerActor.Tell(new DisplayAllBillsMessage());

            Console.ReadLine();
        }
    }
}