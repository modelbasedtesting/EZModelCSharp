using System;
using System.Net.Http;
using System.Collections.Generic;
using SeriousQualityEzModel;
using SynchronousHttpClientExecuter;
using System.Text.Json;

namespace TodoAPIsNaiveAdapter
{
    class TodoAPIsNaiveAdapterProgram
    {
        static int Main(string[] args)
        {
            APIs client = new APIs();
            client.SelfLinkTreatment = SelfLinkTreatmentChoice.OnePerAction;
            client.IncludeSelfLinkNoise = true;
            client.AbstractTheState = true;

            EzModelGraph graph = new EzModelGraph(client, 2000, 300, 30, EzModelGraph.LayoutRankDirection.TopDown);

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

            client.NotifyAdapter = false;
            // If you want stopOnProblem to stop, you need to return false from the AreStatesAcceptablySimilar method
            client.StopOnProblem = true;

            client.WaitForObserverKeystroke = true;
            graph.RandomDestinationCoverage("TodoAPIsNaiveAdapter", 2);

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

        public bool AbstractTheState = false;

        // Initially the system is not running, and this affects a lot of
        // state.
        // When AbstractTheState is true, we pass the following state values to EzModel
        // for the Todos.
        //        string[] abstractStates = { "Zero", "One", "1 < Todos < Max-1", "Max-1", "Max" };
        // abstractStates is bigger when the resolved and active todos are kept track of.
        static string[] abstractState = {
            "Active.0, Resolved.0, MaxTodos.False", 
            "Active.0, Resolved.1, MaxTodos.False", 
            "Active.0, Resolved.>1, MaxTodos.False", 
            "Active.1, Resolved.0, MaxTodos.False", 
            "Active.>1, Resolved.0, MaxTodos.False",
            "Active.1, Resolved.1, MaxTodos.False",
            "Active.1, Resolved.>1, MaxTodos.False",
            "Active.>1, Resolved.1, MaxTodos.False",
            "Active.>1, Resolved.>1, MaxTodos.False",
            "Active.Max-1, Resolved.0, MaxTodos.False", 
            "Active.0, Resolved.Max-1, MaxTodos.False",
            "Active.0, Resolved.Max, MaxTodos.True", 
            "Active.1, Resolved.Max-1, MaxTodos.True", 
            "Active.Max, Resolved.0, MaxTodos.True", 
            "Active.Max-1, Resolved.1, MaxTodos.True", 
            "Active.>1, Resolved.>1, MaxTodos.True"};

        string abstractTodos = abstractState[4];

        bool svInSession = false;
        uint svResolvedTodos = 0; 
        uint svActiveTodos = 3;

        uint activeTodosCount;
        uint resolvedTodosCount;
        uint actionCount = 0;
        const uint maximumTodosCount = 3; // Arbitrary maximum
        Random random = new Random(DateTime.Now.Millisecond);

        const string startSession = "Start Session";
        const string endSession = "End Session";
        const string getTodosList = "Get Todos List";

        const string addActiveTodo = "Add an Active Todo";
        const string addMaximumActiveTodo = "Add Maximum Active Todo";
        const string addResolvedTodo = "Add a Resolved Todo";
        const string addMaximumResolvedTodo = "Add Maximum Resolved Todo";
        const string editTodo = "Edit a Todo";
        const string deleteResolvedTodo = "Delete a Resolved Todo";
        const string deleteFinalResolvedTodo = "Delete Final Resolved Todo";
        const string deleteActiveTodo = "Delete an Active Todo";
        const string deleteFinalActiveTodo = "Delete Final Active Todo";
        const string resolveActiveTodo = "Resolve an Active Todo";
        const string resolveFinalActiveTodo = "Resolve Final Active Todo";
        const string activateResolvedTodo = "Activate a Resolved Todo";
        const string activateFinalResolvedTodo = "Activate Final Resolved Todo";

        const string getDocs = "Get Documentation";
        const string getHeartbeat = "Get Service Heartbeat";

        // Actions outside of the APIs that cover legitimate REST methods
        const string invalidRequest = "invalidRequest";

        string StringifyStateVector(bool inSession)
        {
            string s = String.Format("InSession.{0}, Resolved.{1}, Active.{2}", inSession, resolvedTodosCount, activeTodosCount);
            return s;
        }

        // Interface method
        public string GetInitialState()
        {
            activeTodosCount = svActiveTodos;
            resolvedTodosCount = svResolvedTodos;

            if (AbstractTheState)
            {
                return String.Format("InSession.{0}, {1}", svInSession, abstractState[4]);
            }
            return StringifyStateVector(svInSession);
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

            if (AbstractTheState)
            {
                abstractTodos = vState[1].Split(".")[1];
            }
            else
            {
                resolvedTodosCount = uint.Parse(vState[1].Split(".")[1]);
                activeTodosCount = uint.Parse(vState[2].Split(".")[1]);
            }

            if (inSession)
            {
                // fire up the APIs server
            }
            else
            { 
                // shut down the APIs server.  Done..
                return;
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
            if (!executer.GetRequest(acceptHeaders, "todos"))
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

        bool compareSUTtoModelState(string prefix, bool inSession, TodosList sutTodos)
        {
            bool comparable = true;
            int SUTactiveTodosCount = 0;
            int SUTresolvedTodosCount = 0;

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

            if (inSession)
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
            }
            return comparable;
        }

        // Interface method
        public string AdapterTransition(string startState, string action)
        {
            string observed = "";

            string[] vState = startState.Split(", ");
            bool inSession = vState[0].Contains("True") ? true : false;
            if (AbstractTheState)
            {
                abstractTodos = vState[1].Split(".")[1];
            }
            else
            {
                resolvedTodosCount = uint.Parse(vState[1].Split(".")[1]);
                activeTodosCount = uint.Parse(vState[2].Split(".")[1]);
            }
            TodoItem t;

            actionCount++;

            Console.WriteLine("{2} {3} <<<<{0,6} **** {1,30} ****", actionCount, action, activeTodosCount, resolvedTodosCount);

            TodosList sutTodos = inSession ? GetTodosList() : new TodosList();

            if (!compareSUTtoModelState("BEFORE", inSession, sutTodos))
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
            int index;
            uint headroom = maximumTodosCount - activeTodosCount - resolvedTodosCount;

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
                            Console.WriteLine("Start Session failed");
                            Environment.Exit(-1);
                        }
                        inSession = true;
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
                        inSession = false;
                        break;

                    case getTodosList:
                        // already done before coming into this switch statement..
                        break;

                    case editTodo:
                        // Don't resolve or activate as part of edit, because that
                        // could change the size category of the active or resolved
                        // todos.  That is, you could go from zero to some, or from
                        // max to some, and that would not match the model.
                        acceptHeaders.Add("application/json");

                        int todoIndex = random.Next(sutTodos.todos.Count);
                        t = sutTodos.todos[todoIndex];
                        bool postThisEdit = random.Next(1) == 0 ? true : false;

                        todo.id = t.id;
                        todo.title = actionCount.ToString() + "editSomeTodos";
                        todo.description = (postThisEdit ? "POST;" : "PUT;") + t.description;
                        todo.doneStatus = t.doneStatus;

                        body = new StringContent(JsonSerializer.Serialize(todo));

                        if (postThisEdit)
                        {
                            if (!executer.PostRequest(acceptHeaders, String.Format("todos/{0}", t.id), body))
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
                        break;

                    case addActiveTodo:
                        acceptHeaders.Add("application/json");

                        todoNoId.title = actionCount.ToString() + "Add an Active Todo";
                        todoNoId.doneStatus = false;
                        todoNoId.description = "Created by POST method";
                        body = new StringContent(JsonSerializer.Serialize(todoNoId));

                        if (!executer.PostRequest(acceptHeaders, "todos", body))
                        {
                            Console.WriteLine("Add Some Active Todos failed.");
                            Environment.Exit(-7);
                        }
                        activeTodosCount++;
                        break;

                    case addResolvedTodo:
                        acceptHeaders.Add("application/json");

                        todoNoId.title = actionCount.ToString() + "AddSomeResolvedTodos";
                        todoNoId.doneStatus = true;
                        todoNoId.description = "Created by POST method";
                        body = new StringContent(JsonSerializer.Serialize(todoNoId));

                        if (!executer.PostRequest(acceptHeaders, "todos", body))
                        {
                            Console.WriteLine("Add Some Resolved Todos failed.");
                            Environment.Exit(-27);
                        }
                        resolvedTodosCount++;
                        break;

                    case deleteActiveTodo:
                        acceptHeaders.Add("application/json");

                        index = random.Next((int)activeTodosCount);
                        foreach (TodoItem ti in sutTodos.todos)
                        {
                            if (!ti.doneStatus)
                            {
                                if (index == 0)
                                {
                                    if (!executer.DeleteRequest(acceptHeaders, String.Format("todos/{0}", ti.id)))
                                    {
                                        Console.WriteLine("Delete Active Todo failed.");
                                        Environment.Exit(-17);
                                    }
                                    activeTodosCount--;
                                    break;
                                }
                                index--;
                            }
                        }
                        break;

                    case deleteResolvedTodo:
                        acceptHeaders.Add("application/json");

                        index = random.Next((int)resolvedTodosCount);
                        foreach (TodoItem ti in sutTodos.todos)
                        {
                            if (ti.doneStatus)
                            {
                                if (index == 0)
                                {
                                    if (!executer.DeleteRequest(acceptHeaders, String.Format("todos/{0}", ti.id)))
                                    {
                                        Console.WriteLine("Delete Resolved Todo failed.");
                                        Environment.Exit(-17);
                                    }
                                    resolvedTodosCount--;
                                    break;
                                }
                                index--;
                            }
                        }
                        break;

                    case resolveActiveTodo:
                        acceptHeaders.Add("application/json");

                        index = random.Next((int)activeTodosCount);

                        foreach (TodoItem ti in sutTodos.todos)
                        {
                            if (!ti.doneStatus)
                            {
                                if (index == 0)
                                {
                                    todoNoId.doneStatus = true;
                                    todoNoId.title = ti.title;
                                    todoNoId.description = ti.description;
                                    body = new StringContent(JsonSerializer.Serialize(todoNoId));
                                    if (!executer.PutRequest(acceptHeaders, String.Format("todos/{0}", ti.id), body))
                                    {
                                        Console.WriteLine("Resolve Active Todo Failed");
                                        Environment.Exit(-11);
                                    }
                                    resolvedTodosCount++;
                                    activeTodosCount--;
                                    break;
                                }
                                index--;
                            }
                        }
                        break;

                    case activateResolvedTodo:
                        acceptHeaders.Add("application/json");
                        index = random.Next((int)resolvedTodosCount);

                        foreach (TodoItem ti in sutTodos.todos)
                        {
                            if (ti.doneStatus)
                            {
                                if (index == 0)
                                {
                                    todoNoId.doneStatus = false;
                                    todoNoId.title = ti.title;
                                    todoNoId.description = ti.description;
                                    body = new StringContent(JsonSerializer.Serialize(todoNoId));
                                    if (!executer.PutRequest(acceptHeaders, String.Format("todos/{0}", ti.id), body))
                                    {
                                        Console.WriteLine("Activate Resolved Todo Failed");
                                        Environment.Exit(-11);
                                    }
                                    resolvedTodosCount++;
                                    activeTodosCount--;
                                    break;
                                }
                                index--;
                            }
                        }
                        break;

                    case getDocs:
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
                        Console.WriteLine("ERROR: Unknown action '{0}' in AdapterTransition()", action);
                        break;
                }

                sutTodos = inSession ? GetTodosList() : new TodosList();

                if (!compareSUTtoModelState("AFTER", inSession, sutTodos))
                {
                    Console.WriteLine("Stopping test run: SUT and Model states differ after {0}", action);
                    Environment.Exit(-101);
                }
                Console.WriteLine("****{0,6} **** {1,30} >>>> {2} {3}", actionCount, action, activeTodosCount, resolvedTodosCount);

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

            actions.Add(getTodosList);
            actions.Add(getDocs);
            actions.Add(getHeartbeat);

            if (includeSelfLinkNoise)
            {
                if (activeTodosCount + resolvedTodosCount > 0)
                {
                    actions.Add(editTodo);
                }
            }

            //            actions.Add(endSession);

            // Add an action for a class of invalid actions that extend beyond
            // specific invalid actions cited in the API Challenges list.
            //            actions.Add(invalidRequest);

            if (AbstractTheState)
            {
                string[] stateArray = { vState[1], vState[2], vState[3] };

                abstractTodos = String.Join(", ", stateArray);
                actions.AddRange(GetAvailableAbstractActions(abstractTodos));
                return actions;
            }
            else
            {
                resolvedTodosCount = uint.Parse(vState[1].Split(".")[1]);
                activeTodosCount = uint.Parse(vState[2].Split(".")[1]);
            }

            switch (activeTodosCount + resolvedTodosCount)
            {
                case 0:
                    actions.Add(addActiveTodo);
                    actions.Add(addResolvedTodo);
                    break;

                case 1:
                    // activeTodos == 1 and resolvedTodos == 0
                    // activeTodos == 0 and resolvedTodos == 1
                    actions.Add(addActiveTodo);
                    actions.Add(addResolvedTodo);
                    if (activeTodosCount == 1)
                    {
                        actions.Add(deleteFinalActiveTodo);
                        actions.Add(resolveActiveTodo);
                    }
                    else
                    {
                        actions.Add(deleteFinalResolvedTodo);
                        actions.Add(activateResolvedTodo);
                    }
                    break;

                case maximumTodosCount:
                    // activeTodos > 0 and resolvedTodos > 0
                    // activeTodos == 0 and resolvedTodos == max
                    // activeTodos == max and resolvedTodos == 0
                    if (activeTodosCount > 0)
                    {
                        actions.Add(deleteActiveTodo);
                        actions.Add(resolveActiveTodo);
                    }
                    if (resolvedTodosCount > 0)
                    {
                        actions.Add(deleteResolvedTodo);
                        actions.Add(activateResolvedTodo);
                    }
                    break;

                case maximumTodosCount - 1:
                    // activeTodos > 0 and resolvedTodos > 0
                    // activeTodos == 0 and resolvedTodos == max-1
                    // activeTodos == max-1 and resolvedTodos == 0
                    actions.Add(addMaximumActiveTodo);
                    actions.Add(addMaximumResolvedTodo);
                    if (activeTodosCount > 0)
                    {
                        actions.Add(deleteActiveTodo);
                        actions.Add(resolveActiveTodo);
                    }
                    if (resolvedTodosCount > 0)
                    {
                        actions.Add(deleteResolvedTodo);
                        actions.Add(activateResolvedTodo);
                    }
                    break;

                default:
                    // 1 < activeCount + resolvedCount < maximumTodosCount - 1
                    // activeTodos > 0 and resolvedTodos > 0
                    // activeTodos == 0 and resolvedTodos < max-1
                    // activeTodos < max-1 and resolvedTodos == 0
                    actions.Add(addActiveTodo);
                    actions.Add(addResolvedTodo);
                    if (activeTodosCount > 0)
                    {
                        actions.Add(deleteActiveTodo);
                        actions.Add(resolveActiveTodo);
                    }
                    if (resolvedTodosCount > 0)
                    {
                        actions.Add(deleteResolvedTodo);
                        actions.Add(activateResolvedTodo);
                    }
                    break;
            }

            return actions;
        }

        List<string> GetAvailableAbstractActions(string abstractState)
        {
            List<string> actions = new List<string>();

            switch (abstractState)
            {
                case "Active.0, Resolved.0, MaxTodos.False":
                    actions.Add(addActiveTodo);
                    actions.Add(addResolvedTodo);
                    break;
                case "Active.0, Resolved.1, MaxTodos.False":
                    actions.Add(addActiveTodo);
                    actions.Add(addResolvedTodo);
                    actions.Add(deleteFinalResolvedTodo);
                    actions.Add(activateFinalResolvedTodo);
                    break;
                case "Active.0, Resolved.>1, MaxTodos.False":
                    actions.Add(addActiveTodo);
                    actions.Add(addResolvedTodo);
                    actions.Add(activateResolvedTodo);
                    actions.Add(deleteResolvedTodo);
                    break;
                case "Active.1, Resolved.0, MaxTodos.False":
                    actions.Add(addActiveTodo);
                    actions.Add(addResolvedTodo);
                    actions.Add(deleteFinalActiveTodo);
                    actions.Add(resolveFinalActiveTodo);
                    break;
                case "Active.>1, Resolved.0, MaxTodos.False":
                    actions.Add(addActiveTodo);
                    actions.Add(addResolvedTodo);
                    actions.Add(resolveActiveTodo);
                    actions.Add(deleteActiveTodo);
                    break;
                case "Active.1, Resolved.1, MaxTodos.False":
                    actions.Add(addActiveTodo);
                    actions.Add(addResolvedTodo);
                    actions.Add(deleteFinalResolvedTodo);
                    actions.Add(activateFinalResolvedTodo);
                    actions.Add(deleteFinalActiveTodo);
                    actions.Add(resolveFinalActiveTodo);
                    break;
                case "Active.1, Resolved.>1, MaxTodos.False":
                    actions.Add(addActiveTodo);
                    actions.Add(addResolvedTodo);
                    actions.Add(deleteResolvedTodo);
                    actions.Add(activateResolvedTodo);
                    actions.Add(deleteFinalActiveTodo);
                    actions.Add(resolveFinalActiveTodo);
                    break;
                case "Active.>1, Resolved.1, MaxTodos.False":
                    actions.Add(addActiveTodo);
                    actions.Add(addResolvedTodo);
                    actions.Add(deleteFinalResolvedTodo);
                    actions.Add(activateFinalResolvedTodo);
                    actions.Add(deleteActiveTodo);
                    actions.Add(resolveActiveTodo);
                    break;
                case "Active.>1, Resolved.>1, MaxTodos.False":
                    actions.Add(addActiveTodo);
                    actions.Add(addResolvedTodo);
                    actions.Add(resolveActiveTodo);
                    actions.Add(deleteActiveTodo);
                    actions.Add(activateResolvedTodo);
                    actions.Add(deleteResolvedTodo);
                    break;
                case "Active.Max-1, Resolved.0, MaxTodos.False":
                    actions.Add(addMaximumActiveTodo);
                    actions.Add(addMaximumResolvedTodo);
                    actions.Add(resolveActiveTodo);
                    actions.Add(deleteActiveTodo);
                    break;
                case "Active.0, Resolved.Max-1, MaxTodos.False":
                    actions.Add(addMaximumActiveTodo);
                    actions.Add(addMaximumResolvedTodo);
                    actions.Add(activateResolvedTodo);
                    actions.Add(deleteResolvedTodo);
                    break;
                case "Active.0, Resolved.Max, MaxTodos.True":
                    actions.Add(deleteResolvedTodo);
                    actions.Add(activateResolvedTodo);
                    break;
                case "Active.1, Resolved.Max-1, MaxTodos.True":
                    actions.Add(deleteFinalActiveTodo);
                    actions.Add(deleteResolvedTodo);
                    actions.Add(resolveFinalActiveTodo);
                    actions.Add(activateResolvedTodo);
                    break;
                case "Active.Max, Resolved.0, MaxTodos.True":
                    actions.Add(deleteActiveTodo);
                    actions.Add(resolveActiveTodo);
                    break;
                case "Active.Max-1, Resolved.1, MaxTodos.True":
                    actions.Add(deleteFinalResolvedTodo);
                    actions.Add(deleteActiveTodo);
                    actions.Add(activateFinalResolvedTodo);
                    actions.Add(resolveActiveTodo);
                    break;
                case "Active.>1, Resolved.>1, MaxTodos.True":
                    actions.Add(deleteActiveTodo);
                    actions.Add(deleteResolvedTodo);
                    actions.Add(resolveActiveTodo);
                    actions.Add(activateResolvedTodo);
                    break;
                default:
                    Console.WriteLine("Unknown abstract state {0} in GetAvailableAbstractActions()", abstractState);
                    break;
            }
            return actions;
        }

        string GetAbstractEndState(string startState, string action)
        {
            switch (action)
            {
                case addActiveTodo:
                    switch (startState)
                    {
                        case "Active.0, Resolved.0, MaxTodos.False":
                            return "Active.1, Resolved.0, MaxTodos.False";
                        case "Active.0, Resolved.1, MaxTodos.False":
                            return "Active.1, Resolved.1, MaxTodos.False";
                        case "Active.0, Resolved.>1, MaxTodos.False":
                            return "Active.1, Resolved.>1, MaxTodos.False";
                        case "Active.1, Resolved.0, MaxTodos.False":
                            return "Active.>1, Resolved.0, MaxTodos.False";
                        case "Active.>1, Resolved.0, MaxTodos.False":
                            return startState;
                        case "Active.1, Resolved.1, MaxTodos.False":
                            return "Active.>1, Resolved.1, MaxTodos.False";
                        case "Active.1, Resolved.>1, MaxTodos.False":
                            return "Active.>1, Resolved.>1, MaxTodos.False";
                        case "Active.>1, Resolved.1, MaxTodos.False":
                            return startState;
                        case "Active.>1, Resolved.>1, MaxTodos.False":
                            return startState;
                        case "Active.Max-1, Resolved.0, MaxTodos.False":
                            return "Active.Max, Resolved.0, MaxTodos.True";
                        case "Active.0, Resolved.Max-1, MaxTodos.False":
                            return "Active.1, Resolved.Max-1, MaxTodos.True";
                        default:
                            break;
                    }
                    break;
                case addResolvedTodo:
                    switch (startState)
                    {
                        case "Active.0, Resolved.0, MaxTodos.False":
                            return "Active.0, Resolved.1, MaxTodos.False";
                        case "Active.0, Resolved.1, MaxTodos.False":
                            return "Active.0, Resolved.>1, MaxTodos.False";
                        case "Active.0, Resolved.>1, MaxTodos.False":
                            return startState;
                        case "Active.1, Resolved.0, MaxTodos.False":
                            return "Active.1, Resolved.1, MaxTodos.False";
                        case "Active.>1, Resolved.0, MaxTodos.False":
                            return "Active.>1, Resolved.1, MaxTodos.False";
                        case "Active.1, Resolved.1, MaxTodos.False":
                            return "Active.1, Resolved.>1, MaxTodos.False";
                        case "Active.1, Resolved.>1, MaxTodos.False":
                            return startState;
                        case "Active.>1, Resolved.1, MaxTodos.False":
                            return "Active.>1, Resolved.>1, MaxTodos.False";
                        case "Active.>1, Resolved.>1, MaxTodos.False":
                            return startState;
                        case "Active.Max-1, Resolved.0, MaxTodos.False":
                            return "Active.Max-1, Resolved.1, MaxTodos.True";
                        case "Active.0, Resolved.Max-1, MaxTodos.False":
                            return "Active.0, Resolved.Max, MaxTodos.True";
                        default:
                            break;
                    }
                    break;
                case resolveActiveTodo:
                    switch (startState)
                    {
                        case "Active.>1, Resolved.0, MaxTodos.False":
                            return "Active.1, Resolved.1, MaxTodos.False";
                        case "Active.>1, Resolved.1, MaxTodos.False":
                        case "Active.>1, Resolved.>1, MaxTodos.False":
                            return "Active.1, Resolved.>1, MaxTodos.False";
                        case "Active.Max-1, Resolved.0, MaxTodos.False":
                            return "Active.>1, Resolved.1, MaxTodos.False";
                        case "Active.Max, Resolved.0, MaxTodos.True":
                            return "Active.Max-1, Resolved.1, MaxTodos.True";
                        case "Active.Max-1, Resolved.1, MaxTodos.True":
                            return "Active.>1, Resolved.>1, MaxTodos.True";
                        case "Active.>1, Resolved.>1, MaxTodos.True":
                            return "Active.1, Resolved.Max-1, MaxTodos.True";
                        default:
                            break;
                    }
                    break;
                case activateResolvedTodo:
                    switch (startState)
                    {
                        case "Active.0, Resolved.>1, MaxTodos.False":
                            return "Active.1, Resolved.1, MaxTodos.False";
                        case "Active.1, Resolved.>1, MaxTodos.False":
                        case "Active.>1, Resolved.>1, MaxTodos.False":
                            return "Active.>1, Resolved.1, MaxTodos.False";
                        case "Active.0, Resolved.Max-1, MaxTodos.False":
                            return "Active.1, Resolved.>1, MaxTodos.False";
                        case "Active.0, Resolved.Max, MaxTodos.True":
                            return "Active.1, Resolved.Max-1, MaxTodos.True";
                        case "Active.1, Resolved.Max-1, MaxTodos.True":
                            return "Active.>1, Resolved.>1, MaxTodos.True";
                        case "Active.>1, Resolved.>1, MaxTodos.True":
                            return "Active.Max-1, Resolved.1, MaxTodos.True";
                        default:
                            break;
                    }
                    break;
                case deleteActiveTodo:
                    switch (startState)
                    {
                        case "Active.>1, Resolved.0, MaxTodos.False":
                            return "Active.1, Resolved.0, MaxTodos.False";
                        case "Active.>1, Resolved.1, MaxTodos.False":
                            return "Active.1, Resolved.1, MaxTodos.False";
                        case "Active.>1, Resolved.>1, MaxTodos.False":
                            return "Active.1, Resolved.>1, MaxTodos.False";
                        case "Active.Max-1, Resolved.0, MaxTodos.False":
                            return "Active.>1, Resolved.0, MaxTodos.False";
                        case "Active.Max, Resolved.0, MaxTodos.True":
                            return "Active.Max-1, Resolved.0, MaxTodos.False";
                        case "Active.Max-1, Resolved.1, MaxTodos.True":
                            return "Active.>1, Resolved.1, MaxTodos.False";
                        case "Active.>1, Resolved.>1, MaxTodos.True":
                            return "Active.1, Resolved.>1, MaxTodos.False";
                        default:
                            break;
                    }
                    break;
                case deleteResolvedTodo:
                    switch (startState)
                    {
                        case "Active.0, Resolved.>1, MaxTodos.False":
                            return "Active.0, Resolved.1, MaxTodos.False";
                        case "Active.1, Resolved.>1, MaxTodos.False":
                            return "Active.1, Resolved.1, MaxTodos.False";
                        case "Active.>1, Resolved.>1, MaxTodos.False":
                            return "Active.>1, Resolved.1, MaxTodos.False";
                        case "Active.0, Resolved.Max-1, MaxTodos.False":
                            return "Active.0, Resolved.>1, MaxTodos.False";
                        case "Active.0, Resolved.Max, MaxTodos.True":
                            return "Active.0, Resolved.Max-1, MaxTodos.False";
                        case "Active.1, Resolved.Max-1, MaxTodos.True":
                            return "Active.1, Resolved.>1, MaxTodos.False";
                        case "Active.>1, Resolved.>1, MaxTodos.True":
                            return "Active.>1, Resolved.1, MaxTodos.False";
                        default:
                            break;
                    }
                    break;
                case addMaximumActiveTodo:
                    switch (startState)
                    {
                        case "Active.Max-1, Resolved.0, MaxTodos.False":
                            return "Active.Max, Resolved.0, MaxTodos.True";
                        case "Active.>1, Resolved.>1, MaxTodos.False":
                            return "Active.>1, Resolved.>1, MaxTodos.True";
                        case "Active.0, Resolved.Max-1, MaxTodos.False":
                            return "Active.1, Resolved.Max-1, MaxTodos.True";
                        default:
                            break;
                    }
                    break;
                case addMaximumResolvedTodo:
                    switch (startState)
                    {
                        case "Active.0, Resolved.Max-1, MaxTodos.False":
                            return "Active.0, Resolved.Max, MaxTodos.True";
                        case "Active.>1, Resolved.>1, MaxTodos.False":
                            return "Active.>1, Resolved.>1, MaxTodos.True";
                        case "Active.Max-1, Resolved.0, MaxTodos.False":
                            return "Active.Max-1, Resolved.1, MaxTodos.True";
                        default:
                            break;
                    }
                    break;
                case deleteFinalActiveTodo:
                    switch (startState)
                    {
                        case "Active.1, Resolved.0, MaxTodos.False":
                            return "Active.0, Resolved.0, MaxTodos.False";
                        case "Active.1, Resolved.1, MaxTodos.False":
                            return "Active.0, Resolved.1, MaxTodos.False";
                        case "Active.1, Resolved.>1, MaxTodos.False":
                            return "Active.0, Resolved.>1, MaxTodos.False";
                        case "Active.1, Resolved.Max-1, MaxTodos.True":
                            return "Active.0, Resolved.Max-1, MaxTodos.False";
                        default:
                            break;
                    }
                    break;
                case deleteFinalResolvedTodo:
                    switch (startState)
                    {
                        case "Active.0, Resolved.1, MaxTodos.False":
                            return "Active.0, Resolved.0, MaxTodos.False";
                        case "Active.1, Resolved.1, MaxTodos.False":
                            return "Active.1, Resolved.0, MaxTodos.False";
                        case "Active.>1, Resolved.1, MaxTodos.False":
                            return "Active.>1, Resolved.0, MaxTodos.False";
                        case "Active.Max-1, Resolved.1, MaxTodos.True":
                            return "Active.Max-1, Resolved.0, MaxTodos.False";
                        default:
                            break;
                    }
                    break;
                case activateFinalResolvedTodo:
                    switch (startState)
                    {
                        case "Active.0, Resolved.1, MaxTodos.False":
                            return "Active.1, Resolved.0, MaxTodos.False";
                        case "Active.1, Resolved.1, MaxTodos.False":
                            return "Active.>1, Resolved.0, MaxTodos.False";
                        case "Active.>1, Resolved.1, MaxTodos.False":
                            return "Active.Max-1, Resolved.0, MaxTodos.False";
                        case "Active.Max-1, Resolved.1, MaxTodos.True":
                            return "Active.Max, Resolved.0, MaxTodos.True";
                        default:
                            break;
                    }
                    break;
                case resolveFinalActiveTodo:
                    switch (startState)
                    {
                        case "Active.1, Resolved.0, MaxTodos.False":
                            return "Active.0, Resolved.1, MaxTodos.False";
                        case "Active.1, Resolved.1, MaxTodos.False":
                            return "Active.0, Resolved.>1, MaxTodos.False";
                        case "Active.1, Resolved.>1, MaxTodos.False":
                            return "Active.0, Resolved.Max-1, MaxTodos.False";
                        case "Active.1, Resolved.Max-1, MaxTodos.True":
                            return "Active.0, Resolved.Max, MaxTodos.True";
                        default:
                            break;
                    }
                    break;
                default:
                    Console.WriteLine("Endstate same as startstate: {0}", startState);
                    return startState;
            }
            return String.Empty;
        }
        // Interface method
        public string GetEndState(string startState, string action)
        {
            string[] vState = startState.Split(", ");
            bool inSession = vState[0].Contains("True") ? true : false;

            if (AbstractTheState)
            {
                string[] stateArray = { vState[1], vState[2], vState[3] };

                abstractTodos = String.Join(", ", stateArray);
                string endState = GetAbstractEndState(abstractTodos, action);
                string prefix = vState[0] + ", ";
                if (action == startSession)
                {
                    prefix = "InSession.True, ";
                }
                if (action == endSession)
                {
                    prefix = "InSession.False, ";
                }
                return prefix + endState;
            }
            else
            {
                resolvedTodosCount = uint.Parse(vState[1].Split(".")[1]);
                activeTodosCount = uint.Parse(vState[2].Split(".")[1]);
            }


            switch (action)
            {
                case startSession:
                    inSession = true;
                    break;
                case endSession:
                    inSession = false;
                    break;
                case addResolvedTodo:
                case addMaximumResolvedTodo:
                    resolvedTodosCount++;
                    break;
                case addActiveTodo:
                case addMaximumActiveTodo:
                    activeTodosCount++;
                    break;
                case editTodo:
                    break;
                case activateResolvedTodo:
                    resolvedTodosCount--;
                    activeTodosCount++;
                    break;
                case resolveActiveTodo:
                    activeTodosCount--;
                    resolvedTodosCount++;
                    break;
                case deleteResolvedTodo:
                case deleteFinalResolvedTodo:
                    resolvedTodosCount--;
                    break;
                case deleteActiveTodo:
                case deleteFinalActiveTodo:
                    activeTodosCount--;
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
            return StringifyStateVector(inSession);
        }
    }
}
