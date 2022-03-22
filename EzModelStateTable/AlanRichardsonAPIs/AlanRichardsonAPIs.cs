using System;
using System.Collections.Generic;
using SeriousQualityEzModel;

namespace AlanRichardsonAPIs
{
    class AlanRichardsonAPIsProgram
    {
        static int Main()
        {
            APIs client = new ();
            client.SelfLinkTreatment = SelfLinkTreatmentChoice.AllowAll;
            client.IncludeSelfLinkNoise = false;

            // We learned after generating the graph that it has 62 edges,
            // 13 nodes, and uses 25 actions.  Those specific numbers are
            // fed in to the constructor as a check that the graph can be
            // created and traversed when exactly enough graph components
            // are allocated.  Try reducing any of the three arguments and
            // observe the consequences.
            EzModelGraph graph = new (client, 2000, 32, 40);

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
            client.StopOnProblem = true;

            graph.RandomDestinationCoverage("RichardsonAPIs", 3);

            return 0;
        }
    }

    public partial class APIs : IEzModelClient
    {
        // Initially the system is not running, and this affects a lot of
        // state.
        bool svInSession = false;

        // Reduce state explosion on todos by classifing the quantity of todos
        // into either zero or more than zero.
        bool svHasResolvedTodos = true;

        // Reduce state explosion on todos that remain todo by classifying the
        // quantity of todos todo into either zero or more than zero.
        bool svHasActiveTodos = true;

        // Once the X-AUTH-TOKEN exists, there isn't a way to get rid of it
        // except for stopping the system under test.
        bool svXAuthTokenExists = false;

        // The X-CHALLENGER GUID is created / returned from the system under test.
        // It will be unknown during each new run of the system under test, until
        // it is requested.  It must be supplied with each multi-player session.
        bool svXChallengerGuidExists = false;

        // Actions handled by APIs
        const string startSession = "Start Session";
        const string shutdown = "Shutdown";
        const string getTodos = "Get Todos List";
        const string getActiveTodos = "Get Active Todos List";
        const string headTodos = "Get Todos Headers";
        // postActiveTodo is modeled as a single transition but is implemented
        // in the adapter as multiple transitions of post todo without ID and
        // delete todo by ID, with a weighting toward post.  The transitions
        // iterate until the goal net posts are achieved.
        const string postActiveTodo = "Add an Active Todo";
        const string postResolvedTodo = "Add a Resolved Todo";
        const string showDocs = "Get Documentation";
        const string getChallenges = "Get Challenges";
        const string optionsChallenges = "Get Options Challenges";
        const string headChallenges = "Get Headers Challenges";
        const string getHeartbeat = "Get Service Heartbeat";
        const string optionsHeartbeat = "Get Options for Heartbeat";
        const string headHeartbeat = "Get Headers for Heartbeat";
        const string getTodoId = "Get a Todo";
        const string headTodoId = "Get Headers of a Todo";
        const string postTodoId = "Edit a Todo by Post";
        const string putTodoId = "Edit a Toodo by Put";
        // deleteLessThanAllTodos is modeled as a single transition but is implemented
        // in the adapter as multiple transitions of delete todo by ID and
        // post todo without ID, with a weighting toward delete.  The transitions
        // iterate until the goal net deletions are achieved.
        const string deleteSomeTodos = "Delete some Todos";
        const string deleteAllTodos = "Delete all Todos";
        const string resolveSomeActiveTodos = "Resolve some Active Todos";
        const string resolveAllActiveTodos = "Resolve all Active Todos";
        const string activateSomeResolvedTodos = "Activate some Resolved Todos";
        const string activateAllResolvedTodos = "Activate all Resolved Todos";

        const string createXChallengerGuid = "Get XChallenger Guid";
        const string restoreChallenger = "Restore Saved XChallenger Guid";
        const string postSecretToken = "Get Secret Token";
        const string getSecretNote = "Get Secret Note";
        const string postSecretNote = "Set Secret Note";

        // Actions outside of the APIs that cover legitimate REST methods
        const string invalidRequest = "invalidRequest";

        string StringifyState(int state)
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

        // Interface method
        public int GetInitialState()
        {
            return 0;
        }

        // IEzModelClient Interface method
        public string[] GetActionsList()
        {
            return new string[]
                { selectTea,
                selectCoffee,
                selectHotWater,
                cancelSelection,
                addNickel,
                addDime,
                addQuarter,
                refund,
                dispense,
                selectCocaCola };
        }

