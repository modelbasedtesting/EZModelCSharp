using System;
using System.Text;
using System.Net.Http;
using System.Collections.Generic;
using SeriousQualityEzModel;
using SynchronousHttpClientExecuter;

namespace TodoAPIsAdapterExecution
{
    class TodoAPIsAdapterExecutionProgram
    {
        static int Main(string[] args)
        {
            APIs client = new APIs();
            client.SelfLinkTreatment = SelfLinkTreatmentChoice.OnePerAction;
            client.IncludeSelfLinkNoise = true;

            EzModelGraph graph = new EzModelGraph(client, 300, 30, 30, EzModelGraph.LayoutRankDirection.TopDown);

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

        public APIs()
        {
            executer.server = "http://localhost:4567/";
            notifyAdapter = false;
            stopOnProblem = true;
            waitForObserverKeystroke = false;
        }

        // Initially the system is not running, and this affects a lot of
        // state.
        bool svInSession = false;

        // Reduce state explosion on todos by classifing the quantity of todos
        // into either zero or more than zero.
        enum ResolvedTodos { Zero, Some, Max };
        enum ActiveTodos { Zero, Some, Max };

        ResolvedTodos svResolvedTodos = ResolvedTodos.Zero;
        ActiveTodos svActiveTodos = ActiveTodos.Some;

        uint actionCount = 0;
        uint activeTodosCount = 10; // Default initial state due to jar file behavior.
        uint resolvedTodosCount = 0;
        uint maximumTodosCount = 14; // Arbitrary maximum
        Random random = new Random(DateTime.Now.Millisecond);

        // Actions handled by APIs
        const string startSession = "Start Session";
        const string endSession = "End Session";
        const string getTodosList = "Get Todos List";

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

        const string showDocs = "Get Documentation";
        const string getHeartbeat = "Get Service Heartbeat";

        // Actions outside of the APIs that cover legitimate REST methods
        const string invalidRequest = "invalidRequest";

        string StringifyStateVector(bool inSession, ResolvedTodos resolvedTodos, ActiveTodos activeTodos)
        {
            string s = String.Format("InSession.{0}, ResolvedTodos.{1}, ActiveTodos.{2}", inSession, resolvedTodos.ToString(), activeTodos.ToString());
            return s;
        }

        // Interface method
        public string GetInitialState()
        {
            return StringifyStateVector(svInSession, svResolvedTodos, svActiveTodos);
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
            string arg = vState[1].Split(".")[1];
            ResolvedTodos resolvedTodos = (ResolvedTodos)Enum.Parse(typeof(ResolvedTodos), arg);
            arg = vState[2].Split(".")[1];
            ActiveTodos activeTodos = (ActiveTodos)Enum.Parse(typeof(ActiveTodos), arg);

            if (!inSession)
            {
                // shut down the APIs server.  Done..
                return;
            }

            // inSession == true is implied at this point.
            // check whether the APIs server is running, and then
            // fire up the APIs server if it was not running.
            // When firing up the APIs server, wait for it to fire up
            // and then check again to see if it is running.  If it
            // is not running, fail!!  TODO: so we do need to indicate success/failure..
            // TODO: code up the running check, startup command, and secondary running check.

            switch (activeTodos)
            {
                case ActiveTodos.Zero:
                    // If the class of the system under test is already zeroTodos, do nothing.
                    // Else, drain the todos list.
                    break;
                case ActiveTodos.Some:
                    // If the class of the SUT is zeroTodos, add a todo.
                    // If the class of the SUT is maximumTodos, delete a todo.
                    // Else do nothing.
                    break;
                case ActiveTodos.Max:
                    // if the class of the SUT is not maximumTodos,
                    // add todos until we are at maximumTodos in the SUT.
                    break;
                default:
                    Console.WriteLine("Unknown activeTodos '{0}' in SetStateOfSystemUnderTest()", activeTodos.ToString());
                    break;
            }
            switch (resolvedTodos)
            {
                case ResolvedTodos.Zero:
                    // If the class of the system under test is already zeroTodos, do nothing.
                    // Else, drain the todos list.
                    break;
                case ResolvedTodos.Some:
                    // If the class of the SUT is zeroTodos, add a todo.
                    // If the class of the SUT is maximumTodos, delete a todo.
                    // Else do nothing.
                    break;
                case ResolvedTodos.Max:
                    // if the class of the SUT is not maximumTodos,
                    // add todos until we are at maximumTodos in the SUT.
                    break;
                default:
                    Console.WriteLine("Unknown resolvedTodos '{0}' in SetStateOfSystemUnderTest()", resolvedTodos.ToString());
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

            // TODO: set / confirm startState
            string[] vState = startState.Split(", ");
            bool inSession = vState[0].Contains("True") ? true : false;
            string arg = vState[1].Split(".")[1];
            ResolvedTodos resolvedTodos = (ResolvedTodos)Enum.Parse(typeof(ResolvedTodos), arg);
            arg = vState[2].Split(".")[1];
            ActiveTodos activeTodos = (ActiveTodos)Enum.Parse(typeof(ActiveTodos), arg);

        // TODO: set / confirm startState

            actionCount++;

            Console.WriteLine("****{0,6} **** {1,29} ****", actionCount, action);

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

            switch (activeTodos)
            {
                case ActiveTodos.Zero:
                    // confirm the service has zero Todos
                    break;
                case ActiveTodos.Some:
                    // confirm the service has between zero and max todos
                    break;
                case ActiveTodos.Max:
                    // confirm the service has maximum todos
                    break;
                default:
                    Console.WriteLine("ERROR: Unknown Todos Class '{0}' AdapterTransition()", activeTodos.ToString());
                    break;
            }

            switch (resolvedTodos)
            {
                case ResolvedTodos.Zero:
                    // confirm the service has zero Todos
                    break;
                case ResolvedTodos.Some:
                    // confirm the service has between zero and max todos
                    break;
                case ResolvedTodos.Max:
                    // confirm the service has maximum todos
                    break;
                default:
                    Console.WriteLine("ERROR: Unknown Todos Class '{0}' AdapterTransition()", resolvedTodos.ToString());
                    break;
            }

            // Define the acceptHeaders list just once, outside the
            // switch where they are used in several cases.
            List<string> acceptHeaders = new List<string>();
            StringContent body;
            int selectedActiveTodo = activeTodosCount > 0 ? random.Next((int)activeTodosCount) + 1 : 0;
            int selectedResolvedTodo = resolvedTodosCount > 0 ? random.Next((int)resolvedTodosCount) + 1 : 0;

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

                    case getTodosList:
                        acceptHeaders.Add("application/json");

                        // issue the GET request
                        if (!executer.GetRequest(acceptHeaders, "todos"))
                        {
                            //            Environment.Exit(-2);
                        }
                        break;

                    case editSomeTodos:
                        acceptHeaders.Add("application/json");

                        body = new StringContent(String.Format("{\"id\": {0}, \"title\": \"POST-amended\", \"doneStatus\": true, \"description\": \"This todo modified by POST request with Id\"}", selectedActiveTodo), Encoding.UTF8, "application/json");

                        if (!executer.PostRequest(acceptHeaders, "todos", body))
                        {
                            Console.WriteLine("Edit Active Todo failed");
                            Environment.Exit(-3);
                        }

                        body = new StringContent(String.Format("{\"id\": {0}, \"title\": \"PUT done\", \"doneStatus\": false, \"description\": \"This todo modified by PUT request\"}", selectedResolvedTodo), Encoding.UTF8, "application/json");

                        if (!executer.PutRequest(acceptHeaders, String.Format("todos/{0}", selectedResolvedTodo), body))
                        {
                            Console.WriteLine("Edit Resolved Todo failed");
                            Environment.Exit(-4);
                        }
                        break;

                    case addAllActiveTodos:
                        acceptHeaders.Add("application/json");

                        // TODO: for loop to top up the active todos.
                        for (var k = activeTodosCount + resolvedTodosCount; k < maximumTodosCount; k++)
                        {
                            body = new StringContent("{\"title\": \"POST JSON todo and accept JSON\", \"doneStatus\": false, \"description\": \"input format was JSON, output format should be JSON\"}", Encoding.UTF8, "application/json");

                            if (!executer.PostRequest(acceptHeaders, "todos", body))
                            {
                                Console.WriteLine("Add All Active Todos failed.");
                                Environment.Exit(-3);
                            }
                            else
                            {
                                activeTodosCount++;
                            }
                        }
                        break;

                    case addAllResolvedTodos:
                        acceptHeaders.Add("application/json");

                        // TODO: for loop to top up the active todos.
                        for (var k = activeTodosCount + resolvedTodosCount; k < maximumTodosCount; k++)
                        {
                            body = new StringContent("{\"title\": \"POST JSON todo and accept JSON\", \"doneStatus\": true, \"description\": \"input format was JSON, output format should be JSON\"}", Encoding.UTF8, "application/json");

                            if (!executer.PostRequest(acceptHeaders, "todos", body))
                            {
                                Console.WriteLine("Add All Resolved Todos failed.");
                                Environment.Exit(-23);
                            }
                            else
                            {
                                resolvedTodosCount++;
                            }
                        }
                        break;

                    case deleteAllActiveTodos:
                        acceptHeaders.Add("application/json");

                        // TODO: Get Todos List.  Then iterate
                        // on the list IDs, deleting each todo with
                        // doneStatus == false and counting down..

                        if (!executer.DeleteRequest(acceptHeaders, String.Format("todos/{0}", selectedActiveTodo)))
                        {
                            Console.WriteLine("Delete All Active Todos failed.");
                            Environment.Exit(-5);
                        }
                        else
                        {
                            activeTodosCount--;
                        }
                        break;

                    case deleteAllResolvedTodos:
                        acceptHeaders.Add("application/json");

                        // TODO: Get Todos List.  Then iterate
                        // on the list IDs, deleting each todo with
                        // doneStatus == true and counting down..

                        if (!executer.DeleteRequest(acceptHeaders, String.Format("todos/{0}", selectedResolvedTodo)))
                        {
                            Console.WriteLine("Delete All Resolved Todos failed.");
                            Environment.Exit(-25);
                        }
                        else
                        {
                            resolvedTodosCount--;
                        }
                        break;

                    case deleteSomeActiveTodos:
                        acceptHeaders.Add("application/json");

                        // TODO: Get Todos List.
                        // pick a quantity from one through activeTodosCount
                        // minus 1.  Then iterate
                        // on the list IDs, deleting each todo with
                        // doneStatus == false while counting down
                        // the quantity.

                        if (!executer.DeleteRequest(acceptHeaders, String.Format("todos/{0}", selectedActiveTodo)))
                        {
                            Console.WriteLine("Delete All Active Todos failed.");
                            Environment.Exit(-5);
                        }
                        else
                        {
                            activeTodosCount--;
                        }
                        break;

                    case deleteSomeResolvedTodos:
                        acceptHeaders.Add("application/json");

                        // TODO: Get Todos List.
                        // pick a quantity from one through resolvedTodosCount
                        // minus 1.  Then iterate
                        // on the list IDs, deleting each todo with
                        // doneStatus == true while counting down
                        // the quantity.

                        if (!executer.DeleteRequest(acceptHeaders, String.Format("todos/{0}", selectedResolvedTodo)))
                        {
                            Console.WriteLine("Delete All Resolved Todos failed.");
                            Environment.Exit(-25);
                        }
                        else
                        {
                            resolvedTodosCount--;
                        }
                        break;

                    case showDocs:
                        acceptHeaders.Add("application/json");

                        // issue the GET request
                        if (!executer.GetRequest(acceptHeaders, "docs"))
                        {
                            Console.WriteLine("Get Documentation failed.");
                            Environment.Exit(-9);
                        }
                        break;

                    case getHeartbeat:
                        acceptHeaders.Add("application/json");

                        // issue the GET request
                        if (!executer.GetRequest(acceptHeaders, "heartbeat"))
                        {
                            Console.WriteLine("Get Service Heartbeat failed.");
                            Environment.Exit(-14);
                        }
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
            string arg = vState[1].Split(".")[1];
            ResolvedTodos resolvedTodos = (ResolvedTodos)Enum.Parse(typeof(ResolvedTodos), arg);
            arg = vState[2].Split(".")[1];
            ActiveTodos activeTodos = (ActiveTodos)Enum.Parse(typeof(ActiveTodos), arg);

            if (!inSession)
            {
                actions.Add(startSession);
                return actions;
            }

            if (includeSelfLinkNoise)
            {
                if (activeTodos != ActiveTodos.Zero || resolvedTodos != ResolvedTodos.Zero)
                {
                    actions.Add(editSomeTodos);
                }
            }

            switch (activeTodos)
            {
                case ActiveTodos.Zero:
                    // This if condition is an example of
                    // the adapter execution implementation details
                    // meeting the abstractness of the test model.
                    // Without the adapter, add*ActiveTodos would
                    // be available because we don't know the actual
                    // number of active, resolved, or maximum todos
                    // during model time.  During execution, however,
                    // we keep track of the real numbers and that
                    // affects the set of actions that are really
                    // available at a particular state.
                    // During modeling we don't actually change the
                    // counts of active, resolved, or maximum todos,
                    // and we choose those initial values so the
                    // if condition is always true during modeling.
                    if (resolvedTodosCount < maximumTodosCount)
                    {
                        actions.Add(addSomeActiveTodos);
                        actions.Add(addAllActiveTodos);
                    }
                    break;
                case ActiveTodos.Some:
                    actions.Add(deleteAllTodos);
                    actions.Add(deleteAllActiveTodos);
                    actions.Add(deleteSomeActiveTodos);
                    actions.Add(resolveSomeActiveTodos);
                    actions.Add(resolveAllActiveTodos);
                    // See comment on the if condition above.  Same is true here.
                    if (activeTodosCount + resolvedTodosCount < maximumTodosCount)
                    {
                        actions.Add(addSomeActiveTodos);
                        actions.Add(addAllActiveTodos);
                    }
                    break;
                case ActiveTodos.Max:
                    actions.Add(deleteAllActiveTodos);
                    actions.Add(deleteSomeActiveTodos);
                    actions.Add(deleteAllTodos);
                    actions.Add(resolveSomeActiveTodos);
                    actions.Add(resolveAllActiveTodos);
                    break;
                default:
                    break;
            }

            switch (resolvedTodos)
            {
                case ResolvedTodos.Zero:
                    actions.Add(addSomeResolvedTodos);
                    actions.Add(addAllResolvedTodos);
                    break;
                case ResolvedTodos.Some:
                    if (!actions.Contains(deleteAllTodos))
                    {
                        actions.Add(deleteAllTodos);
                    }
                    actions.Add(addSomeResolvedTodos);
                    actions.Add(activateSomeResolvedTodos);
                    actions.Add(activateAllResolvedTodos);
                    actions.Add(deleteSomeResolvedTodos);
                    actions.Add(deleteAllResolvedTodos);
                    break;
                case ResolvedTodos.Max:
                    if (!actions.Contains(deleteAllTodos))
                    {
                        actions.Add(deleteAllTodos);
                    }
                    actions.Add(activateSomeResolvedTodos);
                    actions.Add(activateAllResolvedTodos);
                    actions.Add(deleteSomeResolvedTodos);
                    actions.Add(deleteAllResolvedTodos);
                    break;
                default:
                    break;
            }

            //            actions.Add(endSession);
            actions.Add(getTodosList);
            actions.Add(headTodos);
            actions.Add(showDocs);
            actions.Add(getHeartbeat);

            // Add an action for a class of invalid actions that extend beyond
            // specific invalid actions cited in the API Challenges list.
            actions.Add(invalidRequest);
            actions.Add(postAmendTodoId);

            return actions;
        }

        // Interface method
        public string GetEndState(string startState, string action)
        {
            string[] vState = startState.Split(", ");
            bool inSession = vState[0].Contains("True") ? true : false;
            string arg = vState[1].Split(".")[1];
            ResolvedTodos resolvedTodos = (ResolvedTodos)Enum.Parse(typeof(ResolvedTodos), arg);
            arg = vState[2].Split(".")[1];
            ActiveTodos activeTodos = (ActiveTodos)Enum.Parse(typeof(ActiveTodos), arg);

            switch (action)
            {
                case startSession:
                    inSession = true;
                    break;
                case endSession:
                    inSession = false;
                    break;
                case editSomeTodos:
                    break;
                case addSomeActiveTodos:
                    if (activeTodos == ActiveTodos.Zero)
                    {
                        activeTodos = ActiveTodos.Some;
                    }
                    break;
                case addSomeResolvedTodos:
                    if (resolvedTodos == ResolvedTodos.Zero)
                    {
                        resolvedTodos = ResolvedTodos.Some;
                    }
                    break;
                case activateSomeResolvedTodos:
                    if (activeTodos == ActiveTodos.Zero)
                    {
                        activeTodos = ActiveTodos.Some;
                    }
                    if (resolvedTodos == ResolvedTodos.Max)
                    {
                        resolvedTodos = ResolvedTodos.Some;
                    }
                    break;
                case deleteAllActiveTodos:
                    activeTodos = ActiveTodos.Zero;
                    break;
                case deleteAllTodos:
                    resolvedTodos = ResolvedTodos.Zero;
                    activeTodos = ActiveTodos.Zero;
                    break;
                case resolveSomeActiveTodos:
                    if (activeTodos == ActiveTodos.Max)
                    {
                        activeTodos = ActiveTodos.Some;
                    }
                    if (resolvedTodos == ResolvedTodos.Zero)
                    {
                        resolvedTodos = ResolvedTodos.Some;
                    }
                    break;
                case resolveAllActiveTodos:
                    activeTodos = ActiveTodos.Zero;
                    resolvedTodos = ResolvedTodos.Max;
                    break;
                case addAllActiveTodos:
                    activeTodos = ActiveTodos.Max;
                    break;
                case addAllResolvedTodos:
                    resolvedTodos = ResolvedTodos.Max;
                    break;
                case deleteAllResolvedTodos:
                    resolvedTodos = ResolvedTodos.Zero;
                    break;
                case deleteSomeResolvedTodos:
                    if (resolvedTodos == ResolvedTodos.Max)
                    {
                        resolvedTodos = ResolvedTodos.Some;
                    }
                    break;
                case activateAllResolvedTodos:
                    resolvedTodos = ResolvedTodos.Zero;
                    activeTodos = ActiveTodos.Max;
                    break;
                case invalidRequest:
                    break;
                default:
                    Console.WriteLine("ERROR: Unknown action '{0}' in GetEndState()", action);
                    break;
            }
            return StringifyStateVector(inSession, resolvedTodos, activeTodos);
        }
    }
}
