using System;
using System.Collections.Generic;
using SeriousQualityEzModel;

namespace TodoAPIsReleaseTokenA
{
    class TodoAPIsReleaseTokenAProgram
    {
        static int Main()
        {
            APIs client = new APIs();
            client.SelfLinkTreatment = SelfLinkTreatmentChoice.OnePerAction;
            client.IncludeSelfLinkNoise = true;

            // We learned after generating the graph that it has 62 edges,
            // 13 nodes, and uses 25 actions.  Those specific numbers are
            // fed in to the constructor as a check that the graph can be
            // created and traversed when exactly enough graph components
            // are allocated.  Try reducing any of the three arguments and
            // observe the consequences.
            EzModelGraph graph = new EzModelGraph(client, 1200, 100, 25, EzModelGraph.LayoutRankDirection.TopDown);

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

            graph.RandomDestinationCoverage("TodoAPIsReleaseTokenA", 7);
            return 0;
        }
    }

    public class APIs : IEzModelClient
    {
        Random rnd = new Random(DateTime.Now.Millisecond);
        SelfLinkTreatmentChoice selfLinkTreatment;
        bool notifyAdapter;
        bool stopOnProblem;
        bool includeSelfLinkNoise = false;

        // IEzModelClient Interface Property
        public SelfLinkTreatmentChoice SelfLinkTreatment
        {
            get => selfLinkTreatment;
            set => selfLinkTreatment = value;
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
        enum ResolvedTodos { Zero, Some, Max };
        enum ActiveTodos { Zero, Some, Max };

        ResolvedTodos svResolvedTodos = ResolvedTodos.Zero;
        ActiveTodos svActiveTodos = ActiveTodos.Some;
        // Once the X-AUTH-TOKEN exists, there isn't a way to get rid of it
        // except for stopping the system under test.
        bool svAuthTokenExists = false;
        bool svSecretNoteExists = false;

        // Actions handled by APIs
        const string startSession = "Start Session";
        const string endSession = "End Session";
        const string addSomeActiveTodos = "Add some Active Todos";
        const string addSomeResolvedTodos = "Add some Resolved Todos";
        const string addAllActiveTodos = "Add all Active Todos";
        const string addAllResolvedTodos = "Add all Resolved Todos";
        const string editTodos = "Edit Todos";
        const string deleteAllResolvedTodos = "Delete all Resolved Todos";
        const string deleteAllActiveTodos = "Delete all Active Todos";
        const string deleteSomeResolvedTodos = "Delete some Resolved Todos";
        const string deleteSomeActiveTodos = "Delete some Active Todos";
        const string deleteAllTodos = "Delete All Todos";
        const string resolveSomeActiveTodos = "Resolve some Active Todos";
        const string resolveAllActiveTodos = "Resolve all Active Todos";
        const string activateSomeResolvedTodos = "Activate some Resolved Todos";
        const string activateAllResolvedTodos = "Activate all Resolved Todos";

        const string getAuthToken = "Get Auth Token";
        const string getSecretNote = "Get Secret Note By Token";
        const string setSecretNote = "Set Secret Note By Token";

        const string releaseAuthToken = "Release Auth Token";
        const string clearSecretNote = "Clear Secret Note";

        string StringifyStateVector(bool inSession, ResolvedTodos resolvedTodos, ActiveTodos activeTodos, bool authTokenExists, bool secretNoteExists)
        {
            string s = String.Format("InSession.{0}, ResolvedTodos.{1}, ActiveTodos.{2}, AuthToken.{3}, SecretNote.{4}", inSession, resolvedTodos.ToString(), activeTodos.ToString(), authTokenExists.ToString(), secretNoteExists.ToString());
            return s;
        }

        // Interface method
        public string GetInitialState()
        {
            return StringifyStateVector(svInSession, svResolvedTodos, svActiveTodos, svAuthTokenExists, svSecretNoteExists);
        }

        // Interface method
        public void SetStateOfSystemUnderTest(string state)
        {
        }

        // Interface method
        public void ReportProblem(string initialState, string observed, string predicted, List<string> popcornTrail)
        {
        }

        // Interface method
        public bool AreStatesAcceptablySimilar(string observed, string expected)
        {
            // Compare reported to expected, if unacceptable return false.
            return true;
        }

        // Interface method
        public void ReportTraversal(string initialState, List<string> popcornTrail)
        {

        }

        // Interface method
        public string AdapterTransition(string startState, string action)
        {
            string expected = GetEndState(startState, action);
            string observed = "";

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
            bool authTokenExists = vState[3].Contains("True") ? true : false;
            bool secretNoteExists = vState[4].Contains("True") ? true : false;

            if (!inSession)
            {
                actions.Add(startSession);
                return actions;
            }

            actions.Add(endSession);

            if (includeSelfLinkNoise)
            {
                actions.Add(editTodos);
            }

            if (!authTokenExists)
            {
                actions.Add(getAuthToken);
            }
            else
            {
                actions.Add(releaseAuthToken);
                actions.Add(getSecretNote);
                actions.Add(setSecretNote);
                if (secretNoteExists)
                {
                    actions.Add(clearSecretNote);
                }
            }

            switch (activeTodos)
            {
                case ActiveTodos.Zero:
                    actions.Add(addSomeActiveTodos);
                    actions.Add(addAllActiveTodos);
                    break;
                case ActiveTodos.Some:
                    actions.Add(deleteAllTodos);
                    actions.Add(addSomeActiveTodos);
                    actions.Add(addAllActiveTodos);
                    actions.Add(deleteAllActiveTodos);
                    actions.Add(deleteSomeActiveTodos);
                    actions.Add(resolveSomeActiveTodos);
                    actions.Add(resolveAllActiveTodos);
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

            return actions;
        }

        // Interface method
        public string GetEndState(string startState, string action)
        {
            // We must parse the startState, else we will 
            string[] vState = startState.Split(", ");
            bool inSession = vState[0].Contains("True") ? true : false;
            string arg = vState[1].Split(".")[1];
            ResolvedTodos resolvedTodos = (ResolvedTodos)Enum.Parse(typeof(ResolvedTodos), arg);
            arg = vState[2].Split(".")[1];
            ActiveTodos activeTodos = (ActiveTodos)Enum.Parse(typeof(ActiveTodos), arg);
            bool authTokenExists = vState[3].Contains("True") ? true : false;
            bool secretNoteExists = vState[4].Contains("True") ? true : false;

            switch (action)
            {
                case startSession:
                    inSession = true;
                    break;
                case endSession:
                    inSession = false;
                    break;
                case editTodos:
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
                case deleteSomeActiveTodos:
                    if (activeTodos == ActiveTodos.Max)
                    {
                        activeTodos = ActiveTodos.Some;
                    }
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
                case getAuthToken:
                    authTokenExists = true;
                    break;
                case releaseAuthToken:
                    authTokenExists = false;
                    break;
                case getSecretNote:
                    break;
                case setSecretNote:
                    secretNoteExists = true;
                    break;
                case clearSecretNote:
                    secretNoteExists = false;
                    break;
                default:
                    Console.WriteLine("ERROR: Unknown action '{0}' in GetEndState()", action);
                    break;
            }
            return StringifyStateVector(inSession, resolvedTodos, activeTodos, authTokenExists, secretNoteExists);
        }
    }
}
