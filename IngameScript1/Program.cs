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

        public const int _LCD_WIDTH = 53; //assuming you're using monospaced font

        public const String _GAUGE_DEF = ".+";

        public String _LCD_NAME =               "LCD Panel - Power Info";
        public String _LOGGING_LCD_NAME =       "LCD Panel - Debug";

        public String _REACTOR_NAME =           "Nuclear Reactor";

        public String _BATTERY_GROUP_NAME =     "battery group";
        public String _BATTERY_NAME =           "Battery";

        /// <summary>
        /// Regex/stuff that is used to get info about battery blocks.
        /// To be used on "DetailedInfo" field.
        /// </summary>
        public static class BAT_E
        {
            public const String UNIT_SUFFIX =          @"(.)W(h|)";
            public const String ELECTR =               @"((\d+)(\.|)(\d+|))";

            public const String TYPE =                 @"Type: .*[\r\n]+";
            public const String MAX_OUTPUT =           @"Max Output: .*[\r\n]+";
            public const String MAX_REQUIRED_INPUT =   @"Max Required Input: (.*)[\r\n]+";
            public const String MAX_STORED_POWER =     @"Max Stored Power: (.*)[\r\n]+";
            public const String CURRENT_INPUT =        @"Current Input: (.*)[\r\n]+";
            public const String CURRENT_OUTPUT =       @"Current Output: (.*)[\r\n]+";
            public const String STORED_POWER =         @"Stored power: (.+?) (.)Wh[\r\n]+";
            public const String FULLY_RECHARGED_IN =   @"Fully (.+?) in: (.*)";

            public const int TYPE_LOC =                0;
            public const int MAX_OUTPUT_LOC =          1;
            public const int MAX_REQUIRED_INPUT_LOC =  2;
            public const int MAX_STORED_POWER_LOC =    3;
            public const int CURRENT_INPUT_LOC =       4;
            public const int CURRENT_OUTPUT_LOC =      5;
            public const int STORED_POWER_LOC =        6;
            public const int FULLY_RECHARGED_IN_LOC =  7;
        }

        /// <summary>
        /// I hate constants so much that I made this class in case of...uh...idk, the Kilo unit suffix changes? lolololol.
        /// </summary>
        public static class UNIT_E
        {
            public const int MILL = 1000000;
            public const int KILO = 1000;

            public const char MILL_C = 'M';
            public const char KILO_C = 'K';
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
        /// To convert a number back into its unit-suffixed form.
        /// </summary>
        /// <example>
        /// 2 340 000 -> "2.34 M"
        /// </example>
        public static String UNIT_UNPREFIX(double d)
        {
            String ret = ""+d;
            if(d > UNIT_E.MILL)
            {
                ret = ret + UNIT_E.MILL_C;
            }
            return ret;
        }

        /// <summary>
        /// To convert those pesky {'m'} -> 1,000,000
        /// </summary>
        /// <param name="c">The unit prefix that represents the factor that it multiplies by.</param>
        /// <returns>The factor.</returns>
        public static double UNIT_PREFIX(char c)
        {
            c = Char.ToUpper(c);

            switch(c)
            {
                case UNIT_E.MILL_C:
                    return UNIT_E.MILL;

                case UNIT_E.KILO_C:
                    return UNIT_E.KILO;
            }
            return 1.0;
        }

        /// <summary>
        /// To convert those pesky {1.79, 'm'} -> 1,790,000
        /// </summary>
        public static double UNIT_PREFIX(double i, char c) => (i * UNIT_PREFIX(c));
        

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
        public String Gauge(int length, double percent, String terms = _GAUGE_DEF)
        {

            String ret = "";

            int step = 100 / length; //a single step. 5 lines has a step of 20, for example. {20, 40, 60, 80, 100}.

            for (int i = 0; i < length; i++)
            {
                if ((step * i) > percent)
                {
                    ret += terms[0];
                }
                else
                {
                    ret += terms[1];
                }
            }

            return ret;
        }

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

            System.Text.RegularExpressions.MatchCollection mc = System.Text.RegularExpressions.Regex.Matches(electrStr, BAT_E.ELECTR);

            if(v) //if debug mode
            {
                Echo(MatchesToString(mc));
            }

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

            System.Text.RegularExpressions.MatchCollection mc = System.Text.RegularExpressions.Regex.Matches(electrStr, BAT_E.UNIT_SUFFIX);

            if (v) //if debug mode
            {
                Echo(MatchesToString(mc));
            }

            ret = (mc[0].Groups[1].Value.ToCharArray()[0]); //0th char of 1st matching group, the '.' before 'W(h|)'.

            return ret;
        }

        /// <summary>
        /// Return how much charge a battery has.
        /// </summary>
        /// <param name="b">A battery block.</param>
        /// <returns></returns>
        public double BatteryCharge(IMyBatteryBlock b, bool v = false)
        {
            double ret = -1.0;

            double num = ExtractUnits(b?.DetailedInfo.Split('\n')[BAT_E.STORED_POWER_LOC], v);
            char denom = ExtractSuffix(b?.DetailedInfo.Split('\n')[BAT_E.STORED_POWER_LOC], v);

            ret = UNIT_PREFIX(num, denom);

            return ret;
        }

        /// <summary>
        /// Return how much max charge a battery can have.
        /// </summary>
        /// <param name="b">A battery block.</param>
        /// <returns></returns>
        public double BatteryMaxCharge(IMyBatteryBlock b, bool v = false)
        {
            double ret = -1.0;

            double num = ExtractUnits(b?.DetailedInfo.Split('\n')[BAT_E.MAX_STORED_POWER_LOC], v);
            char denom = ExtractSuffix(b?.DetailedInfo.Split('\n')[BAT_E.MAX_STORED_POWER_LOC], v);

            ret = UNIT_PREFIX(num, denom);

            return ret;
        }

        /// <summary>
        /// Returns how long until one battery block is fully empty.
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public String BatteryTimeToEmpty(IMyBatteryBlock b)
        {
            String ret = "";

            System.Text.RegularExpressions.MatchCollection mc = System.Text.RegularExpressions.Regex.Matches(b.DetailedInfo, BAT_E.FULLY_RECHARGED_IN);

            foreach (System.Text.RegularExpressions.Match m in mc)
            {
                ret = m.ToString();
            }


            return ret;
        }
        public String BatteryTimeToFull(IMyBatteryBlock b) => BatteryTimeToEmpty(b);


        /// <summary>
        /// Return an ASCII bar representing 
        /// </summary>
        /// <param name="b"></param>
        /// <param name="width"></param>
        /// <returns></returns>
        public String BatteryChargeBar(IMyBatteryBlock b, int width = _LCD_WIDTH, String gaugeDef = _GAUGE_DEF)
        {
            String ret = "";

            double currPow = BatteryCharge(b);
            double maxPow = BatteryMaxCharge(b);

            ret = Gauge(width, (currPow/maxPow), gaugeDef);

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

        public String Log(String s, String nl = "\n")
        {
            _LOG_STRING += (s + nl);
            return (s + nl);
        }

        public void flushLog(IMyTextPanel t, bool append = true)
        {
            t?.WritePublicText(_LOG_STRING, append);
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

            // The main entry point of the script, invoked every time
            // one of the programmable block's Run actions are invoked.
            // 
            // The method itself is required, but the argument above
            // can be removed if not needed.
            
            Echo("Hi! I'm main!");

            IMyReactor      reactor =   GridTerminalSystem?.GetBlockWithName(_REACTOR_NAME) as IMyReactor;
            IMyTextPanel    lcd =       GridTerminalSystem?.GetBlockWithName(_LCD_NAME) as IMyTextPanel;
            IMyTextPanel    lcdDebug =  GridTerminalSystem?.GetBlockWithName(_LOGGING_LCD_NAME) as IMyTextPanel;
            IMyBatteryBlock b =         GridTerminalSystem?.GetBlockWithName(_BATTERY_NAME) as IMyBatteryBlock;
            String[] bS = b.DetailedInfo.Split('\n');

            lcd?.WritePublicText("");

            Echo($"Gauge for 10 wide, 56%, and {_GAUGE_DEF[0]} = empty, {_GAUGE_DEF[1]} = full:");

            Echo(Gauge(10, 56, _GAUGE_DEF));

            Echo("Testing width with '#' and 10...");
            Echo(TestWidth(10, '#'));

            Echo("LCD Panel's current text: ");
            Echo(lcd?.GetPublicText());

            Echo("Getting time to empty for a battery....");
            lcd?.WritePublicText(BatteryTimeToEmpty(b),true);

            lcd?.WritePublicText(batter + " / " + bS[BAT_E.MAX_STORED_POWER_LOC], true);

            flushLog(lcd);

        }
    }
}


