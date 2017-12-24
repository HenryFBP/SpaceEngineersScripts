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

        public String _LCD_NAME =               "LCD Panel - Power Info";
        public String _LOGGING_LCD_NAME =       "LCD Panel - Logging";

        public String _REACTOR_NAME =           "Nuclear Reactor";

        public String _BATTERY_GROUP_NAME =     "battery group";
        public String _BATTERY_NAME =           "Battery";


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
            for(int i = 0; i < length; i++)
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
        public String Gauge(int length, float percent, String[] terms = null)
        {
            
            terms = terms ?? new String[] {".", "+"}; //this is because a string arr is NOT an enum, constant, or struct and those are what optional args must be.

            String ret = "";

            int step = 100 / length; //a single step. 5 lines has a step of 20, for example. {20, 40, 60, 80, 100}.

            for(int i = 0; i < length; i++)
            {
                if((step * i) > percent)
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
        /// Returns how long until one battery block is fully empty.
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public String TimeToEmpty(IMyBatteryBlock b)
        {
            String ret = "";
            String patt = @"Fully .*";

            System.Text.RegularExpressions.MatchCollection mc = System.Text.RegularExpressions.Regex.Matches(b.DetailedInfo, patt);

            foreach (System.Text.RegularExpressions.Match m in mc)
            {
                ret = m.ToString();
            }


            return ret;
        }

        public String TimeToFull(IMyBatteryBlock b)
        {
            return TimeToEmpty(b);
        }

        /// <summary>
        ///     Return a {w}-wide long string to test the display width of a screen.
        /// </summary>
        public String TestWidth(int w, char c='+')
        {
            if(w <= 0)
            {
                return "";
            }

            char[] ret = StringLine(w*2,c).ToCharArray();
            String numS = w.ToString();

            //append the char {n} times...
            for(int i = 0; i < w; i++)
            {
                ret[i] = c;
            }
            
            //overwrite the beginning with the length itself...
            for(int i = 0; i < numS.Length; i++)
            {
                ret[i] = numS[i];
            }

            String retS = new string(ret);
            retS = retS.Substring(0, w);

            return retS;
        }

        public String Log(String s, String nl="\n")
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


            IMyReactor reactor = GridTerminalSystem?.GetBlockWithName(_REACTOR_NAME) as IMyReactor;
            IMyTextPanel lcd = GridTerminalSystem?.GetBlockWithName(_LCD_NAME) as IMyTextPanel;
            IMyBatteryBlock b = GridTerminalSystem?.GetBlockWithName(_BATTERY_NAME) as IMyBatteryBlock;


            Echo("Hi! I'm main!");

            lcd?.WritePublicText("");

            Echo("Gauge for 10 wide, 56%, and ` = empty, ~ = full:");

            Echo(Gauge(10, 56, new string[] { "`","~"}));

            Echo("Testing width with '#' and 10...");
            Echo(TestWidth(10, '#'));

            Echo("LCD Panel's current text: ");
            Echo(lcd?.GetPublicText());

            Echo("Getting time to empty for a battery....");
            lcd?.WritePublicText(TimeToEmpty(b));

            Dictionary<int, int[]> fontData = new Dictionary<int, int[]>()
            {
                {0x0001, new int[] {10, 20, 30 } },
                {0x0002, new int[] {11, 21, 31 } }
            };

            foreach(KeyValuePair<int, int[]> entry in fontData)
            {
                Echo($"Key {entry.Key} = {string.Join(",",entry.Value)}");
            }

            flushLog(lcd);
        }
    }
}