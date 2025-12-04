using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml.Schema;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

namespace VideoMaker
{
    /// <summary>
    /// This class contains all the information needed for the VideoCameraController to create the video.
    /// It is intended to be instantiated using the methods in the VideoScriptReader class.
    /// </summary>
    public class VideoScriptData
    {
        //TODO define default values as constants for videSettings to use. Later use defaults from schema.
        public enum LogoPosition
        {
            BottomRight,
            BottomCenter,
            BottomLeft,
            CenterRight,
            CenterCenter,
            CenterLeft,
            TopRight,
            TopCenter,
            TopLeft,
            Invalid
        }

        public int Width = 1280;
        public int Height = 720;
        public int FrameRate = 20;
        public float LogoScale = 0.2f;
        public LogoPosition logoPosition = LogoPosition.BottomRight;
        public float Duration;
        public PositionAction[] PositionActions;
        public DirectionAction[] DirectionActions;
        public DirectionAction[] UpDirectionActions;
        
        /// <summary>
        /// This function returns the appropriate LogoPosition corresponding the given value from a video script.
        /// </summary>
        /// <param name="pos">Value for the position as defined in a video script.</param>
        /// <returns>LogoPosition enum value for the appropriate logo position. The defualt return is LogoPosition.Invalid if the given position is not appropriate.</returns>
        public static LogoPosition ParsePosition(string pos)
        {
            if (pos.Equals("BR", StringComparison.OrdinalIgnoreCase) || 
                pos.Equals("BottomR", StringComparison.OrdinalIgnoreCase) || 
                pos.Equals("BRight", StringComparison.OrdinalIgnoreCase) ||
                pos.Equals("BottomRight", StringComparison.OrdinalIgnoreCase) || 
                pos.Equals("RB", StringComparison.OrdinalIgnoreCase) || 
                pos.Equals("RightBottom", StringComparison.OrdinalIgnoreCase) || 
                pos.Equals("RBottom", StringComparison.OrdinalIgnoreCase) || 
                pos.Equals("RightB", StringComparison.OrdinalIgnoreCase))

                return LogoPosition.BottomRight;

            if (pos.Equals("BL", StringComparison.OrdinalIgnoreCase) ||
                pos.Equals("BottomL", StringComparison.OrdinalIgnoreCase) ||
                pos.Equals("BLeft", StringComparison.OrdinalIgnoreCase) ||
                pos.Equals("BottomLeft", StringComparison.OrdinalIgnoreCase) ||
                pos.Equals("LB", StringComparison.OrdinalIgnoreCase) ||
                pos.Equals("LeftBottom", StringComparison.OrdinalIgnoreCase) ||
                pos.Equals("LBottom", StringComparison.OrdinalIgnoreCase) ||
                pos.Equals("LeftB", StringComparison.OrdinalIgnoreCase))

                return LogoPosition.BottomLeft;

            if (pos.Equals("TR", StringComparison.OrdinalIgnoreCase) || 
                pos.Equals("TopR", StringComparison.OrdinalIgnoreCase) || 
                pos.Equals("TRight", StringComparison.OrdinalIgnoreCase) ||
                pos.Equals("TopRight", StringComparison.OrdinalIgnoreCase) || 
                pos.Equals("RT", StringComparison.OrdinalIgnoreCase) || 
                pos.Equals("RightTop", StringComparison.OrdinalIgnoreCase) || 
                pos.Equals("RTop", StringComparison.OrdinalIgnoreCase) || 
                pos.Equals("RightT", StringComparison.OrdinalIgnoreCase))

                return LogoPosition.TopRight;

            if (pos.Equals("TL", StringComparison.OrdinalIgnoreCase) ||
                pos.Equals("TopL", StringComparison.OrdinalIgnoreCase) ||
                pos.Equals("TLeft", StringComparison.OrdinalIgnoreCase) ||
                pos.Equals("TopLeft", StringComparison.OrdinalIgnoreCase) ||
                pos.Equals("LT", StringComparison.OrdinalIgnoreCase) ||
                pos.Equals("LeftTop", StringComparison.OrdinalIgnoreCase) ||
                pos.Equals("LTop", StringComparison.OrdinalIgnoreCase) ||
                pos.Equals("LeftT", StringComparison.OrdinalIgnoreCase))

                return LogoPosition.TopLeft;

            return LogoPosition.Invalid;
        }
    }

    /// <summary>
    /// This class contains methods used to read video scripts (for now only IDVS, but with the addition of JSON in the future) and return a corresponding VideoScriptData instance.
    /// </summary>
    public static class VideoScriptReader
    {
        private static readonly VideoLocation DefaultLocation = new VideoLocation("", Vector3.zero, Vector3.zero);

        private static readonly Easing EasingIO = new EasingInOut(order: 2);
        
