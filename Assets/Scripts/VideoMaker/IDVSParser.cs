using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace VideoMaker
{
    public class IDVSParser
    {


        public List<(string, Regex)> valid = new List<(string, Regex)>
        {
            ("Position", new Regex(@"(\w+)\s+is\s+\{\[(\d+(\.\d+)?),(\d+(\.\d+)?),(\d+(\.\d+)?)\],\[(\d+(\.\d+)?),(\d+(\.\d+)?),(\d+(\.\d+)?)\]\}")),      //pN is {[XN,YN,ZN],[xN,yN,zN]}  -   Position declaration
            ("Start",   new Regex(@"Start\s+at\s+(\w+)")),                                                      //Start at pX                       -   Initial command
            ("Wait",    new Regex(@"Wait\s+(\d+(\.\d+)?)\s+seconds")),                                                  //Wait N seconds                    -   command
            ("Move",    new Regex(@"Move\s+in+(\w+)\sto\s+(\w+)+\s+over\s+(\d+(\.\d+)?)\s+seconds")),                    //Move in METHOD to pX over N seconds         -   command
            ("Rotate",  new Regex(@"Rotate\s+around\s+(\w+)\s+(\d+(\.\d+)?)\s+times")),                                  //Rotate around pX N times          -   command
            ("EmptyLine", new Regex(@"^\s*$"))
            // ("Rotate1",  @"Rotate\s+around\s+(\w+)\s+(\d+(\.\d+)?)+\s+times\s+turn\s+(\d+(\.\d+)?)seconds"),
            // ("Rotate2",  @"Rotate\s+around\s+(\w+)\s+(\d+(\.\d+)?)+\s+times\s+orbit\s+(\d+(\.\d+)?)seconds"),
            // ("Rotate3",  @"Rotate\s+around\s+(\w+)\s+(\d+(\.\d+)?)+\s+times\s+turn\s+(\d+(\.\d+)?)seconds\s+orbit\s+(\d+(\.\d+)?)seconds"),
            // ("Rotate4",  @"Rotate\s+around\s+(\w+)\s+(\d+(\.\d+)?)+\s+times\s+orbit\s+(\d+(\.\d+)?)seconds\s+turn\s+(\d+(\.\d+)?)seconds"),
        };

        public (List<videoLocation>, List<object>) Parse(StreamReader script)
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
                int m = -1;
                for (int i = 0; i < valid.Count(); i++)
                {
                    match = valid[i].Item2.Match(line);
                    if (match.Success)
                    {
                        m = i;
                        break;
                    }
                }
                switch (m)
                {
                    case 0: //Position:
                        string name = match.Groups[0].Value;
                        Vector3 pos = new Vector3(float.Parse(match.Groups[1].Value), float.Parse(match.Groups[2].Value), float.Parse(match.Groups[3].Value));
                        Vector3 rot = new Vector3(float.Parse(match.Groups[4].Value), float.Parse(match.Groups[5].Value), float.Parse(match.Groups[6].Value));
                        videoLocation p = new(name, pos, rot);
                        locs.Add(p);
                        break;
                    case 1: //Start:
                        string n = "Start";
                        break;
                    case 5: //Empty line
                        // Do nothing.
                        break;
                    case 6: //Line of dashes only
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