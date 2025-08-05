using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing.Printing;
using UnityEngine;

namespace VideoMaker
{
    public abstract class Action
    {
        //TODO use private variable + setters and getters to make these unchangable
        public float StartTime;
        public float Duration;

        public Action(float duration)
        {
            this.Duration = duration;
        }
    }

    // PositionActions
    public abstract class PositionAction : Action
    {
        public abstract Vector3 GetPosition(float time);

        public PositionAction(float duration) : base(duration) { }
    }

    public class PositionActionHold : PositionAction
    {
        private Vector3 _position;

        public PositionActionHold(float duration, Vector3 position) : base(duration)
        {
            _position = position;
        }

        public override Vector3 GetPosition(float time)
        {
            return _position;
        }
    }

    public class PositionActionPath : PositionAction
    {
        private Path _path;

        public PositionActionPath(float duration, Path path) : base(duration)
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

    // DirectionActions
    public abstract class DirectionAction : Action
    {
        public abstract Vector3 GetDirection(float time, Vector3 position);
        public DirectionAction(float duration) : base(duration) { }
    }

    public class DirectionActionHold : DirectionAction
    {
        private Vector3 _direction;

        public DirectionActionHold(float duration, Vector3 direction) : base(duration)
        {
            _direction = direction;
        }

        public override Vector3 GetDirection(float time, Vector3 position)
        {
            return _direction;
        }
    }

    //Note this can be replaced with a path...
    public class DirectionActionTween : DirectionAction
    {
        private Vector3 _directionFrom;
        private Vector3 _directionTo;
        private Easing _easing;

        public DirectionActionTween(float duration, Vector3 directionFrom, Vector3 directionTo, Easing easing = null) : base(duration)
        {
            _directionFrom = directionFrom;
            _directionTo = directionTo;
            _easing = easing;
        }

        public override Vector3 GetDirection(float time, Vector3 position)
        {
            // float frac = Math.Clamp((time - StartTime) / Duration, 0f, 1f);
            float frac = Math.Clamp(time / Duration, 0f, 1f);

            if (_easing is not null)
            {
                frac = _easing.GetValue(frac);
            }

            return (_directionFrom * (1 - frac) + _directionTo * frac).normalized;
        }
    }

    public class DirectionActionLookAt : DirectionAction
    {
        private Vector3 _target;

        public DirectionActionLookAt(float duration, Vector3 target) : base(duration)
        {
            _target = target;
        }

        public override Vector3 GetDirection(float time, Vector3 position)
        {
            return (_target - position).normalized;
        }
    }

    public class DirectionActionPath : DirectionAction
    {
        private Path _path;
        private float _startTimeOffset;
        private float _endTimeOffset;
        private bool _useUpDirection;
        private bool _invert;

        public DirectionActionPath(float duration,
            Path path,
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