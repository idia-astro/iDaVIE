
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing.Printing;
using UnityEngine;

namespace VideoMaker
{
    public abstract class Path
    {
        private Easing _easing;

        public Path(Easing easing = null)
        {
            _easing = easing;
        }

        public Vector3 GetPosition(float pathParam)
        {
            if (_easing is not null)
            {
                pathParam = _easing.GetValue(pathParam);
            }
            return OnGetPosition(pathParam);
        }

        //Normalised tangent of path
        public Vector3 GetDirection(float pathParam)
        {
            if (_easing is not null)
            {
                pathParam = _easing.GetValue(pathParam);
            }
            return OnGetDirection(pathParam);
        }

        //Normalised curvature of path
        public Vector3 GetUpDirection(float pathParam)
        {
            if (_easing is not null)
            {
                pathParam = _easing.GetValue(pathParam);
            }
            return OnGetUpDirection(pathParam);
        }

        protected abstract Vector3 OnGetPosition(float pathParam);
        protected abstract Vector3 OnGetDirection(float pathParam);
        protected abstract Vector3 OnGetUpDirection(float pathParam);
    }

    public class LinePath : Path
    {
        private Vector3 _startPosition;

        private Vector3 _endPosition;

        public LinePath(Vector3 startPosition, Vector3 endPosition, Easing easing = null) : base(easing)
        {
            this._startPosition = startPosition;
            this._endPosition = endPosition;
        }

        protected override Vector3 OnGetPosition(float pathParam)
        {
            return _startPosition * (1 - pathParam) + _endPosition * pathParam;
        }

        protected override Vector3 OnGetDirection(float pathParam)
        {
            return (_endPosition - _startPosition).normalized;
        }

        protected override Vector3 OnGetUpDirection(float pathParam)
        {
            //Note straight line has zero curvature, which is not allowed here
            return Vector3.up;
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

        public CirclePath(
            Vector3 startPosition, Vector3 endPosition, Vector3 center,
            bool largeAngleDirection = false, int additionalRotations = 0,
            Easing easing = null) : base(easing)
        {
            _center = center;

            Vector3 relStart = startPosition - center;
            Vector3 relEnd = endPosition - center;

            _radius = relStart.magnitude;

            Vector3 axis = Vector3.Cross(relStart, relEnd);
            _basis1 = relStart / _radius;
            _basis2 = Vector3.Cross(axis, _basis1).normalized;

            float angle = Vector3.Angle(relStart, relEnd) / 360f;

            if (largeAngleDirection)
            {
                _rotations = angle - 1 - additionalRotations;
            }
            else
            {
                _rotations = angle + additionalRotations;
            }
        }

        //TODO test this more
        public CirclePath(Vector3 startPosition, Vector3 center, Vector3 axis, float rotations, Easing easing = null) : base(easing)
        {
            _center = center;

            Vector3 relStart = startPosition - center;

            _radius = relStart.magnitude;

            _basis1 = relStart / _radius;
            _basis2 = Vector3.Cross(axis, _basis1).normalized;

            _rotations = rotations;
        }

        //TODO test this more
        public CirclePath(Vector3 center, AxisDirection axis, float startAngle, float rotations, float radius, Easing easing = null) : base(easing)
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

        protected override Vector3 OnGetPosition(float pathParam)
        {
            float angle = 2 * Mathf.PI * _rotations * pathParam;
            return _center + _radius * (Mathf.Cos(angle) * _basis1 + Mathf.Sin(angle) * _basis2);
        }

        protected override Vector3 OnGetDirection(float pathParam)
        {
            float angle = 2 * Mathf.PI * _rotations * pathParam;
            return (Mathf.Cos(angle) * _basis2 - Mathf.Sin(angle) * _basis1).normalized;
        }

        protected override Vector3 OnGetUpDirection(float pathParam)
        {
            float angle = 2 * Mathf.PI * _rotations * pathParam;
            return -(Mathf.Cos(angle) * _basis1 + Mathf.Sin(angle) * _basis2).normalized;
        }
    }
}