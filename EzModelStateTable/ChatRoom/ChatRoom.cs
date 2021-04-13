using System;
using System.Collections.Generic;
using SeriousQualityEzModel;

namespace ChatRoomExample
{
    class ChatRoomProgram
    {
        static void Main()
        {
            ChatRoom rules = new ChatRoom();

            GeneratedGraph graph = new GeneratedGraph(rules, 1000, 100);

            // write graph to dot format file
            string fname = "ChatRoom";
            string suffix = "0000";
            string fullName = fname + suffix;
            graph.CreateGraphVizFileAndImage(fname, suffix, "Initial State");

            // Cover the model with Greedy Postman
            graph.GreedyPostman(fname);

            graph.DisplayStateTable(); // Display the Excel-format state table
            Console.ReadLine();
        }
    }

    public class ChatRoom : IUserRules
    {
        // State Variables
        // Angela
        const string svAngelaStatus = "AStatus";
        const string svAngelaInvitation = "AInvitation";
        const string svAngelaRoom = "ARoom";
        // Jacob
        const string svJacobStatus = "JStatus";
        const string svJacobInvitation = "JInvitation";
        const string svJacobRoom = "JRoom";

        // State values
        // For *Status variables
        const string LoggedOut = ".loggedOut"; 
        const string LoggedIn = ".loggedIn";
        // For *Room variables
        const string notCreated = ".notCreated";
        const string created = ".created";
        const string angelaPresent = ".aPresent";
        const string jacobPresent = ".jPresent";
        // For *Invitation variables; a person will not invite themself.
        const string noneInvited = ".noneInvited";
        const string aInvited = ".aInvited";
        const string jInvited = ".jInvited";
        // IUserRules.valueSeparator abbreviated constant name for syntax convenience
        const string sep = IUserRules.valueSeparator;

        // Keep a vector of state variable values, one state variable represented by
        // each vector element.  Some state variables can have multiple simultaneous
        // values, for example ARoom has possibilities of empty, angelaPresent,
        // jacobPresent, or angelaPresent AND jacobPresent.
        // When responding to GetInitialState and GetEndState, return 
        // a valueSeparator-delimited string from the populated array elements.
        // The vector indices represent the state value of the state variables,
        // in order of Angela then Jacob.  See State Variables declarations, above.
        string[] vState = new string[6];
        const uint iAngelaStatus = 0;
        const uint iAngelaRoom = 1;
        const uint iAngelaInvitation = 2;
        const uint iJacobStatus = 3;
        const uint iJacobRoom = 4;
        const uint iJacobInvitation = 5;

        // Actions
        const string aLogsIn = "A.logsIn";
        const string aLogsOut = "A.logsOut";
        const string jLogsIn = "J.logsIn";
        const string jLogsOut = "J.logsOut";
        const string aCreatesARoom = "A.creates." + svAngelaRoom;
        const string jCreatesJRoom = "J.creates." + svJacobRoom;
        const string aInvitesJ = "A.invites.J";
        const string jInvitesA = "J.invites.A";
        const string jAccepts = "J.accepts";
        const string aAccepts = "A.accepts";
        const string jDeclines = "J.declines";
        const string aDeclines = "A.declines";
        const string aLeavesARoom = "A.leaves." + svAngelaRoom;
        const string jLeavesARoom = "J.leaves." + svAngelaRoom;
        const string aLeavesJRoom = "A.leaves." + svJacobRoom;
        const string jLeavesJRoom = "J.leaves." + svJacobRoom;
        const string aEntersARoom = "A.enters." + svAngelaRoom;
        const string jEntersARoom = "J.enters." + svAngelaRoom;
        const string aEntersJRoom = "A.enters." + svJacobRoom;
        const string jEntersJRoom = "J.enters." + svJacobRoom;


        // TODO: Harry, do we need actions for Jacob leaves Room, and Angela leaves Room,
        // or are those covered by Angela Logs Out and Jacob Logs Out?

        public ChatRoom()
        {
            // Initialize the state vector
            vState[iAngelaStatus] = svAngelaStatus + LoggedOut;
            vState[iAngelaRoom] = svAngelaRoom + notCreated;
            vState[iAngelaInvitation] = svAngelaInvitation + noneInvited;
            vState[iJacobStatus] = svJacobStatus + LoggedOut;
            vState[iJacobRoom] = svJacobRoom + notCreated;
            vState[iJacobInvitation] = svJacobInvitation + noneInvited;
        }

