using System;
using System.Collections.Generic;
using SeriousQualityEzModel;

namespace AlanRichardsonAPIs
{
    class Program
    {
        static void Main(string[] args)
        {
            APIs rules;

            if (args.Length < 2)
            {
                rules = new APIs(10);
            }
            else
            {
                uint arg = uint.Parse(args[1]);
                if (arg < 10)
                {
                    arg = 10;
                }
                rules = new APIs(arg);
            }

            GeneratedGraph graph = new GeneratedGraph(rules);

            graph.DisplayStateTable(); // Display the Excel-format state table
            Console.ReadLine();
        }
    }

    // This struct is needed only for traversals.
    //struct ToDo
    //{
    //    // The first four properties match those of a todo item in
    //    // the system under test.
    //    int id;
    //    string title;
    //    bool doneStatus;
    //    string description;

    //    // The final three properties are for checking the validation rules
    //    // of the system under test.  If any of the properties are allowed,
    //    // then the validation rules of the system under test allow arbitrary
    //    // fields in the data.
    //    string nuisanceString;
    //    int nuisanceInt;
    //    bool nuisanceBool;
    //}

    public class APIs : IUserRules
    {
        // Initially the system is not running, and this affects a lot of
        // state.
        bool svRunning = false;

        // Reduce state explosion by bracketing the number of todos in the
        // todos list into three classes.
        const string zeroTodos = "Zero";
        const string betweenZeroAndMaximumTodos = "BetweenZeroAndMaximum";
        const string maximumTodos = "Maximum";

        string svTodosClassString = betweenZeroAndMaximumTodos;

        // A counter of items in the todos list.
        // The system under test initializes the list with 10 items.
        uint svNumTodos = 10;

        // Once the X-AUTH-TOKEN exists, there isn't a way to get rid of it
        // except for stopping the system under test.
        bool svXAuthTokenExists = false;

        // The X-CHALLENGER GUID is created / returned from the system under test.
        // It will be unknown during each new run of the system under test, until
        // it is requested.  It must be supplied with each multi-player session.
        bool svXChallengerGuidExists = false;

        // A helper variable to limit the size of the state-transition table, and
        // thus also limit the size of the model graph.
        // This is adjusted by the command-line argument.
        uint maxTodos = 10;

        // Helper variables, really for traversals.  Not needed to explain
        // state transitions..


        //// State Variables
        //// The secret note is persisted in the playerdata of the system under test
        //// We must read the playerdata to initialize the secret note.
        //string svSecretNote = "";

        //// Populate the todos list during object constructor.
        //List<ToDo> todosList = new List<ToDo>();

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
        // Actions outside of the APIs that cover legitimate REST methods
        const string invalidRequest = "invalidRequest";
        //const string invalidGetTodo404 = "InvalidEndpointGetTodo";
        //const string invalidGetTodos404 = "InvalidIdGetTodos";
        //const string invalidPostTodos400 = "InvalidContentPostTodos";
        //const string invalidGetTodos406 = "InvalidAcceptGetTodos";
        //const string invalidPostTodos415 = "InvalidContentTypePostTodos";
        //const string invalidDeleteHeartbeat405 = "MethodNotAllowedDeleteHeartbeat";
        //const string serverErrorPatchHeartbeat500 = "InternalServerErrorPatchHeartbeat";
        //const string serverErrorTraceHeartbeat501 = "ServerNotImplementedTraceHeartbeat";
        //const string invalidAuthGetSecretToken401 = "InvalidAuthGetSecretToken";
        //const string invalidNotAuthorizedGetSecretNote403 = "XAuthTokenNotValidGetSecretNote";
        //const string invalidAuthHeaderMissingGetSecretNote401 = "XAuthTokenMissingGetSecretNote";
        //const string invalidNotAuthorizedPostSecretNote403 = "XAuthTokenNotValidPostSecretNote";
        //const string invalidAuthHeaderMissingPostSecretNote401 = "XAuthTokenMissingPostSecretNote";

        public APIs(uint max)
        {
            maxTodos = max;
            // During traversals: 
            // Read the todos list from the system under test.
            // Read the secret note from the player data.
        }

