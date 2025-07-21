using System;
using System.Collections.Generic;
using Valve.Newtonsoft.Json.Linq;
// using Newtonsoft.Json.Linq;
using UnityEngine;


namespace VideoMaker
{
    public class VideoScriptData
    {
        public int FrameRate = 20;
        //TODO add more logo parameters, such as position
        public bool UseLogo = true;
        public float LogoScale = 0.2f;
        public VideoPositionAction[] PositionActions;
        public VideoDirectionAction[] DirectionActions;
        public VideoDirectionAction[] UpDirectionActions;
    }

    public class VideoScriptReader
    {
        const int EasingOrderDefault = 2;

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

        public static VideoScriptData ReadVideoScript(string videoScriptString)
        {
            VideoScriptData data = new();

            JToken root = JToken.Parse(videoScriptString);

            data.FrameRate = ValueOrDefault(root, "frameRate", data.FrameRate);
            data.UseLogo = ValueOrDefault(root, "useLogo", data.UseLogo);
            data.LogoScale = ValueOrDefault(root, "logoScale", data.LogoScale);

            List<VideoPositionAction> positions = new();
            List<VideoDirectionAction> directions = new();
            List<VideoDirectionAction> upDirections = new();

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

                VideoPositionAction positionAction = actions.positions[0];
                Vector3 positionAfter = positionAction.GetPosition(0f);

                VideoDirectionAction directionAction = actions.directions[0];
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

        //TODO return single actions instead of lists? There may not ever be a case where lists are needed
        public static (List<VideoPositionAction> positions, List<VideoDirectionAction> directions, List<VideoDirectionAction> upDirections) ActionDataToActions(JToken actionData,
            Vector3 positionBefore, Vector3 directionBefore, Vector3 upDirectionBefore)
        {
            List<VideoPositionAction> positions = new();
            List<VideoDirectionAction> directions = new();
            List<VideoDirectionAction> upDirections = new();

            float duration = ValueOrDefault(actionData, "duration", 0f);

            JToken positionData = actionData["position"];
            JToken directionData = actionData["lookAt"];
            JToken upDirectionData = actionData["lookUp"];

            VideoCameraPath path = null;
            if (positionData is not null)
            {
                switch (ValueOrDefault(positionData, "type", ""))
                {
                    case "path":
                        path = DataToPath(positionData, positionBefore, positionBefore);
                        positions.Add(new VideoPositionActionPath(duration, path));
                        break;
                    case "position":
                        positions.Add(new VideoPositionActionHold(duration, DataToPosition(positionData)));
                        break;
                    default:
                        positions.Add(new VideoPositionActionHold(duration, positionBefore));
                        break;
                }
            }
            else
            {
                positions.Add(new VideoPositionActionHold(duration, positionBefore));
            }

            directions.Add(DirectionActionFromData(directionData, duration, path, directionBefore, false));
            upDirections.Add(DirectionActionFromData(upDirectionData, duration, path, upDirectionBefore, true));

            return (positions, directions, upDirections);
        }

        public static VideoDirectionAction DirectionActionFromData(JToken data, float duration, VideoCameraPath path, Vector3 defualtDirection, bool pathUpDirection)
        {
            if (data is not null)
            {
                //TDOO check for strings alongPath, alongPathReverse, upFromPath, upFromPathReverse
                // switch (data.Type == JTokenType.String ? data.ToObject<string>() : "") //More efficient, but less sustainable
                switch (ToObjectOrDefualt(data, ""))
                {
                    case "alongPath":
                        return new VideoDirectionActionPath(duration, path);
                    case "alongPathReverse":
                        return new VideoDirectionActionPath(duration, path, invert: true);
                    case "upFromPath":
                        return new VideoDirectionActionPath(duration, path, useUpDirection: true);
                    case "upFromPathReverse":
                        return new VideoDirectionActionPath(duration, path, useUpDirection: true, invert: true);

                }
                switch (ValueOrDefault(data, "type", ""))
                {
                    case "direction":
                        return new VideoDirectionActionHold(duration: duration, direction: DataToDirection(data));
                    case "position":
                        return new VideoDirectionActionLookAt(duration: duration, target: DataToPosition(data));
                    //TODO case "path":

                    default:
                        //Wildcard along path or looking forward
                        if (path is null)
                        {
                            return new VideoDirectionActionHold(duration, defualtDirection);
                        }
                        else
                        {
                            return new VideoDirectionActionPath(duration, path, useUpDirection: pathUpDirection);
                        }
                }
            }
            else
            {
                return new VideoDirectionActionHold(duration, defualtDirection);
            }
        }

        public static (List<VideoPositionAction> positions, List<VideoDirectionAction> directions, List<VideoDirectionAction> upDirections) TransitionDataToActions(JToken transitionData,
            Vector3 positionBefore, Vector3 directionBefore, Vector3 upDirectionBefore,
            Vector3 positionAfter, Vector3 directionAfter, Vector3 upDirectionAfter)
        {
            List<VideoPositionAction> positions = new();
            List<VideoDirectionAction> directions = new();
            List<VideoDirectionAction> upDirections = new();

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

                //TODO check for path and then use DataToPath with start and end positions
                // JToken pathData = travelData["path"];
                VideoCameraPath path = new LinePath(positionBefore, positionAfter, new EasingInOut(order: EasingOrderDefault));
                VideoPositionAction positionAction = new VideoPositionActionPath(duration, path);
                positions.Add(positionAction);

                //TODO lookAt and LookUp options. For now going with default - along path
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

                VideoDirectionAction directionAction = new VideoDirectionActionPath(
                    duration: directionDuration,
                    path: path,
                    startTimeOffset: startTimeOffset, endTimeOffset: endTimeOffset
                );
                directions.Add(directionAction);

                travelDirectionStart = directionAction.GetDirection(0f, positionStart);
                travelDirectionEnd = directionAction.GetDirection(duration - endTimeOffset, positionEnd);

                VideoDirectionAction upDirectionAction = new VideoDirectionActionPath(
                    duration: directionDuration,
                    path: path,
                    startTimeOffset: startTimeOffset, endTimeOffset: endTimeOffset,
                    useUpDirection: true
                );
                upDirections.Add(upDirectionAction);

                travelUpDirectionStart = upDirectionAction.GetDirection(0f, positionStart);
                travelUpDirectionEnd = upDirectionAction.GetDirection(duration - endTimeOffset, positionEnd);
            }


            if (lookStartData is not null)
            {
                JToken easingData = lookStartData["easing"];
                VideoEasing easing = null;
                if (easingData is not null)
                {
                    easing = DataToEasing(easingData);
                }

                if (!startOverlap)
                {
                    positions.Add(new VideoPositionActionHold(duration: endDuration, position: positionBefore));
                }

                directions.Insert(0, new VideoDirectionActionTween(duration: startDuration,
                    directionFrom: directionBefore, directionTo: travelDirectionStart, easing: easing));
                upDirections.Insert(0, new VideoDirectionActionTween(duration: startDuration,
                    directionFrom: upDirectionBefore, directionTo: travelUpDirectionStart, easing: easing));
            }

            if (lookEndData is not null)
            {
                JToken easingData = lookEndData["easing"];
                VideoEasing easing = null;
                if (easingData is not null)
                {
                    easing = DataToEasing(easingData);
                }

                if (!endOverlap)
                {
                    positions.Add(new VideoPositionActionHold(duration: endDuration, position: positionAfter));
                }

                directions.Add(new VideoDirectionActionTween(duration: endDuration,
                    directionFrom: travelDirectionEnd, directionTo: directionAfter, easing: easing));
                upDirections.Add(new VideoDirectionActionTween(duration: endDuration,
                    directionFrom: travelUpDirectionEnd, directionTo: upDirectionAfter, easing: easing));
            }

            return (positions, directions, upDirections);
        }

