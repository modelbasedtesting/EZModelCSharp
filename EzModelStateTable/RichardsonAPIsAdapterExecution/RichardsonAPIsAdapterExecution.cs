using System;
using System.Text;
using System.Net.Http;
using System.Collections.Generic;
using SeriousQualityEzModel;
using SynchronousHttpClientExecuter;

namespace RichardsonAPIsAdapterExecution
{
    class RichardsonAPIsAdapterExecutionProgram
    {
        static int Main(string[] args)
        {
            APIs client = new APIs();
            client.SelfLinkTreatment = SelfLinkTreatmentChoice.OnePerAction;
            client.IncludeSelfLinkNoise = true;

            EzModelGraph graph = new EzModelGraph(client, 3000, 100, 30, EzModelGraph.LayoutRankDirection.TopDown);

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

            client.WaitForObserverKeystroke = true;
            graph.RandomDestinationCoverage("RichardsonAPIs");

            // normal finish
            return 0;
        }
    }

    public class APIs : IEzModelClient
    {
        SynchronousHttpClient executer = new SynchronousHttpClient();
        bool waitForObserverKeystroke;

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

        public bool WaitForObserverKeystroke
        {
            get => waitForObserverKeystroke;
            set => waitForObserverKeystroke = value;
        }

        public APIs( )
        {
            executer.server = "http://localhost:4567/";
            notifyAdapter = false;
            stopOnProblem = true;
            waitForObserverKeystroke = false;                
        }

        // Initially the system is not running, and this affects a lot of
        // state.
        bool svInSession = false;

        // Reduce state explosion by bracketing the number of todos in the
        // todos list into three partitions.  Each partition has distinct
        // outlinks.
        const string zeroTodos = "Zero";
        const string someTodos = "Some";
        const string maximumTodos = "Maximum";

        string svTodosClassString = someTodos;

        // Once the X-AUTH-TOKEN exists, there isn't a way to get rid of it
        // except for stopping the system under test.
        bool svXAuthTokenExists = false;

        // The X-CHALLENGER GUID is created / returned from the system under test.
        // It will be unknown during each new run of the system under test, until
        // it is requested.  It must be supplied with each multi-player session.
        //bool svXChallengerGuidExists = false;

        uint actionCount = 0;
        uint todosCount = 10; // Default initial state due to jar file behavior.
        uint maximumTodosCount = 14; // Arbitrary maximum
        Random random = new Random(DateTime.Now.Millisecond);

        // Actions handled by APIs
        const string startSession = "Start Session";
        const string endSession = "End Session";
        const string getTodos = "Get Todos List";
        const string headTodos = "Get Todos Headers";
        // postNetTodos is modeled as a single transition but is implemented
        // in the adapter as multiple transitions of post todo without ID and
        // delete todo by ID, with a weighting toward post.  The transitions
        // iterate until the goal net posts are achieved.
        const string postNetTodos = "Add Some Todos";
        const string getTodoId = "Get a Todo";
        const string headTodoId = "Get Headers of a Todo";
        const string postAmendTodoId = "Edit a Todo by Post";
        const string putTodoId = "Edit a Todo by Put";
        // deleteNetTodos is modeled as a single transition but is implemented
        // in the adapter as multiple transitions of delete todo by ID and
        // post todo without ID, with a weighting toward delete.  The transitions
        // iterate until the goal net deletions are achieved.
        const string deleteNetTodos = "Delete Some Todos";
        const string showDocs = "Get Documentation";
        //const string createXChallengerGuid = "GetXChallengerGuid";
        //const string restoreChallenger = "RestoreSavedXChallengerGuid";
        const string getChallenges = "Get Challenges";
        const string optionsChallenges = "Get Options Challenges";
        const string headChallenges = "Get Headers Challenges";
        const string getHeartbeat = "Get Server Heartbeat";
        const string optionsHeartbeat = "Get Options for Heartbeat";
        const string headHeartbeat = "Get Headers for Heartbeat";
        const string postSecretToken = "Get Secret Token";
        const string getSecretNote = "Get Secret Note";
        const string postSecretNote = "Set Secret Note";

        // Special actions to reduce state explosion related to number of todos
        const string postMaximumTodo = "Add all Todos";
        const string deleteFinalTodoId = "Delete all Todos";

        // Actions outside of the APIs that cover legitimate REST methods
        const string invalidRequest = "invalidRequest";

        string StringifyStateVector(bool inSession, string todosClass, bool xAuthTokenExists)
        {
            string s = String.Format("InSession.{0}, Todos.{1}, XAuth.{2}", inSession, todosClass, xAuthTokenExists);
            return s;
        }

