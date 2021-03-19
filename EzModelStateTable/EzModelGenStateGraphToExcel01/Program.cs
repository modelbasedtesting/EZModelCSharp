// EZmodel Sample State Generation to Spreadsheet
// copyright 2019 Serious Quality LLC

//#define BOOLEAN
//#define BOOLEAN 

/*
using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections.Generic;

namespace EzModelGenStateGraphToExcel01
{
   public class ActionAttribute : Attribute { }
   public class StateVarAttribute : Attribute { }

   public class StateNode
   {
      public const char nodeSeparator = ',';

        // *************************************
        // **   DESK LAMP MODEL (EXAMPLE 1)   **
        // *************************************
        public enum DESKLAMP { ON, OFF };

        [StateVar]
        public DESKLAMP deskLamp = DESKLAMP.OFF;         // Set initial state variable value

        // Rules of Desklamp state behavior
        [Action]
        public void SwitchLampOn() { deskLamp = DESKLAMP.ON; }
        public bool SwitchLampOnEnabled() { return (deskLamp == DESKLAMP.OFF); }

        [Action]
        public void SwitchLampOff() { deskLamp = DESKLAMP.OFF; }
        public bool SwitchLampOffEnabled() { return (deskLamp == DESKLAMP.ON); }
        // ****************************
        // ** END OF DESK LAMP MODEL **
        // ****************************


        // *************************************
        //// ** TRAFFIC LIGHT MODEL (EXAMPLE 2) **
        //// *************************************
        public enum TRAFFICLIGHT { GREEN, YELLOW, RED };

        [StateVar]
        public TRAFFICLIGHT trafficLight = TRAFFICLIGHT.RED;        // Set initial state variable value

        // Rules of TrafficLight state behavior
        [Action]
        public void TurnGreen() { trafficLight = TRAFFICLIGHT.GREEN; }
        public bool TurnGreenEnabled() { return (trafficLight == TRAFFICLIGHT.RED); }

        [Action]
        public void TurnYellow() { trafficLight = TRAFFICLIGHT.YELLOW; }
        public bool TurnYellowEnabled() { return (trafficLight == TRAFFICLIGHT.GREEN); }

        [Action]
        public void TurnRed() { trafficLight = TRAFFICLIGHT.RED; }
        public bool TurnRedEnabled() { return (trafficLight == TRAFFICLIGHT.YELLOW); }

        //// ********************************
        //// ** END OF TRAFFIC LIGHT MODEL **
        //// ********************************
      public StateNode() { }

      public StateNode(StateNode n)
      {
         foreach (FieldInfo f in typeof(StateNode).GetFields(bf))
            if (Attribute.IsDefined(f, typeof(StateVarAttribute)))
               f.SetValue(this, f.GetValue(n));
      }

      //  compare state variable values
      const BindingFlags bf = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
      public static bool operator ==(StateNode lhs, StateNode rhs)
      {
         // compare the state variable fields in each object
         foreach (FieldInfo f in typeof(StateNode).GetFields(bf))
            if (Attribute.IsDefined(f, typeof(StateVarAttribute)))
               if (!f.GetValue(lhs).Equals(f.GetValue(rhs)))
                  return false;
         return true;
      }

      // node label is the state variable values as a concatenated, delimiter-separated string
      public override string ToString()
      {
         string str = "";
         foreach (FieldInfo f in typeof(StateNode).GetFields(bf))
            if (Attribute.IsDefined(f, typeof(StateVarAttribute)))
               str += f.GetValue(this).ToString() + nodeSeparator;

         return str.Remove(str.Length - 1);  // trim final separator
      }

      public List<MethodInfo> GetEnabledActions()
      {
         // this function returns a list of actions that are enabled in the current state
         List<MethodInfo> enabledActions = new List<MethodInfo>();
         foreach (MethodInfo m in (typeof(StateNode)).GetMethods())
            if (Attribute.IsDefined(m, typeof(ActionAttribute)))
            {
               // check the "Enabled" method for each action
               MethodInfo sm = typeof(StateNode).GetMethod(m.Name + "Enabled");
               if ((bool)sm.Invoke(this, null))
                  enabledActions.Add(m);
            }
         return enabledActions;
      }

      // methods to keep the compiler happy
      public static bool operator !=(StateNode lhs, StateNode rhs) { return !(lhs == rhs); }
      public override bool Equals(object o) { return (this == (StateNode)o); }
      public override int GetHashCode() { return 0; }
   }

   public class Transition
   {
      public StateNode start;
      public string action;
      public StateNode end;

      public const string transitionSeparator = "\t";
      public Transition(StateNode startState, string action, StateNode endState)
      {
         start = startState;
         this.action = action;
         end = endState;
      }

      public override string ToString()
      {
         return start + transitionSeparator + action + transitionSeparator + end;
      }
   }

   public class UserRules
    {
        // Provide methods that answer questions ezModel asks:
        //
        // Once
        // 1. what are the state variables
        //    This will be a set of identifiers.  Should they be distinct strings?
        // 2. for each state variable, what is the type?
        //    Initially, enum is the only type
        // 3. for each state variable, what is the domain?
        //    The domain for an enum type is a set of values with the only operators being equality and inequality.
        // 4. for each state variable, what is the initial value?
        //    For an enum type, pick a value from the set
        // 5. what are the actions?
        //    This will be a set of identifiers.  Should they be distinct strings?
        //
        // Iteratively
        // 6. for a given action and a given state, what is the final state?
        //    Return a state object of state variable, state value pairs.
        //    If the array is empty, it means the action is not available from the given state.
        //
        // Question: what if the UserRules returns a JSON document, such as
        //
        // {
        //     "stateVariables":
        //      [
        //        {
        //          "variable": "desklamp",
        //          "type": "enum",
        //          "domain": ["on", "off"],
        //          "initialValue": "off"
        //        },
        //        {
        //          "variable": "trafficlight",
        //          "type": "enum",
        //          "domain": ["red", "yellow", "green"],
        //          "initialValue": "red"
        //        }
        //      ],
        //     "actions":
        //      [
        //        {
        //          "name": "switchLampOn",
        //          "requisites":
        //           [
        //             {
        //               "variable": "desklamp",
        //               "value": "off"
        //             }
        //           ],
        //          "stateChange":
        //           [ 
        //             {
        //               "variable": "desklamp",
        //               "newValue": "on"
        //             }
        //           ]
        //        },
        //        {
        //          "name": "switchLampOff",
        //          "requisites":
        //           [
        //             {
        //               "variable": "desklamp",
        //               "value": "on"
        //             }
        //           ],
        //          "stateChange":
        //           [
        //             {
        //               "variable": "desklamp",
        //               "value": "off"
        //             }
        //           ]
        //        },
        //        {
        //          "name": "turnGreen",
        //          "requisites":
        //           [
        //             {
        //               "variable": "trafficlight",
        //               "value": "red"
        //             }
        //           ],
        //          "stateChange":
        //           [
        //             {
        //               "variable": "trafficlight",
        //               "value": "green"
        //             }
        //           ]
        //        },
        //        {
        //          "name": "turnYellow",
        //          "requisites":
        //           [
        //             {
        //               "variable": "trafficlight",
        //               "value": "green"
        //             }
        //           ],
        //          "stateChange":
        //           [
        //             {
        //               "variable": "trafficlight",
        //               "value": "yellow"
        //             }
        //           ]
        //        },
        //        {
        //          "name": "turnRed",
        //          "requisites":
        //           [
        //             {
        //               "variable": "trafficlight",
        //               "value": "yellow"
        //             }
        //           ],
        //          "stateChange":
        //           [
        //             {
        //               "variable": "trafficlight",
        //               "value": "red"
        //             }
        //           ]
        //        }
        //      ]
        // }
    }

    public class Program
   {
      public static void Main()
      {
         GeneratedGraph graph = new GeneratedGraph();

         graph.DisplayStateTable(); // Display the Excel-format state table
         Console.ReadLine();
      }


      public class GeneratedGraph
      {
         List<Transition> transitions;
         List<StateNode> totalNodes;
         List<StateNode> unexploredNodes;

         public void DisplayStateTable()
         {
            //Console.WriteLine("Start state,Action,End state\n");
            foreach (Transition t in transitions)
            {
               Console.WriteLine("StartState: {0}\nAction: {1}\nEndState: {2}\n", t.start.ToString(), t.action, t.end.ToString());
            }
         }

         public GeneratedGraph()
         {
            totalNodes = new List<StateNode>();
            unexploredNodes = new List<StateNode>();
            transitions = new List<Transition>();

            StateNode s = new StateNode();
            unexploredNodes.Add(s);
            totalNodes.Add(s);

            while (unexploredNodes.Count > 0)
            {
               // generate all transitions out of state s
               s = FetchUnexploredNode();
               AddNewTransitionsToGraph(s);
            }
         }

         public StateNode FetchUnexploredNode()
         {
            StateNode s = unexploredNodes[0];
            unexploredNodes.RemoveAt(0);
            return s;
         }

         public void AddNewTransitionsToGraph(StateNode startState)
         {
            foreach (MethodInfo enabledAction in startState.GetEnabledActions())
            {
               // an endstate is generated from current state + changes from an invoked action
               StateNode endState = new StateNode(startState);

               // execute action to change the endState state variables
               enabledAction.Invoke(endState, null);

               // if generated endstate is new, add  to the totalNode & unexploredNode lists
               if (!totalNodes.Contains(endState))
               {
                  totalNodes.Add(endState);
                  unexploredNodes.Add(endState);
               }

               // add this {startState, action, endState} transition to the Graph
               transitions.Add(new Transition(startState, enabledAction.Name, endState));
            }
            return;
         }
      }
   }
}
*/