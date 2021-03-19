// EZmodel Sample State Generation to Spreadsheet
// copyright 2019 Serious Quality LLC

// Adjusted March 16-17, 2021 with developer thoughts:
// - decoupled model rules from ezModel state table construction
// - ezModel reads model rules through C# interface IUserRules
// - person providing model rules is responsible for implementing IUserRules methods
//
//     DeskLampAndTrafficLight is the story of an insane person who believes they
//   affect the color of a traffic light when they flip their desk lamp switch
//   from off to on, and from on to off.
//
//   Compliments of Doug Szabo, for Harry Robinson.

using System;
using System.Collections.Generic;

namespace SeriousQualityEzModel
{
    public class DesklampAndTrafficlightProgram
    {
        public static void Main()
        {
            DeskLampAndTrafficLight rules = new DeskLampAndTrafficLight();

            GeneratedGraph graph = new GeneratedGraph(rules);

            graph.DisplayStateTable(); // Display the Excel-format state table
            Console.ReadLine();
        }
    }

    public class DeskLampAndTrafficLight : IUserRules
    {
        // State Variables
        const string svDeskLamp = "DL";
        const string svTrafficLight = "TL";

        // Desk Lamp and Traffic Light
        // State values
        const string on = svDeskLamp + ".on";
        const string off = svDeskLamp + ".off";
        const string green = svTrafficLight + ".green";
        const string yellow = svTrafficLight + ".yellow";
        const string red = svTrafficLight + ".red";

        // Actions
        const string switchOn = svDeskLamp + ".switchOn";
        const string switchOff = svDeskLamp + ".switchOff";
        const string turnGreen = svTrafficLight + ".turnGreen";
        const string turnYellow = svTrafficLight + ".turnYellow";
        const string turnRed = svTrafficLight + ".turnRed";

        // Interface method
        public string GetInitialState()
        {
            // Desk Lamp and Traffic Light
            List<string> initialState = new List<string>();
            initialState.Add(off);
            initialState.Add(red);
            initialState.Sort();

            string stateString = initialState[0] + IUserRules.valueSeparator + initialState[1];

            return stateString;
        }

        // Interface method
        public List<string> GetAvailableActions(string startState)
        {
            List<string> actions = new List<string>();

            // For traffic light and desk lamp, use if-else decisions
            // since there are so few decisions.
            if (startState.Contains(on))
            {
                actions.Add(switchOff);
            }
            else if (startState.Contains(off))
            {
                actions.Add(switchOn);
            }
            else // Problem: the desk lamp state is indeterminate
            {
                Console.WriteLine("ERROR in UserRules.GetAvailableActions(): missing or unknown desklamp state value: {0}", startState);
            }

            if (startState.Contains(green))
            {
                actions.Add(turnYellow);
            }
            else if (startState.Contains(yellow))
            {
                actions.Add(turnRed);
            }
            else if (startState.Contains(red))
            {
                actions.Add(turnGreen);
            }
            else
            {
                Console.WriteLine("ERROR in UserRules.GetAvailableActions().  Missing or unknown trafficlight state value: {0}", startState);
            }

            actions.Sort();

            return actions;

        }

        // Interface method
        public string GetEndState(string startState, string action)
        {
            string endState = startState;

            if (action == switchOn)
            {
                return endState.Replace(off, on);
            }
            else if (action == switchOff)
            {
                return endState.Replace(on, off);
            }
            else if (action == turnGreen)
            {
                return endState.Replace(red, green);
            }
            else if (action == turnYellow)
            {
                return endState.Replace(green, yellow);
            }
            else if (action == turnRed)
            {
                return endState.Replace(yellow, red);
            }
            else
            {
                Console.WriteLine("ERROR: unknown action '{0}' in UserRules.GetEndState()", action);
            }

            return endState;
        }
    }
}