        public static Vector3 DataToPosition(JToken positionData)
        {
            //TODO Add subtype parameters: WCS, source, etc
            return new Vector3(
                ValueOrDefault(positionData, "x", 0f),
                ValueOrDefault(positionData, "y", 0f),
                ValueOrDefault(positionData, "z", 0f)
            );
        }

        public static Vector3 DataToDirection(JToken directionData)
        {
            //TODO move named directions a level higher? (Implement in DirectionActionFromData)
            return ValueOrDefault(directionData, "name", "") switch
            {
                "up" => Vector3.up,
                "down" => Vector3.down,
                "left" => Vector3.left,
                "right" => Vector3.right,
                "forward" => Vector3.forward,
                "back" => Vector3.back,
                //TODO normalise and set to forward if 0 magnitude?
                _ => new Vector3(
                                ValueOrDefault(directionData, "x", 0f),
                                ValueOrDefault(directionData, "y", 0f),
                                ValueOrDefault(directionData, "z", 0f)
                            ),
            };
        }

        public static VideoEasing DataToEasing(JToken easingData)
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

        public static VideoCameraPath DataToPath(JToken pathData, Vector3 startPosition, Vector3 endPosition)
        {
            JToken startPosToken = pathData.SelectToken("startPosition");
            if (startPosToken is not null && ValueOrDefault(startPosToken, "type", "") == "position")
            {
                startPosition = DataToPosition(startPosToken);
            }

            JToken endPosToken = pathData.SelectToken("endPosition");
            if (endPosToken is not null && ValueOrDefault(endPosToken, "type", "") == "position")
            {
                endPosition = DataToPosition(endPosToken);
            }

            VideoEasing easing = null;
            JToken easingToken = pathData.SelectToken("easing");
            if (easingToken is not null && ValueOrDefault(easingToken, "type", "") == "easing")
            {
                easing = DataToEasing(easingToken);
            }

            return ValueOrDefault(pathData, "path", "") switch
            {
                // "line" => new LinePath(startPosition, endPosition, easing),
                "circle" => new CirclePath(
                                        //TODO Make this statement more efficient. I still want a one-liner if possible
                                        startPosition: startPosition,
                                        endPosition: endPosition,
                                        center: DataToPosition(pathData["centerPosition"]),//pathData.SelectToken("centerPosition") is not null ? DataToPosition(pathData["centerPosition"]) : Vector3.zero, // TODO complain if nothing given here
                                        largeAngleDirection: ValueOrDefault(pathData, "largeAngleDirection", false),
                                        additionalRotations: ValueOrDefault(pathData, "additionalRotations", 0),
                                        fullRotation: ValueOrDefault(pathData, "fullRotation", false),
                                        easing: easing
                                    ),
                _ => new LinePath(startPosition, endPosition, easing),
            };
        }
    }
}