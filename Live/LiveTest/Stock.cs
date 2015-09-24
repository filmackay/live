namespace Vertigo.Live.Test
{
    public class Stock
    {
        public Stock()
        {
            MarketCap = Price.Multiply(SharesOnIssue);
        }

        private readonly Live<string> _Code = Live<string>.NewDefault();
        public Live<string> Code { get { return _Code; } }

        private readonly Live<string> _Name = Live<string>.NewDefault();
        public Live<string> Name { get { return _Name; } }

        private readonly Live<decimal> _Price = Live<decimal>.NewDefault();
        public Live<decimal> Price { get { return _Price; } }

        private readonly Live<decimal> _Turnover = Live<decimal>.NewDefault();
        public Live<decimal> Turnover { get { return _Turnover; } }

        private readonly Live<decimal> _SharesOnIssue = Live<decimal>.NewDefault();
        public Live<decimal> SharesOnIssue { get { return _SharesOnIssue; } }

        private readonly Live<decimal?> _Bid = Live<decimal?>.NewDefault();
        public Live<decimal?> Bid { get { return _Bid; } }

        private readonly Live<decimal?> _Ask = Live<decimal?>.NewDefault();
        public Live<decimal?> Ask { get { return _Ask; } }

        public ILiveValue<decimal> PercentOfMarket { get; private set; }
        public ILiveValue<decimal> MarketCap { get; private set; }

        public ILiveValue<decimal> TotalMarketCap
        {
            set { PercentOfMarket = MarketCap.Divide(value); }
        }
    }
}