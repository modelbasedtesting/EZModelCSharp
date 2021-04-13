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
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Diagnostics;

namespace SeriousQualityEzModel
{
    public struct StateTransition
    {
        public string startState;
        public string endState;
        public string action;
        public int traversals;
        public double probability;

        public StateTransition(string startState, string endState, string action)
        {
            this.startState = startState;
            this.endState = endState;
            this.action = action;
            this.traversals = 0;
            this.probability = 1.0;
        }
    }

    public class StateTransitions
    {
        StateTransition[] transitions;

        // Keep track of the number of populated elements in the array
        // because C# arrays are fixed size and the .Length
        // property just tells the size of the array.
        uint count = 0;

        Random rnd = new Random(1);

        public StateTransitions(uint maximumTransitions)
        {
            transitions = new StateTransition[maximumTransitions];
        }

        public int GetTraversalsFloor()
        {
            int floor = int.MaxValue;

            for (int i = 0; i < count; i++)
            {
                if (transitions[i].traversals < floor)
                {
                    floor = transitions[i].traversals;
                }
            }
            return floor;
        }

        public int GetALowHitTransitionIndex()
        {
            // Find the lowest hit count
            int lowHit = GetTraversalsFloor();

            // Create a list of all low-hit transitions
            List<int> lowHitList = new List<int>();
            for (int i = 0; i < count; i++)
            {
                if (transitions[i].traversals == lowHit)
                {
                    lowHitList.Add(i);
                }
            }

            // Target one of the low-hit transitions 
            int low = rnd.Next(lowHitList.Count);
            return (lowHitList[low]);
        }

        public bool Add( string startState, string endState, string action )
        {
            if ( count < transitions.Length )
            {
                transitions[count].startState = startState;
                transitions[count].endState = endState;
                transitions[count].action = action;
                transitions[count].traversals = 0;
                transitions[count].probability = 1.0;
                count++;
                return true;
            }

            return false; // The transition was not added.
        }

        public uint Count()
        {
            return count;
        }

        public string StartStateByIndex( uint index )
        {
            if (index < count)
            {
                return transitions[index].startState;
            }

            return String.Empty;
        }

        public string EndStateByIndex(uint index)
        {
            if (index < count)
            {
                return transitions[index].endState;
            }

            return String.Empty;
        }

        public string ActionByIndex(uint index)
        {
            if (index < count)
            {
                return transitions[index].action;
            }

            return String.Empty;
        }

        public int TraversalsByIndex(uint index)
        {
            if (index < count)
            {
                return transitions[index].traversals;
            }

            return -1;
        }

        public int IndexOfStartState( string matchState )
        {
            for ( int i = 0; i < count; i++ )
            {
                if (transitions[i].startState == matchState)
                {
                    return i;
                }
            }

            Console.WriteLine("IndexOfStartState(): start state {0} not found in graph", matchState);
            return -1;
        }

        public int IndexOfEndState(string matchState)
        {
            for (int i = 0; i < count; i++)
            {
                if (transitions[i].endState == matchState)
                {
                    return i;
                }
            }

            Console.WriteLine("IndexOfEndState(): end state {0} not found in graph", matchState);
            return -1;
        }

        public void IncrementTraversals( uint index )
        {
            if (index < count)
            {
                transitions[index].traversals++;
            }
            else
            {
                Console.WriteLine("IncrementTraversals(): index {0} greater than number of transitions {1} in the graph.", index, count);
            }
        }

        public List<uint> GetOutlinkIndices(string matchStartState)
        {
            List<uint> indices = new List<uint>();

            for (uint i = 0; i < count; i++)
            {
                if (transitions[i].startState == matchStartState)
                {
                    indices.Add(i);
                }
            }

            return indices;
        }

        public int GetTransitionIndexByStartAndEndStates( string matchStartState, string matchEndState )
        {
            for ( int i = 0; i < count; i++ )
            {
                if (transitions[i].startState == matchStartState && transitions[i].endState == matchEndState)
                {
                    return i;
                }
            }

            return -1;
        }
    }

    public struct Node
    {
        public string state;
        public bool visited;
        public int visits;
        public string parent;

