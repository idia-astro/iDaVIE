using UnityEngine;

namespace VideoMaker
{
    /// <summary>
    /// Base class for video camera actions which are used to determine the position and angle of the camera over time.
    /// </summary>
    public abstract class Action
    {
        //TODO should this use a init setter to make it immutable?
        public float Duration { set; get; } = 0f;
    }

    #region PositionActions
    // These actions define the position of the camera over time as well as path directions where relevant.
    
    /// <summary>
    /// Base class for PositionActions
    /// </summary>
    public abstract class PositionAction : Action
    {
        /// <summary>
        /// Used to get the position and path directions at a given time (time is 0 at the start of the action).
        /// </summary>
        /// <param name="time">Time in seconds after the start of the action.</param>
        /// <returns>Tuple of position, forward and up path directions.</returns>
        public (Vector3 position, Vector3 direction, Vector3 upDirection) GetPositionDirection(float time) {
            return OnGetPositionDirection(Duration > 0 ? time / Duration : 0);
        }
        
        /// <summary>
        /// Method to get the position and path direction at a given normalized time.
        /// This is intended as the method each PositionAction should override.
        /// </summary>
        /// <param name="time">Normalised time from 0 to 1.</param>
        /// <returns>Tuple of position, forward and up path directions.</returns>
        protected abstract (Vector3 position, Vector3 direction, Vector3 upDirection) OnGetPositionDirection(float time);
    }
    
    /// <summary>
    /// Used to define a stationary position.
    /// </summary>
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
    
    /// <summary>
    /// Used with a Path object to define a path that is traveled over for the duration of the action.
    /// </summary>
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
    
    #endregion
    
    
    #region DirectionActions
    // These actions define the directions (as Vector3s) of the camera over time.
    // These directions are the forward direction of the camera and the upwards direction of the camera.
    
    /// <summary>
    /// Base class for DirectionActions
    /// </summary>
    public abstract class DirectionAction : Action
    {
        /// <summary>
        /// This is used to negate the direction if the opposite direction is desired.
        /// </summary>
        public bool ReverseDirection { get; set; } = false;
        
        /// <summary>
        /// Used to get the direction given the camera position and path directions (these are needed for actions like the LookAt).
        /// </summary>
        /// <param name="time">Time after action started in seconds.</param>
        /// <param name="position">Position of the camera.</param>
        /// <param name="pathForward">Forwards direction of the path.</param>
        /// <param name="pathUp">Upwards direction of the path.</param>
        /// <returns></returns>
        public Vector3 GetDirection(float time, Vector3 position, Vector3 pathForward, Vector3 pathUp) {
            return (ReverseDirection ? -1 : 1) * OnGetDirection(Duration > 0 ? time / Duration : 0, position, pathForward, pathUp);
        }

        protected abstract Vector3 OnGetDirection(float time, Vector3 position, Vector3 pathForward, Vector3 pathUp);
    }
    
    /// <summary>
    /// Used to define a stationary direction.
    /// </summary>
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

    /// <summary>
    /// Used to rotate from one defined direction to another with an optional easing and rotation axis.
    /// </summary>
    public class DirectionActionTween : DirectionAction
    {
        public enum RotationDirection
        {
            Axis,
            SmallAngle,
            None
        }

        private RotationDirection _rotationDirection;
        
        private Vector3 _directionFrom;
        private Vector3 _directionTo;
        private Easing _easing;

        private Vector3 _axis;
        private Vector3 _basis1;
        private Vector3 _basis2;
        private float _altAngleFrom;
        private float _altAngleTo;
        private float _azAngle;

        public DirectionActionTween(Vector3 directionFrom, Vector3 directionTo, Easing easing = null)
        {
            _directionFrom = directionFrom.normalized;
            _directionTo = directionTo.normalized;
            _easing = easing;
            _rotationDirection = RotationDirection.None;
        }

        public DirectionActionTween(
            Vector3 directionFrom, Vector3 directionTo, 
            Vector3 axis, RotationDirection rotationDirection = RotationDirection.Axis, 
            Easing easing = null)
        {
            _directionFrom = directionFrom.normalized;
            _directionTo = directionTo.normalized;
            _easing = easing;
            _rotationDirection = rotationDirection;

            if (_rotationDirection == RotationDirection.None)
            {
                return;
            }

            _axis = axis;
            _basis2 = Vector3.Cross(directionFrom, axis).normalized;
            _basis1 = Vector3.Cross(axis, _basis2).normalized;

            Vector3 basisTo = (_directionTo - Vector3.Dot(_directionTo, _axis) * _axis).normalized; //-Vector3.Cross(axis, Vector3.Cross(directionTo, axis)).normalized;
            
            _altAngleFrom = Mathf.Acos(Vector3.Dot(_directionFrom, _axis));
            _altAngleTo = Mathf.Acos(Vector3.Dot(_directionTo, _axis));
            
            _azAngle = Mathf.Acos(Vector3.Dot(_basis1, basisTo));

            if (rotationDirection != RotationDirection.SmallAngle && Vector3.Dot(_directionTo, _basis2) < 0)
            {
                _azAngle = 2 * Mathf.PI - _azAngle;
            }
        }

        protected override Vector3 OnGetDirection(float time, Vector3 position, Vector3 pathForward, Vector3 pathUp)
        {
            time = _easing is null? time : _easing.GetValue(time);

            if (_rotationDirection == RotationDirection.None)
            {
                return Vector3.Slerp(_directionFrom, _directionTo, time).normalized;
            }

            float altAngle = Mathf.LerpAngle(_altAngleFrom, _altAngleTo, time);
            float azAngle = time * _azAngle;

            return Mathf.Sin(altAngle) * (Mathf.Cos(azAngle) * _basis1 + Mathf.Sin(azAngle) * _basis2) + Mathf.Cos(altAngle) * _axis;
        }
    }
    
    /// <summary>
    /// Used to look at a given target position in the data cube.
    /// </summary>
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
    
    /// <summary>
    /// Used for the direction along the path that the camera is traveling. 
    /// </summary>
    public class DirectionActionPath : DirectionAction
    {
        protected override Vector3 OnGetDirection(float time, Vector3 position, Vector3 pathForward, Vector3 pathUp)
        {
            return pathForward;
        }
    }

    /// <summary>
    /// Used for the up direction of the path that the camera is traveling. 
    /// </summary>
    public class UpDirectionActionPath : DirectionAction
    {
        protected override Vector3 OnGetDirection(float time, Vector3 position, Vector3 pathForward, Vector3 pathUp)
        {
            return pathUp;
        }
    }
    
    //TODO add DirectionAction for looking at a path.
    
    #endregion
}