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
        VideoScriptData.LogoPosition _logoPos = VideoScriptData.LogoPosition.BottomRight;
        int _height = 720;
        int _width = 1280;
        int _framerate = 20;

        public void setSetting(settingOption set, int value)
        {
            switch (set)
            {
                case settingOption.HEIGHT:
                    _height = value;
                    break;
                case settingOption.WIDTH:
                    _width = value;
                    break;
                case settingOption.FRAMERATE:
                    _framerate = value;
                    break;
                case settingOption.LOGOPOS:
                    _logoPos = (VideoScriptData.LogoPosition)value;
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
        Vector3 _position;
        Vector3 _rotation;

        public videoLocation(string name, Vector3 pos, Vector3 rot)
        {
            _alias = name;
            _position = pos;
            _rotation = rot;
        }

        public string getAlias()
        {
            return _alias;
        }
    }

    public class command
    {
        string _name;
        public command(string name)
        {
            _name = name;
        }
    }

    public class startCommand : command
    {
        videoLocation _position;
        public startCommand(string name, videoLocation pos) : base(name)
        {
            _position = pos;
        }
    }

    public class waitCommand : command
    {
        float _duration = 5.0F;
        public waitCommand(string name, float dur) : base(name)
        {
            _duration = dur;
        }
    }

    public class moveCommand : command
    {
        videoLocation _destination;
        Method _method;
        float _duration = 5.0F;
        public moveCommand(string name, videoLocation dest, Method method, float dur) : base(name)
        {
            _destination = dest;
            _method = method;
            _duration = dur;
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
        videoLocation _centrePoint;
        RotationAxes _rotationAxis;
        float _iterations = 1.0F;
        float _turnDuration = 5.0F;
        float _orbitDuration = 10.0F;

        public rotateCommand(string name, videoLocation centre, float iterations, RotationAxes rotAxis = RotationAxes.PosY, float turn = 5.0F, float orbit = 10.0F) : base(name)
        {
            _centrePoint = centre;
            _rotationAxis = rotAxis;
            _iterations = iterations;
            _turnDuration = turn;
            _orbitDuration = orbit;
        }
    }
}