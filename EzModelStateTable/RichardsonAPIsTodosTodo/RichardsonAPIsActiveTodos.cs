using System;
using System.Collections.Generic;
using SeriousQualityEzModel;

namespace RichardsonAPIsActiveTodos
{
    class RichardsonAPIsActiveTodosProgram
    {
        static int Main()
        {
            APIs client = new APIs();
            client.SelfLinkTreatment = SelfLinkTreatmentChoice.AllowAll;
            client.IncludeSelfLinkNoise = true;

            // We learned after generating the graph that it has 62 edges,
            // 13 nodes, and uses 25 actions.  Those specific numbers are
            // fed in to the constructor as a check that the graph can be
            // created and traversed when exactly enough graph components
            // are allocated.  Try reducing any of the three arguments and
            // observe the consequences.
            EzModelGraph graph = new EzModelGraph(client, 800, 20, 36);

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

            client.NotifyAdapter = false;
            // If you want stopOnProblem to stop, you need to return false from the AreStatesAcceptablySimilar method
            client.StopOnProblem = true;

            graph.RandomDestinationCoverage("RichardsonAPIs", 3);
            return 0;
        }
    }

    public class APIs : IEzModelClient
    {
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

        // Initially the system is not running, and this affects a lot of
        // state.
        bool svRunning = false;

        // Reduce state explosion on todos by classifing the quantity of todos
        // into either zero or more than zero.
        bool svHasTodos = true;

        // Reduce state explosion on todos that remain todo by classifying the
        // quantity of todos todo into either zero or more than zero.
        bool svHasActiveTodos = true;

        // Actions handled by APIs
        const string startup = "java -jar apichallenges.jar";
        const string shutdown = "Shutdown";
        const string getTodos = "GetTodosList";
        const string getActiveTodos = "GetActiveTodosList";
        const string headTodos = "GetTodosHeaders";
        // postActiveTodo is modeled as a single transition but is implemented
        // in the adapter as multiple transitions of post todo without ID and
        // delete todo by ID, with a weighting toward post.  The transitions
        // iterate until the goal net posts are achieved.
        const string postActiveTodo = "AddActiveTodo";
        const string postResolvedTodo = "AddResolvedTodo";
        const string showDocs = "GetDocumentation";
        const string getChallenges = "GetChallenges";
        const string optionsChallenges = "GetOptionsChallenges";
        const string headChallenges = "GetHeadersChallenges";
        const string getHeartbeat = "GetHeartbeatIsServerRunning";
        const string optionsHeartbeat = "GetOptionsForHeartbeat";
        const string headHeartbeat = "GetHeadersForHeartbeat";
        const string getTodoId = "GetTodoFromId";
        const string headTodoId = "GetHeadersOfTodoFromId";
        const string postTodoId = "AmendTodoByIdPostMethod";
        const string putTodoId = "AmendTodoByIdPutMethod";
        // deleteLessThanAllTodos is modeled as a single transition but is implemented
        // in the adapter as multiple transitions of delete todo by ID and
        // post todo without ID, with a weighting toward delete.  The transitions
        // iterate until the goal net deletions are achieved.
        const string deleteLessThanAllTodos = "DeleteLessThanAllTodos";
        const string deleteAllTodos = "DeleteAllTodos";
        const string resolveLessThanAllActiveTodos = "ResolveLessThanAllActiveTodos";
        const string resolveAllActiveTodos = "ResolveAllActiveTodos";
        const string activateAnyResolvedTodos = "ActivateAnyResolvedTodos";

        // Actions outside of the APIs that cover legitimate REST methods
        const string invalidRequest = "invalidRequest";

        string StringifyStateVector(bool running, bool hasTodos, bool hasActiveTodos)
        {
            string s = String.Format("Running.{0}, HasTodos.{1}, HasActiveTodos.{2}", running, hasTodos, hasActiveTodos);
            return s;
        }

