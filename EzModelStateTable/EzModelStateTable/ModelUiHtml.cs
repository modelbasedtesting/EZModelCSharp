using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace SeriousQualityEzModel
{
    public partial class EzModelGraph
    {
        string[] legendColor = { "#000000", "#DF00DF", "#00AFFF", "#990099", "#00DF00", "#0000DF", "#EFD700", "#8F008F", "#77EF77", "#3737AF", "#BF4F4F", "#008F00", "#A71770", "#3FAF3F", "#4F7FEF", "#8FCF27", "#771FAF", "#97FF2F", "#FF7F7F" };

        void InitializeSVGDeltas()
        {
            traversedEdge = new List<uint>();
            pathEdges = new List<string>();
            pathNodes = new List<string>();
            startnode = new List<int>();
            endnode = new List<int>();
            pathEndNode = new List<int>();
        }

        void AppendSvgDelta(uint transitionIndex, int endOfPathTransitionIndex = -1)
        {
            int endOfPathNodeIndex = -1;

            if (endOfPathTransitionIndex > -1)
            {
                endOfPathNodeIndex = transitions.EndStateByTransitionIndex((uint)endOfPathTransitionIndex);
            }

            int startStateIndex = transitions.StartStateByTransitionIndex((uint)transitionIndex);
            int endStateIndex = transitions.EndStateByTransitionIndex((uint)transitionIndex);

            List<int> pathNodeIndices = new List<int>();

            // Get the set of nodes involved in the path, if any.
            foreach (uint tIndex in path)
            {
                int nodeIndex = transitions.StartStateByTransitionIndex(tIndex);
                if (!pathNodeIndices.Contains(nodeIndex))
                {
                    pathNodeIndices.Add(nodeIndex);
                }
                nodeIndex = transitions.EndStateByTransitionIndex(tIndex);
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
            List<int> tempPath = new();
            foreach (int i in path)
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
            endnode.Add(endStateIndex);
            pathEndNode.Add(endOfPathNodeIndex);
        }

        void WriteSvgDeltasFile(string fileName)
        {
            // TODO:
            // Add a histogram of transition hitcounts.
            // One bar for each transition.  Hitcount on the Y axis
            // 
            string[] ezModelGraph = File.ReadAllLines(EzModelFileName + traversalCount + ".svg");
            string rankDir = layoutDirection == LayoutRankDirection.LeftRight ? "LR" : "TD";
            using (FileStream fs = new FileStream(fileName + rankDir + traversalCount + ".html", FileMode.Create))
            {
                using (StreamWriter w = new(fs, Encoding.ASCII))
                {
                    // NOTE: <!DOCTYPE html> means HTML 5 to the browser.
                    w.WriteLine(
@"<!DOCTYPE html>
<!-- 
Serious Quality model view and activity playback

Doug Szabo doug.szabo@gmail.com

Copyright (c) 2021 Serious Quality, LLC

");
                    w.WriteLine("Random seed for this run was {0}", transitions.randomSeed);
                    w.WriteLine(
@"-->
<html>
	<head>
		<style>
input[type=""range""] {
	width:240px;
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
	<body onresize=""updateWindowDimensions()"">
		<script>
			var t0 = performance.now();
		</script>
    <table border=""0"" width=""100%"" height=""100%"">
		<tr>
        <td colspan=""2"">
            <table border=""0"" width=""100%"">
    		<tr>
			<!-- th id=""selectedSvgElementInfo"" style=""height:20px; color:#e00000"">INITIAL STATE</th -->
			<th id=""selectedSvgElementStartState"" style=""text-align:right;color:#e00000;width:40%""> </th>
			<th id=""selectedSvgElementTransition"" style=""text-align:center;color:#e00000;width:20% "">INITIAL STATE</th>
			<th id=""selectedSvgElementEndState"" style=""text-align:left;color:#e00000;width:40%""> </th>
            </tr>
            </table>
        </td>
		</tr>
		<tr>
			<td width=""50"">
				<table id=""legend"" border=""0"">");
                    for (var j = Math.Min(transitions.GetHitcountFloor(), 18); j > -1; j--)
                    {
                        w.WriteLine("<tr><td id=\"floor{0}\" style=\"text-align:right; width: 50%\">{0}</td><td style=\"width:15px; background-color:{1}\"></td></tr>", j, legendColor[j]);
                    }
                    w.WriteLine(
@"				</table>
			</td>
			<td id=""mainBox"">
<!-- Generated by graphviz -->
");
                    bool copyToStream = false;
                    double worldWidth = 0.0;

                    for (int i = 0; i < ezModelGraph.Length; i++)
                    {
                        if (copyToStream == true)
                        {
                            if (ezModelGraph[i].StartsWith("<!-- "))
                            {
                                continue;
                            }
                            if (ezModelGraph[i].StartsWith("<polygon fill=\"white\" "))
                            {
                                string polyString = ezModelGraph[i].Substring(ezModelGraph[i].IndexOf("points=") + 8);
                                polyString = polyString.Substring(0, polyString.Length - 3);
                                string[] polyPoints = polyString.Split(" ");
                                double x0 = double.Parse(polyPoints[0].Split(",")[0]);
                                double x2 = double.Parse(polyPoints[2].Split(",")[0]);
                                worldWidth = x2 - x0;
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
                            w.WriteLine(ezModelGraph[i]);
                        }
                        if (ezModelGraph[i].StartsWith("<svg width"))
                        {
                            w.WriteLine(
@"<svg id=""svgOuter"" onmousedown=""svgMouseDown(evt)"" onmouseup=""svgMouseUp(evt)"" onmouseleave=""svgMouseLeave(evt)"" onwheel=""svgMouseWheel(evt)"" style=""border: 1px solid #7f7f7f;"" ");
                            copyToStream = true;
                        }
                    }
                    w.WriteLine( // This block starts with a closing svg tag, matching an encapsulating svg tag way above.
@"
</td>
</tr>
<tr>
    <td colspan=""2"" height=""23px"">
        <table width=""100%"">
            <tr>
                <td style=""text-align:center; width: 25%""><label id=""transitionFloor""></label></td>
                <td style=""text-align:center; width: 18%""><button onclick=""fitGraph()"">FIT GRAPH</button></td>
                <td style=""text-align:center; width: 25%""><button onclick=""reset()"">RESET TRAVERSAL</button></td>
            </tr>
        </table>
    </td>
</tr>
<tr>
	<td colspan=""2"" height=""63px"">
		<table width=""100%"">
			<tr>
				<td></td>
				<td style=""width:150px"">
			    	<table border=""1"" color=""#7f7f7f"" rules=""none"">
			    		<tr>
			    			<td style=""padding-left: 5px; padding-right: 5px; text-align: center"">Graph Elements</td>
			    		</tr>
			    		<tr>
			    			<td style=""padding-left: 3px"">");
                    w.WriteLine("{0} nodes</td>", totalNodes.Count());
                    w.WriteLine(
@"			    		</tr>
			    		<tr>
							<td style=""padding-left: 3px"">");
                    w.WriteLine("{0} edges</td>", transitions.Count());
                    w.WriteLine(
@"						</tr>
						<tr>
						    <td style=""padding-left: 3px"">");
                    w.WriteLine("{0} actions</td>", actionsList.Length);
                    w.WriteLine(
@"						</tr>
					</table>
				</td>
				<td></td>
			    <td id=""transitionBox"" style=""width: 140px"">
			    	<table border=""1"" color=""#7f7f7f"" rules=""none"">
			    		<tr>
			    			<td style=""text-align: center; padding-top: 5px; height: 19px"" colspan=""3"">Transition</td>
			    		</tr>
			    		<tr>
			    			<td style=""padding-bottom: 7px; padding-left: 3px""><button onclick=""traversalStepBack()"">&lt;</button></td>
							<td id=""stepTD"" style=""padding-bottom: 8px; text-align:center""><label id=""step""></label></td>
						    <td style=""padding-bottom: 7px; padding-right: 3px; text-align: right""><button onclick=""traversalStepForward()"">&gt;</button></td>
						</tr>
					</table>
				</td>
				<td></td>
				<td style=""width: 264px"">
					<table border=""1"" color=""#7f7f7f"" rules=""none"">
						<tr>
                            <td style=""padding-top: 11px; padding-left: 7px; text-align: left; height: 19px"">1</td>
							<td style=""padding-top: 5px; text-align: center; height: 19px""><label id=""speedLabel"">Speed: 1</label></td>
                            <td style=""padding-top: 5px; text-align: center; height: 19px""><button id=""playPause"" onclick=""startTraversal()"">&#9654;</button></td>
                            <td id=""maxSpeed"" style=""padding-top: 11px; padding-right: 5px; text-align: right; height: 19px"">60</td>
						</tr>
			    		<tr>
							<td style=""padding-bottom: 3px; padding-top: 0px; padding-left: 2px; padding-right: 2px"" colspan=""4""><input type=""range"" min=""1"" max=""60"" oninput=""changeSpeed()"" value=""60"" id=""traversalSpeed""></td>
						</tr>
					</table>
				</td>
				<td></td>
		    </tr>
		</table>
	</td>
</tr>
<tr>
	<td style=""text-align: center; color: #00009f"" colspan=""2"">Graph layout by GraphViz 2.47.1.  The rest circa 2021 by Serious Quality LLC</td>
</tr>
</table>
<script type=""text/ecmascript"">

var step = -1; // Because step is an index into an array.
");
                    w.WriteLine("const actionNames = [\"{0}\"];", String.Join("\",\"", actionsList)); //[{0}];", transitions.ActionsToString());
                    w.WriteLine(" ");
                    w.WriteLine("const nodeNames = [{0}];", totalNodes.NodesToString());
                    w.WriteLine(" ");
                    w.WriteLine("const transitionActions = [{0}];", transitions.ActionIndicesToString());
                    w.WriteLine(" ");
                    w.WriteLine("const transitionEnabledFlags = {0};", transitions.EnabledFlagsToString());
                    w.WriteLine(" ");
                    w.WriteLine("const transitionHitCounts = new Array({0}).fill(0);", transitions.Count());
                    w.WriteLine(" ");
                    w.WriteLine("const nodeCount = {0};", totalNodes.Count());
                    w.WriteLine(" ");
                    w.WriteLine("const highestCoverageFloor = {0};", transitions.GetHitcountFloor());
                    w.WriteLine(" ");
                    w.WriteLine("const traversedEdge = [{0}];", String.Join(",", traversedEdge));
                    w.WriteLine(" ");
                    w.WriteLine("const pathEdges = [{0}];", String.Join(",", pathEdges));
                    w.WriteLine(" ");
                    w.WriteLine("const pathNodes = [{0}];", String.Join(",", pathNodes));
                    w.WriteLine(" ");
                    w.WriteLine("const startNode = [{0}];", String.Join(",", startnode));
                    w.WriteLine(" ");
                    w.WriteLine("const endNode = [{0}];", String.Join(",", endnode));
                    w.WriteLine(" ");
                    w.WriteLine("const pathEndNode = [{0}];", String.Join(",", pathEndNode));
                    w.WriteLine(" ");

                    //List<string> subgraphNodes = new List<string>();
                    //List<string> subgraphEdges = new List<string>();

                    //for (int i = 0; i < totalNodes.Count(); i++)
                    //{
                    //    string nodeState = totalNodes.GetStateByIndex((uint)i);

                    //    // Get the outlinks and inlinks of the node
                    //    List<uint> inOutSelf = transitions.GetStateTransitionIndices(nodeState, true);

                    //    List<int> nodeIds = new List<int>();
                    //    nodeIds.Add(i); // The node IDs in GraphViz are 1-based.

                    //    // First output the nodes related to the transitions.
                    //    List<string> stateList = new List<string>();

                    //    stateList.Add(nodeState);

                    //    foreach (uint tIndex in inOutSelf)
                    //    {
                    //        string s = transitions.StartStateByTransitionIndex(tIndex);
                    //        if (!stateList.Contains(s))
                    //        {
                    //            nodeIds.Add(totalNodes.GetIndexByState(s));
                    //            stateList.Add(s);
                    //        }
                    //        s = transitions.EndStateByTransitionIndex(tIndex);
                    //        if (!stateList.Contains(s))
                    //        {
                    //            nodeIds.Add(totalNodes.GetIndexByState(s));
                    //            stateList.Add(s);
                    //        }
                    //    }

                    //    subgraphNodes.Add("[" + string.Join(",", nodeIds) + "]");
                    //    subgraphEdges.Add("[" + String.Join(",", inOutSelf) + "]");
                    //}

                    //w.WriteLine("const subgraphNodes = [{0}];", String.Join(",", subgraphNodes));
                    //w.WriteLine(" ");
                    //w.WriteLine("const subgraphEdges = [{0}];", String.Join(",", subgraphEdges));
                    //w.WriteLine(" ");
                    w.WriteLine(
@"var c = 0;
var t;
var timer_is_on = 0;

var coverageFloor;

const vMargin = 190; 
const hMargin = 54;
const stepElemSize = 18*Math.floor(Math.log10(traversedEdge.length)) + 27;
var stepElem = document.getElementById(""stepTD"");
stepElem.setAttribute(""style"", ""text-align:center; width: "" + stepElemSize.toString() + ""px"");
document.getElementById(""transitionBox"").setAttribute(""style"", ""width: "" + (stepElemSize + 60) + ""px"");
var selectedSvgElement = undefined;
var previousBoxWidth = undefined;
var previousBoxHeight = undefined;
var mbrBox = ""Svg scale box"";
var mbrBits = [0.0,0.0,0.0,0.0];
var newBits = [1.0,2.0,3.0,4.0];
var translateScale = 1.0;
var mouseMovePreviousX = undefined;
var mouseMovePreviousY = undefined;
var didTranslate = false;
var svgRescale = 1.0;
var previousDeltaWasPositive = true;
var bOuterWidth = false;
var strokeScale = 0.0;
var numRenders = 1;
var renderAccum = performance.now() - t0;");
                    w.WriteLine("var worldWidth = {0};", worldWidth);
                    w.WriteLine(
@"var renderTimeout;

document.addEventListener(""load"", init(), false);

function init() {
	renderTimeout = renderAccum + 5;

	maxSpeed = Math.floor(1000.0 / renderTimeout);
    maxSpeed = maxSpeed > 100 ? 100 : maxSpeed;

    setStepText();
    assessCoverageFloor();
    mbrBox = document.getElementById(""svgOuter"").getAttribute(""viewBox"");
    var viewBoxBits = mbrBox.split("" "");
    mbrBits = [parseFloat(viewBoxBits[0]), parseFloat(viewBoxBits[1]),
    parseFloat(viewBoxBits[2]), parseFloat(viewBoxBits[3])];
    newBits = [mbrBits[0], mbrBits[1], mbrBits[2], mbrBits[3]];
    previousBoxWidth = window.innerWidth - hMargin;
    previousBoxHeight = window.innerHeight - vMargin;
    var boxAspect = previousBoxWidth / previousBoxHeight;
    var viewAspect = newBits[2]/newBits[3];
    if (boxAspect > viewAspect)
    {
        var delta = boxAspect*newBits[2]/viewAspect;
        newBits[0] -= 0.5*delta;
        newBits[2] += delta;
    }
    if (viewAspect > boxAspect)
    {
        var delta = viewAspect*newBits[3]/boxAspect;
        newBits[1] -= 0.5*delta;
        newBits[3] += delta;
    }
    strokeScale = Math.log10(newBits[2]*worldWidth/mbrBits[2]) - 1.0;
    updateViewBox(newBits[0], newBits[1], newBits[2], newBits[3]);
    updateWindowDimensions();
    updateSpeedControl();
    changeSpeed();
    translateScale = newBits[2] / (window.innerWidth - hMargin); 
    fitGraph();
}

for (var j=0; j < transitionActions.length; j++)
{
    var action = actionNames[transitionActions[j]];
    var text = document.getElementById(""edge"" + j.toString()).getElementsByTagName(""text"");
    if (text.length > 0)
    {
        text[0].innerHTML = action;
    }
}

function updateSpeedControl() {
    var tS = document.getElementById(""traversalSpeed"")
    if (maxSpeed == tS.max)
    {
        return;
    }

    if (tS.value > maxSpeed)
    {
    	tS.value = maxSpeed;
        changeSpeed();
    }
    tS.max = maxSpeed;
    document.getElementById(""maxSpeed"").innerHTML = maxSpeed;
}

function changeSpeed(e) {
    document.getElementById(""speedLabel"").innerHTML = ""Speed: "" + document.getElementById(""traversalSpeed"").value.toString();
}

function fitGraph() {
    var widthHeight = updateMainBoxDimensions();

    var boxAspect = widthHeight[0] / widthHeight[1];

    var mbrAspect = mbrBits[2]/mbrBits[3];

    var fitX = mbrBits[0];
    var fitY = mbrBits[1];
    var fitW = mbrBits[2];
    var fitH = mbrBits[3];

    if (boxAspect > mbrAspect)
    {
        var delta = boxAspect*mbrBits[2]/mbrAspect - mbrBits[2];
        fitX -= 0.5*delta;
        fitW += delta;
    }
    if (mbrAspect > boxAspect)
    {
        var delta = mbrAspect*mbrBits[3]/boxAspect - mbrBits[3];
        fitY -= 0.5*delta;
        fitH += delta;
    }
    newBits[0] = fitX;
    newBits[1] = fitY;
    newBits[2] = fitW;
    newBits[3] = fitH;

    svgRescale = 1.0;
    translateScale = newBits[2]/widthHeight[0];
    updateViewBox(fitX, fitY, fitW, fitH);
}

function edgeOpacityAndEvents(edgeId, opacity) {
    var edge = document.getElementById(edgeId);
    edge.setAttribute(""opacity"", opacity);
    var text = edge.getElementsByTagName(""text"");
    var path = edge.getElementsByTagName(""path"");
    var poly = edge.getElementsByTagName(""polygon"");
    if (opacity == ""1.0"")
    {
        edge.setAttribute(""opacity"", ""1.0"");
        edge.addEventListener(""click"", attr);
    }
    else
    {
        edge.setAttribute(""opacity"", opacity);
        edge.removeEventListener(""click"", attr);
    }
}

function nodeOpacityAndEvents(nodeId, opacity) {
    var node = document.getElementById(nodeId);
    node.setAttribute(""opacity"", opacity);
    var text = node.getElementsByTagName(""text"");
    var ellipse = node.getElementsByTagName(""ellipse"");
    if (opacity == ""1.0"")
    {
        node.setAttribute(""opacity"", ""1.0"");
        if (text.length > 0)
        {
            for (var i=0; i < text.length; i++)
            {
                text[i].addEventListener(""click"", attr);
            }
        }
        if (ellipse.length > 0)
        {
            ellipse[0].addEventListener(""click"", attr);
        }
    }
    else
    {
        node.setAttribute(""opacity"", opacity);
        if (text.length > 0)
        {
            for (var i=0; i < text.length; i++)
            {
                text[i].removeEventListener(""click"", attr);
            }
        }
        if (ellipse.length > 0)
        {
            ellipse[0].removeEventListener(""click"", attr);
        }
    }
}

function allComponentsToFullOpacity() {
    for (var i=0; i < transitionHitCounts.length; i++)
    {
        edgeOpacityAndEvents(""edge"" + i, ""1.0"");
    }
    for (var i=0; i < nodeCount; i++)
    {
        nodeOpacityAndEvents(""node"" + i, ""1.0"");
    }
}

function releaseSelection() {
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
        document.getElementById(""selectedSvgElementStartState"").innerHTML = "" "";
        document.getElementById(""selectedSvgElementTransition"").innerHTML = "" "";
        document.getElementById(""selectedSvgElementEndState"").innerHTML = "" "";
        
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
            event.stopPropagation();
            var nodenum = id.substring(4);
            var title = p.getElementsByTagName(""title"");
            if (title && title.length > 0)
            {
                msg = title[0].innerHTML;
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
            event.stopPropagation();
            var text = p.getElementsByTagName(""text"");
            if (text && text.length > 0)
            {
                msg = text[0].innerHTML;
                text[0].setAttribute(""fill"", ""red"");
            }
            break;
        default:
            break;
    }

    document.getElementById(""selectedSvgElementStartState"").innerHTML = "" "";
    document.getElementById(""selectedSvgElementTransition"").innerHTML = msg;
    document.getElementById(""selectedSvgElementEndState"").innerHTML = "" "";
}

var temp = svgOuter.getElementsByTagName(""g"");
if (temp.length > 0)
{
    for (var i=0; i < temp.length; i++)
    {
        temp[i].addEventListener(""click"", attr);
    }
}

function svgMouseDown(event) {
    var t = event.target;
    if (t.id != ""svgOuter"")
    {
        return;
    }

    switch (event.button)
    {
        case 0: 
            t.onmousemove = svgMouseMove;
            didTranslate = false;
            event.stopPropagation();
            break;
        case 1: 
            break;
        case 2: 
            break;
        default: 
            break;
    }
}

function svgMouseMove(event) {
	if (event.target.id == ""svgOuter"")
	{
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
	        var dx = translateScale * (mouseMovePreviousX - x);
	        var dy = translateScale * (mouseMovePreviousY - y);
	        mouseMovePreviousX = x;
	        mouseMovePreviousY = y;
	        translateViewBox(dx, dy);
	        event.stopPropagation();
	    }
	}
}

function svgMouseWheel(event) {
    didTranslate = true;
    zoomGraph(event.deltaY);
}

function zoomGraph(delta) {
    if (delta > 0)
    {
        if (previousDeltaWasPositive)
        {
            svgRescale *= 1.01;
        }
        else
        {
            svgRescale = 1.01;
        }
        previousDeltaWasPositive = true;
    }
    else
    {
        if (previousDeltaWasPositive)
        {
            svgRescale = 0.99;
        }
        else
        {
            svgRescale *= 0.99;
        }
        previousDeltaWasPositive = false;
    }

    newBox = rescaleViewBox( svgRescale );
}

function svgMouseLeave(event) {
	if (event.target.id == ""svgOuter"")
	{
	    svgMouseChange(event);
	}
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
    if (t.id == ""svgOuter"")
    {
	    t.onmousemove = null;
	}
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
        return;
    }

    xMin += 0.5 * (1.0 - scale) * width;
    yMin += 0.5 * (1.0 - scale) * height;
    width *= scale;
    height *= scale;

    var worldFactor = mbrBits[2] / worldWidth;
    var condition1 = scale > 1 && (width*worldFactor > 2*mbrBits[2] && height*worldFactor > 2*mbrBits[3]);
    var condition2 = scale <= 1 && (width < 200*worldFactor || height < 100*worldFactor);
    if (condition1 || condition2)
    {
        return viewBox;
    }

    newBits = [xMin, yMin, width, height];
    translateScale = newBits[2] / (window.innerWidth - hMargin);
    strokeScale = Math.log10(newBits[2]/worldFactor) - 1.0;
    var newViewBox = updateViewBox(xMin, yMin, width, height);
    return newViewBox;
}

function updateViewBox(xMin, yMin, width, height) {
    var xString = xMin.toString();
    var yString = yMin.toString();
    var wString = width.toString();
    var hString = height.toString();
    var newViewBox = xString + "" "" + yString + "" "" + wString + "" "" + hString;
    svgOuter.setAttribute(""viewBox"", newViewBox);
    return newViewBox;  
}

// https://stackoverflow.com/questions/10385768/how-do-you-resize-a-browser-window-so-that-the-inner-width-is-a-specific-value
function resizeViewPort(width, height) {
    var tmp = document.documentElement.style.overflow;
    document.documentElement.style.overflow = ""scroll"";

    if (window.outerWidth) {
        bOuterWidth = true;
        window.resizeTo(
            width + (window.outerWidth - window.innerWidth),
            height + (window.outerHeight - window.innerHeight)
        );
    } else {
        bOuterWidth = false;
        window.resizeTo(500, 500);
        window.resizeTo(
            width + (500 - window.innerWidth),
            height + (500 - window.innerHeight)
        );
    }

    document.documentElement.style.overflow = tmp;
}

function updateMainBoxDimensions() {
    var tmp = document.documentElement.style.overflow;
    document.documentElement.style.overflow = ""scroll"";
    var w = window.innerWidth;
    var h = window.innerHeight;
    document.documentElement.style.overflow = tmp;

    if ( w < 650 || h < 570 )
    {
        resizeViewPort(w < 650 ? 650 : w, h < 570 ? 570 : h);
        tmp = document.documentElement.style.overflow;
        document.documentElement.style.overflow = ""scroll"";
        w = window.innerWidth;
        h = window.innerHeight;
        document.documentElement.style.overflow = tmp;
    }

    var mainBox = document.getElementById(""mainBox"");

    var width = w - hMargin;
    var height = h - vMargin;
    var mbCR = mainBox.getBoundingClientRect();
    var legendCR = document.getElementById(""legend"").getBoundingClientRect();
    if (height < legendCR.height)
    {
        height = legendCR.height;
    }
    if (mbCR.width < 450)
    {
        width = 450;
    }
    return [width, height]; 
}

function updateWindowDimensions() {
    var widthHeight = updateMainBoxDimensions();

    var wScale = widthHeight[0] / previousBoxWidth;
    var hScale = widthHeight[1] / previousBoxHeight;
    previousBoxWidth = widthHeight[0];
    previousBoxHeight = widthHeight[1];
    var dViewW = (wScale - 1.0)*newBits[2];
    var dViewH = (hScale - 1.0)*newBits[3];
    newBits[0] -= 0.5*dViewW;
    newBits[1] -= 0.5*dViewH;
    newBits[2] += dViewW;
    newBits[3] += dViewH;
    updateViewBox(newBits[0], newBits[1], newBits[2], newBits[3]);
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
    newBits[0] = xMin;
    newBits[1] = yMin;
    svgOuter.setAttribute(""viewBox"", newViewBox);
}

function setTransitionFloorText() {
    document.getElementById(""transitionFloor"").innerHTML = ""Hitcount floor: "" + coverageFloor + ""/"" + highestCoverageFloor;
    for (var i=0; i <= highestCoverageFloor; i++)
    {
    	document.getElementById(""floor"" + i).style.backgroundColor = (i == coverageFloor) ? ""#a7ffa7"" : ""#ffffff"";
	}
}

function timedTraversal() {
    c = c + 1;
    traversalStepForward();

    var dt = Math.floor(1000.0 / document.getElementById(""traversalSpeed"").value - renderTimeout);
    if (dt < 2)
    {
        dt = 2;
    }
    if (timer_is_on)
    {
        t = setTimeout(timedTraversal, dt);
    }
}

function startTraversal() {
  var playPause = document.getElementById(""playPause"");
  playPause.removeEventListener(""click"", startTraversal);
  playPause.addEventListener(""click"", stopTraversal);
  playPause.innerHTML = ""&#9208"";
  if (!timer_is_on) {
    timer_is_on = 1;
    timedTraversal();
  }
}

function stopTraversal() {
  var playPause = document.getElementById(""playPause"");
  playPause.removeEventListener(""click"", stopTraversal);
  playPause.addEventListener(""click"", startTraversal);
  playPause.innerHTML = ""&#9654"";
  clearTimeout(t);
  timer_is_on = 0;
}

function reset() {
    stopTraversal();
    clearTimeout(t);

    document.getElementById(""selectedSvgElementStartState"").innerHTML = "" "";
    document.getElementById(""selectedSvgElementEndState"").innerHTML = "" "";
    document.getElementById(""selectedSvgElementTransition"").innerHTML = ""INITIAL STATE"";

    for (var i = 0; i < transitionHitCounts.length; i++)
    {
        transitionHitCounts[i] = 0;
    }
    refreshGraphics(""black"");

    var gItems = svgOuter.getElementsByTagName(""g"");
    if (gItems.length > 0)
    {
        var nodeEdge = [""node"", ""edge""];
        for (var i=0; i < gItems.length; i++)
        {
            if (nodeEdge.includes(gItems[i].id.substring(0,4)))
            {
                var text = gItems[i].getElementsByTagName(""text"");
                if (text.length > 0)
                {
                    for (var j=0; j < text.length; j++)
                    {
                        text[j].setAttribute(""fill"", ""black"");
                    }
                }
            }
        }
    }
    step = -1; 
    setStepText();
    assessCoverageFloor();
}

function setStepText() {
    document.getElementById(""step"").innerHTML = (step+1) + ""/"" + traversedEdge.length;
}

function refreshGraphics(refreshColor) {
    var rendered = new Array(transitionHitCounts.length).fill(false);
    var edgesToRender = transitionHitCounts.length;
    var strokeString = scaledStrokeWidthString(3); 

    if (step > -1 && step < traversedEdge.length)
    {
        for (var i=1; i < pathEdges[step].length; i++)
        {
            var index = pathEdges[step][i];
            var hitCount = transitionHitCounts[index];
            var hitColor = getHitColor(hitCount);
            var action = actionNames[transitionActions[index]];

            rendered[index] = true;
            edgesToRender--;

            var edge = document.getElementById(""edge"" + index.toString());
            var path = edge.getElementsByTagName(""path"");
            if (path.length > 0)
            {
                path[0].setAttribute(""stroke-width"", hitCount == 0 ? 1 : strokeString);
                path[0].setAttribute(""stroke"", hitColor);
            }
            var poly = edge.getElementsByTagName(""polygon"");
            if (poly.length > 0)
            {
                poly[0].setAttribute(""stroke-width"", hitCount == 0 ? 1 : strokeString);
                poly[0].setAttribute(""fill"", hitColor);
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
    }

    for (var i = step; i >= 0 && edgesToRender > 0; i--)
    {
        var edgeIndex = traversedEdge[i];
        if (rendered[edgeIndex])
        {
            continue;
        }

        edgesToRender--; 

        rendered[edgeIndex] = true;
        var hitCount = transitionHitCounts[edgeIndex];
        var hitColor = getHitColor(hitCount);
        var action = actionNames[transitionActions[edgeIndex]];
        var edge = document.getElementById(""edge"" + edgeIndex.toString());
        var path = edge.getElementsByTagName(""path"");
        if (path.length > 0)
        {   
            path[0].setAttribute(""stroke-width"", hitCount == 0 ? 1 : refreshColor == null ? strokeString : ""1"");
            path[0].setAttribute(""stroke"", refreshColor == null ? hitColor : refreshColor);
        }
        var poly = edge.getElementsByTagName(""polygon"");
        if (poly.length > 0)
        {   
            poly[0].setAttribute(""stroke-width"", hitCount == 0 ? 1 : refreshColor == null ? strokeString : ""1"");
            poly[0].setAttribute(""fill"", refreshColor == null ? hitColor : refreshColor);
        }
        var text = edge.getElementsByTagName(""text"");
        if (text.length > 0)
        {
            if (hitCount > 0)
            {
                action += "" ("" + hitCount.toString() + "")"";
            }
            text[0].innerHTML = action;
            if (refreshColor != null)
            {
                text[0].setAttribute(""fill"", refreshColor);
            }
        }
    }

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

        if (transitionEnabledFlags[traversedEdge[step]] && transitionHitCounts[traversedEdge[step]] < coverageFloor)
        {
            coverageFloor = transitionHitCounts[traversedEdge[step]];
            setTransitionFloorText();
        }

        refreshGraphics();
        var e = svgOuter.getElementById(""edge"" + traversedEdge[step]);
        var p = e.getElementsByTagName(""text"");
        if (p.length > 0)
        {
            p[0].setAttribute(""fill"", ""black"");
        }
        p = e.getElementsByTagName(""path"");
        if (p.length > 0)
        {
            p[0].setAttribute(""opacity"", 1.0);
        }
        p = document.getElementById(""node"" + endNode[step].toString());
        e = p.getElementsByTagName(""ellipse"");
        if (e.length > 0)
        {
            e[0].setAttribute(""fill"", ""#f7f5eb"");
            e[0].setAttribute(""opacity"", 1.0);
        }
    }

    step--;
    traversalStepCommon();
}

function traversalStepForward() {
    if (step + 1 >= traversedEdge.length)
    {
        stopTraversal(); 
        return;
    }

    t0 = performance.now()
    if (step > -1)
    {
        var e = svgOuter.getElementById(""edge"" + traversedEdge[step]);
        var p = e.getElementsByTagName(""text"");
        if (p.length > 0)
        {
            p[0].setAttribute(""fill"", ""black"");
        }
        p = e.getElementsByTagName(""path"");
        if (p.length > 0)
        {
            p[0].setAttribute(""opacity"", 1.0);
        }
        p = document.getElementById(""node"" + endNode[step].toString());
        e = p.getElementsByTagName(""ellipse"");
        if (e.length > 0)
        {
            e[0].setAttribute(""fill"", ""#f7f5eb"");
            e[0].setAttribute(""opacity"", 1.0);
        }
    }

    step++;
    transitionHitCounts[traversedEdge[step]]++;
    refreshGraphics();
    assessCoverageFloor();
    traversalStepCommon();
    var deltaT = 5 + performance.now() - t0;

    renderAccum += deltaT;
    numRenders++;

    if (numRenders > 4)
    {
        var sample = renderAccum / numRenders;
        if (Math.abs(sample - renderTimeout) > 5)
        {
            renderTimeout = sample;
            maxSpeed = Math.floor(1000.0 / renderTimeout);
            maxSpeed = maxSpeed > 100 ? 100 : maxSpeed;
            updateSpeedControl();
        }
        numRenders = 1;
        renderAccum = deltaT;
    }
    else
    {
        if (Math.abs(deltaT - renderTimeout) > 10)
        {
            maxSpeed += deltaT > renderTimeout ? -1 : 1;
            maxSpeed = maxSpeed > 100 ? 100 : maxSpeed;
            maxSpeed = maxSpeed < 2 ? 2 : maxSpeed;
            updateSpeedControl();
        }
    }
}

window.addEventListener( 'keydown', (e) => { 
	if (document.activeElement.id == ""traversalSpeed"")
	{
		return;
	}
    var key = 0;

    if (e == null) { key = event.keyCode;}  
    else {  key = e.which;} 

    switch(key) {
        case 37: 
            traversalStepBack();
            break;
        case 38: 
            zoomGraph(-1.0);
            break;
        case 39: 
            stopTraversal();
            traversalStepForward();                     
            break;
        case 40: 
            zoomGraph(1.0);
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

function scaledStrokeWidthString(baseWidth) {
    var scaledWidth = strokeScale > 0.1 ? Math.floor(strokeScale * baseWidth) : baseWidth;
    return scaledWidth.toString();
}

function traversalStepCommon() {
    setStepText();

    if (step == -1)
    {
        document.getElementById(""selectedSvgElementTransition"").innerHTML = ""INITIAL STATE"";
        document.getElementById(""selectedSvgElementStartState"").innerHTML = "" "";
        document.getElementById(""selectedSvgElementEndState"").innerHTML = "" "";
        return; 
    }

    document.getElementById(""selectedSvgElementTransition"").innerHTML = actionNames[transitionActions[traversedEdge[step]]];
    document.getElementById(""selectedSvgElementStartState"").innerHTML = nodeNames[startNode[step]];
    document.getElementById(""selectedSvgElementEndState"").innerHTML = nodeNames[endNode[step]];

    if (step == traversedEdge.length-1)
    {
        return;
    }

    var e = svgOuter.getElementById(""edge"" + traversedEdge[step]);
    var p = e.getElementsByTagName(""path"");
    if (p.length > 0)
    {
        p[0].setAttribute(""stroke-width"", parseFloat(p[0].getAttribute(""stroke-width""))*2.5);
        p[0].setAttribute(""opacity"", 0.4);
    }
    p = e.getElementsByTagName(""polygon"");
    if (p.length > 0)
    {
        p[0].setAttribute(""stroke-width"", parseFloat(p[0].getAttribute(""stroke-width""))*2.5);
    }
    p = e.getElementsByTagName(""text"");
    var hc = getHitColor(transitionHitCounts[traversedEdge[step]]);
    if (p.length > 0)
    {
        p[0].setAttribute(""fill"", hc);
    }

    p = document.getElementById(""node"" + endNode[step].toString());

    e = p.getElementsByTagName(""ellipse"");
    if (e.length > 0)
    {
        e[0].setAttribute(""fill"", hc);
        e[0].setAttribute(""opacity"", 0.4);
    }

    return;
}

function getHitColor(hitCount) {
    switch (hitCount)
    {");
                    int k = 0;
                    for (k = 0; k < Math.Min(transitions.GetHitcountFloor(), 18); k++)
                    {
                        w.WriteLine("\tcase {0}:", k);
                        w.WriteLine("\t\treturn \"{0}\";", legendColor[k]);
                    }
                    w.WriteLine("\tdefault:");
                    w.WriteLine("\t\treturn \"{0}\";", legendColor[k]);
                    w.WriteLine(
@"    }
}
</script>
</body>
</html>");
                    w.Close();
                }
            }
        }

        public enum LayoutRankDirection
        {
            TopDown, LeftRight
        }

        public LayoutRankDirection layoutDirection = LayoutRankDirection.LeftRight;

        public enum GraphShape
        {
            Circle, Default
        }

        // currentShape is relied on for EzModel internal calls
        // to CreateGraphVizFileAndImage().
        GraphShape currentShape = GraphShape.Default;

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
            using (FileStream fs = new(EzModelFileName + ".txt", FileMode.Create))
            using (StreamWriter w = new(fs, Encoding.ASCII))
            {
                // In case we have to create a new GraphViz file during a traversal..
                currentShape = shape;

                // preamble for the graphviz "dot format" output
                w.WriteLine("digraph state_machine {");
                w.WriteLine("size = \"13,7.5\";");
                w.WriteLine("node [shape = ellipse];");
                w.WriteLine("rankdir={0};", layoutDirection == LayoutRankDirection.TopDown ? "TD" : "LR");


                // add the state nodes to the image
                for (int i = 0; i < totalNodes.Count(); i++)
                {
                    string stateString = client.StringifyState(totalNodes.GetNodeByIndex((uint)i).state);
                    w.WriteLine("\"{0}\"\t[label=\"{1}\" fillcolor=\"#f7f5eb\", style=filled]", stateString, stateString.Replace(", ", "\\n"));
                }

                // Put a hitcount suffix on the action string so that GraphViz
                // lays out the action with enough space for a hitcount.
                // On the first render, replace the text on each transition with
                // just the action name, no hitcount.
                for (uint i = 0; i < transitions.Count(); i++)
                {
                    w.WriteLine("\"{0}\" -> \"{1}\" [ label=\"{2}\",color=black, penwidth=1 ];",
                        totalNodes.GetNodeByIndex((uint)transitions.StartStateByTransitionIndex(i)).stateString, totalNodes.GetNodeByIndex((uint)transitions.EndStateByTransitionIndex(i)).stateString, actionsList[transitions.ActionIndex((int)i)] + " (0)");
                }

                w.WriteLine("}");
                w.Close();
            }

            string inputFile = EzModelFileName + ".txt";
            string outputFile = EzModelFileName + traversalCount + ".svg";

            Process dotProc = Process.Start(layoutProgram, inputFile + " -Tsvg -o " + outputFile);
            Console.WriteLine("Producing {0} from {1}", outputFile, inputFile);
            if (!dotProc.WaitForExit(300000))
            {
                Console.WriteLine("ERROR: Layout program {0} did not produce file {1} after 300 seconds of execution time.", layoutProgram, outputFile);
                dotProc.Kill(true);
            }
        }

        public void DisplayStateTable()
        {
            Console.WriteLine("Start state{0}End state{0}Action\n", transitions.transitionSeparator);

            for (uint i = 0; i < transitions.Count(); i++)
            {
                Console.WriteLine(transitions.TransitionStringFromTransitionIndex(i, totalNodes, actionsList));
            }
            Console.WriteLine("  ");
        }
    } // partial EzModelGraph
}