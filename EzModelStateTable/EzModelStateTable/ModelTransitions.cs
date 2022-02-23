using System;
using System.Collections.Generic;

namespace SeriousQualityEzModel
{
    public struct StateTransition
    {
        public int startState;
        public int endState;
        public uint actionIndex;
        public int hitCount;
        public double probability;
        public bool enabled;

        public StateTransition(int startState, int endState, uint actionIndex)
        {
            this.startState = startState;
            this.endState = endState;
            this.actionIndex = actionIndex;
            hitCount = 0;
            probability = 1.0;
            enabled = true;
        }
    }

    public class StateTransitions
    {
        readonly StateTransition[] transitions;
        readonly int[] transitionActionProblemCount;

        // Keep track of the number of populated elements in the transitions
        // array.  This is necessary because C# arrays are fixed size, so
        // the array Length tells the capacity of the array, not the number
        // of populated elements in the array.
        uint transitionCount = 0;

        public readonly string transitionSeparator = " | ";

        public int randomSeed;
        readonly Random rnd;

        public StateTransitions(uint maximumTransitions, uint maximumActions, int randomSeed = 1)
        {
            transitions = new StateTransition[maximumTransitions];
            transitionActionProblemCount = new int[maximumActions];
            this.randomSeed = randomSeed;
            rnd = new Random(this.randomSeed);
        }

        public int ActionIndex(int index)
        {
            if (index > -1 && index < transitionCount)
            {
                return (int)transitions[(uint)index].actionIndex;
            }
            return -1;
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

        public string EnabledFlagsToString()
        {
            string flags = "[";

            for (uint i = 0; i < transitionCount; i++)
            {
                flags += (transitions[i].enabled ? "true" : "false") + (i == transitionCount - 1 ? "" : ",");
            }
            flags += "]";
            return flags;
        }

        public bool Add(int startState, int endState, int action)
        {
            if (transitionCount < transitions.Length)
            {
                transitions[transitionCount].startState = startState;
                transitions[transitionCount].endState = endState;
                transitions[transitionCount].enabled = true;
                transitions[transitionCount].actionIndex = (uint)action;
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

        public void DisableTransitionsByAction(int matchAction)
        {
            for (uint j = 0; j < transitionCount; j++)
            {
                if (transitions[j].actionIndex == matchAction)
                {
                    transitions[j].enabled = false;
                }
            }
            return;
        }

        public int EndStateByTransitionIndex(uint tIndex)
        {
            if (tIndex < transitionCount)
            {
                return transitions[tIndex].endState;
            }

            return -1;
        }

        public int GetAnyLowHitTransitionIndex(bool ignoreDisabledTransitions = false)
        {
            // Select a low hitcount transition randomly, without preference
            // for the proximity of the target transition to the current state.
            // This approach yields unpredictable traversal paths through
            // the graph, and is a way to cover the graph chaotically.

            // Find the lowest hit count
            int lowHit = GetHitcountFloor();

            // Create a list of all low-hit transitions
            List<int> lowHitList = new();
            for (int i = 0; i < transitionCount; i++)
            {
                if ((transitions[i].enabled || ignoreDisabledTransitions) && transitions[i].hitCount == lowHit)
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

        public int GetLowHitTransitionIndexAvoidOutlinks(int state, bool ignoreDisabledTransitions = false)
        {
            // Avoid an outlink transition to drive coverage away from
            // the current node.  If only outlinks have low hitcount,
            // then an outlink will be chosen.

            // Find the lowest hit count
            int lowHit = GetHitcountFloor();

            // Create a list of all low-hit transitions
            List<int> lowHitList = new();
            // Create a list of all low-hit outlink transitions
            List<int> lowHitNonOutlinkList = new List<int>();

            for (int i = 0; i < transitionCount; i++)
            {
                if ((transitions[i].enabled || ignoreDisabledTransitions) && transitions[i].hitCount == lowHit)
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

        public int GetLowHitTransitionIndexPreferOutlink(int state, bool ignoreDisabledTransitions = false)
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
            List<int> lowHitList = new();
            // Create a list of all low-hit outlink transitions
            List<int> lowHitOutlinkList = new();

            for (int i = 0; i < transitionCount; i++)
            {
                if ((transitions[i].enabled || ignoreDisabledTransitions) && transitions[i].hitCount == lowHit)
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

        public List<uint> GetStateTransitionIndices(int state, bool ignoreDisabledTransitions = false)
        {
            // return all transitions involving the state, i.e.,
            // outlinks, inlinks, and self-links.

            List<uint> indices = new();

            for (uint i = 0; i < transitionCount; i++)
            {
                if ((transitions[i].enabled || ignoreDisabledTransitions) && transitions[i].startState == state || transitions[i].endState == state)
                {
                    indices.Add(i);
                }
            }

            return indices;
        }

        public List<uint> GetOutlinkTransitionIndices(int state, bool ignoreDisabledTransitions = false)
        {
            List<uint> indices = new();

            for (uint i = 0; i < transitionCount; i++)
            {
                if ((transitions[i].enabled || ignoreDisabledTransitions) && transitions[i].startState == state)
                {
                    indices.Add(i);
                }
            }

            return indices;
        }

        public int GetTransitionIndexByStartAndEndStates(int startState, int endState, bool ignoreDisabledTransitions = false)
        {
            int lowHit = int.MaxValue;
            int lowHitIndex = -1;

            for (int i = 0; i < transitionCount; i++)
            {
                // There can be multiple arcs between start and end state.
                // Track the index of the transition with the lowest hitCount.
                // Return the tracked index to the caller, so that coverage is
                // increased.
                if ((transitions[i].enabled || ignoreDisabledTransitions) && transitions[i].startState == startState && transitions[i].endState == endState)
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
                transitionActionProblemCount[transitions[tIndex].actionIndex]++;
                return transitionActionProblemCount[transitions[tIndex].actionIndex];
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

        public int StartStateByTransitionIndex(uint tIndex)
        {
            if (tIndex < transitionCount)
            {
                return transitions[tIndex].startState;
            }

            return -1;
        }

        public int TransitionIndexOfEndState(int endState, bool ignoreDisabledTransitions = false)
        {
            for (int i = 0; i < transitionCount; i++)
            {
                if ((transitions[i].enabled || ignoreDisabledTransitions) && transitions[i].endState == endState)
                {
                    return i;
                }
            }

            Console.WriteLine("IndexOfEndState(): end state {0} not found in graph", endState);
            return -1;
        }

        public int TransitionIndexOfStartState(int startState, bool ignoreDisabledTransitions = false)
        {
            for (int i = 0; i < transitionCount; i++)
            {
                if ((transitions[i].enabled || ignoreDisabledTransitions) && transitions[i].startState == startState)
                {
                    return i;
                }
            }

            Console.WriteLine("IndexOfStartState(): start state {0} not found in graph", startState);
            return -1;
        }

        public string TransitionStringFromTransitionIndex(uint tIndex, Nodes nodes, string[] actionsList)
        {
            if (tIndex < transitionCount)
            {
                return String.Format("{0}{3}{1}{3}{2}", nodes.GetNodeByIndex((uint)transitions[tIndex].startState).stateString, nodes.GetNodeByIndex((uint)transitions[tIndex].endState).stateString, actionsList[transitions[tIndex].actionIndex], transitionSeparator);
            }
            return String.Empty;
        }
    } // StateTransitions
}

