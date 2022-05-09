using SeriousQualityEzModel;

namespace TodoAPIsStartEndOnly
{
    class TodoAPIsStartEndOnlyProgram
    {
        static int Main()
        {
            APIs client = new()
            {
                SelfLinkTreatment = SelfLinkTreatmentChoice.SkipAll,
                IncludeSelfLinkNoise = true
            };

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

            // If you want to drive the system under test as EzModel generates test steps,
            // set client.NotifyAdapter true.
            client.NotifyAdapter = false;
            // If you want EzModel to stop generating test steps when a problem is
            // detected, set client.NotifyAdapter true, set client.StopOnProblem true,
            // and then return false from the client.AreStatesAcceptablySimilar() method.
            client.StopOnProblem = true;

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

    public partial class APIs
    {
        SelfLinkTreatmentChoice skipSelfLinks;
        bool notifyAdapter;
        bool stopOnProblem;
        bool includeSelfLinkNoise = false;

        // These properties are unimportant until after the model is building.
        // Get them out of the way, in a place that will be easy to get at later.
        // Interface Properties
        public SelfLinkTreatmentChoice SelfLinkTreatment
        {
            get => skipSelfLinks;
            set => skipSelfLinks = value;
        }

        // IEzModelClient Interface Property
        public bool NotifyAdapter
        {
            get => notifyAdapter;
            set => notifyAdapter = value;
        }

        // IEzModelClient Interface Property
        public bool StopOnProblem
        {
            get => stopOnProblem;
            set => stopOnProblem = value;
        }

        public bool IncludeSelfLinkNoise
        {
            get => includeSelfLinkNoise;
            set => includeSelfLinkNoise = value;
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
