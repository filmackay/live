// attempt at making a markup extension based binding..

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Threading;

namespace Vertigo.Live
{
    public class Binding : MarkupExtension, IDisposable
    {
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            // only provide bindings to individual values
            var provideValueTarget = (IProvideValueTarget)serviceProvider.GetService(typeof(IProvideValueTarget));
            if (provideValueTarget == null)
                return null;

            // get details of target
            var targetObject = provideValueTarget.TargetObject as DependencyObject;
            var targetProperty = provideValueTarget.TargetProperty as DependencyProperty;
            if (targetObject == null || targetProperty == null)
                return null;

            _timer = Observable
                .Timer(TimeSpan.Zero, TimeSpan.FromSeconds(0.25))
                .ObserveOn(new DispatcherSynchronizationContext())
                .Subscribe(l => targetObject.SetValue(targetProperty, l.ToString()));

            return "X";
        }

        [DefaultValue(null)]
        public PropertyPath Path { get; set; }
        [DefaultValue(null)]
        public object Source { get; set; }

        private void Dispose(bool final)
        {
            Debug.WriteLine("Binding.Dispose");
            _timer.Dispose();
        }

        #region Deconstructor
        private bool _disposed;

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                Dispose(false);
                GC.SuppressFinalize(this);
            }
        }

        ~Binding()
        {
            Dispose(true);
        }
        #endregion

        private IDisposable _timer;
    }

    public static class BindingExtensions
    {
        public static readonly DependencyProperty BindingProperty = DependencyProperty.RegisterAttached("Live.Binding", typeof(Binding), typeof(FrameworkElement));

        public static void SetLiveBinding(this DependencyObject obj, DependencyProperty prop, Binding binding)
        {
            // get rid of old binding
            var oldBinding = GetLiveBinding(obj, prop);
            if (oldBinding != null)
                oldBinding.Dispose();

            // set new binding
            obj.SetValue(BindingProperty, binding);

            // start binding
            if (binding != null)
                binding.ProvideValue(new ProvideValueTarget(obj, prop));

            obj.
        }

        public static Binding GetLiveBinding(this DependencyObject obj, DependencyProperty prop)
        {
            return obj.GetValue(BindingProperty) as Binding;
        }
    }

    public struct ProvideValueTarget : IServiceProvider, IProvideValueTarget
    {
        private readonly DependencyObject _targetObject;
        private readonly DependencyProperty _targetProperty;

        public ProvideValueTarget(DependencyObject targetObject, DependencyProperty targetProperty)
        {
            _targetObject = targetObject;
            _targetProperty = targetProperty;
        }

        object IServiceProvider.GetService(Type serviceType)
        {
            if (serviceType == typeof(IProvideValueTarget))
                return this;
            return null;
        }

        object IProvideValueTarget.TargetObject { get { return _targetObject; } }
        object IProvideValueTarget.TargetProperty { get { return _targetProperty; } }
    }
}
