using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VideoMaker
{

    //Paths
    public abstract class VideoCameraPath
    {
        public abstract Vector3 GetPosition(float pathParam);
        //Normalised tangent of path
        public abstract Vector3 GetDirection(float pathParam);
        //Normalised curvature of path
        public abstract Vector3 GetUpDirection(float pathParam);
    }

    public class LinePath : VideoCameraPath
    {
        private Vector3 _startPosition;

        private Vector3 _endPosition;

        public LinePath(Vector3 startPosition, Vector3 endPosition)
        {
            this._startPosition = startPosition;
            this._endPosition = endPosition;
        }

        public override Vector3 GetPosition(float pathParam)
        {
            return _startPosition * (1 - pathParam) + _endPosition * pathParam;
        }

        public override Vector3 GetDirection(float pathParam)
        {
            return (_endPosition - _startPosition).normalized;
        }

        public override Vector3 GetUpDirection(float pathParam)
        {
            //Note straight line has zero curvature, which is not allowed here
            return Vector3.up;
        }
    }

    public class CirclePath : VideoCameraPath
    {
        private Vector3 _center;
        private Vector3 _basis1;
        private Vector3 _basis2;
        private float _radius;
        private float _rotations;

        public CirclePath(
            Vector3 startPosition, Vector3 endPosition, Vector3 center,
            bool largeAngleDirection = false, int additionalRotations = 0, bool fullRotation = false
        )
        {
            _center = center;

            Vector3 relStart = startPosition - center;
            Vector3 relEnd = endPosition - center;

            _radius = relStart.magnitude;

            Vector3 axis = Vector3.Cross(relStart, relEnd);
            _basis1 = relStart / _radius;
            _basis2 = Vector3.Cross(axis, _basis1).normalized;

            float angle = Vector3.Angle(relStart, relEnd) / 360f;

            if (fullRotation)
            {
                _rotations = 1 + additionalRotations;
            }
            else
            {
                if (largeAngleDirection)
                {
                    _rotations = angle - 1 - additionalRotations;
                }
                else
                {
                    _rotations = angle + additionalRotations;
                }
            }
        }

        public override Vector3 GetPosition(float pathParam)
        {
            float angle = 2 * Mathf.PI * _rotations * pathParam;
            return _center + _radius * (Mathf.Cos(angle) * _basis1 + Mathf.Sin(angle) * _basis2);
        }

        public override Vector3 GetDirection(float pathParam)
        {
            float angle = 2 * Mathf.PI * _rotations * pathParam;
            return (Mathf.Cos(angle) * _basis2 - Mathf.Sin(angle) * _basis1).normalized;
        }

        public override Vector3 GetUpDirection(float pathParam)
        {
            float angle = 2 * Mathf.PI * _rotations * pathParam;
            return - (Mathf.Cos(angle) * _basis1 + Mathf.Sin(angle) * _basis2).normalized;
        }
    }

    // VideoCameraActions
    public abstract class VideoCameraAction
    {
        //TODO use private variable + setters and getters to make these unchangable
        public float StartTime;
        public float Duration;

        public VideoCameraAction(float duration)
        {
            this.Duration = duration;
        }
    }

    // VideoPositionActions
    public abstract class VideoPositionAction : VideoCameraAction
    {
        public abstract Vector3 GetPosition(float time);

        public VideoPositionAction(float duration) : base(duration) { }
    }

    public class VideoPositionActionHold : VideoPositionAction
    {
        private Vector3 _position;

        public VideoPositionActionHold(float duration, Vector3 position) : base(duration)
        {
            _position = position;
        }

        public override Vector3 GetPosition(float time)
        {
            return _position;
        }
    }

    public class VideoPositionActionPath : VideoPositionAction
    {
        private VideoCameraPath _path;

        public VideoPositionActionPath(float duration, VideoCameraPath path) : base(duration)
        {
            _path = path;
        }

        public override Vector3 GetPosition(float time)
        {
            // float pathParam = Math.Clamp((time - StartTime) / Duration, 0f, 1f);
            float pathParam = Math.Clamp(time / Duration, 0f, 1f);

            return _path.GetPosition(pathParam);
        }
    }

    // VideoDirectionActions
    public abstract class VideoDirectionAction : VideoCameraAction
    {
        public abstract Vector3 GetDirection(float time, Vector3 position);
        public VideoDirectionAction(float duration) : base(duration) { }
    }

    public class VideoDirectionActionHold : VideoDirectionAction
    {
        private Vector3 _direction;

        public VideoDirectionActionHold(float duration, Vector3 direction) : base(duration)
        {
            _direction = direction;
        }

        public override Vector3 GetDirection(float time, Vector3 position)
        {
            return _direction;
        }
    }

    //Note this can be replaced with a path...
    public class VideoDirectionActionTween : VideoDirectionAction
    {
        private Vector3 _directionFrom;
        private Vector3 _directionTo;

        public VideoDirectionActionTween(float duration, Vector3 directionFrom, Vector3 directionTo) : base(duration)
        {
            _directionFrom = directionFrom;
            _directionTo = directionTo;
        }

        public override Vector3 GetDirection(float time, Vector3 position)
        {
            // float frac = Math.Clamp((time - StartTime) / Duration, 0f, 1f);
            float frac = Math.Clamp(time / Duration, 0f, 1f);
            return (_directionFrom * (1 - frac) + _directionTo * frac).normalized;
        }
    }


    public class VideoDirectionActionLookAt : VideoDirectionAction
    {
        private Vector3 _target;

        public VideoDirectionActionLookAt(float duration, Vector3 target) : base(duration)
        {
            _target = target;
        }

        public override Vector3 GetDirection(float time, Vector3 position)
        {
            return (_target - position).normalized;
        }
    }

    public class VideoDirectionActionPath : VideoDirectionAction
    {
        private VideoCameraPath _path;
        private float _startTimeOffset;
        private float _endTimeOffset;
        private bool _useUpDirection;
        private bool _invert;

        public VideoDirectionActionPath(float duration,
            VideoCameraPath path,
            float startTimeOffset = 0f, float endTimeOffset = 0f,
            bool useUpDirection = false, bool invert = false
        ) : base(duration)
        {
            _path = path;
            _startTimeOffset = startTimeOffset;
            _endTimeOffset = endTimeOffset;
            _useUpDirection = useUpDirection;
            _invert = invert;
        }

        public override Vector3 GetDirection(float time, Vector3 position)
        {
            // float pathParam = Math.Clamp(
            //     (time + _startTimeOffset - StartTime) / (Duration + _startTimeOffset + _endTimeOffset),
            //     0f, 1f
            // );

            float pathParam = Math.Clamp(
                (time + _startTimeOffset) / (Duration + _startTimeOffset + _endTimeOffset),
                0f, 1f
            );

            int sign = 1;

            if (_invert)
            {
                sign = -1;
            }

            if (_useUpDirection)
            {
                return sign * _path.GetUpDirection(pathParam);
            }

            return sign * _path.GetDirection(pathParam);
        }
    }
}