using System;

namespace SeriousQualityEzModel
{
    public struct Node
    {
        public int state;
        public bool visited;
        public int parent;
        public string stateString;

        public Node(int initialState, IEzModelClient client)
        {
            this.state = initialState;
            this.visited = false;
            this.parent = -1;
            this.stateString = client.StringifyState(this.state);
        }
    }

    public class Nodes
    {
        private readonly Node[] nodes;
        uint count = 0;

        public Nodes(uint maximumNodes)
        {
            nodes = new Node[maximumNodes];
        }

        public bool Add(int state, IEzModelClient client)
        {
            if (count < nodes.Length)
            {
                if (count != state)
                {
                    Console.WriteLine("ERROR: state {0} != count {1}", state, count);
                }
                nodes[count].state = state;
                nodes[count].visited = false;
                nodes[count].stateString = client.StringifyState(state);
                nodes[count].parent = -1;
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

        public bool Contains(int state)
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

        public int GetIndexByState(int state)
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

        public int GetStateByIndex(uint index)
        {
            if (index < count)
            {
                return nodes[index].state;
            }

            return -1;
        }

        public string NodesToString()
        {
            string result = String.Empty;

            for (uint i = 0; i < count; i++)
            {
                // Might need to make the next ToString() prettier
                result += "\"" + nodes[i].stateString.Replace("\n", ", ") + "\"";
                if (i < count - 1)
                {
                    result += ",";
                }
            }
            return result;
        }

        public void SetParentByIndex(uint index, int parentState)
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
}

