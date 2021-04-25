// EZmodel Sample State Generation to Spreadsheet
// copyright 2019 Serious Quality LLC

// Adjusted March 15-17, 2021 with developer thoughts:
// - removed Reflection from original code
// - decoupled model rules from ezModel state table construction
// - ezModel reads model rules through C# interface IUserRules
// - person providing model rules is responsible for implementing IUserRules methods
//
//   Compliments of Doug Szabo, for Harry Robinson.

// TODO: scan this code and look for a condition where
// transition uniqueness is characterized by start and end state; that is not the right way to do it.
// Ensure transition uniqueness is characterized by start state and action.

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
        public uint actionIndex;
        public int hitCount;
        public double probability;
        public bool enabled;

        public StateTransition(string startState, string endState, uint actionIndex)
        {
            this.startState = startState;
            this.endState = endState;
            this.actionIndex = actionIndex;
            this.hitCount = 0;
            this.probability = 1.0;
            this.enabled = true;
        }
    }

    public struct TransitionAction
    {
        public string action;
        public int faultCount; // Keep track of errors involving this action

        public TransitionAction(string actionArg)
        {
            this.action = actionArg;
            this.faultCount = 0;
        }
    }

    public class StateTransitions
    {
        StateTransition[] transitions;
        TransitionAction[] actions;

        // Keep track of the number of populated elements in the transitions
        // array.  This is necessary because C# arrays are fixed size, so
        // the array Length tells the capacity of the array, not the number
        // of populated elements in the array.
        uint transitionCount = 0;
        uint actionCount = 0;

        public readonly string transitionSeparator = " | ";

        Random rnd = new Random(DateTime.Now.Millisecond);

        public StateTransitions(uint maximumTransitions, uint maximumActions)
        {
            transitions = new StateTransition[maximumTransitions];
            actions = new TransitionAction[maximumActions];
        }

        public string ActionByTransitionIndex(uint tIndex)
        {
            if (tIndex < transitionCount)
            {
                return actions[transitions[tIndex].actionIndex].action;
            }

            return String.Empty;
        }

        public bool Add(string startState, string endState, string action)
        {
            if (transitionCount < transitions.Length)
            {
                transitions[transitionCount].startState = startState;
                transitions[transitionCount].endState = endState;
                TransitionAction ta = new TransitionAction(action);
                bool actionFound = false;
                for (uint i = 0; i < actionCount; i++)
                {
                    if (actions[i].action == action)
                    {
                        transitions[transitionCount].actionIndex = i;
                        actionFound = true;
                        break;
                    }
                }
                if (!actionFound)
                {
                    transitions[transitionCount].actionIndex = actionCount;
                    actions[actionCount].action = action;
                    actionCount++;
                }
                transitions[transitionCount].hitCount = 0;
                transitions[transitionCount].probability = 1.0;
                transitionCount++;
                return true;
            }

            return false; // The transition was not added.
        }

        public uint Count()
        {
            return transitionCount;
        }

        public void DisableTransition(uint tIndex)
        {
            if (tIndex < transitionCount)
            {
                transitions[tIndex].enabled = false;
            }
        }

        public void DisableTransitionsByAction(string matchAction)
        {
            for (uint i = 0; i < actionCount; i++)
            {
                if (actions[i].action == matchAction)
                {
                    for (uint j = 0; j < transitionCount; j++)
                    {
                        if (transitions[j].actionIndex == i)
                        {
                            transitions[j].enabled = false;
                        }
                    }
                    return;
                }
            }
        }

        public string EndStateByTransitionIndex(uint tIndex)
        {
            if (tIndex < transitionCount)
            {
                return transitions[tIndex].endState;
            }

            return String.Empty;
        }

        public int GetAnyLowHitTransitionIndex()
        {
            // Select a low hitcount transition randomly, without preference
            // for the proximity of the target transition to the current state.
            // This approach yields unpredictable traversal paths through
            // the graph, and is a way to cover the graph chaotically.

            // Find the lowest hit count
            int lowHit = GetHitcountFloor();

            // Create a list of all low-hit transitions
            List<int> lowHitList = new List<int>();
            for (int i = 0; i < transitionCount; i++)
            {
                if (transitions[i].hitCount == lowHit)
                {
                    lowHitList.Add(i);
                }
            }

            // Target one of the low-hit transitions 
            int low = lowHitList[rnd.Next(lowHitList.Count)];
            return low;
        }

        public int GetHitcountFloor()
        {
            int floor = int.MaxValue;

            for (int i = 0; i < transitionCount; i++)
            {
                if (transitions[i].hitCount < floor)
                {
                    floor = transitions[i].hitCount;
                }
            }
            return floor;
        }

        public int GetLowHitTransitionIndexAvoidOutlinks(string state)
        {
            // Avoid an outlink transition to drive coverage away from
            // the current node.  If only outlinks have low hitcount,
            // then an outlink will be chosen.

            // Find the lowest hit count
            int lowHit = GetHitcountFloor();

            // Create a list of all low-hit transitions
            List<int> lowHitList = new List<int>();
            // Create a list of all low-hit outlink transitions
            List<int> lowHitNonOutlinkList = new List<int>();

            for (int i = 0; i < transitionCount; i++)
            {
                if (transitions[i].hitCount == lowHit)
                {
                    if (transitions[i].startState != state)
                    {
                        lowHitNonOutlinkList.Add(i);
                    }
                    lowHitList.Add(i);
                }
            }

            int low;

            if (lowHitNonOutlinkList.Count > 0)
            {
                low = lowHitNonOutlinkList[rnd.Next(lowHitNonOutlinkList.Count)];
            }
            else
            {
                // By definition, there will be at least one item
                // in the lowHitList list, so we can just ask for
                // a random choice from that list.
                low = lowHitList[rnd.Next(lowHitList.Count)];
            }

            return low;
        }

        public int GetLowHitTransitionIndexPreferOutlink(string state)
        {
            // Prefer an outlink transition with a low hit count, so that
            // the traversal doesn't make big jumps around the graph when
            // there are local opportunities.  If there are no outlink
            // transitions with low hitcount, choose any other transition
            // with low hitcount.  Thus, big jumps around the graph will
            // happen occasionally.  Harry Robinson calls this approach
            // Albatross coverage.

            // Find the lowest hit count
            int lowHit = GetHitcountFloor();

            // Create a list of all low-hit transitions
            List<int> lowHitList = new List<int>();
            // Create a list of all low-hit outlink transitions
            List<int> lowHitOutlinkList = new List<int>();

            for (int i = 0; i < transitionCount; i++)
            {
                if (transitions[i].hitCount == lowHit)
                {
                    if (transitions[i].startState == state)
                    {
                        lowHitOutlinkList.Add(i);
                    }
                    lowHitList.Add(i);
                }
            }

            int low;

            if (lowHitOutlinkList.Count > 0)
            {
                low = lowHitOutlinkList[rnd.Next(lowHitOutlinkList.Count)];
            }
            else
            {
                // By definition, there will be at least one item
                // in the lowHitList list, so we can just ask for
                // a random choice from that list.
                low = lowHitList[rnd.Next(lowHitList.Count)];
            }

            return low;
        }

        public List<uint> GetOutlinkTransitionIndices(string state)
        {
            List<uint> indices = new List<uint>();

            for (uint i = 0; i < transitionCount; i++)
            {
                if (transitions[i].startState == state)
                {
                    indices.Add(i);
                }
            }

            return indices;
        }

        public int GetTransitionIndexByStartAndEndStates(string startState, string endState)
        {
            int lowHit = int.MaxValue;
            int lowHitIndex = -1;

            for (int i = 0; i < transitionCount; i++)
            {
                // There can be multiple arcs between start and end state.
                // Track the index of the transition with the lowest hitCount.
                // Return the tracked index to the caller, so that coverage is
                // increased.
                if (transitions[i].startState == startState && transitions[i].endState == endState)
                {
                    if (transitions[i].hitCount <= lowHit)
                    {
                        lowHit = transitions[i].hitCount;
                        lowHitIndex = i;
                    }
                }
            }
            return lowHitIndex;
        }

        public int HitcountByTransitionIndex(uint tIndex)
        {
            if (tIndex < transitionCount)
            {
                return transitions[tIndex].hitCount;
            }

            return -1;
        }

        public int IncrementActionFailures(uint tIndex)
        {
            if (tIndex < transitionCount)
            {
                actions[transitions[tIndex].actionIndex].faultCount++;
                return actions[transitions[tIndex].actionIndex].faultCount;
            }
            return -1;
        }

        public void IncrementHitCount(uint tIndex)
        {
            if (tIndex < transitionCount)
            {
                transitions[tIndex].hitCount++;
            }
            else
            {
                Console.WriteLine("IncrementHitCount(): index {0} greater than number of transitions {1} in the graph.", tIndex, transitionCount);
            }
        }

        public string StartStateByTransitionIndex( uint tIndex )
        {
            if (tIndex < transitionCount)
            {
                return transitions[tIndex].startState;
            }

            return String.Empty;
        }

        public int TransitionIndexOfEndState(string endState)
        {
            for (int i = 0; i < transitionCount; i++)
            {
                if (transitions[i].endState == endState)
                {
                    return i;
                }
            }

            Console.WriteLine("IndexOfEndState(): end state {0} not found in graph", endState);
            return -1;
        }

        public int TransitionIndexOfStartState(string startState)
        {
            for (int i = 0; i < transitionCount; i++)
            {
                if (transitions[i].startState == startState)
                {
                    return i;
                }
            }

            Console.WriteLine("IndexOfStartState(): start state {0} not found in graph", startState);
            return -1;
        }

        public string TransitionStringFromTransitionIndex(uint tIndex)
        {
            if (tIndex < transitionCount)
            {
                return String.Format("{0}{3}{1}{3}{2}", transitions[tIndex].startState, transitions[tIndex].endState, ActionByTransitionIndex(tIndex), transitionSeparator);
            }
            return String.Empty;
        }
    } // StateTransitions

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

        public bool Add(string state)
        {
            if (count < nodes.Length)
            {
                nodes[count].state = state;
                nodes[count].visits = 0;
                nodes[count].visited = false;
                nodes[count].parent = "";
                count++;
                return true;
            }

            return false; // The node was not added.
        }

        public void ClearAllVisits()
        {
            for (uint i = 0; i < count; i++)
            {
                nodes[i].visited = false;
            }
        }

        public bool Contains(string state)
        {
            for (uint i = 0; i < count; i++)
            {
                if (nodes[i].state == state)
                {
                    return true;
                }
            }
            return false;
        }

        public uint Count()
        {
            return count;
        }

        public int GetIndexByState(string state)
        {
            for (int i = 0; i < count; i++)
            {
                if (nodes[i].state == state)
                {
                    return i;
                }
            }
            return -1;
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

        public string GetStateByIndex(uint index)
        {
            if (index < count)
            {
                return nodes[index].state;
            }

            return String.Empty;
        }

        public void SetParentByIndex(uint index, string parentState)
        {
            if (index < count)
            {
                nodes[index].parent = parentState;
            }
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
    } // Nodes


    public interface IEzModelClient
    {
        // The test automation programmer implements a public class that communicates with
        // EzModel through the IEzModelClient interface.  The test automation is
        // responsible for
        //  (1) setting the states and actions that make up the rules of the model,
        //  and optionally
        //  (2) driving the system under test,
        //  (3) measuring the state of the system under test,
        //  (4) reporting outcomes including problems, and
        //  (5) reporting the action path from initial state to a problem or end of run.

        const string valueSeparator = ", ";

        // Test Execution:
        // When true, EzModel will call the rules.AdapterTransition() method for
        // each traversal step.  rules.AdapterTransition() is responsible for
        // driving the action of the traversal step in the system under test, and
        // must return the end state of the system under test.
        // EzModel should not change the NotifyAdapter setting.
        bool NotifyAdapter { get; set; }

        // Modeling:
        // When true, EzModel will not add transitions to the state table that
        // are self-links.  Set this value before calling the GeneratedGraph constructor,
        // as the creation of transitions occurs in the constructor body.
        // EzModel should not change the SkipSelfLinks setting.
        bool SkipSelfLinks { get; set; }

        // Test Execution:
        // When true, EzModel will end the current traversal strategy on the first
        // problem indicated by the rules module.  For example, if StopOnProblem is
        // true, EzModel identifies a false from rules.AreStatesAcceptablySimilar()
        // as a problem, and stops the current traversal strategy, returning
        // control to the rules module.
        // EzModel shoud not change the StopOnProblem setting.
        bool StopOnProblem { get; set; }

        // Modeling:
        // EzModel always calls these methods.
        string GetInitialState();
        List<string> GetAvailableActions(string startState);
        string GetEndState(string startState, string action);
        void ReportTraversal(string initialState, List<string> popcornTrail);

        // Test Execution:
        // EzModel calls these methods only when NotifyAdapter is true.
        string AdapterTransition(string startState, string action);
        bool AreStatesAcceptablySimilar(string observed, string predicted);
        void ReportProblem(string initialState, string observed, string predicted, List<string> popcornTrail);
        void SetStateOfSystemUnderTest(string state);
    }

    public class GeneratedGraph
    {
        StateTransitions transitions;
        Nodes totalNodes;
        List<string> unexploredStates;
        Queue<int> path = new Queue<int>();

        IEzModelClient client;

        public GeneratedGraph(IEzModelClient theEzModelClient, uint maxTransitions, uint maxNodes, uint maxActions)
        {
            client = theEzModelClient;

            transitions = new StateTransitions(maxTransitions, maxActions);

            totalNodes = new Nodes(maxNodes);

            unexploredStates = new List<string>();

            string state = client.GetInitialState();
            unexploredStates.Add(state); // Adding to the <List> instance
            totalNodes.Add(state); // Adding to the Nodes class instance

            while (unexploredStates.Count > 0)
            {
                // generate all transitions out of state s
                state = FetchUnexploredState();
                AddNewTransitionsToGraph(state);
            }
        }

        void AddNewTransitionsToGraph(string startState)
        {
            List<string> Actions = client.GetAvailableActions(startState);

            foreach (string action in Actions)
            {
                // an endstate is generated from current state + changes from an invoked action
                string endState = client.GetEndState(startState, action);

                // if generated endstate is new, add  to the totalNode & unexploredNode lists
                if (!totalNodes.Contains(endState))
                {
                    totalNodes.Add(endState); // Adds a Node to Nodes class instance
                    unexploredStates.Add(endState); // Adds a string to List instance
                }

                // add this {startState, endState, action} transition to the Graph
                // except in the case where client.SkipSelfLinks is true AND startState == endState
                if (client.SkipSelfLinks == true && (startState == endState))
                {
                    continue;
                }
                transitions.Add(startState: startState, endState: endState, action: action);
            }
        }

        public void CreateGraphVizFileAndImage(string fname, string suffix, string title, int transitionIndex = -1, int endOfPathTransitionIndex = -1)
        {
            // NOTE: transitionIndex and endOfPathTransitionIndex are optional.  Not every graph rendition
            // is being traversed, and not every graph rendition has a path with an end node..
            // If transitionIndex is non-negative, light up the start node and fatten the outlink.
            // If endOfPathTransitionIndex is non-negative, light it up in the end of path color.

            // Create a new file.
            using (FileStream fs = new FileStream(fname + suffix + ".txt", FileMode.Create))
            using (StreamWriter w = new StreamWriter(fs, Encoding.ASCII))
            {
                // preamble for the graphviz "dot format" output
                w.WriteLine("digraph state_machine {");
                w.WriteLine("size = \"17.8,10\";");
                w.WriteLine("node [shape = ellipse];");
                w.WriteLine("rankdir=LR;");

                int endOfPathNodeIndex = -1;

                if (endOfPathTransitionIndex > -1)
                {
                    endOfPathNodeIndex = totalNodes.GetIndexByState(transitions.EndStateByTransitionIndex((uint)endOfPathTransitionIndex));
                }

                int startStateIndex = -1;

                if (transitionIndex > -1)
                {
                    startStateIndex = totalNodes.GetIndexByState(transitions.StartStateByTransitionIndex((uint)transitionIndex));
                }

                List<int> pathNodeIndices = new List<int>();

                // Get the set of nodes involved in the path, if any.
                foreach (uint tIndex in path)
                {
                    int nodeIndex = totalNodes.GetIndexByState(transitions.StartStateByTransitionIndex(tIndex));
                    if (!pathNodeIndices.Contains(nodeIndex))
                    {
                        pathNodeIndices.Add(nodeIndex);
                    }
                    nodeIndex = totalNodes.GetIndexByState(transitions.EndStateByTransitionIndex(tIndex));
                    if (!pathNodeIndices.Contains(nodeIndex))
                    {
                        pathNodeIndices.Add(nodeIndex);
                    }
                }

                // add the state nodes to the image
                for (int i = 0; i < totalNodes.Count(); i++)
                {
                    string decoration = "";

                    if (startStateIndex == i || endOfPathNodeIndex == i || pathNodeIndices.Contains(i))
                    {
                        decoration = ", style=filled, fillcolor=\"";
                    }

                    if (startStateIndex != i && endOfPathNodeIndex != i && pathNodeIndices.Contains(i))
                    {
                        decoration += "lightgrey\"";
                    }

                    if (startStateIndex == i && endOfPathNodeIndex != i)
                    {
                        decoration += "green\"";
                    }

                    if (startStateIndex == i && endOfPathNodeIndex == i)
                    {
                        decoration += "cyan:green\"";
                    }

                    if (startStateIndex != i && endOfPathNodeIndex == i)
                    {
                        decoration += "cyan\"";
                    }

                    w.WriteLine("\"{0}\"\t[label=\"{1}\"{2}]", totalNodes.GetNodeByIndex((uint)i).state, totalNodes.GetNodeByIndex((uint)i).state.Replace(",", "\\n"), decoration);
                }

                // Insert the info node into the image
                w.WriteLine();
                w.WriteLine("node [shape = rectangle];");
                w.Write("\"Info node\"\t[label=\"");
                w.Write("++++++++++++++\\n");
                w.Write("Step: {0}\\n", suffix);
                w.Write("{0}\\n", title);
                w.Write("Floor:  {0}\", ", transitions.GetHitcountFloor());
                w.WriteLine("fillcolor=lightgrey, style=filled, color=black]");
                w.WriteLine();

                // Capture the target transition of the path so that we properly decorate it.
                Queue<int> arcPath = new Queue<int>(path.ToArray());
                arcPath.Enqueue(endOfPathTransitionIndex);

                // Color each link by its hit count
                for (uint i = 0; i < transitions.Count(); i++)
                {
                    // Set all path arcs to lightgrey except the currently actioned arc.
                    string linkAppearance = ", color=lightgrey, penwidth=15";
                    int hitCount = transitions.HitcountByTransitionIndex(i);

                    if (i == transitionIndex || !arcPath.Contains((int)i))
                    {
                        linkAppearance = GetLinkAppearance(hitCount);
                    }
                    if (i == transitionIndex)
                    {
                        linkAppearance = linkAppearance.Replace("penwidth=3", "penwidth=15");
                    }

                    w.WriteLine("\"{0}\" -> \"{1}\" [ label=\"{2} ({3})\"{4} ];",
                        transitions.StartStateByTransitionIndex(i), transitions.EndStateByTransitionIndex(i), transitions.ActionByTransitionIndex(i), hitCount, linkAppearance);
                }

                w.WriteLine("}");
                w.Close();
            }

            // Invoke Graphviz to create the image file
            CreateGraphVizImage(fname + suffix);
        }

        static void CreateGraphVizImage(string fname)
        {
            // Only for Windows 
            //            Process.Start("C:\\Program Files\\Graphviz\\bin\\dot.exe",
            //              fname + ".txt -Tjpg -o " + fname + ".jpg");
            Process.Start("dot", fname + ".txt -Tsvg -o svg/" + fname + ".svg");
        }

        public void DisplayStateTable()
        {
            Console.WriteLine("Start state{0}End state{0}Action\n", transitions.transitionSeparator);

            for (uint i = 0; i < transitions.Count(); i++)
            {
                string start = transitions.StartStateByTransitionIndex(i);
                string end = transitions.EndStateByTransitionIndex(i);

                if (client.SkipSelfLinks)
                {
                    if (start == end)
                    {
                        continue;
                    }
                }
                Console.WriteLine(transitions.TransitionStringFromTransitionIndex(i));
            }
            Console.WriteLine("  ");
        }

        string FetchUnexploredState()
        {
            string state = unexploredStates[0];
            unexploredStates.RemoveAt(0);
            return state;
        }

        Queue<int> FindPathLessTraveled(string startState, string endState)
        {
            Queue<Node> queue = new Queue<Node>();
            Stack<Node> route = new Stack<Node>();
            Queue<int> path = new Queue<int>();

            return path;
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

                foreach (uint tIndex in transitions.GetOutlinkTransitionIndices(currentNode.state))
                {
                    string parentState = transitions.StartStateByTransitionIndex(tIndex);
                    int startIndex = GetNodeIndexByState(parentState);

                    if (startIndex < 0) { continue; }

                    totalNodes.Visit((uint)startIndex);

                    int endIndex = GetNodeIndexByState(transitions.EndStateByTransitionIndex(tIndex));

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

        int GetNodeIndexByState(string matchState)
        {

            for (uint i = 0; i < totalNodes.Count(); i++)
            {
                if (totalNodes.GetStateByIndex(i) == matchState)
                {
                    return (int)i;
                }
            }
            Console.WriteLine("PROBLEM: GetNodeByState did not find a node with state {0}", matchState);
            return -1;
        }

        public void RandomDestinationCoverage(string fname)
        {
        ResetPosition:

            // Each transition will now be a target to be reached
            //  1. find a transition with a low hit count
            //  2. move along a path from where you are to the start node of that transition
            //  3. move along the target transition (so now you shd be in that transition's end node)

            // InitialState is needed in case rules.ReportProblem() is called.
            string initialState = client.GetInitialState();

            if (client.NotifyAdapter)
            {
                client.SetStateOfSystemUnderTest(initialState);
            }

            // State is the start state of the current transition.
            string state = initialState;

            // Record the actions taken in case rules.ReportProblem() is called.
            // This list is built up only when the rules module has NotifyAdapter == true.
            List<string> popcornTrail = new List<string>();

            int fileCtr = 0;
            int loopctr = 0;
            string suffix;
            int minimumCoverageFloor = 2;

            while (transitions.GetHitcountFloor() < minimumCoverageFloor)
            {
                loopctr++;

                path = new Queue<int>();

                // Prefer an outlink transition with a low hit count
                int targetIndex = transitions.GetLowHitTransitionIndexPreferOutlink(state);

                string targetStartState = transitions.StartStateByTransitionIndex((uint)targetIndex);

                if (state != targetStartState)
                {
                    path = FindShortestPath(state, targetStartState);
                    foreach (int tIndex in path)
                    {
                        // mark the transitions covered along the way
                        transitions.IncrementHitCount((uint)tIndex);
                        fileCtr++;
                        suffix = String.Format("{0}", fileCtr.ToString("D4"));
                        if (client.NotifyAdapter)
                        {
                            string action = transitions.ActionByTransitionIndex((uint)tIndex);
                            popcornTrail.Add(action);
                            string reportedEndState = client.AdapterTransition(state, action);
                            string predicted = transitions.EndStateByTransitionIndex((uint)tIndex);
                            if (!client.AreStatesAcceptablySimilar(reportedEndState, predicted))
                            {
                                // Inconsistency detected.
                                // Let the adapter report the problem, including the popcorn trail.
                                client.ReportProblem(initialState, reportedEndState, predicted, popcornTrail);
                                // If the adapter wants to stop on problem, stop.
                                if (client.StopOnProblem)
                                {
                                    return;
                                }

                                // On first fault on an action, Disable the transition.
                                if (transitions.IncrementActionFailures((uint)tIndex) == 1)
                                {
                                    // NOTE: the cause of the problem detected may be in the route to this transition,
                                    // rather than in this transition.
                                    // Building a capability for EzModel to pick an alternate route to this transition
                                    // is useful, and coincident with the Beeline strategy.
                                    // Beeline may isolate the problem transition, for instance: if the first problem
                                    // was detected on transition Z in the route ...,Y,Z, and then Beeline succeeds in
                                    // route ...,X,Z, we may find that another route of ...,Y,Z also has a problem.  Y
                                    // is then the suspect transition.
                                    transitions.DisableTransition((uint)tIndex);
                                }
                                else
                                {
                    // On second or later fault on the same action, disable the action everywhere.
                transitions.DisableTransitionsByAction(transitions.ActionByTransitionIndex((uint)tIndex));
            // NOTE: there may be a systemic problem with the action itself.  Two incidents involving the
            // same action is reason enough to avoid the action for the remainder of the run.  Development
            // team can root-cause the issue.
                                }
                                // Go back to the start of this function, and reset the adapter.
                                goto ResetPosition;
                            }
                        }
                        this.CreateGraphVizFileAndImage(fname, suffix, transitions.ActionByTransitionIndex((uint)tIndex), tIndex, targetIndex);
                    }
                }

                // mark that we covered the target Transition as well
                transitions.IncrementHitCount((uint)targetIndex);

                state = transitions.EndStateByTransitionIndex((uint)targetIndex);  // move to the end node of the target transition
                if (client.NotifyAdapter)
                {
                    string action = transitions.ActionByTransitionIndex((uint)targetIndex);
                    popcornTrail.Add(action);
                    string reportedEndState = client.AdapterTransition(transitions.StartStateByTransitionIndex((uint)targetIndex), action);
                    string predicted = transitions.EndStateByTransitionIndex((uint)targetIndex);
                    if (!client.AreStatesAcceptablySimilar(reportedEndState, predicted))
                    {
                        client.ReportProblem(initialState, reportedEndState, predicted, popcornTrail);
                        if (client.StopOnProblem)
                        {
                            return;
                        }

                        if (transitions.IncrementActionFailures((uint)targetIndex) == 1)
                        {
                            transitions.DisableTransition((uint)targetIndex);
                        }
                        else
                        {
                            transitions.DisableTransitionsByAction(transitions.ActionByTransitionIndex((uint)targetIndex));
                        }
                        // Inconsistency.  Restart traversal.
                        goto ResetPosition;
                    }
                }

                fileCtr++;
                suffix = String.Format("{0}", fileCtr.ToString("D4"));
                this.CreateGraphVizFileAndImage(fname, suffix, transitions.ActionByTransitionIndex((uint)targetIndex), targetIndex, targetIndex);
            }
            // TODO: Trace floor coverage
            Console.WriteLine("Reached coverage floor of {0} in {1} iterations.", minimumCoverageFloor, loopctr);

            if (client.NotifyAdapter)
            {
                client.ReportTraversal(initialState, popcornTrail);
            }
        }

        // A sanity check for the client's model
        public List<string> ReportDuplicateOutlinks()
        {
            // Call this method to learn whether any nodes have multiples of an action as an outlink.
            // It is nonsensical to duplicate an action as an outlink.
            // For each action in the returned list, the caller should eliminate redundancies.
            // The GetAvailableActions() implementation is a good place to start the search
            // for the origin of duplicate actions.

            // Report the entire transition of each duplicate outlink.

            List<string> duplicates = new List<string>();

            for (uint i = 0; i < totalNodes.Count(); i++)
            {
                string state = totalNodes.GetStateByIndex(i);
                List<string> actions = new List<string>();
                List<string> duplicateActions = new List<string>();
                List<uint> outs = transitions.GetOutlinkTransitionIndices(state);

                // Reporting all the duplicates requires up to two passes on each node.
                // The first pass detects duplicates.
                // The second pass happens only if duplicates were detected in the
                // first pass.
                // The second pass copies the transitions containing the duplicate
                // actions to the duplicates collection, which is returned to the
                // caller.
                for (uint j = 0; j < outs.Count; j++)
                {
                    string action = transitions.ActionByTransitionIndex(outs[(int)j]);

                    if (actions.Contains(action))
                    {
                        if (!duplicateActions.Contains(action))
                        {
                            duplicateActions.Add(action);
                        }
                    }
                    else
                    {
                        actions.Add(action);
                    }
                }

                for (uint j = 0; duplicateActions.Count > 0 && j < outs.Count; j++)
                {
                    string action = transitions.ActionByTransitionIndex(outs[(int)j]);

                    if (duplicateActions.Contains(action))
                    {
                        duplicates.Add(transitions.TransitionStringFromTransitionIndex(outs[(int)j]));
                    }
                }
            }

            return duplicates;
        } // ReportDuplicateOutlinks()

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
    } // GeneratedGraph
} // EzModelStateTable namespace
