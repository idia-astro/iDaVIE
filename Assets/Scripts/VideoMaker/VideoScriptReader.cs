using System;
using System.Collections.Generic;
using Valve.Newtonsoft.Json.Linq;
using UnityEngine;
using System.Runtime.Remoting.Messaging;


namespace VideoMaker
{
    public class VideoScriptData
    {
        public int Width = 1280;
        public int Height = 720;
        public int FrameRate = 20;
        //TODO add more logo parameters, such as position
        public bool UseLogo = true;
        public float LogoScale = 0.2f;
        public PositionAction[] PositionActions;
        public DirectionAction[] DirectionActions;
        public DirectionAction[] UpDirectionActions;
    }

    public class VideoScriptReader
    {
        const int EasingOrderDefault = 2;

        public static VideoScriptData ReadVideoScript(string videoScriptString)
        {
            VideoScriptData data = new();

            JToken root = JToken.Parse(videoScriptString);

            data.Width = ValueOrDefault(root, "height", data.Height);
            data.Width = ValueOrDefault(root, "width", data.Width);
            data.FrameRate = ValueOrDefault(root, "frameRate", data.FrameRate);
            data.UseLogo = ValueOrDefault(root, "useLogo", data.UseLogo);
            data.LogoScale = ValueOrDefault(root, "logoScale", data.LogoScale);

            List<PositionAction> positions = new();
            List<DirectionAction> directions = new();
            List<DirectionAction> upDirections = new();

            Vector3 positionBefore = Vector3.zero;
            Vector3 directionBefore = Vector3.forward;
            Vector3 upDirectionBefore = Vector3.up;
            JToken transitionData = null;

            foreach (JToken actionData in (JArray)root["actions"])
            {
                if (ValueOrDefault(actionData, "type", "") == "transition")
                {
                    transitionData = actionData;
                    continue;
                }

                var actions = ActionDataToActions(actionData, positionBefore, directionBefore, upDirectionBefore);

                PositionAction positionAction = actions.positions[0];
                Vector3 positionAfter = positionAction.GetPosition(0f);

                DirectionAction directionAction = actions.directions[0];
                Vector3 directionAfter = directionAction.GetDirection(0f, positionAfter);

                directionAction = actions.upDirections[0];
                Vector3 upDirectionAfter = directionAction.GetDirection(0f, positionAfter);

                if (transitionData is not null)
                {
                    var transitionActions = TransitionDataToActions(transitionData,
                        positionBefore, directionBefore, upDirectionBefore,
                        positionAfter, directionAfter, upDirectionAfter);

                    positions.AddRange(transitionActions.positions);
                    directions.AddRange(transitionActions.directions);
                    upDirections.AddRange(transitionActions.upDirections);

                    transitionData = null;
                }

                positions.AddRange(actions.positions);
                directions.AddRange(actions.directions);
                upDirections.AddRange(actions.upDirections);


                positionAction = actions.positions[^1];
                positionBefore = positionAction.GetPosition(positionAction.Duration);

                directionAction = actions.directions[^1];
                directionBefore = directionAction.GetDirection(directionAction.Duration, positionBefore);

                directionAction = actions.upDirections[^1];
                upDirectionBefore = directionAction.GetDirection(directionAction.Duration, positionBefore);
            }

            data.PositionActions = positions.ToArray();
            data.DirectionActions = directions.ToArray();
            data.UpDirectionActions = upDirections.ToArray();

            return data;
        }

        private static T ToObjectOrDefualt<T>(JToken token, T defaultValue)
        {
            Type type = typeof(T);

            if (
                (
                    (type == typeof(int) || type == typeof(float)) &&
                    (token.Type == JTokenType.Integer || token.Type == JTokenType.Float)
                ) || (
                    type == typeof(string) && token.Type == JTokenType.String
                ) || (
                    type == typeof(bool) && token.Type == JTokenType.Boolean
                )
            ) {
                return token.ToObject<T>();
            }

            return defaultValue;
        }

        private static T ValueOrDefault<T>(JToken token, string childPath, T defaultValue)
        {
            JToken childToken = token.SelectToken(childPath);
            if (childToken is null)
            {
                return defaultValue;
            }

            return ToObjectOrDefualt(childToken, defaultValue);
        }