        // IEzModelClient Interface method
        public List<int> GetAvailableActions(int currentState)
        {
            List<int> actions = new ();

            if (!inSession)
            {
                actions.Add(startSession);
                return actions;
            }

            actions.Add(shutdown);

            if (includeSelfLinkNoise)
            {
                actions.Add(getTodos);
                actions.Add(getTodoId);
                actions.Add(headTodos);
                actions.Add(headTodoId);
                actions.Add(postTodoId);
                actions.Add(putTodoId);
                actions.Add(showDocs);
                actions.Add(getChallenges);
                actions.Add(optionsChallenges);
                actions.Add(headChallenges);
                actions.Add(getHeartbeat);
                actions.Add(optionsHeartbeat);
                actions.Add(headHeartbeat);

                // Add an action for a class of invalid actions that extend beyond
                // specific invalid actions cited in the API Challenges list.
                actions.Add(invalidRequest);
            }

            actions.Add(postActiveTodo);
            actions.Add(postResolvedTodo);

            if (hasResolvedTodos)
            {
                actions.Add(deleteSomeTodos);
                actions.Add(deleteAllTodos);
                actions.Add(activateSomeResolvedTodos);
            }

            if (hasActiveTodos)
            {
                actions.Add(resolveSomeActiveTodos);
                actions.Add(resolveAllActiveTodos);
            }

            actions.Add(getSecretNote);
            actions.Add(postSecretNote);
            actions.Add(postSecretToken);
            actions.Add(restoreChallenger);
            actions.Add(createXChallengerGuid);

            return actions;
        }

        // Interface method
        public int GetEndState(int startState, int action)
        {
            Dictionary<string, string> endState = new(statesDict[startState]);

            switch (action)
            {
                case invalidRequest:
                    break;
                case startSession:
                    inSession = true;
                    break;
                case shutdown:
                    // Set all state variables back to initial state on shutdown,
                    // because if the APIs server starts up again, it will take
                    // on those initial state values.
                    inSession = false;
                    hasResolvedTodos = svHasResolvedTodos;
                    hasActiveTodos = svHasActiveTodos;
                    xChallengerGuidExists = svXChallengerGuidExists;
                    xAuthTokenExists = svXAuthTokenExists;
                    break;
                case getTodos:
                case headTodos:
                case getTodoId:
                case headTodoId:
                case postTodoId:
                case putTodoId:
                    break;
                case postActiveTodo:
                    hasResolvedTodos = true;
                    hasActiveTodos = true;
                    break;
                case postResolvedTodo:
                    hasResolvedTodos = true;
                    break;
                case activateSomeResolvedTodos:
                    hasActiveTodos = true;
                    break;
                case deleteSomeTodos:
                    // do not set hasTodos true here.  The reason is, as a REST
                    // method deleteLessThanAllTodos can be called when the
                    // todos count is zero, meaning hasTodos is false.  Thus
                    // hasTodos would continue to be false after deleteLessThanAllTodos.
                    break;
                case deleteAllTodos:
                    hasResolvedTodos = false;
                    // We cannot have todos todo if we do not have todos.
                    hasActiveTodos = false;
                    break;
                case resolveSomeActiveTodos:
                    // hasTodosTodo will still be true, so leave it alone.
                    // This could alter the hasActiveTodos, but we won't alter it here.
                    break;
                case resolveAllActiveTodos:
                    hasActiveTodos = false;
                    break;
                case showDocs:
                    break;
                case createXChallengerGuid:
                    xChallengerGuidExists = true;
                    break;
                case restoreChallenger:
                    break;
                case getChallenges:
                case optionsChallenges:
                case headChallenges:
                    break;
                case getHeartbeat:
                case optionsHeartbeat:
                case headHeartbeat:
                    break;
                case postSecretToken:
                    xAuthTokenExists = true;
                    break;
                case getSecretNote:
                case postSecretNote:
                    break;
                default:
                    Console.WriteLine("ERROR: Unknown action '{0}' in GetEndState()", action);
                    break;
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

        // IEzModelClient Interface Property
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

        // Interface method
        public void SetStateOfSystemUnderTest(string state)
        {
        }

        // Interface method
        public void ReportProblem(string initialState, string observed, string predicted, List<string> popcornTrail)
        {
        }

        // Interface method
        public bool AreStatesAcceptablySimilar(string observed, string expected)
        {
            // Compare reported to expected, if unacceptable return false.
            return true;
        }

        // Interface method
        public void ReportTraversal(string initialState, List<string> popcornTrail)
        {

        }

        // Interface method
        public string AdapterTransition(string startState, string action)
        {
            string expected = GetEndState(startState, action);
            string observed = "";

            return observed;
        }
    }
}
