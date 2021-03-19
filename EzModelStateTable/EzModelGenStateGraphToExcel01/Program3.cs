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

using System;
using System.Collections.Generic;

namespace EzModelGenStateGraphToExcel01
{
    public struct EzModelIdentifier
    {
        public readonly uint ordinal;
        public readonly string friendlyName;

        public EzModelIdentifier(uint Ordinal, string FriendlyName)
        {
            ordinal = Ordinal;
            friendlyName = FriendlyName;
        }
    }

    // The user implements a public class of rules that inherits the IUserRules interface
    public interface IUserRules
    {
        List<EzModelIdentifier> GetInitialState();
        List<EzModelIdentifier> GetAvailableActions(List<EzModelIdentifier> startState);
        List<EzModelIdentifier> GetEndState(List<EzModelIdentifier> startState, EzModelIdentifier action);
    }

    public class DeskLampAndTrafficLight : IUserRules
    {
        // Desk Lamp and Traffic Light
        // State values
        EzModelIdentifier on = new EzModelIdentifier(1, "deskLampOn");
        EzModelIdentifier off = new EzModelIdentifier(2, "deskLampOff");
        EzModelIdentifier green = new EzModelIdentifier(3, "trafficLightGreen");
        EzModelIdentifier yellow = new EzModelIdentifier(4, "trafficLightYellow");
        EzModelIdentifier red = new EzModelIdentifier(5, "trafficLightRed");

        // Actions
        EzModelIdentifier switchOn = new EzModelIdentifier(6, "switchDeskLampOn");
        EzModelIdentifier switchOff = new EzModelIdentifier(7, "switchDeskLampOff");
        EzModelIdentifier turnGreen = new EzModelIdentifier(8, "turnGreen");
        EzModelIdentifier turnYellow = new EzModelIdentifier(9, "turnYellow");
        EzModelIdentifier turnRed = new EzModelIdentifier(10, "turnRed");

        // Interface method
        public List<EzModelIdentifier> GetInitialState()
        {
            // Desk Lamp and Traffic Light
            List<EzModelIdentifier> initialState = new List<EzModelIdentifier>();
            initialState.Add(off);
            initialState.Add(red);
            return initialState;
        }

        // Interface method
        public List<EzModelIdentifier> GetAvailableActions(List<EzModelIdentifier> startState)
        {
            List<EzModelIdentifier> actions = new List<EzModelIdentifier>();

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

            return actions;

        }

        // Interface method
        public List<EzModelIdentifier> GetEndState(List<EzModelIdentifier> startState, EzModelIdentifier action)
        {
            // C# List quirk:
            // Do not assign startState to endState, because that would be a reference assignment.
            // We want a copy of startState that we can mutate, and we get that by copying each
            // item in startState to a new endState list.
            List<EzModelIdentifier> endState = new List<EzModelIdentifier>();
            foreach (EzModelIdentifier item in startState)
            {
                endState.Add(item);
            }

            if (action.ordinal == switchOn.ordinal)
            {
                endState.Remove(off);
                endState.Add(on);
            }
            else if (action.ordinal == switchOff.ordinal)
            {
                endState.Remove(on);
                endState.Add(off);
            }
            else if (action.ordinal == turnGreen.ordinal)
            {
                endState.Remove(red);
                endState.Add(green);
            }
            else if (action.ordinal == turnYellow.ordinal)
            {
                endState.Remove(green);
                endState.Add(yellow);
            }
            else if (action.ordinal == turnRed.ordinal)
            {
                endState.Remove(yellow);
                endState.Add(red);
            }
            else
            {
                Console.WriteLine("ERROR: unknown action '{0}' in UserRules.GetEndState()", action);
            }

            return endState;
        }
    }

    public class ChutesAndLadders : IUserRules
    {
        // Chutes and Ladders
        // State values

