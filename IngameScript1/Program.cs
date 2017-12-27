using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        // This file contains your actual script.
        //
        // You can either keep all your code here, or you can create separate
        // code files to make your program easier to navigate while coding.
        //
        // In order to add a new utility class, right-click on your project, 
        // select 'New' then 'Add Item...'. Now find the 'Space Engineers'
        // category under 'Visual C# Items' on the left hand side, and select
        // 'Utility Class' in the main area. Name it in the box below, and
        // press OK. This utility class will be merged in with your code when
        // deploying your final script.
        //
        // You can also simply create a new utility class manually, you don't
        // have to use the template if you don't want to. Just do so the first
        // time to see what a utility class looks like.



        public String _LOG_STRING = "";

        //CHANGE ME IF YOU LIKE. By default, with the font I use, this is {26}.
        public const int            U_LCD_WIDTH =           LCD_E.ONE_LCD_PANE;
        
        public const String         U_LCD_NAME =            "LCD Panel - Power Info";
        public const String         U_LOGGING_LCD_NAME =    "LCD Panel - Debug";

        public const String         U_REACTOR_NAME =        "Nuclear Reactor";

        public const String         U_BATTERY_GROUP_NAME =  "Batteries";
        public const String         U_BATTERY_NAME =        "Battery";


        public void EchoI(String s, bool b)
        {
            if(b)
            {
                Echo(s);
            }
        }

        /// <summary>
        /// Enum for LCD width. Assuming using monospace font.
        /// </summary>
        public class LCD_E
        {
            public const int ONE_LCD_PANE = 26; //assuming monospaced font
            public const int TWO_LCD_PANE = ONE_LCD_PANE * 2;
        }


        public const String _GAUGE_DEF = ".+";

        public const String _CAPS = "[]";


        /// <summary>
        ///     Generate ASCII charge bar.
        /// </summary>
        /// <remarks>
        ///     This function takes a length, a percent out of 100, and two start-end characters.
        ///     It represents a filled ASCII bar of that length that represents the percent.
        /// </remarks>
        /// <example>
        ///     Gauge(10, 67.3, {"-", "%"} returns "%%%%%%%---".
        ///     Ten lines, seven filled for 67.3 percent. (It rounds half-round-down.)
        /// </example>
        public String Gauge(int length = U_LCD_WIDTH, double percent = 50.0, String terms = _GAUGE_DEF, bool v = false)
        {
            EchoI($"Gauge function passed: length='{length}', percent='{percent}', terms='{terms}', v='{v}'.", v);

            String ret = "";

            double step = (100.0 / ((double)length)); //a single step. 5 lines has a step of 20, for example. {20, 40, 60, 80, 100}.

            EchoI($"One step is '{step}' long.", v);

            for (double i = 0; i < length; i++)
            {
                EchoI($"At the {i}th bar pip.", v);
                EchoI("Checking if (step * i) > percent, aka", v);
                EchoI($"         if {step * i} > {percent}.", v);

                if ((step * i) > percent) //if we are in the empty region
                {
                    ret += terms[0];
                }
                else
                {
                    ret += terms[1];
                }
            }

            EchoI($"Returning this string:\n'{ret}'\n", v);

            return ret;
        }



        /// <summary>
        /// Regex/stuff that is used to get info about battery blocks.
        /// To be used on "DetailedInfo" field.
        /// </summary>
        public class BATT_E
        {
            public Program p = null;

            public void Echo(String s) => p.Echo(s);
            public void EchoI(String s, bool b) => p.EchoI(s,b);

            public BATT_E(Program p)
            {
                this.p = p;
            }

            public const String UNIT_SUFFIX =           @"(.)W(h|)";
            public const String NUMBER_VALUE =          @"((\d+)(\.|)(\d+|))";

            public const String TYPE =                  @"Type: .*[\r\n]+";
            public const String MAX_OUTPUT =            @"Max Output: .*[\r\n]+";
            public const String MAX_REQUIRED_INPUT =    @"Max Required Input: (.*)[\r\n]+";
            public const String MAX_STORED_POWER =      @"Max Stored Power: (.*)[\r\n]+";
            public const String CURRENT_INPUT =         @"Current Input: (.*)[\r\n]+";
            public const String CURRENT_OUTPUT =        @"Current Output: (.*)[\r\n]+";
            public const String STORED_POWER =          @"Stored power: (.+?) (.)Wh[\r\n]+";
            public const String FULLY_RECHARGED_IN =    @"Fully (.+?) in: (.*)";

            public const String TYPE_S =                @"Type: {0}";
            public const String MAX_OUTPUT_S =          @"Max Output: {0}";
            public const String MAX_REQUIRED_INPUT_S =  @"Max Required Input: {0}";
            public const String MAX_STORED_POWER_S =    @"Max Stored Power: {0}";
            public const String CURRENT_INPUT_S =       @"Current Input: {0}";
            public const String CURRENT_OUTPUT_S =      @"Current Output: {0}";
            public const String STORED_POWER_S =        @"Stored power: {0}";
            public const String FULLY_RECHARGED_IN_S =  @"Fully {0} in: {1}";

            public const int TYPE_LOC =                 0;
            public const int MAX_OUTPUT_LOC =           1;
            public const int MAX_REQUIRED_INPUT_LOC =   2;
            public const int MAX_STORED_POWER_LOC =     3;
            public const int CURRENT_INPUT_LOC =        4;
            public const int CURRENT_OUTPUT_LOC =       5;
            public const int STORED_POWER_LOC =         6;
            public const int FULLY_RECHARGED_IN_LOC =   7;



            /// <summary>
            /// Gets units of electricity from a batt info string.
            /// </summary>
            /// <example>
            /// "Stored Power: 2.64 MWh" -> {2,640,000}
            /// </example>
            /// <param name="electrStr"></param>
            /// <returns></returns>
            public double ExtractUnits(String electrStr, bool v = false)
            {
                double ret = -1;

                //this.EchoI($"Extracting numerical units from this batteryString: '{electrStr}'", v);

                System.Text.RegularExpressions.MatchCollection mc = System.Text.RegularExpressions.Regex.Matches(electrStr, BATT_E.NUMBER_VALUE);

                //this.EchoI(MatchesToString(mc),v);

                ret = double.Parse(mc[0].Groups[0].Value);

                return ret;
            }

            /// <summary>
            /// Get unit denominator from a batt info string.
            /// </summary>
            /// <example>
            /// "Stored Power: 2.64 MWh" -> {'M'}
            /// </example>
            /// <param name="electrStr"></param>
            /// <returns></returns>
            public char ExtractSuffix(String electrStr, bool v = false)
            {
                char ret = '\0';

                //this.EchoI($"Extracting unit denominator from this batteryString: '{electrStr}'", v);

                System.Text.RegularExpressions.MatchCollection mc = System.Text.RegularExpressions.Regex.Matches(electrStr, BATT_E.UNIT_SUFFIX);

                //this.EchoI(MatchesToString(mc),v);

                ret = (mc[0].Groups[1].Value.ToCharArray()[0]); //0th char of 1st matching group, the '.' before 'W(h|)'.

                return ret;
            }


            /// <summary>
            /// Return the max output for a battery.
            /// </summary>
            /// <param name="b">A battery block.</param>
            /// <returns></returns>
            public double BatteryMaxOutput(IMyBatteryBlock b, bool v = false)
            {
                double ret = -1.0;

                double num = ExtractUnits(b?.DetailedInfo.Split('\n')[BATT_E.MAX_OUTPUT_LOC], v);
                char denom = ExtractSuffix(b?.DetailedInfo.Split('\n')[BATT_E.MAX_OUTPUT_LOC], v);

                ret = UNIT_E.PREFIX(num, denom);

                return ret;
            }

            /// <summary>
            /// Return the max output for a block group's batteries.
            /// </summary>
            /// <param name="bb">The List of batteries.</param>
            /// <returns></returns>
            public double BatteriesMaxOutput(List<IMyBatteryBlock> bb, bool v = false)
            {
                double ret = -1.0;
                
                foreach (IMyBatteryBlock b in bb)
                {
                    ret += this.BatteryMaxOutput(b, v);
                }

                return ret;
            }


            /// <summary>
            /// Return the max required input for a battery.
            /// </summary>
            /// <param name="b">A battery block.</param>
            /// <returns></returns>
            public double BatteryMaxRequiredInput(IMyBatteryBlock b, bool v = false)
            {
                double ret = -1.0;

                double num = ExtractUnits(b?.DetailedInfo.Split('\n')[BATT_E.MAX_REQUIRED_INPUT_LOC], v);
                char denom = ExtractSuffix(b?.DetailedInfo.Split('\n')[BATT_E.MAX_REQUIRED_INPUT_LOC], v);

                ret = UNIT_E.PREFIX(num, denom);

                return ret;
            }

            /// <summary>
            /// Return the max required input for a block group's batteries.
            /// </summary>
            /// <param name="bb">The List of batteries.</param>
            /// <returns></returns>
            public double BatteriesMaxRequiredInput(List<IMyBatteryBlock> bb, bool v = false)
            {
                double ret = -1.0;

                foreach (IMyBatteryBlock b in bb)
                {
                    ret += this.BatteryMaxRequiredInput(b, v);
                }

                return ret;
            }


            /// <summary>
            /// Return the max stored power for a battery.
            /// </summary>
            /// <param name="b">A battery block.</param>
            /// <returns></returns>
            public double BatteryMaxStoredPower(IMyBatteryBlock b, bool v = false)
            {
                double ret = -1.0;

                double num = ExtractUnits(b?.DetailedInfo.Split('\n')[BATT_E.MAX_STORED_POWER_LOC], v);
                char denom = ExtractSuffix(b?.DetailedInfo.Split('\n')[BATT_E.MAX_STORED_POWER_LOC], v);

                ret = UNIT_E.PREFIX(num, denom);

                return ret;
            }

            /// <summary>
            /// Return the max stored power for a block group's batteries.
            /// </summary>
            /// <param name="bb">The List of batteries.</param>
            /// <returns></returns>
            public double BatteriesMaxStoredPower(List<IMyBatteryBlock> bb, bool v = false)
            {
                double ret = -1.0;

                foreach (IMyBatteryBlock b in bb)
                {
                    ret += this.BatteryMaxStoredPower(b, v);
                }

                return ret;
            }


            /// <summary>
            /// Return how much stored power a battery has.
            /// </summary>
            /// <param name="b">A battery block.</param>
            /// <returns></returns>
            public double BatteryStoredPower(IMyBatteryBlock b, bool v = false)
            {
                double ret = -1.0;

                double num = ExtractUnits(b?.DetailedInfo.Split('\n')[BATT_E.STORED_POWER_LOC], v);
                char denom = ExtractSuffix(b?.DetailedInfo.Split('\n')[BATT_E.STORED_POWER_LOC], v);

                ret = UNIT_E.PREFIX(num, denom);

                return ret;
            }

            /// <summary>
            /// Return how much stored power a block group's batteries has.
            /// </summary>
            /// <param name="bb">The List of batteries.</param>
            /// <returns></returns>
            public double BatteriesStoredPower(List<IMyBatteryBlock> bb, bool v = false)
            {
                double ret = -1.0;

                foreach (IMyBatteryBlock b in bb)
                {
                    ret += this.BatteryStoredPower(b, v);
                }

                return ret;
            }


            /// <summary>
            /// Return the current input for a battery.
            /// </summary>
            /// <param name="b">A battery block.</param>
            /// <returns></returns>
            public double BatteryCurrentInput(IMyBatteryBlock b, bool v = false)
            {
                double ret = -1.0;

                //EchoI($"Getting current input as a number for battery named '{b.Name}'...", v);

                double num = ExtractUnits(b?.DetailedInfo.Split('\n')[BATT_E.CURRENT_INPUT_LOC], v);
                char denom = ExtractSuffix(b?.DetailedInfo.Split('\n')[BATT_E.CURRENT_INPUT_LOC], v);

                ret = UNIT_E.PREFIX(num, denom, v);

                return ret;
            }

            /// <summary>
            /// Return the current input for a block group's batteries.
            /// </summary>
            /// <param name="bb">The List of batteries.</param>
            /// <returns></returns>
            public double BatteriesCurrentInput(List<IMyBatteryBlock> bb, bool v = false)
            {
                double ret = -1.0;

                foreach (IMyBatteryBlock b in bb)
                {
                    ret += this.BatteryCurrentInput(b, v);
                }

                return ret;
            }


            /// <summary>
            /// Return the current output for a battery.
            /// </summary>
            /// <param name="b">A battery block.</param>
            /// <returns></returns>
            public double BatteryCurrentOutput(IMyBatteryBlock b, bool v = false)
            {
                double ret = -1.0;

                double num = ExtractUnits(b?.DetailedInfo.Split('\n')[BATT_E.CURRENT_OUTPUT_LOC], v);
                char denom = ExtractSuffix(b?.DetailedInfo.Split('\n')[BATT_E.CURRENT_OUTPUT_LOC], v);

                ret = UNIT_E.PREFIX(num, denom);

                return ret;
            }

            /// <summary>
            /// Return the current output for a block group's batteries.
            /// </summary>
            /// <param name="bb">The List of batteries.</param>
            /// <returns></returns>
            public double BatteriesCurrentOutput(List<IMyBatteryBlock> bb, bool v = false)
            {
                double ret = -1.0;

                foreach (IMyBatteryBlock b in bb)
                {
                    ret += this.BatteryCurrentOutput(b, v);
                }

                return ret;
            }


            /// <summary>
            /// Returns how long until one battery block is fully empty or full.
            /// </summary>
            /// <param name="b">The battery.</param>
            /// <returns></returns>
            public String BatteryTimeToEmpty(IMyBatteryBlock b, bool v = false)
            {
                String ret = "";

                System.Text.RegularExpressions.MatchCollection mc = System.Text.RegularExpressions.Regex.Matches(b.DetailedInfo, BATT_E.FULLY_RECHARGED_IN);

                foreach (System.Text.RegularExpressions.Match m in mc)
                {
                    ret = m.ToString();
                }
                
                return ret;
            }
            public String BatteryTimeToFull(IMyBatteryBlock b, bool v = false) => BatteryTimeToEmpty(b, v);

            /// <summary>
            /// Returns how long until a list of battery blocks is fully empty or full.
            /// </summary>
            /// <param name="bb"></param>
            /// <param name="v"></param>
            /// <returns></returns>
            public String BatteriesTimeToEmpty(List<IMyBatteryBlock> bb, bool v = false)
            {
                return @"UNIMP, soz :'( Maybe once I parse dates it'll be easier?";
            }
            /// <summary>
            /// See <see cref="BatteriesTimeToEmpty(List{IMyBatteryBlock}, bool)"/>.
            /// </summary>
            public String BatteriesTimeToFull(List<IMyBatteryBlock> bb, bool v = false) => BatteriesTimeToEmpty(bb, v);


            /// <summary>
            /// Return an ASCII bar representing how full a battery is.
            /// </summary>
            /// <param name="b">The battery.</param>
            /// <param name="width">How wide the bar will be.</param>
            /// <returns>An ASCII-art bar.</returns>
            public String BatteryChargeBar(IMyBatteryBlock b, int width = U_LCD_WIDTH, String terms = _GAUGE_DEF, bool v = false)
            {
                return p.Gauge(width, (100 * (this.BatteryStoredPower(b) / this.BatteryMaxStoredPower(b))), terms, v);
            }


            /// <summary>
            /// Return an ASCII bar representing how full a battery group is.
            /// </summary>
            /// <param name="bg">The battery group.</param>
            /// <param name="width">How wide the bar will be.</param>
            /// <returns>An ASCII-art bar.</returns>
            public String BatteriesChargeBar(List<IMyBatteryBlock> bb, int width = U_LCD_WIDTH, String terms = _GAUGE_DEF, bool v = false)
            {
                return p.Gauge(width, (100 * (BatteriesStoredPower(bb) / BatteriesMaxStoredPower(bb))), terms, v);
            }

            /// <summary>
            /// Return a detailedInfo value for ALL batteries in a block group.
            /// </summary>
            /// <param name="bb">The blockGroup.</param>
            /// <param name="v">Verbose?</param>
            /// <returns>A detailedInfo String.</returns>
            public String DetailedInfos(List<IMyBatteryBlock> bb, bool v = false)
            {
                EchoI($"Getting detailedInfo string for ALL '{bb.Count}' batteries...whew...",v);
                String ret = "";

                ret += "Type: Battery\n";
                ret += String.Format(BATT_E.MAX_OUTPUT_S,               UNIT_E.UNPREFIX(this.BatteriesMaxOutput(bb,v))) + "\n";
                ret += String.Format(BATT_E.MAX_REQUIRED_INPUT_S,       UNIT_E.UNPREFIX(this.BatteriesMaxRequiredInput(bb,v))) + "\n";
                ret += String.Format(BATT_E.MAX_STORED_POWER_S,         UNIT_E.UNPREFIX(this.BatteriesMaxStoredPower(bb,v))) + "\n";
                ret += String.Format(BATT_E.CURRENT_INPUT_S,            UNIT_E.UNPREFIX(this.BatteriesCurrentInput(bb,v))) + "\n";
                ret += String.Format(BATT_E.CURRENT_OUTPUT_S,           UNIT_E.UNPREFIX(this.BatteriesCurrentOutput(bb,v))) + "\n";
                ret += String.Format(BATT_E.STORED_POWER_S,             UNIT_E.UNPREFIX(this.BatteriesStoredPower(bb,v))) + "\n";
                ret += String.Format(BATT_E.FULLY_RECHARGED_IN_S,@"aww",@"shucks") +     @"UNIMPL, SOZ :("; 
                
                return ret;
            }

        }

        /// <summary>
        /// I hate constants so much that I made this class in case of...uh...idk, the Kilo unit suffix changes? lolololol.
        /// </summary>
        public static class UNIT_E
        {
            public const int MILL =         1000000;
            public const int KILO =         1000;
            public const int UNCHANGED =    1;

            public const char MILL_C = 'M';
            public const char KILO_C = 'K';

            /// <summary>
            /// To convert those pesky {'m'} -> 1,000,000
            /// </summary>
            /// <param name="c">The unit prefix that represents the factor that it multiplies by.</param>
            /// <returns>The factor.</returns>
            public static double PREFIX(char c, bool v = false)
            {
                c = Char.ToUpper(c);

                switch (c)
                {
                    case UNIT_E.MILL_C:
                        return UNIT_E.MILL;

                    case UNIT_E.KILO_C:
                        return UNIT_E.KILO;
                }
                return UNIT_E.UNCHANGED;
            }

            /// <summary>
            /// To convert those pesky {1.79, 'm'} -> 1,790,000
            /// </summary>
            public static double PREFIX(double i, char c, bool v = false) => (i * UNIT_E.PREFIX(c,v));

            /// <summary>
            /// To convert a number back into its unit-suffixed form.
            /// </summary>
            /// <example>
            /// 2 340 000 -> "2.34 M"
            /// </example>
            public static String UNPREFIX(double d)
            {
                String ret = "" + d;
                if (d > UNIT_E.MILL)
                {
                    ret += UNIT_E.MILL_C;
                }
                else if(d > UNIT_E.KILO)
                {
                    ret += UNIT_E.KILO_C; 
                }
                
                return ret;
            }

        }

        /// <summary>   
        /// Return a human-readable string of all the regex matches in a MatchCollection.
        /// </summary>
        /// <param name="mc">A MatchCollection.</param>
        /// <returns>The human-readable string.</returns>
        public static String MatchesToString(System.Text.RegularExpressions.MatchCollection mc)
        {
            String ret = "";

            for(int i = 0; i < mc.Count; i++)//go thru matches
            {
                ret += $"     {i}th match: \n";
                for (int j = 0; j < mc[i].Groups.Count; j++) //go thru capturing groups
                {
                    ret += ($"{j}th captured group: '{mc[i].Groups[j].ToString()}'\n");
                }
            }

            return ret;
            
        }

        /// <summary>
        ///     Extend a char {c} out by {length} places.
        /// </summary>
        /// <example>
        ///     StringLine(5, '~') -> "~~~~~".
        /// </example>
        /// <param name="length"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        public String StringLine(int length, char c = ' ')
        {
            String ret = "";
            for (int i = 0; i < length; i++)
            {
                ret += c;
            }
            return ret;
        }




        /// <summary>
        ///     Return a {w}-wide long string to test the display width of a screen.
        /// </summary>
        public String TestWidth(int w, char c = '+')
        {
            if (w <= 0)
            {
                return "";
            }

            char[] ret = StringLine(w * 2, c).ToCharArray();
            String numS = w.ToString();

            //append the char {n} times...
            for (int i = 0; i < w; i++)
            {
                ret[i] = c;
            }

            //overwrite the beginning with the length itself...
            for (int i = 0; i < numS.Length; i++)
            {
                ret[i] = numS[i];
            }

            String retS = new string(ret);
            retS = retS.Substring(0, w);

            return retS;
        }

        /// <summary>
        /// Logs a string to an LCD.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="nl"></param>
        /// <returns></returns>
        public String Log(String s, IMyTextPanel l, String nl = "\n", bool append = true)
        {
            s = $"[{GetTime()}]: {s}";
            l?.WritePublicText(s+nl, append);
            return s+nl;
        }

        public void flushLog(IMyTextPanel t, bool append = true)
        {
            t?.WritePublicText(_LOG_STRING, append);
        }

        /// <summary>
        /// Return the current HH:MM:SS tt.
        /// </summary>
        public String GetTime()
        {
            var n = DateTime.Now;
            return $"{n.Hour.ToString("D2")}:{n.Minute.ToString("D2")}:{n.Second.ToString("D2")} {n.ToString("tt",System.Globalization.CultureInfo.InvariantCulture)}";
        }



        public Program()
        {
            // The constructor, called only once every session and
            // always before any other method is called. Use it to
            // initialize your script. 
            //     
            // The constructor is optional and can be removed if not
            // needed.


            Echo("Hi! I'm the constructor!");
            
        }

        public void Save()
        {
            // Called when the program needs to save its state. Use
            // this method to save your state to the Storage field
            // or some other means. 
            // 
            // This method is optional and can be removed if not
            // needed.
        }

        public void Main(string argument)
        {

            BATT_E bATT_E = new BATT_E(this);


            // The main entry point of the script, invoked every time
            // one of the programmable block's Run actions are invoked.
            // 
            // The method itself is required, but the argument above
            // can be removed if not needed.
            
            Echo("Hi! I'm main!");


            IMyReactor      reactor =       GridTerminalSystem?.GetBlockWithName(U_REACTOR_NAME) as IMyReactor;
            IMyTextPanel    lcd =           GridTerminalSystem?.GetBlockWithName(U_LCD_NAME) as IMyTextPanel;
            IMyTextPanel    lcdDebug =      GridTerminalSystem?.GetBlockWithName(U_LOGGING_LCD_NAME) as IMyTextPanel;
            IMyBatteryBlock b =             GridTerminalSystem?.GetBlockWithName(U_BATTERY_NAME) as IMyBatteryBlock;
            IMyBlockGroup   bg =            GridTerminalSystem?.GetBlockGroupWithName(U_BATTERY_GROUP_NAME);

            Log("Testing the logging function...", lcdDebug);


            List<IMyBatteryBlock> batteryList = new List<IMyBatteryBlock>();
            bg.GetBlocksOfType<IMyBatteryBlock>(batteryList); //add all found battery blocks to this list
            Echo($"Made list of batteries. it is '{batteryList.Count}' big.");


            String[] battStr = bATT_E.DetailedInfos(batteryList, true).Split('\n');

            lcd?.WritePublicText("");

            Echo($"Gauge for 10 wide, 56%, and {_GAUGE_DEF[0]} = empty, {_GAUGE_DEF[1]} = full:");


            Echo(Gauge(10, 56, _GAUGE_DEF));

            Echo("Testing width with '#' and 10...");
            Echo(TestWidth(10, '#'));

            Echo("LCD Panel's current text: ");
            Echo(lcd?.GetPublicText());

            Echo("Getting time to empty for a battery....");
            lcd?.WritePublicText(bATT_E.BatteryTimeToEmpty(b),true);

            lcd?.WritePublicText("\n" + battStr[BATT_E.STORED_POWER_LOC], true);

            if(U_LCD_WIDTH < LCD_E.TWO_LCD_PANE) //if it'll be too short...
            {
                lcd?.WritePublicText("\n",true);
            }

            lcd?.WritePublicText(" / ", true);

            if (U_LCD_WIDTH < LCD_E.TWO_LCD_PANE) //if it'll be too short...
            {
                lcd?.WritePublicText("\n", true);
            }

            lcd?.WritePublicText(battStr[BATT_E.MAX_STORED_POWER_LOC], true);

            lcd?.WritePublicText("\n"+
                _CAPS[0] + 
                bATT_E.BatteriesChargeBar(batteryList, (U_LCD_WIDTH - _CAPS.Length), v: true) +
                _CAPS[1], true);

        }
    }
}


