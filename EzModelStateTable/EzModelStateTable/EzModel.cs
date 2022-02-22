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

namespace SeriousQualityEzModel
{
    public partial class EzModelGraph
    {
        readonly StateTransitions transitions;
        readonly Nodes totalNodes;
        List<int> unexploredStates;

        Queue<int> path = new();

        // These next 5 collections are uninitialized.  A traversal routine
        // must call InitializeSVGDeltas before the first call to
        // AppendSVGDeltas.  That ensures the collections are empty when the
        // traversal begins, or when it resets.
        List<uint> traversedEdge;
        List<string> pathEdges;
        List<string> pathNodes;
        List<int> startnode;
        List<int> endnode;
        List<int> pathEndNode;
        string[] actionsList;

        const string EzModelFileName = "EzModelDigraph";
        uint problemCount = 0;
        uint traversalCount = 0;
        readonly IEzModelClient client;

        public EzModelGraph(IEzModelClient theEzModelClient, uint maxTransitions = 1000, uint maxNodes = 20, uint maxActions = 20, LayoutRankDirection layoutRankDirection = LayoutRankDirection.LeftRight, int randomSeed = 1)
        {
            client = theEzModelClient;

            transitions = new StateTransitions(maxTransitions, maxActions, randomSeed);

            totalNodes = new Nodes(maxNodes);

            this.layoutDirection = layoutRankDirection;
        }

        int FetchUnexploredState()
        {
            int state = unexploredStates[0];
            unexploredStates.RemoveAt(0);
            return state;
        }

        public bool GenerateGraph()
        {
            unexploredStates = new List<int>();

            int state = client.GetInitialState();
            unexploredStates.Add(state); // Adding to the <List> instance
            totalNodes.Add(state, client); // Adding to the Nodes class instance

            List<TempTransition> tempTransitions = new();

            while (unexploredStates.Count > 0)
            {
                // generate all transitions out of state s
                state = FetchUnexploredState();
                List<int> Actions = client.GetAvailableActions(state);

                foreach (int action in Actions)
                {
                    // an endstate is generated from current state + changes from an invoked action
                    int endState = client.GetEndState(state, action);

                    // if generated endstate is new, add  to the totalNode & unexploredNode lists
                    if (!totalNodes.Contains(endState))
                    {
                        if (!totalNodes.Add(endState, client)) // try to Adds the Node to Nodes class instance
                        {
                            Console.WriteLine("Not enough nodes: choose a larger maximumNodes argument in the call to GeneratedGraph()");
                            return false;
                        }

                        unexploredStates.Add(endState); // Adds a string to List instance
                    }

                    tempTransitions.Add(new TempTransition(state, endState, action));
                }
            }

            actionsList = client.GetActionsList();

            switch (client.SelfLinkTreatment)
            {
                case SelfLinkTreatmentChoice.SkipAll:
                    foreach (TempTransition t in tempTransitions)
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
                    for (int i = tempTransitions.Count - 1; i >= 0; i--)
                    {
                        if (tempTransitions[i].startState == tempTransitions[i].endState)
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
                        int match = tempTransitions[0].action;
                        List<int> indices = new();
                        for (int i = 0; i < tempTransitions.Count; i++)
                        {
                            if (tempTransitions[i].action == match)
                            {
                                indices.Add(i);
                            }
                        }

                        int index = rnd.Next(indices.Count);

                        TempTransition t = tempTransitions[indices[index]];

                        if (!transitions.Add(t.startState, t.endState, t.action))
                        {
                            Console.WriteLine("Possibly not enough transitions: choose a larger maximumTransitions argument in the call to GeneratedGraph()");
                            return false;
                        }

                        for (int i = indices.Count - 1; i >= 0; i--)
                        {
                            tempTransitions.RemoveAt(indices[i]);
                        }
                    }
                    break;
                case SelfLinkTreatmentChoice.AllowAll:
                    foreach (TempTransition t in tempTransitions)
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
            List<string> report = new();

            int startState = 0;

            for (int i = 1; i < totalNodes.Count(); i++)
            {
                int endState = i;
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
                int sIndex = transitions.StartStateByTransitionIndex(i);
                int eIndex = transitions.EndStateByTransitionIndex(i);
                bool startExists = sIndex > -1;
                bool endExists = eIndex > -1;

                if (!startExists && !endExists)
                {
                    report.Add(String.Format("The start and end states do not exist for the transition {0} -> {1} : {2}", sIndex, eIndex, transitions.ActionIndex((int)i)));
                }
                else if (!startExists || !endExists)
                {
                    report.Add(String.Format("The {0}{1} state does not exist for the transition {2} -> {3} : {4}", startExists ? "" : "start", endExists ? "" : "end", sIndex, eIndex, transitions.ActionIndex((int)i)));
                }
            }

            return report;
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

            List<string> duplicates = new();

            for (uint i = 0; i < totalNodes.Count(); i++)
            {
                int state = totalNodes.GetStateByIndex(i);
                List<int> actions = new();
                List<int> duplicateActions = new();
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
                    int action = transitions.ActionIndex((int)outs[(int)j]);

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
                    int action = transitions.ActionIndex((int)outs[(int)j]);

                    if (duplicateActions.Contains(action))
                    {
                        duplicates.Add(transitions.TransitionStringFromTransitionIndex(outs[(int)j], totalNodes, actionsList));
                    }
                }
            }

            return duplicates;
        }

