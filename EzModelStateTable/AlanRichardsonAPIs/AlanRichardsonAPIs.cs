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

        // Legitimate HTTP Actions not documented by APIs

        public APIs(uint max)
        {
            maxTodos = max;
            // During traversals: 
            // Read the todos list from the system under test.
            // Read the secret note from the player data.
        }

        string StringifyStateVector(bool running, uint numTodos, bool xAuthTokenExists, bool xChallengerGuidExists)
        {
            string s = String.Format("Running.{0}, Todos.{1}, XAuth.{2}, XChallenger.{3}", running, numTodos, xAuthTokenExists, xChallengerGuidExists);
            return s;
        }

        // Interface method
        public string GetInitialState()
        {
            return StringifyStateVector(svRunning, svNumTodos, svXAuthTokenExists, svXChallengerGuidExists);
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
            uint numTodos = uint.Parse(vState[1].Split(".")[1]);
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
            if (numTodos <= maxTodos)
            {
                actions.Add(postTodos);
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

            if (numTodos > 0)
            {
                actions.Add(getTodoId);
                actions.Add(headTodoId);
                actions.Add(postTodoId);
                actions.Add(putTodoId);
                actions.Add(deleteTodoId);
            }

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
            uint numTodos = uint.Parse(vState[1].Split(".")[1]);
            bool xAuthTokenExists = vState[2].Contains("True") ? true : false;
            bool xChallengerGuidExists = vState[3].Contains("True") ? true : false;

            switch (action)
            {
                case startup:
                    running = true;
                    break;
                case shutdown:
                    running = false;
                    // The software restores the todos list to the initial state.
                    numTodos = 10;
                    break;
                case getTodos:
                case headTodos:
                case getTodoId:
                case headTodoId:
                case postTodoId:
                case putTodoId:
                    break;
                case postTodos:
                    numTodos++;
                    break;
                case deleteTodoId:
                    numTodos--;
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
            return StringifyStateVector(running, numTodos, xAuthTokenExists, xChallengerGuidExists);
        }
    }
}