        // WARNING:
        // Do not Sort the squares list at any point. The
        // index choices in the code depend on the list items being
        // in the order they were added to the squares list, which is true
        // until the list gets sorted.
        List<EzModelIdentifier> squares = new List<EzModelIdentifier>();
        List<EzModelIdentifier> bottomOfLadder = new List<EzModelIdentifier>();
        List<EzModelIdentifier> topOfChute = new List<EzModelIdentifier>();

        const string winningSquare = "VictorySquare_100";

        // Actions
        // Individual spin actions, for graph-building
        EzModelIdentifier spin6 = new EzModelIdentifier(1, "Spin_6");
        EzModelIdentifier spin5 = new EzModelIdentifier(2, "Spin_5");
        EzModelIdentifier spin4 = new EzModelIdentifier(3, "Spin_4");
        EzModelIdentifier spin3 = new EzModelIdentifier(4, "Spin_3");
        EzModelIdentifier spin2 = new EzModelIdentifier(5, "Spin_2");
        EzModelIdentifier spin1 = new EzModelIdentifier(6, "Spin_1");
        // Evaluation actions
        EzModelIdentifier descendChute = new EzModelIdentifier(7, "descendChute");
        EzModelIdentifier climbLadder = new EzModelIdentifier(8, "ascendLadder");
        EzModelIdentifier winRestart = new EzModelIdentifier(9, "winRestart");

        public ChutesAndLadders()
        {
            squares.Add(new EzModelIdentifier(100, "Pseudo-square_0"));

            uint i = 101;

            while (i < 200)
            {
                squares.Add(new EzModelIdentifier(i, String.Format("Square_{0}", i - 100)));
                i++;
            }

            squares.Add(new EzModelIdentifier(200, winningSquare));

            // The list of squares that are at the bottom of ladders
            bottomOfLadder.Add(squares[1]);
            bottomOfLadder.Add(squares[4]);
            bottomOfLadder.Add(squares[9]);
            bottomOfLadder.Add(squares[21]);
            bottomOfLadder.Add(squares[28]);
            bottomOfLadder.Add(squares[36]);
            bottomOfLadder.Add(squares[51]);
            bottomOfLadder.Add(squares[71]);
            bottomOfLadder.Add(squares[80]);

            // The list of squares that are at the top of chutes
            topOfChute.Add(squares[16]);
            topOfChute.Add(squares[48]);
            topOfChute.Add(squares[49]);
            topOfChute.Add(squares[56]);
            topOfChute.Add(squares[62]);
            topOfChute.Add(squares[64]);
            topOfChute.Add(squares[87]);
            topOfChute.Add(squares[93]);
            topOfChute.Add(squares[95]);
            topOfChute.Add(squares[98]);
        }

        // Interface method
        public List<EzModelIdentifier> GetInitialState()
        {
            List<EzModelIdentifier> initialState = new List<EzModelIdentifier>();
            initialState.Add(squares[0]);
            // Chutes and Ladders begins off the board,
            // which we model as a pseudo-square number zero.
            return initialState;
        }

        // Interface method
        public List<EzModelIdentifier> GetAvailableActions(List<EzModelIdentifier> startState)
        {
            List<EzModelIdentifier> actions = new List<EzModelIdentifier>();

            if (startState.Count != 1)
            {
                Console.WriteLine("ERROR: Chutes and Ladders state contains more than one value in GetAvailableActions()");
                return actions;
            }

            if (bottomOfLadder.Contains(startState[0]))
            {
                actions.Add(climbLadder);
            }
            else if (topOfChute.Contains(startState[0]))
            {
                actions.Add(descendChute);
            }
            else if (startState[0].friendlyName == winningSquare)
            {
                // Winner!!
                actions.Add(winRestart);
            }
            else
            {
                actions.Add(spin1);
                actions.Add(spin2);
                actions.Add(spin3);
                actions.Add(spin4);
                actions.Add(spin5);
                actions.Add(spin6);
            }

            return actions;
        }

