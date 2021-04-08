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

namespace SeriousQualityEzModel
{
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
        List<string> transitions = new List<string>();
        List<string> totalNodes;
        List<string> unexploredNodes;
        IUserRules rules; // We are able to refer to the user rules by interface, because we are only calling interface methods

        string transitionSeparator = " | ";

        string BuildTransition(string startState, string action, string endState)
        {
            string t = startState + transitionSeparator + action + transitionSeparator + endState;
            return t;
        }

        public void DisplayStateTable()
        {
            Console.WriteLine("Start state{0}Action{0}End state\n", transitionSeparator);

            foreach (string t in transitions)
            {
                Console.WriteLine(t);

            int locSeparator1 = t.IndexOf(transitionSeparator);
            int locSeparator2 = t.IndexOf(transitionSeparator,locSeparator1+1);

            string startState = t.Substring(0,locSeparator1-1);
            string action = t.Substring(locSeparator1+1, locSeparator2-locSeparator1-1);
            string endState = t.Substring(locSeparator2 + 1);

            string s = startState + transitionSeparator + endState + transitionSeparator + action;
            Console.WriteLine(s);

         }
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
                transitions.Add(BuildTransition(startState, action, endState));
            }
        }
    }
}
