// EZmodel Sample State Generation
// copyright 2019 Serious Quality LLC

#define BOOLEAN 

using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections.Generic;

namespace CreateGraphMLStateModel
{
    public class ActionAttribute : Attribute { }
    public class StateVarAttribute : Attribute { }

    public class StateNode
    {
        public const char nodeSeparator = ',';

        //++++++++++++++++++++++++++++++++++++++++++
        // C# state model behavior rules start here

        public enum BOOLEAN { False, True };

        // Set initial state variable values
        [StateVar]
        public BOOLEAN var1 = BOOLEAN.False;
        [StateVar]
        public BOOLEAN var2 = BOOLEAN.False;
        [StateVar]
        public BOOLEAN var3 = BOOLEAN.False;

        // Define behavior rules
        [Action]
        public void SetVar1True() { var1 = BOOLEAN.True; }
        public bool SetVar1TrueEnabled() { return (var1==BOOLEAN.False); }
        [Action]
        public void SetVar1False() { var1 = BOOLEAN.False; }
        public bool SetVar1FalseEnabled() { return (var1 == BOOLEAN.True); }



        [Action]
        public void SetVar2True() { var2 = BOOLEAN.True; }
        public bool SetVar2TrueEnabled() { return (var2 == BOOLEAN.False); }
        [Action]
        public void SetVar2False() { var2 = BOOLEAN.False; }
        public bool SetVar2FalseEnabled() { return (var2 == BOOLEAN.True); }

        [Action]
        public void SetVar3True() { var3 = BOOLEAN.True; }
        public bool SetVar3TrueEnabled() { return (var3 == BOOLEAN.False); }
        [Action]
        public void SetVar3False() { var3 = BOOLEAN.False; }
        public bool SetVar3FalseEnabled() { return (var3 == BOOLEAN.True); }


        // C# state behavior rules end here
        //++++++++++++++++++++++++++++++++++++++++++


        public StateNode node;
        public StateNode() { }

        public StateNode(StateNode n)
        {
            foreach (FieldInfo f in typeof(StateNode).GetFields(bf))
                if (Attribute.IsDefined(f, typeof(StateVarAttribute)))
                    f.SetValue(this, f.GetValue(n));
        }

        //  compare state variable values
        const BindingFlags bf = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        public static bool operator ==(StateNode lhs, StateNode rhs)
        {
            // compare the state variable fields in each object
            foreach (FieldInfo f in typeof(StateNode).GetFields(bf))
                if (Attribute.IsDefined(f, typeof(StateVarAttribute)))
                    if (!f.GetValue(lhs).Equals(f.GetValue(rhs)))
                        return false;
            return true;
        }

        // node label is the state variable values as a concatenated, delimiter-separated string
        public override string ToString()
        {
            string str = "";
            foreach (FieldInfo f in typeof(StateNode).GetFields(bf))
                if (Attribute.IsDefined(f, typeof(StateVarAttribute)))
                    str += f.GetValue(this).ToString() + nodeSeparator;

            return str.Remove(str.Length - 1);  // trim final separator
        }

        public List<MethodInfo> GetEnabledActions()
        {
            // this function returns a list of actions that are enabled in the current state
            List<MethodInfo> enabledActions = new List<MethodInfo>();
            foreach (MethodInfo m in (typeof(StateNode)).GetMethods())
                if (Attribute.IsDefined(m, typeof(ActionAttribute)))
                {
                    // check the "Enabled" method for each action
                    MethodInfo sm = typeof(StateNode).GetMethod(m.Name + "Enabled");
                    if ((bool)sm.Invoke(this, null))
                        enabledActions.Add(m);
                }
            return enabledActions;
        }

        // methods to keep the compiler happy
        public static bool operator !=(StateNode lhs, StateNode rhs) { return !(lhs == rhs); }
        public override bool Equals(object o) { return (this == (StateNode)o); }
        public override int GetHashCode() { return 0; }
    }

    public class Transition
    {
        public StateNode start;
        public string action;
        public StateNode end;

        public const string transitionSeparator = "\t";
        public Transition(StateNode startState, string action, StateNode endState)
        {
            start = startState;
            this.action = action;
            end = endState;
        }

        public override string ToString()
        {
            return start + transitionSeparator + action + transitionSeparator + end;
        }
    }

