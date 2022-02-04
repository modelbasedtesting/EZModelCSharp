// TODO: To fill in the template, carry out all the instructions in // TODO comments.

using System;
using System.Collections.Generic;

// TODO: Right-click on the project Dependencies folder and choose Add Reference...
// Check the box for EzModelStateTable.
using SeriousQualityEzModel;

namespace VendingMachineNamespace
{
    class VendingMachineProgram
    {
        static int Main()
        {
            VendingMachine client = new VendingMachine();
            client.SelfLinkTreatment = SelfLinkTreatmentChoice.SkipAll;
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

            graph.RandomDestinationCoverage("ClientTemplate", 3);
            return 0;
        }
    }

    public class VendingMachine : IEzModelClient
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

        List<(string, object)> state = new List<(string, object)>
        {
            ("HasCoffeeIngredients", true),
            ("HasTea", true),
            ("HasHotWater", true),
            ("InsufficientChange", false),
            ("HasZeroMoney", true),
            ("HasTeaMoney", false),
            ("HasCoffeeMoney", false),
            ("CoffeeSelected", false),
            ("TeaSelected", false),
            ("HotWaterSelected", false)
        };

        // TODO: Declare transitions
        // SUGGESTION: Choose "const string" as the transition
        // data type whenever possible.  A "const string" value
        // can be utilized in a switch case, which is useful in
        // GetEndState().
        // Example:
        const string selectTea = "Select Tea";
        const string selectCoffee = "Select Coffee";
        const string selectHotWater = "Select Hot Water";
        const string cancelSelection = "Cancel Selection";
        const string refund = "Refund";
        const string dispense = "Dispense";

        string StringifyStateVector(List<(string, object)> state)
        {
            // TODO: Join state variable values into an ordered string

            // Example:

            string stateString = "";
            foreach((string s, object o) in state )
            {
                stateString += s + "." + o.ToString() + ", ";
            }
            if (stateString.Length > 2)
            {
                stateString = stateString.Substring(0, stateString.Length - 2);
            }
            Console.WriteLine(stateString);
            return stateString;
        }

        // IEzModelClient Interface method
        public string GetInitialState()
        {
            return StringifyStateVector( state );
        }

        // IEzModelClient Interface method
        public List<string> GetAvailableActions(string startState)
        {
            List<string> actions = new List<string>();

            // TODO: Parse the startState.  Example:
            string[] vState = startState.Split(", ");

            bool HasCoffeeIngredients = vState[0].Split(".")[1] == "True" ? true : false;
            bool HasTea = vState[1].Split(".")[1] == "True" ? true : false;
            bool HasHotWater = vState[2].Split(".")[1] == "True" ? true : false;
            bool InsufficientChange = vState[3].Split(".")[1] == "True" ? true : false;
            bool HasZeroMoney = vState[4].Split(".")[1] == "True" ? true : false;
            bool HasTeaMoney = vState[5].Split(".")[1] == "True" ? true : false;
            bool HasCoffeeMoney = vState[6].Split(".")[1] == "True" ? true : false;
            bool CoffeeSelected = vState[7].Split(".")[1] == "True" ? true : false;
            bool TeaSelected = vState[8].Split(".")[1] == "True" ? true : false;
            bool HotWaterSelected = vState[9].Split(".")[1] == "True" ? true : false;

            // TODO: Accumulate actions available according to the start state.
            // Example:
            if (HasCoffeeIngredients)
            {
                actions.Add( selectCoffee );
            }

            if (HasTea)
            {
                actions.Add( selectTea );
            }

            if (HasHotWater)
            {
                actions.Add( selectHotWater );
            }

            actions.Add( Cancel );

            if (( CoffeeSelected && HasCoffeeMoney) || (TeaSelected && HasTeaMoney) || HotWaterSelected )
            {
                actions.Add( Dispense );
            }

            if (!HasZeroMoney)
            {
                actions.Add( Refund );
            }

            return actions;
        }

        // IEzModelClient Interface method
        public string GetEndState(string startState, string action)
        {
            // TODO: Parse the start state for use in helping how to address
            // the action.
            // Example:
            string[] vState = startState.Split(", ");
            bool stateArg1 = v(0].Contains("True") ? true : false;
            uint stateArg2 = uint.Parse(v(1].Split(".")[1]);

            switch (action)
            {
                // TODO: update the case logic to cover actual actions.
                // Return the end state reasoned about the model.
                // Example:
                case actionA:
                    stateArg1 = true;
                    break;

                case actionB:
                    stateArg1 = false;
                    break;

                case actionC:
                    stateArg2++;
                    break;

                default:
                    Console.WriteLine("ERROR: Unknown action '{0}' in GetEndState()", action);
                    break;
            }
            return StringifyStateVector(stateArg1, stateArg2);
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
