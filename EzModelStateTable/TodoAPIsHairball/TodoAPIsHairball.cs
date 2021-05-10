using System;
using System.Collections.Generic;
using SeriousQualityEzModel;

namespace TodoAPIsHairball
{
    class TodoAPIsHairballProgram
    {
        static int Main()
        {
            APIs client = new APIs();
            client.SelfLinkTreatment = SelfLinkTreatmentChoice.OnePerAction;
            client.IncludeSelfLinkNoise = true;

            // If you increase maxTodos (around line 86, below), then alter
            // the EzModelGraph arguments like so:
            // maxTransitions = 100 + 145 * maxTodos
            // maxNodes = 5 + 4 * maxTodos
            // maxActions = 35
            EzModelGraph graph = new EzModelGraph(client, 2200, 61, 35);

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

            // If you want to drive the system under test as EzModel generates test steps,
            // set client.NotifyAdapter true.
            client.NotifyAdapter = false;
            // If you want EzModel to stop generating test steps when a problem is
            // detected, set client.NotifyAdapter true, set client.StopOnProblem true,
            // and then return false from the client.AreStatesAcceptablySimilar() method.
            client.StopOnProblem = true;

            graph.RandomDestinationCoverage("Hairball", 4);
            return 0;
        }
    }

    public class APIs : IEzModelClient
    {
        SelfLinkTreatmentChoice skipSelfLinks;
        bool notifyAdapter;
        bool stopOnProblem;
        bool includeSelfLinkNoise = false;

        // Interface Properties
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

        // Initially the system is not running, and this affects a lot of
        // state.
        bool svInSession = false;

        // Reduce state explosion on todos by classifing the quantity of todos
        // into either zero or more than zero.
        bool svHasTodos = true;

        // Reduce state explosion on todos that remain todo by classifying the
        // quantity of todos todo into either zero or more than zero.
        bool svHasActiveTodos = true;

        // Once the X-AUTH-TOKEN exists, there isn't a way to get rid of it
        // except for stopping the system under test.
        bool svXAuthTokenExists = false;

        // DATA VARIABLES.  Not state variables....
        // Not done todos count can be imputed from todosCount - doneTodosCount
        uint resolvedTodosCount = 0;

        // A counter of items in the todos list.
        // The system under test initializes the list with 10 items.
        uint todosCount = 10;

        // A helper variable to limit the size of the state-transition table, and
        // thus also limit the size of the model graph.
        const uint maxTodos = 14;

        // Actions handled by APIs
        const string startSession = "Start Session";
        const string shutdown = "Shutdown";
        const string getTodos = "Get Todos List";
        const string headTodos = "Get Todos Headers";
        const string postTodos = "Add a Todo";
        const string getTodoId = "Get a Todo";
        const string headTodoId = "Get Headers of a Todo";
        const string postTodoId = "Edit a Todo by Post";
        const string putTodoId = "Edit a Todo by Put";
        const string deleteTodoId = "Delete a Todo";
        const string showDocs = "Get Documentation";
        //        const string createXChallengerGuid = "GetXChallengerGuid";
        //        const string restoreChallenger = "RestoreSavedXChallengerGuid";
        const string getChallenges = "Get Challenges";
        const string optionsChallenges = "Get Options Challenges";
        const string headChallenges = "Get Headers Challenges";
        const string getHeartbeat = "Get Service Heartbeat";
        const string optionsHeartbeat = "Get Options for Heartbeat";
        const string headHeartbeat = "Get Headers for Heartbeat";

        const string postSecretToken = "Get Secret Token";
        const string getSecretNote = "Get Secret Note";
        const string postSecretNote = "Set Secret Note";

        // Actions outside of the APIs that cover legitimate REST methods
        const string invalidGetTodo404 = "Invalid Endpoint Get Todo";
        const string invalidGetTodos404 = "Invalid Id Get Todos";
        const string invalidPostTodos400 = "Invalid Content Post Todos";
        const string invalidGetTodos406 = "Invalid Accept Get Todos";
        const string invalidPostTodos415 = "Invalid Content Type Post Todos";
        const string invalidDeleteHeartbeat405 = "Method Not Allowed Delete Heartbeat";
        const string serverErrorPatchHeartbeat500 = "Internal Server Error Patch Heartbeat";
        const string serverErrorTraceHeartbeat501 = "Server Not Implemented Trace Heartbeat";
        const string invalidAuthGetSecretToken401 = "Invalid Auth Get Secret Token";
        const string invalidNotAuthorizedGetSecretNote403 = "XAuth Token Not Valid Get Secret Note";
        const string invalidAuthHeaderMissingGetSecretNote401 = "XAuth Token Missing Get Secret Note";
        const string invalidNotAuthorizedPostSecretNote403 = "XAuth Token Not Valid Set Secret Note";
        const string invalidAuthHeaderMissingPostSecretNote401 = "XAuth Token Missing Set Secret Note";

        string StringifyStateVector(bool inSession, uint numTodos, bool xAuthTokenExists)
        {
            string s = String.Format("InSession.{0}, Todos.{1}, XAuth.{2}", inSession, numTodos, xAuthTokenExists);
            return s;
        }

        // IEzModelClient Interface method
        public string GetInitialState()
        {
            return StringifyStateVector(svInSession, todosCount, svXAuthTokenExists);
        }

