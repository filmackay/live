using System;
using Vertigo.Live;

namespace LiveDemo
{
    public class Security
    {
        public Security()
        {
            MarketCap = Price.Multiply(SharesOnIssue);
        }

        private readonly Live<string> _Code = Live<string>.NewDefault();
        public Live<string> Code { get { return _Code; } }

        private readonly Live<decimal> _Price = Live<decimal>.NewDefault();
        public Live<decimal> Price { get { return _Price; } }

        private readonly Live<decimal> _SharesOnIssue = Live<decimal>.NewDefault();
        public Live<decimal> SharesOnIssue { get { return _SharesOnIssue; } }

        public ILiveValue<decimal> MarketCap { get; private set; }
        public ILiveValue<decimal> PercentOfMarket { get; private set; }

        public ILiveValue<decimal> TotalMarketCap
        {
            set { PercentOfMarket = MarketCap.Divide(value); }
        }
    }
}
