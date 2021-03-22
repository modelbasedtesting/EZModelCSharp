using System;
using System.Collections.Generic;
using SeriousQualityEzModel;

namespace MultiUserChatRoomExample
{
    class MultiUserChatRoomProgram
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Args = {0}", args.ToString());

            ChatRoom rules;

            if (args.Length > 2)
            {
                rules = new ChatRoom(uint.Parse(args[1]), uint.Parse(args[2]));
            }
            else
            {
                rules = new ChatRoom(2, 2);
            }

            GeneratedGraph graph = new GeneratedGraph(rules);

            graph.DisplayStateTable(); // Display the Excel-format state table
            Console.ReadLine();
        }
    }

    public class ChatRoom : IUserRules
    {
        // Take the number of users chatting and the number who create chat rooms as arguments.
        // Main( uint numUsers, uint numRoomHosts )
        // constraint: numRoomHosts <= numUsers
        // Users are then numbered 1 through numUsers,
        // and Rooms are numbered 1 through numRoomHosts.
        // For simplicity, user i hosts room i.
        // A room host creates their room, and invites others to their room.
        // A room host cannot invite themself to their room.
        uint users;
        uint roomHosts;

        // State Variables
        // Per User
        const string svStatus = "Status";
        const string svInvitation = "Invitation";
        const string svRoom = "Room";

        // State values
        // For *Status variables
        const string LoggedOut = ".loggedOut";
        const string LoggedIn = ".loggedIn";
        // For *Room variables
        const string notCreated = ".notCreated";
        const string created = ".created";
        const string userPresent = "Present";
        // For *Invitation variables; a person will not invite themself.
        const string noneInvited = ".noneInvited";
        const string userInvited = "Invited";
        // IUserRules.valueSeparator abbreviated constant name for syntax convenience
        const string sep = IUserRules.valueSeparator;

        // Keep a vector of state variable values, one state variable represented by
        // each vector element.  Some state variables can have multiple simultaneous
        // values, for example U1Room has possibilities of notCreated, created AND U1Present,
        // created AND U2Present, or created AND U1Present AND U2Present.
        // When responding to GetInitialState and GetEndState, return 
        // a valueSeparator-delimited string from the populated array elements.
        string[,] vState;
        const uint iStatus = 0;
        const uint iInvitation = 1;
        const uint iRoom = 2;

        // Actions
        const string LogsIn = ".logsIn";
        const string LogsOut = ".logsOut";
        const string CreatesRoom = ".createsRoom";
        const string Invites = ".invites.";
        const string Accepts = ".accepts.";
        const string Declines = ".declines.";
        const string LeavesRoom = ".leaves.";
        const string EntersRoom = ".enters.";

        // TODO: Harry, do we need actions for Jacob leaves Room, and Angela leaves Room,
        // or are those covered by Angela Logs Out and Jacob Logs Out?

        public ChatRoom(uint numUsers, uint numRoomHosts)
        {
            roomHosts = numRoomHosts;
            users = numUsers;

            vState = new string[users, 3];

            // Initialize the state vector
            for ( uint i = 0; i < users; i++ )
            {
                vState[i, iStatus] = String.Format("U{0}{1}{2}", i + 1, svStatus, LoggedOut);
                vState[i, iInvitation] = String.Format("U{0}{1}{2}", i + 1, svInvitation, noneInvited);
                vState[i, iRoom] = String.Format("U{0}{1}{2}", i + 1, svRoom, notCreated);
            }
        }

        string StringifyStateVector()
        {
            string s = "";

            for ( uint i=0; i < users; i++ )
            {
                s += vState[i, iStatus] + sep + vState[i, iInvitation] + sep + vState[i, iRoom];
                if ( i < users-1 )
                {
                    s += sep;
                }
            }

            return s;
        }

        // Interface method
        public string GetInitialState()
        {
            return StringifyStateVector();
        }

        // Interface method
        public List<string> GetAvailableActions(string startState)
        {
            List<string> actions = new List<string>();

            string[] vStart = startState.Split(sep);

            for (uint i = 0; i < users; i++)
            {
                string status = vStart[i * 3];
                string invitation = vStart[i * 3 + 1];
                string room = vStart[i * 3 + 2];
                string user = String.Format("U{0}", i + 1);
                actions.Add(vState[i, 0].Contains(LoggedIn) ? user + LogsOut : user + LogsIn);

                // Someone who is logged in can do stuff
                if (status.Contains(LoggedIn))
                {
                    if (room.Contains(notCreated))
                    {
                        actions.Add(String.Format("U{0}{1}", i, CreatesRoom));
                    }
                    else if (room.Contains(created))
                    {
                        for (uint j = 0; j < users; j++)
                        {
                            string present = String.Format("U{0}Present", j + 1);

                            if (room.Contains(present))
                            {
                                actions.Add(String.Format("U{0}{1}", j+1, LeavesRoom));
                            }

                            if (vStart[iJacobStatus].Contains(LoggedIn))
                            {
                                if (room.Contains(jacobPresent))
                                {
                                    actions.Add(jLeavesARoom);
                                }
                                else if (invitation.Contains(jInvited))
                                {
                                    actions.Add(jAccepts);
                                    actions.Add(jDeclines);
                                }
                                else
                                {
                                    if (room.Contains(angelaPresent))
                                    {
                                        actions.Add(aInvitesJ);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("ERROR: AngelaRoom status is not valid '{0}'", vStart[iAngelaRoom]);
                    }
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