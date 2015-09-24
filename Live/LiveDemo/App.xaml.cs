using System;
using System.Windows;
using Vertigo;
using Vertigo.Live;
//using Petra;

namespace LiveDemo
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            // setup timelines
            Consumer.Dispatcher = new DispatcherConsumer(Dispatcher, TimeSpan.FromMilliseconds(1000));
        }
    }
}
