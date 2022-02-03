using System;
using System.Net.Http;
using System.Collections.Generic;
using SeriousQualityEzModel;
using SynchronousHttpClientExecuter;
using System.Text.Json;

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

            client.NotifyAdapter = true;
            // If you want stopOnProblem to stop, you need to return false from the AreStatesAcceptablySimilar method
            client.StopOnProblem = true;

            client.WaitForObserverKeystroke = false;
            graph.RandomDestinationCoverage("TodoAPIsAdapterExecution", 4);

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

        // Reduce state explosion on todos by classifing the quantity of todos
        // into either zero or more than zero.
        enum ResolvedTodos { Zero, Some, Max };
        enum ActiveTodos { Zero, Some, Max };

        // Initially the system is not running, and this affects a lot of
        // state.
        bool svInSession = false;
        bool svAtMaxTodos = false;

        ResolvedTodos svResolvedTodos = ResolvedTodos.Zero;
        ActiveTodos svActiveTodos = ActiveTodos.Some;

        uint actionCount = 0;
        uint activeTodosCount = 10; // Default initial state due to jar file behavior.
        uint resolvedTodosCount = 0;
        const uint maximumTodosCount = 14; // Arbitrary maximum
        Random random = new Random(DateTime.Now.Millisecond);

        // Actions handled by APIs
        uint addQuantity = 0;
        // addQuantity is a random number between 0 and
        // maximumTodosCount - activeTodosCount - resolvedTodosCount - 1.
        // addQuantity is the decided amount of addition change when an action is
        // addSome*, e.g., addSomeActiveTodos
        uint resolveQuantity = 0;
        // resolveQuantity is the amount active todos affected by Some-type actions.
        uint activateQuantity = 0;
        // activateQuantity is the amount resolved todos affected by Some-type actions.

        // The above numbers are needed ahead of GetEndState so that the adapter/executer
        // is guided in how much change data in the SUT.
        // The numbers is applied in GetEndState, and then after the
        // adapter/executer does work on the SUT, the SUT state can be
        // compared to the model state.
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

        const string getDocs = "Get Documentation";
        const string getHeartbeat = "Get Service Heartbeat";

        // Actions outside of the APIs that cover legitimate REST methods
        const string invalidRequest = "invalidRequest";

        string StringifyStateVector(bool inSession, ResolvedTodos resolvedTodos, ActiveTodos activeTodos, bool atMaxTodos)
        {
            string s = String.Format("InSession.{0}, ResolvedTodos.{1}, ActiveTodos.{2}, AtMaxTodos.{3}", inSession, resolvedTodos.ToString(), activeTodos.ToString(), atMaxTodos.ToString());
            return s;
        }

        // Interface method
        public string GetInitialState()
        {
            return StringifyStateVector(svInSession, svResolvedTodos, svActiveTodos, svAtMaxTodos);
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
            bool atMaxTodos = vState[3].Contains("True") ? true : false;

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
            Console.WriteLine("observed vs expected:");
            Console.WriteLine(observed);
            Console.WriteLine(expected);
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

        public class TodoItem
        {
            public int id { get; set; }
            public string title { get; set; }
            public bool doneStatus { get; set; }
            public string description { get; set; }
        }

        public class TodoItemNoId
        {
            public string title { get; set; }
            public bool doneStatus { get; set; }
            public string description { get; set; }
        }

        public class TodosList
        {
            public IList<TodoItem> todos { get; set; }
        }

        TodosList GetTodosList()
        {
            List<string> acceptHeaders = new List<string>();
            acceptHeaders.Add("application/json");

            TodosList todos = new TodosList();

            // issue the GET request
            if (!executer.GetRequest(acceptHeaders, "todos", new List<string[]>()))
            {
                Console.WriteLine("Get Todos List failed");
                Environment.Exit(-2);
            }
            try
            {
                string response = executer.responseBody;
                todos = JsonSerializer.Deserialize<TodosList>(response);
                return todos;
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception working on response for Get Todos List");
                Console.WriteLine(e.Message);
                return todos;
            }
        }

        bool compareSUTtoModelState(string prefix, bool modelInSession, TodosList sutTodos, ActiveTodos modelActiveTodos, ResolvedTodos modelResolvedTodos, bool modelAtMaxTodos)
        {
            bool comparable = true;
            int SUTactiveTodosCount = 0;
            int SUTresolvedTodosCount = 0;

            // IN CASE THE ADAPTER FINDS A DISCREPANCY BETWEEN THE SUT STATE
            // AND THE STARTSTATE ARGUMENT, OUTPUT A NOTICE.  CONTINUE RUNNING.
            if (modelInSession)
            {
                // confirm the service is running
                // for Java, look for the pid or process status.

            }
            else
            {
                // confirm the service is not running
            }

            if (modelInSession)
            {
                foreach (TodoItem t in sutTodos.todos)
                {
                    if (t.doneStatus)
                    {
                        SUTresolvedTodosCount++;
                    }
                    else
                    {
                        SUTactiveTodosCount++;
                    }
                }

                Console.WriteLine("{4} SUT:model todo count {0}:{1} active, {2}:{3} resolved", SUTactiveTodosCount, activeTodosCount, SUTresolvedTodosCount, resolvedTodosCount, prefix);

                if (activeTodosCount != SUTactiveTodosCount)
                {
                    Console.WriteLine("PROBLEM: SUT {0} and model {1} active todos count differ.", SUTactiveTodosCount, activeTodosCount);
                    comparable = false;
                }

                if (resolvedTodosCount != SUTresolvedTodosCount)
                {
                    Console.WriteLine("PROBLEM: SUT {0} and model {1} resolved todos count differ.", SUTresolvedTodosCount, resolvedTodosCount);
                    comparable = false;
                }

                switch (modelActiveTodos)
                {
                    case ActiveTodos.Zero:
                        if (activeTodosCount != 0)
                        {
                            Console.WriteLine("MODEL ERROR: Active todos category is Zero when count is {0}", activeTodosCount);
                            comparable = false;
                        }
                        break;
                    case ActiveTodos.Some:
                        if (activeTodosCount < 1)
                        {
                            Console.WriteLine("MODEL ERROR: Active todos category is Some when counts are active {0}, resolved {1}, max {2}", activeTodosCount, resolvedTodosCount, maximumTodosCount);
                            comparable = false;
                        }
                        break;
                    case ActiveTodos.Max:
                        if (activeTodosCount + resolvedTodosCount != maximumTodosCount)
                        {
                            Console.WriteLine("MODEL ERROR: Active todos category is Max when counts are max {0} vs active {1} + resolved {2}", maximumTodosCount, activeTodosCount, resolvedTodosCount);
                            comparable = false;
                        }
                        break;
                    default:
                        Console.WriteLine("ERROR: Unknown Todos Class '{0}' AdapterTransition()", modelActiveTodos.ToString());
                        break;
                }

                switch (modelResolvedTodos)
                {
                    case ResolvedTodos.Zero:
                        if (resolvedTodosCount != 0)
                        {
                            Console.WriteLine("MODEL ERROR: Resolved todos category is Zero when count is {0}", resolvedTodosCount);
                            comparable = false;
                        }
                        break;
                    case ResolvedTodos.Some:
                        if (resolvedTodosCount < 1)
                        {
                            Console.WriteLine("MODEL ERROR: Resolved todos category is Some when counts are active {0}, resolved {1}, max {2}", activeTodosCount, resolvedTodosCount, maximumTodosCount);
                            comparable = false;
                        }
                        break;
                    case ResolvedTodos.Max:
                        if (resolvedTodosCount + activeTodosCount != maximumTodosCount)
                        {
                            Console.WriteLine("MODEL ERROR: Resolved todos category is Max when counts are max {0} vs active {1} + resolved {2}", maximumTodosCount, activeTodosCount, resolvedTodosCount);
                            comparable = false;
                        }
                        break;
                    default:
                        Console.WriteLine("ERROR: Unknown Todos Class '{0}' AdapterTransition()", modelResolvedTodos.ToString());
                        break;
                }
            }
            return comparable;
        }

        // Interface method
        public string AdapterTransition(string startState, string action)
        {
            string observed = "";

            string[] vState = startState.Split(", ");
            bool inSessionStart = vState[0].Contains("True") ? true : false;
            string arg = vState[1].Split(".")[1];
            ResolvedTodos resolvedStart = (ResolvedTodos)Enum.Parse(typeof(ResolvedTodos), arg);
            arg = vState[2].Split(".")[1];
            ActiveTodos activeStart = (ActiveTodos)Enum.Parse(typeof(ActiveTodos), arg);
            bool atMaxStart = vState[3].Contains("True") ? true : false;

            actionCount++;

            Console.WriteLine("{2} {3} <<<<{0,6} **** {1,30} ****", actionCount, action, activeStart, resolvedStart);

            TodosList sutTodos = inSessionStart ? GetTodosList() : new TodosList();

            if (!compareSUTtoModelState("BEFORE", inSessionStart, sutTodos, activeStart, resolvedStart, atMaxStart))
            {
                Console.WriteLine("Stopping test run: SUT and Model states differ before {0}", action);
                Environment.Exit(-100);
            }

            // Define the acceptHeaders list just once, outside the
            // switch where they are used in several cases.
            List<string> acceptHeaders = new List<string>();
            StringContent body;
            string response = String.Empty;
            TodoItem todo = new TodoItem();
            TodoItemNoId todoNoId = new TodoItemNoId();
            uint quantity;

            // We need to know the start state abstract values to decide how to
            // constrain the addQuantity, resolveQuantity, and activateQuantity.
            string endState = GetEndState(startState, action);
            vState = endState.Split(", ");
            bool inSessionEnd = vState[0].Contains("True") ? true : false;
            arg = vState[1].Split(".")[1];
            ResolvedTodos resolvedEnd = (ResolvedTodos)Enum.Parse(typeof(ResolvedTodos), arg);
            arg = vState[2].Split(".")[1];
            ActiveTodos activeEnd = (ActiveTodos)Enum.Parse(typeof(ActiveTodos), arg);
            bool atMaxEnd = vState[3].Contains("True") ? true : false;


            uint headroom = maximumTodosCount - activeTodosCount - resolvedTodosCount;
            addQuantity = headroom < 2 ? 0 : (uint)random.Next((int)headroom - 2) + 1;
            resolveQuantity = activeTodosCount < 2 ? 0 : (uint)random.Next((int)activeTodosCount - 2) + 1;
            activateQuantity = resolvedTodosCount < 2 ? 0 : (uint)random.Next((int)resolvedTodosCount - 2) + 1;

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
                        bool started = executer.Startup("/Users/dougs/Downloads/", "java", "-jar apichallenges.jar");
                        if (!started)
                        {
                            // We couldn't start the APIs server.  Stop the test run.
                            // The exit code of -1 indicates abnormal termination,
                            // and in this case it is because we couldn't start the
                            // APIs server.
                            Console.WriteLine("Start Session failed");
                            Environment.Exit(-1);
                        }
                        break;

                    case endSession:
                        // Send the shutdown command.
                        // Question for Alan Richardson: is it acceptable
                        // to call the Shutdown API on the Heroku-hosted
                        // API Challenges.
                        acceptHeaders.Add("application/json");

                        if (!executer.GetRequest(acceptHeaders, "shutdown", new List<string[]>()))
                        {
                            // There is a bug in shutdown on the API server:
                            // the function does not return a response
                            // to the caller.  It cuts off the network
                            // conversation before the caller gets a
                            // response, which causes an HTTP client
                            // exception at the caller.
                            //            Environment.Exit(-15);
                        }
                        inSessionStart = false;
                        break;

                    case getTodosList:
                        // already done before coming into this switch statement..
                        break;

                    case editSomeTodos:
                        // Don't resolve or activate as part of edit, because that
                        // could change the size category of the active or resolved
                        // todos.  That is, you could go from zero to some, or from
                        // max to some, and that would not match the model.
                        acceptHeaders.Add("application/json");

                        bool postThisEdit = false;

                        quantity = (uint)random.Next((int)(activeTodosCount + resolvedTodosCount));

                        foreach( TodoItem t in sutTodos.todos )
                        {
                            postThisEdit = !postThisEdit;
                            if (quantity == 0)
                            {
                                break;
                            }
                            todo.id = t.id;
                            todo.title = actionCount.ToString() + "editSomeTodos";
                            todo.description = (postThisEdit ? "POST;" : "PUT;") + t.description;
                            todo.doneStatus = t.doneStatus;
                            quantity--;
                            Console.Write("-");

                            body = new StringContent(JsonSerializer.Serialize(todo));

                            if (postThisEdit)
                            {
                                if (!executer.PostRequest(acceptHeaders, String.Format("todos/{0}", t.id), body, new List<string[]>()))
                                {
                                    Console.WriteLine("Edit Some Todos failed on POST {0}", t.id);
                                    Environment.Exit(-3);
                                }
                            }
                            else
                            {
                                if (!executer.PutRequest(acceptHeaders, String.Format("todos/{0}", t.id), body))
                                {
                                    Console.WriteLine("Edit Some Todos failed on PUT {0}", t.id);
                                    Environment.Exit(-33);
                                }
                            }
                        }
                        Console.WriteLine(" ");
                        break;

                    case addSomeActiveTodos:
                        if (atMaxStart)
                        {
                            Console.WriteLine("ERROR: addSomeActiveTodos is atMaxStart");
                            break;
                        }

                        addQuantity = headroom == 1 ? 0 : (uint)random.Next((int)(headroom - 2) + 1);

                        if (activeTodosCount == 0 && addQuantity == 0)
                        {
                            addQuantity = 1; // Necessary to get to model end state.
                        }

                        Console.WriteLine("headroom = {1}, addQuantity = {0}", addQuantity, headroom);

                        acceptHeaders.Add("application/json");

                        for (var k = 0; k < addQuantity; k++)
                        {
                            todoNoId.title = actionCount.ToString() + "AddSomeActiveTodos";
                            todoNoId.doneStatus = false;
                            todoNoId.description = "Created by POST method";
                            body = new StringContent(JsonSerializer.Serialize(todoNoId));

                            if (!executer.PostRequest(acceptHeaders, "todos", body, new List<string[]>()))
                            {
                                Console.WriteLine("Add Some Active Todos failed.");
                                Environment.Exit(-7);
                            }
                            Console.Write("+");
                        }
                        Console.WriteLine(" ");
                        activeTodosCount += addQuantity;
                        break;

                    case addSomeResolvedTodos:
                        if (atMaxStart)
                        {
                            Console.WriteLine("ERROR: addSomeResolvedTodos is atMaxStart");
                            break;
                        }

                        addQuantity = headroom == 1 ? 0 : (uint)random.Next((int)(headroom - 2) + 1);

                        if (resolvedTodosCount == 0 && addQuantity == 0)
                        {
                            addQuantity = 1; // Necessary to get to model end state.
                        }

                        Console.WriteLine("headroom = {1}, addQuantity = {0}", addQuantity, headroom);

                        acceptHeaders.Add("application/json");

                        for (var k = 0; k < addQuantity; k++)
                        {
                            todoNoId.title = actionCount.ToString() + "AddSomeResolvedTodos";
                            todoNoId.doneStatus = true;
                            todoNoId.description = "Created by POST method";
                            body = new StringContent(JsonSerializer.Serialize(todoNoId));

                            if (!executer.PostRequest(acceptHeaders, "todos", body, new List<string[]>()))
                            {
                                Console.WriteLine("Add Some Resolved Todos failed.");
                                Environment.Exit(-27);
                            }
                            Console.Write("+");
                        }
                        Console.WriteLine(" ");
                        resolvedTodosCount += addQuantity;
                        break;

                    case addAllActiveTodos:
                        acceptHeaders.Add("application/json");

                        for (var k = 0; k < headroom; k++)
                        {
                            todoNoId.title = actionCount.ToString() + "AddAllActiveTodos";
                            todoNoId.doneStatus = false;
                            todoNoId.description = "Created by POST method";
                            body = new StringContent(JsonSerializer.Serialize(todoNoId));

                            if (!executer.PostRequest(acceptHeaders, "todos", body, new List<string[]>()))
                            {
                                Console.WriteLine("Add All Active Todos failed.");
                                Environment.Exit(-3);
                            }
                            Console.Write("+");
                        }
                        Console.WriteLine(" ");
                        activeTodosCount += headroom;
                        break;

                    case addAllResolvedTodos:
                        acceptHeaders.Add("application/json");

                        for (var k = 0; k < headroom; k++)
                        {
                            todoNoId.title = actionCount.ToString() + "AddAllResolvedTodos";
                            todoNoId.doneStatus = true;
                            todoNoId.description = "Created by POST method";
                            body = new StringContent(JsonSerializer.Serialize(todoNoId));

                            if (!executer.PostRequest(acceptHeaders, "todos", body, new List<string[]>()))
                            {
                                Console.WriteLine("Add All Resolved Todos failed.");
                                Environment.Exit(-23);
                            }
                            Console.Write("+");
                        }
                        Console.WriteLine(" ");
                        resolvedTodosCount += headroom;
                        break;

                    case deleteAllActiveTodos:
                        acceptHeaders.Add("application/json");

                        foreach (TodoItem t in sutTodos.todos)
                        {
                            if (!t.doneStatus)
                            {
                                if (!executer.DeleteRequest(acceptHeaders, String.Format("todos/{0}", t.id)))
                                {
                                    Console.WriteLine("Delete All Active Todos failed.");
                                    Environment.Exit(-5);
                                }
                                Console.Write("-");
                            }
                        }
                        Console.WriteLine(" ");
                        activeTodosCount = 0;
                        break;

                    case deleteAllResolvedTodos:
                        acceptHeaders.Add("application/json");

                        foreach (TodoItem t in sutTodos.todos)
                        {
                            if (t.doneStatus)
                            {
                                if (!executer.DeleteRequest(acceptHeaders, String.Format("todos/{0}", t.id)))
                                {
                                    Console.WriteLine("Delete All Resolved Todos failed.");
                                    Environment.Exit(-25);
                                }
                                Console.Write("-");
                            }
                        }
                        Console.WriteLine(" ");
                        resolvedTodosCount = 0;
                        break;

                    case deleteAllTodos:
                        acceptHeaders.Add("application/json");
                        foreach (TodoItem t in sutTodos.todos)
                        {
                            if (!executer.DeleteRequest(acceptHeaders, String.Format("todos/{0}", t.id)))
                            {
                                Console.WriteLine("Delete All Todos failed.");
                                Environment.Exit(-24);
                            }
                            Console.Write("-");
                        }
                        Console.WriteLine(" ");
                        activeTodosCount = 0;
                        resolvedTodosCount = 0;
                        break;

                    case deleteSomeActiveTodos:
                        acceptHeaders.Add("application/json");

                        if (activeTodosCount == 1)
                        {
                            if (atMaxStart)
                            { // We need to delete a resolved todo so that activeTodos and resolvedTodos
                                // both fit the Some category.
                                Console.WriteLine("Deleting a resolved todo in deleteSomeActiveTodos to reach correct abstract end state");

                                foreach (TodoItem t in sutTodos.todos)
                                {
                                    if (!t.doneStatus)
                                    {
                                        continue;
                                    }
                                    if (!executer.DeleteRequest(acceptHeaders, String.Format("todos/{0}", t.id)))
                                    {
                                        Console.WriteLine("Delete Some Active Todos failed.");
                                        Environment.Exit(-17);
                                    }
                                    Console.WriteLine("X");
                                    resolvedTodosCount--;
                                    break; // we only need to delete one..
                                }
                            }
                            break;
                        }

                        quantity = (uint)random.Next((int)activeTodosCount - 2) + 1;
                        activeTodosCount -= quantity;

                        foreach (TodoItem t in sutTodos.todos)
                        {
                            if (t.doneStatus)
                            {
                                continue;
                            }
                            if (!executer.DeleteRequest(acceptHeaders, String.Format("todos/{0}", t.id)))
                            {
                                Console.WriteLine("Delete Some Active Todos failed.");
                                Environment.Exit(-16);
                            }
                            quantity--;
                            Console.Write("-");
                            if (quantity == 0)
                            {
                                break;
                            }
                        }
                        Console.WriteLine(" ");
                        break;

                    case deleteSomeResolvedTodos:
                        acceptHeaders.Add("application/json");

                        if (resolvedTodosCount == 1)
                        {
                            if (atMaxStart)
                            { // We need to delete an active todo so that activeTodos and resolvedTodos
                                // both fit the Some category.
                                Console.WriteLine("Deleting an active todo in deleteSomeResolvedTodos to reach correct abstract end state");

                                foreach (TodoItem t in sutTodos.todos)
                                {
                                    if (t.doneStatus)
                                    {
                                        continue;
                                    }
                                    if (!executer.DeleteRequest(acceptHeaders, String.Format("todos/{0}", t.id)))
                                    {
                                        Console.WriteLine("Delete Some Resolved Todos failed.");
                                        Environment.Exit(-19);
                                    }
                                    Console.WriteLine("X");
                                    activeTodosCount--;
                                    break; // we only need to delete one..
                                }
                            }
                            break;
                        }

                        quantity = (uint)random.Next((int)resolvedTodosCount - 2) + 1;
                        resolvedTodosCount -= quantity;

                        foreach (TodoItem t in sutTodos.todos)
                        {
                            if (!t.doneStatus)
                            {
                                continue;
                            }
                            if (!executer.DeleteRequest(acceptHeaders, String.Format("todos/{0}", t.id)))
                            {
                                Console.WriteLine("Delete Some Resolved Todos failed.");
                                Environment.Exit(-25);
                            }
                            quantity--;
                            Console.Write("-");
                            if (quantity == 0)
                            {
                                break;
                            }
                        }
                        Console.WriteLine(" ");
                        break;

                    case resolveSomeActiveTodos:
                        acceptHeaders.Add("application/json");
                        // If activeTodosCount == 1 and ResolvedTodosCount == 0
                        // we must add a resolved todo to get to the right end state
                        if (activeTodosCount == 1 && resolvedTodosCount == 0)
                        {
                            Console.WriteLine("Adding a resolved todo in resolveSomeActiveTodos to achieve correct end state.");
                            todoNoId.title = actionCount.ToString() + "ResolveSomeActiveTodos";
                            todoNoId.doneStatus = true;
                            todoNoId.description = "Created by POST method";
                            body = new StringContent(JsonSerializer.Serialize(todoNoId));

                            if (!executer.PostRequest(acceptHeaders, "todos", body, new List<string[]>()))
                            {
                                Console.WriteLine("Resolve Some Active Todos failed.");
                                Environment.Exit(-28);
                            }
                            Console.WriteLine("+");
                            resolvedTodosCount++;
                            break;
                        }

                        if (activeTodosCount == 1)
                        {
                            break;
                        }

                        quantity = (uint)random.Next((int)activeTodosCount - 2) + 1;
                        activeTodosCount -= quantity;
                        resolvedTodosCount += quantity;

                        foreach (TodoItem t in sutTodos.todos)
                        {
                            if (!t.doneStatus)
                            {
                                todoNoId.doneStatus = true;
                                todoNoId.title = t.title;
                                todoNoId.description = t.description;
                                body = new StringContent(JsonSerializer.Serialize(todoNoId));
                                if (!executer.PutRequest(acceptHeaders, String.Format("todos/{0}", t.id), body))
                                {
                                    Console.WriteLine("Resolve Some Active Todos Failed");
                                    Environment.Exit(-11);
                                }
                                quantity--;
                                Console.Write("*");
                                if (quantity == 0)
                                {
                                    break;
                                }
                            }
                        }
                        Console.WriteLine(" ");
                        break;

                    case activateSomeResolvedTodos:
                        acceptHeaders.Add("application/json");
                        // If activeTodosCount == 0 and ResolvedTodosCount == 1
                        // we must add an active todo to get to the right end state
                        if (activeTodosCount == 0 && resolvedTodosCount == 1)
                        {
                            Console.WriteLine("Adding an active todo in activateSomeResolvedTodos to achieve correct end state.");
                            todoNoId.title = actionCount.ToString() + "ActivateSomeResolvedTodos";
                            todoNoId.doneStatus = false;
                            todoNoId.description = "Created by POST method";
                            body = new StringContent(JsonSerializer.Serialize(todoNoId));

                            if (!executer.PostRequest(acceptHeaders, "todos", body, new List<string[]>()))
                            {
                                Console.WriteLine("Activate Some Resolved Todos failed.");
                                Environment.Exit(-29);
                            }
                            Console.WriteLine("+");
                            activeTodosCount++;
                            break;
                        }

                        if (resolvedTodosCount == 1)
                        {
                            break;
                        }

                        quantity = (uint)random.Next((int)resolvedTodosCount - 2) + 1;
                        resolvedTodosCount -= quantity;
                        activeTodosCount += quantity;

                        foreach (TodoItem t in sutTodos.todos)
                        {
                            if (t.doneStatus)
                            {
                                todoNoId.doneStatus = false;
                                todoNoId.title = t.title;
                                todoNoId.description = t.description;
                                body = new StringContent(JsonSerializer.Serialize(todoNoId));
                                if (!executer.PutRequest(acceptHeaders, String.Format("todos/{0}", t.id), body))
                                {
                                    Console.WriteLine("Activate Some Resolved Todos Failed");
                                    Environment.Exit(-21);
                                }
                                quantity--;
                                Console.Write("*");
                                if (quantity == 0)
                                {
                                    break;
                                }
                            }
                        }
                        Console.WriteLine(" ");
                        break;

                    case resolveAllActiveTodos:
                        acceptHeaders.Add("application/json");

                        resolvedTodosCount += activeTodosCount;
                        activeTodosCount = 0;

                        foreach (TodoItem t in sutTodos.todos)
                        {
                            if (!t.doneStatus)
                            {
                                todoNoId.doneStatus = true;
                                todoNoId.title = t.title;
                                todoNoId.description = t.description;
                                body = new StringContent(JsonSerializer.Serialize(todoNoId));
                                if (!executer.PutRequest(acceptHeaders, String.Format("todos/{0}", t.id), body))
                                {
                                    Console.WriteLine("Resolve All Resolved Todos Failed");
                                    Environment.Exit(-32);
                                }
                            }
                        }
                        break;

                    case activateAllResolvedTodos:
                        acceptHeaders.Add("application/json");

                        activeTodosCount += resolvedTodosCount;
                        resolvedTodosCount = 0;

                        foreach (TodoItem t in sutTodos.todos)
                        {
                            if (t.doneStatus)
                            {
                                todoNoId.doneStatus = false;
                                todoNoId.title = t.title;
                                todoNoId.description = t.description;
                                body = new StringContent(JsonSerializer.Serialize(todoNoId));
                                if (!executer.PutRequest(acceptHeaders, String.Format("todos/{0}", t.id), body))
                                {
                                    Console.WriteLine("Activate All Resolved Todos Failed");
                                    Environment.Exit(-22);
                                }
                            }
                        }
                        break;

                    case getDocs:
                        acceptHeaders.Add("application/json");

                        // issue the GET request
                        if (!executer.GetRequest(acceptHeaders, "docs", new List<string[]>()))
                        {
                            Console.WriteLine("Get Documentation failed.");
                            Environment.Exit(-9);
                        }
                        break;

                    case getHeartbeat:
                        acceptHeaders.Add("application/json");

                        // issue the GET request
                        if (!executer.GetRequest(acceptHeaders, "heartbeat", new List<string[]>()))
                        {
                            Console.WriteLine("Get Service Heartbeat failed.");
                            Environment.Exit(-14);
                        }
                        break;

                    default:
                        Console.WriteLine("ERROR: Unknown action '{0}' in AdapterTransition()", action);
                        break;
                }

                sutTodos = inSessionEnd ? GetTodosList() : new TodosList();

                if (!compareSUTtoModelState("AFTER", inSessionEnd, sutTodos, activeEnd, resolvedEnd, atMaxEnd))
                {
                    Console.WriteLine("Stopping test run: SUT and Model states differ after {0}", action);
                    Environment.Exit(-101);
                }
                Console.WriteLine("****{0,6} **** {1,30} >>>> {2} {3}", actionCount, action, activeEnd, resolvedEnd);

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
            else
            {
                System.Threading.Thread.Sleep(20);
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
            if (!inSession)
            {
                actions.Add(startSession);
                return actions;
            }

            string arg = vState[1].Split(".")[1];
            ResolvedTodos resolvedTodos = (ResolvedTodos)Enum.Parse(typeof(ResolvedTodos), arg);
            arg = vState[2].Split(".")[1];
            ActiveTodos activeTodos = (ActiveTodos)Enum.Parse(typeof(ActiveTodos), arg);
            bool atMaxTodos = vState[3].Contains("True") ? true : false;

            actions.Add(getTodosList);
            actions.Add(getDocs);
            actions.Add(getHeartbeat);

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
                    if (!atMaxTodos)
                    {
                        actions.Add(addAllActiveTodos);
                        actions.Add(addSomeActiveTodos);
                    }
                    break;
                case ActiveTodos.Some:
                    actions.Add(deleteAllTodos);
                    actions.Add(deleteAllActiveTodos);
                    actions.Add(deleteSomeActiveTodos);
                    actions.Add(resolveSomeActiveTodos);
                    actions.Add(resolveAllActiveTodos);
                    // See comment on the if condition above.  Same is true here.
                    if (!atMaxTodos)
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
                    if (!atMaxTodos)
                    {
                        actions.Add(addSomeResolvedTodos);
                        actions.Add(addAllResolvedTodos);
                    }
                    break;
                case ResolvedTodos.Some:
                    if (!actions.Contains(deleteAllTodos))
                    {
                        actions.Add(deleteAllTodos);
                    }
                    if (!atMaxTodos)
                    {
                        actions.Add(addSomeResolvedTodos);
                        actions.Add(addAllResolvedTodos);
                    }
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

            // Add an action for a class of invalid actions that extend beyond
            // specific invalid actions cited in the API Challenges list.
//            actions.Add(invalidRequest);

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
            bool atMaxTodos = vState[3].Contains("True") ? true : false;

            switch (action)
            {
                case startSession:
                    inSession = true;
                    break;
                case endSession:
                    inSession = false;
                    break;
                case addSomeResolvedTodos:
                    resolvedTodos = ResolvedTodos.Some;
                    atMaxTodos = false;
                    break;
                case addSomeActiveTodos:
                    activeTodos = ActiveTodos.Some;
                    atMaxTodos = false;
                    break;
                case addAllResolvedTodos:
                    resolvedTodos = ResolvedTodos.Max;
                    atMaxTodos = true;
                    break;
                case addAllActiveTodos:
                    activeTodos = ActiveTodos.Max;
                    atMaxTodos = true;
                    break;
                case editSomeTodos:                        
                    break;
                case activateSomeResolvedTodos:
                    if (!(atMaxTodos && activeTodos == ActiveTodos.Max))
                    {
                        if (activeTodos == ActiveTodos.Zero)
                        {
                            activeTodos = ActiveTodos.Some;
                        }
                    }
                    resolvedTodos = ResolvedTodos.Some;
                    break;
                case resolveSomeActiveTodos:
                    if (!(atMaxTodos && resolvedTodos == ResolvedTodos.Max))
                    {
                        if (resolvedTodos == ResolvedTodos.Zero)
                        {
                            resolvedTodos = ResolvedTodos.Some;
                        }
                    }
                    activeTodos = ActiveTodos.Some;
                    break;
                case activateAllResolvedTodos:
                    resolvedTodos = ResolvedTodos.Zero;
                    activeTodos = atMaxTodos ? ActiveTodos.Max : ActiveTodos.Some;
                    break;
                case resolveAllActiveTodos:
                    activeTodos = ActiveTodos.Zero;
                    resolvedTodos = atMaxTodos ? ResolvedTodos.Max : ResolvedTodos.Some;
                    break;
                case deleteSomeResolvedTodos:
                    resolvedTodos = ResolvedTodos.Some;
                    activeTodos = activeTodos == ActiveTodos.Zero ? ActiveTodos.Zero : ActiveTodos.Some;
                    atMaxTodos = false;
                    break;
                case deleteSomeActiveTodos:
                    activeTodos = ActiveTodos.Some;
                    resolvedTodos = resolvedTodos == ResolvedTodos.Zero ? ResolvedTodos.Zero : ResolvedTodos.Some;
                    atMaxTodos = false;
                    break;
                case deleteAllResolvedTodos:
                    resolvedTodos = ResolvedTodos.Zero;
                    activeTodos = activeTodos == ActiveTodos.Zero ? ActiveTodos.Zero : ActiveTodos.Some;
                    atMaxTodos = false;
                    break;
                case deleteAllActiveTodos:
                    activeTodos = ActiveTodos.Zero;
                    resolvedTodos = resolvedTodos == ResolvedTodos.Zero ? ResolvedTodos.Zero : ResolvedTodos.Some;
                    atMaxTodos = false;
                    break;
                case deleteAllTodos:
                    resolvedTodos = ResolvedTodos.Zero;
                    activeTodos = ActiveTodos.Zero;
                    atMaxTodos = false;
                    break;
                case invalidRequest:
                    break;
                case getTodosList:
                    break;
                case getDocs:
                    break;
                case getHeartbeat:
                    break;
                default:
                    Console.WriteLine("ERROR: Unknown action '{0}' in GetEndState()", action);
                    break;
            }
            return StringifyStateVector(inSession, resolvedTodos, activeTodos, atMaxTodos);
        }
    }
}