        // Interface method
        public string GetInitialState()
        {
            return StringifyStateVector(svRunning, svHasTodos, svHasActiveTodos);
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

        // Interface method
        public List<string> GetAvailableActions(string startState)
        {
            List<string> actions = new List<string>();

            // We must parse the startState, because we will be fed
            // a variety of start states and we keep track of only
            // one state in this object.
            string[] vState = startState.Split(", ");
            bool running = vState[0].Contains("True") ? true : false;
            bool hasTodos = vState[1].Contains("True") ? true : false;
            bool hasActiveTodos = vState[2].Contains("True") ? true : false;

            if (!running)
            {
                actions.Add(startup);
                return actions;
            }

            actions.Add(shutdown);

            if (includeSelfLinkNoise)
            {
                actions.Add(getTodos);
                actions.Add(headTodos);
                actions.Add(showDocs);
                actions.Add(getChallenges);
                actions.Add(optionsChallenges);
                actions.Add(headChallenges);
                actions.Add(getHeartbeat);
                actions.Add(optionsHeartbeat);
                actions.Add(headHeartbeat);
                actions.Add(getTodoId);
                actions.Add(getActiveTodos);
                actions.Add(headTodoId);
                actions.Add(postTodoId);
                actions.Add(putTodoId);
                // Add an action for a class of invalid actions that extend beyond
                // specific invalid actions cited in the API Challenges list.
                actions.Add(invalidRequest);
            }

            actions.Add(postActiveTodo);
            actions.Add(postResolvedTodo);

            if (hasTodos)
            {
                actions.Add(deleteLessThanAllTodos);
                actions.Add(deleteAllTodos);
                actions.Add(activateAnyResolvedTodos);
            }

            if (hasActiveTodos)
            {
                actions.Add(resolveLessThanAllActiveTodos);
                actions.Add(resolveAllActiveTodos);
            }

            return actions;
        }

        // Interface method
        public string GetEndState(string startState, string action)
        {
            // We must parse the startState, else we will 
            string[] vState = startState.Split(", ");
            bool running = vState[0].Contains("True") ? true : false;
            bool hasTodos = vState[1].Contains("True") ? true : false;
            bool hasActiveTodos = vState[2].Contains("True") ? true : false;

            switch (action)
            {
                case invalidRequest:
                    break;
                case startup:
                    running = true;
                    break;
                case shutdown:
                    // Set all state variables back to initial state on shutdown,
                    // because if the APIs server starts up again, it will take
                    // on those initial state values.
                    running = false;
                    hasTodos = svHasTodos;
                    hasActiveTodos = svHasActiveTodos;
                    break;
                case getTodos:
                case headTodos:
                case getTodoId:
                case headTodoId:
                case postTodoId:
                case putTodoId:
                    break;
                case postActiveTodo:
                    hasTodos = true;
                    hasActiveTodos = true;
                    break;
                case postResolvedTodo:
                    hasTodos = true;
                    break;
                case activateAnyResolvedTodos:
                    hasActiveTodos = true;
                    break;
                case deleteLessThanAllTodos:
                    // do not set hasTodos true here.  The reason is, as a REST
                    // method deleteLessThanAllTodos can be called when the
                    // todos count is zero, meaning hasTodos is false.  Thus
                    // hasTodos would continue to be false after deleteLessThanAllTodos.
                    break;
                case deleteAllTodos:
                    hasTodos = false;
                    // We cannot have todos todo if we do not have todos.
                    hasActiveTodos = false;
                    break;
                case resolveLessThanAllActiveTodos:
                    // hasTodosTodo will still be true, so leave it alone.
                    // This could alter the hasActiveTodos, but we won't alter it here.
                    break;
                case resolveAllActiveTodos:
                    hasActiveTodos = false;
                    break;
                case showDocs:
                    break;
                case getChallenges:
                case optionsChallenges:
                case headChallenges:
                    break;
                case getHeartbeat:
                case optionsHeartbeat:
                case headHeartbeat:
                    break;
                default:
                    Console.WriteLine("ERROR: Unknown action '{0}' in GetEndState()", action);
                    break;
            }
            return StringifyStateVector(running, hasTodos, hasActiveTodos);
        }
    }
}
