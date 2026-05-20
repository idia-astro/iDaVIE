using UnityEngine;

namespace VideoMaker
{
    /// <summary>
    /// Base easing class with a linear curve.
    /// Easing functions are used to re-time animations, usually to imitate acceleration and deceleration.
    /// </summary>
    public class Easing
    {
        /// <summary>
        /// Main method to call for retiming. The input should be normalized.
        /// </summary>
        /// <param name="valueIn">Normalized value in.</param>
        /// <returns>Normalized, retimed value out.</returns>
        public float GetValue(float valueIn)
        {
            if (valueIn < 0)
            {
                return 0;
            }
            if (valueIn > 1)
            {
                return 1;
            }
            return OnGetValue(valueIn);
        }
        
        /// <summary>
        /// This method is to be overriden by derived classes.
        /// </summary>
        /// <param name="valueIn">Normalized value in.</param>
        /// <returns>Normalized, retimed value out.</returns>
        protected virtual float OnGetValue(float valueIn)
        {
            return valueIn;
        }
    }
    
    /// <summary>
    /// Slow-in easing using a monomial of a given order.
    /// </summary>
    public class EasingIn : Easing
    {
        private int _order;

        public EasingIn(int order)
        {
            _order = order;
        }

        protected override float OnGetValue(float valueIn)
        {
            return Mathf.Pow(valueIn, _order);
        }
    }

    /// <summary>
    /// Slow-out easing using a transformed monomial of a given order.
    /// </summary>
    public class EasingOut : Easing
    {
        private int _order;

        public EasingOut(int order)
        {
            _order = order;
        }

        protected override float OnGetValue(float valueIn)
        {
            return 1 - Mathf.Pow(1 - valueIn, _order);
        }
    }

    /// <summary>
    /// Slow-in and slow-out easing using a piecewise defined function of monomials of a given order.
    /// </summary>
    public class EasingInOut : Easing
    {
        private int _order;

        public EasingInOut(int order)
        {
            _order = order;
        }

        protected override float OnGetValue(float valueIn)
        {
            if (valueIn < 0.5f)
            {
                return 0.5f * Mathf.Pow(2 * valueIn, _order);
            }
            return 1 - 0.5f * Mathf.Pow(2 * (1 - valueIn), _order);
        }
    }

    /// <summary>
    /// Slow-in, steady middle and slow-out.
    /// Uses a piecewise defined funtion with a monomial function of given order for the first iterval, a linear function for the middle interval and a transformed monomial function of the same order for the last interval.
    /// </summary>
    public class EasingInLinOut : Easing
    {
        private int _order;
        private float _t1;
        private float _t2;
    
        /// <summary>
        /// Construct the Easing object given an <c>order</c> for the ease-in and ease-out mononomials and the durations for these parts.
        /// The sum of the (normalized) durations of the ease-in and ease-out parts should not exceed 1.
        /// </summary>
        /// <param name="order">Order of the ease-in and ease-out intervals.</param>
        /// <param name="timeIn">The normalized duration of the ease-in part of the easing.</param>
        /// <param name="timeOut">The normalized duration of the ease-out part of the easing.</param>
        public EasingInLinOut(int order, float timeIn, float timeOut)
        {
            _order = order;
            _t1 = timeIn;
            _t2 = 1 - timeOut;
    
            _t2 = _t2 < _t1 ? _t1 : _t2;
        }
    
        protected override float OnGetValue(float valueIn)
        {
            //TODO refactor this
            float mag = 0.5f * (1 + _t2 - _t1);

            if (valueIn < _t1 && _t1 > 0f)
            {
                return 0.5f * valueIn * valueIn / _t1 / mag;
            }
            if (valueIn < _t2)
            {
                return (valueIn - 0.5f * _t1) / mag;
            }
            return (
                _t2 - 0.5f * _t1
                + valueIn * (1 - 0.5f * valueIn) / (1 - _t2)
                - _t2 * (1 - 0.5f * _t2) / (1 - _t2)
                ) / mag;
        }
    }
}