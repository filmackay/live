using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Vertigo.Live
{
    // Primary (1) side of N:1 association
    public class LiveEntitySet<TPrimary, TForeign, TDataContext> : LiveSet<TForeign, LiveEntitySetInner<TPrimary, TForeign, TDataContext>, LiveEntitySet<TPrimary, TForeign, TDataContext>>
        where TPrimary : class, ILiveEntity<TPrimary, TDataContext>, new()
        where TForeign : class, ILiveEntity<TForeign, TDataContext>, new()
        where TDataContext : LiveDataContext<TDataContext>
    {
        internal readonly TPrimary _parentObject;
        internal readonly Func<TForeign, LiveEntitySetForeign<TForeign, TPrimary, TDataContext>> _selector;

        public LiveEntitySet(TPrimary parentObject, Func<TForeign, LiveEntitySetForeign<TForeign, TPrimary, TDataContext>> selector)
            : base(new HashSet<TForeign>(), new HashSet<TForeign>(), new TForeign[] { })
        {
            _parentObject = parentObject;
            _selector = selector;
        }
    }

    public class LiveEntitySetInner<TPrimary, TForeign, TDataContext> : LiveSetInner<TForeign, LiveEntitySetInner<TPrimary, TForeign, TDataContext>, LiveEntitySet<TPrimary, TForeign, TDataContext>>
        where TPrimary : class, ILiveEntity<TPrimary, TDataContext>, new()
        where TForeign : class, ILiveEntity<TForeign, TDataContext>, new()
        where TDataContext : LiveDataContext<TDataContext>
    {
        protected override bool _Add(TForeign item)
        {
            var ret = base._Add(item);
            // if add was successful,point child to parent
            if (ret)
                _parent._selector(item).InternalPublishValue = _parent._parentObject;
            return ret;
        }

        public override bool Remove(TForeign item)
        {
            var ret = base.Remove(item);
            // if remove was successful - un-point child to parent
            if (ret)
                _parent._selector(item).InternalPublishValue = null;
            return ret;
        }

        public bool Detach(TForeign item)
        {
            var ret = base.Remove(item);
            // if remove was successful - un-point child to parent
            if (ret)
                _parent._selector(item).InternalDetach();
            return ret;
        }
    }

    // Foreign (N) side of N:1 association
    public class LiveEntitySetForeign<TForeign, TPrimary, TDataContext> : Live<TPrimary>, ILiveEntityParentRef
        where TDataContext : LiveDataContext<TDataContext>
        where TPrimary : class, ILiveEntity<TPrimary, TDataContext>, new()
        where TForeign : class, ILiveEntity<TForeign, TDataContext>, new()
    {
        private readonly TForeign _entity;
        private readonly Func<TPrimary, LiveEntitySet<TPrimary, TForeign, TDataContext>> _getSiblingSet;
        private readonly Action<TPrimary, TForeign> _setForeignKey;

        public LiveEntitySetForeign(TForeign entity, Func<TPrimary, LiveEntitySet<TPrimary, TForeign, TDataContext>> getSiblingSet, Action<TPrimary, TForeign> setForeignKey) : base(null)
        {
            _entity = entity;
            _getSiblingSet = getSiblingSet;
            _setForeignKey = setForeignKey;
            Connect(null);
        }

        public override TPrimary PublishValue
        {
            set
            {
                using (this.Lock())
                {
                    // link via parent
                    if (base.PublishValue != null)
                        _getSiblingSet(base.PublishValue).PublishInner.Remove(_entity);
                    if (value != null)
                        _getSiblingSet(value).PublishInner.Add(_entity);
                }
            }

            get { return base.PublishValue; }
        }

        internal TPrimary InternalPublishValue
        {
            set
            {
                using (this.Lock())
                using (Publish.Transaction())
                {
                    // only called by parent
                    base.PublishValue = value;

                    // update our DataContext if new one is differet to the existing
                    if (value != null && value.DataContext != null)
                        value.DataContext.Add(_entity, true);

                    // update foreign key columns; do this after DataContext move to make sure that we dont create a database update for the old DataContext
                    _setForeignKey(value, _entity);
                }
            }
        }

        public void Detach()
        {
            using (this.Lock())
            {
                // link via parent
                if (base.PublishValue != null)
                    _getSiblingSet(base.PublishValue).PublishInner.Detach(_entity);
            }
        }

        internal void InternalDetach()
        {
            using (this.Lock())
            {
                // only called by parent
                base.PublishValue = null;

                // dont change foreign key columns
            }
        }
    }
}