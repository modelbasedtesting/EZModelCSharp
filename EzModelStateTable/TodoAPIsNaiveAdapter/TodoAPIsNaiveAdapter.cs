using System;
using System.Text; // StringContent Encoding
using System.Net.Http;
using System.Collections.Generic;
using SeriousQualityEzModel;
using SynchronousHttpClientExecuter;
using System.Text.Json;
using System.Xml.Serialization;
using System.IO;
using System.Xml;

namespace TodoAPIsNaiveAdapter
{
    class TodoAPIsNaiveAdapterProgram
    {
        static int Main(string[] args)
        {
            APIs client = new APIs();
            client.SelfLinkTreatment = SelfLinkTreatmentChoice.OnePerAction;
            client.IncludeSelfLinkNoise = true;
            client.AbstractTheState = false;

            EzModelGraph graph = new EzModelGraph(client, 3000, 1000, 50, EzModelGraph.LayoutRankDirection.TopDown);

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
        uint svActiveTodos = 10;
        bool svSecretToken = false;
        string svAuthToken = String.Empty;

        uint activeTodosCount;
        uint resolvedTodosCount;
        uint actionCount = 0;
        bool secretToken;

        const uint maximumTodosCount = 12; // Arbitrary maximum
        string AuthToken = "";

        Random random = new Random(DateTime.Now.Millisecond);

        const string startSession = "Start Session";
        const string endSession = "End Session";

        // Manipulating data in the todos list
        // To cover the Alan Richardson APIs Challenge for creating a Todo using Content-Type
        // application/xml and application/json, randomly select the Content-Type header for
        // "add*" actions below as application/xml or application/json.
        const string addActiveTodo = "Add an Active Todo"; // Content-Type application/json
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

        // Read-only actions
        const string getTodosList = "Get Todos List";
        const string getDocs = "Get Documentation";
        const string getHeartbeat = "Get Service Heartbeat";
        const string getChallenges = "Get Challenges List";
        const string getTodoById = "Get Specific Todo"; 
        const string getResolvedTodos = "Get Resolved Todos List"; 
        const string getTodosHead = "Get Todos HEAD"; 
        const string getTodosOPTIONS = "Get Todos OPTIONS"; 
        const string getXMLTodosList = "Get XML Todos List"; 
        const string getAnyTodosList = "Get Any Todos List"; // Accept */*
        const string getXMLJSONTodosList = "Prefer XML Todos List"; // Accept XML, Accept JSON
        const string getNoAcceptTodosList = "Get No Accept Header Todos List";
        const string getSecretNote = "Get Secret Note"; // supply valid X-AUTH-TOKEN

        // State-changing actions outside of todos list
        const string createNewChallengerSession = "Create new Challenger Session"; // POST /challenger 201 
        const string getSecretTokenAuthPass = "Get Secret Token Auth Pass"; // 201

        // Actions expecting a non 2xx HTTP response code
        const string getTodoFail = "Get Todo Fail"; // Get /todo - wrong endpoint 404
        const string getNonexistentTodo = "Get Nonexistent Todo"; // Get /todos/{not an id} 404
        const string addTodoInvalidDoneStatus = "Add Todo with Invalid Done Status"; // POST x 400
        const string getTodoGzip = "Get Todo in GZip Format"; // Accept application/gzip 406
        const string addTodoUnsupportedContentType = "Add Todo Unsupported Content Type"; // Post 415
        const string deleteHeartbeat = "Delete Heartbeat"; // 405 method not allowed
        const string patchHeartbeat = "Patch Heartbeat"; // 500 internal server error
        const string traceHeartbeat = "Trace Heartbeat"; // 501 not implemented
        const string getSecretTokenFailedAuth = "Get Secret Token Failed Auth"; // 401
        const string getSecretNoteWrongAuthToken = "Get Secret Note Wrong Auth Token"; // 403
        const string getSecretNoteNoAuthToken = "Get Secret Note No Auth Token"; // 401
        const string setSecretNoteWrongAuthToken = "Set Secret Note Wrong Auth Token"; // 403
        const string setSecretNoteNoAuthToken = "Set Secret Note No Auth Token"; // 401

        // Actions outside of the APIs that cover legitimate REST methods
        const string invalidRequest = "invalidRequest";

        string StringifyStateVector(bool inSession)
        {
            string s = String.Format("InSession.{0}, Resolved.{1}, Active.{2}, SecretToken.{3}", inSession, resolvedTodosCount, activeTodosCount, secretToken);
            return s;
        }

        // Interface method
        public string GetInitialState()
        {
            activeTodosCount = svActiveTodos;
            resolvedTodosCount = svResolvedTodos;
            secretToken = svSecretToken;
            AuthToken = svAuthToken;

            Console.WriteLine("GET INITIAL STATE secret token = {0}", secretToken);
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
                // If the process of the apichallenges.jar is active, do nothing.
                // Fire up the APIs server otherwise
                //if (!executer.Startup())
                //{
                //    Console.WriteLine("Failed to start apichallenges.jar in SetStateOfSystemUnderTest()");
                //}
            }
            else
            {
                // shut down the APIs server.  Done..
                executer.GetRequest(new List<string>(), "shutdown", new List<string[]>());
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

        public class TodoItemInvalidDoneStatus
        {
            public string title { get; set; }
            public string doneStatus { get; set; }
            public string description { get; set; }
        }

        public class TodosList
        {
            public IList<TodoItem> todos { get; set; }
        }

        public class ChallengeItem
        {
            public string name { get; set; }
            public string description { get; set; }
            public bool status { get; set; } // true when challenge achieved, false otherwise
        }

        public class ChallengesList
        {
            public IList<ChallengeItem> challenges { get; set; }
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

        string GetXmlSerializedTodoItemNoId(TodoItemNoId value)
        {
            // https://stackoverflow.com/questions/4123590/serialize-an-object-to-xml
            if (value == null)
            {
                return string.Empty;
            }
            try
            {
                XmlSerializer xmlserializer = new XmlSerializer(typeof(TodoItemNoId));
                StringWriter stringWriter = new StringWriter();
                using (XmlWriter writer = XmlWriter.Create(stringWriter))
                {
                    xmlserializer.Serialize(writer, value);
                    string serial = stringWriter.ToString();
                    serial = "<todo>" + serial.Substring(serial.IndexOf("<title>"));
                    serial = serial.Substring(0, serial.IndexOf("</TodoItemNoId>")) + "</todo>";
//                    Console.WriteLine(serial);
//                    ConsoleKeyInfo key = Console.ReadKey();
                    return serial;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine( "Exception in GetSerializedXml(): {0}", e.Message);
                return string.Empty;
            }
        }

        string GetXmlSerializedTodoItemInvalidDoneStatus(TodoItemInvalidDoneStatus value)
        {
            // https://stackoverflow.com/questions/4123590/serialize-an-object-to-xml
            if (value == null)
            {
                return string.Empty;
            }
            try
            {
                XmlSerializer xmlserializer = new XmlSerializer(typeof(TodoItemInvalidDoneStatus));
                StringWriter stringWriter = new StringWriter();
                using (XmlWriter writer = XmlWriter.Create(stringWriter))
                {
                    xmlserializer.Serialize(writer, value);
                    string serial = stringWriter.ToString();
                    serial = "<todo>" + serial.Substring(serial.IndexOf("<title>"));
                    serial = serial.Substring(0, serial.IndexOf("</TodoItemNoId>")) + "</todo>";
                    //                    Console.WriteLine(serial);
                    //                    ConsoleKeyInfo key = Console.ReadKey();
                    return serial;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception in GetSerializedXml(): {0}", e.Message);
                return string.Empty;
            }
        }

        int UnusedTodoId( List<int> idList )
        {
            if (idList.Count == 0)
            {
                return 1; // Set the Todo ID to 1 if there aren't any Todo items.
            }

            int unused = 1;
            bool found = false;

            while (!found)
            {
                if (!idList.Contains(unused))
                {
                    found = true;
                }
                else
                {
                    unused++;
                }
            }
            return unused;
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
            List<int> todoIds = new List<int>();
            if (sutTodos.todos != null)
            {
                foreach (TodoItem t2 in sutTodos.todos)
                {
                    todoIds.Add(t2.id);
                }
            }

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
            bool debuggerHugger = false;
            bool isXmlContent = random.Next(2) == 0 ? false : true;
            // switching between JSON and XML content type covers a Richardson APIs challenge
            string contentType = isXmlContent ? "application/xml" : "application/json";

            // switching between Accept type of XML and JSON covers a Richardson APIs challenge.
            string acceptType = random.Next(2) == 0 ? "application/json" : "application/xml";

            List<string[]> customHeaders = new List<string[]>();

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

                        if (!executer.GetRequest(acceptHeaders, "shutdown", customHeaders))
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

                    case getChallenges:
                        debuggerHugger = false;
                        acceptHeaders.Add(acceptType);

                        ChallengesList challenges = new ChallengesList();

                        // issue the GET request
                        if (!executer.GetRequest(acceptHeaders, "challenges", customHeaders))
                        {
                            Console.WriteLine("Get Challenges List failed");
                            Environment.Exit(-32);
                        }
                        try
                        {
                            response = executer.responseBody;
//                            challenges = JsonSerializer.Deserialize<ChallengesList>(response);
                            Console.WriteLine(response);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Exception working on response for Get Challenges List");
                            Console.WriteLine(e.Message);
                        }
                        break;

                    case getTodoById:
                        debuggerHugger = false;
                        acceptHeaders.Add("application/json");

                        int todoId = todoIds[random.Next(todoIds.Count)];

                        // issue the GET request
                        if (!executer.GetRequest(acceptHeaders, "todos/" + todoId.ToString(),  customHeaders))
                        {
                            Console.WriteLine("Get Todo by ID failed");
                            Environment.Exit(-33);
                        }
                        try
                        {
                            response = executer.responseBody;
                            // todo = JsonSerializer.Deserialize<TodoItem>(response);
                            Console.WriteLine(response);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Exception working on response for Get Todo by ID");
                            Console.WriteLine(e.Message);
                        }
                        break;

                    case getResolvedTodos:
                        debuggerHugger = false;
                        acceptHeaders.Add(acceptType);

                        // issue the GET request
                        if (!executer.GetRequest(acceptHeaders, "todos?doneStatus=true", customHeaders))
                        {
                            Console.WriteLine("Get Resolved Todos failed");
                            Environment.Exit(-34);
                        }
                        try
                        {
                            response = executer.responseBody;
                            // sutTodos = JsonSerializer.Deserialize<TodosList>(response);
                            Console.WriteLine(response);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Exception working on response for Get Resolved Todos");
                            Console.WriteLine(e.Message);
                        }
                        break;

                    case getTodosHead: // HEAD request on /todos
                        debuggerHugger = false;
                        acceptHeaders.Add(acceptType);

                        if (!executer.HeadRequest(acceptHeaders, "todos"))
                        {
                            Console.WriteLine("Get Todos HEAD failed");
                            Environment.Exit(-35);
                        }
                        try
                        {
                            response = executer.response.Headers.ToString();
                            Console.WriteLine(response);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Exception working on response headers for Get Todos Head");
                            Console.WriteLine(e.Message);
                        }
                        break;

                    case getTodosOPTIONS: // OPTIONS request on /todos
                        debuggerHugger = false;
                        acceptHeaders.Add(acceptType);

                        if (!executer.OptionsRequest(acceptHeaders, "todos"))
                        {
                            Console.WriteLine("Get Todos OPTIONS failed");
                            Environment.Exit(-36);
                        }
                        try
                        {
                            response = executer.response.Headers.ToString();
                            Console.WriteLine(response);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Exception working on response headers for Get Todos OPTIONS");
                            Console.WriteLine(e.Message);
                        }
                        break;

                    case getXMLTodosList: // Accept application/xml
                        debuggerHugger = false;
                        acceptHeaders.Add("application/xml");

                        // issue the GET request
                        if (!executer.GetRequest(acceptHeaders, "todos", customHeaders))
                        {
                            Console.WriteLine("Get XML Todos List failed");
                            Environment.Exit(-42);
                        }
                        try
                        {
                            response = executer.responseBody;
                            Console.WriteLine(response); // should look like XML
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Exception working on response for Get XML Todos List");
                            Console.WriteLine(e.Message);
                        }
                        break;

                    case getAnyTodosList: // Accept */*
                        debuggerHugger = false;
                        acceptHeaders.Add("*/*");

                        // issue the GET request
                        if (!executer.GetRequest(acceptHeaders, "todos", customHeaders))
                        {
                            Console.WriteLine("Get Any Todos List failed");
                            Environment.Exit(-43);
                        }
                        try
                        {
                            response = executer.responseBody;
                            // sutTodos = JsonSerializer.Deserialize<TodosList>(response);
                            Console.WriteLine(response);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Exception working on response for Get Any Todos List");
                            Console.WriteLine(e.Message);
                        }
                        break;

                    case getXMLJSONTodosList: // Accept application/xml, application/json
                        debuggerHugger = false;
                        acceptHeaders.Add("application/xml");
                        acceptHeaders.Add("application/json");

                        // issue the GET request
                        if (!executer.GetRequest(acceptHeaders, "todos", customHeaders))
                        {
                            Console.WriteLine("Get XML JSON Todos List failed");
                            Environment.Exit(-44);
                        }
                        try
                        {
                            response = executer.responseBody;
                            // sutTodos = JsonSerializer.Deserialize<TodosList>(response);
                            Console.WriteLine(response); // Should be XML
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Exception working on response for Get XML JSON Todos List");
                            Console.WriteLine(e.Message);
                        }
                        break;

                    case getNoAcceptTodosList: // No Accept header, expect JSON
                        debuggerHugger = false;
                        if (!executer.GetRequest(acceptHeaders, "todos", customHeaders))
                        {
                            Console.WriteLine("Get No Accept Todos List failed");
                            Environment.Exit(-45);
                        }
                        try
                        {
                            response = executer.responseBody;
                            // sutTodos = JsonSerializer.Deserialize<TodosList>(response);
                            Console.WriteLine(response);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Exception working on response for Get No Accept Todos List");
                            Console.WriteLine(e.Message);
                        }
                        break;

                    case getTodosList:
                        // already done before coming into this switch statement..
                        break;

                    case editTodo:
                        // Don't resolve or activate as part of edit, because that
                        // could change the size category of the active or resolved
                        // todos.  That is, you could go from zero to some, or from
                        // max to some, and that would not match the model.
                        acceptHeaders.Add(acceptType);

                        int todoIndex = random.Next(sutTodos.todos.Count);
                        t = sutTodos.todos[todoIndex];
                        bool postThisEdit = random.Next(1) == 0 ? true : false;

                        todo.id = t.id;
                        todo.title = actionCount.ToString() + " EditATodo";
                        todo.description = (postThisEdit ? "POST;" : "PUT;") + t.description;
                        todo.doneStatus = t.doneStatus;

                        body = new StringContent(JsonSerializer.Serialize(todo));

                        if (postThisEdit)
                        {
                            if (!executer.PostRequest(acceptHeaders, String.Format("todos/{0}", t.id), body, customHeaders))
                            {
                                Console.WriteLine("Edit a Todo failed on POST {0}", t.id);
                                Environment.Exit(-3);
                            }
                        }
                        else
                        {
                            if (!executer.PutRequest(acceptHeaders, String.Format("todos/{0}", t.id), body))
                            {
                                Console.WriteLine("Edit a Todo failed on PUT {0}", t.id);
                                Environment.Exit(-33);
                            }
                        }
                        break;

                    case addActiveTodo:
                        acceptHeaders.Add(acceptType);

                        todoNoId.title = actionCount.ToString() + " AddAnActiveTodo";
                        todoNoId.doneStatus = false;
                        todoNoId.description = "Created by POST method";

                        Console.WriteLine("request Content-Type = {0}", contentType);

                        if (isXmlContent)
                        {
                            body = new StringContent(GetXmlSerializedTodoItemNoId(todoNoId), Encoding.UTF8, contentType);
                        }
                        else
                        {
                            body = new StringContent(JsonSerializer.Serialize(todoNoId), Encoding.UTF8, contentType);
                        }

                        if (!executer.PostRequest(acceptHeaders, "todos", body, customHeaders))
                        {
                            Console.WriteLine("Add an Active Todo failed.");
                            Environment.Exit(-7);
                        }
                        activeTodosCount++;
                        break;

                    case addMaximumActiveTodo:
                        acceptHeaders.Add(acceptType);

                        todoNoId.title = actionCount.ToString() + " AddMaximumActiveTodo";
                        todoNoId.doneStatus = false;
                        todoNoId.description = "Created by POST method";

                        Console.WriteLine("request Content-Type = {0}", contentType);

                        if (isXmlContent)
                        {
                            body = new StringContent(GetXmlSerializedTodoItemNoId(todoNoId), Encoding.UTF8, contentType);
                        }
                        else
                        {
                            body = new StringContent(JsonSerializer.Serialize(todoNoId), Encoding.UTF8, contentType);
                        }

                        if (!executer.PostRequest(acceptHeaders, "todos", body, customHeaders))
                        {
                            Console.WriteLine("Add Maximum Active Todo failed.");
                            Environment.Exit(-7);
                        }
                        activeTodosCount++;
                        break;

                    case addResolvedTodo:
                        acceptHeaders.Add(acceptType);

                        todoNoId.title = actionCount.ToString() + " AddResolvedTodo";
                        todoNoId.doneStatus = true;
                        todoNoId.description = "Created by POST method";

                        Console.WriteLine("request Content-Type = {0}", contentType);

                        if (isXmlContent)
                        {
                            body = new StringContent(GetXmlSerializedTodoItemNoId(todoNoId), Encoding.UTF8, contentType);
                        }
                        else
                        {
                            body = new StringContent(JsonSerializer.Serialize(todoNoId), Encoding.UTF8, contentType);
                        }

                        if (!executer.PostRequest(acceptHeaders, "todos", body, customHeaders))
                        {
                            Console.WriteLine("Add a Resolved Todo failed.");
                            Environment.Exit(-27);
                        }
                        resolvedTodosCount++;
                        break;

                    case addMaximumResolvedTodo:
                        acceptHeaders.Add(acceptType);

                        todoNoId.title = actionCount.ToString() + " AddMaximumResolvedTodo";
                        todoNoId.doneStatus = true;
                        todoNoId.description = "Created by POST method";

                        Console.WriteLine("request Content-Type = {0}", contentType);

                        if (isXmlContent)
                        {
                            body = new StringContent(GetXmlSerializedTodoItemNoId(todoNoId), Encoding.UTF8, contentType);
                        }
                        else
                        {
                            body = new StringContent(JsonSerializer.Serialize(todoNoId), Encoding.UTF8, contentType);
                        }

                        if (!executer.PostRequest(acceptHeaders, "todos", body, customHeaders))
                        {
                            Console.WriteLine("Add Maximum Resolved Todo failed.");
                            Environment.Exit(-37);
                        }
                        resolvedTodosCount++;
                        break;

                    case deleteActiveTodo:
                        acceptHeaders.Add(acceptType);

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

                    case deleteFinalActiveTodo:
                        acceptHeaders.Add(acceptType);

                        foreach (TodoItem ti in sutTodos.todos)
                        {
                            if (!ti.doneStatus)
                            {
                                if (!executer.DeleteRequest(acceptHeaders, String.Format("todos/{0}", ti.id)))
                                {
                                    Console.WriteLine("Delete Final Active Todo failed.");
                                    Environment.Exit(-18);
                                }
                                activeTodosCount--;
                                break;
                            }
                        }
                        break;

                    case deleteResolvedTodo:
                        acceptHeaders.Add(acceptType);

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

                    case deleteFinalResolvedTodo:
                        acceptHeaders.Add(acceptType);

                        foreach (TodoItem ti in sutTodos.todos)
                        {
                            if (ti.doneStatus)
                            {
                                if (!executer.DeleteRequest(acceptHeaders, String.Format("todos/{0}", ti.id)))
                                {
                                    Console.WriteLine("Delete Final Resolved Todo failed.");
                                    Environment.Exit(-17);
                                }
                                resolvedTodosCount--;
                                break;
                            }
                        }
                        break;

                    case resolveActiveTodo:
                        acceptHeaders.Add(acceptType);

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
                        acceptHeaders.Add(acceptType);
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
                                    resolvedTodosCount--;
                                    activeTodosCount++;
                                    break;
                                }
                                index--;
                            }
                        }
                        break;

                    case getDocs:
                        acceptHeaders.Add(acceptType);

                        // issue the GET request
                        if (!executer.GetRequest(acceptHeaders, "docs", customHeaders))
                        {
                            Console.WriteLine("Get Documentation failed.");
                            Environment.Exit(-9);
                        }
                        break;

                    case getHeartbeat:
                        acceptHeaders.Add(acceptType);

                        // issue the GET request
                        if (!executer.GetRequest(acceptHeaders, "heartbeat", customHeaders))
                        {
                            Console.WriteLine("Get Service Heartbeat failed.");
                            Environment.Exit(-14);
                        }
                        break;

                    case createNewChallengerSession:
                        acceptHeaders.Add(acceptType);

                        if (!executer.PostRequest(acceptHeaders, "challenger", new StringContent(""), customHeaders))
                        {
                            Console.WriteLine("POST Challenger ID failed.");
                            Environment.Exit(-19);
                        }
                        Console.WriteLine(executer.responseBody);
                        break;

                    case getTodoFail:
                        acceptHeaders.Add(acceptType);

                        // issue the GET request
                        if (!executer.GetRequest(acceptHeaders, "todo", customHeaders))
                        {
                            Console.WriteLine("Get Todo Fail failed.");
                            Environment.Exit(-14);
                        }
                        // Expect 404 HTTP code
                        break;

                    case getNonexistentTodo:
                        debuggerHugger = false;
                        acceptHeaders.Add(acceptType);

                        // issue the GET request
                        if (!executer.GetRequest(acceptHeaders, "todos/" + UnusedTodoId(todoIds).ToString(), customHeaders))
                        {
                            Console.WriteLine("Get NonExistent Todo failed.");
                            Environment.Exit(-15);
                        }
                        // Expect 404 HTTP code
                        Console.WriteLine(executer.responseBody);
                        break;

                    case addTodoInvalidDoneStatus:
                        debuggerHugger = false;
                        acceptHeaders.Add(acceptType);

                        TodoItemInvalidDoneStatus tids = new TodoItemInvalidDoneStatus();
                        tids.title = actionCount.ToString() + " AddTodoInvalidDoneStatus";
                        tids.doneStatus = "false"; // a valid doneStatus would be boolean..
                        tids.description = "Created by POST method";

                        Console.WriteLine("request Content-Type = {0}", contentType);

                        if (isXmlContent)
                        {
                            body = new StringContent(GetXmlSerializedTodoItemInvalidDoneStatus(tids), Encoding.UTF8, contentType);
                        }
                        else
                        {
                            body = new StringContent(JsonSerializer.Serialize(tids), Encoding.UTF8, contentType);
                        }

                        if (!executer.PostRequest(acceptHeaders, "todos", body, customHeaders))
                        {
                            Console.WriteLine("Add Todo Invalid Done Status failed.");
                            Environment.Exit(-8);
                        }
                        // Expect 404 HTTP code
                        Console.WriteLine(executer.responseBody);
                        break;

                    case getTodoGzip:
                        debuggerHugger = false;
                        acceptHeaders.Add(acceptType);
                        // should get NOT ACCEPTABLE response

                        if (!executer.GetRequest(acceptHeaders, "todos", customHeaders))
                        {
                            Console.WriteLine("Get Todos in GZip failed.");
                            Environment.Exit(-9);
                        }
                        // Expect 406 HTTP code
                        Console.WriteLine(executer.responseBody);
                        break;

                    case addTodoUnsupportedContentType:
                        debuggerHugger = false;
                        acceptHeaders.Add(acceptType);

                        todoNoId.title = actionCount.ToString() + " AddTodoUnsupportedContentType";
                        todoNoId.doneStatus = true;
                        todoNoId.description = "Created by POST method";

                        contentType = "application/text";

                        Console.WriteLine("request Content-Type = {0}", contentType);

                        body = new StringContent(JsonSerializer.Serialize(todoNoId), Encoding.UTF8, contentType);

                        if (!executer.PostRequest(acceptHeaders, "todos", body, customHeaders))
                        {
                            Console.WriteLine("Add Todo Unsupported Content Type failed.");
                            Environment.Exit(-28);
                        }

                        // Expect 415 HTTP code
                        Console.WriteLine(executer.responseBody);
                        break;

                    case deleteHeartbeat:
                        debuggerHugger = false;
                        acceptHeaders.Add(acceptType);

                        if (!executer.DeleteRequest(acceptHeaders, "heartbeat"))
                        {
                            Console.WriteLine("Delete Heartbeat failed.");
                            Environment.Exit(-18);
                        }

                        Console.WriteLine(executer.responseBody);
                        break;

                    case patchHeartbeat:
                        debuggerHugger = false;
                        acceptHeaders.Add(acceptType);

                        if (!executer.PatchRequest(acceptHeaders, "heartbeat", new StringContent("No nada, nothing")))
                        {
                            Console.WriteLine("Patch Heartbeat failed.");
                            Environment.Exit(-19);
                        }

                        Console.WriteLine(executer.responseBody);
                        break;

                    case traceHeartbeat:
                        debuggerHugger = false;
                        acceptHeaders.Add(acceptType);

                        if (!executer.TraceRequest(acceptHeaders, "heartbeat"))
                        {
                            Console.WriteLine("Trace Heartbeat failed.");
                            Environment.Exit(-19);
                        }

                        Console.WriteLine(executer.responseBody);
                        break;

                    case getSecretTokenFailedAuth:
                        debuggerHugger = false;
                        acceptHeaders.Add(acceptType);

                        Console.WriteLine("request Content-Type = {0}", contentType);

                        if (!executer.PostRequest(acceptHeaders, "secret/token", new StringContent(""), customHeaders, "Doogielicious:password"))
                        {
                            Console.WriteLine("Get Secret Token Failed Auth failed.");
                            Environment.Exit(-9);
                        }

                        Console.WriteLine(executer.responseBody);
                        break;

                    case getSecretTokenAuthPass:
                        debuggerHugger = true;
                        acceptHeaders.Add(acceptType);

                        Console.WriteLine("request Content-Type = {0}", contentType);

                        if (!executer.PostRequest(acceptHeaders, "secret/token", new StringContent(""), customHeaders, "admin:password"))
                        {
                            Console.WriteLine("Get Secret Token Auth Pass failed.");
                            Environment.Exit(-10);
                        }

                        if (executer.response.Headers.Contains("X-AUTH-TOKEN"))
                        {
                            IEnumerable<string> values;
                            if (executer.response.Headers.TryGetValues("X-AUTH-TOKEN", out values))
                            {
                                var enumerator = values.GetEnumerator();
                                if (enumerator.MoveNext())
                                { 
                                    AuthToken = enumerator.Current;
                                    secretToken = true;
                                }
                                else
                                {
                                    Console.WriteLine("X-AUTH-TOKEN enumerator has no next value in response from getSecretTokenAuthPass");
                                    Environment.Exit(-22);
                                }
                            }
                            else
                            {
                                Console.WriteLine("X-AUTH-TOKEN has no value in response from getSecretTokenAuthPass");
                                Environment.Exit(-21);
                            }
                        }
                        else
                        {
                            Console.WriteLine("X-AUTH-TOKEN header not returned from getSecretTokenAuthPass");
                            Environment.Exit(-20);
                        }

                        Console.WriteLine("Auth Token = {0}", AuthToken);
                        Console.WriteLine(executer.responseBody);
                        break;

                    case getSecretNoteWrongAuthToken:
                        debuggerHugger = false;
                        acceptHeaders.Add(acceptType);

                        customHeaders.Add(new[] { "X-AUTH-TOKEN", "This ain't right" });

                        // issue the GET request
                        if (!executer.GetRequest(acceptHeaders, "secret/note", customHeaders))
                        {
                            Console.WriteLine("Get Secret Note Wrong Auth Token failed.");
                            Environment.Exit(-15);
                        }

                        Console.WriteLine(executer.responseBody);
                        break;

                    case getSecretNoteNoAuthToken:
                        debuggerHugger = false;
                        acceptHeaders.Add(acceptType);

                        // issue the GET request
                        if (!executer.GetRequest(acceptHeaders, "secret/note", customHeaders))
                        {
                            Console.WriteLine("Get Secret Note No Auth Token failed.");
                            Environment.Exit(-16);
                        }

                        Console.WriteLine(executer.responseBody);
                        break;

                    case setSecretNoteWrongAuthToken:
                        debuggerHugger = false;
                        acceptHeaders.Add(acceptType);

                        customHeaders.Add(new[] { "X-AUTH-TOKEN", "This ain't right" });

                        if (!executer.PostRequest(acceptHeaders, "secret/note", new StringContent(@"{""note"":""my note""}"), customHeaders))
                        {
                             Console.WriteLine("Set Secret Note Wrong Auth Token failed.");
                            Environment.Exit(-29);
                        }

                        Console.WriteLine(executer.responseBody);
                        break;

                    case setSecretNoteNoAuthToken:
                        debuggerHugger = false;
                        acceptHeaders.Add(acceptType);

                        if (!executer.PostRequest(acceptHeaders, "secret/note", new StringContent(@"{""note"":""my note""}"), customHeaders))
                        {
                            Console.WriteLine("Set Secret Note No Auth Token failed.");
                            Environment.Exit(-39);
                        }

                        Console.WriteLine(executer.responseBody);
                        break;

                    case getSecretNote:
                        debuggerHugger = true;
                        acceptHeaders.Add(acceptType);

                        Console.WriteLine("AuthToken = {0}", AuthToken);

                        customHeaders.Add(new[] { "X-AUTH-TOKEN", AuthToken });

                        // issue the GET request
                        if (!executer.GetRequest(acceptHeaders, "secret/note", customHeaders))
                        {
                            Console.WriteLine("Get Secret Note failed.");
                            Environment.Exit(-35);
                        }

                        Console.WriteLine(executer.responseBody);
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

            if (waitForObserverKeystroke || debuggerHugger)
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

            if (!secretToken)
            {
                actions.Add(getSecretTokenAuthPass); // challenge
            }

            if (secretToken)
            {
                actions.Add(getSecretNote); // challenge
            }

            if (includeSelfLinkNoise)
            {
                if (activeTodosCount + resolvedTodosCount > 0)
                {
                    actions.Add(editTodo);
                    actions.Add(getChallenges); // challenge
                    actions.Add(getTodoById); // challenge
                    actions.Add(getTodosHead); // challenge
                    actions.Add(getTodosOPTIONS); // challenge
                    actions.Add(getXMLTodosList); // challenge
                    actions.Add(getAnyTodosList); // challenge
                    actions.Add(getXMLJSONTodosList); // challenge
                    actions.Add(getNoAcceptTodosList); // challenge
                    if (resolvedTodosCount > 0)
                    {
                        actions.Add(getResolvedTodos); // challenge
                    }
                }
                actions.Add(createNewChallengerSession); // challenge
                actions.Add(getTodoFail); // challenge
                actions.Add(getNonexistentTodo); // challenge
                actions.Add(addTodoInvalidDoneStatus); // challenge
                actions.Add(getTodoGzip); // challenge
                actions.Add(addTodoUnsupportedContentType); // challenge
                actions.Add(deleteHeartbeat); // challenge
                actions.Add(patchHeartbeat); // challenge
                actions.Add(traceHeartbeat); // challenge
                actions.Add(getSecretTokenFailedAuth); // challenge
                actions.Add(getSecretNoteWrongAuthToken); // challenge
                actions.Add(getSecretNoteNoAuthToken); // challenge
                actions.Add(setSecretNoteWrongAuthToken); // challenge
                actions.Add(setSecretNoteNoAuthToken); // challenge
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
                    if (activeTodosCount + resolvedTodosCount > maximumTodosCount)
                    {
                        Console.WriteLine("active, resolved = {0}, {1}", activeTodosCount, resolvedTodosCount);
                    }
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

            /*
                case getChallenges:
                    break;
case getTodoById:
                    break;
case getResolvedTodos:
                    break;
case getTodosHead:
                    break;
case getTodosOPTIONS:
                    break;
case getXMLTodosList:
                    break;
case getAnyTodosList:
                    break;
case getXMLJSONTodosList:
                    break;
case getNoAcceptTodosList:
                    break;
             */
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
                case getChallenges:
                    break;
                case getTodoById:
                    break;
                case getResolvedTodos:
                    break;
                case getTodosHead:
                    break;
                case getTodosOPTIONS:
                    break;
                case getXMLTodosList:
                    break;
                case getAnyTodosList:
                    break;
                case getXMLJSONTodosList:
                    break;
                case getNoAcceptTodosList:
                    break;

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
            string endState = String.Empty;

            if (AbstractTheState)
            {
                string[] stateArray = { vState[1], vState[2], vState[3] };

                abstractTodos = String.Join(", ", stateArray);
                endState = GetAbstractEndState(abstractTodos, action);
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
                case getChallenges:
                case getTodoById:
                case getResolvedTodos:
                case getTodosHead:
                case getTodosOPTIONS:
                case getXMLTodosList:
                case getAnyTodosList:
                case getXMLJSONTodosList:
                case getNoAcceptTodosList:
                case createNewChallengerSession:
                case getTodoFail:
                case getNonexistentTodo:
                case addTodoInvalidDoneStatus:
                case getTodoGzip:
                case addTodoUnsupportedContentType:
                case deleteHeartbeat:
                case patchHeartbeat:
                case traceHeartbeat:
                case getSecretTokenFailedAuth:
                case getSecretNoteWrongAuthToken:
                case getSecretNoteNoAuthToken:
                case setSecretNoteWrongAuthToken:
                case setSecretNoteNoAuthToken:
                case getSecretNote:
                    break;

                case getSecretTokenAuthPass:
                    secretToken = true;
                    break;

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
            endState = StringifyStateVector(inSession);
            return endState;
        }
    }
}
