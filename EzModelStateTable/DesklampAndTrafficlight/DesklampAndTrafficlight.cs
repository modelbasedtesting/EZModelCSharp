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
using SeriousQualityEzModel;

namespace DesklampAndTrafficlightExample
{
    class DesklampAndTrafficlightProgram
    {
        public static void Main()
        {
            DeskLampAndTrafficLight client = new DeskLampAndTrafficLight();
            client.SkipSelfLinks = false;

            GeneratedGraph graph = new GeneratedGraph(client, 200, 20, 10);

            graph.DisplayStateTable(); // Display the Excel-format state table

            graph.CreateGraphVizFileAndImage(GeneratedGraph.GraphShape.Circle);

            client.NotifyAdapter = false;
            // If you want stopOnProblem to stop, you need to return false from the AreStatesAcceptablySimilar method
            client.StopOnProblem = true;

            graph.RandomDestinationCoverage("DesklampAndTrafficlight");
        }
    }

    public class DeskLampAndTrafficLight : IEzModelClient
    {
        bool skipSelfLinks;
        bool notifyAdapter;
        bool stopOnProblem;

        // Interface Properties
        public bool SkipSelfLinks
        {
            get => skipSelfLinks;
            set => skipSelfLinks = value;
        }

        public bool NotifyAdapter
        {
            get => notifyAdapter;
            set => notifyAdapter = value;
        }

        public bool StopOnProblem
        {
            get => stopOnProblem;
            set => stopOnProblem = value;
        }

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
        // enable next line to observe treatment of a second transition on two nodes
        const string duplicate = "Duuuuh";
        const string dup2 = "dup2";
        const string dup3 = "dup3";

        // Interface method
        public string GetInitialState()
        {
            // Desk Lamp and Traffic Light
            List<string> initialState = new List<string>();
            initialState.Add(off);
            initialState.Add(red);
            initialState.Sort();

            string stateString = initialState[0] + IEzModelClient.valueSeparator + initialState[1];

            return stateString;
        }

        // Interface method
        public void SetStateOfSystemUnderTest(string state)
        {
        }

        // Interface method
        public void ReportProblem(string initialState, string observed, string predicted, List<string> popcornTrail)
        {
        }

        // Interface method
        public bool AreStatesAcceptablySimilar(string observed, string expected)
        {
            // Compare reported to expected, if unacceptable return false.
            return true;
        }

        // Interface method
        public void ReportTraversal(string initialState, List<string> popcornTrail)
        {

        }

        // Interface method
        public string AdapterTransition(string startState, string action)
        {
            string expected = GetEndState(startState, action);
            string observed = "";
            // What does execution mean?
            //
            // read the graph
            // follow the transition list
            // for each transition,
            //  - set / confirm the start state
            //  - drive execution of the action (of the transition)
            //  - compare endState to state of system under test
            //    - if matching, go to next transition
            //    - if not matching, halt and report
            //      - start state and list of transitions up to the mismatch
            //      - predicted versus actual endState
            return observed;

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
                // enable next line to observe treatment of a second transition on two nodes
                actions.Add(duplicate);
                actions.Add(dup2);
                actions.Add(dup3);
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
            // enable next block to observe treatment of a second transition on two nodes
            else if (action == duplicate || action == dup2 || action == dup3)
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
