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
using System.Globalization;

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

        public string ActionIndicesToString()
        {
            string result = String.Empty;

            for (uint i = 0; i < transitionCount; i++)
            {
                result += transitions[i].actionIndex;
                if (i < transitionCount - 1)
                {
                    result += ",";
                }
            }
            return result;
        }

        public string ActionsToString()
        {
            string result = String.Empty;

            for (uint i = 0; i < actionCount; i++)
            {
                result += "\"" + actions[i].action + "\"";
                if (i < actionCount - 1)
                {
                    result += ",";
                }
            }
            return result;
        }

        public string EnabledFlagsToString()
        {
            string flags = "[";

            for (uint i=0; i < transitionCount; i++)
            {
                flags += (transitions[i].enabled ? "true" : "false") + (i == transitionCount-1 ? "" : ",");
            }
            flags += "]";
            return flags;
        }

        public bool Add(string startState, string endState, string action)
        {
            if (transitionCount < transitions.Length)
            {
                transitions[transitionCount].startState = startState;
                transitions[transitionCount].endState = endState;
                transitions[transitionCount].enabled = true;

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
                    if (actionCount < actions.Length)
                    {
                        transitions[transitionCount].actionIndex = actionCount;
                        actions[actionCount].action = action;
                        actionCount++;
                    }
                    else
                    {
                        Console.WriteLine("Not enough actions: choose a larger value for the maximumActions argument.");
                        return false;  // The transition was not added because the action could not be added.
                    }
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
                if (transitions[i].enabled && transitions[i].hitCount == lowHit)
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
                if (transitions[i].enabled && transitions[i].hitCount < floor)
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
                if (transitions[i].enabled && transitions[i].hitCount == lowHit)
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
                if (transitions[i].enabled && transitions[i].hitCount == lowHit)
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

        public List<uint> GetStateTransitionIndices(string state)
        {
            // return all transitions involving the state, i.e.,
            // outlinks, inlinks, and self-links.

            List<uint> indices = new List<uint>();

            for (uint i = 0; i < transitionCount; i++)
            {
                if (transitions[i].enabled && transitions[i].startState == state || transitions[i].endState == state)
                {
                    indices.Add(i);
                }
            }

            return indices;
        }

        public List<uint> GetOutlinkTransitionIndices(string state)
        {
            List<uint> indices = new List<uint>();

            for (uint i = 0; i < transitionCount; i++)
            {
                if (transitions[i].enabled && transitions[i].startState == state)
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
                if (transitions[i].enabled && transitions[i].startState == startState && transitions[i].endState == endState)
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
                if (transitions[i].enabled && transitions[i].endState == endState)
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
                if (transitions[i].enabled && transitions[i].startState == startState)
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

    public enum SelfLinkTreatmentChoice
    {
        SkipAll, OnePerAction, AllowAll
    }

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
        SelfLinkTreatmentChoice SelfLinkTreatment { get; set; }

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

    public class EzModelGraph
    {
        StateTransitions transitions;
        Nodes totalNodes;
        List<string> unexploredStates;

        Queue<int> path = new Queue<int>();

        // These next 5 collections are uninitialized.  A traversal routine
        // must call InitializeSVGDeltas before the first call to
        // AppendSVGDeltas.  That ensures the collections are empty when the
        // traversal begins, or when it resets.
        List<uint> traversedEdge;
        List<string> pathEdges;
        List<string> pathNodes;
        List<int> startnode;
        List<int> pathEndNode;

        const string EzModelFileName = "EzModelDigraph";
        uint problemCount = 0;
        uint traversalCount = 0;
        double wallStartTime; // Initialize at the top of a traversal.

        IEzModelClient client;

        public enum GraphShape
        {
            Circle, Default
        }

        // currentShape is relied on for EzModel internal calls
        // to CreateGraphVizFileAndImage().
        GraphShape currentShape = GraphShape.Default;

        Random rnd = new Random(DateTime.Now.Millisecond);

        struct tempTransition
        {
            public string startState;
            public string endState;
            public string action;

            public tempTransition(string startState, string endState, string action)
            {
                this.startState = startState;
                this.endState = endState;
                this.action = action;
            }
        };

        public EzModelGraph(IEzModelClient theEzModelClient, uint maxTransitions = 1000, uint maxNodes = 20, uint maxActions = 20)
        {
            client = theEzModelClient;

            transitions = new StateTransitions(maxTransitions, maxActions);

            totalNodes = new Nodes(maxNodes);
        }

        public bool GenerateGraph()
        {
            unexploredStates = new List<string>();

            string state = client.GetInitialState();
            unexploredStates.Add(state); // Adding to the <List> instance
            totalNodes.Add(state); // Adding to the Nodes class instance

            List<tempTransition> tempTransitions = new List<tempTransition>();

            while (unexploredStates.Count > 0)
            {
                // generate all transitions out of state s
                state = FetchUnexploredState();
                List<string> Actions = client.GetAvailableActions(state);

                foreach (string action in Actions)
                {
                    // an endstate is generated from current state + changes from an invoked action
                    string endState = client.GetEndState(state, action);

                    // if generated endstate is new, add  to the totalNode & unexploredNode lists
                    if (!totalNodes.Contains(endState))
                    {
                        if (!totalNodes.Add(endState)) // try to Adds the Node to Nodes class instance
                        {
                            Console.WriteLine("Not enough nodes: choose a larger maximumNodes argument in the call to GeneratedGraph()");
                            return false;
                        }    
                        
                        unexploredStates.Add(endState); // Adds a string to List instance
                    }

                    tempTransitions.Add(new tempTransition(state, endState, action));
                }
            }

            switch (client.SelfLinkTreatment)
            {
                case SelfLinkTreatmentChoice.SkipAll:
                    foreach (tempTransition t in tempTransitions)
                    {
                        // add this {startState, endState, action} transition to the Graph
                        // except in the case where client.SkipSelfLinks is true AND startState == endState
                        if (t.startState != t.endState)
                        {
                            if (!transitions.Add(t.startState, t.endState, t.action))
                            {
                                Console.WriteLine("Possibly not enough transitions: choose a larger maximumTransitions argument in the call to GeneratedGraph()");
                                return false;
                            }
                        }
                    }
                    break;
                case SelfLinkTreatmentChoice.OnePerAction:
                    for (int i = tempTransitions.Count-1; i >= 0; i--)
                    {
                        if (tempTransitions[i].startState != tempTransitions[i].endState)
                        {
                            if (!transitions.Add(tempTransitions[i].startState, tempTransitions[i].endState, tempTransitions[i].action))
                            {
                                Console.WriteLine("Possibly not enough transitions: choose a larger maximumTransitions argument in the call to GeneratedGraph()");
                                return false;
                            }
                            tempTransitions.RemoveAt(i);
                        }
                    }
                    while (tempTransitions.Count > 0)
                    {
                        // The remaining tempTransitions are all self-links.
                        // Select one self-link for each different action.
                        string match = tempTransitions[0].action;
                        List<int> indices = new List<int>();
                        for (int i=0; i < tempTransitions.Count; i++)
                        {
                            if (tempTransitions[i].action == match)
                            {
                                indices.Add(i);
                            }
                        }

                        int index = rnd.Next(indices.Count);

                        tempTransition t = tempTransitions[indices[index]];

                        if (!transitions.Add(t.startState, t.endState, t.action))
                        {
                            Console.WriteLine("Possibly not enough transitions: choose a larger maximumTransitions argument in the call to GeneratedGraph()");
                            return false;
                        }

                        for (int i=indices.Count-1; i >= 0; i--)
                        {
                            tempTransitions.RemoveAt(indices[i]);
                        }
                    }
                    break;
                case SelfLinkTreatmentChoice.AllowAll:
                    foreach (tempTransition t in tempTransitions)
                    {
                        if (!transitions.Add(t.startState, t.endState, t.action))
                        {
                            Console.WriteLine("Possibly not enough transitions: choose a larger maximumTransitions argument in the call to GeneratedGraph()");
                            return false;
                        }
                    }
                    break;
                default:
                    break;
            }
            return true; // graph generated :-)
        }

        public List<string> AnalyzeConnectivity()
        {
            // Per Harry Robinson, algorithm to determine connection problems or
            // strongly connected graph: select a node.  Determine whether there
            // is a path from that node to each of the other nodes in the graph.
            // Any nodes for which there is not a path are not strongly connected.
            // For each of the other nodes, determine whether there is a path back
            // to the selected node.  Any nodes for which there is not a path are
            // not strongly connected.
            // Bonus: report transitions where the start or end state does not
            // match a node.
            // The return string will be empty for a strongly connected graph.
            // For a graph with connection problems, the returned string will
            // contain all the problem assessments from this routine.
            List<string> report = new List<string>();

            string startState = totalNodes.GetStateByIndex(0);

            for (uint i = 1; i < totalNodes.Count(); i++)
            {
                string endState = totalNodes.GetStateByIndex(i);
                Queue<int> pathList = FindShortestPath(startState, endState);
                if (pathList.Count == 0)
                {
                    // There is no path from the startState to the endState.
                    report.Add(String.Format("There is no path from [{0}] to [{1}].\n", startState, endState));
                }

                pathList.Clear();
                pathList = FindShortestPath(endState, startState);
                if (pathList.Count == 0)
                {
                    // There is no path from the startState to the endState.
                    report.Add(String.Format("There is no path from [{0}] to [{1}].\n", endState, startState));
                }
            }

            // Transition check.  Report each transition where the start or end state does not match a node.
            for (uint i = 0; i < transitions.Count(); i++)
            {
                string s = transitions.StartStateByTransitionIndex(i);
                string e = transitions.EndStateByTransitionIndex(i);
                bool startExists = totalNodes.GetIndexByState(s) > -1;
                bool endExists = totalNodes.GetIndexByState(e) > -1;

                if (!startExists && !endExists)
                {
                    report.Add(String.Format("The start and end states do not exist for the transition {0} -> {1} : {2}", s, e, transitions.ActionByTransitionIndex(i)));
                }
                else if (!startExists || !endExists)
                {
                    report.Add(String.Format("The {0}{1} state does not exist for the transition {2} -> {3} : {4}", startExists ? "" : "start", endExists ? "" : "end", s, e, transitions.ActionByTransitionIndex(i)));
                }
            }

            return report;
        }

        void InitializeSVGDeltas()
        {
            traversedEdge = new List<uint>();
            pathEdges = new List<string>();
            pathNodes = new List<string>();
            startnode = new List<int>();
            pathEndNode = new List<int>();
        }

        void AppendSvgDelta(uint transitionIndex, int endOfPathTransitionIndex = -1)
        {
            int endOfPathNodeIndex = -1;

            if (endOfPathTransitionIndex > -1)
            {
                endOfPathNodeIndex = totalNodes.GetIndexByState(transitions.EndStateByTransitionIndex((uint)endOfPathTransitionIndex));
            }

            int startStateIndex = totalNodes.GetIndexByState(transitions.StartStateByTransitionIndex((uint)transitionIndex));

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
            pathNodeIndices.Add(endOfPathNodeIndex);
            traversedEdge.Add(transitionIndex);
            // CAUTION TODO:
            // Here the edge of the endOfPathTransitionIndex is added to the pathEdges
            // because it is needed to complete the path from the RandomDestinationCoverage()
            // routine.  At time of writing this code it is unknown whether other
            // traversals will have the same kind of path structure.  So the TODO
            // is to analyze edges involved in the path for other traversals, and
            // make adjustments to this bit of code if needed to keep the pathEdges
            // correctly representing the path.
            List<int> tempPath = new List<int>();
            foreach( int i in path )
            {
                tempPath.Add(i);
            }
            if (endOfPathTransitionIndex > -1)
            {
                tempPath.Add(endOfPathTransitionIndex);
            }
            pathEdges.Add(String.Format("[{0}]", String.Join(",", tempPath)));
            pathNodes.Add(String.Format("[{0}]", String.Join(",", pathNodeIndices)));
            startnode.Add(startStateIndex);
            pathEndNode.Add(endOfPathNodeIndex);
        }

        void WriteSvgDeltasFile(string fileName)
        {
            // TODO:
            // Add a histogram of transition hitcounts.
            // One bar for each transition.  Hitcount on the Y axis
            // 
            string[] ezModelGraph = File.ReadAllLines(EzModelFileName + traversalCount + ".svg");
            using (FileStream fs = new FileStream(fileName + traversalCount + ".html", FileMode.Create))
            {
                using (StreamWriter w = new StreamWriter(fs, Encoding.ASCII))
                {
                    // NOTE: <!DOCTYPE html> means HTML 5 to the browser.
                    w.WriteLine(
@"<!DOCTYPE html>
<html>
	<head>
		<style>
input[type=""range""] {
	width:300px;
}
TD { 
	font-family: Arial; 
	font-size: 10pt; 
}
TH {
	font-family: Arial;
	font-size: 12pt;
}
</style>
	</head>
	<body>
	<table border=""0"" width=""100%"" height=""100%"">
		<tr>
			<th colspan=""2"" id=""selectedSvgElementInfo"" style=""height:20px; color:#e00000""></th> 
		</tr>
		<tr>
			<td width=""50"">
				<table border=""0"">
					<tr><td style=""text-align:right"">18</td><td style=""width:50%; background-color:#FF7F7F""></td></tr> 
					<tr><td style=""text-align:right"">17</td><td style=""width:50%; background-color:#97FF2F""></td></tr>
					<tr><td style=""text-align:right"">16</td><td style=""width:50%; background-color:#771FAF""></td></tr>
					<tr><td style=""text-align:right"">15</td><td style=""width:50%; background-color:#4F7FEF""></td></tr>
					<tr><td style=""text-align:right"">14</td><td style=""width:50%; background-color:#8FCF27""></td></tr>
					<tr><td style=""text-align:right"">13</td><td style=""width:50%; background-color:#A71770""></td></tr>
					<tr><td style=""text-align:right"">12</td><td style=""width:50%; background-color:#77EF77""></td></tr>
					<tr><td style=""text-align:right"">11</td><td style=""width:50%; background-color:#CF77CF""></td></tr>
					<tr><td style=""text-align:right"">10</td><td style=""width:50%; background-color:#BF4F4F""></td></tr>
					<tr><td style=""text-align:right"">9</td><td style=""width:50%; background-color:#3737AF""></td></tr>
					<tr><td style=""text-align:right"">8</td><td style=""width:50%; background-color:#008F00""></td></tr>
					<tr><td style=""text-align:right"">7</td><td style=""width:50%; background-color:#EFD700""></td></tr>
					<tr><td style=""text-align:right"">6</td><td style=""width:50%; background-color:#8F008F""></td></tr>
					<tr><td style=""text-align:right"">5</td><td style=""width:50%; background-color:#0000DF""></td></tr>
					<tr><td style=""text-align:right"">4</td><td style=""width:50%; background-color:#FFAF00""></td></tr>
					<tr><td style=""text-align:right"">3</td><td style=""width:50%; background-color:#DF00DF""></td></tr>
					<tr><td style=""text-align:right"">2</td><td style=""width:50%; background-color:#00DF00""></td></tr>
					<tr><td style=""text-align:right"">1</td><td style=""width:50%; background-color:#00AFFF""></td></tr>
					<tr><td style=""text-align:right"">0</td><td style=""width:50%; background-color:#000000""></td></tr>
				</table>
			</td>
			<td>
				<table border=""1"" rules=""none"" width=""100%"" height=""100%"">
					<tr>
						<td>
<!-- Generated by graphviz -->
");
                    bool copyToStream = false;
                    bool addGradientDefs = false;

                    for (var i=0; i < ezModelGraph.Length; i++)
                    {
                        if (copyToStream == true)
                        {
                            if (ezModelGraph[i].StartsWith("<!-- "))
                            {
                                continue;
                            }
                            if (ezModelGraph[i].StartsWith("<polygon fill=\"white\" "))
                            {
                                continue;
                            }
                            if (ezModelGraph[i].StartsWith("<g id=\"edge"))
                            {  // zero-base the edge IDs by decrementing.
                                string substr = ezModelGraph[i].Substring(11);
                                int edgeId = int.Parse(substr.Substring(0, substr.IndexOf("\" class=\"")));
                                int newId = edgeId - 1;
                                ezModelGraph[i] = ezModelGraph[i].Replace("\"edge" + edgeId.ToString(), "\"edge" + newId.ToString());
                            }
                            if (ezModelGraph[i].StartsWith("<g id=\"node"))
                            {  // zero-base the node IDs by decrementing.
                                string substr = ezModelGraph[i].Substring(11);
                                int nodeId = int.Parse(substr.Substring(0, substr.IndexOf("\" class=\"")));
                                int newId = nodeId - 1;
                                ezModelGraph[i] = ezModelGraph[i].Replace("\"node" + nodeId.ToString(), "\"node" + newId.ToString());
                            }
                            if (ezModelGraph[i].StartsWith("<ellipse "))
                            {
                                ezModelGraph[i] = ezModelGraph[i].Replace(" stroke=", " onclick=\"attr(evt)\" stroke=");
                            }
                            if (ezModelGraph[i].StartsWith("<polygon "))
                            {
                                ezModelGraph[i] = ezModelGraph[i].Replace(" stroke=", " onclick=\"attr(evt)\" stroke=");
                            }
                            if (ezModelGraph[i].StartsWith("<text "))
                            {
                                ezModelGraph[i] = ezModelGraph[i].Replace(" text-anchor=\"middle\" ", " text-anchor=\"middle\" onclick=\"attr(evt)\" ");
                            }
                            w.WriteLine(ezModelGraph[i]);
                            if (addGradientDefs == true)
                            {
                                w.WriteLine(
@"<defs><linearGradient id=""greenBlueGradient"" x1=""0%"" y1=""0%"" x2=""100%"" y2=""0%"" spreadMethod=""pad"">    <stop offset=""0%"" stop-color=""yellowgreen"" stop-opacity=""1""/>    <stop offset=""100%"" stop-color=""lightskyblue"" stop-opacity=""1""/></linearGradient><linearGradient id=""blueGreenGradient"" x1=""0%"" y1=""0%"" x2=""100%"" y2=""0%"" spreadMethod=""pad"">    <stop offset=""0%"" stop-color=""lightskyblue"" stop-opacity=""1""/>    <stop offset=""100%"" stop-color=""yellowgreen"" stop-opacity=""1""/></linearGradient></defs>");
                                addGradientDefs = false;
                            }
                        }
                        if (ezModelGraph[i].StartsWith("<svg width"))
                        {
                            w.WriteLine(
@"<svg id=""svgOuter"" onmousedown=""svgMouseDown(evt)"" onmouseup=""svgMouseUp(evt)"" onmouseleave=""svgMouseLeave(evt)"" onwheel=""svgMouseWheel(evt)"" ");
                            copyToStream = true;
                            addGradientDefs = true;
                        }
                    }
                    w.WriteLine( // This block starts with a closing svg tag, matching an encapsulating svg tag way above.
@"            </td>
		</tr>
	</table>
</td>
</tr>
<tr>
	<td></td>
	<td>
		<table>
			<tr>
			    <td>
			    	<table border=""1"" rules=""none"">
			    		<tr>
			    			<td style=""text-align:center; padding-top:2px; padding-left:2px; padding-right:2px"" colspan=""3"">Transition</td>
			    		</tr>
			    		<tr>
			    			<td style=""padding-left:2px""><button onclick=""traversalStepBack()"">&lt;</button></td>
							<td id=""stepTD"" style=""text-align:center; width:100""><label id=""step""></label></td>
						    <td style=""padding-right:2px""><button onclick=""traversalStepForward()"">&gt;</button></td>
						</tr>
						<tr>
							<td style=""text-align:center; padding-bottom:2px; padding-left:2px; padding-right:2px"" colspan=""3""><label id=""transitionFloor""></label></td>
						</tr>
					</table>
				</td>
				<td style=""width:30px""></td>
				<td>
					<table border=""1"" rules=""none"">
						<tr>
						    <td style=""padding-top:5px; padding-left:20px;""><button onclick=""startTraversal()"">&#9654;</button></td>
							<td style=""text-align:center; padding-top:5px""><label id=""speedLabel"">Speed: 1</label></td>
						    <td style=""text-align:right; padding-top:5px; padding-right:20px""><button onclick=""stopTraversal()"">&#9209;</button></td>
						</tr>
			    		<tr>
							<td style=""padding-bottom:5px; padding-left:2px; padding-right:2px"" colspan=""3"">1<input type=""range"" min=""1"" max=""60"" oninput=""changeSpeed()"" value=""1"" id=""traversalSpeed"">60</td>
						</tr>
					</table>
				</td>
				<td style=""width:30px""></td>
				<td>
					<table>
						<tr>
							<td><button onclick=""fitGraph()"">FIT GRAPH</button></td>
							<td><input type=""checkbox"" id=""recycleCbox"" name=""recycleCBox"" value=""recycleColors""><label for=""recycleCbox""> Recycle hitcount colors</label></td>
							<td><button id=""btnReset"" onclick=""reset()"">RESET TRAVERSAL</button></td>
						</tr>
						<tr>
							<td colspan=""2""><input type=""range"" min=""0"" max=""75"" oninput=""updateDisplayAttributes()"" value=""35"" id=""subgraphOpacity""></td>
							<td><input type=""checkbox"" onclick=""updateDisplayAttributes()"" id=""isolateSubgraph""><label for=""isolateSubgraph""> Isolate subgraph</label></td>
						</tr>
					</table>
				</td>
		    </tr>
		</table>
	</td>
</tr>
</table>
<script type=""text/ecmascript"">
var step = -1; // Because step is an index into an array.
");
                    w.WriteLine("const actionNames = [{0}];", transitions.ActionsToString());
                    w.WriteLine(" ");
                    w.WriteLine("const transitionActions = [{0}];", transitions.ActionIndicesToString());
                    w.WriteLine(" ");
                    w.WriteLine("const transitionEnabledFlags = {0};", transitions.EnabledFlagsToString());
                    w.WriteLine(" ");
                    w.WriteLine("const transitionHitCounts = new Array({0}).fill(0);", transitions.Count());
                    w.WriteLine(" ");
                    w.WriteLine("const nodeCount = {0};", totalNodes.Count());
                    w.WriteLine(" ");
                    w.WriteLine("const traversedEdge = [{0}];", String.Join(",", traversedEdge));
                    w.WriteLine(" ");
                    w.WriteLine("const pathEdges = [{0}];", String.Join(",", pathEdges));
                    w.WriteLine(" ");
                    w.WriteLine("const pathNodes = [{0}];", String.Join(",", pathNodes));
                    w.WriteLine(" ");
                    w.WriteLine("const startNode = [{0}];", String.Join(",", startnode));
                    w.WriteLine(" ");
                    w.WriteLine("const pathEndNode = [{0}];", String.Join(",", pathEndNode));
                    w.WriteLine(" ");

                    List<string> subgraphNodes = new List<string>();
                    List<string> subgraphEdges = new List<string>();

                    for (int i = 0; i < totalNodes.Count(); i++)
                    {
                        string nodeState = totalNodes.GetStateByIndex((uint)i);

                        // Get the outlinks and inlinks of the node
                        List<uint> inOutSelf = transitions.GetStateTransitionIndices(nodeState);

                        List<int> nodeIds = new List<int>();
                        nodeIds.Add(i); // The node IDs in GraphViz are 1-based.

                        // First output the nodes related to the transitions.
                        List<string> stateList = new List<string>();

                        stateList.Add(nodeState);

                        foreach (uint tIndex in inOutSelf)
                        {
                            string s = transitions.StartStateByTransitionIndex(tIndex);
                            if (!stateList.Contains(s))
                            {
                                nodeIds.Add(totalNodes.GetIndexByState(s));
                                stateList.Add(s);
                            }
                            s = transitions.EndStateByTransitionIndex(tIndex);
                            if (!stateList.Contains(s))
                            {
                                nodeIds.Add(totalNodes.GetIndexByState(s));
                                stateList.Add(s);
                            }
                        }

                        subgraphNodes.Add("[" + string.Join(",", nodeIds) + "]");
                        subgraphEdges.Add("[" + String.Join(",", inOutSelf) + "]");
                    }

                    w.WriteLine("const subgraphNodes = [{0}];", String.Join(",", subgraphNodes));
                    w.WriteLine(" ");
                    w.WriteLine("const subgraphEdges = [{0}];", String.Join(",", subgraphEdges));
                    w.WriteLine(" ");
                    w.WriteLine(
@"var c = 0;
var t;
var timer_is_on = 0;

var coverageFloor;

var initialBox = svgOuter.getAttribute(""viewBox"");
var initialBits = initialBox.split("" "");

const stepElemSize = 18*Math.floor(Math.log10(traversedEdge.length)) + 27;
var stepElem = document.getElementById(""stepTD"");
stepElem.setAttribute(""style"", ""text-align:center; width:"" + stepElemSize.toString() + ""px"");
setStepText();
assessCoverageFloor();

var selectedSvgElement = undefined;

for (var j=0; j < transitionActions.length; j++)
{
    var action = actionNames[transitionActions[j]];
    var text = document.getElementById(""edge"" + j.toString()).getElementsByTagName(""text"");
    if (text.length > 0)
    {
        text[0].innerHTML = action;
    }
}

function changeSpeed(e) {
	document.getElementById(""speedLabel"").innerHTML = ""Speed: "" + document.getElementById(""traversalSpeed"").value.toString();
}

function fitGraph() {
    document.getElementById(""svgOuter"").setAttribute(""viewBox"", initialBox);
}

var previousSubgraph = undefined;
var previousFill = undefined;
var currentSubgraph = undefined;

function allComponentsToFullOpacity() {
    for (var i=0; i < transitionHitCounts.length; i++)
    {
        document.getElementById(""edge"" + i).setAttribute(""opacity"", ""1.0"");
    }
    for (var i=0; i < nodeCount; i++)
    {
        document.getElementById(""node"" + i).setAttribute(""opacity"", ""1.0"");
    }
}

function updateDisplayAttributes() {
    if (currentSubgraph == undefined || !document.getElementById(""isolateSubgraph"").checked)
    {
    	if (previousSubgraph != undefined)
    	{
	    	var ellipse = document.getElementById(""node"" + previousSubgraph).getElementsByTagName(""ellipse"")[0];
	    	ellipse.setAttribute(""fill"", previousFill);
		}
		allComponentsToFullOpacity();
    }
    if (document.getElementById(""isolateSubgraph"").checked && currentSubgraph != undefined)
    {
    	opacityValue = document.getElementById(""subgraphOpacity"").value / 100.0;
        for (var i=0; i < transitionHitCounts.length; i++)
        {
            document.getElementById(""edge"" + i).setAttribute(""opacity"", subgraphEdges[currentSubgraph].includes(i) ? ""1.0"" : opacityValue.toString());
        }
        for (var i=0; i < nodeCount; i++)
        {
            document.getElementById(""node"" + i).setAttribute(""opacity"", subgraphNodes[currentSubgraph].includes(i) ? ""1.0"" : opacityValue.toString());
        }

        if (previousSubgraph != undefined)
        {
	        var ellipse = document.getElementById(""node"" + previousSubgraph).getElementsByTagName(""ellipse"")[0];
	        ellipse.setAttribute(""fill"", previousFill);
	    }
        previousSubgraph = subgraphNodes[currentSubgraph][0];
        var ellipse = document.getElementById(""node"" + previousSubgraph).getElementsByTagName(""ellipse"")[0];
        previousFill = ellipse.getAttribute(""fill"");
        ellipse.setAttribute(""fill"", ""#ffff77"");
    }
}

function releaseSelection() {
	if (currentSubgraph != undefined)
	{
        currentSubgraph = undefined;
    }
    updateDisplayAttributes();

    if (selectedSvgElement != undefined)
    {
        var text = selectedSvgElement.getElementsByTagName(""text"");
        if (text && text.length > 0)
        {
            for (var i = 0; i < text.length; i++)
            {
                text[i].setAttribute(""fill"", ""black"");
            }
        }
        document.getElementById(""selectedSvgElementInfo"").innerHTML = "" "";
        selectedSvgElement = undefined;
    }
}

function attr(event) {
    releaseSelection();
    var t = event.target;
    var p = t.parentNode;
    selectedSvgElement = p;
    var id = p.getAttribute(""id"");
    var msg = """";

    switch (id.substring(0,4))
    {
        case ""node"":
        	var nodenum = id.substring(4);
        	if (currentSubgraph != undefined)
        	{
        		if (currentSubgraph == nodenum)
        		{
        			return;
        		}
        	}
            currentSubgraph = nodenum;
            updateDisplayAttributes();
            var title = p.getElementsByTagName(""title"");
            if (title && title.length > 0)
            {
                msg += title[0].innerHTML;
            }
            var text = p.getElementsByTagName(""text"");
            if (text && text.length > 0)
            {
                for (var i=0; i<text.length; i++)
                {
                    text[i].setAttribute(""fill"", ""red"");
                }
            }
            break;
        case ""edge"":
            var text = p.getElementsByTagName(""text"");
            if (text && text.length > 0)
            {
                msg += text[0].innerHTML;
                text[0].setAttribute(""fill"", ""red"");
            }
            break;
        default:
            break;
    }

    document.getElementById(""selectedSvgElementInfo"").innerHTML = msg;
}

var viewBoxBits = document.getElementById(""svgOuter"").getAttribute(""viewBox"").split("" "");
var originalBits = [parseFloat(viewBoxBits[0]), parseFloat(viewBoxBits[1]),
    parseFloat(viewBoxBits[2]), parseFloat(viewBoxBits[3])];
var newBits = originalBits;
var translateScale = newBits[2] / originalBits[2];

function svgMouseDown(event) {
    var t = event.target;
    switch (event.button)
    {
        case 0: // main button, e.g., left on a default setup
            t.onmousemove = svgMouseMove;
            didTranslate = false;
            break;
        case 1: // mouse wheel
            break;
        case 2: // secondary button, e.g., right on a default setup
            break;
        default: // we don't interepret any other mouse buttons, yet
            break;
    }
    // https://www.javascripttutorial.net/javascript-dom/javascript-mouse-events/
}

var mouseMovePreviousX = undefined;
var mouseMovePreviousY = undefined;
var didTranslate = false;

function svgMouseMove(event) {
    didTranslate = true;
    var t = event.target;
    var x = event.clientX;
    var y = event.clientY;
    if (mouseMovePreviousX == undefined || mouseMovePreviousY == undefined)
    {
        mouseMovePreviousX = x;
        mouseMovePreviousY = y;
    }
    else
    {
        var dx = translateScale * 0.5 * (mouseMovePreviousX - x);
        var dy = translateScale * 0.5 * (mouseMovePreviousY - y);
        mouseMovePreviousX = x;
        mouseMovePreviousY = y;
        translateViewBox(dx, dy);
    }
}

var svgRescale = 1.0;
var previousDeltaYWasPositive = true;

function svgMouseWheel(event) {
    didTranslate = true;
    if (event.deltaY > 0)
    {
    	if (previousDeltaYWasPositive)
    	{
	    	svgRescale *= 1.01;
	    }
	    else
	    {
	    	svgRescale = 1.01;
	    }
	    previousDeltaYWasPositive = true;
    }
    else
    {
    	if (previousDeltaYWasPositive)
    	{
    		svgRescale = 0.99;
    	}
    	else
    	{
    		svgRescale *= 0.99;
    	}
    	previousDeltaYWasPositive = false;
    }

    newBox = rescaleViewBox( svgRescale );
}

function svgMouseLeave(event) {
    svgMouseChange(event);
}

function svgMouseUp(event) {
    if (!didTranslate && selectedSvgElement != undefined)
    {
        releaseSelection();
    }
    svgMouseChange(event);
}

function svgMouseChange(event) {
	svgRescale = 1.0;
    didTranslate = false;
    mouseMovePreviousX = undefined;
    mouseMovePreviousY = undefined;
    var t = event.target;
    t.onmousemove = null;
}

function rescaleViewBox(scale) {
    var svgOuter = document.getElementById(""svgOuter"");
    var viewBox = svgOuter.getAttribute(""viewBox"");
    var boxBits = viewBox.split("" "");
    var width = parseFloat(boxBits[2]);
    var height = parseFloat(boxBits[3]);
    var xMin = parseFloat(boxBits[0]);
    var yMin = parseFloat(boxBits[1]);

    if (Math.abs(scale - 1.0) < 0.001)
    {
        // do nothing on an almost nothing scale change.
        return;
    }

    xMin += 0.5 * (1.0 - scale) * width;
    yMin += 0.5 * (1.0 - scale) * height;
    width *= scale;
    height *= scale;

    if (width < 20 || height < 20 || width > 5000 || height > 5000)
    {
        return viewBox;
    }

    newBits = [xMin, yMin, width, height];
    translateScale = newBits[2] / originalBits[2];
    xString = xMin.toFixed(2).toString();
    yString = yMin.toFixed(2).toString();
    wString = width.toFixed(2).toString();
    hString = height.toFixed(2).toString();
    var newViewBox = xString + "" "" + yString + "" "" + wString + "" "" + hString;
    svgOuter.setAttribute(""viewBox"", newViewBox);
    return newViewBox;
}

function translateViewBox(dx, dy) {
    var svgOuter = document.getElementById(""svgOuter"");
    var viewBox = svgOuter.getAttribute(""viewBox"");
    var boxBits = viewBox.split("" "");
    var xMin = parseFloat(boxBits[0]) + dx;
    var yMin = parseFloat(boxBits[1]) + dy;
    xString = xMin.toFixed(2).toString();
    yString = yMin.toFixed(2).toString();

    var newViewBox = xString + "" "" + yString + "" "" + boxBits[2] + "" "" + boxBits[3];
    svgOuter.setAttribute(""viewBox"", newViewBox);
}

function setTransitionFloorText() {
	document.getElementById(""transitionFloor"").innerHTML = ""Hitcount floor: "" + coverageFloor;
}

function timedTraversal() {
	c = c + 1;
	traversalStepForward();
	if (timer_is_on)
	{
		var dt = 1000.0 / parseFloat(document.getElementById(""traversalSpeed"").value);
		t = setTimeout(timedTraversal, Math.round(dt));
	}
}

function startTraversal() {
  if (!timer_is_on) {
    timer_is_on = 1;
    timedTraversal();
  }
}

function stopTraversal() {
  clearTimeout(t);
  timer_is_on = 0;
}

function reset() {
	timer_is_on = 0;
	clearTimeout(t);

    document.getElementById(""selectedSvgElementInfo"").innerHTML = "" "";
    for (var i = 0; i < transitionHitCounts.length; i++)
    {
        transitionHitCounts[i] = 0;
    }
    refreshGraphics(""black"");

    step = -1; // Because step is an index into an array.
    setStepText();
    assessCoverageFloor();
}

function setStepText() {
	document.getElementById(""step"").innerHTML = (step+1) + ""/"" + traversedEdge.length;
}

function refreshGraphics(refreshColor) {
    // Set all path and polygon strokes and stroke-width values to 2,
    // and stroke color to black.  Set the fill to none on all graph nodes.

	// Walk backwards from the current step and set a 
	// rendered flag on each transition as it is encountered.
	// Do not render a transition that has rendered==true.
	var rendered = new Array(transitionHitCounts.length).fill(false);
    var edgesToRender = traversedEdge.length;

    // Before we walk backwards, clear each edge of the lookahead path
    // that is beyond the present edge.
    if (step > -1 && step < traversedEdge.length)
    {
        for (var i=1; i < pathEdges[step].length; i++)
        {
            var index = pathEdges[step][i];
            var hitColor = getHitColor(transitionHitCounts[index]);

            rendered[index] = true;
            edgesToRender--;

            var edge = document.getElementById(""edge"" + index.toString());
            var path = edge.getElementsByTagName(""path"");
            if (path.length > 0)
            {
                path[0].setAttribute(""stroke-width"", ""3"");
                path[0].setAttribute(""stroke"", hitColor);
            }
            var poly = edge.getElementsByTagName(""polygon"");
            if (poly.length > 0)
            {
                poly[0].setAttribute(""stroke-width"", ""3"");
                poly[0].setAttribute(""fill"", hitColor);
                poly[0].setAttribute(""stroke"", hitColor);
            }
        }
    }

    for (var i = step; i >= 0 && edgesToRender > 0; i--)
    {
    	var edgeIndex = traversedEdge[i];
    	if (rendered[edgeIndex])
    	{
    		continue;
    	}

        // When there are many steps, we will cover all the edges before we
        // get through all the steps, and we can stop looking for edges to
        // paint.  So count down the edges to render to zero, and stop.
        edgesToRender--; 

    	rendered[edgeIndex] = true;
        var hitCount = transitionHitCounts[edgeIndex];
        var hitColor = getHitColor(hitCount);
        var action = actionNames[transitionActions[edgeIndex]];
        var edge = document.getElementById(""edge"" + edgeIndex.toString());
        var path = edge.getElementsByTagName(""path"");
        if (path.length > 0)
        {   // for hitCount 0 set the stroke width to 1.
            path[0].setAttribute(""stroke-width"", hitCount == 0 ? 1 : refreshColor == null ? ""3"" : ""1"");
            path[0].setAttribute(""stroke"", refreshColor == null ? hitColor : refreshColor);
        }
        var poly = edge.getElementsByTagName(""polygon"");
        if (poly.length > 0)
        {   // for hitCount 0 set the stroke width to 1.
            poly[0].setAttribute(""stroke-width"", hitCount == 0 ? 1 : refreshColor == null ? ""3"" : ""1"");
            poly[0].setAttribute(""fill"", refreshColor == null ? hitColor : refreshColor);
            poly[0].setAttribute(""stroke"", refreshColor == null ? hitColor : refreshColor);
        }
        var text = edge.getElementsByTagName(""text"");
        if (text.length > 0)
        {
            if (hitCount > 0)
            {
                action += "" ("" + hitCount.toString() + "")"";
            }
            text[0].innerHTML = action;
        }
    }

    // Clear all the nodes.
    for (var i = 0; i < nodeCount; i++)
    {
        var node = document.getElementById(""node"" + i.toString());
        var ellipse = node.getElementsByTagName(""ellipse"");
        if (ellipse.length > 0)
        {
            ellipse[0].setAttribute(""fill"", ""#f7f5eb"");
        }
    }
}

function traversalStepBack() {
	stopTraversal();
    if (step - 1 < -1)
    {
        return;
    }

    if (step > -1)
    {
        transitionHitCounts[traversedEdge[step]]--;
        if (transitionHitCounts[traversedEdge[step]] < coverageFloor)
        {
        	coverageFloor = transitionHitCounts[traversedEdge[step]];
        	setTransitionFloorText();
        }
        refreshGraphics();
    }
    step--;
    traversalStepCommon();
}

function traversalStepForward() {
    if (step + 1 >= traversedEdge.length)
    {
        stopTraversal(); // in case the traversal was running.  Now we can use the back button.
        return;
    }
    step++;
    transitionHitCounts[traversedEdge[step]]++;
    refreshGraphics();
    assessCoverageFloor();
    traversalStepCommon();
}

window.addEventListener( 'keydown', (e) => { 
    var key = 0;


    if (e == null) { key = event.keyCode;}  
    else {  key = e.which;} 


    switch(key) {
        case 37: // left arrow                
          stopTraversal();
          traversalStepBack();
          break;
        case 38: // up arrow          
          break;
        case 39: // right arrow
        	stopTraversal();
        	traversalStepForward();                     
          break;
        case 40: // down arrow 
          break;
    }     
});

function assessCoverageFloor()
{
	var floor = 2000000000;
	for (var i=0; i < transitionHitCounts.length; i++)
	{
		if (transitionEnabledFlags[i] && transitionHitCounts[i] < floor)
		{
			floor = transitionHitCounts[i];
		}
	}
	if (floor != coverageFloor)
	{
		coverageFloor = floor;
		setTransitionFloorText();
	}
}

function traversalStepCommon() {
    setStepText();

    if (step == -1)
    {
        return; // no work to do because we are at the initial state.
    }

    document.getElementById(""selectedSvgElementInfo"").innerHTML = actionNames[transitionActions[traversedEdge[step]]];

    // Now paint the current path and nodes in light grey
    for (var i = 0; i < pathEdges[step].length; i++)
    {
        var svgEdge = pathEdges[step][i];
        var edge = document.getElementById(""edge"" + svgEdge.toString());
        var path = edge.getElementsByTagName(""path"");
        if (path.length > 0)
        {
            path[0].setAttribute(""stroke-width"", ""15"");
            path[0].setAttribute(""stroke"", ""lightgrey"");
        }
        var poly = edge.getElementsByTagName(""polygon"");
        if (poly.length > 0)
        {
            poly[0].setAttribute(""stroke-width"", ""15"");
            poly[0].setAttribute(""fill"", ""lightgrey"");
            poly[0].setAttribute(""stroke"", ""lightgrey"");
        }
    }
    for (var i = 0; i < pathNodes[step].length; i++)
    {
        var svgNode = pathNodes[step][i];
        var node = document.getElementById(""node"" + svgNode.toString());
        var ellipse = node.getElementsByTagName(""ellipse"");
        if (ellipse.length > 0)
        {
            ellipse[0].setAttribute(""fill"", ""lightgrey"");
        }
    }

    // Finally, paint the current transition
    svgEdge = traversedEdge[step];
    edge = document.getElementById(""edge"" + svgEdge.toString());
    var path = edge.getElementsByTagName(""path"");
    if (path.length > 0)
    {
        path[0].setAttribute(""stroke-width"", ""15"");
        path[0].setAttribute(""stroke"", ""url(#greenBlueGradient)"");
    }
    var poly = edge.getElementsByTagName(""polygon"");
    if (poly.length > 0)
    {
        poly[0].setAttribute(""stroke-width"", ""15"");
        poly[0].setAttribute(""fill"", ""url(#greenBlueGradient)"");
        poly[0].setAttribute(""stroke"", ""url(#greenBlueGradient)"");
    }

    if (startNode[step] == pathEndNode[step])
    {
    	var nodeNum = startNode[step];
    	var node = document.getElementById(""node"" + nodeNum.toString());

        var ellipse = node.getElementsByTagName(""ellipse"");
        if (ellipse.length > 0)
        {
            if (pathEdges[step].length == 1)
            {
                // This is a self loop with the path coming out of the 
                // start node, going into the end node, so make the 
                // node blue to green.
                ellipse[0].setAttribute(""fill"", ""url(#blueGreenGradient)"");
            }
            else
            {
                // This is probably the path coming into the end node,
                // so make the node green to blue.
                ellipse[0].setAttribute(""fill"", ""url(#greenBlueGradient)"");

            }
        }
    }
    else
    {
        var start = startNode[step];
        var node = document.getElementById(""node"" + start.toString());
        var ellipse = node.getElementsByTagName(""ellipse"");
        if (ellipse.length > 0)
        {
            ellipse[0].setAttribute(""fill"", ""yellowgreen"");
        }

        var pathEnd = pathEndNode[step];
        node = document.getElementById(""node"" + pathEnd.toString());
        ellipse = node.getElementsByTagName(""ellipse"");
        if (ellipse.length > 0)
        {
            ellipse[0].setAttribute(""fill"", ""lightskyblue"");
        }
    }
}

function getHitColor(hitCount) {
	var caseNum = document.getElementById(""recycleCbox"").checked ? hitCount%19 : hitCount;

    switch (caseNum)
    {
        case 0:
            if (hitCount != 0)
            {
                return ""#FF7F7F"";
            }
            return ""#000000"";
        case 1:
            return ""#00AFFF"";
        case 2:
            return ""#00DF00"";
        case 3:
            return ""#DF00DF"";
        case 4:
            return ""#FFAF00"";
        case 5:
            return ""#0000DF"";
        case 6:
            return ""#8F008F"";
        case 7:
            return ""#EFD700"";
        case 8:
            return ""#008F00"";
        case 9:
            return ""#3737AF"";
        case 10:
            return ""#BF4F4F"";
        case 11:
            return ""#77EF77"";
        case 12:
            return ""#3FAF3F"";
        case 13:
            return ""#A71770"";
        case 14:
            return ""#8FCF27"";
        case 15:
            return ""#4F7FEF"";
        case 16:
            return ""#771FAF"";
        case 17:
            return ""#97FF2F"";
        default:
            return ""#FF7F7F"";
    }
}
</script>
</body>
</html>");
                    w.Close();
                }
            }
        }

        public void CreateGraphVizFileAndImage(GraphShape shape = GraphShape.Default)
        {
            string layoutProgram = "dot";

            switch (shape)
            {
                case GraphShape.Circle:
                    layoutProgram = "circo";
                    break;
                default:
                    layoutProgram = "dot";
                    break;
                    // https://stackoverflow.com/questions/5343899/how-to-force-node-position-x-and-y-in-graphviz
            }

            // Create a GraphViz input file for the whole graph.
            using (FileStream fs = new FileStream(EzModelFileName + ".txt", FileMode.Create))
            using (StreamWriter w = new StreamWriter(fs, Encoding.ASCII))
            {
                // In case we have to create a new GraphViz file during a traversal..
                currentShape = shape;

                // preamble for the graphviz "dot format" output
                w.WriteLine("digraph state_machine {");
                w.WriteLine("size = \"13,7.5\";");
                w.WriteLine("node [shape = ellipse];");
                w.WriteLine("rankdir=LR;");


                // add the state nodes to the image
                for (int i = 0; i < totalNodes.Count(); i++)
                {
                    w.WriteLine("\"{0}\"\t[label=\"{1}\" fillcolor=\"#f7f5eb\", style=filled]", totalNodes.GetNodeByIndex((uint)i).state, totalNodes.GetNodeByIndex((uint)i).state.Replace(", ", "\\n"));
                }

                // Put a hitcount suffix on the action string so that GraphViz
                // lays out the action with enough space for a hitcount.
                // On the first render, replace the text on each transition with
                // just the action name, no hitcount.
                for (uint i = 0; i < transitions.Count(); i++)
                {
                    w.WriteLine("\"{0}\" -> \"{1}\" [ label=\"{2}\",color=black, penwidth=1 ];",
                        transitions.StartStateByTransitionIndex(i), transitions.EndStateByTransitionIndex(i), transitions.ActionByTransitionIndex(i) + " (0)");
                }

                w.WriteLine("}");
                w.Close();
            }

            string inputFile = EzModelFileName + ".txt";
            string outputFile = EzModelFileName + traversalCount + ".svg";

            Process dotProc = Process.Start(layoutProgram, inputFile + " -Tsvg -o " + outputFile);
            Console.WriteLine("Producing {0} from {1}", outputFile, inputFile);
            if (!dotProc.WaitForExit(60000))
            {
                Console.WriteLine("ERROR: Layout program {0} did not produce file {1} after 60 seconds of execution time.", layoutProgram, outputFile);
                dotProc.Kill(true);
            }
        }

        public void DisplayStateTable()
        {
            Console.WriteLine("Start state{0}End state{0}Action\n", transitions.transitionSeparator);

            for (uint i = 0; i < transitions.Count(); i++)
            {
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

                    if (startIndex < 0)
                    { continue; }

                    totalNodes.Visit((uint)startIndex);

                    int endIndex = GetNodeIndexByState(transitions.EndStateByTransitionIndex(tIndex));

                    if (endIndex < 0)
                    { continue; }

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

        public void RandomDestinationCoverage(string fname, int minimumCoverageFloor=2)
        {
            wallStartTime = DateTime.Now.Ticks;

        ResetPosition:
            InitializeSVGDeltas();

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

            int loopctr = 0;

            while (transitions.GetHitcountFloor() < minimumCoverageFloor)
            {
            // The unconditional increment of loopctr on the next line is correct
            // only because there is a transition added to the traversal at the
            // bottom of this loop.
                loopctr++;

                path = new Queue<int>();

            InitializeNewPath:
                // Prefer an outlink transition with a low hit count
                int targetIndex = transitions.GetLowHitTransitionIndexPreferOutlink(state);

                string targetStartState = transitions.StartStateByTransitionIndex((uint)targetIndex);

                if (state != targetStartState)
                {
                    path = FindShortestPath(state, targetStartState);

                    // Handle graphs that are not strongly connected.  In such a 
                    // graph, eventually a path of zero length is returned.  In
                    // the code above this, we see that it is the target transition
                    // that there is no path to.  So, we will remove that transition
                    // from the list of candidates - disable it - and ask for
                    // an alternative low hitcount transition.  If we get through
                    // the whole list of transitions and find no candidates, then
                    // the traversal stops with a note that there are no more
                    // traversal paths available in the graph, due to lack of strong
                    // connections.
                    if (path.Count == 0)
                    {
                        transitions.DisableTransition((uint)targetIndex);
                        Console.WriteLine("Disabled transition #{0} because there is no path to it from state {1}", targetIndex + 1, state);
                        goto InitializeNewPath;
                    }

                    foreach (int tIndex in path)
                    {
                        // mark the transitions covered along the way
                        transitions.IncrementHitCount((uint)tIndex);
                        loopctr++;

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
                                    Console.WriteLine("Stopping due to problem.  Achieved floor coverage of {0} before stop. Completed {1} iterations of traversal.", transitions.GetHitcountFloor(), loopctr);
                                    WriteSvgDeltasFile(String.Format("{0}StopOnProblem{1}", fname, ++problemCount));
                                    return;
                                }

                                // TODO:
                                // Provide a way for the user to override disabling transitions.
                                // Reason is that the problem might not be severe enough that
                                // disabling the transition is necessary to continue the traversal.

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
                                // Write an HTML file called Problem{problemCount}.html.  The
                                // traversal it shows will be all the steps up to the problem,
                                // so those are the steps to reproduce.  The dev can read the
                                // arrays of edges, etc, at the bottom of the file to work through
                                // the steps.  Hubba, hubba.
                                WriteSvgDeltasFile(String.Format("{0}Problem{1}", fname, ++problemCount));

                                // Re-write the graph file because transitions are disabled.
                                // *** Only write enabled transitions to the graph file!!
                                traversalCount++;
                                CreateGraphVizFileAndImage(currentShape);

                                // Go back to the start of this function, and reset the adapter.
                                goto ResetPosition;
                            }
                        }
                        AppendSvgDelta((uint)tIndex, targetIndex);
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

                AppendSvgDelta((uint)targetIndex, targetIndex);
            }
            // TODO: Trace floor coverage
            Console.WriteLine("Reached coverage floor of {0} in {1} iterations.", minimumCoverageFloor, loopctr);
            WriteSvgDeltasFile(String.Format("{0}RandomDestinationCoverage", fname));
            traversalCount++;

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
