
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing.Printing;
using UnityEngine;

namespace VideoMaker
{
    public abstract class Path
    {
        public Easing Easing { set; get; }


        public (Vector3 position, Vector3 direction, Vector3 upDirection) GetPositionDirection(float pathParam)
        {
            pathParam = Easing is not null ? Easing.GetValue(pathParam) : pathParam;

            return OnGetPositionDirection(pathParam);
        }

        protected abstract (Vector3 position, Vector3 direction, Vector3 upDirection) OnGetPositionDirection(float pathParam);
    }

    public class LinePath : Path
    {
        private Vector3 _start;

        private Vector3 _end;

        public LinePath(Vector3 start, Vector3 end)
        {
            this._start = start;
            this._end = end;
        }

        protected override (Vector3 position, Vector3 direction, Vector3 upDirection) OnGetPositionDirection(float pathParam)
        {
            return (
                _start * (1 - pathParam) + _end * pathParam,
                (_end - _start).normalized,
                Vector3.up
            );
        }
    }

    public class CirclePath : Path
    {
        public enum AxisDirection
        {
            Up,
            Down,
            Left,
            Right,
            Forward,
            Back,
            None
        }

        private Vector3 _center;
        private Vector3 _basis1;
        private Vector3 _basis2;
        private float _radius;
        private float _rotations;

        public CirclePath(Vector3 start, Vector3 end, Vector3 center, int rotations = 1)
        {
            _center = center;

            Vector3 relStart = start - center;
            Vector3 relEnd = end - center;

            _radius = relStart.magnitude;

            Vector3 axis = Vector3.Cross(relEnd, relStart);
            if (axis.magnitude == 0)
            {
                //Start, center and end are co-linear, so use a default axis
                axis = Vector3.up;
            }

            _basis1 = relStart / _radius;
            _basis2 = Vector3.Cross(_basis1, axis).normalized;

            float angle = Vector3.Angle(relStart, relEnd) / 360f;

            if (rotations <= 0)
            {
                _rotations = angle + rotations;
            }
            else
            {
                _rotations = angle + rotations - 1;
            }
            // _rotations = angle + rotations - (rotations < 0 ? 1 : 0);

            Debug.Log($"Circle with center {_center.ToString()}, radius {_radius}, basis1 {_basis1.ToString()} and basis2 {_basis2.ToString()}");
        }

        //TODO test this more - problem seems over-subscribed without altering the center
        public CirclePath(Vector3 start, Vector3 center, Vector3 axis, float rotations)
        {
            _center = center;

            Vector3 relStart = start - center;

            _radius = relStart.magnitude;

            _basis1 = relStart / _radius;
            _basis2 = Vector3.Cross(_basis1, axis).normalized;

            _rotations = rotations;

            Debug.Log($"Circle with center {_center.ToString()}, radius {_radius}, axis {axis.ToString()}, basis1 {_basis1.ToString()} and basis2 {_basis2.ToString()}");
        }

        //TODO test this more
        public CirclePath(Vector3 center, AxisDirection axis, float startAngle, float rotations, float radius)
        {
            _center = center;
            _radius = radius;
            _rotations = rotations;

            float sin = Mathf.Sin(startAngle);
            float cos = Mathf.Cos(startAngle);

            switch (axis)
            {
                case AxisDirection.Up:
                    _basis1 = new(sin, 0, -cos);
                    _basis2 = new(cos, 0, sin);
                    break;
                case AxisDirection.Down:
                    _basis1 = new(-sin, 0, -cos);
                    _basis2 = new(-cos, 0, sin);
                    break;
                case AxisDirection.Left:
                    _basis1 = new(0, cos, sin);
                    _basis2 = new(0, -sin, cos);
                    break;
                case AxisDirection.Right:
                    _basis1 = new(0, cos, -sin);
                    _basis2 = new(0, -sin, -cos);
                    break;
                case AxisDirection.Back:
                    _basis1 = new(cos, sin, 0);
                    _basis2 = new(-sin, cos, 0);
                    break;
                case AxisDirection.Forward:
                    _basis1 = new(cos, -sin, 0);
                    _basis2 = new(-sin, -cos, 0);
                    break;
            }
        }

        protected override (Vector3 position, Vector3 direction, Vector3 upDirection) OnGetPositionDirection(
            float pathParam)
        {
            float angle = 2 * Mathf.PI * _rotations * pathParam;
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);

            return (
                _center + _radius * (cos * _basis1 + sin * _basis2),
                (cos * _basis2 - sin * _basis1).normalized,
                -(cos * _basis1 + sin * _basis2).normalized
            );
        }
    }

    public class CubicPath : Path
    {
        private readonly Vector3 _a;
        private readonly Vector3 _b;
        private readonly Vector3 _c;
        private readonly Vector3 _d;

        public CubicPath(Vector3 start, Vector3 end, Vector3 startD, Vector3 endD, bool isSecondDerivative = false)
        {
            if (isSecondDerivative)
            {
                _a = (endD - startD) / 6;
                _b = 0.5f * startD;
                _c = end - start - (2 * startD + endD) / 6;
                _d = start;
            }
            else
            {
                _a = 2 * (start - end) + startD + endD;
                _b = 3 * (end - start) - 2 * startD - endD;
                _c = startD;
                _d = start;
            }
        }
        
        protected override (Vector3 position, Vector3 direction, Vector3 upDirection) OnGetPositionDirection(float pathParam)
        {
            float t = pathParam;
            float t2 = pathParam * pathParam;
            float t3 = t2 * pathParam;

            return (
                _a * t3 + _b * t2 + _c * t + _d,
                3 * t2 * _a + 2 * t * _b + _c,
                6 * t * _a + 2 * _b
            );
        }
    }
}