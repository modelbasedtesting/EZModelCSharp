using SeriousQualityEzModel;

namespace IstqbVendingMachine
{
    class VendingMachineProgram
    {
        static int Main()
        {
            VendingMachine client = new()
            {
                SelfLinkTreatment = SelfLinkTreatmentChoice.AllowAll,
                IncludeSelfLinkNoise = true
            };

            EzModelGraph graph = new (client, 1000, 300, 20);

            if (!graph.GenerateGraph())
            {
                Console.WriteLine("Failed to generate graph.");
                return -1;
            }

            List<string> report = graph.AnalyzeConnectivity();
            if (report.Count > 0)
            {
                Console.WriteLine("The graph is not strongly connected.");
                Console.WriteLine("problems report:");
                foreach (string S in report)
                {
                    Console.WriteLine(S);
                }
                return -2;
            }

            List<string> duplicateActions = graph.ReportDuplicateOutlinks();
            if (duplicateActions.Count > 0)
            {
                Console.WriteLine("There are duplicate outlinks in the graph.");
                foreach (string S in duplicateActions)
                {
                    Console.WriteLine(S);
                }
            }

            graph.DisplayStateTable(); // Display the Excel-format state table

            graph.CreateGraphVizFileAndImage(EzModelGraph.GraphShape.Default);

            // If you want to drive the system under test as EzModel generates test steps,
            // set client.NotifyAdapter true.
            client.NotifyAdapter = false;
            // If you want EzModel to stop generating test steps when a problem is
            // detected, set client.NotifyAdapter true, set client.StopOnProblem true,
            // and then return false from the client.AreStatesAcceptablySimilar() method.
            client.StopOnProblem = true;

            graph.RandomDestinationCoverage("ClientTemplate", 3);
            return 0;
        }
    }

    public partial class VendingMachine : IEzModelClient
    {
        readonly Dictionary<string, string> state;

        // State variable names
        const string HAS_COFFEE = "CoffeeAvailable"; // yes when COFFEE_INVENTORY > 0, no when COFFEE_INVENTORY == 0
        const string HAS_TEA = "TeaAvailable"; // yes when TEA_INVENTORY > 0, no when TEA_INVENTORY == 0
        const string HAS_HOT_WATER = "HotWaterAvailable"; // yes when HOT_WATER_INVENTORY > 0, no when HOT_WATER_INVENTORY == 0
        const string MONEY_INSERTED = "MoneyInserted"; // Cents, 0 or more.
        const string DISPENSE_LIGHT = "DispenseLight"; // on or off
        const string DRINK_SELECTION = "Selection"; // choose from None, Tea, Coffee, or Hot water

        const string HOT_WATER_PRICE = "HotWater"; // whole number as cents.
        const string TEA_PRICE = "Tea"; // whole number as cents
        const string COFFEE_PRICE = "Coffee"; // whole number as cents

        readonly List<string> stateVariableList = new()
        {
            //            HAS_COFFEE,
            //            HAS_TEA,
            //            HAS_HOT_WATER,
            MONEY_INSERTED,
            DISPENSE_LIGHT,
            DRINK_SELECTION
        };

        // TODO: Declare actions
        // SUGGESTION: Choose "const string" as the action
        // data type whenever possible.  A "const string" value
        // can be utilized in a switch case, which is useful in
        // GetEndState().
        // Example:
        const string selectTea = "Select Tea";
        const string selectCoffee = "Select Coffee";
        const string selectHotWater = "Select Hot Water";
        const string selectCocaCola = "Select Coca-Cola";
        const string cancelSelection = "Cancel Selection";
        const string addNickel = "Insert nickel";
        const string addDime = "Insert dime";
        const string addQuarter = "Insert quarter";
        const string refund = "Refund";
        const string dispense = "Dispense";

        // MORE ACTIONS TO ADD:
        // setCoffeePrice
        // setTeaPrice
        // setHotWaterPrice
        // addCoffeeIngredients - as inventory count
        // addTeaIngredients
        // addHotWaterIngredients

        int actionsDict( string name )
        {
            // What is the point of this function?
            // The goal is to make a "const" dictionary, although this is
            // not quite it.  So there is more to figure out and this is where
            // we are right now.
            // Usage: provide constant values mapped to constant strings
            // The strings (action names) are used throughout this class.
            // The values (action enumerators) are passed to and from EzModel.
            // The GetEndState() method could be written with a switch block 
            // instead of a multiplicity of if blocks when the mapping of
            // action names to action enumerators is constant.
            switch (name)
            {
                case selectTea: return 0;
                case selectCoffee: return 1;
                case selectHotWater: return 2;
                case cancelSelection: return 3;
                case addNickel: return 4;
                case addDime: return 5;
                case addQuarter: return 6;
                case refund: return 7;
                case dispense: return 8;
                case selectCocaCola: return 9;
                default: return -1;
            }
        }

