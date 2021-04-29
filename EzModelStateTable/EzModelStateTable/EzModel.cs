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

        public List<uint> GetStateTransitionIndices(string state)
        {
            // return all transitions involving the state, i.e.,
            // outlinks, inlinks, and self-links.

            List<uint> indices = new List<uint>();

            for (uint i = 0; i < transitionCount; i++)
            {
                if (transitions[i].startState == state || transitions[i].endState == state)
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

        void EachSelfLoopOnce()
        {
            // for each action
            // collect transition indices that are self-loops
            // select one of the indices at random
            // drop all the other transitions
        }

        void InitializeSVGDeltas()
        {
            traversedEdge = new List<uint>();
            pathEdges = new List<string>();
            pathNodes = new List<string>();
            startnode = new List<int>();
            pathEndNode = new List<int>();
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
                    w.WriteLine(
@"<!DOCTYPE html>
<html>
	<head>
		<style>
.parent {
    width: 96vw;
    margin: 2vw 2vw;
    position: relative;
}
.child {
	width: 100%;
	height: 100%;
	position: absolute
	top: 0;
	left: 0;
	opacity: 0.7;
}
.child-2 {
	margin-top: 30px;
}
</style>
	</head>
	<body>
<div class=""parent"">
<!-- Generated by graphviz version 2.47.1(20210417.1919) -->
<!--Title: state_machine Pages: 1 -->
");
                    bool copyToStream = false;

                    for (var i=0; i < ezModelGraph.Length; i++)
                    {
                        if (copyToStream == true)
                        {
                            w.WriteLine(ezModelGraph[i]);
                        }
                        if (ezModelGraph[i].StartsWith("<svg width"))
                        {
                            w.WriteLine("<svg ");
                            copyToStream = true;
                        }
                    }

                    w.WriteLine(
@"<div class=""child"">
    <button onclick=""traversalStepForward()"">Forward</button>
    <button onclick=""traversalStepBack()"">Back</button>
    <button onclick=""startTraversal()"">Start</button>
    <input id=""step"" type=""text"" length=""10"" size=""10"" value=""0"" />
    <button onclick=""stopTraversal()"">Stop</button>
    <text id=""edge""> </text>
</div>
<div class=""child child-2"">
    <button onclick=""reset()"">RESET</button>
");
                    double wallTime = 1.0e-7 * (DateTime.Now.Ticks - wallStartTime);
                    w.WriteLine("<text id=\"wallTime\">Traversal wall time {0} seconds</text>", wallTime.ToString("F3", CultureInfo.CreateSpecificCulture("en-US")));
                    w.WriteLine(
@"</div>
</div>
<script>
var step = -1; // Because step is an index into an array.
");
                    w.WriteLine("const actionNames = [{0}];", transitions.ActionsToString());
                    w.WriteLine(" ");
                    w.WriteLine("const transitionActions = [{0}];", transitions.ActionIndicesToString());
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
                    w.WriteLine(
    @"var c = 0;
var t;
var timer_is_on = 0;
setStepText();

function timedTraversal() {
  c = c + 1;
  traversalStepForward();
  if (timer_is_on)
  {
	  t = setTimeout(timedTraversal, 9);
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

    document.getElementById(""edge"").innerHTML = "" "";
    for (var i = 0; i < transitionHitCounts.length; i++)
    {
        transitionHitCounts[i] = 0;
    }
    refreshGraphics(""black"");

    step = -1; // Because step is an index into an array.
    setStepText();
}

function setStepText() {
	document.getElementById(""step"").value = (step+1) + ""/"" + traversedEdge.length;
}

function refreshGraphics(refreshColor) {
	var rendered = new Array(transitionHitCounts.length).fill(false);
	// Walk backwards from the current step and set a 
	// rendered flag on each transition as it is encountered.
	// Do not render a transition that has rendered true.
    for (var i = step; i >= 0; i--)
    {
    	var edgeIndex = traversedEdge[i];
    	if (rendered[edgeIndex])
    	{
    		continue;
    	}

    	rendered[edgeIndex] = true;
        var hitCount = transitionHitCounts[edgeIndex];
        var hitColor = getHitColor(hitCount);
        var action = actionNames[transitionActions[edgeIndex]];
        var svgEdge = traversedEdge[i] + 1;
        var edge = document.getElementById(""edge"" + svgEdge.toString());
        var path = edge.getElementsByTagName(""path"");
        if (path.length > 0)
        {
            path[0].setAttribute(""stroke-width"", refreshColor == null ? ""3"" : ""1"");
            path[0].setAttribute(""stroke"", refreshColor == null ? hitColor : refreshColor);
        }
        var poly = edge.getElementsByTagName(""polygon"");
        if (poly.length > 0)
        {
            poly[0].setAttribute(""stroke-width"", refreshColor == null ? ""3"" : ""1"");
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
    for (var i = 1; i <= nodeCount; i++)
    {
        var node = document.getElementById(""node"" + i.toString());
        var ellipse = node.getElementsByTagName(""ellipse"");
        if (ellipse.length > 0)
        {
            ellipse[0].setAttribute(""fill"", ""none"");
        }
    }
}

function traversalStepBack()
{
    if (step - 1 < -1)
    {
        return;
    }

    if (step > -1)
    {
        transitionHitCounts[traversedEdge[step]]--;
        refreshGraphics();
    }
    step--;
    traversalStepCommon();
}

function traversalStepForward()
{
    if (step + 1 >= traversedEdge.length)
    {
        // TODO:
        // Set a guard flag so that if the user continues
        // tapping back or forward into this body, we actually
        // avoid re-executing the code blocks above here.
        // Those code blocks only need to run once when the
        // step is taken to zero or max steps.
        stopTraversal(); // in case the traversal was running.  Now we can use the back button.
        return;
    }
    // Refresh the graphics:
    // set all path and polygon strokes and stroke-width values to 2, and stroke colors to black.
    // set the fill to none on all graph nodes.
    refreshGraphics();
    step++;
    transitionHitCounts[traversedEdge[step]]++;
    traversalStepCommon();
}


function traversalStepCommon()
{
    setStepText();

    if (step == -1)
    {
        return; // no work to do because we are at the initial state.
    }

    // Now paint the current path and nodes in light grey
    for (var i = 0; i < pathEdges[step].length; i++)
    {
        var svgEdge = pathEdges[step][i] + 1;
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
        var svgNode = pathNodes[step][i] + 1;
        var node = document.getElementById(""node"" + svgNode.toString());
        var ellipse = node.getElementsByTagName(""ellipse"");
        if (ellipse.length > 0)
        {
            ellipse[0].setAttribute(""fill"", ""lightgrey"");
        }
    }

    // Finally, paint the current transition in fat red, 
    // paint the start node of the path in green, and 
    // paint the end node of the path in cyan.   
    // NOTE: Doing a gradient in svg is very difficult 
    // because of the calculations.  Look at that later, 
    // for when the start and end node of the path are
    // the same. 
    document.getElementById(""edge"").innerHTML = actionNames[transitionActions[traversedEdge[step]]];
    svgEdge = traversedEdge[step] + 1;
    edge = document.getElementById(""edge"" + svgEdge.toString());
    var path = edge.getElementsByTagName(""path"");
    if (path.length > 0)
    {
        path[0].setAttribute(""stroke-width"", ""15"");
        var hitCount = transitionHitCounts[traversedEdge[step]];
        var color = getHitColor(hitCount);
        path[0].setAttribute(""stroke"", ""red"");
    }
    var poly = edge.getElementsByTagName(""polygon"");
    if (poly.length > 0)
    {
        poly[0].setAttribute(""stroke-width"", ""15"");
        poly[0].setAttribute(""fill"", ""red"");
        poly[0].setAttribute(""stroke"", ""red"");
    }

    var start = startNode[step] + 1;
    var node = document.getElementById(""node"" + start.toString());
    var ellipse = node.getElementsByTagName(""ellipse"");
    if (ellipse.length > 0)
    {
        ellipse[0].setAttribute(""fill"", ""yellowgreen"");
    }

    var pathEnd = pathEndNode[step] + 1;
    node = document.getElementById(""node"" + pathEnd.toString());
    ellipse = node.getElementsByTagName(""ellipse"");
    if (ellipse.length > 0)
    {
        ellipse[0].setAttribute(""fill"", ""lightskyblue"");
    }

    // write all transitions with their hitcounts, use color function.
    // write all nodes in clear fill.
    // write all path nodes in light grey fill.
    // write start node in green, end of path node in cyan.
    // overdraw path edges, except the edge of the latest transition - make that fat.
}


function getHitColor(hitCount)
{
    switch (hitCount)
    {
        case 0:
            return ""#000000"";
        case 1:
            return ""#DF0000"";
        case 2:
            return ""#00DF00"";
        case 3:
            return ""#0000DF"";
        case 4:
            return ""#9F9F00"";
        case 5:
            return ""#009F9F"";
        case 6:
            return ""#9F009F"";
        case 7:
            return ""#2F7F7F"";
        case 8:
            return ""#7F2F7F"";
        case 9:
            return ""#7F7F2F"";
        case 10:
            return ""#3F3FAF"";
        case 11:
            return ""#AF3F3F"";
        case 12:
            return ""#3FAF3F"";
        case 13:
            return ""#AF1F77"";
        case 14:
            return ""#1FAF77"";
        case 15:
            return ""#1F77AF"";
        case 16:
            return ""#771FAF"";
        case 17:
            return ""#77AF1F"";
        default:
            return ""#FF1F1F"";
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

            // Create a new file.
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
                    string pos = "";
                    w.WriteLine("\"{0}\"\t[label=\"{1}\" {2}]", totalNodes.GetNodeByIndex((uint)i).state, totalNodes.GetNodeByIndex((uint)i).state.Replace(",", "\\n"), pos);
                }

                // Color each link by its hit count
                for (uint i = 0; i < transitions.Count(); i++)
                {
                    w.WriteLine("\"{0}\" -> \"{1}\" [ label=\"{2}\",color=black, penwidth=2 ];",
                        transitions.StartStateByTransitionIndex(i), transitions.EndStateByTransitionIndex(i), transitions.ActionByTransitionIndex(i));
                }

                w.WriteLine("}");
                w.Close();
            }

            Process dotProc = Process.Start(layoutProgram, EzModelFileName + ".txt -Tsvg -o " + EzModelFileName + traversalCount + ".svg");
            dotProc.WaitForExit();
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
            int minimumCoverageFloor = 2;

            while (transitions.GetHitcountFloor() < minimumCoverageFloor)
            {
            // The unconditional increment of loopctr on the next line is correct
            // only because there is a transition added to the traversal at the
            // bottom of this loop.
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
