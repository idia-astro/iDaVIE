using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using UnityEngine;


namespace VideoMaker
{
    public class VideoScriptData
    {
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
            TopLeft
        }

        public int Width = 1280;
        public int Height = 720;
        public int FrameRate = 20;
        //TODO add more logo parameters, such as position
        public float LogoScale = 0.2f;
        public LogoPosition logoPosition = LogoPosition.BottomRight;
        public PositionAction[] PositionActions;
        public DirectionAction[] DirectionActions;
        public DirectionAction[] UpDirectionActions;
    }

    public class VideoScriptReader
    {
        public readonly Vector3 PositionDefault = Vector3.zero;
        public readonly Vector3 DirectionDefault = Vector3.forward;
        public readonly Vector3 UpDirectionDefault = Vector3.up;

        const int EasingOrderDefault = 2;

        private readonly JSchema _schema;
        private readonly Dictionary<string, JSchema> _definitions;

        public VideoScriptReader(string schemaString = "")
        {
            _schema = JSchema.Parse(schemaString);

            //Extracting definitions from the schema
            _definitions = new();
            if (_schema.ExtensionData.TryGetValue("definitions", out JToken defsToken)
                || _schema.ExtensionData.TryGetValue("$defs", out defsToken))
            {
                if (defsToken is JObject defsObj)
                {
                    foreach (var def in defsObj.Properties())
                    {
                        string name = def.Name;
                        JSchema defSchema = def.Value.ToObject<JSchema>();

                        _definitions[name] = defSchema;
                        // Debug.Log($"Definition: {name}, Type: {defSchema.Type}");
                    }
                }
            }
        }


        public VideoScriptData ReadVideoScript(string videoScriptString)
        {
            JToken root = JToken.Parse(videoScriptString);

            IList<string> errorMessages;
            if (!root.IsValid(_schema, out errorMessages))
            {
                Debug.Log("Json validation failed");
                foreach (string msg in errorMessages)
                {
                    Debug.Log(msg);
                }
            }
            else
            {
                Debug.Log("Json validation succeeded");
            }

            VideoScriptData data = new();

            data.Width = ValueOrDefault(root, "height", data.Height);
            data.Width = ValueOrDefault(root, "width", data.Width);
            data.FrameRate = ValueOrDefault(root, "frameRate", data.FrameRate);

            data.logoPosition = ValueOrDefault(root, "logoPosition", "bottomRight") switch
            {
                "bottomLeft" => VideoScriptData.LogoPosition.BottomCenter,
                "bottomCenter" => VideoScriptData.LogoPosition.BottomLeft,
                "centerRight" => VideoScriptData.LogoPosition.CenterRight,
                "centerCenter" or "center" => VideoScriptData.LogoPosition.CenterCenter,
                "centerLeft" => VideoScriptData.LogoPosition.CenterLeft,
                "topRight" => VideoScriptData.LogoPosition.TopRight,
                "topCenter" => VideoScriptData.LogoPosition.TopCenter,
                "topLeft" => VideoScriptData.LogoPosition.TopLeft,
                _ => VideoScriptData.LogoPosition.BottomRight
            };

            data.LogoScale = ValueOrDefault(root, "logoScale", data.LogoScale);

            List<PositionAction> positionActions = new();
            List<DirectionAction> directionActions = new();
            List<DirectionAction> upDirectionActions = new();

            float time = 0;

            Vector3 positionFirst = Vector3.zero; //TODO make constants for these defaults
            Vector3 directionFirst = Vector3.forward;
            Vector3 upDirectionFirst = Vector3.up;
            // Vector3 positionPrevious = Vector3.zero;
            // Vector3 directionPrevious = Vector3.forward;
            // Vector3 upDirectionPrevious = Vector3.up;

            JArray actions = (JArray)root["actions"];

            //TODO redo everything in this loop. Check workbook.
            int i = 0;
            while (i < actions.Count)
            {
                if (
                    DoesActionRequireRelativePosition(actions[i], isPrevious: false) ||
                    DoesActionRequireRelativeDirection(actions[i], isPrevious: false) ||
                    DoesActionRequireRelativeDirection(actions[i], isPrevious: false, isUp: true)
                    )
                {
                    //TODO check if next action requires previous, etc
                }
                else
                {
                    // (float endTime,
                    // PositionAction positionAction,
                    // DirectionAction directionAction,
                    // DirectionAction upDirectionAction) = DataToAction(
                    //     actions[i], time,
                    //     positionActions.Count > 0 ? positionActions[-1] : null,
                    //     directionActions.Count > 0 ? directionActions[-1] : null,
                    //     upDirectionActions.Count > 0 ? upDirectionActions[-1] : null,
                    //     positionFirst, directionFirst, upDirectionFirst,
                    //     Vector3.zero, Vector3.forward, Vector3.up
                    // );

                    // if (i == 0)
                    // {
                    //     Vector3 pathForwardFirst;
                    //     Vector3 pathUpFirst;

                    //     (positionFirst, pathForwardFirst, pathUpFirst) = positionAction?.GetPositionDirection(time) ?? (
                    //         PositionDefault, DirectionDefault, UpDirectionDefault
                    //     );
                    //     directionFirst = directionAction?.GetDirection(time, positionFirst, pathForwardFirst, pathUpFirst) ?? DirectionDefault;
                    //     upDirectionFirst = upDirectionAction?.GetDirection(time, positionFirst, pathForwardFirst, pathUpFirst) ?? UpDirectionDefault;
                    // }

                    // if (positionAction is not null)
                    // {
                    //     positionActions.Add(positionAction);
                    // }

                    // if (directionAction is not null)
                    // {
                    //     directionActions.Add(directionAction);
                    // }

                    // if (upDirectionAction is not null)
                    // {
                    //     upDirectionActions.Add(upDirectionAction);
                    // }

                    // time = endTime;
                }
            }

            data.PositionActions = positionActions.ToArray();
            data.DirectionActions = directionActions.ToArray();
            data.UpDirectionActions = upDirectionActions.ToArray();

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

        private string GetValidDefinition(JToken jToken)
        {
            foreach (string defName in _definitions.Keys)
            {
                if (jToken.IsValid(_definitions[defName]))
                {
                    return defName;
                }
            }

            return "";
        }

        private bool ShouldActionPositionContinue(JToken data)
        {
            return ValueOrDefault(data, "position", "continue") == "continue";
        }

        private bool ShouldActionDirectionContinue(JToken data)
        {
            JToken directionData = data.SelectToken("rotation") ?? data.SelectToken("lookAt") ?? data.SelectToken("lookAway");

            if (directionData is null) {
                return true;
            }

            return ToObjectOrDefualt(directionData, "") == "continue";
        }

        private bool ShouldActionUpDirectionContinue(JToken data)
        {
            JToken directionData = data.SelectToken("rotation") ?? data.SelectToken("lookUp") ?? data.SelectToken("lookDown");

            if (directionData is null) {
                return true;
            }

            return ToObjectOrDefualt(directionData, "") == "continue";
        }

        private bool DoesActionRequireRelativePosition(JToken actionData, bool isPrevious)
        {
            string rel = isPrevious ? "previous" : "next";
            //Look for previous in position / path
            JToken positionData = actionData.SelectToken("position");

            if (positionData is not null && GetValidDefinition(positionData) switch
            {
                "relativeAction" => ToObjectOrDefualt(positionData, "") == rel,
                "path" => DoesPathRequireRelativePosition(positionData, isPrevious),
                _ => false
            })
            {
                return true;
            }


            //Look at paths in look-at targets
            foreach (var (dir, dirRev) in new List<(string, string)> { ("At", "Away"), ("Up", "Down") })
            {
                JToken dirData = actionData.SelectToken($"look{dir}") ?? actionData.SelectToken($"look{dirRev}");

                if (dirData is null)
                {
                    continue;
                }

                if (GetValidDefinition(dirData) == "path" && DoesPathRequireRelativePosition(dirData, isPrevious))
                {
                    return true;
                }
            }

            return false;
        }

        private bool DoesPathRequireRelativePosition(JToken pathData, bool isPrevious)
        {
            string rel = isPrevious ? "previous" : "next";
            //TODO this is not robust enough. Sort it out!
            if (ValueOrDefault(pathData, "start", "previous") == rel || ValueOrDefault(pathData, "end", "next") == rel)
            {
                return true;
            }
            return false;
        }

        //TODO check if path requires previous direction
        private bool DoesActionRequireRelativeDirection(JToken actionData, bool isPrevious, bool isUp = false)
        {
            string rel = isPrevious ? "previous" : "next";

            JToken dirData = actionData.SelectToken($"look{(isUp ? "Up" : "At")}") ?? actionData.SelectToken($"look{(isUp ? "Down" : "Away")}") ?? actionData.SelectToken("rotation");

            if (dirData is null)
            {
                return false;
            }

            return GetValidDefinition(dirData) switch
            {
                "relativeAction" => ToObjectOrDefualt(dirData, "") == rel,
                "directionBetween" or "rotateBetween" => ValueOrDefault(dirData, "start", "previous") == rel || ValueOrDefault(dirData, "end", "") == rel,
                "easing" => true,
                _ => false
            };
        }

        private PositionAction DataToPositionAction(JToken data, float duration,
            Vector3 positionFirst, Vector3 directionFirst, Vector3 upDirectionFirst,
            Vector3 positionPrevious, Vector3 directionPrevious, Vector3 upDirectionPrevious,
            Vector3 positionNext, Vector3 directionNext, Vector3 upDirectionNext
        )
        {
            //Note continueAction schema should have been handled and position token is garanteed
            switch (GetValidDefinition(data.SelectToken("position")))
            {
                default:
                    return new PositionActionHold(positionPrevious);
            }
        }

        private DirectionAction DataToDirectionAction(JToken data, float duration,
            Vector3 positionFirst, Vector3 directionFirst, Vector3 upDirectionFirst,
            Vector3 positionPrevious, Vector3 directionPrevious, Vector3 upDirectionPrevious,
            Vector3 positionNext, Vector3 directionNext, Vector3 upDirectionNext, bool isUp = false)
        {
            //Note continue schema should have been handled and one of rotation, lookAt or lookAway tokens should exist
            //TODO Check rotation

            bool reverse = false;
            JToken directionData = data.SelectToken(isUp ? "lookUp" : "lookAt");
            if (directionData is null)
            {
                reverse = true;
                directionData = data.SelectToken(isUp ? "lookDown" : "LookAway");
            }

            DirectionAction directionAction;

            switch (GetValidDefinition(directionData))
            {
                default:
                    directionAction = new DirectionActionHold(isUp ? upDirectionPrevious : upDirectionPrevious);
                    break;
            }

            directionAction.ReverseDirection = reverse;
            return directionAction;
        }

        
        //TODO remove this
        // private (float endTime, PositionAction position, DirectionAction direction, DirectionAction upDirection) DataToAction(
        //     JToken data, float time,
        //     PositionAction positionActionPrevious,
        //     DirectionAction directionActionPrevious,
        //     DirectionAction upDirectionActionPrevious,
        //     Vector3 positionFirst, Vector3 directionFirst, Vector3 upDirectionFirst,
        //     Vector3 positionNext, Vector3 directionNext, Vector3 upDirectionNext
        // )
        // {

        //     float duration = ValueOrDefault(data, "duration", 0f);

        //     //Applying continue first, as this is necessary to determine previous positions / directions
        //     //Position data and continue
        //     JToken positionData = data.SelectToken("position");
        //     string positionSchema = positionData is null ? "continueAction" : GetValidDefinition(positionData);

        //     if (positionActionPrevious is not null && positionSchema == "continueAction")
        //     {
        //         positionActionPrevious.Duration += duration;
        //     }

        //     //TODO check rotation

        //     //Direction data and continue
        //     bool directionReverse = false;
        //     JToken directionData = data.SelectToken("lookAt");
        //     if (directionData is null)
        //     {
        //         directionReverse = true;
        //         directionData = data.SelectToken("LookAway");
        //     }

        //     string directionSchema = directionData is null ? "continueAction" : GetValidDefinition(directionData);

        //     if (directionActionPrevious is not null && directionSchema == "continueAction")
        //     {
        //         directionActionPrevious.Duration += duration;
        //     }

        //     //Up-direction data and continue
        //     bool upDirectionReverse = false;
        //     JToken upDirectionData = data.SelectToken("lookUp");
        //     if (directionData is null)
        //     {
        //         upDirectionReverse = true;
        //         directionData = data.SelectToken("LookDown");
        //     }

        //     string upDirectionSchema = upDirectionData is null ? "continueAction" : GetValidDefinition(upDirectionData);

        //     if (upDirectionActionPrevious is not null && upDirectionSchema == "continueAction")
        //     {
        //         upDirectionActionPrevious.Duration += duration;
        //     }

        //     //Determining (new) previous positions and directions
        //     (Vector3 positionPrevious, Vector3 pathForwardPrevious, Vector3 pathUpPrevious) = positionActionPrevious?.GetPositionDirection(time) ?? (Vector3.zero, Vector3.forward, Vector3.up);
        //     Vector3 directionPrevious = directionActionPrevious?.GetDirection(time, positionPrevious, pathForwardPrevious, pathUpPrevious) ?? Vector3.forward;
        //     Vector3 upDirectionPrevious = upDirectionActionPrevious?.GetDirection(time, positionPrevious, pathForwardPrevious, pathUpPrevious) ?? Vector3.up;

        //     //Determining actions
        //     PositionAction positionAction = null;
        //     DirectionAction directionAction = null;
        //     DirectionAction upDirectionAction = null;


        //     //Position action
        //     if (positionData is not null && positionSchema != "continueAction")
        //     {
        //         positionAction = positionSchema switch
        //         {
        //             //TODO finish here
        //             "relativeAction" => ToObjectOrDefualt(data, "") == "previous" ? new PositionActionHold(positionPrevious) : new PositionActionHold(positionNext),
        //             "namedPosition" => ToObjectOrDefualt(data, "center") switch
        //             {
        //                 //Add other named positions here
        //                 _ => new PositionActionHold(positionNext)
        //             },
        //             _ => null
        //         };

        //         //TODO what to do if this is null here?
        //         if (positionAction is not null)
        //         {
        //             positionAction.StartTime = time;
        //             positionAction.Duration = duration;
        //         }
        //     }


        //     //TODO check rotation and return if it exists

        //     //Direction action
        //     if (directionAction is not null && directionSchema != "continueAction")
        //     {
        //         directionAction = DataToDirectionAction(directionData, positionPrevious, directionPrevious, positionNext, directionNext);

        //         if (directionAction is not null)
        //         {
        //             directionAction.StartTime = time;
        //             directionAction.Duration = duration;
        //             directionAction.ReverseDirection = directionReverse;
        //         }
        //     }

        //     //upDirection action
        //     if (upDirectionAction is not null && directionSchema != "continueAction")
        //     {
        //         upDirectionAction = DataToDirectionAction(upDirectionData, positionPrevious, upDirectionPrevious, positionNext, upDirectionNext);

        //         if (upDirectionAction is not null)
        //         {
        //             upDirectionAction.StartTime = time;
        //             upDirectionAction.Duration = duration;
        //             upDirectionAction.ReverseDirection = upDirectionReverse;
        //         }
        //     }

        //     return (time + duration, positionAction, directionAction, upDirectionAction);
        // }

        // private DirectionAction DataToDirectionAction(JToken data,
        //     // PositionAction positionActionPrevious, DirectionAction directionActionPrevious,
        //     Vector3 positionPrevious, Vector3 directionPrevious,
        //     Vector3 positionNext, Vector3 directionNext
        // ) => GetValidDefinition(data) switch
        // {
        //     //TODO finish this
        //     _ => null
        // };


        //TODO Everything below this line needs to be re-worked
        private Path DataToPath(JToken data,
            Vector3 positionPrevious, Vector3 directionPrevious, Vector3 upDirectionPrevious,
            Vector3 positionNext, Vector3 directionNext, Vector3 upDirectionNext
        )
        {
            //TODO!
            return new LinePath(positionPrevious, positionNext);
        }


        //TODO return single actions instead of lists? There may not ever be a case where lists are needed
        // private static (List<PositionAction> positions, List<DirectionAction> directions, List<DirectionAction> upDirections) ActionDataToActions(JToken actionData,
        //     Vector3 positionBefore, Vector3 directionBefore, Vector3 upDirectionBefore)
        // {
        //     List<PositionAction> positions = new();
        //     List<DirectionAction> directions = new();
        //     List<DirectionAction> upDirections = new();

        //     float duration = ValueOrDefault(actionData, "duration", 0f);

        //     JToken positionData = actionData["position"];
        //     JToken directionData = actionData["lookAt"];
        //     JToken upDirectionData = actionData["lookUp"];

        //     Path path = null;
        //     if (positionData is not null)
        //     {
        //         path = DataToPath(positionData, positionBefore, positionBefore);
        //         Vector3? position = DataToPosition(positionData);

        //         if (path is not null)
        //         {
        //             positions.Add(new PositionActionPath(duration, path));
        //         }
        //         else if (position is not null)
        //         {
        //             positions.Add(new PositionActionHold(duration, (Vector3)position));
        //         }
        //         else
        //         {
        //             positions.Add(new PositionActionHold(duration, positionBefore));
        //         }
        //     }
        //     else
        //     {
        //         positions.Add(new PositionActionHold(duration, positionBefore));
        //     }

        //     directions.Add(DirectionActionFromData(directionData, duration, path, directionBefore, false));
        //     upDirections.Add(DirectionActionFromData(upDirectionData, duration, path, upDirectionBefore, true));

        //     return (positions, directions, upDirections);
        // }

        // private static DirectionAction DirectionActionFromData(JToken data, float duration, Path path, Vector3 defualtDirection,
        //     bool pathUpDirection, float startTimeOffset = 0f, float endTimeOffset = 0f)
        // {
        //     if (data is not null)
        //     {
        //         switch (ToObjectOrDefualt(data, ""))
        //         {
        //             case "alongPath":
        //                 return new DirectionActionPath(duration, path, startTimeOffset: startTimeOffset, endTimeOffset: endTimeOffset);
        //             case "alongPathReverse":
        //                 return new DirectionActionPath(duration, path, invert: true, startTimeOffset: startTimeOffset, endTimeOffset: endTimeOffset);
        //             case "upFromPath":
        //                 return new DirectionActionPath(duration, path, useUpDirection: true, startTimeOffset: startTimeOffset, endTimeOffset: endTimeOffset);
        //             case "upFromPathReverse":
        //                 return new DirectionActionPath(duration, path, useUpDirection: true, invert: true, startTimeOffset: startTimeOffset, endTimeOffset: endTimeOffset);

        //         }

        //         Vector3? direction = DataToDirection(data);

        //         if (direction is not null)
        //         {
        //             return new DirectionActionHold(duration: duration, direction: (Vector3)direction);
        //         }

        //         Vector3? position = DataToPosition(data);

        //         if (position is not null)
        //         {
        //             return new DirectionActionLookAt(duration: duration, target: (Vector3)position);
        //         }

        //         // Defualt: along path or looking forward
        //         if (path is null)
        //         {
        //             return new DirectionActionHold(duration, defualtDirection);
        //         }
        //         else
        //         {
        //             return new DirectionActionPath(duration, path, useUpDirection: pathUpDirection, startTimeOffset: startTimeOffset, endTimeOffset: endTimeOffset);
        //         }
        //     }
        //     else
        //     {
        //         return new DirectionActionHold(duration, defualtDirection);
        //     }
        // }

        // private static (List<PositionAction> positions, List<DirectionAction> directions, List<DirectionAction> upDirections) TransitionDataToActions(JToken transitionData,
        //     Vector3 positionBefore, Vector3 directionBefore, Vector3 upDirectionBefore,
        //     Vector3 positionAfter, Vector3 directionAfter, Vector3 upDirectionAfter)
        // {
        //     List<PositionAction> positions = new();
        //     List<DirectionAction> directions = new();
        //     List<DirectionAction> upDirections = new();

        //     JToken lookStartData = transitionData["lookStart"];
        //     float startDuration = 0.0f;
        //     bool startOverlap = false;

        //     if (lookStartData is not null)
        //     {
        //         startDuration = ValueOrDefault(lookStartData, "duration", 0f);
        //         startOverlap = ValueOrDefault(lookStartData, "overlapWithTravel", false);
        //     }

        //     JToken lookEndData = transitionData["lookEnd"];
        //     float endDuration = 0.0f;
        //     bool endOverlap = false;

        //     if (lookEndData is not null)
        //     {
        //         endDuration = ValueOrDefault(lookEndData, "duration", 0f);
        //         endOverlap = ValueOrDefault(lookEndData, "overlapWithTravel", false);
        //     }

        //     JToken travelData = transitionData["travel"];

        //     Vector3 travelDirectionStart = directionAfter;
        //     Vector3 travelUpDirectionStart = upDirectionAfter;
        //     Vector3 travelDirectionEnd = directionBefore;
        //     Vector3 travelUpDirectionEnd = upDirectionBefore;

        //     if (travelData is not null)
        //     {
        //         float duration = ValueOrDefault(travelData, "duration", 0f);

        //         JToken travelPositionData = travelData["position"];
        //         Path path = travelPositionData is not null ? DataToPath(travelPositionData, positionBefore, positionAfter) : null;
        //         path ??= new LinePath(positionBefore, positionAfter, new EasingInOut(order: EasingOrderDefault));

        //         PositionAction positionAction = new PositionActionPath(duration, path);
        //         positions.Add(positionAction);

        //         float directionDuration = duration;
        //         float startTimeOffset = 0f;
        //         if (startOverlap)
        //         {
        //             directionDuration -= startDuration;
        //             startTimeOffset = startDuration;
        //         }

        //         float endTimeOffset = 0f;
        //         if (endOverlap)
        //         {
        //             directionDuration -= endDuration;
        //             endTimeOffset = endDuration;
        //         }

        //         Vector3 positionStart = positionAction.GetPosition(startTimeOffset);
        //         Vector3 positionEnd = positionAction.GetPosition(duration - endTimeOffset);

        //         //TODO there is something wrong with this! Maybe even the method
        //         DirectionAction directionAction = DirectionActionFromData(travelData["lookAt"], directionDuration, path, directionBefore, false, startTimeOffset, endTimeOffset);
        //         directions.Add(directionAction);

        //         travelDirectionStart = directionAction.GetDirection(0f, positionStart);
        //         travelDirectionEnd = directionAction.GetDirection(duration - endTimeOffset, positionEnd);

        //         DirectionAction upDirectionAction = DirectionActionFromData(travelData["LookUp"], directionDuration, path, upDirectionBefore, true, startTimeOffset, endTimeOffset);

        //         upDirections.Add(upDirectionAction);

        //         travelUpDirectionStart = upDirectionAction.GetDirection(0f, positionStart);
        //         travelUpDirectionEnd = upDirectionAction.GetDirection(duration - endTimeOffset, positionEnd);
        //     }

        //     //TODO eliminate repitition. Use a function?
        //     if (lookStartData is not null)
        //     {
        //         JToken easingData = lookStartData["easing"];
        //         Easing easing = easingData is not null ? DataToEasing(easingData) : null;

        //         if (!startOverlap)
        //         {
        //             positions.Add(new PositionActionHold(duration: startDuration, position: positionBefore));
        //         }

        //         directions.Insert(0, new DirectionActionTween(duration: startDuration,
        //             directionFrom: directionBefore, directionTo: travelDirectionStart, easing: easing));
        //         upDirections.Insert(0, new DirectionActionTween(duration: startDuration,
        //             directionFrom: upDirectionBefore, directionTo: travelUpDirectionStart, easing: easing));
        //     }

        //     if (lookEndData is not null)
        //     {
        //         JToken easingData = lookEndData["easing"];
        //         Easing easing = easingData is not null ? DataToEasing(easingData) : null;
                
        //         if (!endOverlap)
        //         {
        //             positions.Add(new PositionActionHold(duration: endDuration, position: positionAfter));
        //         }

        //         directions.Add(new DirectionActionTween(duration: endDuration,
        //             directionFrom: travelDirectionEnd, directionTo: directionAfter, easing: easing));
        //         upDirections.Add(new DirectionActionTween(duration: endDuration,
        //             directionFrom: travelUpDirectionEnd, directionTo: upDirectionAfter, easing: easing));
        //     }

        //     return (positions, directions, upDirections);
        // }

        private static Vector3 DataToPosition(JToken data)
        {
            // if (ToObjectOrDefualt(positionData, "") == "center")
            // {
            //     return Vector3.zero;
            // }

            // if (ValueOrDefault(positionData, "type", "") != "position")
            // {
            //     return null;
            // }

            //TODO Add subtype parameters: WCS, source, etc
            return new Vector3(
                ValueOrDefault(data, "x", 0f),
                ValueOrDefault(data, "y", 0f),
                ValueOrDefault(data, "z", 0f)
            );
        }

        // private static Vector3? DataToDirection(JToken directionData)
        // {
        //     //TODO move named directions a level higher? (Implement in DirectionActionFromData)
        //     return ToObjectOrDefualt(directionData, "") switch
        //     {
        //         "up" => Vector3.up,
        //         "down" => Vector3.down,
        //         "left" => Vector3.left,
        //         "right" => Vector3.right,
        //         "forward" => Vector3.forward,
        //         "back" => Vector3.back,
        //         //TODO normalise and set to forward if 0 magnitude?
        //         _ => ValueOrDefault(directionData, "type", "") == "direction" ? new Vector3(
        //                         ValueOrDefault(directionData, "x", 0f),
        //                         ValueOrDefault(directionData, "y", 0f),
        //                         ValueOrDefault(directionData, "z", 0f)
        //                     ) : null
        //     };
        // }

        //TODO re-write both this and the accelDecel (generic inOut)
        private Easing DataToEasing(JToken data)
        {
            int order = ValueOrDefault(data, "order", EasingOrderDefault);

            switch (ValueOrDefault(data, "kind", ""))
            {
                case "linear":
                    return new Easing();
                case "in":
                    return new EasingIn(order);
                case "out":
                    return new EasingOut(order);
                case "inOut":
                    return new EasingInOut(order);
                default:
                    float timeIn = ValueOrDefault(data, "timeIn", -1f);
                    float timeMid = ValueOrDefault(data, "timeMid", -1f);
                    float timeOut = ValueOrDefault(data, "timeOut", -1f);

                    if (
                            (timeIn < 0 && timeMid < 0 && timeOut < 0)
                            || (timeIn < 0 && timeMid < 0)
                            || (timeIn < 0 && timeOut < 0)
                            || (timeMid < 0 && timeOut < 0)
                        )
                    {
                        return new EasingInOut(order);
                    }
                    else if (timeIn < 0)
                    {
                        timeIn = 1 - timeMid - timeOut;
                    }
                    else if (timeMid < 0)
                    {
                        timeMid = 1 - timeIn - timeOut;
                    }
                    else if (timeOut < 0)
                    {
                        timeOut = 1 - timeIn - timeMid;
                    }

                    float sum = timeIn + timeMid + timeOut;

                    return new EasingInLinOut(order, timeIn / sum, timeOut / sum);
            }
        }

        // private static Path DataToPath(JToken pathData, Vector3 startPosition, Vector3 endPosition)
        // {
        //     if (ValueOrDefault(pathData, "type", "") != "path")
        //     {
        //         return null;
        //     }
        //     JToken startPosToken = pathData.SelectToken("startPosition");
        //     startPosition = startPosToken is not null ? DataToPosition(startPosToken) ?? startPosition : startPosition;

        //     JToken endPosToken = pathData.SelectToken("endPosition");
        //     endPosition = endPosToken is not null ? DataToPosition(endPosToken) ?? endPosition : endPosition;

        //     JToken easingToken = pathData.SelectToken("easing");
        //     Easing easing = easingToken is not null ? DataToEasing(easingToken) : null;

        //     //TODO change to "name". Also whether startPosition and endPositions are defined or not is signifigant for some paths, it should be indicated



        //     switch (ValueOrDefault(pathData, "name", ""))
        //     {
        //         // "line" => new LinePath(startPosition, endPosition, easing),
        //         case "circle":
        //             CirclePath.AxisDirection axisDirection = ValueOrDefault(pathData, "axis", "") switch
        //             {
        //                 "up" => CirclePath.AxisDirection.Up,
        //                 "down" => CirclePath.AxisDirection.Down,
        //                 "left" => CirclePath.AxisDirection.Left,
        //                 "right" => CirclePath.AxisDirection.Right,
        //                 "forward" => CirclePath.AxisDirection.Forward,
        //                 "back" => CirclePath.AxisDirection.Back,
        //                 _ => CirclePath.AxisDirection.None
        //             };

        //              // TODO complain if nothing given here
        //             Vector3 center = pathData.SelectToken("centerPosition") is not null ? DataToPosition(pathData["centerPosition"]) ?? Vector3.zero : Vector3.zero;

        //             if (axisDirection != CirclePath.AxisDirection.None && pathData["startAngle"] is not null)
        //             {
        //                 return new CirclePath(
        //                     center: center,
        //                     axis: axisDirection,
        //                     startAngle: ValueOrDefault(pathData, "startAngle", 0f),
        //                     rotations: ValueOrDefault(pathData, "rotations", 1f),
        //                     radius: ValueOrDefault(pathData, "radius", 1f),
        //                     easing: easing
        //                 );
        //             }

        //             Vector3? axis = pathData["axis"] is not null ? DataToDirection(pathData["axis"]) : null;

        //             //Check is not robust enough for more circle options
        //             if (axis is not null)
        //             {
        //                 return new CirclePath(
        //                     startPosition: startPosition,
        //                     axis: (Vector3)axis,
        //                     center: center,
        //                     rotations: ValueOrDefault(pathData, "rotations", 1f),
        //                     easing: easing
        //                 );
        //             }

        //                 return new CirclePath(
        //                         //TODO Make this statement more efficient. I still want a one-liner if possible
        //                         startPosition: startPosition,
        //                         endPosition: endPosition,
        //                         center: center,
        //                         largeAngleDirection: ValueOrDefault(pathData, "largeAngleDirection", false),
        //                         additionalRotations: ValueOrDefault(pathData, "additionalRotations", 0),
        //                         easing: easing
        //                     );
                    
        //         default:
        //             return new LinePath(startPosition, endPosition, easing);
        //     }
        // }
    }
}