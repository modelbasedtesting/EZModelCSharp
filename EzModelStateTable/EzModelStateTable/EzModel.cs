// EZmodel Sample State Generation to Spreadsheet
// copyright 2019 Serious Quality LLC

// Adjusted March 15-17, 2021 with developer thoughts:
// - removed Reflection from original code
// - decoupled model rules from ezModel state table construction
// - ezModel reads model rules through C# interface IUserRules
// - person providing model rules is responsible for implementing IUserRules methods
//
//   Compliments of Doug Szabo, for Harry Robinson.

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;

namespace SeriousQualityEzModel
{
    struct StateTransition
    {
        public string startState;
        public string endState;
        public string action;

        public StateTransition(string startState, string endState, string action)
        {
            this.startState = startState;
            this.endState = endState;
            this.action = action;
        }
    }

    // The user implements a public class of rules that inherits the IUserRules interface
    public interface IUserRules
    {
        const string valueSeparator = ", ";

        string GetInitialState();
        List<string> GetAvailableActions(string startState);
        string GetEndState(string startState, string action);
    }

    public class GeneratedGraph
    {
        List<StateTransition> transitions = new List<StateTransition>();
        List<string> totalNodes;
        List<string> unexploredNodes;
        IUserRules rules; // We are able to refer to the user rules by interface, because we are only calling interface methods
        bool skipSelfLinks = false;

        string transitionSeparator = " | ";

        public void SkipSelfLinks(bool Skip)
        {
            skipSelfLinks = Skip;
        }

        public void DisplayStateTable()
        {
            Console.WriteLine("Start state{0}End state{0}Action\n", transitionSeparator);

            foreach (StateTransition t in transitions)
            {
                if (skipSelfLinks)
                {
                    if (t.startState == t.endState)
                    {
                        continue;
                    }
                }
                Console.WriteLine(t.startState + transitionSeparator + t.endState + transitionSeparator + t.action);
            }
        }

        public bool StateTableToFile( string filePath )
        {

            // Return true if able to finish writing the state table
            // to the chosen file path.
            // Return false otherwise.
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            return true;
        }

        public GeneratedGraph(IUserRules theRules)
        {
            totalNodes = new List<string>();
            unexploredNodes = new List<string>();
            rules = theRules; // we follow the Rules!

            string s = rules.GetInitialState();
            unexploredNodes.Add(s);
            totalNodes.Add(s);

            while (unexploredNodes.Count > 0)
            {
                // generate all transitions out of state s
                s = FetchUnexploredNode();
                AddNewTransitionsToGraph(s);
            }
        }

        string FetchUnexploredNode()
        {
            string s = unexploredNodes[0];
            unexploredNodes.RemoveAt(0);
            return s;
        }

        void AddNewTransitionsToGraph(string startState)
        {
            List<string> Actions = rules.GetAvailableActions(startState);

            foreach (string action in Actions)
            {
                // an endstate is generated from current state + changes from an invoked action
                string endState = rules.GetEndState(startState, action);

                // if generated endstate is new, add  to the totalNode & unexploredNode lists
                if (!totalNodes.Contains(endState))
                {
                    totalNodes.Add(endState);
                    unexploredNodes.Add(endState);
                }

                // add this {startState, action, endState} transition to the Graph
                transitions.Add(new StateTransition(startState: startState, endState: endState, action: action));
            }
        }
    }
}
