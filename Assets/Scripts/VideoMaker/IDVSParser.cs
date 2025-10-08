using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;

namespace VideoMaker
{
    /// <summary>
    /// This class is used to parse an IDVS script to generate suitable commands for the video recording.
    /// </summary>
    public class IdvsParser
    {
        /// <summary>
        /// This enumeration are the potential options for a line to parse to.
        /// </summary>
        private enum LineOption
        {
            Position,
            Start,
            Wait,
            Move,
            Rotate,
            Rotate1,
            Rotate2,
            Rotate3,
            Rotate4,
            Emptyline,
            Setting,
            Invalid
        }

        // A real number, with optional negative sign and decimals.
        private static readonly string REAL = @"(-?\s*\d+(?:\.\d+)?)";
        // A positive real number, with optional decimals.
        private static readonly string positiveREAL = @"(\s*\d+(?:\.\d+)?)";
        // An alias, used for position names, movement methods, setting names, and setting values.
        private static readonly string Alias = @"(\w+)";

        /// <summary>
        /// This list contains the regular expressions that define a given line.
        /// </summary>
        private List<(LineOption, Regex)> _valid = new List<(LineOption, Regex)>
        {
            //pN is {[XN,YN,ZN],[xN,yN,zN]}                                     -   Position declaration
            (LineOption.Position,   new Regex($@"^\s*{Alias}\s+is\s+\{{\s*(?:\[|\()\s*{REAL}\s*,\s*{REAL}\s*,\s*{REAL}\s*(?:\]|\))\s*,\s*(?:\[|\()\s*{REAL}\s*,\s*{REAL}\s*,\s*{REAL}(?:\]|\))\s*\}}\s*$")),
            //Start at pX                                                       -   Initial command
            (LineOption.Start,      new Regex($@"^\s*Start\s+at\s+{Alias}\s*$")),
            //Wait N seconds                                                    -   command
            (LineOption.Wait,       new Regex($@"^\s*Wait\s+{positiveREAL}\s+seconds?\s*$")),
            //Move in METHOD to pX over N seconds                               -   command
            (LineOption.Move,       new Regex($@"^\s*Move\s+in\s+{Alias}\sto\s+{Alias}+\s+over\s+{positiveREAL}\s+seconds?\s*$")),
            //Rotate around pX N times                                          -   command
            (LineOption.Rotate,     new Regex($@"^\s*Rotate\s+around\s+{Alias}\s+{positiveREAL}\s+times?\s*$")),
            //Rotate around pX N times turn N seconds orbit N seconds           -   command
            //Turn is the duration for a turn to look at pX, orbit is the duration for a single orbit around pX
            (LineOption.Rotate1,    new Regex($@"Rotate\s+around\s+{Alias}\s+{positiveREAL}+\s+times\s+turn\s+{positiveREAL}\s+seconds?\s*$")),
            (LineOption.Rotate2,    new Regex($@"Rotate\s+around\s+{Alias}\s+{positiveREAL}+\s+times\s+orbit\s+{positiveREAL}\s+seconds?\s*$")),
            (LineOption.Rotate3,    new Regex($@"Rotate\s+around\s+{Alias}\s+{positiveREAL}+\s+times\s+turn\s+{positiveREAL}\s+seconds?\s+orbit\s+{positiveREAL}\s+seconds?\s*$")),
            (LineOption.Rotate4,    new Regex($@"Rotate\s+around\s+{Alias}\s+{positiveREAL}+\s+times\s+orbit\s+{positiveREAL}\s+seconds?\s+turn\s+{positiveREAL}\s+seconds?\s*$")),
            (LineOption.Setting,    new Regex($@"^\s*{Alias}\s*:\s*{Alias}\s*$")),
            (LineOption.Emptyline,  new Regex(@"^\s*$"))
        };

        /// <summary>
        /// This function parses a file, read in by VideoScriptReader, and returns a list of video settings, script commands, and locations.
        /// </summary>
        /// <param name="script">The stream of text to be parsed.</param>
        /// <param name="filename">The filename of the file read in, used for error reporting.</param>
        /// <returns></returns>
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
                    line = line.Substring(0, line.IndexOf("#")); //Strip line comments.
                Match match = null;
                LineOption matchType = LineOption.Invalid;

                // Iterate over potential regex matches.
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
                        string posAlias = match.Groups[1].Value;
                        Vector3 pos = new Vector3(float.Parse(match.Groups[2].Value), float.Parse(match.Groups[3].Value), float.Parse(match.Groups[4].Value));
                        Vector3 rot = new Vector3(float.Parse(match.Groups[5].Value), float.Parse(match.Groups[6].Value), float.Parse(match.Groups[7].Value));
                        foreach (var loc in locs)
                        {
                            if (loc.GetAlias().Equals(posAlias))
                            {
                                UnityEngine.Debug.LogWarning($"Parse warning in {filename}:{lineNumber}:  Redefinition of position alias `{posAlias}`.");
                                locs.Remove(loc);
                                break;
                            }
                        }
                        VideoLocation p = new(posAlias, pos, rot);
                        locs.Add(p);
                        break;

                    case LineOption.Start: //Start:
                        string startPos = match.Groups[1].Value;
                        VideoLocation startPosLoc = ParseLoc(startPos, locs);

                        bool validStart = true;
                        if (startPosLoc == null)
                        {
                            UnityEngine.Debug.LogError($"Parse error in {filename}:{lineNumber}: Invalid position alias `{startPos}`.");
                            validStart = false;
                        }

                        if (validStart)
                        {
                            StartCommand start = new(startPosLoc);
                            commands.Add(start);
                        }
                        break;

                    case LineOption.Wait: //Wait
                        float waitTime = float.Parse(match.Groups[1].Value);
                        WaitCommand wait = new(waitTime);
                        commands.Add(wait);
                        break;

