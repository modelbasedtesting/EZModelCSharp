using System;
using System.Collections.Generic;
using SeriousQualityEzModel;

namespace NewAbstract
{
    class AbstractModelProgram
    {
        static int Main()
        {
            TemplateClient client = new TemplateClient();
            client.SelfLinkTreatment = SelfLinkTreatmentChoice.AllowAll;
            client.IncludeSelfLinkNoise = true;

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

            graph.RandomDestinationCoverage("NewAbstract", 3);
            return 0;
        }
    }

    public class TemplateClient : IEzModelClient
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

        // TODO: Declare state variables
        // Example:
        string[] svNode = { "0,0", "0 < P+Q < max", "R,0", "0,S", "P+Q == max" };

        // TODO: Declare transitions
        // SUGGESTION: Choose "const string" as the transition
        // data type whenever possible.  A "const string" value
        // can be utilized in a switch case, which is useful in
        // GetEndState().
        // Example:
        const string actionA = "A"; // 0->1
        const string actionB = "B"; // 1->1
        const string actionC = "C"; // 1->4
        const string actionD = "D"; // 4->1
        const string actionE = "E"; // 1->0
        const string actionF = "F"; // 1->2
        const string actionG = "G"; // 2->1
        const string actionH = "H"; // 2->0
        const string actionJ = "J"; // 0->2
        const string actionK = "K"; // 1->3
        const string actionM = "M"; // 3->1
        const string actionN = "N"; // 0->0
        const string actionR = "R"; // 2->2
        const string actionS = "S"; // 3->3
        const string actionT = "T"; // 4->4
        const string actionV = "V"; // 3->4
        const string actionW = "W"; // 4->3
        const string actionY = "Y"; // 0->4
        const string actionZ = "Z"; // 4->0
        const string actionAA = "AA"; // 2->4
        const string actionAB = "AB"; // 4->2
        const string actionAC = "AC"; // 0->3
        const string actionAD = "AD"; // 3->0
        const string actionAE = "AE"; // 2->3
        const string actionAF = "AF"; // 3->2

        string StringifyStateVector(string node)
        {
            return node;
        }

        // IEzModelClient Interface method
        public string GetInitialState()
        {
            return StringifyStateVector(svNode[0]);
        }

        // IEzModelClient Interface method
        public List<string> GetAvailableActions(string startState)
        {
            List<string> actions = new List<string>();

            switch (startState)
            {
                case "0,0":
                    actions.Add(actionA);
                    actions.Add(actionJ);
                    actions.Add(actionN);
                    actions.Add(actionY);
                    actions.Add(actionAC);
                    break;
                case "0 < P+Q < max":
                    actions.Add(actionB);
                    actions.Add(actionC);
                    actions.Add(actionE);
                    actions.Add(actionF);
                    actions.Add(actionK);
                    break;
                case "R,0":
                    actions.Add(actionG);
                    actions.Add(actionH);
                    actions.Add(actionR);
                    actions.Add(actionAA);
                    actions.Add(actionAE);
                    break;
                case "0,S":
                    actions.Add(actionM);
                    actions.Add(actionS);
                    actions.Add(actionV);
                    actions.Add(actionAD);
                    actions.Add(actionAF);
                    break;
                case "P+Q == max":
                    actions.Add(actionD);
                    actions.Add(actionT);
                    actions.Add(actionW);
                    actions.Add(actionZ);
                    actions.Add(actionAB);
                    break;
                default:
                    Console.WriteLine("Unknown startState {0} in GetAvailableActions()", startState);
                    break;
            }
            return actions;
        }

        // IEzModelClient Interface method
        public string GetEndState(string startState, string action)
        {
            switch (action)
            {
                case actionE:
                case actionH:
                case actionN:
                case actionZ:
                case actionAD:
                    return svNode[0];

                case actionA:
                case actionB:
                case actionD:
                case actionG:
                case actionM:
                    return svNode[1];

                case actionF:
                case actionJ:
                case actionR:
                case actionAB:
                case actionAF:
                    return svNode[2];

                case actionK:
                case actionS:
                case actionW:
                case actionAC:
                case actionAE:
                    return svNode[3];

                case actionC:
                case actionT:
                case actionV:
                case actionY:
                case actionAA:
                    return svNode[4];
                    
                default:
                    Console.WriteLine("ERROR: Unknown action '{0}' in GetEndState()", action);
                    break;
            }
            return startState;
        }

        // IEzModelClient Interface method
        public void SetStateOfSystemUnderTest(string state)
        {
            // TODO: Implement this when NotifyAdapter is true.
        }

        // IEzModelClient Interface method
        public void ReportProblem(string initialState, string observed, string predicted, List<string> popcornTrail)
        {
            // TODO: Implement this when NotifyAdapter is true
        }

        // IEzModelClient Interface method
        public bool AreStatesAcceptablySimilar(string observed, string expected)
        {
            // TODO: Implement this when NotifyAdapter is true

            // Compare reported to expected, if unacceptable return false.
            return true;
        }

        // IEzModelClient Interface method
        public void ReportTraversal(string initialState, List<string> popcornTrail)
        {
            // TODO: Implement this when NotifyAdapter is true
        }

        // IEzModelClient Interface method
        public string AdapterTransition(string startState, string action)
        {
            // TODO: Finish implementation when NotifyAdapter is true

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
    }
}