    public class Program
    {
        public static void Main()
        {
            GeneratedGraph graph = new GeneratedGraph();
            //graph.DisplayGraph();

            string folderName = "C:\\GraphMLout\\";
            string fileName = "EZmodel_HelloWorld";
            graph.CreateGraphMLFile(folderName + fileName);    // create the yED-formatted state graph
        }

        public class GeneratedGraph
        {
            List<Transition> transitions;
            List<StateNode> totalNodes;
            List<StateNode> unexploredNodes;

            public GeneratedGraph()
            {
                totalNodes = new List<StateNode>();
                unexploredNodes = new List<StateNode>();
                transitions = new List<Transition>();

                StateNode s = new StateNode();
                unexploredNodes.Add(s);
                totalNodes.Add(s);

                while (unexploredNodes.Count > 0)
                {
                    // generate all transitions out of state s
                    s = FetchUnexploredNode();
                    AddNewTransitionsToGraph(s);
                }
            }
            public StateNode FetchUnexploredNode()
            {
                StateNode s = unexploredNodes[0];
                unexploredNodes.RemoveAt(0);
                return s;
            }
            public void AddNewTransitionsToGraph(StateNode startState)
            {
                foreach (MethodInfo enabledAction in startState.GetEnabledActions())
                {
                    // an endstate is generated from current state + changes from an invoked action
                    StateNode endState = new StateNode(startState);

                    // execute action to change the endState state variables
                    enabledAction.Invoke(endState, null);

                    // if generated endstate is new, add  to the totalNode & unexploredNode lists
                    if (!totalNodes.Contains(endState))
                    {
                        totalNodes.Add(endState);
                        unexploredNodes.Add(endState);
                    }

                    // add this {startState, action, endState} transition to the Graph
                    transitions.Add(new Transition(startState, enabledAction.Name, endState));
                }
                return;
            }

            public void CreateGraphMLFile(string fname)
            {
                // Create a new file.
                using (FileStream fs = new FileStream(fname + ".graphml", FileMode.Create))
                using (StreamWriter w = new StreamWriter(fs, Encoding.ASCII))
                {
                    int fontSize = 25;

                    // preamble for the file
                    string preamble = GetPreamble();
                    w.WriteLine(preamble);
                    w.WriteLine();

                    // +++++++++++++++++++++++
                    // Add nodes to the Graph
                    // +++++++++++++++++++++++

                    // Calculate nodeHeight for all nodes by "(# state variables) * heightMultiplier"
                    int nodeHeight = GetNodeHeight(fontSize, totalNodes[0].ToString());

                    // Calculate node width for each node by "(length of longest state variable name) * widthMultiplier"
                    int idNum = 0;
                    foreach (StateNode s in totalNodes)
                    {
                        string nodeLabel = s.ToString();
                        string nodeLabelWithLineBreaks = nodeLabel.Replace(",", "\n");
                        int nodeWidth = GetNodeWidth(fontSize, nodeLabel);
                        string nodeFmt = GetNodeFmt();

                        // the node format string takes N parameters: {0} nodeLabel, {1} fontSize, {2} nodeWidth, {3} nodeHeight
                        w.WriteLine(nodeFmt, nodeLabel, nodeLabelWithLineBreaks, fontSize, nodeWidth, nodeHeight);
                        idNum++;
                        w.Flush();
                    }

                    // +++++++++++++++++++++++
                    // Add edges to the Graph
                    // +++++++++++++++++++++++

                    // the edge format string takes 4  parameters: id {0}, source {1}, target {2}, label {0}
                    string edgeFmt = GetEdgeFmt();

                    int edge_id = idNum + 1;
                    foreach (Transition t in transitions)
                    {
                        w.WriteLine(edgeFmt, edge_id, t.start.ToString(), t.end.ToString(), t.action, fontSize);
                        edge_id++;
                        w.Flush();
                    }

                    // postamble
                    w.WriteLine("</graph>\n</graphml>");
                    w.Flush();
                    w.Close();
                }
            }
            public int GetNodeHeight(int fontSize, string nodeLabel)
            {
                int heightMultiplier = (fontSize == 25) ? 36 : 48;
                string tmpLabel = totalNodes[0].ToString();      // # state variables = # of separators + 1
                int numSep = 0;
                foreach (char c in tmpLabel)
                    if (c == StateNode.nodeSeparator)
                        numSep++;
                return (heightMultiplier * (numSep + 1));
            }
            public int GetNodeWidth(int fontSize, string nodeLabel)
            {
                int max = 0;
                int len = 0;
                string tmpLabel = nodeLabel + ",";
                while (tmpLabel.Length > 0)
                {
                    len = tmpLabel.IndexOf(",");
                    if (len > max)
                        max = len;
                    tmpLabel = tmpLabel.Substring(len + 1);
                }

                int widthMultiplier = (fontSize == 25) ? 16 : 20;
                int widthPadding = 15;

                return (max * widthMultiplier) + widthPadding;
            }