        // Swap the following statement in to see state explosion due to explicit todos list length 
        // string StringifyStateVector(bool running, uint numTodos, bool xAuthTokenExists, bool xChallengerGuidExists)
        // Comment this out if you swap in the previous statement.
        string StringifyStateVector(bool running, string todosClass, bool xAuthTokenExists, bool xChallengerGuidExists)
        {
            // Swap the following statement in to see state explosion due to explicit todos list length 
            // string s = String.Format("Running.{0}, Todos.{1}, XAuth.{2}, XChallenger.{3}", running, numTodos, xAuthTokenExists, xChallengerGuidExists);
            // Comment this out if you swap in the previous statement.
            string s = String.Format("Running.{0}, Todos.{1}, XAuth.{2}, XChallenger.{3}", running, todosClass, xAuthTokenExists, xChallengerGuidExists);
            return s;
        }

        // Interface method
        public string GetInitialState()
        {
            // Swap the following statement in to see state explosion due to explicit todos list length
            // return StringifyStateVector(svRunning, svNumTodos, svXAuthTokenExists, svXChallengerGuidExists);
            // Comment this out if you swap in the previous statement.
            return StringifyStateVector(svRunning, svTodosClassString, svXAuthTokenExists, svXChallengerGuidExists);
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

            // Swap the following statement in to see state explosion due to explicit todos list length
            // uint numTodos = uint.Parse(vState[1].Split(".")[1]);
            // Comment this out if you swap in the previous statement.
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
                    actions.Add(postTodos);
                    actions.Add(postMaximumTodo);
                    actions.Add(deleteTodoId);
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

            // Explicit, invalid actions found in the API Challenges list.
            // Being specific about invalid actions could lead to confusion
            // when interpreting output from the
            // traversal program in that implementing invalid actions not
            // listed below would be outside the scope of the model.
            // A single, invalidRequest link in the model gives latitude to
            // the traversal program to implement any amount of specific
            // invalid reuqests, thus the traversal program can exceed the
            // coverage of the API Challenges list and still match the model.
            //actions.Add(invalidGetTodo404);
            //actions.Add(invalidGetTodos404);
            //actions.Add(invalidPostTodos400);
            //actions.Add(invalidGetTodos406);
            //actions.Add(invalidPostTodos415);
            //actions.Add(invalidDeleteHeartbeat405);
            //actions.Add(serverErrorPatchHeartbeat500);
            //actions.Add(serverErrorTraceHeartbeat501);
            //actions.Add(invalidAuthGetSecretToken401);
            //actions.Add(invalidNotAuthorizedGetSecretNote403);
            //actions.Add(invalidAuthHeaderMissingGetSecretNote401);
            //actions.Add(invalidNotAuthorizedPostSecretNote403);
            //actions.Add(invalidAuthHeaderMissingPostSecretNote401);

            // if (numTodos > 0)
            //            if (todosClass != zeroTodos)
            //            {
            actions.Add(getTodoId);
                actions.Add(headTodoId);
                actions.Add(postTodoId);
                actions.Add(putTodoId);
                actions.Add(deleteTodoId);
//            }

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
            //            uint numTodos = uint.Parse(vState[1].Split(".")[1]);
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
                    running = false;
                    // The software restores the todos list to the initial state.
                    // numTodos = 10;
                    todosClass = betweenZeroAndMaximumTodos;
                    break;
                case getTodos:
                case headTodos:
                case getTodoId:
                case headTodoId:
                case postTodoId:
                case putTodoId:
                    break;
                case postTodos:
                    // numTodos++;
                    if (todosClass == zeroTodos)
                    {
                        todosClass = betweenZeroAndMaximumTodos;
                    }
                    break;
                case deleteTodoId:
//                    numTodos--;
                    if (todosClass == maximumTodos)
                    {
                        todosClass = betweenZeroAndMaximumTodos;
                    }
                    break;
                case deleteFinalTodoId:
                    todosClass = zeroTodos;
                    break;
                case postMaximumTodo:
                    todosClass = maximumTodos;
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
            //            return StringifyStateVector(running, numTodos, xAuthTokenExists, xChallengerGuidExists);
            return StringifyStateVector(running, todosClass, xAuthTokenExists, xChallengerGuidExists);

        }
    }
}