        public Node( string initialState )
        {
            this.state = initialState;
            this.visited = false;
            this.visits = 0;
            this.parent = "";
        }
    }

    public class Nodes
    {
        Node[] nodes;
        uint count = 0;

        public Nodes(uint maximumNodes)
        {
            nodes = new Node[maximumNodes];
        }

        public uint Count()
        {
            return count;
        }

        public bool Add(string state)
        {
            if (count < nodes.Length)
            {
                nodes[count].state = state;
                nodes[count].visits = 0;
                nodes[count].visited = false;
                count++;
                return true;
            }

            return false; // The node was not added.
        }

        public string StateByIndex(uint index)
        {
            if (index < count)
            {
                return nodes[index].state;
            }

            return String.Empty;
        }

        public void ClearAllVisits()
        {
            for (uint i = 0; i < count; i++)
            {
                nodes[i].visited = false;
            }
        }

        public bool Contains( string matchState )
        {
            for (uint i = 0; i < count; i++)
            {
                if (nodes[i].state == matchState)
                {
                    return true;
                }
            }
            return false;
        }

        public void Visit(uint index)
        {
            if (index < count)
            {
                nodes[index].visited = true;
            }
        }

        public bool WasVisited(uint index)
        {
            if (index < count)
            {
                return nodes[index].visited;
            }

            Console.WriteLine("Nodes::WasVisited() index {0} exceeded collection size {1}", index, count);
            return true; // The non-existent node is unreachable, but send back true to prevent endless loops on traversal algorithms.
        }

        public Node GetNodeByIndex(uint index)
        {
            if (index < count)
            {
                return nodes[index];
            }

            Console.WriteLine("Nodes::GetNodeByIndex() index {0} exceeded collection size {1}", index, count);
            return new Node();
        }

        public int GetIndexByState(string matchState)
        {
            for (int i = 0; i < count; i++)
            {
                if (nodes[i].state == matchState)
                {
                    return i;
                }
            }
            return -1;
        }

