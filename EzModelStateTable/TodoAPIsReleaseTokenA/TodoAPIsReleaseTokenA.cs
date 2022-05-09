using SeriousQualityEzModel;

namespace TodoAPIsReleaseTokenA
{
    class TodoAPIsReleaseTokenAProgram
    {
        static int Main()
        {
            APIs client = new()
            {
                SelfLinkTreatment = SelfLinkTreatmentChoice.OnePerAction,
                IncludeSelfLinkNoise = true
            };

            EzModelGraph graph = new (client, 2000, 200, 25, EzModelGraph.LayoutRankDirection.TopDown);

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
                // return -2;
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
            client.StopOnProblem = true;

            graph.RandomDestinationCoverage("TodoAPIsReleaseTokenA", 7);
            return 0;
        }
    }

    public partial class APIs : IEzModelClient
    {
        readonly Dictionary<string, string> state;

        const string IN_SESSION = "InSession";
        const string RESOLVED_TODOS = "ResolvedTodos";
        const string ACTIVE_TODOS = "ActiveTodos";
        const string AUTH_TOKEN_EXISTS = "AuthTokenExists";
        const string SECRET_NOTE_EXISTS = "SecretNoteExists";

        readonly List<string> stateVariableList = new()
        {
            IN_SESSION,
            RESOLVED_TODOS,
            ACTIVE_TODOS,
            AUTH_TOKEN_EXISTS,
            SECRET_NOTE_EXISTS
        };

        readonly Dictionary<string, int> actions;
        readonly Dictionary<int, Dictionary<string, string>> statesDict;
        int statesCounter;

        // Actions handled by APIs
        const string startSession = "Start Session";
        const string endSession = "End Session";
        const string addSomeActiveTodos = "Add some Active Todos";
        const string addSomeResolvedTodos = "Add some Resolved Todos";
        const string addAllActiveTodos = "Add all Active Todos";
        const string addAllResolvedTodos = "Add all Resolved Todos";
        const string editSomeTodos = "Edit some Todos";
        const string deleteAllResolvedTodos = "Delete all Resolved Todos";
        const string deleteAllActiveTodos = "Delete all Active Todos";
        const string deleteSomeResolvedTodos = "Delete some Resolved Todos";
        const string deleteSomeActiveTodos = "Delete some Active Todos";
        const string deleteAllTodos = "Delete All Todos";
        const string resolveSomeActiveTodos = "Resolve some Active Todos";
        const string resolveAllActiveTodos = "Resolve all Active Todos";
        const string activateSomeResolvedTodos = "Activate some Resolved Todos";
        const string activateAllResolvedTodos = "Activate all Resolved Todos";

        const string getAuthToken = "Get Auth Token";
        const string getSecretNote = "Get Secret Note By Token";
        const string setSecretNote = "Set Secret Note By Token";

        const string releaseAuthToken = "Release Auth Token";
        const string clearSecretNote = "Clear Secret Note";

        public APIs()
        {
            state = new Dictionary<string, string>
            {
                [IN_SESSION] = "No",
                [RESOLVED_TODOS] = "0",
                [ACTIVE_TODOS] = "0",
                [AUTH_TOKEN_EXISTS] = "No",
                [SECRET_NOTE_EXISTS] = "No"
            };

            statesDict = new Dictionary<int, Dictionary<string, string>>();

            actions = new Dictionary<string, int>
            {
                [startSession] = 0,
                [endSession] = 1,
                [addSomeActiveTodos] = 2,
                [addSomeResolvedTodos] = 3,
                [addAllActiveTodos] = 4,
                [addAllResolvedTodos] = 5,
                [editSomeTodos] = 6,
                [deleteAllResolvedTodos] = 7,
                [deleteAllActiveTodos] = 8,
                [deleteSomeResolvedTodos] = 9,
                [deleteSomeActiveTodos] = 10,
                [deleteAllTodos] = 11,
                [resolveSomeActiveTodos] = 12,
                [resolveAllActiveTodos] = 13,
                [activateSomeResolvedTodos] = 14,
                [activateAllResolvedTodos] = 15,
                [getAuthToken] = 16,
                [getSecretNote] = 17,
                [setSecretNote] = 18,
                [releaseAuthToken] = 19,
                [clearSecretNote] = 20
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
                endSession,
                addSomeActiveTodos,
                addSomeResolvedTodos,
                addAllActiveTodos,
                addAllResolvedTodos,
                editSomeTodos,
                deleteAllResolvedTodos,
                deleteAllActiveTodos,
                deleteSomeResolvedTodos,
                deleteSomeActiveTodos,
                deleteAllTodos,
                resolveSomeActiveTodos,
                resolveAllActiveTodos,
                activateSomeResolvedTodos,
                activateAllResolvedTodos,
                getAuthToken,
                getSecretNote,
                setSecretNote,
                releaseAuthToken,
                clearSecretNote};
        }

