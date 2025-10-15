using System;
using System.Collections.Generic;
using System.Data;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using UnityEngine;


namespace VideoMaker
{
    //TODO consider how to generate the actual schema from this class in order to reduce book keeping
    //note that the class members should remain static and constant for reference in scripts
    public static class SchemaDefs
    {
        public static class Logo
        {
            public static class Position
            {
                public const string Def = "logoPosition";
                public const string Default = BottomRight;
                public const string BottomRight = "bottomRight";
                public const string BottomCenter = "bottomCenter";
                public const string BottomLeft = "bottomLeft";
                public const string CenterRight = "centerRight";
                public const string Center = "center";
                public const string CenterCenter = "centerCenter";
                public const string CenterLeft = "centerLeft";
                public const string TopRight = "topRight";
                public const string TopCenter = "topCenter";
                public const string TopLeft = "topLeft";
            }
        }

        public static class Continue
        {
            public const string Def = "continue";
            public const string Value = "continue";
        }

        public static class Relative
        {
            public const string Def = "relativeAction";
            public const string First = "first";
            public const string Previous = "previous";
            public const string Next = "next";
        }

        public static class NamedDirection
        {
            public const string Def = "namedDirection";
            public const string Forward = "forward";
            public const string Back = "back";
            public const string Left = "left";
            public const string Right = "right";
            public const string Up = "up";
            public const string Down = "down";
        }

        public static class PathDirection
        {
            public const string Def = "pathDirection";
            public const string Along = "alongPath";
            public const string Up = "upPath";
        }

        public static class Direction
        {
            public const string Def = "direction";
            public const string Type = "direction";
        }

        public static class Rotation
        {
            public const string Def = "rotation";
            public const string Type = "rotation";
        }

        public static class NamedPosition
        {
            public const string Def = "namedPosition";
            public const string Center = "center";
        }

        public static class Position
        {
            public const string Def = "position";
            public const string Type = "position";
        }


        public static class Easing
        {
            public const string Def = "easing";
            public const string Type = "easing";
            public const string Linear = "linear";
            public const string In = "in";
            public const string Out = "out";
            public const string InOut = "inOut";

            public const string Order = "order";
            public const int OrderDefault = 2;
        }

        public static class DirectionBetween
        {
            public const string Def = "directionBetween";
            public const string Type = "lookBetween";

            public const string Start = "start";
            public const string StartDefault = Relative.Previous;
            public const string End = "end";
            public const string EndDefault = Relative.Next;
            public const string Easing = "easing";
        }

        public static class RotationBetween
        {
            public const string Def = "rotateBetween";
            public const string Type = "rotateBetween";

            public const string Start = "start";
            public const string StartDefault = Relative.Previous;
            public const string End = "end";
            public const string EndDefault = Relative.Next;
            public const string Easing = "easing";
        }

        public static class Path
        {
            public const string Def = "path";
            public const string Type = "path";
            public const string KindDefault = Line.Kind;

            public const string Start = "start";
            public const string StartDefault = Relative.Previous;
            public const string End = "end";
            public const string EndDefault = Relative.Next;

            public const string Easing = "easing";

            public static class Line
            {
                public const string Kind = "line";
            }

            public static class Circle
            {
                public const string Kind = "circle";
                public const string Center = "center";
                public const string CenterDefault = "center";

                public const string Axis = "axis";
                public const string AxisDefault = "up";

                public static class StartCenterEnd
                {
                    public const int Index = 0;
                    public const string Rotations = "rotations";
                    public const int RotationsDefault = 1;
                }
                public static class StartCenterAxis
                {
                    public const int Index = 1;
                    public const string Rotations = "rotations";
                    public const float RotationsDefault = 1f;
                }
            }

            public static class Spiral
            {
                public const string Kind = "spiral";

                public static class StartCenterEnd
                {
                    public const int Index = 0;
                }
                public static class StartCenterAxis
                {
                    public const int Index = 1;
                }
            }

            //TODO Replace with cubic Spline
            public static class Cubic
            {
                public const string Kind = "cubic";

                public const string StartDirection = "startDirection";
                public const string StartDirectionDefault = "previous";
                // public const string StartScale = "startScale";
                // public const float StartScaleDefault = 1;

                public const string EndDirection = "endDirection";
                public const string EndDirectionDefault = "next";
                // public const string EndScale = "endScale";
                // public const float EndScaleDefault = 1;
            }
        }

        public static class Action
        {
            public const string Def = "action";
            public const string Duration = "duration";

            public const string Position = "position";
            public const string PositionDefault = Continue.Value;

            public const string LookAt = "lookAt";
            public const string LookAtDefault = Continue.Value;
            public const string LookAway = "lookAway";

            public const string LookUp = "lookUp";
            public const string LookUpDefault = Continue.Value;
            public const string LookDown = "lookDown";

            public const string Rotation = "rotation";
        }
    }
}