        // Interface method
        public List<EzModelIdentifier> GetEndState(List<EzModelIdentifier> startState, EzModelIdentifier action)
        {

            List<EzModelIdentifier> endState = new List<EzModelIdentifier>();

            // The end state for certain rolls on squares 96, 97, and 99
            // is the same as the start state.  These special cases are
            // spelled out in the switch cases, below.

            if (action.ordinal == spin6.ordinal)
            {
                // on squares 96, 97, and 99, the end state is the same as the start state
                if (startState[0].ordinal > 194)
                {
                    return startState;
                }
                endState.Add(squares[(int)startState[0].ordinal - 100 + 6]);
            }
            else if (action.ordinal == spin5.ordinal)
            {
                // on squares 96, 97, and 99, the end state is the same as the start state
                if (startState[0].ordinal > 195)
                {
                    return startState;
                }
                endState.Add(squares[(int)startState[0].ordinal - 100 + 5]);
            }
            else if (action.ordinal == spin4.ordinal)
            {
                // on squares 97 and 99, the end state is the same as the start state
                if (startState[0].ordinal > 196)
                {
                    return startState;
                }
                endState.Add(squares[(int)startState[0].ordinal - 100 + 4]);
            }
            else if (action.ordinal == spin3.ordinal)
            {
                // on square 99, the end state is the same as the start state
                if (startState[0].ordinal > 197)
                {
                    return startState;
                }
                endState.Add(squares[(int)startState[0].ordinal - 100 + 3]);
            }
            else if (action.ordinal == spin2.ordinal)
            {
                // on square 99, the end state is the same as the start state
                if (startState[0].ordinal > 198)
                {
                    return startState;
                }
                endState.Add(squares[(int)startState[0].ordinal - 100 + 2]);
            }
            else if (action.ordinal == spin1.ordinal)
            {
                if (startState[0].ordinal == 200)
                {
                    Console.WriteLine("ERROR: spin == one action taken when startState is square 100, in UserRules.GetEndState()");
                    return startState;
                }
                endState.Add(squares[(int)startState[0].ordinal - 100 + 1]);
            }
            else if (action.ordinal == descendChute.ordinal)
            {
                // the end state is determined by a relationship to the start state
                switch (startState[0].ordinal)
                {
                    case 198:
                        endState.Add(squares[78]);
                        break;
                    case 195:
                        endState.Add(squares[75]);
                        break;
                    case 193:
                        endState.Add(squares[73]);
                        break;
                    case 187:
                        endState.Add(squares[24]);
                        break;
                    case 164:
                        endState.Add(squares[60]);
                        break;
                    case 162:
                        endState.Add(squares[19]);
                        break;
                    case 156:
                        endState.Add(squares[53]);
                        break;
                    case 149:
                        endState.Add(squares[11]);
                        break;
                    case 148:
                        endState.Add(squares[26]);
                        break;
                    case 116:
                        endState.Add(squares[6]);
                        break;
                    default:
                        Console.WriteLine("ERROR: goDownChute action taken when at square {0}, not at a topOfChute square, in UserRules.GetEndState()", startState[0].friendlyName);
                        return startState; // return something...
                }
            }
            else if (action.ordinal == climbLadder.ordinal)
            {
                // the end state is determined by a relationship to the start state
                switch (startState[0].ordinal)
                {
                    case 101:
                        endState.Add(squares[38]);
                        break;
                    case 104:
                        endState.Add(squares[14]);
                        break;
                    case 109:
                        endState.Add(squares[31]);
                        break;
                    case 121:
                        endState.Add(squares[42]);
                        break;
                    case 128:
                        endState.Add(squares[84]);
                        break;
                    case 136:
                        endState.Add(squares[44]);
                        break;
                    case 151:
                        endState.Add(squares[67]);
                        break;
                    case 171:
                        endState.Add(squares[91]);
                        break;
                    case 180:
                        endState.Add(squares[100]);
                        break;
                    default:
                        Console.WriteLine("ERROR: climbLadder action taken when at square {0}, not at a bottomOfLadder square, in UserRules.GetEndState()", startState[0].friendlyName);
                        return startState; // return something...
                }
            }
            else if (action.ordinal == winRestart.ordinal)
            {
                // this action is only possible when the start state is squares[100]
                endState.Add(squares[0]);
            }
            else
            {
                Console.WriteLine("ERROR: unknown action '{0}' in UserRules.GetEndState()", action);
                return startState;
            }
            return endState;
        }
    }

