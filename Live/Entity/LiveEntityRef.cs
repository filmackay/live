using System;

namespace Vertigo.Live
{
    // Primary side of 1:1 association, pointing to foreign entity
    public class LiveAssociationPrimary<TPrimary, TForeign, TDataContext> : Live<TForeign>
        where TDataContext : LiveDataContext<TDataContext>
        where TForeign : class, ILiveEntity<TForeign, TDataContext>, new()
        where TPrimary : class, ILiveEntity<TPrimary, TDataContext>, new()
    {
        private readonly TPrimary _primary;
        private readonly Func<TForeign, LiveAssociationForeign<TForeign, TPrimary, TDataContext>> _getForeignRef;

        public LiveAssociationPrimary(TPrimary entity, Func<TForeign, LiveAssociationForeign<TForeign, TPrimary, TDataContext>> getForeignRef)
        {
            _primary = entity;
            _getForeignRef = getForeignRef;
            Connect(null);
        }

        public override TForeign PublishValue
        {
            set
            {
                if (base.PublishValue == value)
                    return;

                base.PublishValue = value;
                _getForeignRef(value).InternalValue = _primary;
            }
        }
    }

    // Foreign side of 1:1 association, pointing to primary entity
    public class LiveAssociationForeign<TForeign, TPrimary, TDataContext> : Live<TPrimary>, ILiveEntityParentRef
        where TDataContext : LiveDataContext<TDataContext>
        where TForeign : class, ILiveEntity<TForeign, TDataContext>, new()
        where TPrimary : class, ILiveEntity<TPrimary, TDataContext>, new()
    {
        private readonly TForeign _foreign;
        private readonly Func<TPrimary, LiveAssociationPrimary<TPrimary, TForeign, TDataContext>> _getPrimaryRef;
        private readonly Action<TForeign, TPrimary> _setForeignKey;

        public LiveAssociationForeign(TForeign entity, Func<TPrimary, LiveAssociationPrimary<TPrimary, TForeign, TDataContext>> getPrimaryRef, Action<TForeign, TPrimary> setForeignKey)
        {
            _foreign = entity;
            _getPrimaryRef = getPrimaryRef;
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
                        _getPrimaryRef(base.PublishValue).PublishValue = null;
                    if (value != null)
                        _getPrimaryRef(value).PublishValue = _foreign;
                }
            }
        }

        internal TPrimary InternalValue
        {
            set
            {
                using (this.Lock())
                {
                    // only called by primary
                    base.PublishValue = value;

                    // potentially update our DataContext
                    if (value != null && value.DataContext != null)
                        value.DataContext.Add(_foreign, true);

                    // update foreign key columns; do this after DataContext move to make sure that we dont create a database update for the old DataContext
                    _setForeignKey(_foreign, value);
                }
            }
        }

        public void Detach()
        {
            using (this.Lock())
            {
                // link via parent
                if (base.PublishValue != null)
                    _getPrimaryRef(base.PublishValue).PublishValue = null;
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