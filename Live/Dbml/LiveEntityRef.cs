using System;

namespace Vertigo.Live
{
    // Primary side of 1:1 association, pointing to foreign entity
    public class LiveAssociationPrimary<TPrimary, TForeign> : Live<TForeign>
        where TForeign : class, ILiveEntity<TForeign>, new()
        where TPrimary : class, ILiveEntity<TPrimary>, new()
    {
        private readonly TPrimary _primary;
        private readonly Func<TForeign, LiveAssociationForeign<TForeign, TPrimary>> _getForeignRef;

        public LiveAssociationPrimary(TPrimary entity, Func<TForeign, LiveAssociationForeign<TForeign, TPrimary>> getForeignRef)
        {
            _primary = entity;
            _getForeignRef = getForeignRef;
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
    public class LiveAssociationForeign<TForeign, TPrimary> : Live<TPrimary>
        where TForeign : class, ILiveEntity<TForeign>, new()
        where TPrimary : class, ILiveEntity<TPrimary>, new()
    {
        private readonly TForeign _foreign;
        private readonly Func<TPrimary, LiveAssociationPrimary<TPrimary, TForeign>> _getPrimaryRef;
        private readonly Action<TForeign, TPrimary> _setForeignKey;

        public LiveAssociationForeign(TForeign entity, Func<TPrimary, LiveAssociationPrimary<TPrimary, TForeign>> getPrimaryRef, Action<TForeign, TPrimary> setForeignKey)
        {
            _foreign = entity;
            _getPrimaryRef = getPrimaryRef;
            _setForeignKey = setForeignKey;
        }

        public override TPrimary PublishValue
        {
            set
            {
                // link via primary
                _getPrimaryRef(value).PublishValue = _foreign;
            }
        }

        internal TPrimary InternalValue
        {
            set
            {
                // only called by primary
                base.PublishValue = value;

                // update foreign key columns
                _setForeignKey(_foreign, value);
            }
        }
    }
}