            public string GetPreamble()
            {
                string preamble = @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""no""?>
<graphml xsi:schemaLocation=""http://graphml.graphdrawing.org/xmlns http://www.yworks.com/xml/schema/graphml.html/2.0/ygraphml.xsd "" xmlns=""http://graphml.graphdrawing.org/xmlns"" xmlns:demostyle=""http://www.yworks.com/yFilesHTML/demos/FlatDemoStyle/1.0"" xmlns:bpmn=""http://www.yworks.com/xml/yfiles-for-html/bpmn/2.0"" xmlns:demotablestyle=""http://www.yworks.com/yFilesHTML/demos/FlatDemoTableStyle/1.0"" xmlns:uml=""http://www.yworks.com/yFilesHTML/demos/UMLDemoStyle/1.0"" xmlns:compat=""http://www.yworks.com/xml/yfiles-compat-arrows/1.0"" xmlns:GraphvizNodeStyle=""http://www.yworks.com/yFilesHTML/graphviz-node-style/1.0"" xmlns:VuejsNodeStyle=""http://www.yworks.com/demos/yfiles-vuejs-node-style/1.0"" xmlns:y=""http://www.yworks.com/xml/yfiles-common/3.0"" xmlns:x=""http://www.yworks.com/xml/yfiles-common/markup/3.0"" xmlns:yjs=""http://www.yworks.com/xml/yfiles-for-html/2.0/xaml"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
	<key id=""d0"" for=""node"" attr.type=""boolean"" attr.name=""Expanded"" y:attr.uri=""http://www.yworks.com/xml/yfiles-common/2.0/folding/Expanded"">
		<default>true</default>
	</key>
	<key id=""d1"" for=""node"" attr.type=""string"" attr.name=""url""/>
	<key id=""d2"" for=""node"" attr.type=""string"" attr.name=""description""/>
	<key id=""d3"" for=""node"" attr.name=""NodeLabels"" y:attr.uri=""http://www.yworks.com/xml/yfiles-common/2.0/NodeLabels""/>
	<key id=""d4"" for=""node"" attr.name=""NodeGeometry"" y:attr.uri=""http://www.yworks.com/xml/yfiles-common/2.0/NodeGeometry""/>
	<key id=""d5"" for=""all"" attr.name=""UserTags"" y:attr.uri=""http://www.yworks.com/xml/yfiles-common/2.0/UserTags""/>
	<key id=""d6"" for=""node"" attr.name=""NodeStyle"" y:attr.uri=""http://www.yworks.com/xml/yfiles-common/2.0/NodeStyle""/>
	<key id=""d7"" for=""node"" attr.name=""NodeViewState"" y:attr.uri=""http://www.yworks.com/xml/yfiles-common/2.0/folding/1.1/NodeViewState""/>
	<key id=""d8"" for=""edge"" attr.type=""string"" attr.name=""url""/>
	<key id=""d9"" for=""edge"" attr.type=""string"" attr.name=""description""/>
	<key id=""d10"" for=""edge"" attr.name=""EdgeLabels"" y:attr.uri=""http://www.yworks.com/xml/yfiles-common/2.0/EdgeLabels""/>
	<key id=""d11"" for=""edge"" attr.name=""EdgeGeometry"" y:attr.uri=""http://www.yworks.com/xml/yfiles-common/2.0/EdgeGeometry""/>
	<key id=""d12"" for=""edge"" attr.name=""EdgeStyle"" y:attr.uri=""http://www.yworks.com/xml/yfiles-common/2.0/EdgeStyle""/>
	<key id=""d13"" for=""edge"" attr.name=""EdgeViewState"" y:attr.uri=""http://www.yworks.com/xml/yfiles-common/2.0/folding/1.1/EdgeViewState""/>
	<key id=""d14"" for=""port"" attr.name=""PortLabels"" y:attr.uri=""http://www.yworks.com/xml/yfiles-common/2.0/PortLabels""/>
	<key id=""d15"" for=""port"" attr.name=""PortLocationParameter"" y:attr.uri=""http://www.yworks.com/xml/yfiles-common/2.0/PortLocationParameter"">
		<default>
			<x:Static Member=""y:FreeNodePortLocationModel.NodeCenterAnchored""/>
		</default>
	</key>
	<key id=""d16"" for=""port"" attr.name=""PortStyle"" y:attr.uri=""http://www.yworks.com/xml/yfiles-common/2.0/PortStyle"">
		<default>
			<x:Static Member=""y:VoidPortStyle.Instance""/>
		</default>
	</key>
	<key id=""d17"" for=""port"" attr.name=""PortViewState"" y:attr.uri=""http://www.yworks.com/xml/yfiles-common/2.0/folding/1.1/PortViewState""/>
	<key id=""d18"" attr.name=""SharedData"" y:attr.uri=""http://www.yworks.com/xml/yfiles-common/2.0/SharedData""/>
	<data key=""d18"">
		<y:SharedData/>
	</data>
	<graph id=""G"" edgedefault =""directed"">
        <data key=""d5"">
             <y:Json >{ ""version"":""2.0.0"",""layout"":""layout-hierarchic""}</y:Json>
             </data>
     ";

                return (preamble);
            }
            public string GetNodeFmt()
            {
                string nodeFmt =
                @"
                <node id=""{0}"">
                    <data key=""d3"">
                        <x:List>
                            <y:Label>
                                <y:Label.Text><![CDATA[{1}]]></y:Label.Text>
                                    <y:Label.LayoutParameter>
                                        <y:RatioAnchoredLabelModelParameter LayoutOffset=""5,5""/>
                                    </y:Label.LayoutParameter>
                                <y:Label.Style>
                                    <yjs:DefaultLabelStyle horizontalTextAlignment=""LEFT"" autoFlip=""false"" textFill=""#FF000000"">
                                        <yjs:DefaultLabelStyle.font>
                                            <yjs:Font fontSize=""{2}"" fontFamily=""Lucida Console""/>
                                        </yjs:DefaultLabelStyle.font>
                                    </yjs:DefaultLabelStyle>
                                </y:Label.Style>
                            </y:Label>
                        </x:List>
                    </data>
                    <data key=""d4"">
                        <y:RectD X=""0"" Y=""0"" Width=""{3}"" Height=""{4}""/>
                    </data>
                    <data key=""d6"">
                        <yjs:ShapeNodeStyle fill=""#FFFFCC00"">
                            <yjs:ShapeNodeStyle.stroke>
                                <yjs:Stroke fill=""#FF000000"" miterLimit=""1.45""/>
                            </yjs:ShapeNodeStyle.stroke>
                        </yjs:ShapeNodeStyle>
                    </data>
                    <port name=""p0"">
                        <data key=""d15"">
                            <y:FreeNodePortLocationModelParameter Ratio=""1.0,0.5""/>
                        </data>
                    </port>
                </node>
";
                return nodeFmt;
            }
            public string GetEdgeFmt()
            {
                string edgeFmt =
                @"
                <edge id=""{0}"" source=""{1}"" target=""{2}"">
	                <data key=""d10"">
		                <x:List>
			                <y:Label>
				                <y:Label.Text><![CDATA[{3}]]></y:Label.Text>
				                <y:Label.Style>
					                <yjs:DefaultLabelStyle textFill=""BLUE"">
						                <yjs:DefaultLabelStyle.font>
							                <yjs:Font fontSize=""{4}""/>
						                </yjs:DefaultLabelStyle.font>
					                </yjs:DefaultLabelStyle>
				                </y:Label.Style>
			                </y:Label>
		                </x:List>
	                </data>
	                <data key=""d12"">
		                <yjs:PolylineEdgeStyle>
			                <yjs:PolylineEdgeStyle.stroke>
				                <yjs:Stroke fill=""BLACK"" thickness=""0.75""/>
			                </yjs:PolylineEdgeStyle.stroke>
			                <yjs:PolylineEdgeStyle.targetArrow>
				                <yjs:Arrow type=""TRIANGLE"" scale=""0.75"" stroke=""BLACK"" fill=""BLACK"" cropLength=""0""/>
			                </yjs:PolylineEdgeStyle.targetArrow>
		                </yjs:PolylineEdgeStyle>
	                </data>
                </edge>
                ";
                return edgeFmt;
            }

        }
    }
}
