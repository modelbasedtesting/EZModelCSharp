﻿// EZmodel State Graph Generation
// copyright 2021 Serious Quality LLC

// Model the classic board game Monopoly
// Step 3: model community chest and chance cards

using System;
using System.Collections.Generic;
using SeriousQualityEzModel;
using System.Linq;

namespace ChanceAndCommChest
{
    class ChanceAndCommChestProgram
    {
        public static void Main()
        {
            Monopoly client = new Monopoly()
            {
                SelfLinkTreatment = SelfLinkTreatmentChoice.AllowAll
            };

            EzModelGraph graph = new EzModelGraph(client, 1100, 110, 20, EzModelGraph.LayoutRankDirection.LeftRight);

            if (graph.GenerateGraph())
            {
                graph.DisplayStateTable(); // Display the Excel-format state table

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

        // Stave variables
        public uint svSquare = 0; // Index into the Square array indicating board position.

        public enum SquareType
        {
            Chance,
            CommunityChest,
            GoToJail,
            FreeParking,
            JustVisiting,
            Go,
            Tax,
            RailRoad,
            Utility,
            ColorGroupMember,
            InJail
        }

        public enum ColorGroup
        {
            None = 0, // Just Visiting, Free Parking, Go to Jail, Go, Luxury Tax, Income Tax, Chance, Community Chest
            Purple,
            Sky,
            Magenta,
            Orange,
            Red,
            Yellow,
            Green,
            NavyBlue,
            Black, // railroads
            White // utilities
        }

        public struct GameSquare
        {
            public uint location; // State
            public string title; // constant
            public SquareType squareType; // constant
            public ColorGroup colorGroup; // constant
            public uint ownerId; // variable
            public bool isMortgaged; // variable
            public uint setSize; // constant - 2, 3, or even 4 for railroads
            public uint numHouses; // 0 - 5, 5 == hotel
            public uint price; // constant
            public uint baseRent; // constant - rent when nothing built on

            public GameSquare(uint loc, string t, SquareType sT, ColorGroup cG, uint sSize, uint p, uint bR)
            {
                this.location = loc;
                this.title = t;
                this.squareType = sT;
                this.colorGroup = cG;
                this.setSize = sSize;
                this.price = p;
                this.baseRent = bR;

                this.isMortgaged = false;
                this.ownerId = 0;
                this.numHouses = 0;
            }
        }

        // State Values for the 40 squares on the board + the in Jail pseudo-square
        public GameSquare[] gameSquares = {
    new GameSquare( 0, "Go [0]", SquareType.Go, ColorGroup.None, 0, 0, 0),
    new GameSquare( 1, "Mediterannean Ave [1]", SquareType.ColorGroupMember, ColorGroup.Purple, 2, 60, 2),
    new GameSquare( 2, "Community Chest [2]", SquareType.CommunityChest, ColorGroup.None, 0, 0, 0),
    new GameSquare( 3, "Baltic Ave [3]", SquareType.ColorGroupMember, ColorGroup.Purple, 2, 60, 4),
    new GameSquare( 4, "Income Tax [4]", SquareType.Tax, ColorGroup.None, 0, 0, 0),
    new GameSquare( 5, "Reading Railroad [5]", SquareType.RailRoad, ColorGroup.Black, 4, 200, 0),
    new GameSquare( 6, "Oriental Ave [6]", SquareType.ColorGroupMember, ColorGroup.Sky, 3, 100, 6),
    new GameSquare( 7, "Chance [7]", SquareType.Chance, ColorGroup.None, 0, 0, 0),
    new GameSquare( 8, "Vermont Ave [8]", SquareType.ColorGroupMember, ColorGroup.Sky, 3, 100, 6),
    new GameSquare( 9, "Connecticut Ave [9]", SquareType.ColorGroupMember, ColorGroup.Sky, 3, 120, 8),
    new GameSquare(10, "Just Visiting [10]", SquareType.JustVisiting, ColorGroup.None, 0, 0, 0),
    new GameSquare(11, "St. Charles Place [11]", SquareType.ColorGroupMember, ColorGroup.Magenta, 3, 140, 10),
    new GameSquare(12, "Electric Co. [12]", SquareType.Utility, ColorGroup.White, 2, 150, 0),
    new GameSquare(13, "States Ave [13]", SquareType.ColorGroupMember, ColorGroup.Magenta, 3, 140, 10),
    new GameSquare(14, "Virginia Ave [14]", SquareType.ColorGroupMember, ColorGroup.Magenta, 3, 160, 12),
    new GameSquare(15, "Pennsylvania Railroad [15]", SquareType.RailRoad, ColorGroup.Black, 4, 200, 0),
    new GameSquare(16, "St. James Place [16]", SquareType.ColorGroupMember, ColorGroup.Orange, 3, 180, 14),
    new GameSquare(17, "Community Chest [17]", SquareType.CommunityChest, ColorGroup.None, 0, 0, 0),
    new GameSquare(18, "Tennessee Ave [18]", SquareType.ColorGroupMember, ColorGroup.Orange, 3, 180, 14),
    new GameSquare(19, "New York Ave [19]", SquareType.ColorGroupMember, ColorGroup.Orange, 3, 200, 16),
    new GameSquare(20, "Free Parking [20]", SquareType.FreeParking, ColorGroup.None, 0, 0, 0),
    new GameSquare(21, "Kentucky Ave [21]", SquareType.ColorGroupMember, ColorGroup.Red, 3, 220, 18),
    new GameSquare(22, "Chance [22]", SquareType.Chance, ColorGroup.None, 0, 0, 0),
    new GameSquare(23, "Indiana Ave [23]", SquareType.ColorGroupMember, ColorGroup.Red, 3, 220, 18),
    new GameSquare(24, "Illinois Ave [24]", SquareType.ColorGroupMember, ColorGroup.Red, 3, 240, 20),
    new GameSquare(25, "B & O Railroad [25]", SquareType.RailRoad, ColorGroup.Black, 4, 200, 0),
    new GameSquare(26, "Atlantic Ave [26]", SquareType.ColorGroupMember, ColorGroup.Yellow, 3, 260, 22),
    new GameSquare(27, "Ventnor Ave [27]", SquareType.ColorGroupMember, ColorGroup.Yellow, 3, 260, 22),
    new GameSquare(28, "Water Works [28]", SquareType.Utility, ColorGroup.White, 2, 150, 0),
    new GameSquare(29, "Marvin Gardens [29]", SquareType.ColorGroupMember, ColorGroup.Yellow, 3, 280, 24),
    new GameSquare(30, "Go to Jail [30]", SquareType.GoToJail, ColorGroup.None, 0, 0, 0),
    new GameSquare(31, "Pacific Ave [31]", SquareType.ColorGroupMember, ColorGroup.Green, 3, 300, 26),
    new GameSquare(32, "North Carolina Ave [32]", SquareType.ColorGroupMember, ColorGroup.Green, 3, 300, 26),
    new GameSquare(33, "Community Chest [33]", SquareType.CommunityChest, ColorGroup.None, 0, 0, 0),
    new GameSquare(34, "Pennsylvania Ave [34]", SquareType.ColorGroupMember, ColorGroup.Green, 3, 320, 28),
    new GameSquare(35, "Short Line Railroad [35]", SquareType.RailRoad, ColorGroup.Black, 4, 200, 0),
    new GameSquare(36, "Chance [36]", SquareType.Chance, ColorGroup.None, 0, 0, 0),
    new GameSquare(37, "Park Place [37]", SquareType.ColorGroupMember, ColorGroup.NavyBlue, 2, 350, 35),
    new GameSquare(38, "Luxury Tax [38]", SquareType.Tax, ColorGroup.None, 0, 0, 75),
    new GameSquare(39, "Board Walk [39]", SquareType.ColorGroupMember, ColorGroup.NavyBlue, 2, 400, 50),
    new GameSquare(40, "In Jail [40 pseudosquare]", SquareType.InJail, ColorGroup.None, 0, 0, 0)
};

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

        // Evaluation actions
        const string goToJail = "Go to Jail";
        const string goToJustVisiting = "Go to Just Visiting";

        // Chance / Community chest card actions
        const string advanceToGo = "Advance to Go";

        // Chance card actions
        const string goBack3 = "Go back three spaces";

        // Community Chest card actions

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
            uint[] communityChest = { 2, 17, 33 };
            uint[] chance = { 7, 22, 36 };

            if (currentSquare == 40)
            {
                actions.Add(goToJustVisiting);
            }
            else if (currentSquare == 30)
            {
                actions.Add(goToJail);
            }
            else if (communityChest.Contains(currentSquare))
            {
                actions.Add(advanceToGo);
            }
            else if (chance.Contains(currentSquare))
            {
                actions.Add(goBack3);
            }
            else
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
            uint currentSquare = findGameSquareFromTitle(startState);

            if (action.StartsWith("Move_"))
            {
                uint moveAmount = uint.Parse(action.Substring(5));
                currentSquare = (currentSquare + moveAmount) % 40;
            }
            else
            {
                switch (action)
                {
                    case goToJail:
                        currentSquare = 40; // In Jail
                        break;

                    case goToJustVisiting:
                        currentSquare = 10; // Just visiting
                        break;

                    case advanceToGo:
                        currentSquare = 0;
                        break;

                    case goBack3:
                        if (currentSquare < 3)
                        {
                            // This won't happen on an off-the-shelf Monopoly board
                            currentSquare += 40;
                        }
                        currentSquare -= 3;
                        break;
                       
                    default:
                        Console.WriteLine("ERROR: unknown action '{0}' in UserRules.GetEndState()", action);
                        return startState;
                }
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