                    case LineOption.Move: //Move
                        string moveMethodAlias = match.Groups[1].Value;
                        MovementMethod method = MoveCommand.ParseMethod(moveMethodAlias);
                        string destLocAlias = match.Groups[2].Value;
                        VideoLocation destLoc = ParseLoc(destLocAlias, locs);
                        float moveTime = float.Parse(match.Groups[3].Value);

                        bool validMove = true;
                        if (method == MovementMethod.Invalid)
                        {
                            UnityEngine.Debug.LogError($"Parse error in {filename}:{lineNumber}: Invalid move method `{moveMethodAlias}`.");
                            validMove = false;
                        }
                        if (destLoc == null)
                        {
                            UnityEngine.Debug.LogError($"Parse error in {filename}:{lineNumber}: Invalid position alias `{destLocAlias}`.");
                            validMove = false;
                        }

                        if (validMove)
                        {
                            MoveCommand move = new(destLoc, method, moveTime);
                            commands.Add(move);
                        }
                        break;

                    case LineOption.Rotate:  //Rotate around pX N times
                    case LineOption.Rotate1: //Rotate around pX N times turn N seconds
                    case LineOption.Rotate2: //Rotate around pX N times orbit N seconds
                    case LineOption.Rotate3: //Rotate around pX N times turn N seconds orbit N seconds
                    case LineOption.Rotate4: //Rotate around pX N times orbit N seconds turn N seconds
                        commands.Add(ParseRotate(matchType, match, locs, filename, lineNumber));
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
                            UnityEngine.Debug.LogError($"Parse error in {filename}:{lineNumber}: Invalid setting name `{match.Groups[1].Value}`.");
                        }
                        int settingValue = 0;
                        if (setting == VideoSettings.SettingOption.Logopos)
                        {
                            settingValue = (int)VideoScriptData.ParsePosition(settingVal);
                            if (settingValue == (int)VideoScriptData.LogoPosition.Invalid)
                            {
                                UnityEngine.Debug.LogError($"Parse error in {filename}:{lineNumber}: Invalid logo position value `{settingVal}`.");
                                validSetting = false;
                            }
                        }
                        else
                        {
                            try
                            {
                                settingValue = int.Parse(settingVal);
                            }
                            catch (System.FormatException)
                            {
                                Debug.LogError($"Parse error in {filename}:{lineNumber}: Format exception, expected positive real number, not `{settingVal}`.");
                                validSetting = false;
                            }
                            catch (System.OverflowException)
                            {
                                Debug.LogError($"Parse error in {filename}:{lineNumber}: Number {settingVal} is too big for integer type.");
                                validSetting = false;
                            }
                        }
                        if (validSetting)
                        {
                            settings.SetSetting(setting, settingValue);
                        }
                        break;

                    default:
                        UnityEngine.Debug.LogError($"Line {filename}:{lineNumber}: \"{line}\" does not match any acceptable pattern, ignored!");
                        break;
                }
            }
            return (settings, locs, commands);
        }

        /// <summary>
        /// This function is used to parse the different rotate command variants. See the switch-case above for details on the variants.
        /// </summary>
        /// <param name="rotVariant">The specific variant.</param>
        /// <param name="match">The regex match of the line.</param>
        /// <param name="locs">The list of locations to check aliases against.</param>
        /// <param name="filename">The filename of the file being parsed, used for error reporting.</param>
        /// <param name="lineNumber">The line number being parsed, used for error reporting.</param>
        /// <returns></returns>
        private RotateCommand ParseRotate(LineOption rotVariant, Match match, List<VideoLocation> locs, string filename, int lineNumber)
        {
            // Default values
            float rotateIters = RotateCommand.defaultIterations;
            RotationAxes rotAxis = RotateCommand.defaultRotAxis;
            float rotateTurn = RotateCommand.defaultTurnDur;
            float rotateOrbit = RotateCommand.defaultOrbitDur;

            // Parse regex match
            string rotateLocAlias = match.Groups[1].Value;
            VideoLocation rotateLoc = ParseLoc(rotateLocAlias, locs);
            rotateIters = float.Parse(match.Groups[2].Value);

            // Set turn duration if applicable
            switch (rotVariant)
            {
                case LineOption.Rotate1:
                case LineOption.Rotate3:
                    rotateTurn = float.Parse(match.Groups[3].Value);
                    break;
                case LineOption.Rotate4:
                    rotateTurn = float.Parse(match.Groups[4].Value);
                    break;
            }

            //Set orbit duration if applicable
            switch (rotVariant)
            {
                case LineOption.Rotate2:
                case LineOption.Rotate4:
                    rotateOrbit = float.Parse(match.Groups[3].Value);
                    break;
                case LineOption.Rotate3:
                    rotateOrbit = float.Parse(match.Groups[4].Value);
                    break;
            }

            bool validRotate = true;
            //Check if location is valid alias
            if (rotateLoc == null)
            {
                UnityEngine.Debug.LogError($"Parse error in {filename}:{lineNumber}:  Invalid position alias `{rotateLocAlias}`.");
                validRotate = false;
            }

            if (validRotate)
            {
                return new RotateCommand(rotateLoc, rotateIters, rotAxis, rotateTurn, rotateOrbit);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Searches through a list of locations to see if the alias is present.
        /// </summary>
        /// <param name="alias">The alias to search for.</param>
        /// <param name="locs">The list of locations to check.</param>
        /// <returns>Returns the location with the given alias if in locs, null otherwise.</returns>
        private VideoLocation ParseLoc(string alias, List<VideoLocation> locs)
        {
            foreach (var loc in locs)
            {
                if (loc.GetAlias().Equals(alias))
                {
                    return loc;
                }
            }
            return null;
        }
    }
}