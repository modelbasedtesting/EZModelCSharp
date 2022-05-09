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
        static int Main()
        {
            Monopoly client = new ()
            {
                SelfLinkTreatment = SelfLinkTreatmentChoice.AllowAll
            };

            EzModelGraph graph = new (client, 1100, 110, 14, EzModelGraph.LayoutRankDirection.LeftRight);

            if (!graph.GenerateGraph())
            {
                Console.WriteLine("Unable to generate the graph");
                return -1;
            }

            List<String> report = graph.AnalyzeConnectivity();
            if (report.Count > 0)
            {
                Console.WriteLine("The graph is not strongly connected.");
                Console.WriteLine("problems report:");
                foreach (string S in report)
                {
                    Console.WriteLine(S);
                }
                return -2;
            }

            List<string> duplicateActions = graph.ReportDuplicateOutlinks();
            if (duplicateActions.Count > 0)
            {
                Console.WriteLine("There are duplicate outlinks in the graph.");
                foreach (string S in duplicateActions)
                {
                    Console.WriteLine(S);
                }
            }

            graph.DisplayStateTable(); // Display the Excel-format state table

            graph.CreateGraphVizFileAndImage(EzModelGraph.GraphShape.Default);

            client.NotifyAdapter = false;
            // If you want stopOnProblem to stop, you need to return false from the AreStatesAcceptablySimilar method
            client.StopOnProblem = false;

            graph.RandomDestinationCoverage("Monopoly", 1);
            return 0;
        }
    }

    public partial class Monopoly : IEzModelClient
    {
        readonly Dictionary<string, string> state;

        // State variable names
        const string GAME_SQUARE = "GameSquare";

        readonly List<string> stateVariableList = new()
        {
            GAME_SQUARE
        };

        readonly Dictionary<string, int> actions;
        readonly Dictionary<int, Dictionary<string, string>> statesDict;
        int statesCounter;

        // State Values for the 40 squares on the board + the in Jail pseudo-square
        public string[] gameSquares = {
            "Go [0]",
            "Mediterannean Ave [1]",
            "Community Chest [2]",
            "Baltic Ave [3]",
            "Income Tax [4]",
            "Reading Railroad [5]",
            "Oriental Ave [6]",
            "Chance [7]",
            "Vermont Ave [8]",
            "Connecticut Ave [9]",
            "Just Visiting [10]",
            "St. Charles Place [11]",
            "Electric Co. [12]",
            "States Ave [13]",
            "Virginia Ave [14]",
            "Pennsylvania Railroad [15]",
            "St. James Place [16]",
            "Community Chest [17]",
            "Tennessee Ave [18]",
            "New York Ave [19]",
            "Free Parking [20]",
            "Kentucky Ave [21]",
            "Chance [22]",
            "Indiana Ave [23]",
            "Illinois Ave [24]",
            "B & O Railroad [25]",
            "Atlantic Ave [26]",
            "Ventnor Ave [27]",
            "Water Works [28]",
            "Marvin Gardens [29]",
            "Go to Jail [30]",
            "Pacific Ave [31]",
            "North Carolina Ave [32]",
            "Community Chest [33]",
            "Pennsylvania Ave [34]",
            "Short Line Railroad [35]", 
            "Chance [36]",
            "Park Place [37]",
            "Luxury Tax [38]",
            "Board Walk [39]"
        };

        // Actions
        // Individual dice actions, for graph-building
        const string roll3 = "Move_3";
        const string roll2 = "Move_2";
        const string roll11 = "Move_11";
        const string roll12 = "Move_12";

        public Monopoly()
        {
            state = new Dictionary<string, string>
            {
                [GAME_SQUARE] = gameSquares[0]
            };

            statesDict = new Dictionary<int, Dictionary<string, string>>();

            actions = new Dictionary<string, int>
            {
                [roll2] = 0,
                [roll3] = 1,
                [roll11] = 2,
                [roll12] = 3
            };

            statesCounter = 0;
            statesDict[statesCounter] = new Dictionary<string, string>(state);
        }

        public string StringifyState(int state)
        {
            string stateString = "";

            foreach (string stateVariable in stateVariableList)
            {
                stateString += stateVariable + "." + statesDict[state][stateVariable] + "\n";
            }

            return stateString.Substring(0, stateString.Length - 1);
        }

        int UpdateStates(Dictionary<string, string> state)
        {
            foreach(KeyValuePair<int, Dictionary<string, string>> entry in statesDict)
            {
                if (!entry.Value.Except(state).Any())
                {
                    return entry.Key;
                }
            }
            statesDict[statesCounter] = new Dictionary<string, string>(state);
            statesCounter++;
            return statesCounter - 1;
        }

        // Interface method for model creation
        public int GetInitialState()
        {
            return 0;
        }

        uint findGameSquareFromTitle(string squareTitle)
        {
            for (uint i = 0; i < gameSquares.Length; i++)
            {
                if (gameSquares[i] == squareTitle)
                {
                    return i;
                }
            }
            // Error: squareTitle not matched
            // TODO: throw an exception
            return 42;
        }

        // IEzModelClient Interface method
        public string[] GetActionsList()
        {
            return new string[]
                { roll2,
                roll3,
                roll11,
                roll12 };
        }

        // Interface method for model creation
        public List<int> GetAvailableActions(int CurrentState)
        {
            List<int> actionList = new ();

            actionList.Add(actions[roll2]);
            actionList.Add(actions[roll3]);
            actionList.Add(actions[roll11]);
            actionList.Add(actions[roll12]);

            return actionList;
        }

        // Interface method for model creation
        public int GetEndState(int startState, int action)
        {
            Dictionary<string, string> endState = new(statesDict[startState]);

            uint currentSquare = findGameSquareFromTitle(endState[GAME_SQUARE]);

            if (action == actions[roll2])
            {
                currentSquare = currentSquare + 2 % 40;
            }
            else if (action == actions[roll3])
            {
                currentSquare = currentSquare + 3 % 40;
            }
            else if (action == actions[roll11])
            {
                currentSquare = currentSquare + 11 % 40;
            }
            else if (action == actions[roll12])
            {
                currentSquare = currentSquare + 12 % 40;
            }
            else
            {
                switch (action)
                {
                    default:
                        Console.WriteLine("ERROR: unknown action '{0}' in UserRules.GetEndState()", action);
                        return startState;
                }
            }

            endState[GAME_SQUARE] = gameSquares[currentSquare];
            return UpdateStates(endState);
        }
    }

    public partial class Monopoly
    {
        SelfLinkTreatmentChoice skipSelfLinks;
        bool notifyAdapter;
        bool stopOnProblem;
        bool includeSelfLinkNoise = false;

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

        // IEzModelClient Interface method
        public void SetStateOfSystemUnderTest(int state)
        {
            // TODO: Implement this when NotifyAdapter is true.
        }

        // IEzModelClient Interface method
        public void ReportProblem(int initialState, int observed, int predicted, List<int> popcornTrail)
        {
            // TODO: Implement this when NotifyAdapter is true
        }

        // IEzModelClient Interface method
        public bool AreStatesAcceptablySimilar(int observed, int expected)
        {
            // TODO: Implement this when NotifyAdapter is true

            // Compare reported to expected, if unacceptable return false.
            return true;
        }

        // IEzModelClient Interface method
        public void ReportTraversal(int initialState, List<int> popcornTrail)
        {
            // TODO: Implement this when NotifyAdapter is true
        }

        // IEzModelClient Interface method
        public int AdapterTransition(int startState, int action)
        {
            // TODO: Finish implementation when NotifyAdapter is true

            int expected = GetEndState(startState, action);
            int observed = -1;

            // Responsibilities:
            // Optionally, validate that the state of the system under test
            // is acceptably similar to the startState argument. 
            // Required: drive the system under test according to the action
            // argument.
            // If executing the action is problematic, output a problem
            // notice in some way, and return an empty string to the caller
            // to indicate the start state was not reached.
            // If the action executes without problem, then measure the state
            // of the system under test and return the stringified SUT
            // state vector to the caller.

            return observed;
        }
    }
}
