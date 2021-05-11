using System;
using System.Collections.Generic;
using SeriousQualityEzModel;

namespace TodoAPIs3NodesE
{
    class TodoAPIs3NodesEProgram
    {
        static int Main()
        {
            APIs client = new APIs();
            client.SelfLinkTreatment = SelfLinkTreatmentChoice.SkipAll;
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

            graph.RandomDestinationCoverage("TodoAPIs3NodesE", 3);
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

        // A counter of items in the todos list.
        // The real system under test initializes the list with 10 items.
        uint todosCount = 0;

        // A helper variable to limit the size of the state-transition table, and
        // thus also limit the size of the model graph.
        const uint maxTodos = 1;

        // Actions handled by APIs
        const string startSession = "Start Session";
        const string endSession = "End Session";
        const string getTodos = "Get Todos List";
        const string addTodo = "Add a Todo";
        const string getTodo = "Get a Todo";
        const string editTodo = "Edit a Todo";
        const string deleteTodo = "Delete a Todo";

        string StringifyStateVector(bool inSession, uint numTodos)
        {
            string s = String.Format("InSession.{0}, Todos.{1}", inSession, numTodos);
            return s;
        }

        // IEzModelClient Interface method
        public string GetInitialState()
        {
            return StringifyStateVector(svInSession, todosCount);
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

            if (!inSession)
            {
                actions.Add(startSession);
                return actions;
            }

            actions.Add(addTodo);
            actions.Add(deleteTodo);
            actions.Add(getTodos);
            actions.Add(endSession);

            if (includeSelfLinkNoise)
            {
            }

            actions.Add(getTodo);
            actions.Add(editTodo);

            return actions;
        }

        // IEzModelClient Interface method
        public string GetEndState(string startState, string action)
        {
            // We must parse the startState, else we will 
            string[] vState = startState.Split(", ");
            bool inSession = vState[0].Contains("True") ? true : false;
            uint numTodos = uint.Parse(vState[1].Split(".")[1]);

            switch (action)
            {
                case startSession:
                    inSession = true;
                    break;
                case endSession:
                    inSession = false;
                    numTodos = 0;
                    break;

                case getTodos:
                case getTodo:
                case editTodo:
                    break;
                case addTodo:
                    if (numTodos < maxTodos)
                    {
                        numTodos++;
                    }
                    break;
                case deleteTodo:
                    if (numTodos > 0)
                    {
                        numTodos--;
                    }
                    break;
                default:
                    Console.WriteLine("ERROR: Unknown action '{0}' in GetEndState()", action);
                    break;
            }
            return StringifyStateVector(inSession, numTodos);
        }
    }
}
