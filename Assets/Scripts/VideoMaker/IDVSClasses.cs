using Unity.VisualScripting;
using UnityEngine;

namespace VideoMaker
{
    public enum Method
    {
        LINE,
        ARC,
        INVALID
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

    public class videoSettings
    {
        public enum settingOption
        {
            HEIGHT,
            WIDTH,
            FRAMERATE,
            LOGOPOS,
            INVALID
        }
        public VideoScriptData.LogoPosition logoPos { get; private set; } = VideoScriptData.LogoPosition.BottomRight;
        public int height { get; private set; } = 720;
        public int width { get; private set; } = 1280;
        public int framerate { get; private set; } = 20;

        public void setSetting(settingOption set, int value)
        {
            switch (set)
            {
                case settingOption.HEIGHT:
                    height = value;
                    break;
                case settingOption.WIDTH:
                    width = value;
                    break;
                case settingOption.FRAMERATE:
                    framerate = value;
                    break;
                case settingOption.LOGOPOS:
                    logoPos = (VideoScriptData.LogoPosition)value;
                    break;
                default:
                    UnityEngine.Debug.LogError("Invalid setting option " + set.ToString() + " when setting settings in videoSettings class. How did you even manage that?");
                    break;
            }
        }

        public static settingOption ParseSetting(string settingName)
        {
            if (settingName.Equals("HEIGHT", System.StringComparison.OrdinalIgnoreCase))
                return settingOption.HEIGHT;
            if (settingName.Equals("WIDTH", System.StringComparison.OrdinalIgnoreCase))
                return settingOption.WIDTH;
            if (settingName.Equals("FRAMERATE", System.StringComparison.OrdinalIgnoreCase))
                return settingOption.FRAMERATE;
            if (settingName.Equals("LOGOPOS", System.StringComparison.OrdinalIgnoreCase))
                return settingOption.LOGOPOS;
            return settingOption.INVALID;
        }
    }

    public class videoLocation
    {
        string _alias;
        public Vector3 position { get; private set; }
        public Vector3 rotation { get; private set; }
        
        public Vector3 forward => Quaternion.Euler(rotation) * Vector3.forward;
        public Vector3 up => Quaternion.Euler(rotation) * Vector3.up;

        public videoLocation(string name, Vector3 pos, Vector3 rot)
        {
            _alias = name;
            position = pos;
            rotation = rot;
        }

        public string getAlias()
        {
            return _alias;
        }
    }

    public class command
    {
        public string name { get; private set; }
        public command(string name)
        {
            name = name;
        }
    }

    public class startCommand : command
    {
        public videoLocation position { get; private set; }
        public startCommand(string name, videoLocation pos) : base(name)
        {
            position = pos;
        }
    }

    public class waitCommand : command
    {
        public float duration { get; private set; }
        public waitCommand(string name, float dur = 5.0f) : base(name)
        {
            duration = dur;
        }
    }

    public class moveCommand : command
    {
        public videoLocation destination { get; private set; }
        public Method method { get; private set; }
        public float duration { get; private set; }
        public moveCommand(string name, videoLocation dest, Method method, float dur = 5.0f) : base(name)
        {
            destination = dest;
            this.method = method;
            duration = dur;
        }

        public static Method ParseMethod(string methodAlias)
        {
            if (methodAlias.Equals("LINE", System.StringComparison.OrdinalIgnoreCase))
                return Method.LINE;
            if (methodAlias.Equals("ARC", System.StringComparison.OrdinalIgnoreCase))
                return Method.ARC;
            return Method.INVALID;
        }
    }

    public class rotateCommand : command
    {
        public videoLocation centrePoint { get; private set; }
        public RotationAxes rotationAxis { get; private set; }
        public float iterations { get; private set; }
        public float turnDuration { get; private set; }
        public float orbitDuration { get; private set; }

        public rotateCommand(string name, videoLocation centre, 
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