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
        public float StartTime { set; get; }
        public float Duration { set; get; } = 0f;

        public float EndTime { get => StartTime + Duration; }
    }

    // PositionActions
    public abstract class PositionAction : Action
    {
        public (Vector3 position, Vector3 direction, Vector3 upDirection) GetPositionDirection(float time) {
            return OnGetPositionDirection(Duration > 0 ? (time - StartTime) / Duration : 0);
        }

        protected abstract (Vector3 position, Vector3 direction, Vector3 upDirection) OnGetPositionDirection(float time)
    }

    public class PositionActionHold : PositionAction
    {
        private Vector3 _position;

        public PositionActionHold(Vector3 position)
        {
            _position = position;
        }

        protected override (Vector3 position, Vector3 direction, Vector3 upDirection) OnGetPositionDirection(float time)
        {
            return (_position, Vector3.forward, Vector3.up);
        }
    }

    public class PositionActionPath : PositionAction
    {
        private Path _path;

        public PositionActionPath(Path path)
        {
            _path = path;
        }

        protected override (Vector3 position, Vector3 direction, Vector3 upDirection) OnGetPositionDirection(float time)
        {
            return _path.GetPositionDirection(time);
        }
    }

    // DirectionActions
    public abstract class DirectionAction : Action
    {
        public bool ReverseDirection { get; set; } = false;
        public Vector3 GetDirection(float time, Vector3 position, Vector3 pathForward, Vector3 pathUp) {
            return (ReverseDirection ? -1 : 1) * OnGetDirection(Duration > 0 ? (time - StartTime) / Duration : 0, position, pathForward, pathUp);
        }

        protected abstract Vector3 OnGetDirection(float time, Vector3 position, Vector3 pathForward, Vector3 pathUp);
    }

    public class DirectionActionHold : DirectionAction
    {
        private Vector3 _direction;

        public DirectionActionHold(Vector3 direction)
        {
            _direction = direction;
        }

        protected override Vector3 OnGetDirection(float time, Vector3 position, Vector3 pathForward, Vector3 pathUp)
        {
            return _direction;
        }
    }

    public class DirectionActionTween : DirectionAction
    {
        private Vector3 _directionFrom;
        private Vector3 _directionTo;
        private Easing _easing;

        public DirectionActionTween(Vector3 directionFrom, Vector3 directionTo, Easing easing = null)
        {
            _directionFrom = directionFrom;
            _directionTo = directionTo;
            _easing = easing;
        }

        protected override Vector3 OnGetDirection(float time, Vector3 position, Vector3 pathForward, Vector3 pathUp)
        {
            time = _easing is null? time : _easing.GetValue(time);

            return (_directionFrom * (1 - time) + _directionTo * time).normalized;
        }
    }

    public class DirectionActionLookAt : DirectionAction
    {
        private Vector3 _target;

        public DirectionActionLookAt(Vector3 target)
        {
            _target = target;
        }

        protected override Vector3 OnGetDirection(float time, Vector3 position, Vector3 pathForward, Vector3 pathUp)
        {
            return (_target - position).normalized;
        }
    }

    public class DirectionActionPath : DirectionAction
    {
        protected override Vector3 OnGetDirection(float time, Vector3 position, Vector3 pathForward, Vector3 pathUp)
        {
            return pathForward;
        }
    }

    public class UpDirectionActionPath : DirectionAction
    {
        protected override Vector3 OnGetDirection(float time, Vector3 position, Vector3 pathForward, Vector3 pathUp)
        {
            return pathUp;
        }
    }
}