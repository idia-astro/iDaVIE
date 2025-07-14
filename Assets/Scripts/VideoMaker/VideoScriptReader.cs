using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;


namespace VideoMaker
{
    public class VideoScriptReader
    {
        public int FrameRate = 20;
        //TODO add more logo parameters, such as position
        public bool UseLogo = true;
        public float LogoScale = 0.2f;
        public VideoPositionAction[] PositionActions;
        public VideoDirectionAction[] DiractionActions;
        public VideoDirectionAction[] UpDirectionActions;

        public VideoScriptReader(string videoScriptString)
        {
            JObject root = JObject.Parse(videoScriptString);

            if (root.ContainsKey("frameRate"))
            {
                FrameRate = root.Value<int>("frameRate");// ?? FramesRate;
            }

            if (root.ContainsKey("useLogo"))
            {
                UseLogo = root.Value<bool>("useLogo");// ?? UseLogo;
            }

            if (root.ContainsKey("logoScale"))
            {
                LogoScale = root.Value<float>("logoScale"); //?? LogoScale;
            }
            
            List<VideoPositionAction> positions = new();
            List<VideoDirectionAction> directions = new();
            List<VideoDirectionAction> upDirections = new();

            Vector3 positionBefore = new(0, 0, 0);
            Vector3 directionBefore = new(0, 0, 0);
            Vector3 upDirectionBefore = new(0, 0, 0);
            JToken? transitionData = null;

            foreach (JToken actionData in (JArray)root["actions"])
            {
                if (actionData.Value<string>("type") == "transition")
                {
                    transitionData = actionData;
                    continue;
                }

                var actions = ActionDataToActions(actionData);

                VideoPositionAction positionAction = actions.positions[actions.positions.Count - 1];
                Vector3 positionAfter = positionAction.GetPosition(positionAction.Duration);

                VideoDirectionAction directionAction = actions.directions[actions.directions.Count - 1];
                Vector3 directionAfter = directionAction.GetDirection(directionAction.Duration, positionAfter);

                directionAction = actions.upDirections[actions.upDirections.Count - 1];
                Vector3 upDirectionAfter = directionAction.GetDirection(directionAction.Duration, positionAfter);

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

                positionBefore = positionAfter;
                directionBefore = directionAfter;
                upDirectionBefore = upDirectionAfter;
            }
        }

        //TODO return single actions instead of lists? There may not ever be a case where lists are needed
        public static (List<VideoPositionAction> positions, List<VideoDirectionAction> directions, List<VideoDirectionAction> upDirections) ActionDataToActions(JToken actionData)
        {
            List<VideoPositionAction> positions = new();
            List<VideoDirectionAction> directions = new();
            List<VideoDirectionAction> upDirections = new();

            float duration = actionData.Value<float>("duration");

            JToken positionData = actionData["position"];
            JToken directionData = actionData["lookAt"];
            JToken upDirectionData = actionData["lookUp"];

            VideoCameraPath? path = null;
            if (positionData is not null)
            {
                switch (positionData.Value<string>("type"))
                {
                    case "path":
                        path = DataToPath(positionData);
                        positions.Add(new VideoPositionActionPath(duration, path));
                        break;
                    case "position":
                        positions.Add(new VideoPositionActionHold(duration, DataToPosition(positionData)));
                        break;
                    default:
                        positions.Add(new VideoPositionActionHold(duration, Vector3.zero));
                        break;
                }
            }
            else
            {
                positions.Add(new VideoPositionActionHold(duration, Vector3.zero));
            }


            directions.Add(DirectionActionFromData(directionData, duration, path, Vector3.forward));
            upDirections.Add(DirectionActionFromData(upDirectionData, duration, path, Vector3.up));

            return (positions, directions, upDirections);
        }

        public static VideoDirectionAction DirectionActionFromData(JToken data, float duration, VideoCameraPath? path, Vector3 defualtDirection)
        {
            if (data is not null)
            {
                switch (data.Value<string>("type"))
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
                            return new VideoDirectionActionPath(duration, path);
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
                startDuration = lookStartData.Value<float>("duration"); //?? 0f;
                startOverlap = lookStartData.Value<bool>("overlapWithTravel");// ?? false;
            }

            JToken lookEndData = transitionData["lookEnd"];
            float endDuration = 0.0f;
            bool endOverlap = false;

            if (lookEndData is not null)
            {
                endDuration = lookEndData.Value<float>("duration");// ?? 0f;
                endOverlap = lookEndData.Value<bool>("overlapWithTravel");// ?? false;
            }

            JToken travelData = transitionData["travel"];

            Vector3 travelDirectionStart = Vector3.forward;
            Vector3 travelUpDirectionStart = Vector3.up;
            Vector3 travelDirectionEnd = Vector3.forward;
            Vector3 travelUpDirectionEnd = Vector3.up;

            if (travelData is not null)
            {
                float duration = travelData.Value<float>("duration");// ?? 0f;

                //TODO check for path and then use DataToPath with start and end positions
                // JToken pathData = travelData["path"];
                VideoCameraPath path = new LinePath(positionBefore, positionAfter, new EasingInOut(order: 2));
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
                positionData.Value<float>("x"),// ?? 0,
                positionData.Value<float>("y"),// ?? 0,
                positionData.Value<float>("z")// ?? 0
            );
        }

        public static Vector3 DataToDirection(JToken directionData)
        {
            switch (directionData.Value<string>("name"))
            {
                case "up":
                    return Vector3.up;
                case "down":
                    return Vector3.down;
                case "left":
                    return Vector3.left;
                case "right":
                    return Vector3.right;
                case "forward":
                    return Vector3.forward;
                case "back":
                    return Vector3.back;
            }

            //TODO normalise and set to forward if 0 magnitude?
            return new Vector3(
                directionData.Value<float>("x"),// ?? 0,
                directionData.Value<float>("y"),// ?? 0,
                directionData.Value<float>("z") //?? 0
            );
        }

        public static VideoEasing? DataToEasing(JToken easingData)
        {
            switch (easingData.Value<string>("easing"))
            {
                case "in":
                    return new EasingIn(easingData.Value<int>("order"));// ?? 2);
                case "out":
                    return new EasingOut(easingData.Value<int>("order"));// ?? 2);
                case "inOut":
                    return new EasingInOut(easingData.Value<int>("order"));// ?? 2);
                case "accelDecel":
                    return new EasingAccelDecel(easingData.Value<float>("timeAccel"),// ?? 0.0f,
                        easingData.Value<float>("timeDecel"));// ?? 1.0f);
            }

            return null;
        }

        public static VideoCameraPath DataToPath(JToken pathData)
        {
            switch (pathData.Value<string>("path"))
            {

                case "line":
                    return new LinePath(DataToPosition(pathData["startPosition"]), DataToPosition(pathData["endPosition"]), DataToEasing(pathData["easing"]));
                case "circle":
                    return new CirclePath(
                        startPosition: DataToPosition(pathData["startPosition"]),
                        endPosition: DataToPosition(pathData["endPosition"]),
                        center: DataToPosition(pathData["center"]),
                        largeAngleDirection: pathData.Value<bool>("largeAngleDirection"),// ?? false,
                        additionalRotations: pathData.Value<int>("additionalRotations"),// ?? 0,
                        fullRotation: pathData.Value<bool>("fullRotation"),// ?? false,
                        easing: DataToEasing(pathData["easing"])
                    );
            }

            return new LinePath(Vector3.zero, Vector3.zero);
        }

        // public static VideoCameraPath DataToPath(JToken pathData, Vector3 startPosition, Vector3 endPosition)
        // {
            
        // }
        
    }
}