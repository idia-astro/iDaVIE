using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Unity.Mathematics;
using UnityEngine;

namespace VideoMaker
{
    public class IDVSParser
    {


        public List<(string, Regex)> valid = new List<(string, Regex)>
        {
            ("Position", new Regex(@"(\w+)\s+is\s+\{\[(\d+(\.\d+)?),(\d+(\.\d+)?),(\d+(\.\d+)?)\],\[(\d+(\.\d+)?),(\d+(\.\d+)?),(\d+(\.\d+)?)\]\}")),      //pN is {[XN,YN,ZN],[xN,yN,zN]}  -   Position declaration
            ("Start",   new Regex(@"Start\s+at\s+(\w+)")),                                                      //Start at pX                       -   Initial command
            ("Wait",    new Regex(@"Wait\s+(\d+(\.\d+)?)\s+seconds?")),                                                  //Wait N seconds                    -   command
            ("Move",    new Regex(@"Move\s+in\s+(\w+)\sto\s+(\w+)+\s+over\s+(\d+(\.\d+)?)\s+seconds?")),                    //Move in METHOD to pX over N seconds         -   command
            ("Rotate",  new Regex(@"Rotate\s+around\s+(\w+)\s+(\d+(\.\d+)?)\s+times?")),                                  //Rotate around pX N times          -   command
            ("EmptyLine", new Regex(@"^\s*$"))
            // ("Rotate1",  @"Rotate\s+around\s+(\w+)\s+(\d+(\.\d+)?)+\s+times\s+turn\s+(\d+(\.\d+)?)seconds"),
            // ("Rotate2",  @"Rotate\s+around\s+(\w+)\s+(\d+(\.\d+)?)+\s+times\s+orbit\s+(\d+(\.\d+)?)seconds"),
            // ("Rotate3",  @"Rotate\s+around\s+(\w+)\s+(\d+(\.\d+)?)+\s+times\s+turn\s+(\d+(\.\d+)?)seconds\s+orbit\s+(\d+(\.\d+)?)seconds"),
            // ("Rotate4",  @"Rotate\s+around\s+(\w+)\s+(\d+(\.\d+)?)+\s+times\s+orbit\s+(\d+(\.\d+)?)seconds\s+turn\s+(\d+(\.\d+)?)seconds"),
        };

        public (List<videoLocation>, List<object>) Parse(StreamReader script, string filename)
        {
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
                string m = null;
                for (int i = 0; i < valid.Count(); i++)
                {
                    match = valid[i].Item2.Match(line);
                    if (match.Success)
                    {
                        m = valid[i].Item1;
                        break;
                    }
                }
                switch (m)
                {
                    case "Position": //Position:
                        string name = match.Groups[1].Value;
                        Vector3 pos = new Vector3(float.Parse(match.Groups[2].Value), float.Parse(match.Groups[4].Value), float.Parse(match.Groups[6].Value));
                        Vector3 rot = new Vector3(float.Parse(match.Groups[8].Value), float.Parse(match.Groups[10].Value), float.Parse(match.Groups[12].Value));
                        videoLocation p = new(name, pos, rot);
                        locs.Add(p);
                        break;

                    case "Start": //Start:
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
                        if (startPosLoc != null)
                        {
                            startCommand start = new(startName, startPosLoc);
                            commands.Add(start);
                        }
                        else
                        {
                            UnityEngine.Debug.LogError("Parse error in " + filename + $":{lineNumber}: Invalid position alias " + startPos + ".");
                        }
                        break;

                    case "Wait": //Wait
                        string waitName = "Wait";
                        float waitTime = float.Parse(match.Groups[1].Value);
                        waitCommand wait = new(waitName, waitTime);
                        commands.Add(wait);
                        break;

                    case "Move": //Move
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

                        if (method == Method.INVALID)
                        {
                            UnityEngine.Debug.LogError("Parse error in " + filename + $":{lineNumber}: Invalid move method " + moveMethodAlias + ".");
                        }
                        else if (destLoc == null)
                        {
                            UnityEngine.Debug.LogError("Parse error in " + filename + $":{lineNumber}: Invalid position alias " + destLocAlias + ".");
                        }
                        else
                        {
                            moveCommand move = new(moveName, destLoc, method, moveTime);
                            commands.Add(move);
                        }
                        break;

                    case "Rotate": //Rotate
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

                        if (rotateLoc == null)
                        {
                            UnityEngine.Debug.LogError("Parse error in " + filename + $":{lineNumber}: Invalid position alias " + rotateLocAlias + ".");
                        }
                        else
                        {
                            rotateCommand rotate = new(rotateName, rotateLoc, rotateIters);
                            commands.Add(rotate);
                        }
                        break;

                    case "EmptyLine": //Empty line
                        // Do nothing.
                        break;

                    default:
                        UnityEngine.Debug.LogError("Line " + lineNumber.ToString() + ": \"" + line + "\" does not match any acceptable pattern!");
                        break;
                }
            }
            return (locs, commands);
        }
    }
}