        public void SetParentByIndex(uint index, string parentState)
        {
            if (index < count)
            {
                nodes[index].parent = parentState;
            }
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
        StateTransitions transitions;
        Nodes totalNodes;
        List<string> unexploredStates;
        IUserRules rules; // We are able to refer to the user rules by interface, because we are only calling interface methods

        bool skipSelfLinks = false;

        public string transitionSeparator = " | ";

        public void SkipSelfLinks(bool Skip)
        {
            skipSelfLinks = Skip;
        }

        public void DisplayStateTable()
        {
            Console.WriteLine("Start state{0}End state{0}Action\n", transitionSeparator);

            for (uint i = 0; i < transitions.Count(); i++)
            {
                string start = transitions.StartStateByIndex(i);
                string end = transitions.EndStateByIndex(i);

                if (skipSelfLinks)
                {
                    if (start == end)
                    {
                        continue;
                    }
                }
                Console.WriteLine(start + transitionSeparator + end + transitionSeparator + transitions.ActionByIndex(i));
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

        public GeneratedGraph(IUserRules theRules, uint maxTransitions, uint maxNodes)
        {
            rules = theRules; // we follow the Rules!

            transitions = new StateTransitions(maxTransitions);

            totalNodes = new Nodes(maxNodes);

            unexploredStates = new List<string>();

            string state = rules.GetInitialState();
            unexploredStates.Add(state); // Adding to the <List> instance
            totalNodes.Add(state); // Adding to the Nodes class instance

            while (unexploredStates.Count > 0)
            {
                // generate all transitions out of state s
                state = FetchUnexploredState();
                AddNewTransitionsToGraph(state);
            }
        }

        string FetchUnexploredState()
        {
            string state = unexploredStates[0];
            unexploredStates.RemoveAt(0);
            return state;
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
                    totalNodes.Add(endState); // Adds a Node to Nodes class instance
                    unexploredStates.Add(endState); // Adds a string to List instance
                }

                // add this {startState, action, endState} transition to the Graph
                transitions.Add(startState: startState, endState: endState, action: action);
            }
        }

        int GetNodeIndexByState(string matchState)
        {

            for (uint i=0; i < totalNodes.Count(); i++)
            {
                if (totalNodes.StateByIndex(i) == matchState)
                {
                    return (int)i;
                }
            }
            Console.WriteLine("PROBLEM: GetNodeByState did not find a node with state {0}", matchState);
            return -1;
        }

        Queue<int> FindShortestPath(string startState, string endState)
        {
            Queue<Node> queue = new Queue<Node>();
            Stack<Node> route = new Stack<Node>();
            Queue<int> path = new Queue<int>();

            totalNodes.ClearAllVisits();

            int targetIndex = totalNodes.GetIndexByState(endState);

            queue.Enqueue(totalNodes.GetNodeByIndex((uint)totalNodes.GetIndexByState(startState)));

            while (queue.Count > 0)
            {
                Node currentNode = queue.Dequeue();

                foreach (uint tIndex in transitions.GetOutlinkIndices(currentNode.state))
                {
                    string parentState = transitions.StartStateByIndex(tIndex);

                    int startIndex = GetNodeIndexByState(parentState);

                    if (startIndex < 0) { continue; }

                    totalNodes.Visit((uint)startIndex);

                    int endIndex = GetNodeIndexByState(transitions.EndStateByIndex(tIndex));

                    if (endIndex < 0) { continue; }

                    if (!totalNodes.WasVisited((uint)endIndex))
                    {
                        totalNodes.Visit((uint)endIndex);

                        totalNodes.SetParentByIndex((uint)endIndex, parentState);

                        queue.Enqueue(totalNodes.GetNodeByIndex((uint)endIndex));

                        if (endIndex == targetIndex)
                        {
                            Node tmp = totalNodes.GetNodeByIndex((uint)endIndex);

                            while (true)
                            {
                                route.Push(tmp);
                                tmp = totalNodes.GetNodeByIndex((uint)totalNodes.GetIndexByState(tmp.parent));

                                if (tmp.state == startState)
                                {
                                    route.Push(tmp);
                                    while (route.Count > 1)
                                    {
                                        Node StartNode = route.Pop();
                                        Node EndNode = route.Pop();
                                        route.Push(EndNode);
                                        int pathTIndex = transitions.GetTransitionIndexByStartAndEndStates(StartNode.state, EndNode.state);
                                        path.Enqueue(pathTIndex);
                                    }
                                    break;
                                }
                            }
                            break;
                        }
                    }
                }
            }
            return path;
        }

        public void GreedyPostman(string fname)
        {

            // Each transition will now be a target to be reached
            //  1. find a transition with a low hit count
            //  2. move along a path from where you are to the start node of that transition
            //  3. move along the target transition (so now you shd be in that transition's end node)

            //            StateModel s = new StateModel();    // start with the initial state of the model
            string state = rules.GetInitialState();
//            Queue<Transition> path = new Queue<Transition>();

            int fileCtr = 0;
            int loopctr = 0;
            string suffix = "";

            while (loopctr < 25)
            {
                loopctr++;

                // Find a transition with a low hit count
                int targetIndex = transitions.GetALowHitTransitionIndex();

                string targetStartState = transitions.StartStateByIndex((uint)targetIndex);

                if (state != targetStartState)
                {
                    Queue<int> path = FindShortestPath(state, targetStartState);
                    foreach (int tIndex in path)
                    {
                        // mark the transitions covered along the way
                        transitions.IncrementTraversals((uint)tIndex);
                        fileCtr++;
                        suffix = String.Format("{0}", fileCtr.ToString("D4"));
                        this.CreateGraphVizFileAndImage(fname, suffix, transitions.ActionByIndex((uint)tIndex));
                    }
                }

                // mark that we covered the target Transition as well
                transitions.IncrementTraversals((uint)targetIndex);
                state = transitions.EndStateByIndex((uint)targetIndex);  // move to the end node of the target transition

                fileCtr++;
                suffix = String.Format("{0}", fileCtr.ToString("D4"));
                this.CreateGraphVizFileAndImage(fname, suffix, transitions.ActionByIndex((uint)targetIndex));
            }
        }

        public void CreateGraphVizFileAndImage(string fname, string suffix, string action)
        {
            // Create a new file.
            using (FileStream fs = new FileStream(fname + suffix + ".txt", FileMode.Create))
            using (StreamWriter w = new StreamWriter(fs, Encoding.ASCII))
            {
                // preamble for the graphviz "dot format" output
                w.WriteLine("digraph state_machine {");
                w.WriteLine("node [shape = ellipse];");
                w.WriteLine("rankdir=LR;");

                // add the state nodes to the image
                for (uint i = 0; i < totalNodes.Count(); i++)
                {
                    // TODO: Get the string formatting correct for GraphViz.
                    w.WriteLine("\"{0}\"\t[label=\"{1}\"]", i, totalNodes.GetNodeByIndex(i).state.Replace(",", "\\n"));
                }

                // Insert the info node into the image
                w.WriteLine();
                w.WriteLine("node [shape = rectangle];");
                w.Write("\"Info node\"\t[label=\"");
                w.Write("++++++++++++++\\n");
                w.Write("Step: {0}\\n", suffix);
                w.Write("{0}\\n", action);
                w.Write("Floor:  {0}\", ", transitions.GetTraversalsFloor());
                w.WriteLine("fillcolor=lightgrey, color=black]");
                w.WriteLine();

                // Color each link by its hit count
                for (uint i = 0; i < transitions.Count(); i++)
                {
                    int traversals = transitions.TraversalsByIndex(i);
                    w.WriteLine("\"{0}\" -> \"{1}\" [ label=\"{2} ({3})\"{4} ];",
                        transitions.StartStateByIndex(i), transitions.EndStateByIndex(i), transitions.ActionByIndex(i), traversals, GetLinkAppearance(traversals));
                }

                w.WriteLine("}");
                w.Close();
            }

            // Invoke Graphviz to create the image file
            CreateGraphvizImage(fname + suffix);
        }

        static string GetLinkAppearance(int counter)
        {
            string linkColor = "";

            if (counter > 0)
            {
                if (counter >= 20 && counter < 25)
                    linkColor = "coral";
                else if (counter >= 25 && counter < 30)
                    linkColor = "coral1";
                else if (counter >= 30 && counter < 35)
                    linkColor = "coral2";
                else if (counter >= 35 && counter < 40)
                    linkColor = "coral3";
                else if (counter >= 20 && counter < 45)
                    linkColor = "coral4";
                else if (counter >= 45)
                    linkColor = "gold";
                else
                {
                    switch (counter)
                    {
                        case 1: linkColor = "red"; break;
                        case 2: linkColor = "green"; break;
                        case 3: linkColor = "magenta"; break;
                        case 4: linkColor = "blue"; break;
                        case 5: linkColor = "coral"; break;
                        case 6: linkColor = "darkgreen"; break;
                        case 7: linkColor = "violet"; break;
                        case 8: linkColor = "crimson"; break;
                        case 9: linkColor = "darkorange"; break;
                        case 10: linkColor = "darkorchid"; break;
                        case 11: linkColor = "deeppink"; break;
                        case 12: linkColor = "deepskyblue"; break;
                        case 13: linkColor = "forestgreen"; break;
                        case 14: linkColor = "firebrick"; break;
                        case 15: linkColor = "darkslateblue"; break;
                        case 16: linkColor = "darkgoldenrod"; break;
                        case 17: linkColor = "cornflowerblue"; break;
                        case 18: linkColor = "goldenrod"; break;
                        case 19: linkColor = "chartreuse"; break;
                        default:
                            Console.WriteLine("shouldn't have reached here!");
                            Console.ReadLine();
                            break;
                    }
                }
                // color visited transitions sth not black
                return String.Format(",color=\"{0}\", penwidth=3", linkColor);
            }
            else
                return ",color=black";    // color unvisited transitions black

        }

        static void CreateGraphvizImage(string fname)
        {
            // Only for Windows 
//            Process.Start("C:\\Program Files\\Graphviz\\bin\\dot.exe",
  //              fname + ".txt -Tjpg -o " + fname + ".jpg");
        }
    }
}
