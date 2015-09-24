using System;

namespace Vertigo.Live
{
    // Primary side of N:1 association
    public class LiveEntitySet<TPrimary, TForeign> : LiveSet<TForeign, LiveEntitySetInner<TPrimary, TForeign>, LiveEntitySet<TPrimary, TForeign>>
        where TPrimary : class, ILiveEntity<TPrimary>, new()
        where TForeign : class, ILiveEntity<TForeign>, new()
    {
        internal readonly TPrimary _parentObject;
        internal readonly Func<TForeign, LiveEntitySetForeign<TForeign, TPrimary>> _selector;

        public LiveEntitySet(TPrimary parentObject, Func<TForeign, LiveEntitySetForeign<TForeign, TPrimary>> selector)
        {
            _parentObject = parentObject;
            _selector = selector;
        }
    }

    public class LiveEntitySetInner<TPrimary, TForeign> : LiveSetInner<TForeign, LiveEntitySetInner<TPrimary, TForeign>, LiveEntitySet<TPrimary, TForeign>>
        where TPrimary : class, ILiveEntity<TPrimary>, new()
        where TForeign : class, ILiveEntity<TForeign>, new()
    {
        protected override bool _Add(TForeign item)
        {
            var ret = base._Add(item);
            // if add was successful,point child to parent
            if (ret)
                _parent._selector(item).InternalValue = _parent._parentObject;
            return ret;
        }

        public override bool Remove(TForeign item)
        {
            var ret = base.Remove(item);
            // if remove was successful - un-point child to parent
            if (ret)
                _parent._selector(item).InternalValue = null;
            return ret;
        }
    }

    // Foreign side of N:1 association
    public class LiveEntitySetForeign<TForeign, TPrimary> : Live<TPrimary>
        where TPrimary : class, ILiveEntity<TPrimary>, new()
        where TForeign : class, ILiveEntity<TForeign>, new()
    {
        private readonly TForeign _entity;
        private readonly Func<TPrimary, LiveEntitySet<TPrimary, TForeign>> _getSiblingSet;
        private readonly Action<TPrimary, TForeign> _setForeignKey;

        public LiveEntitySetForeign(TForeign entity, Func<TPrimary, LiveEntitySet<TPrimary, TForeign>> getSiblingSet, Action<TPrimary, TForeign> setForeignKey)
        {
            _entity = entity;
            _getSiblingSet = getSiblingSet;
            _setForeignKey = setForeignKey;
        }

        public override TPrimary PublishValue
        {
            set
            {
                // link via parent
                if (base.PublishValue != null)
                    _getSiblingSet(base.PublishValue).Inner.Remove(_entity);
                if (value != null)
                    _getSiblingSet(value).Inner.Add(_entity);
            }
        }

        internal TPrimary InternalValue
        {
            set
            {
                // only called by parent
                base.PublishValue = value;

                // update foreign key columns
                _setForeignKey(value, _entity);
            }
        }
    }
}