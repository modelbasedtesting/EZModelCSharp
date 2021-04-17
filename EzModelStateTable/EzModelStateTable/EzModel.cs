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

        // Keep track of the number of populated elements in the transitions array
        // because C# arrays are fixed size and the .Length
        // property just tells the size of the array.
        uint transitionCount = 0;
        uint actionCount = 0;

        Random rnd = new Random(DateTime.Now.Millisecond);

        public StateTransitions(uint maximumTransitions, uint maximumActions)
        {
            transitions = new StateTransition[maximumTransitions];
            actions = new TransitionAction[maximumActions];
        }

        public int GetTraversalsFloor()
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

        public int GetALowHitTransitionIndex()
        {
            // Find the lowest hit count
            int lowHit = GetTraversalsFloor();

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
            int low = rnd.Next(lowHitList.Count);
            // TODO: Trace the hit list
//            Console.WriteLine("{0}, {1}", low, lowHitList.ToString());
            return (lowHitList[low]);
        }

        public bool Add( string startState, string endState, string action )
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

        public string StartStateByIndex( uint index )
        {
            if (index < transitionCount)
            {
                return transitions[index].startState;
            }

            return String.Empty;
        }

        public string EndStateByIndex(uint index)
        {
            if (index < transitionCount)
            {
                return transitions[index].endState;
            }

            return String.Empty;
        }

        public string ActionByIndex(uint index)
        {
            if (index < transitionCount)
            {
                return actions[transitions[index].actionIndex].action;
            }

            return String.Empty;
        }

        public int TraversalsByIndex(uint index)
        {
            if (index < transitionCount)
            {
                return transitions[index].hitCount;
            }

            return -1;
        }

        public int IndexOfStartState( string matchState )
        {
            for ( int i = 0; i < transitionCount; i++ )
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
            for (int i = 0; i < transitionCount; i++)
            {
                if (transitions[i].endState == matchState)
                {
                    return i;
                }
            }

            Console.WriteLine("IndexOfEndState(): end state {0} not found in graph", matchState);
            return -1;
        }

        public void IncrementHitCount( uint index )
        {
            if (index < transitionCount)
            {
                transitions[index].hitCount++;
            }
            else
            {
                Console.WriteLine("IncrementHitCount(): index {0} greater than number of transitions {1} in the graph.", index, transitionCount);
            }
        }

        public int IncrementActionFaults( uint index )
        {
            if (index < transitionCount)
            {
                actions[transitions[index].actionIndex].faultCount++;
                return actions[transitions[index].actionIndex].faultCount;
            }
            return -1;
        }

        public List<uint> GetOutlinkIndices(string matchStartState)
        {
            List<uint> indices = new List<uint>();

            for (uint i = 0; i < transitionCount; i++)
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
            int lowHit = int.MaxValue;
            int lowHitIndex = -1;

            for ( int i = 0; i < transitionCount; i++ )
            {
                // There can be multiple arcs between start and end state.
                // Track the index of the transition with the lowest hitCount.
                // Return the tracked index to the caller, so that coverage is
                // increased.
                if (transitions[i].startState == matchStartState && transitions[i].endState == matchEndState)
                {
                    if (transitions[i].hitCount <= lowHit)
                    {
                        // TODO: Trace transition selection
//                        Console.WriteLine("{0}, {1}, {2}", transitions[i].action, transitions[i].hitCount, transitions[i].startState);
                        lowHit = transitions[i].hitCount;
                        lowHitIndex = i;
                    }
                }
            }
            return lowHitIndex;
        }

        public void Disable(uint tIndex)
        {
            if ( tIndex < transitionCount )
            {
                transitions[tIndex].enabled = false;
            }
        }

        public void DisableByAction(string matchAction)
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
                nodes[count].parent = "";
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