        string StringifyStateVector(string[] v)
        {
            if (v.Length != 6)
            {
                string e = String.Format("ERROR: wrong-size state vector of length {0} in StringifyStateVector", v.Length);

                Console.WriteLine(e);
                return e;
            }

            return v[iAngelaStatus] + sep
                + v[iAngelaRoom] + sep
                + v[iAngelaInvitation] + sep
                + v[iJacobStatus] + sep
                + v[iJacobRoom] + sep
                + v[iJacobInvitation];
        }

        // Interface method
        public string GetInitialState()
        {
            return StringifyStateVector(vState);
        }

        // Interface method
        public List<string> GetAvailableActions(string startState)
        {
            // Do a vector element comparison to interpret start state
            string[] vStart = startState.Split(sep);

            List<string> actions = new List<string>();

            // Folks can always login or logout
            actions.Add(vStart[iAngelaStatus].Contains(LoggedIn) ? aLogsOut : aLogsIn);
            actions.Add(vStart[iJacobStatus].Contains(LoggedIn) ? jLogsOut : jLogsIn);

            // Someone who is logged in can do stuff
            if (vStart[iAngelaStatus].Contains(LoggedIn))
            {
                if (vStart[iAngelaRoom].Contains(notCreated))
                {
                    actions.Add(aCreatesARoom);
                }
                else if (vStart[iAngelaRoom].Contains(created))
                {
                    if (vStart[iAngelaRoom].Contains(angelaPresent))
                    {
                        actions.Add(aLeavesARoom);
                    }

                    if (vStart[iJacobStatus].Contains(LoggedIn))
                    {
                        if (vStart[iAngelaRoom].Contains(jacobPresent))
                        {
                            actions.Add(jLeavesARoom);
                        }
                        else if (vStart[iAngelaInvitation].Contains(jInvited))
                        {
                            actions.Add(jAccepts);
                            actions.Add(jDeclines);
                        }
                        else
                        {
                            if (vStart[iAngelaRoom].Contains(angelaPresent))
                            {
                                actions.Add(aInvitesJ);
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine("ERROR: AngelaRoom status is not valid '{0}'", vStart[iAngelaRoom]);
                }
            }

            if (vStart[iJacobStatus].Contains(LoggedIn))
            {
                if (vStart[iJacobRoom].Contains(notCreated))
                {
                    actions.Add(jCreatesJRoom);
                }
                else if (vStart[iJacobRoom].Contains(created))
                {
                    if (vStart[iJacobRoom].Contains(jacobPresent))
                    {
                        actions.Add(jLeavesJRoom);
                    }

                    if (vStart[iAngelaStatus].Contains(LoggedIn))
                    {
                        if (vStart[iJacobRoom].Contains(angelaPresent))
                        {
                            actions.Add(aLeavesJRoom);
                        }
                        else if (vStart[iJacobInvitation].Contains(aInvited))
                        {
                            actions.Add(aAccepts);
                            actions.Add(aDeclines);
                        }
                        else
                        {
                            if (vStart[iJacobRoom].Contains(jacobPresent))
                            {
                                actions.Add(jInvitesA);
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine("ERROR: JacobRoom status is not valid '{0}'", vStart[iJacobRoom]);
                }
            }
            return actions;
        }

        // Interface method
        public string GetEndState(string startState, string action)
        {
            string[] vStart = startState.Split(sep);

            switch (action)
            {
                case aLogsIn:
                    vStart[iAngelaStatus] = svAngelaStatus + LoggedIn;
                    break;
                case aLogsOut:
                    vStart[iAngelaStatus] = svAngelaStatus + LoggedOut;
                    if (vStart[iAngelaRoom].Contains(jacobPresent))
                    {
                        vStart[iAngelaRoom] = svAngelaRoom + created + jacobPresent;
                    }
                    else
                    {
                        vStart[iAngelaRoom] = svAngelaRoom + notCreated;
                    }

                    if (vStart[iJacobRoom].Contains(angelaPresent))
                    {
                        if (vStart[iJacobRoom].Contains(jacobPresent))
                        {
                            vStart[iJacobRoom] = svJacobRoom + created + jacobPresent;
                        }
                        else
                        {
                            vStart[iJacobRoom] = svJacobRoom + notCreated;
                        }
                    }
                    break;
                case jLogsIn:
                    vStart[iJacobStatus] = svJacobStatus + LoggedIn;
                    break;
                case jLogsOut:
                    vStart[iJacobStatus] = svJacobStatus + LoggedOut;
                    if (vStart[iJacobRoom].Contains(angelaPresent))
                    {
                        vStart[iJacobRoom] = svJacobRoom + created + angelaPresent;
                    }
                    else
                    {
                        vStart[iJacobRoom] = svJacobRoom + notCreated;
                    }
                    if (vStart[iAngelaRoom].Contains(jacobPresent))
                    {
                        if (vStart[iAngelaRoom].Contains(angelaPresent))
                        {
                            vStart[iAngelaRoom] = svAngelaRoom + created + angelaPresent;
                        }
                        else
                        {
                            vStart[iAngelaRoom] = svAngelaRoom + notCreated;
                        }
                    }
                    break;
                case aCreatesARoom:
                    // Person automaticall enters a room when they create it.
                    vStart[iAngelaRoom] = svAngelaRoom + created + angelaPresent;
                    break;
                case jCreatesJRoom:
                    // Person automaticall enters a room when they create it.
                    vStart[iJacobRoom] = svJacobRoom + created + jacobPresent;
                    break;
                case aInvitesJ:
                    vStart[iAngelaInvitation] = svAngelaInvitation + jInvited;
                    break;
                case jInvitesA:
                    vStart[iJacobInvitation] = svJacobInvitation + aInvited;
                    break;
                case jAccepts:
                    // The logic here would be different if more than two people
                    // were available for chat.
                    // Angela could logout before Jacob accepts the invitation, in which
                    // case her room would not exist.  If she is logged out, angela's
                    // room and invitation states are unchanged.
                    if (vStart[iAngelaRoom].Contains(created))
                    {
                        vStart[iAngelaRoom] += jacobPresent;
                        vStart[iAngelaInvitation] = svAngelaInvitation + noneInvited;
                    }
                    break;
                case aAccepts:
                    // The logic here would be different if more than two people
                    // were available for chat.
                    // Jacob could logout before Angela accepts the invitation, in which
                    // case his room would not exist.  If he is logged out, jacob's
                    // room and invitation states are unchanged.
                    if (vStart[iJacobRoom].Contains(created))
                    {
                        vStart[iJacobRoom] += angelaPresent;
                        vStart[iJacobInvitation] = svJacobInvitation + noneInvited;
                    }
                    break;
                case jDeclines:
                    vStart[iAngelaInvitation] = svAngelaInvitation + noneInvited;
                    break;
                case aDeclines:
                    vStart[iJacobInvitation] = svJacobInvitation + noneInvited;
                    break;
                case aLeavesARoom:
                    if (vStart[iAngelaRoom].Contains(jacobPresent))
                    {
                        vStart[iAngelaRoom] = svAngelaRoom + created + jacobPresent;
                    }
                    else
                    {
                        // When a room is emptied, the room disappears.
                        vStart[iAngelaRoom] = svAngelaRoom + notCreated;
                    }
                    break;
                case jLeavesARoom:
                    if (vStart[iAngelaRoom].Contains(angelaPresent))
                    {
                        vStart[iAngelaRoom] = svAngelaRoom + created + angelaPresent;
                    }
                    else
                    {
                        // When a room is emptied, the room disappears.
                        vStart[iAngelaRoom] = svAngelaRoom + notCreated;
                    }
                    break;
                case aLeavesJRoom:
                    if (vStart[iJacobRoom].Contains(jacobPresent))
                    {
                        vStart[iJacobRoom] = svJacobRoom + created + jacobPresent;
                    }
                    else
                    {
                        // When a room is emptied, the room disappears.
                        vStart[iJacobRoom] = svJacobRoom + notCreated;
                    }
                    break;
                case jLeavesJRoom:
                    if (vStart[iJacobRoom].Contains(angelaPresent))
                    {
                        vStart[iJacobRoom] = svJacobRoom + created + angelaPresent;
                    }
                    else
                    {
                        // When a room is emptied, the room disappears.
                        vStart[iJacobRoom] = svJacobRoom + notCreated;
                    }
                    break;
                case aEntersARoom:
                    vStart[iAngelaRoom] += angelaPresent;
                    break;
                case jEntersARoom:
                    vStart[iAngelaRoom] += jacobPresent;
                    break;
                case aEntersJRoom:
                    vStart[iJacobRoom] += angelaPresent;
                    break;
                case jEntersJRoom:
                    vStart[iJacobRoom] += jacobPresent;
                    break;

                default:
                    Console.WriteLine("ERROR: Unknown action '{0}' in GetEndState()", action);
                    break;
            }
            return StringifyStateVector(vStart);
        }
    }
}
