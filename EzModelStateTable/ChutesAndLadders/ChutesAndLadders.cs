// EZmodel Sample State Generation to Spreadsheet
// copyright 2019 Serious Quality LLC

// Adjusted March 16-17, 2021 with developer thoughts:
// - decoupled model rules from ezModel state table construction
// - ezModel reads model rules through C# interface IUserRules
// - person providing model rules is responsible for implementing IUserRules methods
//
//     ChutesAndLadders yields the unabridged, and finite, graph of the classic
//   Chutes and Ladders board game.  Fun for all ages.
//
//   Compliments of Doug Szabo, for Harry Robinson.

using System;
using System.Collections.Generic;
using SeriousQualityEzModel;

namespace ChutesAndLaddersExample
{
    public class ChutesAndLaddersProgram
    {
        public static void Main()
        {
            ChutesAndLadders client = new ChutesAndLadders();
            client.SkipSelfLinks = false;

            GeneratedGraph graph = new GeneratedGraph(client, 1100, 110, 10);

            graph.DisplayStateTable(); // Display the Excel-format state table

            // write graph file before traversal
            graph.CreateGraphVizFileAndImage(GeneratedGraph.GraphShape.Circle);

            // Enable NotifyAdapter ONLY when the AdapterTransition function is
            // fully coded.  Otherwise, the decision about available actions
            // can get screwed up by incomplete AdapterTransition code.
            client.NotifyAdapter = false;
            // If you want stopOnProblem to stop, you need to return false from the AreStatesAcceptablySimilar method
            client.StopOnProblem = true;

            graph.RandomDestinationCoverage("ChutesAndLadders");
        }
    }

    public class ChutesAndLadders : IEzModelClient
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
        const string svSquare = "Square";

        // Chutes and Ladders
        // State values
        List<string> bottomsOfLadders = new List<string>();
        List<string> topsOfChutes = new List<string>(); 
        const string winningSquare = svSquare + ".100";

        // Actions
        // Individual spin actions, for graph-building
        const string spin6 = "Move_6";
        const string spin5 = "Move_5";
        const string spin4 = "Move_4";
        const string spin3 = "Move_3";
        const string spin2 = "Move_2";
        const string spin1 = "Move_1";
        // Evaluation actions
        const string descendChute = "descendChute";
        const string ascendLadder = "ascendLadder";
        const string winRestart = "winRestart";

        public ChutesAndLadders()
        {
            // The list of squares that are at the bottom of ladders
            bottomsOfLadders.Add(svSquare + ".1");
            bottomsOfLadders.Add(svSquare + ".4");
            bottomsOfLadders.Add(svSquare + ".9");
            bottomsOfLadders.Add(svSquare + ".21");
            bottomsOfLadders.Add(svSquare + ".28");
            bottomsOfLadders.Add(svSquare + ".36");
            bottomsOfLadders.Add(svSquare + ".51");
            bottomsOfLadders.Add(svSquare + ".71");
            bottomsOfLadders.Add(svSquare + ".80");

            // The list of squares that are at the top of chutes
            topsOfChutes.Add(svSquare + ".16");
            topsOfChutes.Add(svSquare + ".48");
            topsOfChutes.Add(svSquare + ".49");
            topsOfChutes.Add(svSquare + ".56");
            topsOfChutes.Add(svSquare + ".62");
            topsOfChutes.Add(svSquare + ".64");
            topsOfChutes.Add(svSquare + ".87");
            topsOfChutes.Add(svSquare + ".93");
            topsOfChutes.Add(svSquare + ".95");
            topsOfChutes.Add(svSquare + ".98");
        }

        // Interface method
        public string GetInitialState()
        {
            // Chutes and Ladders begins off the board,
            // which we model as a pseudo-square number zero.
            return svSquare + ".0";
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

            if (bottomsOfLadders.Contains(startState))
            {
                actions.Add(ascendLadder);
            }
            else if (topsOfChutes.Contains(startState))
            {
                actions.Add(descendChute);
            }
            else if (startState == winningSquare)
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
        public string GetEndState(string startState, string action)
        {
            // The end state for certain rolls on squares 96, 97, and 99
            // is the same as the start state.  These special cases are
            // spelled out in the switch cases, below.

            uint currentState = uint.Parse(startState.Split(".")[1]);

            switch (action)
            {
                case spin6:
                // on squares 96, 97, and 99, the end state is the same as the start state
                    if (currentState > 94 )
                    {
                        return startState;
                    }
                    return svSquare + String.Format(".{0}", currentState + 6);

                case spin5:
                    // on squares 96, 97, and 99, the end state is the same as the start state
                    if (currentState > 95)
                    {
                        return startState;
                    }
                    return svSquare + String.Format(".{0}", currentState + 5);

                case spin4:
                    // on squares 97 and 99, the end state is the same as the start state
                    if (currentState > 96)
                    {
                        return startState;
                    }
                    return svSquare + String.Format(".{0}", currentState + 4);

                case spin3:
                    // on square 99, the end state is the same as the start state
                    if (currentState > 97)
                    {
                        return startState;
                    }
                    return svSquare + String.Format(".{0}", currentState + 3);

                case spin2:
                    // on square 99, the end state is the same as the start state
                    if (currentState > 98)
                    {
                        return startState;
                    }
                    return svSquare + String.Format(".{0}", currentState + 2);

                case spin1:
                    // on square 99, the end state is the same as the start state
                    if (currentState > 99)
                    {
                        return startState;
                    }
                    return svSquare + String.Format(".{0}", currentState + 1);

                case descendChute:
                    // the end state is determined by a relationship to the start state
                    switch (currentState)
                    {
                        case 98:
                            return svSquare + ".78";
                        case 95:
                            return svSquare + ".75";
                        case 93:
                            return svSquare + ".73";
                        case 87:
                            return svSquare + ".24";
                        case 64:
                            return svSquare + ".60";
                        case 62:
                            return svSquare + ".19";
                        case 56:
                            return svSquare + ".53";
                        case 49:
                            return svSquare + ".11";
                        case 48:
                            return svSquare + ".26";
                        case 16:
                            return svSquare + ".6";
                        default:
                            Console.WriteLine("ERROR: descendChute action taken when at square {0}, not at a topsOfChutes square, in UserRules.GetEndState()", currentState);
                            return startState; // return something...
                    }

                case ascendLadder:
                    // the end state is determined by a relationship to the start state
                    switch (currentState)
                    {
                        case 1:
                            return svSquare + ".38";
                        case 4:
                            return svSquare + ".14";
                        case 9:
                            return svSquare + ".31";
                        case 21:
                            return svSquare + ".42";
                        case 28:
                            return svSquare + ".84";
                        case 36:
                            return svSquare + ".44";
                        case 51:
                            return svSquare + ".67";
                        case 71:
                            return svSquare + ".91";
                        case 80:
                            return svSquare + ".100";
                        default:
                            Console.WriteLine("ERROR: ascendLadder action taken when at square {0}, not at a bottomsOfLadders square, in UserRules.GetEndState()", currentState);
                            return startState; // return something...
                    }

                case winRestart:
                    return svSquare + ".0";

                default:
                    Console.WriteLine("ERROR: unknown action '{0}' in UserRules.GetEndState()", action);
                    return startState;
            }
        }
    }
}
