using System;

namespace RichardsonAPIsAdapterExecution
{
    class RichardsonAPIsAdapterExecutionProgram
    {
        static void Main()
        {
            APIs cient = new APIs();
            cient.SkipSelfLinks = false;

            GeneratedGraph graph = new GeneratedGraph(cient, 3000, 100, 30);

            graph.DisplayStateTable(); // Display the Excel-format state table

            // write graph to dot format file
            string fname = "RichardsonAPIs";
            string suffix = "0000";
            graph.CreateGraphVizFileAndImage(fname, suffix, "Initial State");

            cient.NotifyAdapter = true;
            // If you want stopOnProblem to stop, you need to return false from the AreStatesAcceptablySimilar method
            cient.StopOnProblem = true;

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
            // Parse the state into state variable values, and then drive the state of
            // the system under test to match the parsed state values.
            // TODO: this method should report true or false to indicate whether it
            // succeeded in driving the state of the system under test.
            string[] vState = startState.Split(", ");
            bool running = vState[0].Contains("True") ? true : false;
            string todosClass = vState[1].Split(".")[1];
            bool xAuthTokenExists = vState[2].Contains("True") ? true : false;
            bool xChallengerGuidExists = vState[3].Contains("True") ? true : false;

            if (!running)
            {
                // shut down the APIs server.  Done..
                return
            }

            // running == true is implied at this point.
            // check whether the APIs server is running, and then
            // fire up the APIs server if it was not running.
            // When firing up the APIs server, wait for it to fire up
            // and then check again to see if it is running.  If it
            // is not running, fail!!  TODO: so we do need to indicate success/failure..
            // TODO: code up the running check, startup command, and secondary running check.

            // handle xAUthToken and xChallengerGuid before todosClass.  todosClass can be a lot of work.
            if (xAuthTokenExists)
            {
                // check whether the token exists.  If it does not exist, get it by calling the API.
            }

            if (xChallengerGuidExists)
            {
                // check whether the GUID exists.  If it does not exist, get it by calling
				// the API. (do we get a GUID when we are running locally?  If not, then
				// we need to do something here for local runs.
            }

            switch todosClass {
                case zeroTodos:
                    // If the class of the system under test is already zeroTodos, do nothing.
                    // Else, drain the todos list.
                    break;
                case betweenZeroAndMaximumTodos:
                    // If the class of the SUT is zeroTodos, add a todo.
                    // If the class of the SUT is maximumTodos, delete a todo.
                    // Else do nothing.
                    break;
                case maximumTodos:
                    // if the class of the SUT is not maximumTodos,
                    // add todos until we are at maximumTodos in the SUT.
                    break;
                default:
                    Console.WriteLine("Unknown todosClass '{0}' in SetStateOfSystemUnderTest()", todosClass);
            }
        }

        // Interface method
        public void ReportProblem(string initialState, string observed, string predicted, List<string> popcornTrail)
        {
        }

        // Interface method
        public bool AreStatesAcceptablySimilar(string observed, string expected)
        {
            // Compare reported to expected, if unacceptable return false.

            // For the Alan Richardson APIs, we want matches on the SUT state versus the model state.
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
            actions.Add(deleteTodoId);

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
