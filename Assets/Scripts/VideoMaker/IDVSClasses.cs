using Unity.VisualScripting;
using UnityEngine;

namespace VideoMaker
{
    /// <summary>
    /// The possible movement options.
    /// </summary>
    public enum MovementMethod
    {
        Line,
        Arc,
        Invalid
    }

    /// <summary>
    /// The possible rotation axes for the rotate command.
    /// </summary>
    public enum RotationAxes
    {
        PosX,
        NegX,
        PosY,
        NegY,
        PosZ,
        NegZ,
        Invalid
    }

    /// <summary>
    /// A small utility class holding the possible settings for parsing an IDVS file.
    /// </summary>
    public class VideoSettings
    {
        /// <summary>
        /// The various setting options, used when checking if a setting command is valid.
        /// </summary>
        public enum SettingOption
        {
            Height,
            Width,
            Framerate,
            Logopos,
            Invalid
        }
        public VideoScriptData.LogoPosition logoPos { get; private set; } = VideoScriptData.LogoPosition.BottomRight;
        public int height { get; private set; } = 720;
        public int width { get; private set; } = 1280;
        public int framerate { get; private set; } = 20;

        /// <summary>
        /// Called to set a function value.
        /// </summary>
        /// <param name="set">The setting to set.</param>
        /// <param name="value">The value that `set` should be.</param>
        public void SetSetting(SettingOption set, int value)
        {
            switch (set)
            {
                case SettingOption.Height:
                    height = value;
                    break;
                case SettingOption.Width:
                    width = value;
                    break;
                case SettingOption.Framerate:
                    framerate = value;
                    break;
                case SettingOption.Logopos:
                    logoPos = (VideoScriptData.LogoPosition)value;
                    break;
                default:
                    UnityEngine.Debug.LogError("Invalid setting option " + set.ToString() + " when setting settings in videoSettings class. How did you even manage that?");
                    break;
            }
        }

        /// <summary>
        /// Determines which SettingOption a given string refers to.
        /// </summary>
        /// <param name="settingName">The string to evaluate.</param>
        /// <returns></returns>
        public static SettingOption ParseSetting(string settingName)
        {
            if (settingName.Equals("HEIGHT", System.StringComparison.OrdinalIgnoreCase))
                return SettingOption.Height;
            if (settingName.Equals("WIDTH", System.StringComparison.OrdinalIgnoreCase))
                return SettingOption.Width;
            if (settingName.Equals("FRAMERATE", System.StringComparison.OrdinalIgnoreCase))
                return SettingOption.Framerate;
            if (settingName.Equals("LOGOPOS", System.StringComparison.OrdinalIgnoreCase))
                return SettingOption.Logopos;
            return SettingOption.Invalid;
        }
    }

    /// <summary>
    /// This class holds positions, passed to the video recorder.
    /// </summary>
    public class VideoLocation
    {
        string _alias;
        public Vector3 position { get; private set; }
        public Vector3 rotation { get; private set; }

        public Vector3 forward => Quaternion.Euler(rotation) * Vector3.forward;
        public Vector3 up => Quaternion.Euler(rotation) * Vector3.up;

        /// <summary>
        /// Constructor, sets name, position, and rotation.
        /// </summary>
        /// <param name="name">The alias of the location.</param>
        /// <param name="pos">The Euler position of the location.</param>
        /// <param name="rot">The Euler rotation of the location.</param>
        public VideoLocation(string name, Vector3 pos, Vector3 rot)
        {
            _alias = name;
            position = pos;
            rotation = rot;
        }

        /// <summary>
        /// Returns the current alias of this position.
        /// </summary>
        /// <returns>String corresponding to the alias of the position.</returns>
        public string GetAlias()
        {
            return _alias;
        }

        public Vector3 Rotate(Vector3 direction)
        {
            return Quaternion.Euler(rotation) * direction;
        }
    }

    /// <summary>
    /// The base class of any video script command.
    /// </summary>
    public class Command
    {
        //TODO: This should probably be an enumeration, not a string.
        public string name { get; private set; }
        public Command(string comName)
        {
            name = comName;
        }
    }

    /// <summary>
    /// A start command, giving an initial position.
    /// </summary>
    public class StartCommand : Command
    {
        public VideoLocation position { get; private set; }
        public StartCommand(VideoLocation pos) : base("Start")
        {
            position = pos;
        }
    }

    /// <summary>
    /// A wait command, with no change for a specified duration.
    /// </summary>
    public class WaitCommand : Command
    {
        public float duration { get; private set; }
        public WaitCommand(float dur = 5.0f) : base("Wait")
        {
            duration = dur;
        }
    }

    /// <summary>
    /// A move command, moving to a specified destination in the provided method over the specified duration.
    /// </summary>
    public class MoveCommand : Command
    {
        public VideoLocation destination { get; private set; }
        public MovementMethod method { get; private set; }
        public float duration { get; private set; }
        public MoveCommand(VideoLocation dest, MovementMethod method, float dur = 5.0f) : base("Move")
        {
            destination = dest;
            this.method = method;
            duration = dur;
        }

        /// <summary>
        /// Function to parse a string to a movement method.
        /// </summary>
        /// <param name="methodAlias">The string to evaluate.</param>
        /// <returns>A MovementMethod, Invalid if not corresponding to any of the valid methods.</returns>
        public static MovementMethod ParseMethod(string methodAlias)
        {
            if (methodAlias.Equals("LINE", System.StringComparison.OrdinalIgnoreCase))
                return MovementMethod.Line;
            if (methodAlias.Equals("ARC", System.StringComparison.OrdinalIgnoreCase))
                return MovementMethod.Arc;
            return MovementMethod.Invalid;
        }
    }

    /// <summary>
    /// A rotate command, rotating about a given point, for a specified number of revolutions, with optional determinations of the rotation axis, the duration of the initial turn to face the centre point, and the duration of each orbit.
    /// </summary>
    public class RotateCommand : Command
    {
        public static readonly float defaultIterations = 1.0f;
        public static readonly RotationAxes defaultRotAxis = RotationAxes.PosY;
        public static readonly float defaultTurnDur = 5.0f;
        public static readonly float defaultOrbitDur = 10.0f;
        public VideoLocation centrePoint { get; private set; }
        public RotationAxes rotationAxis { get; private set; }
        public float iterations { get; private set; }
        public float turnDuration { get; private set; }
        public float orbitDuration { get; private set; }

        public RotateCommand(VideoLocation centre,
            float iterations = 1.0f,
            RotationAxes rotAxis = RotationAxes.PosY,
            float turn = 5.0F,
            float orbit = 10.0F) : base("Rotate")
        {
            centrePoint = centre;
            rotationAxis = rotAxis;
            this.iterations = iterations;
            turnDuration = turn;
            orbitDuration = orbit;
        }
    }
}