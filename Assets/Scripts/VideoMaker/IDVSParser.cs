using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace VideoMaker
{
    public class IDVSParser
    {
        private enum lineOption
        {
            POSITION,
            START,
            WAIT,
            MOVE,
            ROTATE,
            EMPTYLINE,
            SETTING,
            INVALID
        }

        private List<(lineOption, Regex)> valid = new List<(lineOption, Regex)>
        {
            (lineOption.POSITION, new Regex(@"^\s*(\w+)\s+is\s+\{\[(\d+(\.\d+)?),(\d+(\.\d+)?),(\d+(\.\d+)?)\],\[(\d+(\.\d+)?),(\d+(\.\d+)?),(\d+(\.\d+)?)\]\}")),      //pN is {[XN,YN,ZN],[xN,yN,zN]}  -   Position declaration
            (lineOption.START,   new Regex(@"^\s*Start\s+at\s+(\w+)")),                                                      //Start at pX                       -   Initial command
            (lineOption.WAIT,    new Regex(@"^\s*Wait\s+(\d+(\.\d+)?)\s+seconds?")),                                                  //Wait N seconds                    -   command
            (lineOption.MOVE,    new Regex(@"^\s*Move\s+in\s+(\w+)\sto\s+(\w+)+\s+over\s+(\d+(\.\d+)?)\s+seconds?")),                    //Move in METHOD to pX over N seconds         -   command
            (lineOption.ROTATE,  new Regex(@"^\s*Rotate\s+around\s+(\w+)\s+(\d+(\.\d+)?)\s+times?")),                                  //Rotate around pX N times          -   command
            (lineOption.EMPTYLINE, new Regex(@"^\s*$")),
            (lineOption.SETTING, new Regex(@"^\s*(\w+)\s*:\s*(\w+)"))
            // ("Rotate1",  @"Rotate\s+around\s+(\w+)\s+(\d+(\.\d+)?)+\s+times\s+turn\s+(\d+(\.\d+)?)seconds"),
            // ("Rotate2",  @"Rotate\s+around\s+(\w+)\s+(\d+(\.\d+)?)+\s+times\s+orbit\s+(\d+(\.\d+)?)seconds"),
            // ("Rotate3",  @"Rotate\s+around\s+(\w+)\s+(\d+(\.\d+)?)+\s+times\s+turn\s+(\d+(\.\d+)?)seconds\s+orbit\s+(\d+(\.\d+)?)seconds"),
            // ("Rotate4",  @"Rotate\s+around\s+(\w+)\s+(\d+(\.\d+)?)+\s+times\s+orbit\s+(\d+(\.\d+)?)seconds\s+turn\s+(\d+(\.\d+)?)seconds"),
        };

        public (videoSettings, List<videoLocation>, List<object>) Parse(StreamReader script, string filename)
        {
            videoSettings settings = new();
            List<videoLocation> locs = new();
            List<object> commands = new();
            string line = "";
            int lineNumber = 0;
            while ((line = script.ReadLine()) != null)
            {
                lineNumber++;
                if (line.Contains('#'))
                    line = line.Substring(0, line.IndexOf("#")); //Strip comments.
                Match match = null;
                lineOption matchType = lineOption.INVALID;
                for (int i = 0; i < valid.Count(); i++)
                {
                    match = valid[i].Item2.Match(line);
                    if (match.Success)
                    {
                        matchType = valid[i].Item1;
                        break;
                    }
                }
                switch (matchType)
                {
                    case lineOption.POSITION: //Position:
                        string name = match.Groups[1].Value;
                        Vector3 pos = new Vector3(float.Parse(match.Groups[2].Value), float.Parse(match.Groups[4].Value), float.Parse(match.Groups[6].Value));
                        Vector3 rot = new Vector3(float.Parse(match.Groups[8].Value), float.Parse(match.Groups[10].Value), float.Parse(match.Groups[12].Value));
                        foreach (var loc in locs)
                        {
                            if (loc.getAlias().Equals(name))
                            {
                                UnityEngine.Debug.LogWarning("Parse warning in " + filename + $":{lineNumber}: Redefinition of position alias `" + name + "`.");
                                locs.Remove(loc);
                                break;
                            }
                        }
                        videoLocation p = new(name, pos, rot);
                        locs.Add(p);
                        break;

                    case lineOption.START: //Start:
                        string startName = "Start";
                        string startPos = match.Groups[1].Value;
                        videoLocation startPosLoc = null;
                        foreach (var loc in locs)
                        {
                            if (loc.getAlias().Equals(startPos))
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
                            startCommand start = new(startName, startPosLoc);
                            commands.Add(start);
                        }
                        break;

                    case lineOption.WAIT: //Wait
                        string waitName = "Wait";
                        float waitTime = float.Parse(match.Groups[1].Value);
                        waitCommand wait = new(waitName, waitTime);
                        commands.Add(wait);
                        break;

                    case lineOption.MOVE: //Move
                        string moveName = "Move";
                        string moveMethodAlias = match.Groups[1].Value;
                        Method method = moveCommand.ParseMethod(moveMethodAlias);
                        string destLocAlias = match.Groups[2].Value;
                        videoLocation destLoc = null;
                        foreach (var loc in locs)
                        {
                            if (loc.getAlias().Equals(destLocAlias))
                            {
                                destLoc = loc;
                                break;
                            }
                        }
                        float moveTime = float.Parse(match.Groups[3].Value);

                        bool validMove = true;
                        if (method == Method.INVALID)
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
                            moveCommand move = new(moveName, destLoc, method, moveTime);
                            commands.Add(move);
                        }
                        break;

                    case lineOption.ROTATE: //Rotate
                        string rotateName = "Rotate";
                        string rotateLocAlias = match.Groups[1].Value;
                        videoLocation rotateLoc = null;
                        foreach (var loc in locs)
                        {
                            if (loc.getAlias().Equals(rotateLocAlias))
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
                            rotateCommand rotate = new(rotateName, rotateLoc, rotateIters);
                            commands.Add(rotate);
                        }
                        break;

                    case lineOption.EMPTYLINE: //Empty line
                        // Do nothing.
                        break;
                    
                    case lineOption.SETTING:
                        var setting = videoSettings.ParseSetting(match.Groups[1].Value);
                        string settingVal = match.Groups[2].Value;
                        bool validSetting = true;
                        if (setting == videoSettings.settingOption.INVALID)
                        {
                            UnityEngine.Debug.LogError("Parse error in " + filename + $":{lineNumber}: Invalid setting name `" + match.Groups[1].Value + "`.");
                        }
                        int settingValue;
                        if (setting == videoSettings.settingOption.LOGOPOS)
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
                            settings.setSetting(setting, settingValue);
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