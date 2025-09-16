using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml.Schema;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;


namespace VideoMaker
{
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

    public static class VideoScriptReader
    {
        private static readonly videoLocation DefaultLocation = new videoLocation("", Vector3.zero, Vector3.zero);
        
        public static VideoScriptData ReadIDVSVideoScript(StreamReader videoIDVSScriptStream, string filePath)
        {
            VideoScriptData data = new();
            IDVSParser parser = new();
            (videoSettings settings, List<videoLocation> locations, List<object> commands) = parser.Parse(videoIDVSScriptStream, filePath);

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
            
            videoLocation locationPrevious = ((startCommand)commands[0])?.position ?? DefaultLocation;

            Easing easing = new EasingInOut(2);
            
            for (int i = 1; i < commands.Count; i++)
            {
                switch (commands[i])
                {
                    case waitCommand command:
                        positionActions.Add(new PositionActionHold(locationPrevious.position){Duration = command.duration});
                        directionActions.Add(new DirectionActionHold(locationPrevious.forward){Duration = command.duration});
                        upDirectionActions.Add(new DirectionActionHold(locationPrevious.up){Duration = command.duration});
                        data.Duration += command.duration;
                        break;
                    case moveCommand command:
                        Path path;
                        switch (command.method)
                        {
                            case Method.LINE:
                                path = new LinePath(locationPrevious.position, command.destination.position){Easing = easing};
                                break;
                            case Method.ARC:
                                //Double-derivative at boundaries (magnitude)
                                // float dy = - (
                                //     Mathf.Sign(Vector3.Dot(locationPrevious.forward, 
                                //         command.destination.forward)) * 
                                //     (command.destination.position - locationPrevious.position).magnitude 
                                //     / command.duration);

                                float dy = (command.destination.position - locationPrevious.position).magnitude;
                                
                                path = new CubicPath(
                                    locationPrevious.position, command.destination.position,
                                    dy * locationPrevious.forward, dy * command.destination.forward);
                                break;
                            default:
                                continue;
                        }

                        positionActions.Add(new PositionActionPath(path){Duration = command.duration});
                        directionActions.Add(new DirectionActionTween(
                            locationPrevious.forward, command.destination.forward, easing: easing){Duration = command.duration});
                        upDirectionActions.Add(new DirectionActionTween(
                            locationPrevious.up, command.destination.up, easing: easing){Duration = command.duration});
                        locationPrevious = command.destination;
                        data.Duration += command.duration;
                        break;
                    case rotateCommand command:
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
                        
                        //Turn to face center location
                        positionActions.Add(new PositionActionHold(locationPrevious.position){Duration = command.turnDuration});
                        directionActions.Add(new DirectionActionTween(locationPrevious.forward, 
                            (command.centrePoint.position - locationPrevious.position)){Duration = command.turnDuration});
                        upDirectionActions.Add(new DirectionActionTween(locationPrevious.up, axis){Duration = command.turnDuration});
                        
                        //Orbit
                        positionActions.Add(new PositionActionPath(new CirclePath(
                            start: locationPrevious.position, 
                            center: command.centrePoint.position, 
                            axis: axis, 
                            rotations: command.iterations
                            ){Easing = easing}){Duration = command.orbitDuration});
                        directionActions.Add(new DirectionActionLookAt(command.centrePoint.position){Duration = command.orbitDuration});
                        upDirectionActions.Add(new DirectionActionHold(axis){Duration = command.orbitDuration});

                        (Vector3 position, Vector3 alongPath, Vector3 upPath) =
                            positionActions[^1].GetPositionDirection(command.orbitDuration);
                        
                        locationPrevious = new videoLocation("",
                            position,
                            Quaternion.LookRotation(
                                directionActions[^1].GetDirection(command.orbitDuration, position, alongPath, upPath),
                                upDirectionActions[^1].GetDirection(command.orbitDuration, position, alongPath, upPath)
                                ).eulerAngles
                        );
                        data.Duration += command.turnDuration + command.orbitDuration;
                        break;
                }
            }

            data.PositionActions = positionActions.ToArray();
            data.DirectionActions = directionActions.ToArray();
            data.UpDirectionActions = directionActions.ToArray();
            
            return data;
        }
    }
}