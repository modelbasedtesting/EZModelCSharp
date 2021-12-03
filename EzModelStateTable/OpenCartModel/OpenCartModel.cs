// TODO: To fill in the template, carry out all the instructions in // TODO comments.

using System;
using System.Collections.Generic;

// TODO: Right-click on the project Dependencies folder and choose Add Reference...
// Check the box for EzModelStateTable.
using SeriousQualityEzModel;

namespace OpenCartModel
{
   class OpenCartModel
   {
      static int Main(string[] args)
      {
         TemplateClient client = new TemplateClient();
         client.SelfLinkTreatment = SelfLinkTreatmentChoice.SkipAll;
         client.IncludeSelfLinkNoise = true;

         EzModelGraph graph = new EzModelGraph(client, 2200, 260, 35);

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
         graph.layoutDirection = EzModelGraph.LayoutRankDirection.TopDown;

         graph.CreateGraphVizFileAndImage(EzModelGraph.GraphShape.Default);

         // If you want to drive the system under test as EzModel generates test steps,
         // set client.NotifyAdapter true.
         client.NotifyAdapter = true;
         // If you want EzModel to stop generating test steps when a problem is
         // detected, set client.NotifyAdapter true, set client.StopOnProblem true,
         // and then return false from the client.AreStatesAcceptablySimilar() method.
         client.StopOnProblem = true;

         graph.RandomDestinationCoverage("ClientTemplate", 3);
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
      //bool svTrueFalse = true;
      uint svNumCameras = 0;
      uint svNumPhones = 0;
      uint svNumComputers = 0;
      uint svNumTablets = 0;

      // TODO: Declare transitions
      // SUGGESTION: Choose "const string" as the transition
      // data type whenever possible.  A "const string" value
      // can be utilized in a switch case, which is useful in
      // GetEndState().
      // Example:
      const string addCamera = "Add Camera";
      const string addPhone = "Add Phone";
      const string addComputer = "Add Computer";
      const string addTablet = "Add Tablet";
      const string emptyTheCart = "Empty the Cart";


      string StringifyStateVector(uint svNumCameras, uint svNumPhones, uint svNumComputers, uint svNumTablets)
      {
         // TODO: Join state variable values into an ordered string

         // Example:
         string s = String.Format("Cameras.{0}, Phones.{1}, Computers.{2}, Tablets.{3}",
            svNumCameras, svNumPhones, svNumComputers, svNumTablets);
         return s;
      }

      // IEzModelClient Interface method
      public string GetInitialState()
      {
         // TODO: feed state values

         // Example:
         return StringifyStateVector(svNumCameras, svNumPhones, svNumComputers, svNumTablets);

      }

      // IEzModelClient Interface method
      public List<string> GetAvailableActions(string startState)
      {
         List<string> actions = new List<string>();

         // TODO: Parse the startState.  Example:
         string[] vState = startState.Split(", ");

         svNumCameras = uint.Parse(vState[0].Split(".")[1]);
         svNumPhones = uint.Parse(vState[1].Split(".")[1]);
         svNumComputers = uint.Parse(vState[2].Split(".")[1]);
         svNumTablets = uint.Parse(vState[3].Split(".")[1]);

         const int maxNumProd = 3;  // the highest # of each product allowed in cart

         // TODO: Accumulate actions available according to the start state.
         // Example:
         if (svNumCameras < maxNumProd)
         {
            actions.Add(addCamera);
         }

         if (svNumPhones < maxNumProd)
         {
            actions.Add(addPhone);
         }

         if (svNumComputers < maxNumProd)
         {
            actions.Add(addComputer);
         }

         if (svNumTablets < maxNumProd)
         {
            actions.Add(addTablet);
         }

         uint totalInCart = svNumCameras + svNumPhones + svNumComputers + svNumTablets;
         if (totalInCart > 0)
         {
            actions.Add(emptyTheCart);
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
         svNumCameras = uint.Parse(vState[0].Split(".")[1]);
         svNumPhones = uint.Parse(vState[1].Split(".")[1]);
         svNumComputers = uint.Parse(vState[2].Split(".")[1]);
         svNumTablets = uint.Parse(vState[3].Split(".")[1]);

         switch (action)
         {
            // TODO: update the case logic to cover actual actions.
            // Return the end state reasoned about the model.
            // Example:
            case addCamera:
               svNumCameras++;
               break;

            case addPhone:
               svNumPhones++;
               break;

            case addComputer:
               svNumComputers++;
               break;

            case addTablet:
               svNumTablets++;
               break;

            case emptyTheCart:
               svNumCameras = 0;
               svNumPhones = 0;
               svNumComputers = 0;
               svNumTablets = 0;
               break;

            default:
               Console.WriteLine("ERROR: Unknown action '{0}' in GetEndState()", action);
               break;

         }
         return StringifyStateVector(svNumCameras, svNumPhones, svNumComputers, svNumTablets);
      }

      // IEzModelClient Interface method
      public void SetStateOfSystemUnderTest(string state)
      {
         // TODO: Implement this when NotifyAdapter is true.
         // Go to OpenCart Site via Selenium commands
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
         // Big switch statement with selenium code.

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
