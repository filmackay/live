using System.ComponentModel;

namespace Vertigo.Live.Test
{
    public class OldStock : INotifyPropertyChanged
    {
        public string Code { get; set; }
        public string Name { get; set; }

        private void OnPropertyChange(string name)
        {
            var propertyChanged = PropertyChanged;
            if (propertyChanged != null)
                propertyChanged(this, new PropertyChangedEventArgs(name));
        }

        private decimal _price;
        public decimal Price
        {
            get
            {
                return _price;
            }
            set
            {
                _price = value;
                OnPropertyChange("Price");
            }
        }

        private long _turnover;
        public long Turnover
        {
            get
            {
                return _turnover;
            }
            set
            {
                _turnover = value;
                OnPropertyChange("Turnover");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}