        // Interface method
        public string GetInitialState()
        {
            // NOTE: EzModel will call SetStateOfSystemUnderTest if notifyAdapter
            // is true, so don't call SetStateOfSystemUnderTest in this function.
            string state = StringifyStateVector(svInSession, svTodosClassString, svXAuthTokenExists);

            return state;
        }

        // Interface method
        public void SetStateOfSystemUnderTest(string state)
        {
            // Parse the state into state variable values, and then drive the state of
            // the system under test to match the parsed state values.
            // TODO: this method should report true or false to indicate whether it
            // succeeded in driving the state of the system under test.
            string[] vState = state.Split(", ");
            bool inSession = vState[0].Contains("True") ? true : false;
            string todosClass = vState[1].Split(".")[1];
            bool xAuthTokenExists = vState[2].Contains("True") ? true : false;
            //bool xChallengerGuidExists = vState[3].Contains("True") ? true : false;

            if (!inSession)
            {
                // shut down the APIs server.  Done..
                return;
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

            //if (xChallengerGuidExists)
            //{
                // check whether the GUID exists.  If it does not exist, get it by calling
				// the API. (do we get a GUID when we are running locally?  If not, then
				// we need to do something here for local runs.
            //}

            switch (todosClass) {
                case zeroTodos:
                    // If the class of the system under test is already zeroTodos, do nothing.
                    // Else, drain the todos list.
                    break;
                case someTodos:
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
                    break;
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
            // The traversal started with the SUT in the following state:
            // The following actions occurred in sequence

        }

        void ShowActionSeparator()
        {

        }

        // Interface method
        public string AdapterTransition(string startState, string action)
        {
            string expected = GetEndState(startState, action);

            string observed = "";

            // In this model, actions are all about REST communications and
            // whether the APIs server is up or down.
            // With respect to the four state variables,


            //  - set / confirm the start state
            string[] vState = startState.Split(", ");
            bool inSession = vState[0].Contains("True") ? true : false;
            string todosClass = vState[1].Split(".")[1];
            bool xAuthTokenExists = vState[2].Contains("True") ? true : false;
            //bool xChallengerGuidExists = vState[3].Contains("True") ? true : false;

            actionCount++;

            Console.WriteLine("****{0,6} **** {1,29} ****", actionCount, action );

            // IN CASE THE ADAPTER FINDS A DISCREPANCY BETWEEN THE SUT STATE
            // AND THE STARTSTATE ARGUMENT, OUTPUT A NOTICE.  CONTINUE RUNNING.
            if (inSession)
            {
                // confirm the service is running
                // for Java, look for the pid or process status.

            }
            else
            {
                // confirm the service is not running
            }

            switch (todosClass)
            {
                case zeroTodos:
                    // confirm the service has zero Todos
                    break;
                case someTodos:
                    // confirm the service has between zero and max todos
                    break;
                case maximumTodos:
                    // confirm the service has maximum todos
                    break;
                default:
                    Console.WriteLine("ERROR: Unknown Todos Class '{0}' AdapterTransition()", todosClass);
                    break;
            }

            if (xAuthTokenExists)
            {
                // confirm the service knows the xAuthToken
            }
            else
            {
                // confirm the service does not know the xAuthToken
            }

            //if (xChallengerGuidExists)
            //{
            //    // confirm the service knows the xChallengerGuid
            //}
            //else
            //{
            //    // confirm the service does not know the xChallengerGuid
            //}

            // Define the acceptHeaders list just once, outside the
            // switch where they are used in several cases.
            List<string> acceptHeaders = new List<string>();
            StringContent body;
            int selectedTodo = random.Next((int)todosCount) + 1;

            try
            {
                //  - drive execution of the action (of the transition)
                switch (action)
                {
                    // TODO:
                    // Add the Trace cases
                    // https://www.blackhillsinfosec.com/three-minutes-with-the-http-trace-method/

                    case invalidRequest:
                        // Make a set of invalid requests, give them a weight of 16.
                        // Generate a random number
                        // Select a request at random, using weights.
                        // cut the weight of the selected request in half.
                        // Report the selected request to the console.
                        // Issue the selected request to the APIs service.
                        break;

                    case startSession:
                        // launch the java app
                        // The app dumps a lot of information to the
                        // standard output on startup: port it is running
                        // on, list of challenges.
                        bool started = executer.Startup();
                        if (!started)
                        {
                            // We couldn't start the APIs server.  Stop the test run.
                            // The exit code of -1 indicates abnormal termination,
                            // and in this case it is because we couldn't start the
                            // APIs server.
                            //            Environment.Exit(-1);   
                        }
                        break;

                    case endSession:
                        // Send the shutdown command.
                        // Question for Alan Richardson: is it acceptable
                        // to call the Shutdown API on the Heroku-hosted
                        // API Challenges.
                        acceptHeaders.Add("application/json");

                        if (!executer.GetRequest(acceptHeaders, "shutdown"))
                        {
                            // There is a bug in shutdown on the API server:
                            // the function does not return a response
                            // to the caller.  It cuts off the network
                            // conversation before the caller gets a
                            // response, which causes an HTTP client
                            // exception at the caller.
                            //            Environment.Exit(-15);
                        }
                        break;

                    case getTodos:
                        acceptHeaders.Add("application/json");

                        // issue the GET request
                        if (!executer.GetRequest(acceptHeaders, "todos"))
                        {
                            //            Environment.Exit(-2);
                        }
                        break;

                    case headTodos:
                        acceptHeaders.Add("application/json");

                        // issue the HEAD request
                        if (!executer.HeadRequest(acceptHeaders, "todos"))
                        {
                            //           Environment.Exit(-7);
                        }
                        break;

                    case getTodoId:
                        acceptHeaders.Add("application/json");

                        // issue the GET request
                        if (!executer.GetRequest(acceptHeaders, String.Format("todos/{0}", selectedTodo)))
                        {
                            //                        Environment.Exit(-8);
                        }
                        break;

                    case headTodoId:
                        acceptHeaders.Add("application/json");

                        if (!executer.HeadRequest(acceptHeaders, String.Format("todos/{0}", selectedTodo)))
                        {
                            //           Environment.Exit(-6);
                        }
                        break;

                    case postAmendTodoId:
                        acceptHeaders.Add("application/json");

                        body = new StringContent(String.Format("{\"id\": {0}, \"title\": \"POST-amended\", \"doneStatus\": true, \"description\": \"This todo modified by POST request with Id\"}", selectedTodo), Encoding.UTF8, "application/json");

                        if (!executer.PostRequest(acceptHeaders, "todos", body))
                        {
                            //           Environment.Exit(-3);
                        }
                        break;

                    case putTodoId:
                        // modify an existing todo
                        acceptHeaders.Add("application/json");
                        body = new StringContent(String.Format("{\"id\": {0}, \"title\": \"PUT done\", \"doneStatus\": false, \"description\": \"This todo modified by PUT request\"}", selectedTodo), Encoding.UTF8, "application/json");

                        if (!executer.PutRequest(acceptHeaders, String.Format("todos/{0}", selectedTodo), body))
                        {
                            //           Environment.Exit(-4);
                        }
                        break;

                    case postMaximumTodo:
                        acceptHeaders.Add("application/json");

                        body = new StringContent("{\"title\": \"POST JSON todo and accept JSON\", \"doneStatus\": false, \"description\": \"input format was JSON, output format should be JSON\"}", Encoding.UTF8, "application/json");

                        if (!executer.PostRequest(acceptHeaders, "todos", body))
                        {
                            //           Environment.Exit(-3);
                        }
                        else
                        {
                            todosCount++;
                        }
                        break;

                    case postNetTodos:

                        break;

                    case deleteFinalTodoId:
                        acceptHeaders.Add("application/json");

                        if (!executer.DeleteRequest(acceptHeaders, String.Format("todos/{0}", selectedTodo)))
                        {
                            //                      Environment.Exit(-5);
                        }
                        else
                        {
                            todosCount--;
                        }
                        break;

                    case deleteNetTodos:
                        break;

                    case showDocs:
                        acceptHeaders.Add("application/json");

                        // issue the GET request
                        if (!executer.GetRequest(acceptHeaders, "docs"))
                        {
                            //                        Environment.Exit(-9);
                        }
                        break;

                    //case createXChallengerGuid:
                    //    // Issue the ??? request
                    //    break;

                    //case restoreChallenger:
                    //    // Issue the ??? request
                    //    break;

                    case getChallenges:
                        acceptHeaders.Add("application/json");

                        // issue the GET request
                        if (!executer.GetRequest(acceptHeaders, "challenges"))
                        {
                            //                        Environment.Exit(-2);
                        }
                        break;

                    case optionsChallenges:
                        acceptHeaders.Add("application/json");

                        // issue the GET request
                        if (!executer.OptionsRequest(acceptHeaders, "challenges"))
                        {
                            //            Environment.Exit(-10);
                        }
                        break;

                    case headChallenges:
                        acceptHeaders.Add("application/json");

                        // issue the HEAD request
                        if (!executer.HeadRequest(acceptHeaders, "challenges"))
                        {
                            //            Environment.Exit(-11);
                        }
                        break;

                    case getHeartbeat:
                        acceptHeaders.Add("application/json");

                        // issue the GET request
                        if (!executer.GetRequest(acceptHeaders, "heartbeat"))
                        {
                            //           Environment.Exit(-14);
                        }
                        break;

                    case optionsHeartbeat:
                        acceptHeaders.Add("application/json");

                        // issue the GET request
                        if (!executer.OptionsRequest(acceptHeaders, "challenges"))
                        {
                            //                       Environment.Exit(-13);
                        }
                        break;

                    case headHeartbeat:
                        acceptHeaders.Add("application/json");

                        // issue the HEAD request
                        if (!executer.HeadRequest(acceptHeaders, "heartbeat"))
                        {
                            //                        Environment.Exit(-12);
                        }
                        break;

                    case postSecretToken:
                        // Issue POST request
                        break;

                    case getSecretNote:
                        // Issue GET request
                        break;

                    case postSecretNote:
                        // Issue POST request
                        break;

                    default:
                        Console.WriteLine("ERROR: Unknown action '{0}' in GetEndState()", action);
                        break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("AdapterTransition EXCEPTION on action {0}", action);
                Console.WriteLine("start state = {0}", startState);
                Console.WriteLine("Exception: {0}", e.Message);
            }

            if (waitForObserverKeystroke)
            {
                var keyValue = Console.ReadKey();
            }

            //  - report endState to state of system under test
            //  NOTE: for shutdown, we can measure the running state but have to assume the
            //        other state values and report thus:
            //        todos.svNumTodos, xAuthToken.false, xChallengerGuid.false
            //        For all other actions,
            //
            // 1.determine running state value
            //
            // If running == true
            //   2. read the todosCount from the service
            //      set the todos class from the count
            //   3. read the xAuthToken from the service
            //      set true / false based on existence of xAuthToken
            //   4. read the xChallengerGuid from the service
            //      set true / false based on existence of xChallengerGuid
            // End if
            // Build observed state string

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
            bool inSession = vState[0].Contains("True") ? true : false;
            string todosClass = vState[1].Split(".")[1];
            bool xAuthTokenExists = vState[2].Contains("True") ? true : false;
            //bool xChallengerGuidExists = vState[3].Contains("True") ? true : false;

            if (!inSession)
            {
                actions.Add(startSession);
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
                case maximumTodos:
                    actions.Add(deleteNetTodos);
                    actions.Add(postNetTodos);
                    break;
                case someTodos:
                    actions.Add(postMaximumTodo);
                    actions.Add(postNetTodos);
                    actions.Add(deleteFinalTodoId);
                    actions.Add(deleteNetTodos);
                    break;
                default:
                    Console.WriteLine("ERROR: Unknown Todos Class '{0}' GetAvailableActions()", todosClass);
                    break;
            }

            actions.Add(endSession);
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
            actions.Add(postAmendTodoId);
            actions.Add(putTodoId);
            actions.Add(getSecretNote);
            actions.Add(postSecretNote);
            actions.Add(postSecretToken);
            //actions.Add(restoreChallenger);
            //actions.Add(createXChallengerGuid);

            return actions;
        }

        // Interface method
        public string GetEndState(string startState, string action)
        {
            // We must parse the startState, else we will 
            string[] vState = startState.Split(", ");
            bool inSession = vState[0].Contains("True") ? true : false;
            string todosClass = vState[1].Split(".")[1];
            bool xAuthTokenExists = vState[2].Contains("True") ? true : false;
            //bool xChallengerGuidExists = vState[3].Contains("True") ? true : false;

            switch (action)
            {
                case invalidRequest:
                    break;
                case startSession:
                    inSession = true;
                    break;
                case endSession:
                    // Set all state variables back to initial state on shutdown,
                    // because if the APIs server starts up again, it will take
                    // on those initial state values.
                    inSession = false;
                    todosClass = svTodosClassString;
                    //xChallengerGuidExists = svXChallengerGuidExists;
                    xAuthTokenExists = svXAuthTokenExists;
                    break;
                case getTodos:
                case headTodos:
                case getTodoId:
                case headTodoId:
                case postAmendTodoId:
                case putTodoId:
                    break;
                case postNetTodos:
                    if (todosClass == zeroTodos)
                    {
                        todosClass = someTodos;
                    }
                    break;
                case deleteNetTodos:
                    if (todosClass == maximumTodos)
                    {
                        todosClass = someTodos;
                    }
                    break;
                case deleteFinalTodoId:
                    if (todosClass == someTodos)
                    {
                        todosClass = zeroTodos;
                    }
                    break;
                case postMaximumTodo:
                    if (todosClass == someTodos)
                    {
                        todosClass = maximumTodos;
                    }
                    break;
                case showDocs:
                    break;
                //case createXChallengerGuid:
                //    xChallengerGuidExists = true;
                //    break;
                //case restoreChallenger:
                //    break;
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
            return StringifyStateVector(inSession, todosClass, xAuthTokenExists);
        }
    }
}
