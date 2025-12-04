using UnityEngine;

namespace VideoMaker
{
    /// <summary>
    /// Base class for parameterized Path objects.
    /// These are used to define a position and directions of a path given a path parameter.
    /// </summary>
    public abstract class Path
    {
        /// <summary>
        /// Easing object to optionally retime the path parameter.
        /// </summary>
        public Easing Easing { set; get; }

        /// <summary>
        /// Get the position on the path and the forwards and upwards directions of the path at a point defined using the path parameter.
        /// </summary>
        /// <param name="pathParam">Normalized path parameter.</param>
        /// <returns>
        /// Tuple of
        /// - <c>position</c> - Position of the path.
        /// - <c>direction</c> - The forwards direction or normalized tangent vector of the path.
        /// - <c>upDirection</c> - The upwards direction or normalized curvature vector of the path.
        /// </returns>
        public (Vector3 position, Vector3 direction, Vector3 upDirection) GetPositionDirection(float pathParam)
        {
            pathParam = Easing is not null ? Easing.GetValue(pathParam) : pathParam;

            return OnGetPositionDirection(pathParam);
        }

        protected abstract (Vector3 position, Vector3 direction, Vector3 upDirection) OnGetPositionDirection(float pathParam);
    }

    /// <summary>
    /// Straight line path from one <c>start</c> position to another <c>end</c> position.
    /// </summary>
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

    /// <summary>
    /// Circular path defined in 3D space.
    /// This path may traverse a portion of the circle and / or multiple rotations.
    /// Note this path has multiple constructors as there are many different combinations of data that may be desired to construct it.
    /// </summary>
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
        
        /// <summary>
        /// Constructs a <c>CirclePath</c> using the given parameters.
        /// Note that the radius of the path is defined using the distance between <c>start</c> and <c>center</c>, thus if the distance between <c>end</c> and <c>center</c> is different, then the path will not end at the same position as <c>end</c>.
        /// This behaviour may be changed in the future where the <c>end</c> position is preserved by moving the <c>center</c> position if necessary.
        /// </summary>
        /// <param name="start">Starting position of the path.</param>
        /// <param name="end">End position of the path.</param>
        /// <param name="center">Center of the path.</param>
        /// <param name="rotations">
        /// The number of rotations for the path.
        /// A value of 1 produces one partial rotation from start to end following the smallest angle between them.
        /// A value greater than 1 will add additional full rotations around the path in the same direction as the small angle between start and end.
        /// A value of 0 will produce one partial rotation from start to end following the largest angle between them.
        /// A value less than 0 will add additional full rotations in the direction of the large angle between start and end.
        /// </param>
        public CirclePath(Vector3 start, Vector3 end, Vector3 center, int rotations = 1)
        {
            //TODO move center to preserve radius instead of moving end point
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
        }
        
        /// <summary>
        /// Constructs a <c>CirclePath</c> using the given parameters.
        /// Note that the <c>center</c> may be moved to preserve the <c>start</c> and <c>axis</c> of the circle.
        /// </summary>
        /// <param name="start">Starting position of the path.</param>
        /// <param name="center">Center of the circle, may be altered to preserve other parameters.</param>
        /// <param name="axis">Axis of (the plane containing) the cricle. This axis determines the direction of rotation using the right-hand rule. </param>
        /// <param name="rotations">Number of rotations around the circle that the path takes. May be fractional.</param>
        public CirclePath(Vector3 start, Vector3 center, Vector3 axis, float rotations)
        {
            //Move center so that start and axis are preserved
            center += Vector3.Project(start - center, axis); //Vector3.Dot(start - center, axis) * axis;
            _center = center;

            Vector3 relStart = start - center;

            _radius = relStart.magnitude;

            _basis1 = relStart / _radius;
            _basis2 = Vector3.Cross(_basis1, axis).normalized;

            _rotations = rotations;
        }

        //TODO test this more
        /// <summary>
        /// Constructs a <c>CirclePath</c> using the given parameters.
        /// Note, defining a "zero-angle" for arbitrary axes is non-trivial, so the <c>axis</c> directions for this constructor are limited.
        /// </summary>
        /// <param name="center">Center position of the cricle.</param>
        /// <param name="axis">
        /// Axis direction selected from a limited set of directions defined by the <c>AxisDirection</c> enum.
        /// These axis directions have different "zero-angle" positions:
        /// - Up / Down: zero position is (0, 0, -1)
        /// - Left / right: zero position is (0, 1, 0)
        /// - Back / Forward: zero position is (1, 0, 0)
        /// </param>
        /// <param name="startAngle">Starting angle for the circle path. The starting position is rotated from the zero-position by this angle.</param>
        /// <param name="rotations">Number of rotations the path follows starting from the <c>startAngle</c>. May be fractional.</param>
        /// <param name="radius">Radius of the circle.</param>
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