        //TODO return single actions instead of lists? There may not ever be a case where lists are needed
        private static (List<PositionAction> positions, List<DirectionAction> directions, List<DirectionAction> upDirections) ActionDataToActions(JToken actionData,
            Vector3 positionBefore, Vector3 directionBefore, Vector3 upDirectionBefore)
        {
            List<PositionAction> positions = new();
            List<DirectionAction> directions = new();
            List<DirectionAction> upDirections = new();

            float duration = ValueOrDefault(actionData, "duration", 0f);

            JToken positionData = actionData["position"];
            JToken directionData = actionData["lookAt"];
            JToken upDirectionData = actionData["lookUp"];

            Path path = null;
            if (positionData is not null)
            {
                path = DataToPath(positionData, positionBefore, positionBefore);
                Vector3? position = DataToPosition(positionData);

                if (path is not null)
                {
                    positions.Add(new PositionActionPath(duration, path));
                }
                else if (position is not null)
                {
                    positions.Add(new PositionActionHold(duration, (Vector3)position));
                }
                else
                {
                    positions.Add(new PositionActionHold(duration, positionBefore));
                }
            }
            else
            {
                positions.Add(new PositionActionHold(duration, positionBefore));
            }

            directions.Add(DirectionActionFromData(directionData, duration, path, directionBefore, false));
            upDirections.Add(DirectionActionFromData(upDirectionData, duration, path, upDirectionBefore, true));

            return (positions, directions, upDirections);
        }

        private static DirectionAction DirectionActionFromData(JToken data, float duration, Path path, Vector3 defualtDirection,
            bool pathUpDirection, float startTimeOffset = 0f, float endTimeOffset = 0f)
        {
            if (data is not null)
            {
                switch (ToObjectOrDefualt(data, ""))
                {
                    case "alongPath":
                        return new DirectionActionPath(duration, path, startTimeOffset: startTimeOffset, endTimeOffset: endTimeOffset);
                    case "alongPathReverse":
                        return new DirectionActionPath(duration, path, invert: true, startTimeOffset: startTimeOffset, endTimeOffset: endTimeOffset);
                    case "upFromPath":
                        return new DirectionActionPath(duration, path, useUpDirection: true, startTimeOffset: startTimeOffset, endTimeOffset: endTimeOffset);
                    case "upFromPathReverse":
                        return new DirectionActionPath(duration, path, useUpDirection: true, invert: true, startTimeOffset: startTimeOffset, endTimeOffset: endTimeOffset);

                }

                Vector3? direction = DataToDirection(data);

                if (direction is not null)
                {
                    return new DirectionActionHold(duration: duration, direction: (Vector3)direction);
                }

                Vector3? position = DataToPosition(data);

                if (position is not null)
                {
                    return new DirectionActionLookAt(duration: duration, target: (Vector3)position);
                }

                // Defualt: along path or looking forward
                if (path is null)
                {
                    return new DirectionActionHold(duration, defualtDirection);
                }
                else
                {
                    return new DirectionActionPath(duration, path, useUpDirection: pathUpDirection, startTimeOffset: startTimeOffset, endTimeOffset: endTimeOffset);
                }
            }
            else
            {
                return new DirectionActionHold(duration, defualtDirection);
            }
        }

