// EZmodel Sample State Generation to Spreadsheet
// copyright 2021 Serious Quality LLC

// Model the classic board game Monopoly

using System;
using System.Collections.Generic;
using SeriousQualityEzModel;

namespace Monopoly
{
    public class MonopolyProgram
    {
        public static void Main()
        {
            Monopoly client = new Monopoly();
            client.SelfLinkTreatment = SelfLinkTreatmentChoice.AllowAll;

            EzModelGraph graph = new EzModelGraph(client, 1100, 110, 14, EzModelGraph.LayoutRankDirection.TopDown);

            if (graph.GenerateGraph())
            {
                graph.DisplayStateTable(); // Display the Excel-format state table

                // write graph file before traversal
                graph.CreateGraphVizFileAndImage(EzModelGraph.GraphShape.Default);

                // Enable NotifyAdapter ONLY when the AdapterTransition function is
                // fully coded.  Otherwise, the decision about available actions
                // can get screwed up by incomplete AdapterTransition code.
                client.NotifyAdapter = false;
                // If you want stopOnProblem to stop, you need to return false from the AreStatesAcceptablySimilar method
                client.StopOnProblem = true;

                graph.RandomDestinationCoverage("Monopoly", 1);
            }
        }
    }

    public class Monopoly : IEzModelClient
    {
        SelfLinkTreatmentChoice skipSelfLinks;
        bool notifyAdapter;
        bool stopOnProblem;

        // Interface Properties
        public SelfLinkTreatmentChoice SelfLinkTreatment
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

        // Stave variables
        public uint svSquare = 0; // Index into the Square array indicating board position.

        // State Values for the 40 squares on the board + the in Jail pseudo-square
        string[] Square = {"Go", "Mediterannean Ave", "Community Chest 1",
            "Baltic Ave", "Income Tax", "Reading Railroad", "Oriental Ave",
            "Chance 1", "Vermont Ave", "Connecticut Ave", "Just Visiting",
            "St. Charles Place", "Electric Co.", "States Ave", "Virginia Ave",
            "Pennsylvania Railroad", "St. James Place", "Community Chest 2",
            "Tennessee Ave", "New York Ave", "Free Parking", "Kentucky Ave",
            "Chance 2", "Indiana Ave", "Illinois Ave", "B & O Railroad",
            "Atlantic Ave", "Ventnor Ave", "Water Works", "Marvin Gardens",
            "Go to Jail", "Pacific Ave", "North Carolina Ave",
            "Community Chest 3", "Pennsylvania Ave", "Short Line Railroad",
            "Chance 3", "Park Place", "Luxury Tax", "Board Walk", "In Jail"};

        // Chance and Community Chest cards
        // State values
//        List<string> bottomsOfLadders = new List<string>();
//        List<string> topsOfChutes = new List<string>();

        // Actions
        // Individual dice actions, for graph-building
        const string roll12 = "Move_12";
        const string roll11 = "Move_11";
        const string roll10 = "Move_10";
        const string roll9 = "Move_9";
        const string roll8 = "Move_8";
        const string roll7 = "Move_7";
        const string roll6 = "Move_6";
        const string roll5 = "Move_5";
        const string roll4 = "Move_4";
        const string roll3 = "Move_3";
        const string roll2 = "Move_2";
        const string roll1 = "Move_1"; // not a real thing but lets us cover the board right away.

        // Evaluation actions
        const string goToJail = "Go to Jail";
        const string goToJustVisiting = "Go to Just Visiting";
        const string advanceToGo = "Advance to Go";

        public Monopoly()
        {
            // Set up Real Estate, Chance cards, and Community Chest cards
            // Set up player info: money, for instance
            svSquare = 0;
        }

/* ****    MODEL CREATION   **** */

        // Interface method for model creation
        public string GetInitialState()
        {
            // Chutes and Ladders begins off the board,
            // which we model as a pseudo-square number zero.
            return "0"; // Go
        }

        // Interface method for model creation
        public List<string> GetAvailableActions(string startState)
        {
            List<string> actions = new List<string>();

            uint currentState = uint.Parse(startState.Split(".")[0]);
            if (currentState == 40)
            {
                actions.Add(goToJustVisiting);
            }

            if (currentState == 30)
            {
                actions.Add(goToJail);
            }

            if (currentState != 30 && currentState != 40)
            {
                actions.Add(roll2);
                actions.Add(roll3);
                actions.Add(roll4);
                actions.Add(roll5);
                actions.Add(roll6);
                actions.Add(roll7);
                actions.Add(roll8);
                actions.Add(roll9);
                actions.Add(roll10);
                actions.Add(roll11);
                actions.Add(roll12);
            }

            return actions;
        }

        // Interface method for model creation
        public string GetEndState(string startState, string action)
        {
            uint currentState = uint.Parse(startState.Split(".")[0]);

            switch (action)
            {
                case goToJail:
                    return "40"; // In Jail

                case goToJustVisiting:
                    return "10"; // Just visiting

                case roll2:
                    return ((currentState + 2) % 40).ToString();

                case roll3:
                    return ((currentState + 3) % 40).ToString();

                case roll4:
                    return ((currentState + 4) % 40).ToString();

                case roll5:
                    return ((currentState + 5) % 40).ToString();

                case roll6:
                    return ((currentState + 6) % 40).ToString();

                case roll7:
                    return ((currentState + 7) % 40).ToString();

                case roll8:
                    return ((currentState + 8) % 40).ToString();

                case roll9:
                    return ((currentState + 9) % 40).ToString();

                case roll10:
                    return ((currentState + 10) % 40).ToString();

                case roll11:
                    return ((currentState + 11) % 40).ToString();

                case roll12:
                    return ((currentState + 12) % 40).ToString();

                /*
                    * Handle Chance and Community Chest with this kind of case + switch block
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
                                        case 80:
                                            return svSquare + ".100";
                                        default:
                                            Console.WriteLine("ERROR: ascendLadder action taken when at square {0}, not at a bottomsOfLadders square, in UserRules.GetEndState()", currentState);
                                            return startState; // return something...
                                    }
                */
                default:
                    Console.WriteLine("ERROR: unknown action '{0}' in UserRules.GetEndState()", action);
                    return startState;
            }
        }

/* ****    ADAPTER    **** */
        // Interface method for Adapter
        public void SetStateOfSystemUnderTest(string state)
        {
        }

        // Interface method for Adapter
        public void ReportProblem(string initialState, string observed, string predicted, List<string> popcornTrail)
        {
        }

        // Interface method for Adapter
        public bool AreStatesAcceptablySimilar(string observed, string expected)
        {
            // Compare reported to expected, if unacceptable return false.
            return true;
        }

        // Interface method
        public void ReportTraversal(string initialState, List<string> popcornTrail)
        {

        }

        // Interface method for Adapter
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
    }
}
