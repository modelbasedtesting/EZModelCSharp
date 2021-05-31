
using System;
using System.Collections.Generic;
using SeriousQualityEzModel;

namespace PetClinic
{
    class PetClinicProgram
    {
        static int Main()
        {
            TemplateClient client = new TemplateClient();
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

            graph.RandomDestinationCoverage("PetClinic", 3);
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
        static string baseAddress = "https://spring-petclinic-community.herokuapp.com";
        string[] Urls = {
            // Homepage
           "/",
           // Find Owners landing page
        "/owners/find",
            // Find Owner sub-page of Find Owners with empty lastName argument
        "/owners?lastName=",
            // Find Owner sub-page of Find Owners with non-empty lastName argument gives a list of owners with lastName starting with the argument, or
            // Last name has not been found when there is no match.
            // Add Owner sub-page of Find Owners
            "/owners/new"
        };
        // ON a bad URL we go to Error page.
        //
        // QUESTION: are the HTML pages generated dynamically when we see a
        // URL that has an index in it, like as follows?
        // Clicking an owner link on the Find Owner sub-page of Find Owners
        // goes to an Owner information form by owner ID in the url, e.g.,
        // baseAddress + /owners/2 for Betty Davis.
        // From the Owner information form clicking on Edit owner goes to a sub-form
        // with url like baseAddress + /owners/2/edit for Betty Davis.
        // Update takes us back to Owner info.
        // On the Owner info form, Add new pet takes us to baseAddress +
        // /owners/2/pets/new for Betty Davis.

        uint svOwnerCount = 10; // initial number of owners when petclinic starts
        string[] ownerInfo
        bool svTrueFalse = true;
        uint svSomeNumber = 4;

        // TODO: Declare transitions
        // SUGGESTION: Choose "const string" as the transition
        // data type whenever possible.  A "const string" value
        // can be utilized in a switch case, which is useful in
        // GetEndState().
        // Example:
        const string actionA = "Action A";
        const string actionB = "Action B";
        const string actionC = "Action C";

        /*
            string currentState = "Start";

            [Action]
            public void e_StartBrowser() { currentState = "v_HomePage"; }

            public bool e_StartBrowserEnabled()
            { return (currentState == "Start"); }
            [Action]
            public void e_Veterinarians() { currentState = "v_Veterinarians"; }

            public bool e_VeterinariansEnabled()
            { return (currentState == "v_HomePage" || currentState == "v_FindOwners"); }

            [Action]
            public void e_FindOwners() { currentState = "v_FindOwners"; }

            public bool e_FindOwnersEnabled()
            { return (currentState == "v_HomePage" 
                    || currentState == "v_Veterinarians"
                    || currentState == "v_Owners"
                    || currentState == "v_OwnerInformation"
                    || currentState == "v_NewOwners");
            }

            [Action]
            public void e_HomePage() { currentState = "v_HomePage"; }

            public bool e_HomePageEnabled()
            { return (currentState == "v_FindOwners" || currentState == "v_Veterinarians"); }

            // +++++
            // +++++ Find Owners model
            [Action]
            public void e_OwnerSearch() { currentState = "v_Owners"; }

            public bool e_OwnerSearchEnabled()
            { return (currentState == "v_FindOwners"); }

            [Action]
            public void e_AddOwner() { currentState = "v_NewOwner"; }

            public bool e_AddOwnerEnabled()
            { return (currentState == "v_FindOwners"); }

            // +++++
            // +++++ New Owner model
            [Action]
            public void e_IncorrectData() { currentState = "v_IncorrectData"; }

            public bool e_IncorrectDataEnabled()
            { return (currentState == "v_NewOwner"); }

            // Looks to me like there should be a "hit return to go back to v_NewOwner action here
            [Action]
            public void e_RepentIncorrectData() { currentState = "v_NewOwner"; }

            public bool e_RepentIncorrectDataEnabled()
            { return (currentState == "v_IncorrectData"); }

            [Action]
            public void e_CorrectData() { currentState = "v_OwnerInformation"; }

            public bool e_CorrectDataEnabled()
            { return (currentState == "v_NewOwner"); }

            // +++++
            // +++++ Owner Information model
            [Action]
            public void e_AddVisit() { currentState = "v_NewVisit"; }

            public bool e_AddVisitEnabled()
            { return (currentState == "v_OwnerInformation"); }

            [Action]
            public void e_VisitAddedFailed() { currentState = "v_NewVisit"; }

            public bool e_VisitAddedFailedEnabled()
            { return (currentState == "v_NewVisit"); }

            [Action]
            public void e_VisitAddedSuccessfully() { currentState = "v_OwnerInformation"; }

            public bool e_VisitAddedSuccessfullyEnabled()
            { return (currentState == "v_NewVisit"); }

            [Action]
            public void e_AddNewPet() { currentState = "v_NewPet"; }

            public bool e_AddNewPetEnabled()
            { return (currentState == "v_OwnerInformation"); }

            [Action]
            public void e_AddPetFailed() { currentState = "v_NewPet"; }

            public bool e_AddPetFailedEnabled()
            { return (currentState == "v_NewPet"); }


            [Action]
            public void e_AddPetSuccessfully() { currentState = "v_OwnerInformation"; }

            public bool e_AddPetSuccessfullyEnabled()
            { return (currentState == "v_NewPet"); }

            [Action]
            public void e_EditPet() { currentState = "v_Pet"; }

            public bool e_EditPetEnabled()
            { return (currentState == "v_OwnerInformation"); }

            [Action]
            public void e_UpdatePet() { currentState = "v_OwnerInformation"; }

            public bool e_UpdatePetEnabled()
            { return (currentState == "v_Pet"); }

            [Action]
            public void e_VetSearch() { currentState = "v_VetSearchResult"; }

            public bool e_VetSearchEnabled()
            { return (currentState == "v_Veterinarians"); }

            // +++++
            // As above, there needs to be an action to leave the Search Result state
            [Action]
            public void e_EndVetSearch() { currentState = "v_Veterinarians"; }

            public bool e_EndVetSearchEnabled()
            { return (currentState == "v_VetSearchResult"); }
*/

        string StringifyStateVector(bool stateArg1, uint stateArg2)
        {
            // TODO: Join state variable values into an ordered string

            // Example:
            string s = String.Format("StateArg1.{0}, StateArg2.{1}", stateArg1, stateArg2);
            return s;
        }

        // IEzModelClient Interface method
        public string GetInitialState()
        {
            // TODO: feed state values

            // Example:
            return StringifyStateVector(svTrueFalse, svSomeNumber);
        }

        // IEzModelClient Interface method
        public List<string> GetAvailableActions(string startState)
        {
            List<string> actions = new List<string>();

            // TODO: Parse the startState.  Example:
            string[] vState = startState.Split(", ");
            bool stateArg1 = vState[0].Contains("True") ? true : false;
            uint stateArg2 = uint.Parse(vState[1].Split(".")[1]);

            // TODO: Accumulate actions available according to the start state.
            // Example:
            if (!stateArg1)
            {
                actions.Add(actionA);
                return actions;
            }

            actions.Add(actionB);

            if (includeSelfLinkNoise)
            {
                actions.Add(actionC);
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
            bool stateArg1 = vState[0].Contains("True") ? true : false;
            uint stateArg2 = uint.Parse(vState[1].Split(".")[1]);

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