        private static (List<PositionAction> positions, List<DirectionAction> directions, List<DirectionAction> upDirections) TransitionDataToActions(JToken transitionData,
            Vector3 positionBefore, Vector3 directionBefore, Vector3 upDirectionBefore,
            Vector3 positionAfter, Vector3 directionAfter, Vector3 upDirectionAfter)
        {
            List<PositionAction> positions = new();
            List<DirectionAction> directions = new();
            List<DirectionAction> upDirections = new();

            JToken lookStartData = transitionData["lookStart"];
            float startDuration = 0.0f;
            bool startOverlap = false;

            if (lookStartData is not null)
            {
                startDuration = ValueOrDefault(lookStartData, "duration", 0f);
                startOverlap = ValueOrDefault(lookStartData, "overlapWithTravel", false);
            }

            JToken lookEndData = transitionData["lookEnd"];
            float endDuration = 0.0f;
            bool endOverlap = false;

            if (lookEndData is not null)
            {
                endDuration = ValueOrDefault(lookEndData, "duration", 0f);
                endOverlap = ValueOrDefault(lookEndData, "overlapWithTravel", false);
            }

            JToken travelData = transitionData["travel"];

            Vector3 travelDirectionStart = directionAfter;
            Vector3 travelUpDirectionStart = upDirectionAfter;
            Vector3 travelDirectionEnd = directionBefore;
            Vector3 travelUpDirectionEnd = upDirectionBefore;

            if (travelData is not null)
            {
                float duration = ValueOrDefault(travelData, "duration", 0f);

                JToken travelPositionData = travelData["position"];
                Path path = travelPositionData is not null ? DataToPath(travelPositionData, positionBefore, positionAfter) : null;
                path ??= new LinePath(positionBefore, positionAfter, new EasingInOut(order: EasingOrderDefault));

                PositionAction positionAction = new PositionActionPath(duration, path);
                positions.Add(positionAction);

                float directionDuration = duration;
                float startTimeOffset = 0f;
                if (startOverlap)
                {
                    directionDuration -= startDuration;
                    startTimeOffset = startDuration;
                }

                float endTimeOffset = 0f;
                if (endOverlap)
                {
                    directionDuration -= endDuration;
                    endTimeOffset = endDuration;
                }

                Vector3 positionStart = positionAction.GetPosition(startTimeOffset);
                Vector3 positionEnd = positionAction.GetPosition(duration - endTimeOffset);

                //TODO there is something wrong with this! Maybe even the method
                DirectionAction directionAction = DirectionActionFromData(travelData["lookAt"], directionDuration, path, directionBefore, false, startTimeOffset, endTimeOffset);
                directions.Add(directionAction);

                travelDirectionStart = directionAction.GetDirection(0f, positionStart);
                travelDirectionEnd = directionAction.GetDirection(duration - endTimeOffset, positionEnd);

                DirectionAction upDirectionAction = DirectionActionFromData(travelData["LookUp"], directionDuration, path, upDirectionBefore, true, startTimeOffset, endTimeOffset);

                upDirections.Add(upDirectionAction);

                travelUpDirectionStart = upDirectionAction.GetDirection(0f, positionStart);
                travelUpDirectionEnd = upDirectionAction.GetDirection(duration - endTimeOffset, positionEnd);
            }

            //TODO eliminate repitition. Use a function?
            if (lookStartData is not null)
            {
                JToken easingData = lookStartData["easing"];
                Easing easing = easingData is not null ? DataToEasing(easingData) : null;

                if (!startOverlap)
                {
                    positions.Add(new PositionActionHold(duration: startDuration, position: positionBefore));
                }

                directions.Insert(0, new DirectionActionTween(duration: startDuration,
                    directionFrom: directionBefore, directionTo: travelDirectionStart, easing: easing));
                upDirections.Insert(0, new DirectionActionTween(duration: startDuration,
                    directionFrom: upDirectionBefore, directionTo: travelUpDirectionStart, easing: easing));
            }

            if (lookEndData is not null)
            {
                JToken easingData = lookEndData["easing"];
                Easing easing = easingData is not null ? DataToEasing(easingData) : null;
                
                if (!endOverlap)
                {
                    positions.Add(new PositionActionHold(duration: endDuration, position: positionAfter));
                }

                directions.Add(new DirectionActionTween(duration: endDuration,
                    directionFrom: travelDirectionEnd, directionTo: directionAfter, easing: easing));
                upDirections.Add(new DirectionActionTween(duration: endDuration,
                    directionFrom: travelUpDirectionEnd, directionTo: upDirectionAfter, easing: easing));
            }

            return (positions, directions, upDirections);
        }

        private static Vector3? DataToPosition(JToken positionData)
        {
            if (ToObjectOrDefualt(positionData, "") == "center")
            {
                return Vector3.zero;
            }

            if (ValueOrDefault(positionData, "type", "") != "position")
            {
                return null;
            }

            //TODO Add subtype parameters: WCS, source, etc
            return new Vector3(
                ValueOrDefault(positionData, "x", 0f),
                ValueOrDefault(positionData, "y", 0f),
                ValueOrDefault(positionData, "z", 0f)
            );
        }

