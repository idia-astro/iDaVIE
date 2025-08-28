using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing.Printing;
using UnityEngine;

namespace VideoMaker
{
    public class Easing
    {
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

        protected virtual float OnGetValue(float valueIn)
        {
            return valueIn;
        }
    }

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

    //TODO replace AccelDecel with this
    public class EasingInLinOut : Easing
    {
        private int _order;
        private float _t1;
        private float _t2;

        public EasingInLinOut(int order, float timeIn, float timeOut)
        {
            _order = order;
            _t1 = timeIn;
            _t2 = 1 - timeOut;

            _t2 = _t2 < _t1 ? _t1 : _t2;
        }

        protected override float OnGetValue(float valueIn)
        {
            //TODO finish this
            return valueIn;
        }
    }

    // public class EasingAccelDecel : Easing
    // {
    //     private float _t1;
    //     private float _t2;

    //     public EasingAccelDecel(float t1, float t2)
    //     {
    //         _t1 = t1;
    //         _t2 = t2;
    //     }

    //     protected override float OnGetValue(float valueIn)
    //     {
    //         float mag = 0.5f * (1 + _t2 - _t1);

    //         if (valueIn < _t1 && _t1 > 0f)
    //         {
    //             return 0.5f * valueIn * valueIn / _t1 / mag;
    //         }
    //         if (valueIn < _t2)
    //         {
    //             return (valueIn - 0.5f * _t1) / mag;
    //         }
    //         return (
    //             _t2 - 0.5f * _t1
    //             + valueIn * (1 - 0.5f * valueIn) / (1 - _t2)
    //             - _t2 * (1 - 0.5f * _t2) / (1 - _t2)
    //             ) / mag;
    //     }
    // }
}