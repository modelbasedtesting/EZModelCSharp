﻿// EZmodel State Graph Generation
// copyright 2021 Serious Quality LLC

// Model the classic board game Monopoly
// Step 4: model the bank, and introduce adapter to buy properties that are unowned

using System;
using System.Collections.Generic;
using SeriousQualityEzModel;
using System.Linq;

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

            EzModelGraph graph = new EzModelGraph(client, 1100, 110, 20, EzModelGraph.LayoutRankDirection.LeftRight);

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
                this.ownerId = 0;    // show in state
                this.numHouses = 0;  // show in state
            }

            public uint CurrentRent(bool ArrivedByChanceCard = false)
            {
                // TODO: compute current rent, which is a function of
                //  - $0 for squareTypes Chance, Community Chest, Go to Jail,
                //    Free Parking, Just Visiting, Go, Tax, In Jail
                //  - if ownerID == 0 or the current player -> $0
                //  - isMortgaged -> $0
                //  - squareType: special formula for RailRoad / Utility, and
                //    if ArrivedByChanceCard then the calculation is 10x for
                //    the utility, or double for the railroad
                //  - if numHouses > 0 then rent is from a table for house rent
                //  - if owner ID is same for all members of the color group,
                //    then rent is double baseRent
                //  - finally, pay baseRent

                return 10 * location;
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

        GameSquare findGameSquareFromTitle(string squareTitle)
        {
            for (uint i = 0; i < gameSquares.Length; i++)
            {
                if (gameSquares[i].title == squareTitle)
                {
                    return gameSquares[i];
                }
            }
            // Error: squareTitle not matched
            // TODO: throw an exception
            return gameSquares[0]; // Go, for now.
        }

        // Interface method for model creation
        public List<string> GetAvailableActions(string startState)
        {
            List<string> actions = new List<string>();

            GameSquare currentSquare = findGameSquareFromTitle(startState);

            if (currentSquare.squareType == SquareType.InJail)
            {
                actions.Add(goToJustVisiting);
            }
            else if (currentSquare.squareType == SquareType.GoToJail)
            {
                actions.Add(goToJail);
            }
            else if (currentSquare.squareType == SquareType.CommunityChest)
            {
                actions.Add(advanceToGo);
            }
            else if (currentSquare.squareType == SquareType.Chance)
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
            GameSquare startSquare = findGameSquareFromTitle(startState);
            GameSquare currentSquare = startSquare;
            modelStep++;

            if (action.StartsWith("Move_"))
            {
                uint moveAmount = uint.Parse(action.Substring(5));
                if (currentSquare.location + moveAmount >= gameSquares.Length - 1)
                {
                    CollectGoMoney(1); // TODO: change literal 1 to playerId when multi-player
                }
                currentSquare = gameSquares[(currentSquare.location + moveAmount) % 40];
            }
            else
            {
                switch (action)
                {
                    case goToJail:
                        currentSquare = getGameSquare(SquareType.InJail);
                        break;

                    case goToJustVisiting:
                        currentSquare = getGameSquare(SquareType.JustVisiting);
                        break;

                    case advanceToGo:
                        currentSquare = getGameSquare(SquareType.Go);
                        CollectGoMoney(1); // TODO: change literal 1 to playerId when multi-player
                        break;

                    case goBack3:
                        if (currentSquare.location < 3)
                        {
                            // This won't happen on an off-the-shelf Monopoly board, but just in case:
                            // There is an extra pseudosquare for InJail, so subtract 2 from the
                            // gameSquares array length instead of 3.
                            currentSquare = gameSquares[currentSquare.location + gameSquares.Length - 1 - 3];
                        }
                        else
                        {
                            currentSquare = gameSquares[currentSquare.location - 3];
                        }
                        break;

                    default:
                        Console.WriteLine("ERROR: unknown action '{0}' in UserRules.GetEndState()", action);
                        return startState;
                }
            }

            Console.WriteLine("Step {3}: Player {4} {0} at {1}, landed on {2}", action, startSquare.title, currentSquare.title, modelStep, 1); // TODO: replace literal 1 with playerId.

            ReapConsequences(currentSquare, 1); // TODO: replace literal 1 with playerId.

            return currentSquare.title;
        }

        GameSquare getGameSquare(SquareType sT)
        {
            foreach( GameSquare gs in gameSquares)
            {
                if (gs.squareType == sT)
                {
                    return gs;
                }
            }

            // TODO: throw an exception - gamesquare of type sT not in gameSquares.
            return gameSquares[0];
        }

        void CollectGoMoney(uint playerId)
        {
            players[1].money += 200;
            Console.WriteLine("Player {0} collected 200 for landing on or passing Go, now has {1}", playerId, players[1].money);
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
            Console.WriteLine("{0} properties owned by player {1}.", quantity, playerId);
        }

        void ReapConsequences(GameSquare currentSquare, uint playerId)
        {
            uint cost = 0;
            int playerHad = players[playerId].money;

            if (currentSquare.colorGroup == ColorGroup.None)
            {
                switch (currentSquare.location)
                {
                    // Income Tax - $200 or 10% of cash
                    case 4:
                        uint incomeTax = (uint)Math.Round(players[playerId].money * 0.1);
                        PayOwner(incomeTax, playerId, 0); // Pay the bank
                        Console.WriteLine("Player {0} pays {1} income tax, now has {2}", playerId, incomeTax, players[playerId].money);
                        break;
                    // Luxury Tax - $75
                    case 38:
                        PayOwner(75, playerId, 0); // Pay the bank
                        Console.WriteLine("Player {0} pays 75 luxury tax, now has {1}", playerId, players[playerId].money);
                        break;
                }
            }
            else
            {
                if (currentSquare.ownerId == 0)
                {
                    // chance to buy
                    if (players[playerId].money >= currentSquare.price)
                    {
                        gameSquares[currentSquare.location].ownerId = playerId;
                        players[playerId].money -= (int)currentSquare.price;
                        Console.WriteLine("Player {0} had {4}, purchased {1} for {2}, now has {3}", playerId, currentSquare.title, currentSquare.price, players[playerId].money, playerHad);
                        ListSquaresOwnedBy(playerId);
                    }
                    else
                    {
                        // decline to buy.  TODO: auction
                        Console.WriteLine("Player {0} has {2}, declines to purchase {1} for {3}", playerId, currentSquare.title, players[playerId].money, currentSquare.price);
                    }
                    return;
                }
                else if (currentSquare.ownerId == playerId)
                {
                    return;
                }
                // possibilities:
                // square is owned but mortgaged - free ride
                // square is owned no houses - pay rent
                // square is owned and has a multiplier effect (multiple utilities, multiple railroads, full colorgroup) on rent
                // square is owned and improved (houses / hotel)
                else if (currentSquare.colorGroup == ColorGroup.White && !currentSquare.isMortgaged)
                {
                    // Utilities are the pseudo-color group White
                    // TODO:
                    // pay 4x dice roll when single ownership
                    // pay 10x dice roll when both utilities are owned by one player
                    // (For now, pay 25)
                    cost = 25;
                    PayOwner(cost, playerId, currentSquare.ownerId);
                }
                else if (currentSquare.colorGroup == ColorGroup.Black && !currentSquare.isMortgaged)
                {
                    // Railroads are the pseudo-color group Black
                    // pay 25 when single ownership
                    // TODO:
                    // pay 50, 100, or 200 for double, triple, or quadruple ownership
                    // (For now, pay 25)
                    cost = 25;
                    PayOwner(cost, playerId, currentSquare.ownerId);
                }
                else if (currentSquare.colorGroup != ColorGroup.None && !currentSquare.isMortgaged)
                {
                    // this is a property that can collect rent
                    cost = currentSquare.baseRent;
                    PayOwner(cost, playerId, currentSquare.ownerId);
                }

                if (cost > 0)
                {
                    Console.WriteLine("Player {0} had {4}, has {1} after {2} rent for {3}", playerId, players[playerId].money, cost, currentSquare.title, playerHad);
                }
            }
        }

        void PayOwner(uint cost, uint payerId, uint ownerId)
        {
            players[payerId].money -= (int)cost;
            players[ownerId].money += (int)cost;
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

    public class Player
    {
        uint playerId;
        public int money; // Player is out of game when this goes negative.
        uint location = 0; // 40 means you are in jail.

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