        /// <summary>
        /// Read the data from an IDVS stream and return a corresponding VideoScriptData instance.
        /// This includes constructing VideoMaker.Action instances from the IDVS commands.
        /// </summary>
        /// <param name="videoIdvsScriptStream">Stream for the IDVS file.</param>
        /// <param name="filePath">Path to the IDVS file.</param>
        /// <returns>VideoScriptData instance with data constructed using the IDVS file.</returns>
        public static VideoScriptData ReadIdvsVideoScript(StreamReader videoIdvsScriptStream, string filePath)
        {
            VideoScriptData data = new();
            IdvsParser parser = new();
            (VideoSettings settings, List<VideoLocation> locations, List<object> commands) = parser.Parse(videoIdvsScriptStream, filePath);

            data.logoPosition = settings.logoPos;
            data.Height = settings.height;
            data.Width = settings.width;
            data.FrameRate = settings.framerate;
            
            if (commands.Count == 0)
            {
                return data;
            }

            List<PositionAction> positionActions = new List<PositionAction>();
            List<DirectionAction> directionActions = new List<DirectionAction>();
            List<DirectionAction> upDirectionActions = new List<DirectionAction>();
            
            VideoLocation locationPrevious = ((StartCommand)commands[0])?.position ?? DefaultLocation;
            
            for (int i = 1; i < commands.Count; i++)
            {
                switch (commands[i])
                {
                    case WaitCommand command:
                        positionActions.Add(new PositionActionHold(locationPrevious.position){Duration = command.duration});
                        directionActions.Add(new DirectionActionHold(locationPrevious.forward){Duration = command.duration});
                        upDirectionActions.Add(new DirectionActionHold(locationPrevious.up){Duration = command.duration});
                        data.Duration += command.duration;
                        break;
                    case MoveCommand command:
                        Path path;
                        switch (command.method)
                        {
                            case MovementMethod.Line:
                                path = new LinePath(locationPrevious.position, command.destination.position){Easing = EasingIO};
                                break;
                            case MovementMethod.Arc:
                                Vector3 start = locationPrevious.position;
                                Vector3 startDir = locationPrevious.forward;
                                Vector3 end = command.destination.position;
                                Vector3 endDir = command.destination.forward;
                                
                                //Determining closest points along start and end directions
                                float dirDot = Vector3.Dot(startDir, endDir);

                                float endL = Vector3.Dot(dirDot * startDir - endDir, end - start) /
                                             (1 - dirDot * dirDot);
                                float startL = endL * dirDot + Vector3.Dot(startDir, end - start);
                                
                                //Use closest points to determine the control point for the quad-bezier curve:
                                // - Take halfway (chosen to reduce "sharpness" of curve) to midpoint between closest points
                                // - Use oposite of this point as the control point
                                path = new QuadraticBezierPath(
                                    start: start,
                                    end: end,
                                    controlPoint: 0.5f * ( start + end) - 0.25f *(endL * endDir + startL * startDir)
                                ){Easing = EasingIO};
                                
                                break;
                            default:
                                continue;
                        }
                        positionActions.Add(new PositionActionPath(path){Duration = command.duration});
                        directionActions.Add(new DirectionActionTween(
                            locationPrevious.forward, command.destination.forward, easing: EasingIO){Duration = command.duration});
                        upDirectionActions.Add(new DirectionActionTween(
                            locationPrevious.up, command.destination.up, easing: EasingIO){Duration = command.duration});
                        locationPrevious = command.destination;
                        data.Duration += command.duration;
                        break;
                    case RotateCommand command:
                        //Using global space axes for rotation
                        Vector3 axis = command.rotationAxis switch
                        {
                            RotationAxes.PosX => Vector3.right,
                            RotationAxes.NegX => Vector3.left,
                            RotationAxes.PosY => Vector3.up,
                            RotationAxes.NegY => Vector3.down,
                            RotationAxes.PosZ => Vector3.forward,
                            RotationAxes.NegZ => Vector3.back,
                            _ => Vector3.up //TODO Rather skip?
                        };
                        // Vector3 axis = command.centrePoint.up;
                        
                        //Turn to face center location
                        positionActions.Add(new PositionActionHold(locationPrevious.position){Duration = command.turnDuration});
                        directionActions.Add(new DirectionActionTween(
                                locationPrevious.forward, 
                                (command.centrePoint.position - locationPrevious.position),
                                easing: EasingIO
                            ){Duration = command.turnDuration});
                        upDirectionActions.Add(new DirectionActionTween(
                            locationPrevious.up, axis, easing: EasingIO){Duration = command.turnDuration});
                        
                        //Orbit
                        float orbitDuration = command.orbitDuration * command.iterations;
                        
                        positionActions.Add(new PositionActionPath(new CirclePath(
                            start: locationPrevious.position, 
                            center: command.centrePoint.position, 
                            axis: axis, 
                            rotations: command.iterations
                            ){Easing = new EasingInLinOut(
                            order: 2, 
                            timeIn: command.iterations < 1f ? 0.25f : 0.25f / command.iterations, 
                            timeOut: command.iterations < 1f ? 0.25f : 0.25f / command.iterations)
                        }){Duration = orbitDuration});
                        directionActions.Add(new DirectionActionLookAt(command.centrePoint.position){Duration = orbitDuration});
                        upDirectionActions.Add(new DirectionActionHold(axis){Duration = orbitDuration});

                        (Vector3 position, Vector3 alongPath, Vector3 upPath) =
                            positionActions[^1].GetPositionDirection(orbitDuration);
                        
                        locationPrevious = new VideoLocation("",
                            position,
                            Quaternion.LookRotation(
                                directionActions[^1].GetDirection(orbitDuration, position, alongPath, upPath),
                                upDirectionActions[^1].GetDirection(orbitDuration, position, alongPath, upPath)
                                ).eulerAngles
                        );
                        data.Duration += command.turnDuration + orbitDuration;
                        break;
                }
            }

            data.PositionActions = positionActions.ToArray();
            data.DirectionActions = directionActions.ToArray();
            data.UpDirectionActions = upDirectionActions.ToArray();
            
            return data;
        }
    }
}