        // Interface method
        public List<int> GetAvailableActions(int currentState)
        {
            List<int> actionList = new();
            Dictionary<string, string> current = statesDict[currentState];

            if (current[IN_SESSION] == "No")
            {
                actionList.Add(actions[startSession]);
                return actionList;
            }

            actionList.Add(actions[endSession]);


            if (includeSelfLinkNoise)
            {
                if (current[ACTIVE_TODOS] != "Zero" || current[RESOLVED_TODOS] != "Zero")
                {
                    actionList.Add(actions[editSomeTodos]);
                }
            }

            if (current[AUTH_TOKEN_EXISTS] == "No")
            {
                actionList.Add(actions[getAuthToken]);
            }
            else
            {
                actionList.Add(actions[releaseAuthToken]);
                actionList.Add(actions[getSecretNote]);
                actionList.Add(actions[setSecretNote]);
                if (current[SECRET_NOTE_EXISTS] == "Yes")
                {
                    actionList.Add(actions[clearSecretNote]);
                }
            }

            if (current[ACTIVE_TODOS] == "Zero")
            {
                actionList.Add(actions[addSomeActiveTodos]);
                actionList.Add(actions[addAllActiveTodos]);
            }
            else if (current[ACTIVE_TODOS] == "Some")
            {
                actionList.Add(actions[deleteAllTodos]);
                actionList.Add(actions[addSomeActiveTodos]);
                actionList.Add(actions[addAllActiveTodos]);
                actionList.Add(actions[deleteAllActiveTodos]);
                actionList.Add(actions[deleteSomeActiveTodos]);
                actionList.Add(actions[resolveSomeActiveTodos]);
                actionList.Add(actions[resolveAllActiveTodos]);
            }
            else if (current[ACTIVE_TODOS] == "Max")
            {
                actionList.Add(actions[deleteAllActiveTodos]);
                actionList.Add(actions[deleteSomeActiveTodos]);
                actionList.Add(actions[deleteAllTodos]);
                actionList.Add(actions[resolveSomeActiveTodos]);
                actionList.Add(actions[resolveAllActiveTodos]);
            }

            if (current[RESOLVED_TODOS] == "Zero")
            {
                actionList.Add(actions[addSomeResolvedTodos]);
                actionList.Add(actions[addAllResolvedTodos]);
            }
            else if (current[RESOLVED_TODOS] == "Some")
            {
                if (!actionList.Contains(actions[deleteAllTodos]))
                {
                    actionList.Add(actions[deleteAllTodos]);
                }
                actionList.Add(actions[addSomeResolvedTodos]);
                actionList.Add(actions[addAllResolvedTodos]);
                actionList.Add(actions[activateSomeResolvedTodos]);
                actionList.Add(actions[activateAllResolvedTodos]);
                actionList.Add(actions[deleteSomeResolvedTodos]);
                actionList.Add(actions[deleteAllResolvedTodos]);
            }
            else if (current[RESOLVED_TODOS] == "Max")
            {
                if (!actionList.Contains(actions[deleteAllTodos]))
                {
                    actionList.Add(actions[deleteAllTodos]);
                }
                actionList.Add(actions[activateSomeResolvedTodos]);
                actionList.Add(actions[activateAllResolvedTodos]);
                actionList.Add(actions[deleteSomeResolvedTodos]);
                actionList.Add(actions[deleteAllResolvedTodos]);
            }

            return actionList;
        }