// The test automation worker implements a public class that communicates with
// EzModel through the IEzModelClient interface.  The test automation is
// responsible for
//  (1) setting the states and actions that make up the rules of the model,
//  and optionally
//  (2) driving the system under test,
//  (3) measuring the state of the system under test,
//  (4) reporting outcomes including problems, and
//  (5) reporting the action path from initial state to a problem or end of run.

    public interface IEzModelClient
    {
        const string valueSeparator = ", ";

        // When true, EzModel will not add transitions to the state table that
        // are self-links.  Set this value before calling the GeneratedGraph constructor,
        // as the creation of transitions occurs in the constructor body.
        // EzModel should not change the SkipSelfLinks setting.
        bool SkipSelfLinks { get; set; }

        // When true, EzModel will call the rules.AdapterTransition() method for
        // each traversal step.  rules.AdapterTransition() is responsible for
        // driving the action of the traversal step in the system under test, and
        // must return the end state of the system under test.
        // EzModel should not change the NotifyAdapter setting.
        bool NotifyAdapter { get; set; }

        // When true, EzModel will end the current traversal strategy on the first
        // problem indicated by the rules module.  For example, if StopOnProblem is
        // true, EzModel identifies a false from rules.AreStatesAcceptablySimilar()
        // as a problem, and stops the current traversal strategy, returning
        // control to the rules module.
        // EzModel shoud not change the StopOnProblem setting.
        bool StopOnProblem { get; set; }

        // When the rules module intends to drive the system under test, the
        // GetInitialState method is responsible for initializing both the
        // state of the model and the state of the system under test.
        // When the rules module does not intend to drive the system under test,
        // GetInitialState is responsible only for returning the initial state
        // to EzModel.
        string GetInitialState();
        List<string> GetAvailableActions(string startState);
        string GetEndState(string startState, string action);
        string AdapterTransition(string startState, string action);
        bool AreStatesAcceptablySimilar(string observed, string predicted);
        void ReportProblem(string initialState, string observed, string predicted, List<string> popcornTrail);
        void ReportTraversal(string initialState, List<string> popcornTrail);
        void SetStateOfSystemUnderTest(string state);
    }

    public class GeneratedGraph
    {
        StateTransitions transitions;
        Nodes totalNodes;
        List<string> unexploredStates;

        IEzModelClient client; 

        public string transitionSeparator = " | ";

        public void DisplayStateTable()
        {
            Console.WriteLine("Start state{0}End state{0}Action\n", transitionSeparator);

            for (uint i = 0; i < transitions.Count(); i++)
            {
                string start = transitions.StartStateByIndex(i);
                string end = transitions.EndStateByIndex(i);

                if (client.SkipSelfLinks)
                {
                    if (start == end)
                    {
                        continue;
                    }
                }
                Console.WriteLine(start + transitionSeparator + end + transitionSeparator + transitions.ActionByIndex(i));
            }
            Console.WriteLine("  ");
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

        string FetchUnexploredState()
        {
            string state = unexploredStates[0];
            unexploredStates.RemoveAt(0);
            return state;
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
            // TODO: Trace target transitions
//            Console.WriteLine("Target transition = {0}, {1}", transitions.StartStateByIndex((uint)targetIndex), transitions.ActionByIndex((uint)targetIndex));

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

            while (transitions.GetTraversalsFloor() < minimumCoverageFloor)
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
                        transitions.IncrementHitCount((uint)tIndex);
                        fileCtr++;
                        suffix = String.Format("{0}", fileCtr.ToString("D4"));
                        if (client.NotifyAdapter)
                        {
                            string action = transitions.ActionByIndex((uint)tIndex);
                            popcornTrail.Add(action);
                            string reportedEndState = client.AdapterTransition(state, action);
                            string predicted = transitions.EndStateByIndex((uint)tIndex);
                            if (!client.AreStatesAcceptablySimilar(reportedEndState, predicted))
                            {
                                // Inconsistency.
                                // TODO: Ask the adapter to report a problem, including the popcorn trail.
                                client.ReportProblem(initialState, reportedEndState, predicted, popcornTrail);
                                // If the user wants to stop on problem, stop.  Otherwise:
                                if (client.StopOnProblem)
                                {
                                    return;
                                }

                                // On the actions, add a fault counter.
                                // On first fault on an action, Disable the transition.
                                if (transitions.IncrementActionFaults((uint)tIndex) == 1 )
                                {
                                    // NOTE: the cause of
                                    // the problem may be in the route to this transition.  Building a capability for
                                    // EzModel to pick an alternate route to this transition is useful, and coincident
                                    // with Beeline.  Beeline is an alternate route case.  Beeline can isolate a problem
                                    // transition: if the first problem was detected on transition Z in the route ...,Y,Z, and then
                                    // Beeline succeeds in route ...,X,Z, we may find that another route of ...,Y,Z also has
                                    // a problem.  Y is then the suspect transition.
                                    transitions.Disable((uint)tIndex);
                                }
                                else
                                {
                                    // On second or later fault on the same action, disable the action everywhere.
                                    transitions.DisableByAction(transitions.ActionByIndex((uint)tIndex));
                                    // NOTE: there may be a systemic problem with the action itself.  Two incidents involving the
                                    // same action is reason enough to avoid the action for the remainder of the run.  Development
                                    // team can root-cause the issue.
                                }
                                // Go back to the start of this function, and reset the adapter.
                                goto ResetPosition;
                            }
                        }
                        this.CreateGraphVizFileAndImage(fname, suffix, transitions.ActionByIndex((uint)tIndex));
                    }
                }

                // mark that we covered the target Transition as well
                transitions.IncrementHitCount((uint)targetIndex);

                state = transitions.EndStateByIndex((uint)targetIndex);  // move to the end node of the target transition
                if (client.NotifyAdapter)
                {
                    string action = transitions.ActionByIndex((uint)targetIndex);
                    popcornTrail.Add(action);
                    string reportedEndState = client.AdapterTransition(transitions.StartStateByIndex((uint)targetIndex), action);
                    string predicted = transitions.EndStateByIndex((uint)targetIndex);
                    if (!client.AreStatesAcceptablySimilar(reportedEndState, predicted))
                    {
                        client.ReportProblem(initialState, reportedEndState, predicted, popcornTrail);
                        if (client.StopOnProblem)
                        {
                            return;
                        }

                        if (transitions.IncrementActionFaults((uint)targetIndex) == 1)
                        {
                            transitions.Disable((uint)targetIndex);
                        }
                        else
                        {
                            transitions.DisableByAction(transitions.ActionByIndex((uint)targetIndex));
                        }
                        // Inconsistency.  Stop the traversal.
                        goto ResetPosition;
                    }
                }

                fileCtr++;
                suffix = String.Format("{0}", fileCtr.ToString("D4"));
                this.CreateGraphVizFileAndImage(fname, suffix, transitions.ActionByIndex((uint)targetIndex));
            }
            // TODO: Trace floor coverage
            Console.WriteLine("Reached coverage floor of {0} in {1} iterations.", minimumCoverageFloor, loopctr);

            if (client.NotifyAdapter)
            {
                client.ReportTraversal(initialState, popcornTrail);
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
                    w.WriteLine("\"{0}\"\t[label=\"{1}\"]", totalNodes.GetNodeByIndex(i).state, totalNodes.GetNodeByIndex(i).state.Replace(",", "\\n"));
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
