// EZmodel State Graph Generation
// copyright 2021 Serious Quality LLC

// Model the classic board game Monopoly
// Step 3: model the bank, and introduce adapter to buy properties that are unowned

using System;
using System.Collections.Generic;
using SeriousQualityEzModel;

namespace Banking
{
    class BankingProgram
    {
        public static void Main()
        {
            Monopoly client = new Monopoly(1500)
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

        // Evaluation actions
        const string goToJail = "Go to Jail";
        const string goToJustVisiting = "Go to Just Visiting";

        Player[] players;
        int modelStep = 0;

        // Buying property
        // - track who owns a property: 0 == the bank, 1, 2, ... N = a player
        //   - keep track of which player we are playing? (1, 2, ... N)
        // - when the owner is 0 for the square you are on, you have the option to buy
        //   - if you decline to purchase or if you cannot come up with the face value to buy the property, then
        //     the property goes to auction by the bank
        //     - starting bid is $1
        //       - highest bidder receives the property

        public Monopoly(int InitialMoney)
        {
            players = new Player[2]; // Player 1 plus the bank.

            for (uint i = 0; i < players.Length; i++)
            {
                players[i] = new Player();
                players[i].GameSetup(i + 1);
            }

            svSquare = 0;
        }

        public bool CanBuySquare(uint square)
        {
            return gameSquares[square].ownerId == 0;
        }

        public bool ChooseToPurchase()
        {
            // TODO: make a choice between true and false by some criteria.
            return true;
        }

        public void PlayerBuyOrAuctionSquare(uint playerId, uint square)
        {
            if (players[playerId].CanAffordSquare(gameSquares[square].price) && ChooseToPurchase())
            {
                // TODO: player can decline to buy, sending the square to auction
                players[playerId].BuySquare(gameSquares[square].price);
                gameSquares[square].ownerId = playerId;
                return;
            }

            // Players other than playerId get to bid.
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

            if (currentSquare == 40)
            {
                actions.Add(goToJustVisiting);
            }

            if (currentSquare == 30)
            {
                actions.Add(goToJail);
            }

            if (currentSquare != 30 && currentSquare != 40)
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

            switch (action)
            {
                case goToJail:
                    currentSquare = 40; // In Jail
                    break;

                case goToJustVisiting:
                    currentSquare = 10; // Just visiting
                    break;

                case roll2:
                    currentSquare = (currentSquare + 2) % 40;
                    break;

                case roll3:
                    currentSquare = (currentSquare + 3) % 40;
                    break;

                case roll4:
                    currentSquare = (currentSquare + 4) % 40;
                    break;

                case roll5:
                    currentSquare = (currentSquare + 5) % 40;
                    break;

                case roll6:
                    currentSquare = (currentSquare + 6) % 40;
                    break;

                case roll7:
                    currentSquare = (currentSquare + 7) % 40;
                    break;

                case roll8:
                    currentSquare = (currentSquare + 8) % 40;
                    break;

                case roll9:
                    currentSquare = (currentSquare + 9) % 40;
                    break;

                case roll10:
                    currentSquare = (currentSquare + 10) % 40;
                    break;

                case roll11:
                    currentSquare = (currentSquare + 11) % 40;
                    break;

                case roll12:
                    currentSquare = (currentSquare + 12) % 40;
                    break;

                default:
                    Console.WriteLine("ERROR: unknown action '{0}' in UserRules.GetEndState()", action);
                    return startState;
            }

            string currentState = gameSquares[currentSquare].title;
            return currentState;
        }

        public List<string> AdapterGetAvailableActions(string startState)
        {
            List<string> actions = new List<string>();

            uint currentSquare = findGameSquareFromTitle(startState);

            if (currentSquare == 40)
            {
                actions.Add(goToJustVisiting);
            }

            if (currentSquare == 30)
            {
                actions.Add(goToJail);
            }

            if (currentSquare != 30 && currentSquare != 40)
            {

                actions.Add(roll2);
                actions.Add(roll3);
                /*  Control travel around board to 2 or 3 squares at a time
                        actions.Add(roll4);
                        actions.Add(roll5);
                        actions.Add(roll6);
                        actions.Add(roll7);
                        actions.Add(roll8);
                        actions.Add(roll9);
                        actions.Add(roll10);
                        actions.Add(roll11);
                        actions.Add(roll12); */
            }

            return actions;
        }

        // Interface method for model creation
        public string AdapterGetEndState(string startState, string action)
        {
            uint startSquare = findGameSquareFromTitle(startState);
            uint currentSquare = startSquare;
            modelStep++;

            switch (action)
            {
                case goToJail:
                    currentSquare = 40; // In Jail
                    break;

                case goToJustVisiting:
                    currentSquare = 10; // Just visiting
                    break;

                case roll2:
                    currentSquare = (startSquare + 2) % 40;
                    break;

                case roll3:
                    currentSquare = (startSquare + 3) % 40;
                    break;

                case roll4:
                    currentSquare = (startSquare + 4) % 40;
                    break;

                case roll5:
                    currentSquare = (startSquare + 5) % 40;
                    break;

                case roll6:
                    currentSquare = (startSquare + 6) % 40;
                    break;

                case roll7:
                    currentSquare = (startSquare + 7) % 40;
                    break;

                case roll8:
                    currentSquare = (startSquare + 8) % 40;
                    break;

                case roll9:
                    currentSquare = (startSquare + 9) % 40;
                    break;

                case roll10:
                    currentSquare = (startSquare + 10) % 40;
                    break;

                case roll11:
                    currentSquare = (startSquare + 11) % 40;
                    break;

                case roll12:
                    currentSquare = (startSquare + 12) % 40;
                    break;

                default:
                    Console.WriteLine("ERROR: unknown action '{0}' in UserRules.GetEndState()", action);
                    return startState;
            }

            Console.WriteLine("Step {5}: {0} at square {1} {2}, landed on square {3} {4}", action, startSquare, gameSquares[startSquare].title, currentSquare, gameSquares[currentSquare].title, modelStep);

            if (currentSquare < startSquare)
            {
                players[1].money += 200;
                Console.WriteLine("Player collected 200 for landing on or passing Go, now has {0}", players[1].money);
            }

            ReapConsequences(currentSquare);

            string currentState = gameSquares[currentSquare].title;
            return currentState;
        }

        void ListSquaresOwnedBy(uint playerId)
        {
            int quantity = 0;
            for (uint i = 0; i < 40; i++)
            {
                if (gameSquares[i].ownerId == playerId)
                {
                    Console.WriteLine("    {0}", gameSquares[i].title);
                    quantity++;
                }
            }
            Console.WriteLine("{0} squares owned by player {1}.", quantity, playerId);
        }

        void ReapConsequences(uint currentSquare)
        {
            uint cost = 0;

            if (gameSquares[currentSquare].colorGroup == ColorGroup.None)
            {
                switch (currentSquare)
                {
                    // Income Tax - $200 or 10% of cash
                    case 4:
                        uint incomeTax = (uint)Math.Round(players[1].money * 0.1);
                        PayOwner(incomeTax, 1, 0); // Pay the bank
                        Console.WriteLine("Player pays {0} income tax, now has {1}", incomeTax, players[1].money);
                        break;
                    // Luxury Tax - $75
                    case 38:
                        PayOwner(75, 1, 0); // Pay the bank
                        Console.WriteLine("Player pays 75 luxury tax, now has {0}", players[1].money);
                        break;
                }
            }
            else
            {
                uint squareOwner = gameSquares[currentSquare].ownerId;

                if (squareOwner == 0)
                {
                    // chance to buy
                    if (players[1].money >= gameSquares[currentSquare].price)
                    {
                        gameSquares[currentSquare].ownerId = 1; // TODO: generalize for multi player
                        players[1].money -= (int)gameSquares[currentSquare].price;
                        Console.WriteLine("Player purchased square {0} {1} for {2}, now has {3}", currentSquare, gameSquares[currentSquare].title, gameSquares[currentSquare].price, players[1].money);
                        ListSquaresOwnedBy(1);
                    }
                    else
                    {
                        // decline to buy.  TODO: auction
                        Console.WriteLine("Player declines to purchase square {0} {1}", currentSquare, gameSquares[currentSquare].title);
                    }
                    return;
                }
                else if (squareOwner == 1)
                {
                    return;
                }
                // possibilities:
                // square is owned but mortgaged - free ride
                // square is owned no houses - pay rent
                // square is owned and has a multiplier effect (multiple utilities, multiple railroads, full colorgroup) on rent
                // square is owned and improved (houses / hotel)
                else if (gameSquares[currentSquare].colorGroup == ColorGroup.White)
                {
                    // Utilities are the pseudo-color group White
                    // pay 4x dice roll when single ownership
                    // pay 10x dice roll when both utilities are owned by one player
                    // for now, pay 25
                    cost = 25;
                    PayOwner(cost, 1, gameSquares[currentSquare].ownerId);
                }
                else if (gameSquares[currentSquare].colorGroup == ColorGroup.Black)
                {
                    // Railroads are the pseudo-color group Black
                    // pay 25 when single ownership
                    // pay 50, 100, or 200 for double, triple, or quadruple ownership
                    // for now, pay 25:
                    cost = 25;
                    PayOwner(cost, 1, gameSquares[currentSquare].ownerId);
                }
                else if (gameSquares[currentSquare].colorGroup != ColorGroup.None)
                {
                    // this is a property that can collect rent
                    cost = gameSquares[currentSquare].baseRent;
                    PayOwner(cost, 1, gameSquares[currentSquare].ownerId);
                }

                if (cost > 0)
                {
                    Console.WriteLine("Player has {0} after {1} rent for square {2} {3}", players[1].money, cost, currentSquare, gameSquares[currentSquare].title);
                }
            }
        }

        void PayOwner(uint cost, uint payerId, uint ownerId)
        {
            players[payerId].money -= (int)cost;
            // TODO: activate next line in real game
            //            players[ownerId].money += (int)cost;
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

        // For game simulation we need a player turn procedure
        // The side effect of a player taking a turn is to modify the state of the board.
        //  - a player may indirectly affect the information of other players according to the sell (and buy) and bid rules.

        //************ Player Simulation ****************************

        public bool IsSquareBuyable(uint square)
        {
            GameSquare s = gameSquares[square];
            if (s.squareType == SquareType.ColorGroupMember && s.ownerId == 0)
            {
                return true;
            }
            return false;
        }


        // * *************************************

        // Interface method for Adapter
        public string AdapterTransition(string startState, string action)
        {
            // For Monopoly, this adapter is a simulation of game play with 1 or more players.

            string expected = AdapterGetEndState(startState, action);

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

    public class Player
    {
        uint playerId;
        public int money; // Player is out of game when this goes negative.
        uint location = 0; // 40 means you are in jail.
                            //        bool[] possessions = new bool[41]; // each element is a location value for a property
        bool CommunityChestGoojf = false;  // Goojf == Get out of jail free
        bool ChanceGoojf = false;
        uint numberOfDoubles = 0; // zero initially, set to zero when non-doubles are rolled, or after going to jail.  Go to jail when this hits 3.  Also zero when exiting jail on doubles.
        bool activelyBidding = false;

        public Player()
        {
        }

        public void GameSetup(uint playerId, int money = 1500)
        {
            this.playerId = playerId;
            this.money = money;
        }

        public bool CanAffordSquare(uint price)
        {
            if (price <= money)
            {
                return true;
            }
            return false;
        }

        public void BuySquare(uint price)
        {
            money -= (int)price;
        }
    }

}