        // Interface method
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

            // editSomeTodos, deleteSomeTodos are no-ops

            if (action == actions[addSomeActiveTodos])
            {
                if (endState[ACTIVE_TODOS] == "Zero")
                {
                    endState[ACTIVE_TODOS] = "Some";
                }
            }

            if (action == actions[addSomeResolvedTodos])
            {
                if (endState[RESOLVED_TODOS] == "Zero")
                {
                    endState[RESOLVED_TODOS] = "Some";
                }
            }

            if (action == actions[activateSomeResolvedTodos])
            {
                if (endState[ACTIVE_TODOS] == "Zero")
                {
                    endState[ACTIVE_TODOS] = "Some";
                }
                if (endState[RESOLVED_TODOS] == "Max")
                {
                    endState[RESOLVED_TODOS] = "Some";
                }
            }

            if (action == actions[deleteAllActiveTodos])
            {
                endState[ACTIVE_TODOS] = "Zero";
            }

            if (action == actions[deleteSomeActiveTodos])
            {
                if (endState[ACTIVE_TODOS] == "Max")
                {
                    endState[ACTIVE_TODOS] = "Some";
                }
            }

            if (action == actions[deleteAllTodos])
            {
                endState[RESOLVED_TODOS] = "Zero";
                endState[ACTIVE_TODOS] = "Zero";
            }    

            if (action == actions[resolveSomeActiveTodos])
            {
                if (endState[ACTIVE_TODOS] == "Max")
                {
                    endState[ACTIVE_TODOS] = "Some";
                }
                if (endState[RESOLVED_TODOS] == "Zero")
                {
                    endState[RESOLVED_TODOS] = "Some";
                }
            }

            if (action == actions[resolveAllActiveTodos])
            {
                endState[ACTIVE_TODOS] = "Zero";
                endState[RESOLVED_TODOS] = "Max";
            }

            if (action == actions[addAllActiveTodos])
            {
                endState[ACTIVE_TODOS] = "Max";
            }
 
            if (action == actions[addAllResolvedTodos])
            {
                endState[RESOLVED_TODOS] = "Max";
            }

            if (action == actions[deleteAllResolvedTodos])
            {
                endState[RESOLVED_TODOS] = "Zero";
            }

            if (action == actions[deleteSomeResolvedTodos])
            { 
                if (endState[RESOLVED_TODOS] == "Max")
                {
                    endState[RESOLVED_TODOS] = "Some";
                }
            }

            if (action == actions[activateAllResolvedTodos])
            {
                endState[RESOLVED_TODOS] = "Zero";
                endState[ACTIVE_TODOS] = "Max";
            }

            if (action == actions[getAuthToken])
            {
                endState[AUTH_TOKEN_EXISTS] = "Yes";
            }

            if (action == actions[releaseAuthToken])
            {
                endState[AUTH_TOKEN_EXISTS] = "No";
            }

            // getSecretNote is a no-op

            if (action == actions[setSecretNote])
            {
                endState[SECRET_NOTE_EXISTS] = "Yes";
            }

            if (action == actions[clearSecretNote])
            {
                endState[SECRET_NOTE_EXISTS] = "No";
            }

            return UpdateStates(endState);
        }
    }

    public partial class APIs
    {
        Random rnd = new Random(DateTime.Now.Millisecond);
        SelfLinkTreatmentChoice selfLinkTreatment;
        bool notifyAdapter;
        bool stopOnProblem;
        bool includeSelfLinkNoise = false;

        // IEzModelClient Interface Property
        public SelfLinkTreatmentChoice SelfLinkTreatment
        {
            get => selfLinkTreatment;
            set => selfLinkTreatment = value;
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
