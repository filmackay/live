using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using Vertigo;
using Vertigo.Live;

namespace LiveDemo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private LiveList<Security> _securities;
        public ILiveValue<decimal> TotalMarketCap { get; private set; }
        private readonly Live<int> _TotalPublished = Live<int>.NewDefault();
        private readonly Live<int> _NonPublished = Live<int>.NewDefault();
        public Live<int> TotalPublished { get { return _TotalPublished; } }
        public Live<int> NonPublished { get { return _NonPublished; } }
        public ILiveValue<double> TotalPublishedRate { get; private set; }
        public Random _random = new Random();
        public ILiveList<decimal> MarketCaps { get; private set; }

        public string RandomString(int size)
        {
            var builder = new StringBuilder();
            for (var i = 0; i < size; i++)
                builder.Append(Convert.ToChar(Convert.ToInt32(Math.Floor(26 * _random.NextDouble() + 65))));
            return builder.ToString();
        }

        public MainWindow()
        {
            InitializeComponent();

            // create population of Stocks
            _securities = new LiveList<Security>();
            _securities.PublishInner.Connect(
                Enumerable
                    .Range(0, 1000)
                    .Select(i =>
                        {
                            var security = new Security
                            {
                                Price = { PublishValue = i * 1000000 },
                                SharesOnIssue = { PublishValue = 1 }
                            };
                            do
                            {
                                security.Code.PublishValue = RandomString(3);
                            } while (_securities.PublishInner.Any(s => s.Code == security.Code));

                            return security;
                        }));

            // setup queries
            MarketCaps = _securities.Select(s => s.MarketCap);
            TotalMarketCap = MarketCaps.Sum();
            TotalPublishedRate = TotalPublished
                .Convert<int,double>()
                .ToLiveRateOfChange();

            // bind UI counters
            DataContext = this;

            // make constant deltas
            Observable.Start(() =>
                {
                    while (true)
                    {
                        using (Publish.Transaction())
                        {
                            TotalPublished.PublishValue++;

                            var now = HiResTimer.Now();
                            var securities = _securities.PublishInner.ToArray();
                            securities.FastParallel(
                                (s, from, to) =>
                                {
                                    for (var i = from; i < to; i++)
                                    {
                                        var security = s[i];
                                        var oldPrice = security.Price.PublishValue;
                                        var price = oldPrice;

                                        var delta = 0.01M; //random.Next(-5, 6) / 100M;
                                        //if (delta == 0)
                                        //    delta = 0.01M;

                                        price += delta;
                                        if (price < 0)
                                            price -= delta * 2;
                                        security.Price.SetValue(price, now);
                                    }
                                });
                        }
                    }
                });

            Observable
                .Timer(TimeSpan.Zero, TimeSpan.FromSeconds(0.11))
                .Subscribe(l =>
                    {
                        //using (Publish.Transaction())
                        _NonPublished.PublishValue++;
                    });
        }
    }
}