        readonly Dictionary<int, Dictionary<string, string>> statesDict;
        int statesCounter;

        public VendingMachine()
        {
            state = new Dictionary<string, string>
            {
                //            state[HAS_COFFEE] = "yes";
                //            state[HAS_TEA] = "yes";
                //            state[HAS_HOT_WATER] = "yes";
                [MONEY_INSERTED] = "0",
                [DISPENSE_LIGHT] = "off",
                [DRINK_SELECTION] = "None"
            };

            statesDict = new Dictionary<int, Dictionary<string, string>>();

            statesCounter = 0;
            statesDict[statesCounter] = new Dictionary<string, string>(state);
            statesCounter++;
        }

        // User Interface information
        readonly int teaPrice = 85;
        readonly int coffeePrice = 100;
        readonly int hotWaterPrice = 10;
        readonly int cocaColaPrice = 105;

        public string StringifyState(int state)
        {
            string stateString = "";

            foreach (string stateVariable in stateVariableList)
            {
                stateString += stateVariable + "." + statesDict[state][stateVariable] + "\n";
            }

            return stateString.Substring(0, stateString.Length - 1);
        }

        int UpdateStates(Dictionary<string, string> state)
        {
            foreach (KeyValuePair<int, Dictionary<string, string>> entry in statesDict)
            {
                if (!entry.Value.Except(state).Any())
                {
                    return entry.Key;
                }
            }
            statesDict[statesCounter] = new Dictionary<string, string>(state);
            statesCounter++;
            return statesCounter - 1;
        }

        // IEzModelClient Interface method
        public int GetInitialState()
        {
            return 0;
        }

        // IEzModelClient Interface method
        public string[] GetActionsList()
        {
            return new string[]
                { selectTea,
                selectCoffee,
                selectHotWater,
                cancelSelection,
                addNickel,
                addDime,
                addQuarter,
                refund,
                dispense,
                selectCocaCola };
        }

        // IEzModelClient Interface method
        public List<int> GetAvailableActions(int currentState)
        {
            List<int> actionList = new();

            if (statesDict[currentState][DRINK_SELECTION] == "None")
            {
                actionList.Add(actionsDict(selectCoffee));
                actionList.Add(actionsDict(selectTea));
                actionList.Add(actionsDict(selectHotWater));
                actionList.Add(actionsDict(selectCocaCola));
            }
            else // DRINK_SELECTION is not None
            {
                actionList.Add(actionsDict(cancelSelection));
                if (statesDict[currentState][DISPENSE_LIGHT] == "on")
                {
                    actionList.Add(actionsDict(dispense));
                }
            }

            int insertedMoney = ParseMoneyInserted(statesDict[currentState]);

            if (insertedMoney < 105)
            {
                actionList.Add(actionsDict(addDime));
                actionList.Add(actionsDict(addNickel));
                actionList.Add(actionsDict(addQuarter));
            }

            if (insertedMoney > 0)
            {
                actionList.Add(actionsDict(refund));
            }

            return actionList;
        }

        int ParseMoneyInserted(Dictionary<string, string> state)
        {
            int insertedMoney = 0;
            try
            {
                insertedMoney = int.Parse(state[MONEY_INSERTED]);
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR: could not parse int from MONEY_INSERTED value of '{0}'", state[MONEY_INSERTED]);
                Console.WriteLine(e.Message);
            }

            return insertedMoney;
        }

        bool CanDispense(Dictionary<string, string> state)
        {
            // For the drink selection != None, calculate
            // whether enough coins have been paid and
            // return true whe enough have been paid,
            // false otherwise
            if (state[DRINK_SELECTION] == "None")
            {
                Console.WriteLine("Why is drink selection {0} in CanPayFor()??", state[DRINK_SELECTION]);
                return false;
            }

            int cost = 0;

            if (state[DRINK_SELECTION] == "Coffee")
            {
                cost = coffeePrice;
            }
            if (state[DRINK_SELECTION] == "Tea")
            {
                cost = teaPrice;
            }
            if (state[DRINK_SELECTION] == "Hot water")
            {
                cost = hotWaterPrice;
            }
            if (state[DRINK_SELECTION] == "Coca-Cola")
            {
                cost = cocaColaPrice;
            }

            return ParseMoneyInserted(state) >= cost;
        }