        private static Vector3? DataToDirection(JToken directionData)
        {
            //TODO move named directions a level higher? (Implement in DirectionActionFromData)
            return ToObjectOrDefualt(directionData, "") switch
            {
                "up" => Vector3.up,
                "down" => Vector3.down,
                "left" => Vector3.left,
                "right" => Vector3.right,
                "forward" => Vector3.forward,
                "back" => Vector3.back,
                //TODO normalise and set to forward if 0 magnitude?
                _ => ValueOrDefault(directionData, "type", "") == "direction" ? new Vector3(
                                ValueOrDefault(directionData, "x", 0f),
                                ValueOrDefault(directionData, "y", 0f),
                                ValueOrDefault(directionData, "z", 0f)
                            ) : null
            };
        }

        //TODO re-write both this and the accelDecel (generic inOut)
        private static Easing DataToEasing(JToken easingData)
        {
            return ValueOrDefault(easingData, "easing", "") switch
            {
                "in" => new EasingIn(ValueOrDefault(easingData, "order", EasingOrderDefault)),
                "out" => new EasingOut(ValueOrDefault(easingData, "order", EasingOrderDefault)),
                "inOut" => new EasingInOut(ValueOrDefault(easingData, "order", EasingOrderDefault)),
                "accelDecel" => new EasingAccelDecel(ValueOrDefault(easingData, "timeAccel", 0.0f),
                                        ValueOrDefault(easingData, "timeDecel", 1.0f)),
                _ => null,
            };
        }

        private static Path DataToPath(JToken pathData, Vector3 startPosition, Vector3 endPosition)
        {
            if (ValueOrDefault(pathData, "type", "") != "path")
            {
                return null;
            }
            JToken startPosToken = pathData.SelectToken("startPosition");
            startPosition = startPosToken is not null ? DataToPosition(startPosToken) ?? startPosition : startPosition;

            JToken endPosToken = pathData.SelectToken("endPosition");
            endPosition = endPosToken is not null ? DataToPosition(endPosToken) ?? endPosition : endPosition;

            JToken easingToken = pathData.SelectToken("easing");
            Easing easing = easingToken is not null ? DataToEasing(easingToken) : null;

            //TODO change to "name". Also whether startPosition and endPositions are defined or not is signifigant for some paths, it should be indicated



            switch (ValueOrDefault(pathData, "name", ""))
            {
                // "line" => new LinePath(startPosition, endPosition, easing),
                case "circle":
                    CirclePath.AxisDirection axisDirection = ValueOrDefault(pathData, "axis", "") switch
                    {
                        "up" => CirclePath.AxisDirection.Up,
                        "down" => CirclePath.AxisDirection.Down,
                        "left" => CirclePath.AxisDirection.Left,
                        "right" => CirclePath.AxisDirection.Right,
                        "forward" => CirclePath.AxisDirection.Forward,
                        "back" => CirclePath.AxisDirection.Back,
                        _ => CirclePath.AxisDirection.None
                    };

                     // TODO complain if nothing given here
                    Vector3 center = pathData.SelectToken("centerPosition") is not null ? DataToPosition(pathData["centerPosition"]) ?? Vector3.zero : Vector3.zero;

                    if (axisDirection != CirclePath.AxisDirection.None && pathData["startAngle"] is not null)
                    {
                        return new CirclePath(
                            center: center,
                            axis: axisDirection,
                            startAngle: ValueOrDefault(pathData, "startAngle", 0f),
                            rotations: ValueOrDefault(pathData, "rotations", 1f),
                            radius: ValueOrDefault(pathData, "radius", 1f),
                            easing: easing
                        );
                    }

                    Vector3? axis = pathData["axis"] is not null ? DataToDirection(pathData["axis"]) : null;

                    //Check is not robust enough for more circle options
                    if (axis is not null)
                    {
                        return new CirclePath(
                            startPosition: startPosition,
                            axis: (Vector3)axis,
                            center: center,
                            rotations: ValueOrDefault(pathData, "rotations", 1f),
                            easing: easing
                        );
                    }

                        return new CirclePath(
                                //TODO Make this statement more efficient. I still want a one-liner if possible
                                startPosition: startPosition,
                                endPosition: endPosition,
                                center: center,
                                largeAngleDirection: ValueOrDefault(pathData, "largeAngleDirection", false),
                                additionalRotations: ValueOrDefault(pathData, "additionalRotations", 0),
                                easing: easing
                            );
                    
                default:
                    return new LinePath(startPosition, endPosition, easing);
            }
        }
    }
}