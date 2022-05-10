using SeriousQualityEzModel;

namespace TodoAPIsStartEndOnly
{
    class TodoAPIsStartEndOnlyProgram
    {
        static int Main()
        {
            APIs client = new();

            EzModelGraph graph = new (client, 10, 5, 5);

            if (!graph.GenerateGraph())
            {
                Console.WriteLine("Failed to generate graph.");
                return -1;
            }

            List<string> report = graph.AnalyzeConnectivity();
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

            graph.RandomDestinationCoverage("TodoAPIsStartEndOnly", 5);
            return 0;
        }
    }

    public partial class APIs : IEzModelClient
    {
        readonly Dictionary<string, string> state;

        const string IN_SESSION = "InSession";

        readonly List<string> stateVariableList = new()
        {
            IN_SESSION
        };

        readonly Dictionary<string, int> actions;
        readonly Dictionary<int, Dictionary<string, string>> statesDict;
        int statesCounter;

        // Actions handled by APIs
        const string startSession = "Start Session";
        const string endSession = "End Session";

        public APIs()
        {
            state = new Dictionary<string, string>
            {
                [IN_SESSION] = "No"
            };

            statesDict = new Dictionary<int, Dictionary<string, string>>();

            actions = new Dictionary<string, int>
            {
                [startSession] = 0,
                [endSession] = 1
            };

            statesCounter = 0;
            statesDict[statesCounter] = new Dictionary<string, string>(state);
            statesCounter++;
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
            foreach (KeyValuePair<int, Dictionary<string, string>> entry in statesDict)
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

        // IEzModelClient Interface method
        public int GetInitialState()
        {
            return 0;
        }

        // IEzModelClient Interface method
        public string[] GetActionsList()
        {
            return new string[]
                { startSession,
                endSession };
        }

        // IEzModelClient Interface method
        public List<int> GetAvailableActions(int currentState)
        {
            List<int> actionList = new();
            
            if (statesDict[currentState][IN_SESSION] == "No")
            {
                actionList.Add(actions[startSession]);
            }
            else
            {
                actionList.Add(actions[endSession]);
            }

            return actionList;
        }

        // IEzModelClient Interface method
        public int GetEndState(int startState, int action)
        {
            Dictionary<string, string> endState = new(statesDict[startState]);

            if (action == actions[startSession])
            {
                endState[IN_SESSION] = "Yes";
            }
            
            if (action == actions[endSession])
            {
                endState[IN_SESSION] = "No";
            }

            return UpdateStates(endState);
        }
    }
}