        // IEzModelClient Interface method
        public void SetStateOfSystemUnderTest(string state)
        {
        }

        // IEzModelClient Interface method
        public void ReportProblem(string initialState, string observed, string predicted, List<string> popcornTrail)
        {
        }

        // IEzModelClient Interface method
        public bool AreStatesAcceptablySimilar(string observed, string expected)
        {
            // Compare reported to expected, if unacceptable return false.
            return true;
        }

        // IEzModelClient Interface method
        public void ReportTraversal(string initialState, List<string> popcornTrail)
        {

        }

        // IEzModelClient Interface method
        public string AdapterTransition(string startState, string action)
        {
            string expected = GetEndState(startState, action);
            string observed = "";

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

        // IEzModelClient Interface method
        public List<string> GetAvailableActions(string startState)
        {
            List<string> actions = new List<string>();

            // Parse the startState.
            string[] vState = startState.Split(", ");
            bool inSession = vState[0].Contains("True") ? true : false;
            uint numTodos = uint.Parse(vState[1].Split(".")[1]);
            bool xAuthTokenExists = vState[2].Contains("True") ? true : false;
            //bool xChallengerGuidExists = vState[3].Contains("True") ? true : false;

            if (!inSession)
            {
                actions.Add(startSession);
                return actions;
            }

            actions.Add(postTodos);
            actions.Add(deleteTodoId);
            actions.Add(getTodos);
            actions.Add(shutdown);

            if (includeSelfLinkNoise)
            {
                actions.Add(headTodos);
                actions.Add(showDocs);
                actions.Add(getChallenges);
                actions.Add(optionsChallenges);
                actions.Add(headChallenges);
                actions.Add(getHeartbeat);
                actions.Add(optionsHeartbeat);
                actions.Add(headHeartbeat);

                // Add specific, invalid actions found in the API Challenges list.
                // Being specific about invalid actions could lead to confusion
                // when interpreting output from the adapter program in that
                // implementing invalid actions not listed below would be outside
                // the scope of the model.
                // Option: abstraction of invalid requests in the model.
                // A single, invalidRequest link in the model would give latitude to
                // the adapter program to implement any amount of specific
                // invalid reuqests, thus the adapter program can cover more than
                // is called for by the API Challenges list and still match the model.
                actions.Add(invalidGetTodo404);
                actions.Add(invalidGetTodos404);
                actions.Add(invalidPostTodos400);
                actions.Add(invalidGetTodos406);
                actions.Add(invalidPostTodos415);
                actions.Add(invalidDeleteHeartbeat405);
                actions.Add(serverErrorPatchHeartbeat500);
                actions.Add(serverErrorTraceHeartbeat501);
                actions.Add(invalidAuthGetSecretToken401);
                actions.Add(invalidNotAuthorizedGetSecretNote403);
                actions.Add(invalidAuthHeaderMissingGetSecretNote401);
                actions.Add(invalidNotAuthorizedPostSecretNote403);
                actions.Add(invalidAuthHeaderMissingPostSecretNote401);
            }

            actions.Add(getTodoId);
            actions.Add(headTodoId);
            actions.Add(postTodoId);
            actions.Add(putTodoId);
            actions.Add(getSecretNote);
            actions.Add(postSecretNote);
            actions.Add(postSecretToken);
            //            actions.Add(restoreChallenger);
            //            actions.Add(createXChallengerGuid);

            return actions;
        }

        // IEzModelClient Interface method
        public string GetEndState(string startState, string action)
        {
            // We must parse the startState, else we will 
            string[] vState = startState.Split(", ");
            bool inSession = vState[0].Contains("True") ? true : false;
            uint numTodos = uint.Parse(vState[1].Split(".")[1]);
            bool xAuthTokenExists = vState[2].Contains("True") ? true : false;
            //            bool xChallengerGuidExists = vState[3].Contains("True") ? true : false;

            switch (action)
            {
                case invalidGetTodo404:
                case invalidGetTodos404:
                case invalidPostTodos400:
                case invalidGetTodos406:
                case invalidPostTodos415:
                case invalidDeleteHeartbeat405:
                case serverErrorPatchHeartbeat500:
                case serverErrorTraceHeartbeat501:
                case invalidAuthGetSecretToken401:
                case invalidNotAuthorizedGetSecretNote403:
                case invalidAuthHeaderMissingGetSecretNote401:
                case invalidNotAuthorizedPostSecretNote403:
                case invalidAuthHeaderMissingPostSecretNote401:
                    break;
                case startSession:
                    inSession = true;
                    break;
                case shutdown:
                    // Set all state variables back to initial state on shutdown,
                    // because if the APIs server starts up again, it will take
                    // on those initial state values.
                    inSession = false;
                    //                    xChallengerGuidExists = svXChallengerGuidExists;
                    xAuthTokenExists = svXAuthTokenExists;
                    numTodos = todosCount;
                    break;

                case getTodos:
                case headTodos:
                case getTodoId:
                case headTodoId:
                case postTodoId:
                case putTodoId:
                    break;
                case postTodos:
                    if (numTodos < maxTodos)
                    {
                        numTodos++;
                    }
                    break;
                case deleteTodoId:
                    if (numTodos > 0)
                    {
                        numTodos--;
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
            return StringifyStateVector(inSession, numTodos, xAuthTokenExists);
        }
    }
}