        readonly Random rnd = new(DateTime.Now.Millisecond);

        struct TempTransition
        {
            public int startState;
            public int endState;
            public int action;

            public TempTransition(int startState, int endState, int action)
            {
                this.startState = startState;
                this.endState = endState;
                this.action = action;
            }
        };

        Queue<int> FindShortestPath(int startState, int endState)
        {
            Queue<Node> queue = new();
            Stack<Node> route = new();
            Queue<int> path = new();

            totalNodes.ClearAllVisits();

            int targetIndex = endState;

            queue.Enqueue(totalNodes.GetNodeByIndex((uint)startState));

            while (queue.Count > 0)
            {
                Node currentNode = queue.Dequeue();

                foreach (uint tIndex in transitions.GetOutlinkTransitionIndices(currentNode.state))
                {
                    int parentState = transitions.StartStateByTransitionIndex(tIndex);
                    int startIndex = parentState;

                    if (startIndex < 0)
                    { continue; }

                    totalNodes.Visit((uint)startIndex);

                    int endIndex = transitions.EndStateByTransitionIndex(tIndex);

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
                                tmp = totalNodes.GetNodeByIndex((uint)(tmp.parent));

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

        public void RandomDestinationCoverage(string fname, int minimumCoverageFloor = 2)
        {

        ResetPosition:
            using (FileStream fs = new(String.Format("Traversal_RandomDestinationCoverage-{0}.txt", minimumCoverageFloor), FileMode.Create))
            {
                using (StreamWriter w = new(fs, Encoding.ASCII))
                {
                    InitializeSVGDeltas();

                    // Each transition will now be a target to be reached
                    //  1. find a transition with a low hit count
                    //  2. move along a path from where you are to the start node of that transition
                    //  3. move along the target transition (so now you shd be in that transition's end node)

                    // InitialState is needed in case rules.ReportProblem() is called.
                    int initialState = client.GetInitialState();
                    w.WriteLine(totalNodes.GetNodeByIndex((uint)initialState).stateString);

                    if (client.NotifyAdapter)
                    {
                        // TODO: for Abstract model, client must set its own popcorn trail of details
                        // that aligns with the popcorn trail here, which is about the model.
                        client.SetStateOfSystemUnderTest(initialState);
                    }

                    // State is the start state of the current transition.
                    int state = initialState;

                    // Record the actions taken in case rules.ReportProblem() is called.
                    // This list is built up only when the rules module has NotifyAdapter == true.
                    List<int> popcornTrail = new();

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

                        int targetStartState = transitions.StartStateByTransitionIndex((uint)targetIndex);

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
                                    int action = transitions.ActionIndex(tIndex);
                                    popcornTrail.Add(action);
                                    // TODO: the following console output relates to the AdapterGetEndState method in the Monopoly client.
                                    Console.WriteLine(" "); //  ("During path traversal");
                                    int reportedEndState = client.AdapterTransition(transitions.StartStateByTransitionIndex((uint)tIndex), action);
                                    int predicted = transitions.EndStateByTransitionIndex((uint)tIndex);
                                    w.WriteLine("{0} | {1}", actionsList[action], totalNodes.GetNodeByIndex((uint)predicted).stateString);

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

                                            // TODO: add a display value to show the disabled transition
                                            transitions.DisableTransition((uint)tIndex);
                                        }
                                        else
                                        {
                                            // TODO: add a display value to show the disabled transition

                                            // On second or later fault on the same action, disable the action everywhere.
                                            transitions.DisableTransitionsByAction(transitions.ActionIndex(tIndex));
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
                            int action = transitions.ActionIndex(targetIndex);
                            popcornTrail.Add(action);
                            // TODO: the following console output relates to the AdapterGetEndState method in the Monopoly client.
                            Console.WriteLine(" "); //  ("After Path traversal");
                            int reportedEndState = client.AdapterTransition(targetStartState, action);
                            int predicted = transitions.EndStateByTransitionIndex((uint)targetIndex);
                            w.WriteLine("{0} | {1}", actionsList[action], totalNodes.GetNodeByIndex((uint)predicted).stateString);

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
                                    transitions.DisableTransitionsByAction(transitions.ActionIndex(targetIndex));
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
                    w.Close();
                } // using
            } // using
        }
    } // partial EzModelGraph
} // EzModelStateTable namespace
