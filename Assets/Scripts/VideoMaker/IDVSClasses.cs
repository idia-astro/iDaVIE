using Unity.VisualScripting;
using UnityEngine;

namespace VideoMaker
{
    public enum Method
    {
        Line,
        Arc,
        Invalid
    }

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

    public class VideoSettings
    {
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

    public class VideoLocation
    {
        string _alias;
        public Vector3 position { get; private set; }
        public Vector3 rotation { get; private set; }
        
        public Vector3 forward => Quaternion.Euler(rotation) * Vector3.forward;
        public Vector3 up => Quaternion.Euler(rotation) * Vector3.up;

        public VideoLocation(string name, Vector3 pos, Vector3 rot)
        {
            _alias = name;
            position = pos;
            rotation = rot;
        }

        public string GetAlias()
        {
            return _alias;
        }

        public Vector3 Rotate(Vector3 direction)
        {
            return Quaternion.Euler(rotation) * direction;
        }
    }

    public class Command
    {
        public string name { get; private set; }
        public Command(string name)
        {
            name = name;
        }
    }

    public class StartCommand : Command
    {
        public VideoLocation position { get; private set; }
        public StartCommand(string name, VideoLocation pos) : base(name)
        {
            position = pos;
        }
    }

    public class WaitCommand : Command
    {
        public float duration { get; private set; }
        public WaitCommand(string name, float dur = 5.0f) : base(name)
        {
            duration = dur;
        }
    }

    public class MoveCommand : Command
    {
        public VideoLocation destination { get; private set; }
        public Method method { get; private set; }
        public float duration { get; private set; }
        public MoveCommand(string name, VideoLocation dest, Method method, float dur = 5.0f) : base(name)
        {
            destination = dest;
            this.method = method;
            duration = dur;
        }

        public static Method ParseMethod(string methodAlias)
        {
            if (methodAlias.Equals("LINE", System.StringComparison.OrdinalIgnoreCase))
                return Method.Line;
            if (methodAlias.Equals("ARC", System.StringComparison.OrdinalIgnoreCase))
                return Method.Arc;
            return Method.Invalid;
        }
    }

    public class RotateCommand : Command
    {
        public VideoLocation centrePoint { get; private set; }
        public RotationAxes rotationAxis { get; private set; }
        public float iterations { get; private set; }
        public float turnDuration { get; private set; }
        public float orbitDuration { get; private set; }

        public RotateCommand(string name, VideoLocation centre, 
            float iterations = 1.0f, 
            RotationAxes rotAxis = RotationAxes.PosY, 
            float turn = 5.0F, 
            float orbit = 10.0F) : base(name)
        {
            centrePoint = centre;
            rotationAxis = rotAxis;
            this.iterations = iterations;
            turnDuration = turn;
            orbitDuration = orbit;
        }
    }
}