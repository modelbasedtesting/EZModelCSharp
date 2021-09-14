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

        // array of player pieces?
        // - square # to say which square the player is on?

        enum SquareType
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

        enum ColorGroup
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

        struct GameSquare
        {
            uint location;
            string title;
            SquareType squareType;
            ColorGroup colorGroup;
            uint ownerId;
            bool isMortgaged;
            uint setSize; // 2, 3, or even 4 for railroads
            uint numHouses; // 0 - 5, 5 == hotel
            uint price;
            uint baseRent; // rent when nothing built on

            public GameSquare( uint loc, string t, SquareType sT, ColorGroup cG, uint sSize, uint p, uint bR)
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

        struct Player
        {
            int money; // Player is out of game when this goes negative.
            uint location; // 40 means you are in jail.
            bool[] possessions; // each element is a location value for a property
            bool CommunityChestGoojf;
            bool ChanceGoojf;
            uint numberOfDoubles; // zero initially, set to zero when non-doubles are rolled, or after going to jail.  Go to jail when this hits 3.  Also zero when exiting jail on doubles.
            bool activelyBidding;

            public Player(int initialMoney = 1500)
            {
                money = initialMoney;
                location = 0; // Go
                possessions = new bool[41]; // initial value is false by default
                CommunityChestGoojf = false;
                ChanceGoojf = false;
                numberOfDoubles = 0;
                activelyBidding = false;
            }
        }

        Player Player1;

        // State Values for the 40 squares on the board + the in Jail pseudo-square
        GameSquare[] gameSquares = {
            new GameSquare( 0, "Go", SquareType.Go, ColorGroup.None, 0, 0, 0),
            new GameSquare( 1, "Mediterannean Ave", SquareType.ColorGroupMember, ColorGroup.Purple, 2, 60, 2),
            new GameSquare( 2, "Community Chest 1", SquareType.CommunityChest, ColorGroup.None, 0, 0, 0),
            new GameSquare( 3, "Baltic Ave", SquareType.ColorGroupMember, ColorGroup.Purple, 2, 60, 4),
            new GameSquare( 4, "Income Tax", SquareType.Tax, ColorGroup.None, 0, 0, 0),
            new GameSquare( 5, "Reading Railroad", SquareType.RailRoad, ColorGroup.Black, 4, 200, 0),
            new GameSquare( 6, "Oriental Ave", SquareType.ColorGroupMember, ColorGroup.Sky, 3, 100, 6),
            new GameSquare( 7, "Chance 1", SquareType.Chance, ColorGroup.None, 0, 0, 0),
            new GameSquare( 8, "Vermont Ave", SquareType.ColorGroupMember, ColorGroup.Sky, 3, 100, 6),
            new GameSquare( 9, "Connecticut Ave", SquareType.ColorGroupMember, ColorGroup.Sky, 3, 120, 8),
            new GameSquare(10, "Just Visiting", SquareType.JustVisiting, ColorGroup.None, 0, 0, 0),
            new GameSquare(11, "St. Charles Place", SquareType.ColorGroupMember, ColorGroup.Magenta, 3, 140, 10),
            new GameSquare(12, "Electric Co.", SquareType.Utility, ColorGroup.White, 2, 150, 0),
            new GameSquare(13, "States Ave", SquareType.ColorGroupMember, ColorGroup.Magenta, 3, 140, 10),
            new GameSquare(14, "Virginia Ave", SquareType.ColorGroupMember, ColorGroup.Magenta, 3, 160, 12),
            new GameSquare(15, "Pennsylvania Railroad", SquareType.RailRoad, ColorGroup.Black, 4, 200, 0),
            new GameSquare(16, "St. James Place", SquareType.ColorGroupMember, ColorGroup.Orange, 3, 180, 14),
            new GameSquare(17, "Community Chest 2", SquareType.CommunityChest, ColorGroup.None, 0, 0, 0),
            new GameSquare(18, "Tennessee Ave", SquareType.ColorGroupMember, ColorGroup.Orange, 3, 180, 14),
            new GameSquare(19, "New York Ave", SquareType.ColorGroupMember, ColorGroup.Orange, 3, 200, 16),
            new GameSquare(20, "Free Parking", SquareType.FreeParking, ColorGroup.None, 0, 0, 0),
            new GameSquare(21, "Kentucky Ave", SquareType.ColorGroupMember, ColorGroup.Red, 3, 220, 18),
            new GameSquare(22, "Chance 2", SquareType.Chance, ColorGroup.None, 0, 0, 0),
            new GameSquare(23, "Indiana Ave", SquareType.ColorGroupMember, ColorGroup.Red, 3, 220, 18),
            new GameSquare(24, "Illinois Ave", SquareType.ColorGroupMember, ColorGroup.Red, 3, 240, 20),
            new GameSquare(25, "B & O Railroad", SquareType.RailRoad, ColorGroup.Black, 4, 200, 0),
            new GameSquare(26, "Atlantic Ave", SquareType.ColorGroupMember, ColorGroup.Yellow, 3, 260, 22),
            new GameSquare(27, "Ventnor Ave", SquareType.ColorGroupMember, ColorGroup.Yellow, 3, 260, 22),
            new GameSquare(28, "Water Works", SquareType.Utility, ColorGroup.White, 2, 150, 0),
            new GameSquare(29, "Marvin Gardens", SquareType.ColorGroupMember, ColorGroup.Yellow, 3, 280, 24),
            new GameSquare(30, "Go to Jail", SquareType.GoToJail, ColorGroup.None, 0, 0, 0),
            new GameSquare(31, "Pacific Ave", SquareType.ColorGroupMember, ColorGroup.Green, 3, 300, 26),
            new GameSquare(32, "North Carolina Ave", SquareType.ColorGroupMember, ColorGroup.Green, 3, 300, 26),
            new GameSquare(33, "Community Chest 3", SquareType.CommunityChest, ColorGroup.None, 0, 0, 0),
            new GameSquare(34, "Pennsylvania Ave", SquareType.ColorGroupMember, ColorGroup.Green, 3, 320, 28),
            new GameSquare(35, "Short Line Railroad", SquareType.RailRoad, ColorGroup.Black, 4, 200, 0),
            new GameSquare(36, "Chance 3", SquareType.Chance, ColorGroup.None, 0, 0, 0),
            new GameSquare(37, "Park Place", SquareType.ColorGroupMember, ColorGroup.NavyBlue, 2, 350, 35),
            new GameSquare(38, "Luxury Tax", SquareType.Tax, ColorGroup.None, 0, 0, 75),
            new GameSquare(39, "Board Walk", SquareType.ColorGroupMember, ColorGroup.NavyBlue, 2, 400, 50),
            new GameSquare(40, "In Jail", SquareType.InJail, ColorGroup.None, 0, 0, 0)
        };

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

        // Buying property
        // - track who owns a property: 0 == the bank, 1, 2, ... N = a player
        //   - keep track of which player we are playing? (1, 2, ... N)
        // - when the owner is 0 for the square you are on, you have the option to buy
        //   - if you decline to purchase or if you cannot come up with the face value to buy the property, then
        //     the property goes to auction by the bank
        //     - starting bid is $1
        //       - highest bidder receives the property

        // Paying rent on owned property

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
                    currentState = 40; // In Jail
                    break;

                case goToJustVisiting:
                    currentState = 10; // Just visiting
                    break;

                case roll2:
                    currentState = (currentState + 2) % 40;
                    break;

                case roll3:
                    currentState = (currentState + 3) % 40;
                    break;

                case roll4:
                    currentState = (currentState + 4) % 40;
                    break;

                case roll5:
                    currentState = (currentState + 5) % 40;
                    break;

                case roll6:
                    currentState = (currentState + 6) % 40;
                    break;

                case roll7:
                    currentState = (currentState + 7) % 40;
                    break;

                case roll8:
                    currentState = (currentState + 8) % 40;
                    break;

                case roll9:
                    currentState = (currentState + 9) % 40;
                    break;

                case roll10:
                    currentState = (currentState + 10) % 40;
                    break;

                case roll11:
                    currentState = (currentState + 11) % 40;
                    break;

                case roll12:
                    currentState = (currentState + 12) % 40;
                    break;

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

            return currentState.ToString();
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

        // For game simulation we need a player turn procedure
        // The side effect of a player taking a turn is to modify the state of the board.
        //  - a player may indirectly affect the information of other players according to the sell (and buy) and bid rules.

        // Interface method for Adapter
        public string AdapterTransition(string startState, string action)
        {
            // For Monopoly, this adapter is a simulation of game play with 1 or more players.

            string expected = GetEndState(startState, action);

            // affect the player state

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
