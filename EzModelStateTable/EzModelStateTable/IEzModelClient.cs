using System.Collections.Generic;

namespace SeriousQualityEzModel
{
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
        string StringifyState(int state);
        int GetInitialState();
        string[] GetActionsList();
        List<int> GetAvailableActions(int startState);
        int GetEndState(int startState, int action);
        void ReportTraversal(int initialState, List<int> popcornTrail);

        // Test Execution:
        // EzModel calls these methods only when NotifyAdapter is true.
        int AdapterTransition(int startState, int action);
        bool AreStatesAcceptablySimilar(int observed, int predicted);
        void ReportProblem(int initialState, int observed, int predicted, List<int> popcornTrail);
        void SetStateOfSystemUnderTest(int state);
    }
}

