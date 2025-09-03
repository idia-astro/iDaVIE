using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Xml.Schema;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using UnityEditor;
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

        private class ActionItem
        {
            public int Index { set; get; }
            // public int EndIndex { set; get; }
            public float StartTime { set; get; }
            public float Duration { set; get; }

            public bool RequireRelative
            {
                get => (
                RequirePreviousPosition || RequirePreviousDirection || RequirePreviousUpDirection
                || RequireNextPosition || RequireNextDirection || RequireNextUpDirection
                || RequireFirstPosition || RequireFirstDirection || RequireFirstUpDirection
                );
            }

            public bool RequirePreviousPosition { set; get; }
            public bool RequirePreviousDirection { set; get; }
            public bool RequirePreviousUpDirection { set; get; }

            public bool RequireNextPosition { set; get; }
            public bool RequireNextDirection { set; get; }
            public bool RequireNextUpDirection { set; get; }

            public bool RequireFirstPosition { set; get; }
            public bool RequireFirstDirection { set; get; }
            public bool RequireFirstUpDirection { set; get; }

            public JToken Data { set; get; }
            // public Action Instance { set; get; }

            public Vector3 StartValue { set; get; }
            public Vector3 EndValue { set; get; }
        }


        //TODO put schema related constants in here


        private readonly Vector3 PositionDefault = Vector3.zero;
        private readonly Vector3 DirectionDefault = Vector3.forward;
        private readonly Vector3 UpDirectionDefault = Vector3.up;

        const int EasingOrderDefault = 2;

        private readonly JSchema _schema;
        private readonly Dictionary<string, JSchema> _definitions;
        private readonly Dictionary<string, IList<JSchema>> _pathSchema;

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

            _pathSchema = new();
            //Extract sub-path schema from definitions
            JSchema pathSchema = _definitions.GetValueOrDefault(SchemaDefs.Path.Def);
            if (pathSchema is not null)
            {
                foreach (JSchema kindSchema in pathSchema.OneOf)
                {
                    string kind = ToObjectOrDefualt(kindSchema.Properties["kind"].Const, "");
                    if (kind == "")
                    {
                        return;
                    }
                    _pathSchema[kind] = kindSchema.OneOf;
                }
            }
        }

        public VideoScriptData ReadVideoScript(string videoScriptString)
        {
            JToken root = JToken.Parse(videoScriptString);

            IList<string> errorMessages;
            if (!root.IsValid(_schema, out errorMessages))
            {

                string warn = "Json validation failed\n";
                foreach (string msg in errorMessages)
                {
                    warn += $"{msg}\n";
                }
                Debug.LogWarning(warn);
                return null;
            }
            else
            {
                Debug.Log("Json validation succeeded");
            }

            VideoScriptData data = new();

            data.Width = ValueOrDefault(root, "height", data.Height);
            data.Width = ValueOrDefault(root, "width", data.Width);
            data.FrameRate = ValueOrDefault(root, "frameRate", data.FrameRate);

            data.logoPosition = ValueOrDefault(root, SchemaDefs.Logo.Position.Def, SchemaDefs.Logo.Position.BottomRight) switch
            {
                SchemaDefs.Logo.Position.BottomLeft => VideoScriptData.LogoPosition.BottomCenter,
                SchemaDefs.Logo.Position.BottomCenter => VideoScriptData.LogoPosition.BottomLeft,
                SchemaDefs.Logo.Position.CenterRight => VideoScriptData.LogoPosition.CenterRight,
                SchemaDefs.Logo.Position.CenterCenter or SchemaDefs.Logo.Position.Center => VideoScriptData.LogoPosition.CenterCenter,
                SchemaDefs.Logo.Position.CenterLeft => VideoScriptData.LogoPosition.CenterLeft,
                SchemaDefs.Logo.Position.TopRight => VideoScriptData.LogoPosition.TopRight,
                SchemaDefs.Logo.Position.TopCenter => VideoScriptData.LogoPosition.TopCenter,
                SchemaDefs.Logo.Position.TopLeft => VideoScriptData.LogoPosition.TopLeft,
                _ => VideoScriptData.LogoPosition.BottomRight
            };

            data.LogoScale = ValueOrDefault(root, "logoScale", data.LogoScale);

            float time = 0;

            Vector3 positionFirst = Vector3.zero; //TODO make constants for these defaults
            Vector3 directionFirst = Vector3.forward;
            Vector3 upDirectionFirst = Vector3.up;

            List<ActionItem> positionActionItems = new();
            List<ActionItem> directionActionItems = new();
            List<ActionItem> upDirectionActionItems = new();

            JArray actionDataArray = (JArray)root["actions"];

            //TODO check if first action requires previous, first or uses continue and return error

            //Set-up action items
            //TODO check for dependency loops within each action queue here
            // for (int i = 0; i < actions.Count; i++)
            int posIdx = 0;
            int dirIdx = 0;
            int upIdx = 0;

            foreach (JToken actionData in actionDataArray)
            {
                float duration = ValueOrDefault(actionData, "duration", 0f);

                if (ShouldPositionContinue(actionData))
                {
                    // positionActionItems[-1].EndIndex = i;
                    positionActionItems[-1].Duration += duration;
                }
                else
                {
                    positionActionItems.Add(new ActionItem
                    {
                        // StartIndex = i,
                        // EndIndex = i,
                        Index = posIdx,

                        StartTime = time,
                        Duration = duration,

                        RequirePreviousPosition = DoesPositionRequireRelativePosition(actionData, SchemaDefs.Relative.Previous),
                        RequirePreviousDirection = DoesPositionRequireRelativeDirection(actionData, SchemaDefs.Relative.Previous),
                        RequirePreviousUpDirection = DoesPositionRequireRelativeDirection(actionData, SchemaDefs.Relative.Previous, requiresUp: true),

                        RequireNextPosition = DoesPositionRequireRelativePosition(actionData, SchemaDefs.Relative.Next),
                        RequireNextDirection = DoesPositionRequireRelativeDirection(actionData, SchemaDefs.Relative.Next),
                        RequireNextUpDirection = DoesPositionRequireRelativeDirection(actionData, SchemaDefs.Relative.Next, requiresUp: true),

                        RequireFirstPosition = DoesPositionRequireRelativePosition(actionData, SchemaDefs.Relative.First),
                        RequireFirstDirection = DoesPositionRequireRelativeDirection(actionData, SchemaDefs.Relative.First),
                        RequireFirstUpDirection = DoesPositionRequireRelativeDirection(actionData, SchemaDefs.Relative.First, requiresUp: true),

                        Data = actionData
                    });
                    posIdx++;
                }

                if (ShouldDirectionContinue(actionData))
                {
                    // directionActionItems[-1].EndIndex = i;
                    directionActionItems[-1].Duration += duration;
                }
                else
                {
                    directionActionItems.Add(new ActionItem
                    {
                        // StartIndex = i,
                        // EndIndex = i,
                        Index = dirIdx,

                        StartTime = time,
                        Duration = duration,

                        RequirePreviousPosition = DoesDirectionRequireRelativePosition(actionData, SchemaDefs.Relative.Previous),
                        RequirePreviousDirection = DoesDirectionRequireRelativeDirection(actionData, SchemaDefs.Relative.Previous),
                        RequirePreviousUpDirection = DoesDirectionRequireRelativeDirection(actionData, SchemaDefs.Relative.Previous, requiresUp: true),

                        RequireNextPosition = DoesDirectionRequireRelativePosition(actionData, SchemaDefs.Relative.Next),
                        RequireNextDirection = DoesDirectionRequireRelativeDirection(actionData, SchemaDefs.Relative.Next),
                        RequireNextUpDirection = DoesDirectionRequireRelativeDirection(actionData, SchemaDefs.Relative.Next, requiresUp: true),

                        RequireFirstPosition = DoesDirectionRequireRelativePosition(actionData, SchemaDefs.Relative.First),
                        RequireFirstDirection = DoesDirectionRequireRelativeDirection(actionData, SchemaDefs.Relative.First),
                        RequireFirstUpDirection = DoesDirectionRequireRelativeDirection(actionData, SchemaDefs.Relative.First, requiresUp: true),

                        Data = actionData
                    });
                    dirIdx++;
                }

                if (ShouldUpDirectionContinue(actionData))
                {
                    // upDirectionActionItems[-1].EndIndex = i;
                    upDirectionActionItems[-1].Duration += duration;
                }
                else
                {
                    upDirectionActionItems.Add(new ActionItem
                    {
                        // StartIndex = i,
                        // EndIndex = i,
                        Index = upIdx,

                        StartTime = time,
                        Duration = duration,

                        RequirePreviousPosition = DoesDirectionRequireRelativePosition(actionData, SchemaDefs.Relative.Previous, isUp: true),
                        RequirePreviousDirection = DoesDirectionRequireRelativeDirection(actionData, SchemaDefs.Relative.Previous, isUp: true),
                        RequirePreviousUpDirection = DoesDirectionRequireRelativeDirection(actionData, SchemaDefs.Relative.Previous, isUp: true, requiresUp: true),

                        RequireNextPosition = DoesDirectionRequireRelativePosition(actionData, SchemaDefs.Relative.Next, isUp: true),
                        RequireNextDirection = DoesDirectionRequireRelativeDirection(actionData, SchemaDefs.Relative.Next, isUp: true),
                        RequireNextUpDirection = DoesDirectionRequireRelativeDirection(actionData, SchemaDefs.Relative.Next, isUp: true, requiresUp: true),

                        RequireFirstPosition = DoesDirectionRequireRelativePosition(actionData, SchemaDefs.Relative.First, isUp: true),
                        RequireFirstDirection = DoesDirectionRequireRelativeDirection(actionData, SchemaDefs.Relative.First, isUp: true),
                        RequireFirstUpDirection = DoesDirectionRequireRelativeDirection(actionData, SchemaDefs.Relative.First, isUp: true, requiresUp: true),

                        Data = actionData
                    });
                    upIdx++;
                }
            }

            PositionAction[] positionActions = new PositionAction[positionActionItems.Count];
            DirectionAction[] directionActions = new DirectionAction[directionActionItems.Count];
            DirectionAction[] upDirectionActions = new DirectionAction[upDirectionActionItems.Count];

            //Instancing everything that doesn't have dependancies
            //Positions
            foreach (ActionItem actionItem in positionActionItems)
            {
                if (actionItem.RequireRelative)
                {
                    continue;
                }

                PositionAction action = DataToPositionAction(
                    actionItem.Data, actionItem.StartTime, actionItem.Duration,
                    PositionDefault, DirectionDefault, UpDirectionDefault,
                    PositionDefault, DirectionDefault, UpDirectionDefault,
                    PositionDefault, DirectionDefault, UpDirectionDefault
                );

                // actionItem.Instance = action;
                positionActions[actionItem.Index] = action;

                // actionItem.StartValue = action.GetPositionDirection(action.StartTime).position;
                // actionItem.EndValue = action.GetPositionDirection(action.EndTime).position;
            }

            //Directions
            foreach (ActionItem actionItem in directionActionItems)
            {
                if (actionItem.RequireRelative)
                {
                    continue;
                }

                DirectionAction action = DataToDirectionAction(
                    actionItem.Data, actionItem.StartTime, actionItem.Duration,
                    PositionDefault, DirectionDefault, UpDirectionDefault,
                    PositionDefault, DirectionDefault, UpDirectionDefault,
                    PositionDefault, DirectionDefault, UpDirectionDefault
                );

                directionActions[actionItem.Index] = action;

                //TODO check for existing instance of position action for overlapping time, and use to instance start/end directions
            }

            // Up directions
            foreach (ActionItem actionItem in upDirectionActionItems)
            {
                if (actionItem.RequireRelative)
                {
                    continue;
                }

                DirectionAction action = DataToDirectionAction(
                    actionItem.Data, actionItem.StartTime, actionItem.Duration,
                    PositionDefault, DirectionDefault, UpDirectionDefault,
                    PositionDefault, DirectionDefault, UpDirectionDefault,
                    PositionDefault, DirectionDefault, UpDirectionDefault,
                    isUp: true
                );

                upDirectionActions[actionItem.Index] = action;

                //TODO check for existing instance of position action for overlapping time, and use to instance start/end upDirections
            }

            data.PositionActions = positionActions;
            data.DirectionActions = directionActions;
            data.UpDirectionActions = upDirectionActions;

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
            )
            {
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

        private string GetValidSchema(JToken jToken)
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

        private int GetPathOneOf(string kind, JToken jToken)
        {
            IList<JSchema> oneOf = _pathSchema.GetValueOrDefault(kind);

            if (oneOf is null)
            {
                return -1;
            }

            for (int i = 0; i < oneOf.Count; i++)
            {
                if (jToken.IsValid(oneOf[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        private bool ShouldPositionContinue(JToken data)
        {
            return ValueOrDefault(data, "position", SchemaDefs.Continue.Value) == SchemaDefs.Continue.Value;
        }

        private bool ShouldDirectionContinue(JToken data)
        {
            JToken directionData = data.SelectToken("rotation") ?? data.SelectToken("lookAt") ?? data.SelectToken("lookAway");

            if (directionData is null)
            {
                return true;
            }

            return ToObjectOrDefualt(directionData, "") == "continue";
        }

        private bool ShouldUpDirectionContinue(JToken data)
        {
            JToken directionData = data.SelectToken("rotation") ?? data.SelectToken("lookUp") ?? data.SelectToken("lookDown");

            if (directionData is null)
            {
                return true;
            }

            return ToObjectOrDefualt(directionData, "") == "continue";
        }

        private bool DoesPositionRequireRelativePosition(JToken data, string relative)
        {
            JToken positionData = data.SelectToken(SchemaDefs.Action.Position);

            if (positionData is null)
            {
                return SchemaDefs.Action.PositionDefault == relative;
            }

            return GetValidSchema(positionData) switch
            {
                SchemaDefs.Relative.Def => ToObjectOrDefualt(positionData, "") == relative,
                SchemaDefs.Path.Def => DoesPathRequireRelativePosition(positionData, relative),
                _ => false
            };
        }

        private bool DoesPositionRequireRelativeDirection(JToken data, string relative, bool requiresUp = false)
        {
            JToken positionData = data.SelectToken(SchemaDefs.Action.Position);

            if (positionData is null)
            {
                return false;
            }

            return GetValidSchema(positionData) switch
            {
                SchemaDefs.Path.Def => DoesPathRequireRelativeDirection(positionData, relative, requiresUp),
                _ => false
            };
        }

        private bool DoesDirectionRequireRelativePosition(JToken data, string relative, bool isUp = false)
        {
            JToken directionData = (
                data.SelectToken(isUp ? SchemaDefs.Action.LookUp : SchemaDefs.Action.LookAt)
                ?? data.SelectToken(isUp ? SchemaDefs.Action.LookUp : SchemaDefs.Action.LookAt)
                ?? data.SelectToken(SchemaDefs.Action.Rotation)
            );

            if (directionData is null)
            {
                return false;
            }

            return GetValidSchema(directionData) switch
            {
                SchemaDefs.Path.Def => DoesPathRequireRelativePosition(directionData, relative),
                _ => false
            };
        }

        private bool DoesDirectionRequireRelativeDirection(JToken data, string relative, bool isUp = false, bool requiresUp = false)
        {
            JToken directionData = (
                data.SelectToken(isUp ? SchemaDefs.Action.LookUp : SchemaDefs.Action.LookAt)
                ?? data.SelectToken(isUp ? SchemaDefs.Action.LookUp : SchemaDefs.Action.LookAt)
                ?? data.SelectToken(SchemaDefs.Action.Rotation)
            );

            if (directionData is null)
            {
                return (isUp ? SchemaDefs.Action.LookUpDefault : SchemaDefs.Action.LookAtDefault) == relative;
            }

            return GetValidSchema(directionData) switch
            {
                SchemaDefs.Relative.Def => ToObjectOrDefualt(directionData, "") == relative,
                SchemaDefs.Path.Def => DoesPathRequireRelativeDirection(directionData, relative, requiresUp),
                SchemaDefs.Easing.Def => true,
                SchemaDefs.DirectionBetween.Def => (
                    ValueOrDefault(directionData, SchemaDefs.DirectionBetween.Start, SchemaDefs.DirectionBetween.StartDefault) == relative
                    || ValueOrDefault(directionData, SchemaDefs.DirectionBetween.End, SchemaDefs.DirectionBetween.EndDefault) == relative
                ),
                SchemaDefs.RotationBetween.Def => (
                    ValueOrDefault(directionData, SchemaDefs.RotationBetween.Start, SchemaDefs.RotationBetween.StartDefault) == relative
                    || ValueOrDefault(directionData, SchemaDefs.RotationBetween.End, SchemaDefs.RotationBetween.EndDefault) == relative
                ),
                _ => false
            };
        }

        private bool DoesPathRequireRelativePosition(JToken pathData, string relative)
        {
            //TODO this is not robust enough. Sort it out!
            if (ValueOrDefault(pathData, "start", "previous") == relative || ValueOrDefault(pathData, "end", "next") == relative)
            {
                return true;
            }
            return false;
        }

        private bool DoesPathRequireRelativeDirection(JToken pathData, string relative, bool requiresUp = false)
        {
            return false;
        }

        private PositionAction DataToPositionAction(JToken data, float startTime, float duration,
            Vector3 positionFirst, Vector3 directionFirst, Vector3 upDirectionFirst,
            Vector3 positionPrevious, Vector3 directionPrevious, Vector3 upDirectionPrevious,
            Vector3 positionNext, Vector3 directionNext, Vector3 upDirectionNext
        )
        {
            //Note continueAction schema should have been handled and position token is garanteed
            switch (GetValidSchema(data.SelectToken("position")))
            {
                default:
                    return new PositionActionHold(positionPrevious);
            }
        }

        private DirectionAction DataToDirectionAction(JToken data, float startTime, float duration,
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

            switch (GetValidSchema(directionData))
            {
                default:
                    directionAction = new DirectionActionHold(isUp ? upDirectionPrevious : upDirectionPrevious);
                    break;
            }

            directionAction.ReverseDirection = reverse;
            return directionAction;
        }

        private Vector3 DataToPosition(JToken data, Vector3 first, Vector3 previous, Vector3 next)
        {
            return GetValidSchema(data) switch
            {
                SchemaDefs.Relative.Def => ToObjectOrDefualt(data, "") switch
                {
                    SchemaDefs.Relative.First => first,
                    SchemaDefs.Relative.Previous => previous,
                    SchemaDefs.Relative.Next => next,
                    _ => PositionDefault
                },
                SchemaDefs.NamedPosition.Def => DataToNamedPosition(ToObjectOrDefualt(data, "")),
                SchemaDefs.Position.Def => DataToPosition(data),
                _ => PositionDefault
            };
        }

        private static Vector3 DataToNamedPosition(string name)
        {
            return name switch
            {
                _ => Vector3.zero
            };
        }

        private static Vector3 DataToPosition(JToken data)
        {
            //TODO Add subtype parameters: WCS, source, etc
            return new Vector3(
                ValueOrDefault(data, "x", 0f),
                ValueOrDefault(data, "y", 0f),
                ValueOrDefault(data, "z", 0f)
            );
        }

        private Vector3 DataToDirection(JToken data, Vector3 first, Vector3 previous, Vector3 next)
        {
            return GetValidSchema(data) switch
            {
                SchemaDefs.Relative.Def => ToObjectOrDefualt(data, "") switch
                {
                    SchemaDefs.Relative.First => first,
                    SchemaDefs.Relative.Previous => previous,
                    SchemaDefs.Relative.Next => next,
                    _ => DirectionDefault
                },
                SchemaDefs.NamedDirection.Def => DataToNamedDirection(ToObjectOrDefualt(data, "")),
                SchemaDefs.Direction.Def => DataToDirection(data),
                _ => DirectionDefault
            };
        }

        private Vector3 DataToNamedDirection(string name)
        {
            return name switch
            {
                SchemaDefs.NamedDirection.Forward => Vector3.forward,
                SchemaDefs.NamedDirection.Back => Vector3.back,
                SchemaDefs.NamedDirection.Up => Vector3.up,
                SchemaDefs.NamedDirection.Down => Vector3.down,
                SchemaDefs.NamedDirection.Left => Vector3.left,
                SchemaDefs.NamedDirection.Right => Vector3.right,
                _ => DirectionDefault
            };
        }

        private Vector3 DataToDirection(JToken data)
        {
            float x = ValueOrDefault(data, "x", 0f);
            float y = ValueOrDefault(data, "y", 0f);
            float z = ValueOrDefault(data, "z", 0f);

            if (x == 0 && y == 0 && z == 0)
            {
                return DirectionDefault;
            }

            return new Vector3(x, y, z).normalized;
        }

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
        
        private Path DataToPath(JToken data,
            Vector3 positionFirst, Vector3 directionFirst, Vector3 upDirectionFirst,
            Vector3 positionPrevious, Vector3 directionPrevious, Vector3 upDirectionPrevious,
            Vector3 positionNext, Vector3 directionNext, Vector3 upDirectionNext
        )
        {
            // JToken startData = data.SelectToken(SchemaDefs.Path.Start);
            // bool hasStart = startData is not null;
            Vector3 start = DataToPosition(
                data.SelectToken(SchemaDefs.Path.Start) ?? SchemaDefs.Path.StartDefault,
                positionFirst, positionPrevious, positionNext
            );

            Vector3 end = DataToPosition(
                data.SelectToken(SchemaDefs.Path.End) ?? SchemaDefs.Path.StartDefault,
                positionFirst, positionPrevious, positionNext
            );

            JToken easingData = data.SelectToken(SchemaDefs.Path.Easing);
            Easing easing = easingData is null ? new Easing() : DataToEasing(easingData);

            return ValueOrDefault(data, "kind", SchemaDefs.Path.KindDefault) switch
            {
                SchemaDefs.Path.Line.Kind => new LinePath(start, end),
                SchemaDefs.Path.Circle.Kind => GetPathOneOf(SchemaDefs.Path.Circle.Kind, data) switch
                {
                    SchemaDefs.Path.Circle.StartCenterEnd.Index => new CirclePath(
                        start: start,
                        end: end,
                        center: DataToPosition(
                            data.SelectToken(SchemaDefs.Path.Circle.Center) ?? SchemaDefs.Path.Circle.CenterDefault,
                            positionFirst, positionPrevious, positionNext
                        ),
                        rotations: ValueOrDefault(data, SchemaDefs.Path.Circle.StartCenterEnd.Rotations, SchemaDefs.Path.Circle.StartCenterEnd.RotationsDefault)
                    ),
                    SchemaDefs.Path.Circle.StartCenterAxis.Index => new CirclePath(
                        start: start,
                        center: DataToPosition(
                            data.SelectToken(SchemaDefs.Path.Circle.Center) ?? SchemaDefs.Path.Circle.CenterDefault,
                            positionFirst, positionPrevious, positionNext
                        ),
                        axis: DataToDirection(
                            data.SelectToken(SchemaDefs.Path.Circle.Axis)
                        ),
                        rotations: ValueOrDefault(data, SchemaDefs.Path.Circle.StartCenterAxis.Rotations, SchemaDefs.Path.Circle.StartCenterAxis.RotationsDefault)
                    )
                },
                //TODO Spiral path
                _ => null //TODO throw error
            };
        }
    }
}