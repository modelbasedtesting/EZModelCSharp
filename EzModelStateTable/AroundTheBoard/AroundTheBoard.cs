// EZmodel State Graph Generation
// copyright 2021 Serious Quality LLC

// Model the classic board game Monopoly
// Step 1: travel any token around the board

using System;
using System.Collections.Generic;
using SeriousQualityEzModel;

namespace AroundTheBoard
{
    public class AroundTheBoardProgram
    {
        public static void Main()
        {
            Monopoly client = new Monopoly()
            {
                SelfLinkTreatment = SelfLinkTreatmentChoice.AllowAll
            };

            EzModelGraph graph = new EzModelGraph(client, 1100, 110, 14, EzModelGraph.LayoutRankDirection.TopDown);

            if (graph.GenerateGraph())
            {
                //   graph.DisplayStateTable(); // Display the Excel-format state table

                // write graph file before traversal
                graph.CreateGraphVizFileAndImage(EzModelGraph.GraphShape.Default);

                // Enable NotifyAdapter ONLY when the AdapterTransition function is
                // fully coded.  Otherwise, the decision about available actions
                // can get screwed up by incomplete AdapterTransition code.
                client.NotifyAdapter = true;
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

        // State variables
        public uint svSquare = 0; // Index into the Square array indicating board position.

        public struct GameSquare
        {
            public uint location; // State
            public string title; // constant

            public GameSquare(uint loc, string t)
            {
                this.location = loc;
                this.title = t;
            }
        }


        // State Values for the 40 squares on the board + the in Jail pseudo-square
        public GameSquare[] gameSquares = {
            new GameSquare( 0, "Go"),
            new GameSquare( 1, "Mediterannean Ave"),
            new GameSquare( 2, "Community Chest 1"),
            new GameSquare( 3, "Baltic Ave"),
            new GameSquare( 4, "Income Tax"),
            new GameSquare( 5, "Reading Railroad"),
            new GameSquare( 6, "Oriental Ave"),
            new GameSquare( 7, "Chance 1"),
            new GameSquare( 8, "Vermont Ave"),
            new GameSquare( 9, "Connecticut Ave"),
            new GameSquare(10, "Just Visiting"),
            new GameSquare(11, "St. Charles Place"),
            new GameSquare(12, "Electric Co."),
            new GameSquare(13, "States Ave"),
            new GameSquare(14, "Virginia Ave"),
            new GameSquare(15, "Pennsylvania Railroad"),
            new GameSquare(16, "St. James Place"),
            new GameSquare(17, "Community Chest 2"),
            new GameSquare(18, "Tennessee Ave"),
            new GameSquare(19, "New York Ave"),
            new GameSquare(20, "Free Parking"),
            new GameSquare(21, "Kentucky Ave"),
            new GameSquare(22, "Chance 2"),
            new GameSquare(23, "Indiana Ave"),
            new GameSquare(24, "Illinois Ave"),
            new GameSquare(25, "B & O Railroad"),
            new GameSquare(26, "Atlantic Ave"),
            new GameSquare(27, "Ventnor Ave"),
            new GameSquare(28, "Water Works"),
            new GameSquare(29, "Marvin Gardens"),
            new GameSquare(30, "Go to Jail"),
            new GameSquare(31, "Pacific Ave"),
            new GameSquare(32, "North Carolina Ave"),
            new GameSquare(33, "Community Chest 3"),
            new GameSquare(34, "Pennsylvania Ave"),
            new GameSquare(35, "Short Line Railroad"),
            new GameSquare(36, "Chance 3"),
            new GameSquare(37, "Park Place"),
            new GameSquare(38, "Luxury Tax"),
            new GameSquare(39, "Board Walk")
        };

        // Actions
        // Individual dice actions, for graph-building
        const string roll3 = "Move_3";
        const string roll2 = "Move_2";

        public Monopoly()
        {
            svSquare = 0;
        }

        /* ****    MODEL CREATION   **** */

        // Interface method for model creation
        public string GetInitialState()
        {
            // Chutes and Ladders begins off the board,
            // which we model as a pseudo-square number zero.
            return gameSquares[0].title; // Go
        }

        uint findGameSquareFromTitle(string squareTitle)
        {
            for (uint i = 0; i < gameSquares.Length; i++)
            {
                if (gameSquares[i].title == squareTitle)
                {
                    return i;
                }
            }
            // Error: squareTitle not matched
            // TODO: throw an exception
            return 42;
        }

        // Interface method for model creation
        public List<string> GetAvailableActions(string startState)
        {
            List<string> actions = new List<string>();

            uint currentSquare = findGameSquareFromTitle(startState);

            actions.Add(roll2);
            actions.Add(roll3);

            return actions;
        }

        // Interface method for model creation
        public string GetEndState(string startState, string action)
        {
            uint currentSquare = findGameSquareFromTitle(startState);

            switch (action)
            {
                case roll2:
                    currentSquare = (currentSquare + 2) % 40;
                    break;

                case roll3:
                    currentSquare = (currentSquare + 3) % 40;
                    break;

                default:
                    Console.WriteLine("ERROR: unknown action '{0}' in UserRules.GetEndState()", action);
                    return startState;
            }

            string currentState = gameSquares[currentSquare].title;
            return currentState;
        }

        /* ****    ADAPTER    **** */

        // The rules of the model apply to the adapter
        // The adapter will include the playerId whose turn it is in the state of the system
        // One of the actions of the adapter is to end the turn of the player, the end state then advances the playerId whose turn it is
        //

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

        // * *************************************

        // Interface method for Adapter
        public string AdapterTransition(string startState, string action)
        {

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
