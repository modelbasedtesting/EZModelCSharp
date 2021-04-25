using System;
using System.Collections.Generic;
using SeriousQualityEzModel;

namespace AlanRichardsonAPIs
{
    class AlanRichardsonAPIsProgram
    {
        static void Main()
        {
            APIs client = new APIs();
            client.SkipSelfLinks = true;

            GeneratedGraph graph = new GeneratedGraph(client, 3000, 100, 30);

            List<string> duplicateActions = graph.ReportDuplicateOutlinks();

            graph.DisplayStateTable(); // Display the Excel-format state table

            // write graph to dot format file
            string fname = "RichardsonAPIs";
            string suffix = "0000";
            graph.CreateGraphVizFileAndImage(fname, suffix, "Initial State");

//            client.NotifyAdapter = true;
// If you want stopOnProblem to stop, you need to return false from the AreStatesAcceptablySimilar method
//            client.StopOnProblem = true;

            graph.RandomDestinationCoverage(fname);
        }
    }

    public class APIs : IEzModelClient
    {
        bool skipSelfLinks;
        bool notifyAdapter;
        bool stopOnProblem;

        // Interface Properties
        public bool SkipSelfLinks
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

        // Initially the system is not running, and this affects a lot of
        // state.
        bool svRunning = false;

        // Reduce state explosion by bracketing the number of todos in the
        // todos list into three partitions.  Each partition has distinct
        // outlinks.
        const string zeroTodos = "Zero";
        const string betweenZeroAndMaximumTodos = "BetweenZeroAndMaximum";
        const string maximumTodos = "Maximum";

        string svTodosClassString = betweenZeroAndMaximumTodos;

        // Once the X-AUTH-TOKEN exists, there isn't a way to get rid of it
        // except for stopping the system under test.
        bool svXAuthTokenExists = false;

        // The X-CHALLENGER GUID is created / returned from the system under test.
        // It will be unknown during each new run of the system under test, until
        // it is requested.  It must be supplied with each multi-player session.
        bool svXChallengerGuidExists = false;

        // Actions handled by APIs
        const string startup = "java -jar apichallenges.jar";
        const string shutdown = "Shutdown";
        const string getTodos = "GetTodosList";
        const string headTodos = "GetTodosHeaders";
        const string postTodos = "AddTodoWithoutId";
        const string getTodoId = "GetTodoFromId";
        const string headTodoId = "GetHeadersOfTodoFromId";
        const string postTodoId = "AmendTodoByIdPostMethod";
        const string putTodoId = "AmendTodoByIdPutMethod";
        const string deleteTodoId = "DeleteTodoById";
        const string showDocs = "GetDocumentation";
        const string createXChallengerGuid = "GetXChallengerGuid";
        const string restoreChallenger = "RestoreSavedXChallengerGuid";
        const string getChallenges = "GetChallenges";
        const string optionsChallenges = "GetOptionsChallenges";
        const string headChallenges = "GetHeadersChallenges";
        const string getHeartbeat = "GetHeartbeatIsServerRunning";
        const string optionsHeartbeat = "GetOptionsForHeartbeat";
        const string headHeartbeat = "GetHeadersForHeartbeat";
        const string postSecretToken = "GetSecretToken";
        const string getSecretNote = "GetSecretNoteByToken";
        const string postSecretNote = "SetSecretNoteByToken";

        // Special actions to reduce state explosion related to number of todos
        const string postMaximumTodo = "AddMaximumTodoWithoutId";
        const string deleteFinalTodoId = "DeleteFinalTodoById";
        const string postBetweenZeroAndMaximumTodo = "AddBetweenZeroAndMaximumTodoWithoutId";
        const string deleteBetweenZeroAndMaximumTodoId = "DeleteBetweenZeroAndMaximumTodoById";

        // Actions outside of the APIs that cover legitimate REST methods
        const string invalidRequest = "invalidRequest";

        string StringifyStateVector(bool running, string todosClass, bool xAuthTokenExists, bool xChallengerGuidExists)
        {
            string s = String.Format("Running.{0}, Todos.{1}, XAuth.{2}, XChallenger.{3}", running, todosClass, xAuthTokenExists, xChallengerGuidExists);
            return s;
        }

        // Interface method
        public string GetInitialState()
        {
            return StringifyStateVector(svRunning, svTodosClassString, svXAuthTokenExists, svXChallengerGuidExists);
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
            string todosClass = vState[1].Split(".")[1];
            bool xAuthTokenExists = vState[2].Contains("True") ? true : false;
            bool xChallengerGuidExists = vState[3].Contains("True") ? true : false;

            if (!running)
            {
                actions.Add(startup);
                return actions;
            }

            // Control the size of the state-transition table
            // by limiting the number of Todo list items.
            // The initial todo list is 10 items, so choose
            // a max value greater than or equal to 10.
            // if (numTodos <= maxTodos)
            switch (todosClass)
            {
                case zeroTodos:
                    actions.Add(postTodos);
                    break;
                case betweenZeroAndMaximumTodos:
                    actions.Add(postBetweenZeroAndMaximumTodo);
                    actions.Add(deleteBetweenZeroAndMaximumTodoId);
                    actions.Add(postMaximumTodo);
                    actions.Add(deleteFinalTodoId);
                    break;
                case maximumTodos:
                    actions.Add(deleteTodoId);
                    break;
                default:
                    Console.WriteLine("ERROR: Unknown Todos Class '{0}' GetAvailableActions()", todosClass);
                    break;
            }

            actions.Add(shutdown);
            actions.Add(getTodos);
            actions.Add(headTodos);
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

            actions.Add(getTodoId);
            actions.Add(headTodoId);
            actions.Add(postTodoId);
            actions.Add(putTodoId);
// The next line of code, adding deleteTodoId to actions, is redundant and
// is therefore an error in the model configuration.  It manifests as a
// duplicate deleteTodoById arc between nodes.
//            actions.Add(deleteTodoId);

            if (xAuthTokenExists)
            {
                actions.Add(getSecretNote);
                actions.Add(postSecretNote);
            }
            else
            {
                actions.Add(postSecretToken);
            }

            if (xChallengerGuidExists)
            {
                actions.Add(restoreChallenger);
            }
            else
            {
                actions.Add(createXChallengerGuid);
            }

            return actions;
        }

        // Interface method
        public string GetEndState(string startState, string action)
        {
            // We must parse the startState, else we will 
            string[] vState = startState.Split(", ");
            bool running = vState[0].Contains("True") ? true : false;
            string todosClass = vState[1].Split(".")[1];
            bool xAuthTokenExists = vState[2].Contains("True") ? true : false;
            bool xChallengerGuidExists = vState[3].Contains("True") ? true : false;

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
                    todosClass = svTodosClassString;
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
                case postTodos:
                    if (todosClass == zeroTodos)
                    {
                        todosClass = betweenZeroAndMaximumTodos;
                    }
                    break;
                case postBetweenZeroAndMaximumTodo:
                case deleteBetweenZeroAndMaximumTodoId:
                    break;
                case deleteTodoId:
                    if (todosClass == maximumTodos)
                    {
                        todosClass = betweenZeroAndMaximumTodos;
                    }
                    break;
                case deleteFinalTodoId:
                    if (todosClass == betweenZeroAndMaximumTodos)
                    {
                        todosClass = zeroTodos;
                    }
                    break;
                case postMaximumTodo:
                    if (todosClass == betweenZeroAndMaximumTodos)
                    {
                        todosClass = maximumTodos;
                    }
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
            return StringifyStateVector(running, todosClass, xAuthTokenExists, xChallengerGuidExists);
        }
    }
}
