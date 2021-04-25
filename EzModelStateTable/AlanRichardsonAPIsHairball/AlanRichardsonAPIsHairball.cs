﻿using System;
using System.Collections.Generic;
using SeriousQualityEzModel;

namespace AlanRichardsonAPIsHairball
{
    class AlanRichardsonAPIsHairballProgram
    {
        static void Main()
        {
            APIs client = new APIs();
            client.SkipSelfLinks = false;

            GeneratedGraph graph = new GeneratedGraph( client, 5000, 100, 50);

            graph.DisplayStateTable(); // Display the Excel-format state table

            // write graph to dot format file
            string fname = "RichardsonHairball";
            string suffix = "0000";
            graph.CreateGraphVizFileAndImage(fname, suffix, "Initial State");

// If you want to drive the system under test as EzModel generates test steps,
// set client.NotifyAdapter true.
            client.NotifyAdapter = false;
// If you want EzModel to stop generating test steps when a problem is
// detected, set client.NotifyAdapter true, set client.StopOnProblem true,
// and then return false from the client.AreStatesAcceptablySimilar() method.
            client.StopOnProblem = true;

            graph.RandomDestinationCoverage(fname);
        }
    }

    //      
    // This struct is needed only for traversals.
    //struct ToDo
    //{
    //    // The first four properties match those of a todo item in
    //    // the system under test.
    //    int id;
    //    string title;
    //    bool doneStatus;
    //    string description;
    //}
    // Helper variables for traversals.
    //// The secret note is persisted in the playerdata of the system under test
    //// We must read the playerdata to initialize the secret note.
    //string svSecretNote = "";
    //// Populate the todos list during object constructor.
    //List<ToDo> svTodosList = new List<ToDo>();

    public class APIs : IEzModelClient
    {
        bool skipSelfLinks;
        bool notifyAdapter;
        bool stopOnProblem;

        // IEzModelClient Interface Property
        public bool SkipSelfLinks
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
        const uint maxTodos = 14;

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
        // Actions outside of the APIs that cover legitimate REST methods
        const string invalidGetTodo404 = "InvalidEndpointGetTodo";
        const string invalidGetTodos404 = "InvalidIdGetTodos";
        const string invalidPostTodos400 = "InvalidContentPostTodos";
        const string invalidGetTodos406 = "InvalidAcceptGetTodos";
        const string invalidPostTodos415 = "InvalidContentTypePostTodos";
        const string invalidDeleteHeartbeat405 = "MethodNotAllowedDeleteHeartbeat";
        const string serverErrorPatchHeartbeat500 = "InternalServerErrorPatchHeartbeat";
        const string serverErrorTraceHeartbeat501 = "ServerNotImplementedTraceHeartbeat";
        const string invalidAuthGetSecretToken401 = "InvalidAuthGetSecretToken";
        const string invalidNotAuthorizedGetSecretNote403 = "XAuthTokenNotValidGetSecretNote";
        const string invalidAuthHeaderMissingGetSecretNote401 = "XAuthTokenMissingGetSecretNote";
        const string invalidNotAuthorizedPostSecretNote403 = "XAuthTokenNotValidPostSecretNote";
        const string invalidAuthHeaderMissingPostSecretNote401 = "XAuthTokenMissingPostSecretNote";

        string StringifyStateVector(bool running, uint numTodos, bool xAuthTokenExists, bool xChallengerGuidExists)
        {
            string s = String.Format("Running.{0}, Todos.{1}, XAuth.{2}, XChallenger.{3}", running, numTodos, xAuthTokenExists, xChallengerGuidExists);
            return s;
        }

        // IEzModelClient Interface method
        public string GetInitialState()
        {
            return StringifyStateVector(svRunning, svNumTodos, svXAuthTokenExists, svXChallengerGuidExists);
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
        public string AdapterTransition(string startState, string action )
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
            bool running = vState[0].Contains("True") ? true : false;
            uint numTodos = uint.Parse(vState[1].Split(".")[1]);
            bool xAuthTokenExists = vState[2].Contains("True") ? true : false;
            bool xChallengerGuidExists = vState[3].Contains("True") ? true : false;

            if (!running)
            {
                actions.Add(startup);
                return actions;
            }

            switch (numTodos)
            {
                case 0:
                    actions.Add(postTodos);
                    break;
                case maxTodos:
                    actions.Add(deleteTodoId);
                    break;
                default:
                    actions.Add(postTodos);
                    actions.Add(deleteTodoId);
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

            if (numTodos > 0)
            {
                actions.Add(getTodoId);
                actions.Add(headTodoId);
                actions.Add(postTodoId);
                actions.Add(putTodoId);
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

        // IEzModelClient Interface method
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
                case startup:
                    running = true;
                    break;
                case shutdown:
                    // Set all state variables back to initial state on shutdown,
                    // because if the APIs server starts up again, it will take
                    // on those initial state values.
                    running = false;
                    xChallengerGuidExists = svXChallengerGuidExists;
                    xAuthTokenExists = svXAuthTokenExists;
                    numTodos = svNumTodos;
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