    public class Program
    {
        public static void Main()
        {
            // Toggle commenting of the next two lines to try the two different example models.

            // DeskLampAndTrafficLight rules = new DeskLampAndTrafficLight();
            ChutesAndLadders rules = new ChutesAndLadders();

            GeneratedGraph graph = new GeneratedGraph(rules);

            graph.DisplayStateTable(); // Display the Excel-format state table
            Console.ReadLine();
        }
    }

    public class GeneratedGraph
    {
        List<string> transitions = new List<string>();
        List<List<EzModelIdentifier>> totalNodes;
        List<List<EzModelIdentifier>> unexploredNodes;
        IUserRules rules; // We are able to refer to the user rules by interface, because we are only calling interface methods

        string transitionSeparator = " | ";
        string identifierSeparator = ", ";

        public string BuildTransition(List<EzModelIdentifier> startState, EzModelIdentifier action, List<EzModelIdentifier> endState)
        {
            string t = "";

            foreach (EzModelIdentifier stateValue in startState)
            {
                t += stateValue.friendlyName + identifierSeparator;
            }
            if (startState.Count > 0)
            {
                t = t.Substring(0, t.Length - 2) + transitionSeparator;
            }
            t += action.friendlyName + transitionSeparator;
            foreach (EzModelIdentifier stateValue in endState)
            {
                t += stateValue.friendlyName + identifierSeparator;
            }
            if (endState.Count > 0)
            {
                t = t.Substring(0, t.Length - 2);
            }

            return t;
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
            totalNodes = new List<List<EzModelIdentifier>>();
            unexploredNodes = new List<List<EzModelIdentifier>>();
            rules = theRules; // we follow the Rules!

            List<EzModelIdentifier> s = rules.GetInitialState();
            unexploredNodes.Add(s);
            totalNodes.Add(s);

            while (unexploredNodes.Count > 0)
            {
                // generate all transitions out of state s
                s = FetchUnexploredNode();
                AddNewTransitionsToGraph(s);
            }
        }

        public List<EzModelIdentifier> FetchUnexploredNode()
        {
            List<EzModelIdentifier> s = unexploredNodes[0];
            unexploredNodes.RemoveAt(0);
            return s;
        }

        // An internal method to compare a list to other lists.
        // The built-in List.Contains() method fails here as
        // we have a list of lists, and it turns out Contains()
        // doesn't determine list equivalency as we wish.
        bool TotalNodesContainsEndState(List<EzModelIdentifier> endState)
        {
            bool contains = false;

            foreach (List<EzModelIdentifier> state in totalNodes)
            {
                if (state.Count != endState.Count)
                {
                    // Look at next state if this state is a different size from endState
                    continue;
                }

                contains = true;
                foreach (EzModelIdentifier item in endState)
                {
                    if (!state.Contains(item))
                    {
                        // state not matched.  Exit this foreach loop.
                        contains = false;
                    }
                }
                // We have a match, so endState is contained in totalNodes.
                if (contains == true)
                {
                    return true;
                }
            }
            return contains;
        }

        public void AddNewTransitionsToGraph(List<EzModelIdentifier> startState)
        {
            List<EzModelIdentifier> Actions = rules.GetAvailableActions(startState);

            foreach (EzModelIdentifier action in Actions)
            {
                // an endstate is generated from current state + changes from an invoked action
                List<EzModelIdentifier> endState = rules.GetEndState(startState, action);

                // if generated endstate is new, add  to the totalNode & unexploredNode lists
                if (!TotalNodesContainsEndState(endState))
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