        // IEzModelClient Interface method
        public int GetEndState(int startState, int action)
        {
            Dictionary<string, string> endState = new(statesDict[startState]);

            // Unable to use a switch (action) block because 
            // the actionsDict() method doesn't output constants.
            // TODO: come up with constants that map to the actionsDict
            // values so that a switch block can be used here instead of
            // so many if blocks.
            if (action == actionsDict(selectCoffee))
            {
                endState[DRINK_SELECTION] = "Coffee";
            }

            if (action == actionsDict(selectTea))
            {
                endState[DRINK_SELECTION] = "Tea";
            }

            if (action == actionsDict(selectHotWater))
            {
                endState[DRINK_SELECTION] = "Hot water";
            }

            if (action == actionsDict(selectCocaCola))
            {
                endState[DRINK_SELECTION] = "Coca-Cola";
            }

            if (action == actionsDict(cancelSelection))
            {
                endState[DRINK_SELECTION] = "None";
            }

            int moneyInserted = ParseMoneyInserted(endState);

            if (action == actionsDict(dispense))
            {
                if (endState[DRINK_SELECTION] == "Hot water")
                {
                    moneyInserted -= hotWaterPrice;
                }
                if (endState[DRINK_SELECTION] == "Coffee")
                {
                    moneyInserted -= coffeePrice;
                }
                if (endState[DRINK_SELECTION] == "Tea")
                {
                    moneyInserted -= teaPrice;
                }
                if (endState[DRINK_SELECTION] == "Coca-Cola")
                {
                    moneyInserted -= cocaColaPrice;
                }
                endState[DRINK_SELECTION] = "None";
            }

            if (action == actionsDict(addDime))
            {
                moneyInserted += 10;
            }

            if (action == actionsDict(addNickel))
            {
                moneyInserted += 5;
            }

            if (action == actionsDict(addQuarter))
            {
                moneyInserted += 25;
            }

            if (action == actionsDict(refund))
            {
                moneyInserted = 0;
            }

            endState[MONEY_INSERTED] = moneyInserted.ToString();

            if (endState[DRINK_SELECTION] == "None")
            {
                endState[DISPENSE_LIGHT] = "off";
            }
            else
            {
                endState[DISPENSE_LIGHT] = CanDispense(endState) ? "on" : "off";
            }

            return UpdateStates(endState);
        }
    }

    public partial class VendingMachine
    {
        SelfLinkTreatmentChoice skipSelfLinks;
        bool notifyAdapter;
        bool stopOnProblem;
        bool includeSelfLinkNoise = false;

        // These properties are unimportant until after the model is building.
        // Get them out of the way, in a place that will be easy to get at later.
        // Interface Properties
        public SelfLinkTreatmentChoice SelfLinkTreatment
        {
            get => skipSelfLinks;
            set => skipSelfLinks = value;
        }

        // IEzModelClient Interface Property
        public bool NotifyAdapter
        {
            get => notifyAdapter;
            set => notifyAdapter = value;
        }

        // IEzModelClient Interface Property
        public bool StopOnProblem
        {
            get => stopOnProblem;
            set => stopOnProblem = value;
        }

        public bool IncludeSelfLinkNoise
        {
            get => includeSelfLinkNoise;
            set => includeSelfLinkNoise = value;
        }

        // IEzModelClient Interface method
        public void SetStateOfSystemUnderTest(int state)
        {
            // TODO: Implement this when NotifyAdapter is true.
        }

        // IEzModelClient Interface method
        public void ReportProblem(int initialState, int observed, int predicted, List<int> popcornTrail)
        {
            // TODO: Implement this when NotifyAdapter is true
        }

        // IEzModelClient Interface method
        public bool AreStatesAcceptablySimilar(int observed, int expected)
        {
            // TODO: Implement this when NotifyAdapter is true

            // Compare reported to expected, if unacceptable return false.
            return true;
        }

        // IEzModelClient Interface method
        public void ReportTraversal(int initialState, List<int> popcornTrail)
        {
            // TODO: Implement this when NotifyAdapter is true
        }

        // IEzModelClient Interface method
        public int AdapterTransition(int startState, int action)
        {
            // TODO: Finish implementation when NotifyAdapter is true

            int expected = GetEndState(startState, action);
            int observed = -1;

            // Responsibilities:
            // Optionally, validate that the state of the system under test
            // is acceptably similar to the startState argument. 
            // Required: drive the system under test according to the action
            // argument.
            // If executing the action is problematic, output a problem
            // notice in some way, and return an empty string to the caller
            // to indicate the start state was not reached.
            // If the action executes without problem, then measure the state
            // of the system under test and return the stringified SUT
            // state vector to the caller.

            return observed;
        }
    }
}