    /// <summary>
    /// Cubic polynomial path.
    /// This path may be replaced by a cubic spline path in the future.
    /// </summary>
    public class CubicPath : Path
    {
        private readonly Vector3 _a;
        private readonly Vector3 _b;
        private readonly Vector3 _c;
        private readonly Vector3 _d;

        /// <summary>
        /// Constructs a cubic path that starts at <c>start</c> and ends at <c>end</c>
        /// </summary>
        /// <param name="start">Start position of the path.</param>
        /// <param name="end">End position of the path.</param>
        /// <param name="startD">Initial derivative used to define the path. If <c>isSecondDerivative</c> is true, this is treated as the second derivative.</param>
        /// <param name="endD">Final derivative used to define the path. If <c>isSecondDerivative</c> is true, this is treated as the second derivative.</param>
        /// <param name="isSecondDerivative">If true the start and end derivative parameters are treated as second derivatives.</param>
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

    /// <summary>
    /// A quadratic Bezier path.
    /// </summary>
	public class QuadraticBezierPath : Path
    {

        private Vector3 _start;
        private Vector3 _end;
        private Vector3 _c;

        /// <summary>
        /// Constructs a quadratic Bezier path using a control point.
        /// </summary>
        /// <param name="start">Start position of the path.</param>
        /// <param name="end">End position of the path/</param>
        /// <param name="controlPoint">Position of the control point used to define the Bezier curve.</param>
        public QuadraticBezierPath(Vector3 start, Vector3 end, Vector3 controlPoint)
        {
            _start = start;
            _end = end;
            _c = controlPoint;
        }

        protected override (Vector3 position, Vector3 direction, Vector3 upDirection) OnGetPositionDirection(
            float pathParam)
        {
            float pathParamInv = 1 - pathParam;

            return (
                pathParamInv * pathParamInv * _start + 2 * pathParamInv * pathParam * _c + pathParam * pathParam * _end,
                - 2 * pathParamInv * _start + 2 * (1 - 2 * pathParam) * _c + 2 * pathParam * _end,
                2 * _start - 4 * _c + 2 * _end
                );
        }
    }

    /// <summary>
    /// A cubic Bezier path.
    /// </summary>
    public class CubicBezierPath : Path
    {
        private Vector3 _start;
        private Vector3 _end;
        private Vector3 _c1;
        private Vector3 _c2;
        

        /// <summary>
        /// Constructs a cubic Bezier path using two control points.
        /// </summary>
        /// <param name="start">Start position of the path.</param>
        /// <param name="end">End position of the path/</param>
        /// <param name="controlPoint1">Position of the first control point used to define the Bezier curve.</param>
        /// <param name="controlPoint2">Position of the second control point used to define the Bezier curve.</param>
        public CubicBezierPath(Vector3 start, Vector3 end, Vector3 controlPoint1, Vector3 controlPoint2)
        {
            _start = start;
            _end = end;
            _c1 = controlPoint1;
            _c2 = controlPoint2;
        }

        protected override (Vector3 position, Vector3 direction, Vector3 upDirection) OnGetPositionDirection(
            float pathParam)
        {
            float pathParamInv = 1 - pathParam;

            return (
                (
                    pathParamInv * pathParamInv * pathParamInv * _start 
                    + 3 * pathParamInv * pathParamInv * pathParam * _c1
                    + 3 * pathParamInv * pathParam * pathParam * _c2
                    + pathParam * pathParam * pathParam * _end
                    ),
                (
                    3 * pathParamInv * pathParamInv * (_c1 - _start)
                    + 6 * pathParamInv * pathParam * (_c2 - _c1)
                    + 3 * pathParam * pathParam * (_end - _c2)
                    ),
                (
                    6 * pathParamInv * (_c2 - 2 * _c1 + _start)
                    + 6 * pathParam * (_end - 2 * _c2 + _c1)
                    )
            );
        }
    }
}