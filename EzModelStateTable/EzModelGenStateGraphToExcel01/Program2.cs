// EZmodel Sample State Generation to Spreadsheet
// copyright 2019 Serious Quality LLC

// Adjusted March 15, 2021 with developer thoughts:
// - removed Reflection from original code
// - decoupled model rules from ezModel state table construction
// - ezModel reads model rules through C# interface IUserRules
// - person providing model rules is responsible for implementing IUserRules methods
// - within this file two example model rules classes are provided, called
//     DeskLampAndTrafficLight, and
//     ChutesAndLadders
//
//     DeskLampAndTrafficLight is the story of an insane person who believes they
//   affect the color of a traffic light when they flip their desk lamp switch
//   from off to on, and from on to off.
//
//     ChutesAndLadders yields the unabridged, and finite, graph of the classic
//   Chutes and Ladders board game.  Fun for all ages.
//
//   Compliments of Doug Szabo, for Harry Robinson.

/*
using System;
using System.Collections.Generic;

namespace EzModelGenStateGraphToExcel01
{
    // The user implements a public class of rules that inherits the IUserRules interface
    public interface IUserRules
    {
        string GetInitialState();
        string[] GetAvailableActions(string startState);
        string GetEndState(string startState, string action);
    }

    public class DeskLampAndTrafficLight : IUserRules
    {
        // Desk Lamp and Traffic Light
        // State values
        const string on = "deskLampOn";
        const string off = "deskLampOff";
        const string green = "trafficLightGreen";
        const string yellow = "trafficLightYellow";
        const string red = "trafficLightRed";

        // Actions
        const string switchOn = "switchDeskLampOn";
        const string switchOff = "switchDeskLampOff";
        const string turnGreen = "turnGreen";
        const string turnYellow = "turnYellow";
        const string turnRed = "turnRed";

        // Interface method
        public string GetInitialState()
        {
            // Desk Lamp and Traffic Light
            return off + ", " + red;
        }

        // Interface method
        public string[] GetAvailableActions(string startState)
        {
            List<string> actions = new List<string>();
            
            // For traffic light and desk lamp, use if-else decisions
            // since there are so few decisions.
            if ( startState.Contains(on)) {
                actions.Add(switchOff);
            }
            else if ( startState.Contains(off))
            {
                actions.Add(switchOn);
            }
            else // Problem: the desk lamp state is indeterminate
            {
                Console.WriteLine("ERROR in UserRules.GetAvailableActions(): missing or unknown desklamp state value: {0}", startState);
            }

            if ( startState.Contains(green))
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

            return actions.ToArray();

        }

        // Interface method
        public string GetEndState(string startState, string action)
        {
            string endState = "";

            switch (action)
            {
                case switchOn:
                    endState = startState.Replace(off, on);
                    break;
                case switchOff:
                    endState = startState.Replace(on, off);
                    break;
                case turnGreen:
                    endState = startState.Replace(red, green);
                    break;
                case turnYellow:
                    endState = startState.Replace(green, yellow);
                    break;
                case turnRed:
                    endState = startState.Replace(yellow, red);
                    break;
                default:
                    Console.WriteLine("ERROR: unknown action '{0}' in UserRules.GetEndState()", action);
                    break;
            }

            return endState;
        }
    }

    public class ChutesAndLadders : IUserRules
    {
        // Chutes and Ladders
        // State values

        // NOTE: the square number currently occupied is
        // initialized to zero and then the occupied square
        // is modified in the action logic.  So, the square
        // is not stored in a member variable of this class,
        // rather it is passed back and forth between this
        // class and EzModel.
        
        List<uint> bottomOfLadder = new List<uint>();
        List<uint> topOfChute = new List<uint>();

        // Actions
        // An already-formed array of spins for convenience
        readonly string[] spin = { "1", "2", "3", "4", "5", "6" };
        // Individual spin actions, for graph-building
        const string six = "6";
        const string five = "5";
        const string four = "4";
        const string three = "3";
        const string two = "2";
        const string one = "1";
        // Evaluation actions
        const string goDownChute = "goDownChute";
        const string climbLadder = "climbLadder";
        const string win = "goToOrigin";

        public ChutesAndLadders()
        {
            // The list of squares that are at the bottom of ladders
            bottomOfLadder.Add(1);
            bottomOfLadder.Add(4);
            bottomOfLadder.Add(9);
            bottomOfLadder.Add(21);
            bottomOfLadder.Add(28);
            bottomOfLadder.Add(36);
            bottomOfLadder.Add(51);
            bottomOfLadder.Add(71);
            bottomOfLadder.Add(80);

            // The list of squares that are at the top of chutes
            topOfChute.Add(16);
            topOfChute.Add(48);
            topOfChute.Add(49);
            topOfChute.Add(56);
            topOfChute.Add(62);
            topOfChute.Add(64);
            topOfChute.Add(87);
            topOfChute.Add(93);
            topOfChute.Add(95);
            topOfChute.Add(98);
        }

        // Interface method
        public string GetInitialState()
        {
            // Chutes and Ladders begins off the board,
            // which we model as a pseudo-square number zero.
            return "0";
        }

        // Interface method
        public string[] GetAvailableActions(string startState)
        {
            List<string> actions = new List<string>();

            uint stateNumber = uint.Parse(startState);

            if (bottomOfLadder.Contains(stateNumber))
            {
                actions.Add(climbLadder);
                return actions.ToArray();
            }
            else if (topOfChute.Contains(stateNumber))
            {
                actions.Add(goDownChute);
                return actions.ToArray();
            }
            else if (stateNumber == 100)
            {
                // Winner!!
                actions.Add(win);
                return actions.ToArray();
            }

            return spin;
        }

        // Interface method
        public string GetEndState(string startState, string action)
        {
            uint stateNumber = uint.Parse(startState);

            // The end state for certain rolls on squares 96, 97, and 99
            // is the same as the start state.  These special cases are
            // spelled out in the switch cases, below.
            switch (action)
            {
                case six: // "6"
                    // on squares 96, 97, and 99, the end state is the same as the start state
                    if ( stateNumber > 94 )
                    {
                        return startState;
                    }
                    return String.Format("{0}", stateNumber + 6);
                    
                case five: // "5"
                    // on squares 96, 97, and 99, the end state is the same as the start state
                    if ( stateNumber > 95 )
                    {
                        return startState;
                    }
                    return String.Format("{0}", stateNumber + 5);

                case four: // "4"
                    // on squares 97 and 99, the end state is the same as the start state
                    if ( stateNumber > 96 )
                    {
                        return startState;
                    }
                    return String.Format("{0}", stateNumber + 4);

                case three: // "3"
                    // on square 99, the end state is the same as the start state
                    if ( stateNumber > 97 )
                    {
                        return startState;
                    }
                    return String.Format("{0}", stateNumber + 3);

                case two: // "2"
                    // on square 99, the end state is the same as the start state
                    if ( stateNumber > 98 )
                    {
                        return startState;
                    }
                    return String.Format("{0}", stateNumber + 2);

                case one: // "1"
                    if ( stateNumber == 100 )
                    {
                        Console.WriteLine("ERROR: spin == one action taken when startState is square 100, in UserRules.GetEndState()");
                        return startState;
                    }
                    return String.Format("{0}", stateNumber + 1);
                    
                case goDownChute: // "goDownChute"
                    // the end state is determined by a relationship to the start state
                    switch (stateNumber)
                    {
                        case 98:
                            return "78";
                        case 95:
                            return "75";
                        case 93:
                            return "73";
                        case 87:
                            return "24";
                        case 64:
                            return "60";
                        case 62:
                            return "19";
                        case 56:
                            return "53";
                        case 49:
                            return "11";
                        case 48:
                            return "26";
                        case 16:
                            return "6";
                        default:
                            Console.WriteLine("ERROR: goDownChute action taken when at square {0}, not at a topOfChute square, in UserRules.GetEndState()", startState);
                            return startState; // return something...
                    }

                case climbLadder: // "climbLadder"
                    // the end state is determined by a relationship to the start state
                    switch (stateNumber)
                    {
                        case 1:
                            return "38";
                        case 4:
                            return "14";
                        case 9:
                            return "31";
                        case 21:
                            return "42";
                        case 28:
                            return "84";
                        case 36:
                            return "44";
                        case 51:
                            return "67";
                        case 71:
                            return "91";
                        case 80:
                            return "100";
                        default:
                            Console.WriteLine("ERROR: climbLadder action taken when at square {0}, not at a bottomOfLadder square, in UserRules.GetEndState()", startState);
                            return startState; // return something...
                    }

                case win: // "goToOrigin"
                    // this action is only possible when the start state is squares[100]
                    return "0";

                default:
                    Console.WriteLine("ERROR: unknown action '{0}' in UserRules.GetEndState()", action);
                    return startState;
            }
        }
    }

    public class Program
    {
        public static void Main()
        {
            // Toggle commenting of the next two lines to try the two different example models.

            //DeskLampAndTrafficLight rules = new DeskLampAndTrafficLight();
            ChutesAndLadders rules = new ChutesAndLadders();

            GeneratedGraph graph = new GeneratedGraph(rules);

            graph.DisplayStateTable(); // Display the Excel-format state table
            Console.ReadLine();
        }
    }

    public class GeneratedGraph
    {
        List<string> transitions = new List<string>();
        List<string> totalNodes;
        List<string> unexploredNodes;
        IUserRules rules; // We are able to refer to the user rules by interface, because we are only calling interface methods

        string transitionSeparator = " | ";

        public string BuildTransition(string startState, string action, string endState)
        {
            return startState + transitionSeparator + action + transitionSeparator + endState;
        }

        public void DisplayStateTable()
        {
            Console.WriteLine("Start state{0}Action{0}End state\n", transitionSeparator);

            foreach (string t in transitions)
            {
                Console.WriteLine(t);
            }
        }

        public GeneratedGraph(IUserRules theRules)
        {
            totalNodes = new List<string>();
            unexploredNodes = new List<string>();
            rules = theRules; // we follow the Rules!

            string s = rules.GetInitialState();
            unexploredNodes.Add(s);
            totalNodes.Add(s);

            while (unexploredNodes.Count > 0)
            {
                // generate all transitions out of state s
                s = FetchUnexploredNode();
                AddNewTransitionsToGraph(s);
            }
        }

        public string FetchUnexploredNode()
        {
            string s = unexploredNodes[0];
            unexploredNodes.RemoveAt(0);
            return s;
        }

        public void AddNewTransitionsToGraph(string startState)
        {
            string[] Actions = rules.GetAvailableActions(startState);             

            foreach (string action in Actions)
            {
                // an endstate is generated from current state + changes from an invoked action
                string endState = rules.GetEndState(startState, action);

                // if generated endstate is new, add  to the totalNode & unexploredNode lists
                if (!totalNodes.Contains(endState))
                {
                    totalNodes.Add(endState);
                    unexploredNodes.Add(endState);
                }

                // add this {startState, action, endState} transition to the Graph
                transitions.Add(BuildTransition(startState, action, endState));
            }
        }
    }
}
*/