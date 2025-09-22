using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace VideoMaker
{
    public class IdvsParser
    {
        private enum LineOption
        {
            Position,
            Start,
            Wait,
            Move,
            Rotate,
            Emptyline,
            Setting,
            Invalid
        }

        private List<(LineOption, Regex)> _valid = new List<(LineOption, Regex)>
        {
            //pN is {[XN,YN,ZN],[xN,yN,zN]}       -   Position declaration
            (LineOption.Position, new Regex(@"^\s*(\w+)\s+is\s+\{\s*(?:\[|\()\s*(-?\s*\d+(?:\.\d+)?)\s*,\s*(-?\s*\d+(?:\.\d+)?)\s*,\s*(-?\s*\d+(?:\.\d+)?)\s*(?:\]|\))\s*,\s*(?:\[|\()\s*(-?\s*\d+(?:\.\d+)?)\s*,\s*(-?\s*\d+(?:\.\d+)?)\s*,\s*(-?\s*\d+(?:\.\d+)?)(?:\]|\))\s*\}\s*$")),
            //Start at pX                         -   Initial command
            (LineOption.Start,   new Regex(@"^\s*Start\s+at\s+(\w+)\s*$")),
            //Wait N seconds                      -   command
            (LineOption.Wait,    new Regex(@"^\s*Wait\s+(\d+(?:\.\d+)?)\s+seconds?\s*$")),
            //Move in METHOD to pX over N seconds -   command
            (LineOption.Move,    new Regex(@"^\s*Move\s+in\s+(\w+)\sto\s+(\w+)+\s+over\s+(\d+(?:\.\d+)?)\s+seconds?\s*$")),
            //Rotate around pX N times            -   command
            (LineOption.Rotate,  new Regex(@"^\s*Rotate\s+around\s+(\w+)\s+(\d+(?:\.\d+)?)\s+times?\s*$")),
            (LineOption.Emptyline, new Regex(@"^\s*$")),
            (LineOption.Setting, new Regex(@"^\s*(\w+)\s*:\s*(\w+)\s*$"))
            // ("Rotate1",  @"Rotate\s+around\s+(\w+)\s+(\d+(?:\.\d+)?)+\s+times\s+turn\s+(\d+(?:\.\d+)?)seconds"),
            // ("Rotate2",  @"Rotate\s+around\s+(\w+)\s+(\d+(?:\.\d+)?)+\s+times\s+orbit\s+(\d+(?:\.\d+)?)seconds"),
            // ("Rotate3",  @"Rotate\s+around\s+(\w+)\s+(\d+(?:\.\d+)?)+\s+times\s+turn\s+(\d+(?:\.\d+)?)seconds\s+orbit\s+(\d+(?:\.\d+)?)seconds"),
            // ("Rotate4",  @"Rotate\s+around\s+(\w+)\s+(\d+(?:\.\d+)?)+\s+times\s+orbit\s+(\d+(?:\.\d+)?)seconds\s+turn\s+(\d+(?:\.\d+)?)seconds"),
        };

        public (VideoSettings, List<VideoLocation>, List<object>) Parse(StreamReader script, string filename)
        {
            VideoSettings settings = new();
            List<VideoLocation> locs = new();
            List<object> commands = new();
            string line = "";
            int lineNumber = 0;
            while ((line = script.ReadLine()) != null)
            {
                lineNumber++;
                if (line.Contains('#'))
                    line = line.Substring(0, line.IndexOf("#")); //Strip comments.
                Match match = null;
                LineOption matchType = LineOption.Invalid;
                for (int i = 0; i < _valid.Count(); i++)
                {
                    match = _valid[i].Item2.Match(line);
                    if (match.Success)
                    {
                        matchType = _valid[i].Item1;
                        break;
                    }
                }
                switch (matchType)
                {
                    case LineOption.Position: //Position:
                        string name = match.Groups[1].Value;
                        Vector3 pos = new Vector3(float.Parse(match.Groups[2].Value), float.Parse(match.Groups[3].Value), float.Parse(match.Groups[4].Value));
                        Vector3 rot = new Vector3(float.Parse(match.Groups[5].Value), float.Parse(match.Groups[6].Value), float.Parse(match.Groups[7].Value));
                        foreach (var loc in locs)
                        {
                            if (loc.GetAlias().Equals(name))
                            {
                                UnityEngine.Debug.LogWarning("Parse warning in " + filename + $":{lineNumber}: Redefinition of position alias `" + name + "`.");
                                locs.Remove(loc);
                                break;
                            }
                        }
                        VideoLocation p = new(name, pos, rot);
                        locs.Add(p);
                        break;

                    case LineOption.Start: //Start:
                        string startName = "Start";
                        string startPos = match.Groups[1].Value;
                        VideoLocation startPosLoc = null;
                        foreach (var loc in locs)
                        {
                            if (loc.GetAlias().Equals(startPos))
                            {
                                startPosLoc = loc;
                                break;
                            }
                        }

                        bool validStart = true;
                        if (startPosLoc == null)
                        {
                            UnityEngine.Debug.LogError("Parse error in " + filename + $":{lineNumber}: Invalid position alias `" + startPos + "`.");
                            validStart = false;
                        }

                        if (validStart)
                        {
                            StartCommand start = new(startName, startPosLoc);
                            commands.Add(start);
                        }
                        break;

                    case LineOption.Wait: //Wait
                        string waitName = "Wait";
                        float waitTime = float.Parse(match.Groups[1].Value);
                        WaitCommand wait = new(waitName, waitTime);
                        commands.Add(wait);
                        break;

                    case LineOption.Move: //Move
                        string moveName = "Move";
                        string moveMethodAlias = match.Groups[1].Value;
                        Method method = MoveCommand.ParseMethod(moveMethodAlias);
                        string destLocAlias = match.Groups[2].Value;
                        VideoLocation destLoc = null;
                        foreach (var loc in locs)
                        {
                            if (loc.GetAlias().Equals(destLocAlias))
                            {
                                destLoc = loc;
                                break;
                            }
                        }
                        float moveTime = float.Parse(match.Groups[3].Value);

                        bool validMove = true;
                        if (method == Method.Invalid)
                        {
                            UnityEngine.Debug.LogError("Parse error in " + filename + $":{lineNumber}: Invalid move method " + moveMethodAlias + ".");
                            validMove = false;
                        }
                        if (destLoc == null)
                        {
                            UnityEngine.Debug.LogError("Parse error in " + filename + $":{lineNumber}: Invalid position alias `" + destLocAlias + "`.");
                            validMove = false;
                        }

                        if (validMove)
                        {
                            MoveCommand move = new(moveName, destLoc, method, moveTime);
                            commands.Add(move);
                        }
                        break;

                    case LineOption.Rotate: //Rotate
                        string rotateName = "Rotate";
                        string rotateLocAlias = match.Groups[1].Value;
                        VideoLocation rotateLoc = null;
                        foreach (var loc in locs)
                        {
                            if (loc.GetAlias().Equals(rotateLocAlias))
                            {
                                rotateLoc = loc;
                                break;
                            }
                        }

                        float rotateIters = float.Parse(match.Groups[2].Value);

                        bool validRotate = true;
                        if (rotateLoc == null)
                        {
                            UnityEngine.Debug.LogError("Parse error in " + filename + $":{lineNumber}: Invalid position alias `" + rotateLocAlias + "`.");
                            validRotate = false;
                        }

                        if (validRotate)
                        {
                            RotateCommand rotate = new(rotateName, rotateLoc, rotateIters);
                            commands.Add(rotate);
                        }
                        break;

                    case LineOption.Emptyline: //Empty line
                        // Do nothing.
                        break;
                    
                    case LineOption.Setting:
                        var setting = VideoSettings.ParseSetting(match.Groups[1].Value);
                        string settingVal = match.Groups[2].Value;
                        bool validSetting = true;
                        if (setting == VideoSettings.SettingOption.Invalid)
                        {
                            UnityEngine.Debug.LogError("Parse error in " + filename + $":{lineNumber}: Invalid setting name `" + match.Groups[1].Value + "`.");
                        }
                        int settingValue;
                        if (setting == VideoSettings.SettingOption.Logopos)
                        {
                            settingValue = (int) VideoScriptData.ParsePosition(settingVal);
                            if (settingValue == (int)VideoScriptData.LogoPosition.Invalid)
                            {
                                UnityEngine.Debug.LogError("Parse error in " + filename + $":{lineNumber}: Invalid logo position value `" + settingVal + "`.");
                                validSetting = false;
                            }
                        }
                        else
                        {
                            settingValue = int.Parse(settingVal);
                        }
                        if (validSetting)
                        {
                            settings.SetSetting(setting, settingValue);
                        }
                        break;

                    default:
                        UnityEngine.Debug.LogError("Line " + filename + $":{lineNumber}: \"" + line + "\" does not match any acceptable pattern!");
                        break;
                }
            }
            return (settings, locs, commands);
        }
    }
}