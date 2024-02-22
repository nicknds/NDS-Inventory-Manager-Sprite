using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Network;
using VRage.ObjectBuilders;
using VRage.Scripting;
using VRageMath;
using static IngameScript.Program;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        #region mdk preserve
        double OverheatAverage = 0.085, ActionLimiterMultiplier = 0.005, RunTimeLimiter = 0.075;
        int EchoDelay = 10;
        bool UseVanillaLibrary = true;
        #endregion


        #region Variables


        IMyGridTerminalSystem gtSystem;

        SortedDictionary<string, SortedList<string, string>> settingDictionaryStrings = new SortedDictionary<string, SortedList<string, string>>
{
    { "A. Global Tags", new SortedList<string, string>
        {
            { setKeyExclusion, "exclude" }, { setKeyIncludeGrid, "includeGrid" }, { setKeyCrossGrid, "crossGrid" },
            { setKeyPanel, "[nds]" }, { setKeyGlobalFilter, "" }, { setKeyExcludeGrid, "excludeGrid" }, { setKeyOptionBlockFilter, "" }
        }
    },
    { "B. Default Categories", new SortedList<string, string>
        {
            { setKeyIngot, "ingot" }, { setKeyOre, "ore" }, { setKeyComponent, "component" },
            { setKeyTool, "tool" }, { setKeyAmmo, "ammo" }
        }
    }
};

        SortedList<string, string>
            stateErrorCodes = NewSortedListStringString,
            modItemDictionary = NewSortedListStringString,
            oreKeyedItemDictionary = NewSortedListStringString;

        SortedDictionary<string, SortedList<string, double>> settingDictionaryDoubles = new SortedDictionary<string, SortedList<string, double>>
{
    { "A. Delays", new SortedList<string, double>
        {
            { setKeyDelayScan, 10 }, { setKeyDelayProcessLimits, 20 }, { setKeyDelaySorting, 7.5 },
            { setKeyDelayDistribution, 20 }, { setKeyDelaySpreading, 15 }, { setKeyDelayQueueAssembly, 5 },
            { setKeyDelayQueueDisassembly, 10 }, { setKeyDelayRemoveExcessAssembly, 20 }, { setKeyDelayRemoveExcessDisassembly, 20 },
            { setKeyDelaySortBlueprints, 12.5 }, { setKeyDelaySortCargoPriority, 90 }, { setKeyDelaySpreadBlueprints, 20 },
            { setKeyDelayLoadouts, 15}, { setKeyDelayFillingBottles, 30 }, { setKeyDelayLogic, 10 },
            { setKeyDelayIdleAssemblerCheck, 15 }, { setKeyDelayResetIdleAssembler, 45 }, { setKeyDelayFindModItems, 5 },
            { setKeyDelaySortRefinery, 6 }, { setKeyDelayOrderCargo, 15 }
        }
    },
    { "B. Performance", new SortedList<string, double>
        {
            { setKeyActionLimiterMultiplier, 0.35 }, { setKeyRunTimeLimiter, 0.45 },
            { setKeyOverheatAverage, 0.6 }
        }
    },
    { "C. Defaults", new SortedList<string, double>
        {
            { setKeyIcePerGenerator, 5000 }, { setKeyFuelPerReactor, 25 }, { setKeyAmmoPerGun, 40 },
            { setKeyCanvasPerParachute, 4 }
        }
    },
    { "D. Adjustments", new SortedList<string, double>
        {
            { setKeyBalanceRange, 0.05 }, { setKeyAllowedExcessPercent, 0.1 }, { setKeyDynamicQuotaPercentageIncrement, 0.05 },
            { setKeyDynamicuotaMaxMultiplier, 2.5 }, { setKeyDynamicQuotaNegativeThreshold, 3 }, { setKeyDynamicQuotaPositiveThreshold, 9 }
        }
    }
};

        SortedList<string, SortedList<string, bool>> settingDictionaryBools = new SortedList<string, SortedList<string, bool>>
{
    { "A. Basic", new SortedList<string, bool>
        {
            { setKeyToggleCountItems, true }, { setKeyToggleCountBlueprints, true }, { setKeyToggleSortItems, true },
            { setKeyToggleQueueAssembly, true}, { setKeyToggleQueueDisassembly, true }, { setKeyToggleDistribution, true },
            { setKeyToggleAutoLoadSettings, true }
        }
    },
    { "B. Advanced", new SortedList<string, bool>
        {
            { setKeyToggleProcessLimits, true }, { setKeyToggleSpreadRefieries, true },
            { setKeyToggleSpreadReactors, true }, { setKeyToggleSpreadGuns, true }, { setKeyToggleSpreadGasGenerators, true },
            { setKeyToggleSpreadGravelSifters, true }, { setKeyToggleSpreadParachutes, true }, { setKeyToggleRemoveExcessAssembly, true },
            { setKeyToggleRemoveExcessDisassembly, true }, { setKeyToggleSortBlueprints, true }, { setKeyToggleSortCargoPriority, true},
            { setKeyToggleSpreadBlueprints, true }, { setKeyToggleDoLoadouts, true }, { setKeyToggleLogic, true },
            { setKeyToggleResetIdleAssemblers, true }, { setKeyToggleFindModItems, true }, { setKeyToggleToggleSortRefineries, true},
            { setKeyToggleOrderCargo, true }
        }
    },
    { "C. Settings", new SortedList<string, bool>
        {
            { setKeyAutoConveyorRefineries, false }, { setKeyAutoConveyorReactors, false }, { setKeyAutoConveyorGasGenerators, false },
            { setKeyAutoConveyorGuns, false }, { setKeyToggleDynamicQuota, true }, { setKeyDynamicQuotaIncreaseWhenLow, true },
            { setKeySameGridOnly, false }, { setKeySurvivalKitAssembly, false }, { setKeyAddLoadoutsToQuota, true },
            { setKeyControlConveyors, true }, { setKeyAutoTagBlocks, true }
        }
    }
};

        SortedList<string, int> settingsInts = new SortedList<string, int>() {
    { setKeyUpdateFrequency, 1 }, { setKeyOutputLimit, 15 },
    { setKeySurvivalKitQueuedIngots, 0 }, { setKeyAutoMergeLengthTolerance, 6 },
    { setKeyPrioritizedOreCount, 0 } };

        static SortedList<string, List<string>> settingsListsStrings = new SortedList<string, List<string>>()
{
    { setKeyExcludedDefinitions, new List<string>() { "LargeBlockBed", "LargeBlockLockerRoom",
        "LargeBlockLockerRoomCorner", "LargeBlockLockers", "PassengerSeatSmall", "PassengerSeatLarge", "LargeInteriorTurret" } },
    { setKeyGravelSifterKeys, new List<string>() { "gravelrefinery", "gravelseparator", "gravelsifter" } },
    { setKeyDefaultSuffixes, new List<string>() {
        "K", "M", "B", "T"
        }
    }
};

        SortedList<string, ItemCollection> customItemGroups = new SortedList<string, ItemCollection>();

        static SortedList<string, SortedList<string, ItemDefinition>> itemListMain = new SortedList<string, SortedList<string, ItemDefinition>>();

        SortedList<string, List<long>> indexesStorageLists = new SortedList<string, List<long>>();

        SortedList<string, List<PotentialAssembler>> potentialAssemblerList = new SortedList<string, List<PotentialAssembler>>();

        static Dictionary<long, BlockDefinition> managedBlocks = new Dictionary<long, BlockDefinition>(1500);

        Dictionary<string, Blueprint>
            blueprintList = new Dictionary<string, Blueprint>();

        Dictionary<string, string>
            gunAmmoDictionary = new Dictionary<string, string>(),
            itemCategoryDictionary = new Dictionary<string, string>();

        HashSet<string> antiflickerSet = NewHashSetString,
                        priorityCategories = NewHashSetString,
                        priorityTypes = NewHashSetString,
                        assemblyNeededByMachine = NewHashSetString,
                        clearedSettingLists = NewHashSetString;

        HashSet<IMyCubeGrid> gridList = new HashSet<IMyCubeGrid>(), excludedGridList = new HashSet<IMyCubeGrid>();

        HashSet<long> uniqueIndexSet = NewHashSetLong,
                      clonedEntityIDs = NewHashSetLong,
                      excludedIDs = NewHashSetLong,
                      accessibleIDs = NewHashSetLong,
                      includedIDs = NewHashSetLong,
                      setRemoveIDs = NewHashSetLong;

        List<OutputObject> outputList = new List<OutputObject>(), outputErrorList = new List<OutputObject>();

        static SortedList<string, List<long>> typedIndexes = new SortedList<string, List<long>>
{
    { setKeyIndexAssemblers, NewLongList },
    { setKeyIndexGasGenerators, NewLongList },
    { setKeyIndexGravelSifters, NewLongList },
    { setKeyIndexGun, NewLongList },
    { setKeyIndexHydrogenTank, NewLongList },
    { setKeyIndexOxygenTank, NewLongList },
    { setKeyIndexParachute, NewLongList },
    { setKeyIndexReactor, NewLongList },
    { setKeyIndexRefinery, NewLongList },
    { setKeyIndexStorage, NewLongList },
    { setKeyIndexSortable, NewLongList },
    { setKeyIndexLoadout, NewLongList },
    { setKeyIndexLogic, NewLongList },
    { setKeyIndexPanel, NewLongList},
    { setKeyIndexInventory, NewLongList },
    { setKeyIndexLimit, NewLongList }
};

        List<string>
            fullExclude = new List<string> { setKeySameGridOnly, setKeySurvivalKitAssembly },
            itemCategoryList = NewStringList, modBlueprintList = NewStringList,
            logicSetsList = NewStringList, tagTempStringList = NewStringList,
            processBlockStorageList = NewStringList, processBlockOptionList = NewStringList;

        List<long> tempBalanceItemIndexes;

        List<SortableObject> sortableListDelayHelp = NewSortableObjectList,
                             sortableListMain = NewSortableObjectList,
                             sortableListAlternate = NewSortableObjectList;

        List<IMyBlockGroup> groupList = new List<IMyBlockGroup>();

        List<IMyTerminalBlock> groupBlocks = new List<IMyTerminalBlock>(), scannedBlocks = new List<IMyTerminalBlock>(1500);

        List<ItemDefinition> itemListAllItems = new List<ItemDefinition>();

        ItemCollection itemCollectionProcessSetting, itemCollectionMain,
                       itemCollectionAlternate, itemCollectionProcessTotalLoadout;

        TimeSpan scanSpan = ZeroSpan, fillBottleSpan = ZeroSpan;

        SortedList<string, TimeSpan> delaySpans = new SortedList<string, TimeSpan>();

        SortedList<string, StateRecord> stateRecords = new SortedList<string, StateRecord>();

        SortedList<long, double> tempDistributeItemIndexes;

        DateTime tickStartTime = Now, lastActionClearTime = Now;

        static string currentMajorFunction = "Idle";

        string
            currentFunction = "Program",
            scriptName = "NDS Inventory Manager",
            settingBackup = "", mergeItem = "",
            stoneOreToIngotBasicID = PositionPrefix("0010", "StoneOreToIngotBasic"),
            lastString = "", presetPanelOptions, itemCategoryString, selfContainedIdentifier = "",
            tempItemSetting;

        static string
            crossGridKeyword, includeGridKeyword,
            excludeGridKeyword, exclusionKeyword,
            ingotKeyword, oreKeyword,
            componentKeyword, ammoKeyword,
            toolKeyword, globalFilterKeyword,
            panelTag, optionBlockFilter;

        const string
            componentType = "MyObjectBuilder_Component",
            oreType = "MyObjectBuilder_Ore",
            ingotType = "MyObjectBuilder_Ingot",
            toolType = "MyObjectBuilder_PhysicalGunObject",
            hydBottleType = "MyObjectBuilder_GasContainerObject",
            oxyBottleType = "MyObjectBuilder_OxygenContainerObject",
            ammoType = "MyObjectBuilder_AmmoMagazine",
            dataPadType = "MyObjectBuilder_Datapad",
            consumableType = "MyObjectBuilder_ConsumableItem",
            physicalObjectType = "MyObjectBuilder_PhysicalObject",
            nothingType = "None",
            stoneType = "Stone",
            canvasType = "Canvas",
            blueprintPrefix = "MyObjectBuilder_BlueprintDefinition",
            setKeyExclusion = "exclusionKeyword", //modifier tags
            setKeyIncludeGrid = "includeGridKeyword",
            setKeyExcludeGrid = "excludeGridKeyword",
            setKeyCrossGrid = "crossGridControlKeyword",
            setKeyGlobalFilter = "globalFilterKeyword",
            setKeyOptionBlockFilter = "optionHeader",
            setKeyIngot = "itemIngotKeyword", //control keys
            setKeyOre = "itemOreKeyword",
            setKeyComponent = "itemComponentKeyword",
            setKeyTool = "itemToolKeyword",
            setKeyAmmo = "itemAmmoKeyword",
            setKeyPanel = "panelKeyword",
            setKeyDelayScan = "delayScan", //delays
            setKeyDelayProcessLimits = "delayProcessLimits",
            setKeyDelaySorting = "delaySortItems",
            setKeyDelayDistribution = "delayDistributeItems",
            setKeyDelaySpreading = "delaySpreadItems",
            setKeyDelayQueueAssembly = "delayQueueAssembly",
            setKeyDelayQueueDisassembly = "delayQueueDisassembly",
            setKeyDelayRemoveExcessAssembly = "delayRemoveExcessAssembly",
            setKeyDelayRemoveExcessDisassembly = "delayRemoveExcessDisassembly",
            setKeyDelaySortBlueprints = "delaySortBlueprints",
            setKeyDelaySortCargoPriority = "delaySortCargoPriority",
            setKeyDelaySpreadBlueprints = "delaySpreadBlueprints",
            setKeyDelayLoadouts = "delayLoadouts",
            setKeyDelayFillingBottles = "delayFillingBottles",
            setKeyDelayLogic = "delayLogic",
            setKeyDelayIdleAssemblerCheck = "delayCheckIdleAssemblers",
            setKeyDelayResetIdleAssembler = "delayResetIdleAssembler",
            setKeyDelayFindModItems = "delayFindModItems",
            setKeyDelaySortRefinery = "delaySortRefinery",
            setKeyDelayOrderCargo = "delayOrderCargo",
            setKeyActionLimiterMultiplier = "actionLimiterMultiplier", //performance
            setKeyRunTimeLimiter = "runTimeLimiter",
            setKeyOverheatAverage = "overheatAverage",
            setKeyIcePerGenerator = "icePerO2/H2Generator", //default fill amounts
            setKeyFuelPerReactor = "fuelPerReactor",
            setKeyAmmoPerGun = "ammoPerGun",
            setKeyCanvasPerParachute = "canvasPerParachute",
            setKeyBalanceRange = "balanceRange", //adjustments
            setKeyAllowedExcessPercent = "allowedExcessPercent",
            setKeyDynamicQuotaPercentageIncrement = "dynamicQuotaPercentageIncrement",
            setKeyDynamicuotaMaxMultiplier = "dynamicQuotaMaxMultiplier",
            setKeyDynamicQuotaNegativeThreshold = "dynamicQuotaNegativeThreshold",
            setKeyDynamicQuotaPositiveThreshold = "dynamicQuotaPositiveThreshold",
            setKeyUpdateFrequency = "updateFrequency",
            setKeyOutputLimit = "outputLimit",
            setKeySurvivalKitQueuedIngots = "survivalKitQueuedIngots",
            setKeyAutoMergeLengthTolerance = "autoMergeLengthTolerance",
            setKeyPrioritizedOreCount = "prioritizedOreCount",
            setKeyToggleCountItems = "countItems", //basic toggles
            setKeyToggleCountBlueprints = "countBlueprints",
            setKeyToggleSortItems = "sortItems",
            setKeyToggleQueueAssembly = "queueAssembly",
            setKeyToggleQueueDisassembly = "queueDisassembly",
            setKeyToggleDistribution = "distributeItems",
            setKeyToggleAutoLoadSettings = "autoLoadSettings",
            setKeyToggleProcessLimits = "processLimits",//advanced toggles
            setKeyToggleSpreadRefieries = "spreadRefineries",
            setKeyToggleSpreadReactors = "spreadReactors",
            setKeyToggleSpreadGuns = "spreadGuns",
            setKeyToggleSpreadGasGenerators = "spreadH2/O2Gens",
            setKeyToggleSpreadGravelSifters = "spreadGravelSifters",
            setKeyToggleSpreadParachutes = "spreadParachutes",
            setKeyToggleRemoveExcessAssembly = "removeExcessAssembly",
            setKeyToggleRemoveExcessDisassembly = "removeExcessDisassembly",
            setKeyToggleSortBlueprints = "sortBlueprints",
            setKeyToggleSortCargoPriority = "sortCargoPriority",
            setKeyToggleSpreadBlueprints = "spreadBlueprints",
            setKeyToggleDoLoadouts = "doLoadouts",
            setKeyToggleLogic = "triggerLogic",
            setKeyToggleResetIdleAssemblers = "resetIdleAssemblers",
            setKeyToggleFindModItems = "findModItems",
            setKeyToggleToggleSortRefineries = "sortRefineries",
            setKeyToggleOrderCargo = "orderCargo",
            setKeyAutoConveyorRefineries = "useConveyorRefineries", //settings
            setKeyAutoConveyorReactors = "useConveyorReactors",
            setKeyAutoConveyorGasGenerators = "useConveyorH2/O2Gens",
            setKeyAutoConveyorGuns = "useConveyorGuns",
            setKeyToggleDynamicQuota = "dynamicQuota",
            setKeyDynamicQuotaIncreaseWhenLow = "dynamicQuotaIncreaseWhenLow",
            setKeySameGridOnly = "sameGridOnly",
            setKeySurvivalKitAssembly = "survivalKitAssembly",
            setKeyAddLoadoutsToQuota = "addLoadoutsToQuota",
            setKeyControlConveyors = "controlConveyors",
            setKeyAutoTagBlocks = "autoTagBlocks",
            setKeyExcludedDefinitions = "excludedDefinitions", //setting lists
            setKeyGravelSifterKeys = "gravelSifterKeys",
            setKeyDefaultSuffixes = "numberSuffixes",
            setKeyIndexAssemblers = "asm", //Block index list keys
            setKeyIndexGasGenerators = "gas",
            setKeyIndexGravelSifters = "sft",
            setKeyIndexGun = "gun",
            setKeyIndexHydrogenTank = "htk",
            setKeyIndexOxygenTank = "otk",
            setKeyIndexParachute = "prt",
            setKeyIndexReactor = "rcr",
            setKeyIndexRefinery = "rfy",
            setKeyIndexStorage = "Storage",
            setKeyIndexSortable = "srt",
            setKeyIndexLoadout = "ldt",
            setKeyIndexLogic = "lgc",
            setKeyIndexPanel = "pnl",
            setKeyIndexInventory = "inv",
            setKeyIndexLimit = "lmt";

        const string trueString = "true", falseString = "false";

        const bool shortTrue = true, shortFalse = false;

        const MyAssemblerMode assemblyMode = MyAssemblerMode.Assembly, disassemblyMode = MyAssemblerMode.Disassembly;

        int echoTicks = 10, activeOres = 0, overheatTicks = 0,
            currentErrorCount = 0, totalErrorCount = 0, checkTicks = 0, mergeLengthTolerance = 0, updateFrequency = 1,
            outputLimit = 15, tempProcessPanelOptionSurfaceIndex, tempStorageInventoryIndex, tempStoragePriorityMax,
            tempStorageIndexStart;

        bool
            booted = false, saving, loading, fillingBottles = false,
            reset = false, autoLoadSettings = true, correctScript = false, correctVersion = false,
            scanning = false, allowEcho = true,
            prioritySystemActivated = false, errorFilter = false, useDynamicQuota, increaseDynamicQuotaWhenLow, update = false;

        static readonly string[] functionList = new string[]
        {
            "Main", "Main Control", "Main Output", //0-2
            "Main Sprites", "Processing Block Options", "Status Panel", //3-5
            "Measuring Capacities", "Output Panel", "Counting Listed Items", //6-8
            "Distribution", "Distributing Item", "Counting Item In Inventory", //9-11
            "Processing Limits", "Sorting", "Storing Item", //12-14
            "Counting Blueprints", "Counting Items", "Scanning", //15-17
            "Generating Sprites", "Listing Items", "Item Panel", //18-20
            "Filling Dictionary", "Processing Tags", "Transferring Item", //21-23
            "Spreading Items", "Cargo Panel", "Distributing Blueprint", //24-26
            "Removing Excess Assembly", "Generating Block Options", "Setting Block Quotas", //27-29
            "Save", "Queue Assembly", "Queue Disassembly", // 30-32
            "Inserting Blueprint", "Process Panel Options", "Removing Blueprint", // 33-35
            "Removing Excess Disassembly", "Order Blocks By Priority", "Cargo Priority Loop", // 36-38
            "Sorting Cargo Priority", "Sort Blueprints", "Spread Blueprints", // 39-41
            "Load", "Loadouts", "Sort Refineries", // 42-44
            "Custom Logic", "Matching Items", "Process Logic", // 45-47
            "Checking Idle Assemblers", "Find Mod Items", "Process Setting", // 48-50
            "Main Panel", "Assembly Reserve", "Processing Item Setting", // 51-53
            "Order Storage", "Order Inventory" // 54-55
        };

        int[] outputFunctions = new int[] { 2, 3, 5, 6, 7, 18, 20, 25, 51 };

        static double scriptVersion = 5.27, torchAverage = 0, tickWeight = 0.005;

        double transferredAmount = 0, countedAmount = 0,
            transferAmount, dynamicQuotaMultiplierIncrement, dynamicQuotaMaxMultiplier, dynamicQuotaPositiveThreshold,
            dynamicQuotaNegativeThreshold, balanceRange = 0.05,
            allowedExcessPercent = 0,
            delayResetIdleAssembler = 45,
            scriptHealth = 100, tempTransferAmount, tempStorageMax, tempDistributeItemMax;

        long tempStorageBlockIndex;

        Color defPanelForegroundColor = new Color(0.7019608f, 0.9294118f, 1f, 1f);

        SortedList<string, IEnumerator<FunctionState>> stateList = new SortedList<string, IEnumerator<FunctionState>>();

        const TextAlignment leftAlignment = TextAlignment.LEFT, centerAlignment = TextAlignment.CENTER;

        List<MyInventoryItem> countByListA, amountContainedListA, mainFunctionItemList, tempStorageItemList;

        List<MyProductionItem> blueprintListMain;

        MyInventoryItem tempTransferInventoryItem, tempDistributeItem;

        BlockDefinition mainBlockDefinition, alternateBlockDefinition, storageDefinitionA, blockCheckDefinitionA, tempBlockOptionDefinition,
                        tempDistributeItemBlockDefinition;

        IMyInventory tempTransferOriginInventory;

        EchoMode echoMode = EchoMode.Main;

        enum EchoMode
        {
            Main,
            MergeMenu,
            Functions,
            MergeHelp,
            DelayHelp
        };

        public enum DisplayType
        {
            Detailed,
            CompactAmount,
            Standard,
            CompactPercent
        };

        public enum PanelItemSorting
        {
            Alphabetical,
            AscendingAmount,
            DescendingAmount,
            AscendingPercent,
            DescendingPercent
        };

        public enum PanelType
        {
            Item,
            Cargo,
            Output,
            Status,
            Span,
            None
        };

        enum FunctionState
        {
            Active,
            Continue,
            Complete
        };

        const FunctionState stateActive = FunctionState.Active, stateContinue = FunctionState.Continue, stateComplete = FunctionState.Complete;

        bool PauseTickRun { get { return UnavailableActions(); } }
        bool IsStateActive { get { return StateActive(selfContainedIdentifier); } }
        bool IsStateRunning { get { return StateActive(selfContainedIdentifier, true); } }
        bool RunStateManager { get { return StateManager(selfContainedIdentifier); } }
        double overheatAverage { get { return OverheatAverage; } set { OverheatAverage = value; } }
        double actionLimiterMultiplier { get { return ActionLimiterMultiplier; } set { ActionLimiterMultiplier = value; } }
        double runTimeLimiter { get { return RunTimeLimiter; } set { RunTimeLimiter = value; } }
        int echoDelay { get { return EchoDelay; } }
        static List<long> NewLongList { get { return new List<long>(); } }
        static List<SortableObject> NewSortableObjectList { get { return new List<SortableObject>(); } }
        List<MyInventoryItem> NewItemList { get { return new List<MyInventoryItem>(); } }
        static List<string> NewStringList { get { return new List<string>(); } }
        static List<MyProductionItem> NewProductionList { get { return new List<MyProductionItem>(); } }
        static ItemCollection NewCollection { get { return new ItemCollection(); } }
        static StringBuilder NewBuilder { get { return new StringBuilder(); } }
        static SortedList<long, double> NewSortedListLongDouble { get { return new SortedList<long, double>(); } }
        static SortedList<string, string> NewSortedListStringString { get { return new SortedList<string, string>(); } }
        static HashSet<long> NewHashSetLong { get { return new HashSet<long>(); } }
        static HashSet<string> NewHashSetString { get { return new HashSet<string>(); } }
        static DateTime Now { get { return DateTime.Now; } }
        static TimeSpan ZeroSpan { get { return TimeSpan.Zero; } }
        static TimeSpan scriptSpan = TimeSpan.Zero;
        List<ItemDefinition> GetAllItems
        {
            get
            {
                itemListAllItems.Clear();
                itemListMain.Values.ToList().ForEach(b => itemListAllItems.AddRange(b.Values));
                return itemListAllItems;
            }
        }

        PanelMasterClass panelMaster = new PanelMasterClass();


        #endregion


        #region Main


        Program()
        {
            gtSystem = GridTerminalSystem;
            ItemCollection.parent = this;
            PanelMasterClass.parent = this;
            SetConstants();

            countByListA = NewItemList;
            amountContainedListA = NewItemList;
            mainFunctionItemList = NewItemList;

            blueprintListMain = NewProductionList;

            itemCollectionProcessSetting = NewCollection;
            itemCollectionMain = NewCollection;
            itemCollectionAlternate = NewCollection;
            itemCollectionProcessTotalLoadout = NewCollection;

            loading = TextHasLength(Me.CustomData);
            saving = !loading;

            FillDict();

            Scan(); Scan();

            ControlScript(); ControlScript();
        }

        void SetConstants()
        {
            //Strings
            exclusionKeyword = GetKeyString(setKeyExclusion);
            ingotKeyword = GetKeyString(setKeyIngot);
            oreKeyword = GetKeyString(setKeyOre);
            componentKeyword = GetKeyString(setKeyComponent);
            ammoKeyword = GetKeyString(setKeyAmmo);
            toolKeyword = GetKeyString(setKeyTool);
            globalFilterKeyword = GetKeyString(setKeyGlobalFilter);
            panelTag = GetKeyString(setKeyPanel);
            crossGridKeyword = GetKeyString(setKeyCrossGrid);
            includeGridKeyword = GetKeyString(setKeyIncludeGrid);
            excludeGridKeyword = GetKeyString(setKeyExcludeGrid);
            optionBlockFilter = GetKeyString(setKeyOptionBlockFilter);

            //Lists
            if (itemCategoryList.Count == 0)
                itemCategoryList.AddRange(new List<string> { ingotKeyword, oreKeyword, componentKeyword, toolKeyword, ammoKeyword });

            //Bools
            autoLoadSettings = GetKeyBool(setKeyToggleAutoLoadSettings);
            useDynamicQuota = GetKeyBool(setKeyToggleDynamicQuota);
            increaseDynamicQuotaWhenLow = GetKeyBool(setKeyDynamicQuotaIncreaseWhenLow);

            //Doubles
            dynamicQuotaMultiplierIncrement = GetKeyDouble(setKeyDynamicQuotaPercentageIncrement);
            dynamicQuotaMaxMultiplier = GetKeyDouble(setKeyDynamicuotaMaxMultiplier);
            dynamicQuotaPositiveThreshold = GetKeyDouble(setKeyDynamicQuotaPositiveThreshold);
            dynamicQuotaNegativeThreshold = GetKeyDouble(setKeyDynamicQuotaNegativeThreshold);
            allowedExcessPercent = GetKeyDouble(setKeyAllowedExcessPercent);
            balanceRange = GetKeyDouble(setKeyBalanceRange);
            delayResetIdleAssembler = GetKeyDouble(setKeyDelayResetIdleAssembler);

            //Ints
            updateFrequency = settingsInts[setKeyUpdateFrequency];
            updateFrequency = updateFrequency == 1 || updateFrequency == 10 || updateFrequency == 100 ? updateFrequency : 1;

            //Presets
            StringBuilder builder = NewBuilder;
            builder.Append("All");
            itemCategoryList.ForEach(category => builder.Append($"|{Formatted(category)}"));
            itemCategoryString = builder.ToString();

            presetPanelOptions = PanelMasterClass.PresetPanelOption(itemCategoryString);

            ResetRuntimes();
            outputLimit = settingsInts[setKeyOutputLimit];
            mergeLengthTolerance = settingsInts[setKeyAutoMergeLengthTolerance];
        }

        void SetPostLoad()
        {
            actionLimiterMultiplier = GetKeyDouble(setKeyActionLimiterMultiplier);
            runTimeLimiter = GetKeyDouble(setKeyRunTimeLimiter);
            overheatAverage = GetKeyDouble(setKeyOverheatAverage);
        }

        void Main(string argument)
        {
            if ((!reset && !update) || saving)
            {
                try
                {
                    tickStartTime = Now;
                    bool handledCommand = false;
                    scriptSpan += Runtime.TimeSinceLastRun;
                    torchAverage = TorchAverage(torchAverage, Runtime.LastRunTimeMs);
                    if (autoLoadSettings && !reset)
                    {
                        checkTicks++;
                        if (checkTicks >= 600)
                        {
                            if (!StringsMatch(Me.CustomData.Trim(), settingBackup))
                            {
                                loading = true;
                                checkTicks = -300;
                            }
                            else checkTicks = 0;
                        }
                    }
                    try
                    {
                        if (echoMode != EchoMode.MergeMenu && TextHasLength(argument))
                        {
                            if (!saving && !loading)
                                Commands(argument);
                            else
                                SetLastString("Please wait until save/load completes to run commands");
                            handledCommand = true;
                        }
                    }
                    catch
                    {
                        Output($"Error running command: {argument}");
                    }
                    if (overheatAverage <= 0 || torchAverage <= overheatAverage)
                    {
                        overheatTicks = 0;
                        Script();
                    }
                    else overheatTicks++;
                    echoTicks += updateFrequency;
                    if (echoMode == EchoMode.MergeMenu)
                        MergeCommand(!handledCommand ? argument : "");
                    if (allowEcho && echoTicks >= echoDelay)
                        try
                        {
                            echoTicks = 0;
                            switch (echoMode)
                            {
                                case EchoMode.Main:
                                    MainEcho();
                                    break;
                                case EchoMode.DelayHelp:
                                    DelayHelp();
                                    break;
                                case EchoMode.Functions:
                                    FunctionHelp();
                                    break;
                                case EchoMode.MergeHelp:
                                    MergeHelp();
                                    break;
                                case EchoMode.MergeMenu:
                                    MergingMenu();
                                    break;
                            }
                            PadEcho();
                        }
                        catch { Output("Error caught in echo"); }
                }
                catch { Output("Error in main"); }
            }
            else
            {
                if (reset)
                    Echo("Please recompile to complete reset!");
                if (update)
                    Echo("Remove any settings you want to update/reset and recompile");
            }
        }

        void FunctionHelp()
        {
            Echo($"--Functions List ({stateErrorCodes.Count}/{stateRecords.Count})--");
            Echo("--Enter 'functions?' to hide--");
            string key;
            double ticks;
            bool active, showRange;
            StateRecord record;
            foreach (KeyValuePair<string, StateRecord> kvp in stateRecords)
            {
                record = kvp.Value;
                key = kvp.Key;
                ticks = record.lastTicks;
                active = StateActive(key, true);
                showRange = record.runs > 1;

                Echo($"{key}{(active ? "*" : " ")} {kvp.Value.health:N2}%");

                Echo($"-Ticks: {ShortNumber2(ticks)}{(active ? $" : {ShortNumber2(record.currentTicks)}" : "")}");

                if (showRange) Echo($"--({ShortNumber2(record.minTicks)}-{ShortNumber2(record.maxTicks)})");

                Echo($"-Time: {ShortMSTime(record.lastSpan.TotalMilliseconds)}{(active ? $" : {ShortMSTime(record.currentSpan.TotalMilliseconds)}" : "")}");
                if (showRange) Echo($"--({ShortMSTime(record.minSpan.TotalMilliseconds)}-{ShortMSTime(record.maxSpan.TotalMilliseconds)})");

                Echo($"-Actions: {ShortNumber2(record.lastActions)}{(active ? $" : {ShortNumber2(record.currentActions)}ops" : "")}");
                if (showRange) Echo($"--({ShortNumber2(record.minActions)}-{ShortNumber2(record.maxActions)})");

                if (ticks > 0) Echo($"-Avg: {record.averageTime:N2}ms, {Math.Ceiling(record.averageActions)}ops");

                Echo($"-Runs: {ShortNumber2(record.runs)}");
            }
        }

        void MergeCommand(string argument)
        {
            if (TextHasLength(argument))
            {
                string subArg = RemoveSpaces(argument, true);
                if (subArg == "merge")
                {
                    mergeItem = "";
                    echoMode = EchoMode.Main;
                    SetLastString("Closed Merge Menu");
                    return;
                }
                int index;
                if (int.TryParse(argument, out index))
                {
                    index--;
                    if (!TextHasLength(mergeItem))
                    {
                        if (index < modItemDictionary.Count)
                            mergeItem = modItemDictionary.Keys[index];
                    }
                    else if (index < modBlueprintList.Count)
                    {
                        UpdateItemDef(mergeItem, modBlueprintList[index]);
                        mergeItem = "";
                        echoMode = modItemDictionary.Count > 0 && modBlueprintList.Count > 0 ? EchoMode.MergeMenu : EchoMode.Main;
                        saving = true;
                    }
                }
            }
        }

        void MergingMenu()
        {
            Echo("--Merging Menu--");
            Echo("--Enter 'merge' to cancel--");
            if (!TextHasLength(mergeItem))
            {
                Echo("Choose Item");
                for (int i = 0, max = modItemDictionary.Count; i < max; i++)
                    Echo($"{i + 1} : {modItemDictionary.Values[i]}");

                if (modItemDictionary.Count == 0)
                    echoMode = EchoMode.Main;
            }
            else
            {
                Echo($"Choose Blueprint For {mergeItem}");
                for (int i = 0, max = modBlueprintList.Count; i < max; i++)
                    Echo($"{i + 1} : {modBlueprintList[i]}");

                if (modBlueprintList.Count == 0)
                {
                    echoMode = EchoMode.Main;
                    mergeItem = "";
                }
            }
        }

        void MergeHelp()
        {
            Echo("--Merge Help List--");
            Echo("--Enter 'merge?' to hide--");
            Echo("--Enter 'merge' to begin merge--");
            for (int i = 0; i < modItemDictionary.Count; i++)
                Echo($"ITM: {modItemDictionary.Values[i]}");

            for (int i = 0; i < modBlueprintList.Count; i++)
                Echo($"BPT: {modBlueprintList[i]}");
        }

        void DelayHelp()
        {
            Echo("--Delay List--");
            Echo("--Enter 'delays?' to hide--");
            sortableListDelayHelp.Clear();
            sortableListDelayHelp.Add(new SortableObject { amount = RemainingSpan(scanSpan), text = "Scan and Process" });
            sortableListDelayHelp.Add(new SortableObject { amount = RemainingSpan(fillBottleSpan), text = "Fill Bottles" });
            if (autoLoadSettings)
                sortableListDelayHelp.Add(new SortableObject { amount = 15.0 - ((double)checkTicks / 60.0), text = "Reload" });

            foreach (KeyValuePair<string, TimeSpan> kvp in delaySpans)
                sortableListDelayHelp.Add(new SortableObject { amount = RemainingSpan(kvp.Value), text = $"{kvp.Key}" });

            sortableListDelayHelp = sortableListDelayHelp.OrderBy(x => x.amount).ToList();
            foreach (SortableObject sortableObject in sortableListDelayHelp)
                Echo($"{sortableObject.text}: {Math.Ceiling(sortableObject.amount)}s");
        }

        void MainEcho()
        {
            Echo($"Main: {currentMajorFunction}");
            Echo($"Current: {(TextHasLength(selfContainedIdentifier) ? selfContainedIdentifier : currentFunction)}");
            Echo($"Last: {Round(Runtime.LastRunTimeMs, 4)}");
            Echo($"Avg: {Round(torchAverage, 4)}");
            Echo($"Blocks: {managedBlocks.Count}");
            Echo($"Panels: {typedIndexes[setKeyIndexPanel].Count}");
            Echo($"Active Functions: {stateErrorCodes.Count}");
            if (modItemDictionary.Count + modBlueprintList.Count > 0)
            {
                Echo($"Mod Items: {modItemDictionary.Count}");
                Echo($"Mod Blueprints: {modBlueprintList.Count}");
                if (modItemDictionary.Count > 0 && modBlueprintList.Count > 0)
                    Echo("-Enter 'merge' to begin merge");
            }
            Echo(overheatTicks > 0 ? $"Overheat Ticks: {overheatTicks}" : "");

            if (TextHasLength(lastString))
            {
                Echo($"Last: {lastString}");
                if (Now >= lastActionClearTime)
                    lastString = "";
            }

            Echo("");
            Echo($"Uptime: {scriptSpan.ToString("c")}");
        }

        void Commands(string argument)
        {
            string arg = argument.ToLower(), subArg, key, name, data;
            bool handled = true;
            SetLastString($"Running argument: {argument}");
            subArg = RemoveSpaces(arg);
            SplitData(arg, out key, out data, ' ');
            string value;
            switch (subArg)
            {
                case "save":
                    if (!loading)
                    {
                        SetLastString("Started save process");
                        saving = true;
                    }
                    else
                        SetLastString("Load process is active, please wait to save!");
                    break;
                case "load":
                    if (!saving)
                    {
                        SetLastString("Started load process");
                        loading = true;
                    }
                    else
                        SetLastString("Save process is active, please wait to load!");
                    break;
                case "clearqueue":
                    foreach (long index in typedIndexes[setKeyIndexAssemblers])
                        if (IsBlockOk(index))
                            ((IMyAssembler)managedBlocks[index].block).ClearQueue();
                    SetLastString("Assembler queues cleared");
                    break;
                case "reset":
                    Me.CustomData = "";
                    reset = true;
                    saving = true;
                    SetLastString("Save and reset process started");
                    break;
                case "update":
                    saving = true;
                    update = true;
                    SetLastString("Save and update process started");
                    break;
                case "clearfunctions":
                    ClearFunctions();
                    SetLastString("Active processes stopped");
                    break;
                case "merge?":
                    echoMode = (echoMode == EchoMode.Main && modItemDictionary.Count + modBlueprintList.Count > 0) ? EchoMode.MergeHelp : EchoMode.Main;
                    SetLastString(echoMode == EchoMode.MergeHelp ? "Opened Merge Help List" : "Closed Merge Help List");
                    break;
                case "merge":
                    echoMode = (echoMode == EchoMode.Main && modItemDictionary.Count > 0 && modBlueprintList.Count > 0) ? EchoMode.MergeMenu : EchoMode.Main;
                    SetLastString(echoMode == EchoMode.MergeMenu ? "Opened Merge Menu" : "Closed Merge Menu");
                    break;
                case "functions?":
                    echoMode = echoMode == EchoMode.Main ? EchoMode.Functions : EchoMode.Main;
                    break;
                case "delays?":
                    echoMode = echoMode == EchoMode.Main ? EchoMode.DelayHelp : EchoMode.Main;
                    break;
                case "scan":
                    scanSpan = SpanDelay();
                    delaySpans.Clear();
                    ClearFunctions();
                    SetLastString("Functions and delays reset");
                    break;
                case "echo":
                    allowEcho = !allowEcho;
                    Echo($"Echo Allowed: {allowEcho}");
                    break;
                case "error":
                    errorFilter = !errorFilter;
                    SetLastString(errorFilter ? "Error filter enabled, use 'error' to disable" : "Error filter disabled");
                    break;
                case "full":
                    for (int i = 0; i < settingDictionaryBools.Count; i++)
                        for (int x = 0; x < settingDictionaryBools.Values[i].Count; x++)
                        {
                            value = settingDictionaryBools.Values[i].Keys[x];
                            SetKeyBool(value, i < 2 || (!LeadsString(value, "useconveyor") && !fullExclude.Contains(value)));
                        }

                    SetLastString("All functions");
                    saving = true;
                    break;
                case "basic":
                    for (int i = 0; i < settingDictionaryBools.Count; i++)
                        for (int x = 0; x < settingDictionaryBools.Values[i].Count; x++)
                            SetKeyBool(settingDictionaryBools.Values[i].Keys[x], i == 0);

                    SetLastString("Basic functions only");
                    saving = true;
                    break;
                case "monitor":
                    for (int i = 0; i < settingDictionaryBools.Count; i++)
                        for (int x = 0; x < settingDictionaryBools.Values[i].Count; x++)
                        {
                            value = settingDictionaryBools.Values[i].Keys[x];
                            SetKeyBool(value, value == setKeyToggleAutoLoadSettings || LeadsString(value, "useconveyor") || LeadsString(value, "count"));
                        }
                    SetLastString("Monitoring only");
                    saving = true;
                    break;
                default:
                    if (!TextHasLength(key))
                        SetLastString($"Unhandled command: {argument}");
                    handled = false;
                    break;
            }

            if (!handled && TextHasLength(key))
                switch (key)
                {
                    case "setgroup":
                        name = data.Substring(0, data.IndexOf(" "));
                        data = data.Substring(data.IndexOf(" ") + 1);
                        SetGroup(name, data);
                        saving = true;
                        break;
                    case "set":
                        if (SplitData(data, out name, out data, ' ', false))
                        {
                            SetItemQuotaMain(name, data);
                            saving = true;
                        }
                        break;
                    default:
                        SetLastString($"Unhandled command: {argument}");
                        break;
                }
        }


        #endregion


        #region State Functions

        bool OrderCargo()
        {
            selfContainedIdentifier = functionList[54];
            if (!IsStateActive)
            {
                InitializeState(OrderCargoState(), selfContainedIdentifier);
                if (PauseTickRun) return false;
            }
            return RunStateManager;
        }

        IEnumerator<FunctionState> OrderCargoState()
        {
            IMyInventory inventory;
            while (true)
            {
                foreach (long index in typedIndexes[setKeyIndexStorage])
                {
                    if (PauseTickRun) yield return stateActive;
                    if (!IsBlockOk(index)) continue;

                    inventory = managedBlocks[index].Input;
                    mainFunctionItemList.Clear();
                    inventory.GetItems(mainFunctionItemList);

                    sortableListMain.Clear();
                    foreach (MyInventoryItem item in mainFunctionItemList)
                    {
                        if (PauseTickRun) yield return stateActive;
                        sortableListMain.Add(new SortableObject { text = item.Type.ToString(), key = $"{GetItemCategory(item.Type.ToString())}|{ItemName(item)}" });
                    }
                    sortableListMain = sortableListMain.OrderBy(x => x.key).ToList();

                    while (!OrderInventory(sortableListMain, inventory))
                        yield return stateActive;
                }

                yield return stateContinue;
            }
        }

        bool OrderInventory(List<SortableObject> expectedOrder, IMyInventory inventory)
        {
            if (expectedOrder.Count <= 1) return true;
            selfContainedIdentifier = functionList[55];
            if (!IsStateActive)
            {
                InitializeState(OrderInventoryState(expectedOrder, inventory), selfContainedIdentifier);
                if (PauseTickRun) return false;
            }
            return RunStateManager;
        }

        IEnumerator<FunctionState> OrderInventoryState(List<SortableObject> expectedOrder, IMyInventory inventory)
        {
            for (int x = 0; x < expectedOrder.Count; x++)
                for (int z = x; z < inventory.ItemCount; z++)
                {
                    if (PauseTickRun) yield return stateActive;

                    try
                    {
                        MyInventoryItem item = (MyInventoryItem)inventory.GetItemAt(z);
                        if (item.Type.ToString() == expectedOrder[x].text)
                        {
                            if (x != z)
                                inventory.TransferItemFrom(inventory, z, x, false, item.Amount);
                            break;
                        }
                    }
                    catch { }
                }

            yield return stateComplete;
        }

        bool ProcessItemSetting(string setting)
        {
            selfContainedIdentifier = functionList[53];
            if (!IsStateRunning)
                tempItemSetting = setting;
            if (!IsStateActive)
            {
                InitializeState(ProcessItemSettingState(), selfContainedIdentifier);
                if (PauseTickRun) return false;
            }
            return RunStateManager;
        }

        IEnumerator<FunctionState> ProcessItemSettingState()
        {
            double quota, dataNumber, quotaMaxAmount;
            bool acquiredDefinition, dataBool;
            int index;
            string subSetting, key, data, typeID, subtypeID;
            string[] subSettingArray;

            while (true)
            {
                subSetting = tempItemSetting.Replace("||", "~");
                typeID = subtypeID = "";
                subSettingArray = subSetting.Split('~');

                ItemDefinition definition = new ItemDefinition();
                acquiredDefinition = false;
                for (int i = 0; i < subSettingArray.Length; i++)
                {
                    if (PauseTickRun) yield return stateActive;

                    if (SplitData(subSettingArray[i], out key, out data))
                    {
                        key = key.ToLower();
                        if (key == "type")
                            typeID = data;
                        else if (key == "subtype")
                            subtypeID = data;
                    }
                    if (TextHasLength(typeID) && TextHasLength(subtypeID)) break;
                }

                if (TextHasLength(typeID) && TextHasLength(subtypeID))
                {
                    AddItemDef(subtypeID, subtypeID, typeID, "");
                    acquiredDefinition = GetDefinition(out definition, $"{typeID}/{subtypeID}");
                }

                if (acquiredDefinition)
                {
                    for (int i = 0; i < subSettingArray.Length; i++)
                    {
                        if (PauseTickRun) yield return stateActive;

                        if (SplitData(subSettingArray[i], out key, out data))
                        {
                            dataBool = StringsMatch(data, trueString);
                            switch (RemoveSpaces(key, true).Trim())
                            {
                                case "name":
                                    definition.displayName = data;
                                    break;
                                case "quota":
                                    index = data.IndexOf("<");
                                    if (index > 0)
                                    {
                                        if (double.TryParse(data.Substring(0, index), out quota) &&
                                            double.TryParse(data.Substring(index + 1), out quotaMaxAmount))
                                        {
                                            definition.quota = quota;
                                            if (quotaMaxAmount < quota)
                                                quotaMaxAmount = quota;

                                            if (quotaMaxAmount < 0)
                                                quotaMaxAmount = 0;

                                            definition.quotaMax = quotaMaxAmount;
                                        }
                                    }
                                    else if (double.TryParse(data, out quota))
                                    {
                                        definition.quota = quota;
                                        if (quota < 0)
                                            quota = 0;

                                        definition.quotaMax = quota;
                                    }
                                    break;
                                case "category":
                                    data = data.ToLower();
                                    definition.category = data;
                                    AddCategory(data);
                                    break;
                                case "blueprint":
                                    if (IsBlueprint(definition.blueprintID))
                                        blueprintList.Remove(definition.blueprintID);

                                    definition.blueprintID = data;
                                    if (TextHasLength(data))
                                        modItemDictionary.Remove(definition.FullID);

                                    break;
                                case "assemblymultiplier":
                                    if (double.TryParse(data, out dataNumber))
                                        definition.assemblyMultiplier = dataNumber;

                                    break;
                                case "assemble":
                                    definition.assemble = dataBool;
                                    break;
                                case "disassemble":
                                    definition.disassemble = dataBool;
                                    break;
                                case "refine":
                                    definition.refine = dataBool;
                                    break;
                                case "display":
                                    definition.display = dataBool;
                                    break;
                                case "orekeys":
                                    string[] oreKeys = data.Substring(1, data.Length - 2).Split('|');
                                    if (oreKeys.Length > 0)
                                    {
                                        definition.oreKeys.Clear();
                                        definition.oreKeys.AddRange(oreKeys);
                                    }
                                    if (definition.oreKeys.Count == 0 && IsIngot(definition.typeID))
                                        definition.oreKeys.Add(subtypeID);

                                    break;
                                case "fuel":
                                    definition.fuel = dataBool;
                                    break;
                                case "gas":
                                    definition.gas = dataBool;
                                    break;
                            }
                        }
                    }

                    if (IsBlueprint(definition.blueprintID))
                        blueprintList[definition.blueprintID] = ItemToBlueprint(definition);

                    itemCategoryDictionary[definition.FullID] = definition.category;
                    FinalizeKeys(ref definition);
                    CheckModdedItem(definition);
                }

                yield return stateContinue;
            }
        }

        bool Transfer(ref double transferAmount, IMyInventory originInventory, BlockDefinition destinationBlock, MyInventoryItem item)
        {
            selfContainedIdentifier = functionList[23];
            if (!IsStateRunning)
            {
                tempTransferAmount = transferAmount;
                tempTransferOriginInventory = originInventory;
                alternateBlockDefinition = destinationBlock;
                tempTransferInventoryItem = item;
            }
            if (!IsStateActive)
                InitializeState(TransferState(), selfContainedIdentifier);
            bool done = RunStateManager;

            if (done)
            {
                transferAmount -= transferredAmount;
                transferredAmount = 0;
                return true;
            }

            return false;
        }

        IEnumerator<FunctionState> TransferState()
        {
            IMyInventory destinationInventory;
            while (true)
            {
                destinationInventory = alternateBlockDefinition.Input;
                if (destinationInventory != tempTransferOriginInventory)
                {
                    bool isLimited, stopFunc = false;
                    double itemLimit = alternateBlockDefinition.Settings.limits.ItemCount(out isLimited, tempTransferInventoryItem, alternateBlockDefinition.block), contained = 0, volumeLimit = GetCurrentVolumeLimit(tempTransferInventoryItem, alternateBlockDefinition.block), currentTransferAmount = tempTransferAmount;

                    if (isLimited)
                    {
                        if (itemLimit <= 0)
                            stopFunc = true;
                        else
                        {
                            while (!AmountContained(ref contained, tempTransferInventoryItem, alternateBlockDefinition.block))
                                yield return stateActive;

                            if (contained >= itemLimit)
                                stopFunc = true;
                            else if (currentTransferAmount + contained > itemLimit)
                                currentTransferAmount = itemLimit - contained;
                        }
                    }
                    if (!stopFunc)
                    {
                        if (currentTransferAmount > volumeLimit)
                            currentTransferAmount = volumeLimit;

                        if (!FractionalItem(tempTransferInventoryItem))
                            currentTransferAmount = Math.Floor(currentTransferAmount);

                        if (currentTransferAmount > 0.0 && destinationInventory.TransferItemFrom(tempTransferOriginInventory, tempTransferInventoryItem, (MyFixedPoint)currentTransferAmount))
                        {
                            if (currentTransferAmount >= 0.01)
                                Output($"Moved {ShortNumber2(currentTransferAmount),-6} {ShortenName(ItemName(tempTransferInventoryItem), 12),-12} to {ShortenName(alternateBlockDefinition.block.CustomName, 12),-12}");

                            transferredAmount = currentTransferAmount;
                        }
                    }
                }
                yield return stateContinue;
            }
        }

        bool ProcessSetting(string setting)
        {
            selfContainedIdentifier = functionList[50];
            if (!IsStateActive)
            {
                InitializeState(ProcessSettingState(setting), selfContainedIdentifier);
                if (PauseTickRun) return false;
            }
            return RunStateManager;
        }

        IEnumerator<FunctionState> ProcessSettingState(string setting)
        {
            int index = setting.IndexOf("=");
            string stringValue, groupName;
            if (index != -1)
            {
                string key = setting.Substring(0, index).Trim();
                stringValue = setting.Substring(index + 1).Trim();
                if (StringsMatch(key, "name"))
                {
                    while (!ProcessItemSetting(setting))
                        yield return stateActive;
                }
                else if (key == "group")
                {
                    index = stringValue.IndexOf("=");
                    groupName = stringValue.Substring(0, index).ToLower();
                    stringValue = stringValue.Substring(index + 1).Trim();
                    itemCollectionProcessSetting.Clear();
                    while (!GetTags(itemCollectionProcessSetting, stringValue))
                        yield return stateActive;

                    if (TextHasLength(groupName) && itemCollectionProcessSetting.ItemTypeCount != 0)
                        customItemGroups[groupName] = itemCollectionProcessSetting;
                }
                else
                {
                    stringValue = setting.Substring(index + 1).Trim();
                    double doubleValue;
                    bool boolValue = !StringsMatch(stringValue, falseString);
                    if (!double.TryParse(stringValue, out doubleValue))
                        doubleValue = 0;

                    if (key == "script")
                    {
                        if (stringValue == scriptName)
                            correctScript = true;
                    }
                    else if (key == "version")
                    {
                        if (doubleValue == scriptVersion)
                            correctVersion = true;
                    }
                    else if (settingsInts.ContainsKey(key))
                        settingsInts[key] = (int)doubleValue;
                    else if (!SetKeyString(key, stringValue) && !SetKeyDouble(key, doubleValue) && !SetKeyBool(key, boolValue) && settingsListsStrings.ContainsKey(key) && LeadsString(stringValue, "[") && EndsString(stringValue, "]"))
                    {
                        stringValue = stringValue.Substring(1, stringValue.Length - 2);
                        string[] valueArray = stringValue.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                        if (clearedSettingLists.Add(key))
                            settingsListsStrings[key].Clear();
                        for (int i = 0; i < valueArray.Length; i++)
                        {
                            if (PauseTickRun)
                                yield return stateActive;

                            settingsListsStrings[key].Add(valueArray[i]);
                        }
                    }
                }
            }
            yield return stateComplete;
        }

        bool FindModItems()
        {
            selfContainedIdentifier = functionList[49];
            if (!IsStateActive)
            {
                InitializeState(FindModItemState(), selfContainedIdentifier);
                if (PauseTickRun) return false;
            }
            return RunStateManager;
        }

        IEnumerator<FunctionState> FindModItemState()
        {
            while (true)
            {
                foreach (long index in typedIndexes[setKeyIndexAssemblers])
                {
                    if (PauseTickRun) yield return stateActive;
                    if (!IsBlockOk(index)) continue;


                    blueprintListMain.Clear();
                    ((IMyAssembler)managedBlocks[index].block).GetQueue(blueprintListMain);
                    for (int y = 0; y < blueprintListMain.Count; y++)
                    {
                        if (PauseTickRun) yield return stateActive;

                        if (UnknownBlueprint(blueprintListMain[y]))
                            AddModBlueprint(blueprintListMain[y]);
                    }
                }
                foreach (long index in typedIndexes[setKeyIndexInventory])
                {
                    if (PauseTickRun) yield return stateActive;
                    if (!IsBlockOk(index)) continue;


                    for (int inv = 0; inv < managedBlocks[index].block.InventoryCount; inv++)
                    {
                        mainFunctionItemList.Clear();
                        managedBlocks[index].Input.GetItems(mainFunctionItemList);
                        for (int y = 0; y < mainFunctionItemList.Count; y++)
                        {
                            if (PauseTickRun) yield return stateActive;

                            if (UnknownItem(mainFunctionItemList[y]))
                            {
                                Output($"Unknown Item: {ShortenName(mainFunctionItemList[y].Type.SubtypeId, 14)}, found in: {ShortenName(managedBlocks[index].block.CustomName, 14)}");
                                AddModItem(mainFunctionItemList[y]);
                            }
                        }
                    }
                }
                yield return stateContinue;
            }
        }

        bool CheckIdleAssemblers()
        {
            selfContainedIdentifier = functionList[48];
            if (!IsStateActive)
            {
                InitializeState(IdleAssemblerState(), selfContainedIdentifier);
                if (PauseTickRun) return false;
            }
            return RunStateManager;
        }

        IEnumerator<FunctionState> IdleAssemblerState()
        {
            IMyAssembler assembler;
            MonitoredAssembler monitoredAssembler;
            SortedList<string, double> productionComparison = new SortedList<string, double>();
            List<MyProductionItem> productionList = NewProductionList;
            bool changed;
            IMyInventory inventory;
            while (true)
            {
                foreach (long index in typedIndexes[setKeyIndexAssemblers])
                {
                    if (PauseTickRun) yield return stateActive;
                    if (!IsBlockOk(index) || managedBlocks[index].Settings.NoIdleReset)
                        continue;

                    monitoredAssembler = managedBlocks[index].monitoredAssembler;
                    assembler = (IMyAssembler)managedBlocks[index].block;

                    inventory = assembler.GetInventory(assembler.Mode == assemblyMode ? 0 : 1);
                    changed = monitoredAssembler.Check(delayResetIdleAssembler);

                    if (!changed)
                    {
                        assembler.GetQueue(productionList);
                        foreach (MyProductionItem myProductionItem in productionList)
                        {
                            if (PauseTickRun) yield return stateActive;
                            if (!productionComparison.ContainsKey(BlueprintSubtype(myProductionItem)))
                                productionComparison[BlueprintSubtype(myProductionItem)] = (double)myProductionItem.Amount;
                            else
                                productionComparison[BlueprintSubtype(myProductionItem)] += (double)myProductionItem.Amount;
                        }
                        productionList.Clear();
                        foreach (KeyValuePair<string, double> kvp in productionComparison)
                        {
                            if (PauseTickRun) yield return stateActive;
                            if (!monitoredAssembler.productionComparison.ContainsKey(kvp.Key) ||
                                monitoredAssembler.productionComparison[kvp.Key] != kvp.Value)
                            {
                                changed = true;
                                break;
                            }
                        }
                    }
                    else changed = false;
                    if (!changed)
                    {
                        if (monitoredAssembler.stalling)
                        {
                            assembler.ClearQueue();
                            mainFunctionItemList.Clear();
                            assembler.GetInventory(0).GetItems(mainFunctionItemList);
                            while (!PutInStorage(mainFunctionItemList, index, 0)) yield return stateActive;

                            mainFunctionItemList.Clear();
                            assembler.GetInventory(1).GetItems(mainFunctionItemList);
                            while (!PutInStorage(mainFunctionItemList, index, 1)) yield return stateActive;
                            monitoredAssembler.Reset();
                        }
                        else monitoredAssembler.stalling = true;
                        monitoredAssembler.productionComparison.Clear();
                    }
                    else
                        monitoredAssembler.Reset();
                    foreach (KeyValuePair<string, double> kvp in productionComparison)
                        monitoredAssembler.productionComparison[kvp.Key] = kvp.Value;
                    productionComparison.Clear();
                }
                yield return stateContinue;
            }
        }

        bool MatchItems(ItemCollection collection, string category, string name, bool append = true, string amount = "0", bool acceptZero = true)
        {
            selfContainedIdentifier = functionList[46];
            if (!IsStateActive)
            {
                InitializeState(MatchingItemState(collection, GetAllItems, category, name, append, amount, acceptZero), selfContainedIdentifier);
                if (PauseTickRun) return false;
            }
            return RunStateManager;
        }

        IEnumerator<FunctionState> MatchingItemState(ItemCollection collection, List<ItemDefinition> itemList, string category, string name, bool append, string amount, bool acceptZero)
        {
            if (TextHasLength(name))
            {
                ItemDefinition definition;
                bool match, percentage;
                double calcedAmount;
                for (int i = 0; i < itemList.Count; i++)
                {
                    if (PauseTickRun) yield return stateActive;

                    if (IsWildCard(category) || category == itemList[i].category)
                    {
                        definition = itemList[i];
                        match = name.Length > 2 && LeadsString(name, "'") && EndsString(name, "'");

                        if (IsWildCard(name) || (match && StringsMatch(RemoveSpaces(definition.displayName), name.Substring(1, name.Length - 2))) || (!match && LeadsString(definition.displayName, name)))
                        {
                            if ((percentage = EndsString(amount, "%")) && double.TryParse(amount.Substring(0, amount.Length - 1), out calcedAmount))
                                calcedAmount /= 100.0;
                            else if (!double.TryParse(amount, out calcedAmount))
                                calcedAmount = 0;
                            collection.AddItem(definition.typeID, definition.subtypeID, new VariableItemCount(calcedAmount, percentage, true), append);
                        }
                    }
                }
                if (!acceptZero)
                    for (int i = 0; i < collection.itemList.Count; i += 0)
                    {
                        if (PauseTickRun) yield return stateActive;

                        if (collection.itemList.Values[i].count <= 0)
                            collection.itemList.RemoveAt(i);
                        else
                            i++;
                    }
            }

            yield return stateComplete;
        }

        bool ProcessTimer(List<LogicComparison> logicComparisons, string data)
        {
            selfContainedIdentifier = functionList[47];
            if (!IsStateActive)
            {
                InitializeState(ProcessTimerState(logicComparisons, data), selfContainedIdentifier);
                if (PauseTickRun) return false;
            }
            return RunStateManager;
        }

        IEnumerator<FunctionState> ProcessTimerState(List<LogicComparison> logicComparisons, string data)
        {
            logicSetsList.Clear();
            string customData = RemoveSpaces(data), typeID = "", subtypeID = "", comparison = ">", substring = "";
            string[] logicSetArray;
            int tempIndex, collectionCount, comparisonCollectionCount;

            if (PauseTickRun) yield return stateActive;

            logicSetArray = customData.Split('|');

            for (int x = 0; x < logicSetArray.Length; x++)
            {
                if (PauseTickRun) yield return stateActive;

                logicSetsList.Add(logicSetArray[x]);
            }

            if (logicSetsList.Count > 0)
            {
                for (int x = 0; x < logicSetsList.Count; x++)
                {
                    if (PauseTickRun) yield return stateActive;

                    try
                    {
                        substring = logicSetsList[x];
                        tempIndex = substring.IndexOf(":");
                        typeID = substring.Substring(0, tempIndex);
                        substring = substring.Substring(tempIndex + 1);

                        comparison =
                            substring.Contains(">=") ? ">=" :
                            substring.Contains("<=") ? "<=" :
                            substring.Contains("<") ? "<" :
                            substring.Contains("=") ? "=" : ">";

                        tempIndex = substring.IndexOf(comparison);
                        subtypeID = substring.Substring(0, tempIndex);
                        substring = substring.Substring(tempIndex + comparison.Length);
                    }
                    catch { }
                    itemCollectionMain.Clear();
                    while (!MatchItems(itemCollectionMain, typeID, subtypeID))
                        yield return stateActive;

                    collectionCount = itemCollectionMain.ItemTypeCount;
                    tempIndex = substring.IndexOf(":");
                    if (tempIndex > 0)
                    {
                        itemCollectionAlternate.Clear();
                        typeID = substring.Substring(0, tempIndex);
                        subtypeID = substring.Substring(tempIndex + 1);
                        while (!MatchItems(itemCollectionAlternate, typeID, subtypeID))
                            yield return stateActive;

                        comparisonCollectionCount = itemCollectionAlternate.ItemTypeCount;
                        for (int i = 0; i < collectionCount; i++)
                        {
                            for (int y = 0; y < comparisonCollectionCount; y++)
                            {
                                if (PauseTickRun) yield return stateActive;

                                logicComparisons.Add(new LogicComparison { typeID = itemCollectionMain.ItemIDByIndex(i), compareAgainst = itemCollectionAlternate.ItemIDByIndex(y), comparison = comparison });
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < collectionCount; i++)
                        {
                            if (PauseTickRun) yield return stateActive;

                            logicComparisons.Add(new LogicComparison { typeID = itemCollectionMain.ItemIDByIndex(i), compareAgainst = substring, comparison = comparison });
                        }
                    }
                }
            }
            yield return stateComplete;
        }

        bool Loadouts()
        {
            selfContainedIdentifier = functionList[43];
            if (!IsStateActive)
            {
                InitializeState(LoadoutState(), selfContainedIdentifier);
                if (PauseTickRun) return false;
            }
            return RunStateManager;
        }

        IEnumerator<FunctionState> LoadoutState()
        {
            IMyInventory loadoutInventory, sourceInventory;
            int itemCount;
            ItemDefinition definition;
            double addAmount;
            bool excessFound;
            while (true)
            {
                foreach (long index in typedIndexes[setKeyIndexLoadout])
                {
                    if (PauseTickRun) yield return stateActive;

                    if (!IsBlockOk(index))
                        continue;

                    mainBlockDefinition = managedBlocks[index];
                    loadoutInventory = mainBlockDefinition.Input;
                    itemCollectionMain.Clear();
                    itemCollectionAlternate.Clear();
                    itemCollectionAlternate.AddCollection(mainBlockDefinition.Settings.loadout, mainBlockDefinition.block);
                    while (!CountItemsInList(itemCollectionMain, new List<long> { index }))
                        yield return stateActive;

                    itemCount = itemCollectionMain.ItemTypeCount;
                    for (int x = 0; x < itemCount; x++)
                    {
                        if (PauseTickRun)
                            yield return stateActive;

                        definition = itemCollectionMain.ItemByIndex(x);
                        if (TextHasLength(definition.subtypeID) && itemCollectionAlternate.itemList.ContainsKey(definition.FullID))
                            itemCollectionAlternate.AddItem(definition.typeID, definition.subtypeID, new VariableItemCount(-definition.amount));
                    }
                    excessFound = false;
                    itemCount = itemCollectionAlternate.ItemTypeCount;
                    for (int x = 0; x < itemCount && !excessFound; x++)
                    {
                        if (PauseTickRun)
                            yield return stateActive;

                        definition = itemCollectionAlternate.ItemByIndex(x);
                        excessFound = definition.amount <= -0.01;
                    }
                    if (excessFound)
                    {
                        mainFunctionItemList.Clear();
                        loadoutInventory.GetItems(mainFunctionItemList);
                        for (int x = 0; x < mainFunctionItemList.Count; x++)
                        {
                            addAmount = itemCollectionAlternate.ItemCount(mainFunctionItemList[x]);
                            if (addAmount <= 0 - 0.01)
                            {
                                addAmount *= -1.0;
                                addAmount = Math.Min(addAmount, (double)mainFunctionItemList[x].Amount);
                                while (!PutInStorage(new List<MyInventoryItem> { mainFunctionItemList[x] }, index, 0, addAmount))
                                    yield return stateActive;
                            }
                        }
                    }
                    foreach (long storageIndex in typedIndexes[setKeyIndexStorage])
                    {
                        if (PauseTickRun) yield return stateActive;

                        if (itemCollectionAlternate.IsEmpty)
                            break;
                        if (!IsBlockOk(storageIndex))
                            continue;


                        mainBlockDefinition = managedBlocks[storageIndex];
                        sourceInventory = mainBlockDefinition.Input;
                        mainFunctionItemList.Clear();
                        sourceInventory.GetItems(mainFunctionItemList);
                        for (int y = 0; y < mainFunctionItemList.Count && !itemCollectionAlternate.IsEmpty; y++)
                        {
                            if (PauseTickRun) yield return stateActive;

                            if (itemCollectionAlternate.ItemCount(mainFunctionItemList[y]) > 0)
                            {
                                addAmount = Math.Min((double)mainFunctionItemList[y].Amount, itemCollectionAlternate.ItemCount(mainFunctionItemList[y], null));

                                if (!FractionalItem(mainFunctionItemList[y]))
                                    addAmount = Math.Floor(addAmount);

                                if (addAmount > 0 && loadoutInventory.TransferItemFrom(sourceInventory, mainFunctionItemList[y], (MyFixedPoint)addAmount))
                                    itemCollectionAlternate.AddItem(mainFunctionItemList[y].Type.TypeId, mainFunctionItemList[y].Type.SubtypeId, new VariableItemCount(-addAmount));
                            }
                        }
                    }
                }
                yield return stateContinue;
            }
        }

        bool SortRefineries()
        {
            selfContainedIdentifier = functionList[44];
            if (!IsStateActive)
            {
                InitializeState(SortRefineryStateV2(), selfContainedIdentifier);
                if (PauseTickRun) return false;
            }
            return RunStateManager;
        }

        IEnumerator<FunctionState> SortRefineryStateV2()
        {
            IMyInventory inventory;
            double minPercent, maxPercent;
            SortedList<MyItemType, double> orePriorities = new SortedList<MyItemType, double>();
            while (true)
            {
                foreach (ItemDefinition itemDef in itemListMain[oreType].Values)
                {
                    if (PauseTickRun) yield return stateActive;
                    if (itemDef.amount > 0.5)
                        orePriorities[itemDef.ItemType] = LeastKeyedOrePercentage(itemDef.subtypeID);
                }
                foreach (long index in typedIndexes[setKeyIndexRefinery])
                {
                    if (PauseTickRun) yield return stateActive;

                    if (!IsBlockOk(index))
                        continue;

                    minPercent = double.MaxValue;
                    maxPercent = double.MinValue;

                    mainBlockDefinition = managedBlocks[index];
                    inventory = mainBlockDefinition.Input;

                    mainBlockDefinition.Settings.limits.Clear(false, true);

                    //Sort Ores inside of refinery
                    if (inventory.ItemCount > 1)
                    {
                        sortableListMain.Clear();
                        mainFunctionItemList.Clear();
                        inventory.GetItems(mainFunctionItemList);
                        for (int x = 0; x < mainFunctionItemList.Count; x++)
                        {
                            if (PauseTickRun) yield return stateActive;
                            if ((double)mainFunctionItemList[x].Amount >= 0.01)
                            {
                                if (!orePriorities.ContainsKey(mainFunctionItemList[x].Type))
                                    orePriorities[mainFunctionItemList[x].Type] = LeastKeyedOrePercentage(mainFunctionItemList[x]);
                                sortableListMain.Add(new SortableObject { amount = orePriorities[mainFunctionItemList[x].Type], text = mainFunctionItemList[x].Type.ToString() });
                                minPercent = Math.Min(minPercent, sortableListMain[sortableListMain.Count - 1].amount);
                                maxPercent = Math.Max(maxPercent, sortableListMain[sortableListMain.Count - 1].amount);
                            }
                        }
                        if (minPercent < maxPercent)
                        {
                            sortableListMain = sortableListMain.OrderBy(z => z.amount).ToList();
                            while (!OrderInventory(sortableListMain, inventory))
                                yield return stateActive;
                        }
                    }
                    //Set automatic limits
                    if (activeOres > 1 && !managedBlocks[index].IsClone)
                    {
                        sortableListMain.Clear();
                        foreach (KeyValuePair<MyItemType, double> pair in orePriorities)
                            if (AcceptsItem(mainBlockDefinition, pair.Key.TypeId, pair.Key.SubtypeId))
                                sortableListMain.Add(new SortableObject { amount = pair.Value, text = pair.Key.SubtypeId });
                        sortableListMain = sortableListMain.OrderBy(x => x.amount).ToList();

                        double maxShares = 0, currentShares;
                        int prioritizedOres = settingsInts[setKeyPrioritizedOreCount];

                        for (int z = 1; z <= sortableListMain.Count; z++)
                            maxShares += z;

                        maxShares += prioritizedOres * 10;

                        for (int x = 0; x < sortableListMain.Count; x++)
                        {
                            if (PauseTickRun) yield return stateActive;
                            currentShares = (sortableListMain.Count - x) + 1;

                            if (x < prioritizedOres)
                                currentShares += 10;

                            mainBlockDefinition.Settings.limits.AddItem(oreType, sortableListMain[x].text, new VariableItemCount((currentShares / maxShares) * 0.985, true));
                        }
                    }

                    if (mainBlockDefinition.Settings.limits.ItemTypeCount > 0)
                        typedIndexes[setKeyIndexLimit].Add(index);
                    else
                        typedIndexes[setKeyIndexLimit].Remove(index);
                }
                while (!OrderListByPriority(typedIndexes[setKeyIndexLimit], priorityTypes.Contains(setKeyIndexLimit))) yield return stateActive;
                orePriorities.Clear();
                yield return stateContinue;
            }
        }

        bool Logic()
        {
            selfContainedIdentifier = functionList[45]; ;
            if (!IsStateActive)
            {
                InitializeState(LogicState(), selfContainedIdentifier);
                if (PauseTickRun) return false;
            }
            return RunStateManager;
        }

        IEnumerator<FunctionState> LogicState()
        {
            ItemDefinition definition;
            while (true)
            {
                bool andComparison, pass = true;
                foreach (long index in typedIndexes[setKeyIndexLogic])
                {
                    if (PauseTickRun) yield return stateActive;

                    alternateBlockDefinition = managedBlocks[index];
                    if (!(alternateBlockDefinition.block is IMyTimerBlock) || ((IMyFunctionalBlock)alternateBlockDefinition.block).Enabled)
                    {
                        andComparison = alternateBlockDefinition.Settings.andComparison;
                        for (int x = 0; x < alternateBlockDefinition.Settings.logicComparisons.Count; x++)
                        {
                            if (PauseTickRun)
                                yield return stateActive;

                            if (GetDefinition(out definition, alternateBlockDefinition.Settings.logicComparisons[x].typeID))
                                pass = LogicPass(definition, alternateBlockDefinition.Settings.logicComparisons[x].comparison, alternateBlockDefinition.Settings.logicComparisons[x].compareAgainst);

                            if ((!pass && andComparison) || (pass && !andComparison))
                                break;
                        }
                        if (alternateBlockDefinition.block is IMyTimerBlock)
                        {
                            if (pass)
                                ((IMyTimerBlock)alternateBlockDefinition.block).Trigger();
                        }
                        else
                            ((IMyFunctionalBlock)alternateBlockDefinition.block).Enabled = pass;
                    }
                }
                yield return stateContinue;
            }
        }

        void Script()
        {
            selfContainedIdentifier = functionList[0];
            if (!IsStateActive)
                InitializeState(ScriptState(), selfContainedIdentifier);
            else
                StateManager(selfContainedIdentifier);
        }

        IEnumerator<FunctionState> ScriptState()
        {
            while (true)
            {
                while (!LoadData()) yield return stateActive;

                while (!SaveData()) yield return stateActive;

                ControlScript();

                yield return stateActive;

                if (!scanning && !loading) OutputScript();

                if (PauseTickRun) yield return stateActive;
                yield return stateContinue;
            }
        }

        void ControlScript()
        {
            selfContainedIdentifier = functionList[1];
            if (!IsStateActive)
                InitializeState(ControlState(), selfContainedIdentifier);
            else
                StateManager(selfContainedIdentifier);
        }

        IEnumerator<FunctionState> ControlState()
        {
            yield return stateActive;
            string key;
            while (true)
            {
                if (currentErrorCount >= 10)
                {
                    currentErrorCount = 0;
                    for (int i = 2; i < functionList.Length; i++)
                    {
                        if (PauseTickRun)
                            yield return stateActive;

                        if (StateActive(functionList[i]))
                            StateDisposal(functionList[i]);
                    }
                }

                if (SpanElapsed(scanSpan))
                {
                    scanning = true;
                    currentMajorFunction = functionList[17];
                    while (!Scan())
                        yield return stateActive;

                    scanning = false;
                    scanSpan = SpanDelay(GetKeyDouble(setKeyDelayScan));
                }

                if (GetKeyBool(setKeyToggleCountItems))
                {
                    currentMajorFunction = functionList[16];
                    while (!Count())
                        yield return stateActive;
                }

                if (GetKeyBool(setKeyToggleCountBlueprints))
                {
                    currentMajorFunction = functionList[15];
                    while (!CountBlueprints()) yield return stateActive;
                }

                if (GetKeyBool(setKeyToggleSortBlueprints))
                {
                    key = functionList[40];
                    if (FunctionDelay(key))
                    {
                        currentMajorFunction = key;
                        while (!SortBlueprints())
                            yield return stateActive;

                        delaySpans[key] = SpanDelay(GetKeyDouble(setKeyDelaySortBlueprints));
                    }
                }

                if (GetKeyBool(setKeyToggleQueueAssembly) && GetKeyBool(setKeyToggleCountBlueprints))
                {
                    key = functionList[31];
                    if (FunctionDelay(key))
                    {
                        currentMajorFunction = key;
                        while (!QueueAssembly())
                            yield return stateActive;

                        delaySpans[key] = SpanDelay(GetKeyDouble(setKeyDelayQueueAssembly));
                    }
                }

                if (GetKeyBool(setKeyToggleQueueDisassembly) && GetKeyBool(setKeyToggleCountBlueprints))
                {
                    key = functionList[32];
                    if (FunctionDelay(key))
                    {
                        currentMajorFunction = key;
                        while (!QueueDisassembly())
                            yield return stateActive;

                        delaySpans[key] = SpanDelay(GetKeyDouble(setKeyDelayQueueDisassembly));
                    }
                }

                if (GetKeyBool(setKeyToggleRemoveExcessAssembly) && GetKeyBool(setKeyToggleCountBlueprints))
                {
                    key = functionList[27];
                    if (FunctionDelay(key))
                    {
                        currentMajorFunction = key;
                        while (!RemoveExcessAssembly())
                            yield return stateActive;

                        delaySpans[key] = SpanDelay(GetKeyDouble(setKeyDelayRemoveExcessAssembly));
                    }
                }

                if (GetKeyBool(setKeyToggleRemoveExcessDisassembly) && GetKeyBool(setKeyToggleCountBlueprints))
                {
                    key = functionList[36];
                    if (FunctionDelay(key))
                    {
                        currentMajorFunction = key;
                        while (!RemoveExcessDisassembly())
                            yield return stateActive;

                        delaySpans[key] = SpanDelay(GetKeyDouble(setKeyDelayRemoveExcessDisassembly));
                    }
                }

                if (activeOres > 0 && GetKeyBool(setKeyToggleToggleSortRefineries))
                {
                    key = functionList[44];
                    if (FunctionDelay(key))
                    {
                        currentMajorFunction = key;
                        while (!SortRefineries())
                            yield return stateActive;

                        delaySpans[key] = SpanDelay(GetKeyDouble(setKeyDelaySortRefinery));
                    }
                }

                if (typedIndexes[setKeyIndexLimit].Count > 0 && GetKeyBool(setKeyToggleProcessLimits))
                {
                    key = functionList[12];
                    if (FunctionDelay(key))
                    {
                        currentMajorFunction = key;
                        while (!ProcessLimits())
                            yield return stateActive;

                        delaySpans[key] = SpanDelay(GetKeyDouble(setKeyDelayProcessLimits));
                    }
                }

                if (GetKeyBool(setKeyToggleSortItems))
                {
                    key = functionList[13];
                    if (FunctionDelay(key))
                    {
                        currentMajorFunction = key;
                        bool useBottles = GetKeyDouble(setKeyDelayFillingBottles) > 0 && SpanElapsed(fillBottleSpan);
                        if (useBottles)
                            fillingBottles = !fillingBottles;

                        while (!SortItems())
                            yield return stateActive;

                        delaySpans[key] = SpanDelay(GetKeyDouble(setKeyDelaySorting));
                        if (useBottles)
                            fillBottleSpan = SpanDelay(fillingBottles ? 7.5 : GetKeyDouble(setKeyDelayFillingBottles));
                    }
                }

                if (GetKeyBool(setKeyToggleDistribution))
                {
                    key = functionList[9];
                    if (FunctionDelay(key))
                    {
                        currentMajorFunction = key;
                        while (!DistributeItems())
                            yield return stateActive;

                        delaySpans[key] = SpanDelay(GetKeyDouble(setKeyDelayDistribution));
                    }
                }

                key = functionList[24];
                if (FunctionDelay(key))
                {
                    if (typedIndexes[setKeyIndexRefinery].Count > 1 && GetKeyBool(setKeyToggleSpreadRefieries))
                    {
                        currentMajorFunction = "Spreading Refineries";
                        while (!BalanceItems(typedIndexes[setKeyIndexRefinery]))
                            yield return stateActive;
                    }
                    if (typedIndexes[setKeyIndexReactor].Count > 1 && GetKeyBool(setKeyToggleSpreadReactors))
                    {
                        currentMajorFunction = "Spreading Reactors";
                        while (!BalanceItems(typedIndexes[setKeyIndexReactor]))
                            yield return stateActive;
                    }
                    if (typedIndexes[setKeyIndexGun].Count > 1 && GetKeyBool(setKeyToggleSpreadGuns))
                    {
                        currentMajorFunction = "Spreading Weapons";
                        while (!BalanceItems(typedIndexes[setKeyIndexGun]))
                            yield return stateActive;
                    }
                    if (typedIndexes[setKeyIndexGasGenerators].Count > 1 && GetKeyBool(setKeyToggleSpreadGasGenerators))
                    {
                        currentMajorFunction = "Spreading O2/H2 Gens";
                        while (!BalanceItems(typedIndexes[setKeyIndexGasGenerators]))
                            yield return stateActive;
                    }
                    if (typedIndexes[setKeyIndexGravelSifters].Count > 1 && GetKeyBool(setKeyToggleSpreadGravelSifters))
                    {
                        currentMajorFunction = "Spreading Gravel Sifters";
                        while (!BalanceItems(typedIndexes[setKeyIndexGravelSifters]))
                            yield return stateActive;
                    }
                    if (typedIndexes[setKeyIndexParachute].Count > 1 && GetKeyBool(setKeyToggleSpreadParachutes))
                    {
                        currentMajorFunction = "Spreading Parachutes";
                        while (!BalanceItems(typedIndexes[setKeyIndexParachute]))
                            yield return stateActive;
                    }
                    delaySpans[key] = SpanDelay(GetKeyDouble(setKeyDelaySpreading));
                }

                if (prioritySystemActivated && GetKeyBool(setKeyToggleSortCargoPriority))
                {
                    key = functionList[38];
                    if (FunctionDelay(key))
                    {
                        currentMajorFunction = key;
                        while (!SortCargoPriorities())
                            yield return stateActive;

                        delaySpans[key] = SpanDelay(GetKeyDouble(setKeyDelaySortCargoPriority));
                    }
                }

                if (GetKeyBool(setKeyToggleOrderCargo))
                {
                    key = functionList[54];
                    if (FunctionDelay(key))
                    {
                        currentMajorFunction = key;
                        while (!OrderCargo())
                            yield return stateActive;

                        delaySpans[key] = SpanDelay(GetKeyDouble(setKeyDelayOrderCargo));
                    }
                }

                if (typedIndexes[setKeyIndexAssemblers].Count > 1 && GetKeyBool(setKeyToggleSpreadBlueprints))
                {
                    key = functionList[41];
                    if (FunctionDelay(key))
                    {
                        currentMajorFunction = key;
                        while (!SpreadBlueprints())
                            yield return stateActive;

                        delaySpans[key] = SpanDelay(GetKeyDouble(setKeyDelaySpreadBlueprints));
                    }
                }

                if (typedIndexes[setKeyIndexLoadout].Count > 0 && GetKeyBool(setKeyToggleDoLoadouts))
                {
                    key = functionList[43];
                    if (FunctionDelay(key))
                    {
                        currentMajorFunction = key;
                        while (!Loadouts())
                            yield return stateActive;

                        delaySpans[key] = SpanDelay(GetKeyDouble(setKeyDelayLoadouts));
                    }
                }

                if (typedIndexes[setKeyIndexLogic].Count > 0 && GetKeyBool(setKeyToggleLogic))
                {
                    key = functionList[45];
                    if (FunctionDelay(key))
                    {
                        currentMajorFunction = key;
                        while (!Logic())
                            yield return stateActive;

                        delaySpans[key] = SpanDelay(GetKeyDouble(setKeyDelayLogic));
                    }
                }

                if (typedIndexes[setKeyIndexAssemblers].Count > 0 && GetKeyBool(setKeyToggleResetIdleAssemblers))
                {
                    key = functionList[48];
                    if (FunctionDelay(key))
                    {
                        currentMajorFunction = key;
                        while (!CheckIdleAssemblers())
                            yield return stateActive;

                        delaySpans[key] = SpanDelay(GetKeyDouble(setKeyDelayIdleAssemblerCheck));
                    }
                }

                if (GetKeyBool(setKeyToggleFindModItems))
                {
                    key = functionList[49];
                    if (FunctionDelay(key))
                    {
                        currentMajorFunction = key;
                        while (!FindModItems())
                            yield return stateActive;

                        delaySpans[key] = SpanDelay(GetKeyDouble(setKeyDelayFindModItems));
                    }
                }

                yield return stateContinue;
            }
        }

        void OutputScript()
        {
            selfContainedIdentifier = functionList[2];
            if (!IsStateActive)
            {
                InitializeState(OutputState(), selfContainedIdentifier);
                if (PauseTickRun) return;
            }
            StateManager(selfContainedIdentifier, !scanning && !loading, false);
        }

        IEnumerator<FunctionState> OutputState()
        {
            List<long> indexes = NewLongList;
            while (true)
            {
                indexes.Clear();
                indexes.AddRange(typedIndexes[setKeyIndexPanel]);
                foreach (long index in indexes)
                {
                    if (PauseTickRun) yield return stateActive;
                    for (int p = 0; p < managedBlocks[index].panelDefinitionList.Count; p++)
                        while (!panelMaster.TotalPanelV2(managedBlocks[index].panelDefinitionList.Values[p])) yield return stateActive;
                }

                yield return stateContinue;
            }
        }

        bool OrderListByPriority(List<long> indexList, bool order)
        {
            if (indexList.Count < 2)
                return true;
            selfContainedIdentifier = functionList[37];
            if (!IsStateActive)
            {
                InitializeState(OrderListByPriorityState(indexList, order), selfContainedIdentifier);
                if (PauseTickRun) return false;
            }
            return RunStateManager;
        }

        IEnumerator<FunctionState> OrderListByPriorityState(List<long> indexList, bool order)
        {
            for (int i = 0; i < indexList.Count; i += 0)
            {
                if (PauseTickRun) yield return stateActive;
                if (!uniqueIndexSet.Add(indexList[i]))
                    indexList.RemoveAt(i);
                else i++;
            }
            uniqueIndexSet.Clear();
            if (order && prioritySystemActivated)
            {
                List<long> orderedList = new List<long>(indexList);
                IOrderedEnumerable<long> sortableObjects = orderedList.OrderByDescending(x => managedBlocks[x].Settings.priority);
                indexList.Clear();
                foreach (long index in sortableObjects)
                {
                    if (PauseTickRun) yield return stateActive;
                    indexList.Add(index);
                }
            }
            yield return stateComplete;
        }

        bool SetBlockQuotas(ItemCollection collection)
        {
            selfContainedIdentifier = functionList[29];
            if (!IsStateActive)
            {
                InitializeState(SetBlockQuotaState(collection), selfContainedIdentifier);
                if (PauseTickRun) return false;
            }
            return RunStateManager;
        }

        IEnumerator<FunctionState> SetBlockQuotaState(ItemCollection collection)
        {
            List<ItemDefinition> itemList = GetAllItems;
            bool append = collection.ItemTypeCount > 0;
            foreach (ItemDefinition def in itemList)
            {
                if (PauseTickRun) yield return stateActive;

                def.blockQuota = append ? collection.ItemCount(def.typeID, def.subtypeID, null) : 0;
            }
            yield return stateComplete;
        }

        bool SaveData()
        {
            selfContainedIdentifier = functionList[30];
            if (!saving)
                return true;
            currentMajorFunction = selfContainedIdentifier;
            if (!IsStateActive)
            {
                SetLastString("Saving Data");
                InitializeState(SaveState(), selfContainedIdentifier);
                if (PauseTickRun) return false;
            }
            return RunStateManager;
        }

        IEnumerator<FunctionState> SaveState()
        {
            StringBuilder builder = NewBuilder;
            string currentCategory;
            int duplicates;
            SortedList<string, SortedList<string, ItemDefinition>> categoryAndNameSorter = new SortedList<string, SortedList<string, ItemDefinition>>();

            while (true)
            {
                builder.Clear();
                categoryAndNameSorter.Clear();
                foreach (KeyValuePair<string, SortedList<string, ItemDefinition>> kvpA in itemListMain)
                {
                    if (PauseTickRun)
                        yield return stateActive;

                    duplicates = 0;
                    foreach (KeyValuePair<string, ItemDefinition> kvpB in kvpA.Value)
                    {
                        if (PauseTickRun)
                            yield return stateActive;

                        currentCategory = kvpB.Value.category;
                        if (!categoryAndNameSorter.ContainsKey(currentCategory))
                            categoryAndNameSorter[currentCategory] = new SortedList<string, ItemDefinition>();

                        if (!categoryAndNameSorter[currentCategory].ContainsKey(kvpB.Value.displayName))
                            categoryAndNameSorter[currentCategory][kvpB.Value.displayName] = kvpB.Value;
                        else
                        {
                            duplicates++;
                            categoryAndNameSorter[currentCategory][$"{kvpB.Value.displayName} {duplicates}"] = kvpB.Value;
                        }
                    }
                }

                foreach (KeyValuePair<string, SortedList<string, ItemDefinition>> kvpA in categoryAndNameSorter)
                {
                    if (PauseTickRun)
                        yield return stateActive;

                    AppendHeader(ref builder, $"Items - {Formatted(kvpA.Key)}");
                    foreach (KeyValuePair<string, ItemDefinition> kvpB in kvpA.Value)
                    {
                        if (PauseTickRun)
                            yield return stateActive;

                        ItemDefinitionToBuilders(ref builder, kvpB.Value);
                        BuilderAppendLine(builder);
                    }
                }

                AppendHeader(ref builder, "Item Groups");
                foreach (KeyValuePair<string, ItemCollection> kvp in customItemGroups)
                {
                    if (PauseTickRun)
                        yield return stateActive;

                    builder.Append($"group={kvp.Key}=");
                    while (!CollectionToString(builder, kvp.Value, false))
                        yield return stateActive;
                }
                if (!reset)
                {
                    foreach (KeyValuePair<string, SortedList<string, bool>> kvpA in settingDictionaryBools)
                    {
                        if (PauseTickRun)
                            yield return stateActive;

                        AppendHeader(ref builder, $"Switches - {kvpA.Key}");
                        foreach (KeyValuePair<string, bool> kvpB in kvpA.Value)
                        {
                            if (PauseTickRun)
                                yield return stateActive;

                            BuilderAppendLine(builder, $"{kvpB.Key}={kvpB.Value}");
                        }
                    }
                    foreach (KeyValuePair<string, SortedList<string, double>> kvpA in settingDictionaryDoubles)
                    {
                        if (PauseTickRun)
                            yield return stateActive;

                        AppendHeader(ref builder, $"Numbers - {kvpA.Key}");
                        foreach (KeyValuePair<string, double> kvpB in kvpA.Value)
                        {
                            if (PauseTickRun)
                                yield return stateActive;

                            BuilderAppendLine(builder, $"{kvpB.Key}={kvpB.Value}");
                        }
                    }
                    foreach (KeyValuePair<string, int> kvp in settingsInts)
                    {
                        if (PauseTickRun)
                            yield return stateActive;

                        BuilderAppendLine(builder, $"{kvp.Key}={kvp.Value}");
                    }
                    foreach (KeyValuePair<string, SortedList<string, string>> kvpA in settingDictionaryStrings)
                    {
                        if (PauseTickRun)
                            yield return stateActive;

                        AppendHeader(ref builder, $"Text - {kvpA.Key}");
                        foreach (KeyValuePair<string, string> kvpB in kvpA.Value)
                        {
                            if (PauseTickRun)
                                yield return stateActive;

                            BuilderAppendLine(builder, $"{kvpB.Key}={kvpB.Value}");
                        }
                    }
                    AppendHeader(ref builder, "Lists");
                    foreach (KeyValuePair<string, List<string>> kvp in settingsListsStrings)
                    {
                        if (PauseTickRun)
                            yield return stateActive;

                        builder.Append($"{kvp.Key}=[");
                        for (int i = 0; i < kvp.Value.Count; i++)
                            builder.Append($"{(i > 0 ? "|" : "")}{kvp.Value[i]}");
                        BuilderAppendLine(builder, "]");
                        BuilderAppendLine(builder);
                    }

                    BuilderAppendLine(builder);
                    BuilderAppendLine(builder);
                    BuilderAppendLine(builder, $"script={scriptName}");

                    BuilderAppendLine(builder, update ? "version=-1" : $"version={scriptVersion}");
                }

                Me.CustomData = builder.ToString().Trim();
                settingBackup = Me.CustomData;
                correctScript = true;
                correctVersion = true;
                saving = false;
                SetLastString("Save Data Complete");
                yield return stateContinue;
            }
        }

        bool LoadData()
        {
            selfContainedIdentifier = functionList[42];
            if (!loading)
                return true;
            currentMajorFunction = selfContainedIdentifier;
            if (!IsStateActive)
            {
                SetLastString("Loading Data");
                InitializeState(LoadState(), selfContainedIdentifier);
                if (PauseTickRun) return false;
            }
            return RunStateManager;
        }

        IEnumerator<FunctionState> LoadState()
        {
            string[] settingArray;
            List<string> settingList = NewStringList;
            string excludedDefTemp;
            while (true)
            {
                if (TextHasLength(Me.CustomData))
                {
                    correctScript = false;
                    correctVersion = false;
                    itemCategoryList.Clear();
                    yield return stateActive;
                    settingArray = SplitLines(Me.CustomData);
                    settingList.Clear();
                    for (int i = 0; i < settingArray.Length; i++)
                    {
                        if (PauseTickRun) yield return stateActive;

                        if (LeadsString(settingArray[i], "^") && settingList.Count > 0)
                            settingList[settingList.Count - 1] += $"||{settingArray[i].Substring(1).Trim()}";
                        else
                            settingList.Add(settingArray[i].Trim());
                    }
                    foreach (string setting in settingList)
                    {
                        if (PauseTickRun) yield return stateActive;

                        while (!ProcessSetting(setting))
                            yield return stateActive;
                    }
                    clearedSettingLists.Clear();
                    for (int i = 0; i < settingsListsStrings[setKeyExcludedDefinitions].Count; i++)
                    {
                        if (PauseTickRun) yield return stateActive;

                        excludedDefTemp = settingsListsStrings[setKeyExcludedDefinitions][i];
                        if (excludedDefTemp.Contains("/"))
                            settingsListsStrings[setKeyExcludedDefinitions][i] = excludedDefTemp.Substring(excludedDefTemp.IndexOf("/") + 1);
                    }
                    if (!correctVersion || !correctScript)
                        saving = true;
                }
                else saving = true;

                if (PauseTickRun) yield return stateActive;

                SetConstants();
                if (PauseTickRun) yield return stateActive;

                SetPostLoad();
                if (PauseTickRun) yield return stateActive;

                settingBackup = Me.CustomData.Trim();
                SetLastString("Load Data Complete");
                loading = false;
                yield return stateContinue;
            }
        }

        bool QueueAssembly()
        {
            selfContainedIdentifier = functionList[31];
            if (!IsStateActive)
            {
                InitializeState(QueueAssemblyState(), selfContainedIdentifier);
                if (PauseTickRun) return false;
            }
            return RunStateManager;
        }

        IEnumerator<FunctionState> QueueAssemblyState()
        {
            double queueAmount;
            while (true)
            {
                foreach (KeyValuePair<string, Blueprint> kvp in blueprintList)
                {
                    if (PauseTickRun)
                        yield return stateActive;

                    queueAmount = AssemblyAmount(kvp.Value);
                    if (queueAmount > 0)
                        while (!DistributeBlueprint(kvp.Value, queueAmount, typedIndexes[setKeyIndexAssemblers]))
                            yield return stateActive;
                }
                int queuedIngots = settingsInts[setKeySurvivalKitQueuedIngots];
                if (queuedIngots > 0)
                {
                    Blueprint blueprint = new Blueprint { blueprintID = stoneOreToIngotBasicID };
                    while (!DistributeBlueprint(blueprint, queuedIngots, typedIndexes[setKeyIndexAssemblers], assemblyMode, false))
                        yield return stateActive;
                }
                yield return stateContinue;
            }
        }

        bool QueueDisassembly()
        {
            selfContainedIdentifier = functionList[32];
            if (!IsStateActive)
            {
                InitializeState(QueueDisassemblyState(), selfContainedIdentifier);
                if (PauseTickRun) return false;
            }
            return RunStateManager;
        }

        IEnumerator<FunctionState> QueueDisassemblyState()
        {
            double queueAmount;
            while (true)
            {
                foreach (KeyValuePair<string, Blueprint> kvp in blueprintList)
                {
                    if (PauseTickRun) yield return stateActive;

                    queueAmount = AssemblyAmount(kvp.Value, true);
                    if (queueAmount > 0)
                        while (!DistributeBlueprint(kvp.Value, queueAmount, typedIndexes[setKeyIndexAssemblers], disassemblyMode))
                            yield return stateActive;
                }
                yield return stateContinue;
            }
        }

        bool DistributeBlueprint(Blueprint blueprint, double amount, List<long> assemblerIndexList, MyAssemblerMode mode = assemblyMode, bool count = true)
        {
            selfContainedIdentifier = functionList[26];
            if (!IsStateActive)
            {
                InitializeState(DistributeBlueprintState(blueprint, amount, assemblerIndexList, mode, count), selfContainedIdentifier);
                if (PauseTickRun) return false;
            }
            return RunStateManager;
        }

        IEnumerator<FunctionState> DistributeBlueprintState(Blueprint blueprint, double originalAmount, List<long> assemblerIndexList, MyAssemblerMode mode, bool count)
        {
            double amount = originalAmount, multiplier = 1;
            string blocksubtype;
            if (mode == disassemblyMode)
            {
                multiplier = blueprint.multiplier;
                amount = Math.Floor(originalAmount / multiplier);
            }
            MyDefinitionId blueprintID = MyDefinitionId.Parse(MakeBlueprint(blueprint));
            IMyTerminalBlock block;
            potentialAssemblerList.Clear();
            foreach (long index in assemblerIndexList)
            {
                if (PauseTickRun) yield return stateActive;

                if (!IsBlockOk(index))
                    continue;

                if (UsableAssembler(managedBlocks[index], blueprintID, mode))
                {
                    block = managedBlocks[index].block;
                    blocksubtype = BlockSubtype(block);
                    if (!potentialAssemblerList.ContainsKey(blocksubtype))
                        potentialAssemblerList[blocksubtype] = new List<PotentialAssembler>();

                    potentialAssemblerList[blocksubtype].Add(new PotentialAssembler { index = index, empty = ((IMyAssembler)block).IsQueueEmpty, specific = managedBlocks[index].Settings.UniqueBlueprintsOnly });
                }
            }

            List<long> indexList = NewLongList;
            foreach (KeyValuePair<string, List<PotentialAssembler>> kvpA in potentialAssemblerList)
            {
                if (PauseTickRun)
                    yield return stateActive;

                foreach (PotentialAssembler potentialAssembler in kvpA.Value)
                {
                    if (PauseTickRun)
                        yield return stateActive;

                    if (!potentialAssembler.specific || potentialAssemblerList.Count == 1)
                    {
                        if (mode == disassemblyMode && assemblyNeededByMachine.Contains(kvpA.Key) && indexList.Count > 0)
                            continue;

                        if (potentialAssembler.empty)
                            indexList.Insert(0, potentialAssembler.index);
                        else
                            indexList.Add(potentialAssembler.index);
                    }
                }
            }
            potentialAssemblerList.Clear();

            if (indexList.Count > 0)
            {
                int splitAmount, excessAmount, currentAmount;
                splitAmount = Math.DivRem((int)amount, indexList.Count, out excessAmount);
                if (blueprint.blueprintID == stoneOreToIngotBasicID)
                {
                    excessAmount = 0;
                    splitAmount = (int)amount;
                }

                for (int i = 0; i < indexList.Count && i < amount; i++)
                {
                    currentAmount = splitAmount;
                    if (i < excessAmount)
                        currentAmount++;

                    if (currentAmount > 0)
                        while (!InsertBlueprint(blueprintID, currentAmount * multiplier, managedBlocks[indexList[i]], mode, count))
                            yield return stateActive;
                }
            }

            yield return stateComplete;
        }

        bool InsertBlueprint(MyDefinitionId blueprintID, double amount, BlockDefinition managedBlock, MyAssemblerMode mode, bool count = true)
        {
            selfContainedIdentifier = functionList[33];
            if (!IsStateActive)
            {
                InitializeState(InsertBlueprintState(blueprintID, amount, managedBlock, mode, count), selfContainedIdentifier);
                if (PauseTickRun) return false;
            }
            return RunStateManager;
        }

        IEnumerator<FunctionState> InsertBlueprintState(MyDefinitionId blueprintID, double amount, BlockDefinition managedBlock, MyAssemblerMode mode, bool count)
        {
            double currentAmount = Math.Floor(amount);
            bool contains;
            IMyAssembler assembler = (IMyAssembler)managedBlock.block;
            assembler.Mode = mode;
            if (assembler.Mode != mode)
                assembler.Mode = mode;

            if (assembler.Mode == mode)
            {
                if (assembler.IsQueueEmpty)
                    assembler.AddQueueItem(blueprintID, currentAmount);
                else
                {
                    bool inserted = false;
                    double currentPercent = mode == assemblyMode ? BlueprintPercentage(blueprintID) : 0, nextPercent = 0;

                    blueprintListMain.Clear();
                    assembler.GetQueue(blueprintListMain);
                    contains = false;
                    for (int i = 0; !contains && i < blueprintListMain.Count; i++)
                    {
                        if (PauseTickRun) yield return stateActive;

                        if (BlueprintSubtype(blueprintListMain[i]) == blueprintID.SubtypeName)
                        {
                            contains = true;
                            if (blueprintID.SubtypeName == stoneOreToIngotBasicID)
                                currentAmount = Math.Floor(currentAmount - (double)blueprintListMain[i].Amount);
                        }
                    }
                    if (currentAmount > 0 && (contains || (mode == assemblyMode && !managedBlock.Settings.NoSorting)))
                        for (int i = 0; i < blueprintListMain.Count; i++)
                        {
                            if (PauseTickRun) yield return stateActive;

                            if (!contains && mode == assemblyMode)
                                nextPercent = BlueprintPercentage(blueprintListMain[i].BlueprintId);

                            if ((!contains && currentPercent <= nextPercent) || BlueprintSubtype(blueprintListMain[i]) == blueprintID.SubtypeName)
                            {
                                assembler.InsertQueueItem(i, blueprintID, currentAmount);
                                inserted = true;
                                break;
                            }
                        }

                    if (!inserted && currentAmount > 0)
                        assembler.AddQueueItem(blueprintID, currentAmount);
                }
                if (count)
                    AddBlueprintAmount(blueprintID.SubtypeName, mode == assemblyMode, currentAmount, true);
            }

            yield return stateComplete;
        }

        bool RemoveExcessAssembly()
        {
            selfContainedIdentifier = functionList[27];
            if (!IsStateActive)
            {
                InitializeState(RemoveExcessAssemblyState(), selfContainedIdentifier);
                if (PauseTickRun) return false;
            }
            return RunStateManager;
        }

        IEnumerator<FunctionState> RemoveExcessAssemblyState()
        {
            ItemDefinition definition;
            double excessQueued;
            while (true)
            {
                foreach (KeyValuePair<string, Blueprint> kvp in blueprintList)
                {
                    if (PauseTickRun) yield return stateActive;

                    if (GetDefinition(out definition, $"{kvp.Value.typeID}/{kvp.Value.subtypeID}"))
                    {
                        excessQueued = Math.Floor(definition.currentExcessAssembly);
                        if (excessQueued > 0)
                            while (!RemoveBlueprint(kvp.Value, excessQueued))
                                yield return stateActive;
                    }
                }
                yield return stateContinue;
            }
        }

        bool RemoveBlueprint(Blueprint blueprint, double amount, MyAssemblerMode mode = assemblyMode)
        {
            selfContainedIdentifier = functionList[35];
            if (!IsStateActive)
            {
                InitializeState(RemoveBlueprintState(blueprint, amount, mode), selfContainedIdentifier);
                if (PauseTickRun) return false;
            }
            return RunStateManager;
        }

        IEnumerator<FunctionState> RemoveBlueprintState(Blueprint blueprint, double amount, MyAssemblerMode mode)
        {
            double removalAmount, toBeRemovedAmount = amount;
            IMyAssembler assembler;
            foreach (long index in typedIndexes[setKeyIndexAssemblers])
            {
                if (toBeRemovedAmount <= 0) break;
                if (PauseTickRun) yield return stateActive;
                if (!IsBlockOk(index))
                    continue;

                assembler = (IMyAssembler)managedBlocks[index].block;

                if (assembler.Mode == mode && !assembler.IsQueueEmpty)
                {
                    blueprintListMain.Clear();
                    assembler.GetQueue(blueprintListMain);
                    for (int x = blueprintListMain.Count - 1; x >= 0 && toBeRemovedAmount > 0; x--)
                    {
                        if (PauseTickRun)
                            yield return stateActive;

                        if (BlueprintSubtype(blueprintListMain[x]) == blueprint.blueprintID)
                        {
                            removalAmount = (double)blueprintListMain[x].Amount;
                            if (removalAmount > toBeRemovedAmount)
                                removalAmount = toBeRemovedAmount;

                            assembler.RemoveQueueItem(x, (MyFixedPoint)removalAmount);
                            AddBlueprintAmount(blueprint.blueprintID, mode == assemblyMode, -removalAmount, true);
                            toBeRemovedAmount -= removalAmount;
                        }
                    }
                }
            }
            yield return stateComplete;
        }

        bool RemoveExcessDisassembly()
        {
            selfContainedIdentifier = functionList[36];
            if (!IsStateActive)
            {
                InitializeState(RemoveExcessDisassemblyState(), selfContainedIdentifier);
                if (PauseTickRun) return false;
            }
            return RunStateManager;
        }

        IEnumerator<FunctionState> RemoveExcessDisassemblyState()
        {
            ItemDefinition definition;
            double excessQueued;
            while (true)
            {
                foreach (KeyValuePair<string, Blueprint> kvp in blueprintList)
                {
                    if (PauseTickRun)
                        yield return stateActive;

                    if (GetDefinition(out definition, $"{kvp.Value.typeID}/{kvp.Value.subtypeID}"))
                    {
                        excessQueued = Math.Floor(definition.currentExcessDisassembly);
                        if (excessQueued > 0)
                            while (!RemoveBlueprint(kvp.Value, excessQueued, disassemblyMode))
                                yield return stateActive;
                    }
                }
                yield return stateContinue;
            }
        }

        bool SortCargoPriorities()
        {
            selfContainedIdentifier = functionList[38];
            if (!IsStateActive)
            {
                InitializeState(SortCargoPriorityState(), selfContainedIdentifier);
                if (PauseTickRun) return false;
            }
            return RunStateManager;
        }

        IEnumerator<FunctionState> SortCargoPriorityState()
        {
            while (true)
            {
                foreach (KeyValuePair<string, List<long>> kvp in indexesStorageLists)
                {
                    if (PauseTickRun)
                        yield return stateActive;

                    if (kvp.Value.Count > 1 && priorityCategories.Contains(kvp.Key))
                        while (!SortCargoList(kvp.Value))
                            yield return stateActive;
                }
                yield return stateContinue;
            }
        }

        bool SortCargoList(List<long> indexList)
        {
            selfContainedIdentifier = functionList[39];
            if (!IsStateActive)
            {
                InitializeState(SortCargoListState(indexList), selfContainedIdentifier);
                if (PauseTickRun) return false;
            }
            return RunStateManager;
        }

        IEnumerator<FunctionState> SortCargoListState(List<long> indexList)
        {
            int storageStartIndex = 0;
            while (storageStartIndex < indexList.Count)
            {
                if (PauseTickRun) yield return stateActive;

                if (CurrentVolumePercentage(indexList[storageStartIndex]) >= 0.985)
                    storageStartIndex++;
                else
                    break;
            }
            for (int i = storageStartIndex + 1; i < indexList.Count; i++)
            {
                if (PauseTickRun)
                    yield return stateActive;

                if (!IsBlockOk(indexList[i]))
                    continue;

                if (CurrentVolumePercentage(indexList[storageStartIndex]) >= 0.985)
                    storageStartIndex++;

                if (i > storageStartIndex)
                {
                    mainFunctionItemList.Clear();
                    mainBlockDefinition = managedBlocks[indexList[i]];
                    mainBlockDefinition.Input.GetItems(mainFunctionItemList);

                    for (int x = 0; x < mainFunctionItemList.Count; x += 0)
                    {
                        if (PauseTickRun)
                            yield return stateActive;
                        if (mainBlockDefinition.Settings.loadout.ItemCount(mainFunctionItemList[x], mainBlockDefinition.block) > 0)
                            mainFunctionItemList.RemoveAt(x);
                        else x++;
                    }
                    while (!PutInStorage(mainFunctionItemList, indexList[i], 0, -1, i, storageStartIndex))
                        yield return stateActive;
                }
            }
            yield return stateComplete;
        }

        bool SortBlueprints()
        {
            selfContainedIdentifier = functionList[40];
            if (!IsStateActive)
            {
                InitializeState(SortBlueprintState(), selfContainedIdentifier);
                if (PauseTickRun) return false;
            }
            return RunStateManager;
        }

        IEnumerator<FunctionState> SortBlueprintState()
        {
            IMyAssembler assembler;
            int tempAmount;
            double blueprintPercent, minPercent = 0, maxPercent = 0;
            BlockDefinition managedBlock;
            while (true)
            {
                sortableListMain.Clear();
                foreach (long index in typedIndexes[setKeyIndexAssemblers])
                {
                    if (PauseTickRun) yield return stateActive;
                    managedBlock = managedBlocks[index];
                    if (!IsBlockOk(index) || managedBlock.Settings.NoSorting)
                        continue;

                    assembler = (IMyAssembler)managedBlock.block;
                    if (!assembler.IsQueueEmpty && assembler.Enabled && assembler.Mode == assemblyMode)
                    {
                        blueprintListMain.Clear();
                        assembler.GetQueue(blueprintListMain);
                        if (blueprintListMain.Count > 1)
                        {
                            sortableListMain.Clear();
                            for (int x = 0; x < blueprintListMain.Count; x++)
                            {
                                if (PauseTickRun) yield return stateActive;

                                tempAmount = (int)blueprintListMain[x].Amount;
                                if (x == 0 && assembler.CurrentProgress >= 0.1f)
                                {
                                    if (tempAmount > 10)
                                        tempAmount -= 3;
                                    else
                                        tempAmount = 0;
                                }
                                if (tempAmount > 0)
                                {
                                    blueprintPercent = BlueprintPercentage(blueprintListMain[x].BlueprintId);

                                    minPercent = x == 0 ? blueprintPercent : Math.Min(minPercent, blueprintPercent);

                                    maxPercent = x == 0 ? blueprintPercent : Math.Max(maxPercent, blueprintPercent);

                                    sortableListMain.Add(new SortableObject { amount = blueprintPercent, key = BlueprintSubtype(blueprintListMain[x]) });
                                }
                            }
                            if (minPercent == maxPercent)
                                continue;

                            sortableListMain = sortableListMain.OrderBy(x => x.amount).ToList();

                            for (int s = 0; s < sortableListMain.Count; s++)
                            {
                                if (PauseTickRun) yield return stateActive;
                                for (int a = 0; a < blueprintListMain.Count; a++)
                                {
                                    if (PauseTickRun) yield return stateActive;
                                    if (BlueprintSubtype(blueprintListMain[a]) == sortableListMain[s].key)
                                    {
                                        if (a != s)
                                            assembler.MoveQueueItemRequest(blueprintListMain[a].ItemId, s);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
                yield return stateContinue;
            }
        }

        bool SpreadBlueprints()
        {
            selfContainedIdentifier = functionList[41];
            if (!IsStateActive)
            {
                InitializeState(SpreadBlueprintStateV2(), selfContainedIdentifier);
                if (PauseTickRun) return false;
            }
            return RunStateManager;
        }

        IEnumerator<FunctionState> SpreadBlueprintStateV2()
        {
            SortedList<MyAssemblerMode, SortedList<string, BlueprintSpreadInformation>> blueprintInformation = new SortedList<MyAssemblerMode, SortedList<string, BlueprintSpreadInformation>>();
            List<MyProductionItem> currentProductionList = NewProductionList;
            IMyAssembler currentAssembler;
            string key;
            MyAssemblerMode currentMode;
            bool moveAll;
            double moveAmount, averageAmount, minimalRange = balanceRange;
            List<long> indexList = NewLongList;
            long currentEntityID;

            while (true)
            {
                //Count blueprints, both total counts and individual counts
                foreach (long originIndex in typedIndexes[setKeyIndexAssemblers])
                {
                    if (PauseTickRun) yield return stateActive;

                    if (!IsBlockOk(originIndex)) continue;

                    currentAssembler = (IMyAssembler)managedBlocks[originIndex].block;

                    if (currentAssembler.IsQueueEmpty) continue;

                    currentAssembler.GetQueue(currentProductionList);
                    currentEntityID = currentAssembler.EntityId;
                    currentMode = currentAssembler.Mode;

                    foreach (MyProductionItem productionItem in currentProductionList)
                    {
                        if (PauseTickRun) yield return stateActive;
                        key = BlueprintSubtype(productionItem);

                        if (!blueprintInformation.ContainsKey(currentMode))
                            blueprintInformation[currentMode] = new SortedList<string, BlueprintSpreadInformation>();

                        if (!blueprintInformation[currentMode].ContainsKey(key))
                            blueprintInformation[currentMode][key] = new BlueprintSpreadInformation();

                        blueprintInformation[currentMode][key].AddCount(currentEntityID, (double)productionItem.Amount);
                    }
                    currentProductionList.Clear();
                }

                //Determine which assemblers can use the blueprints known to be in queue
                foreach (long originIndex in typedIndexes[setKeyIndexAssemblers])
                {
                    if (PauseTickRun) yield return stateActive;

                    if (!IsBlockOk(originIndex))
                        continue;

                    currentAssembler = (IMyAssembler)managedBlocks[originIndex].block;
                    currentEntityID = currentAssembler.EntityId;

                    foreach (KeyValuePair<MyAssemblerMode, SortedList<string, BlueprintSpreadInformation>> modePair in blueprintInformation)
                        foreach (KeyValuePair<string, BlueprintSpreadInformation> blueprintPair in modePair.Value)
                        {
                            if (PauseTickRun) yield return stateActive;
                            if (UsableAssembler(managedBlocks[originIndex], MyDefinitionId.Parse($"{blueprintPrefix}/{blueprintPair.Key}"), modePair.Key))
                                blueprintPair.Value.acceptingIndexList.Add(originIndex);
                        }
                }

                //Spread blueprints from each assembler
                for (int x = 0; x < typedIndexes[setKeyIndexAssemblers].Count; x++)
                {
                    if (PauseTickRun) yield return stateActive;

                    if (!IsBlockOk(typedIndexes[setKeyIndexAssemblers][x]))
                        continue;

                    currentAssembler = (IMyAssembler)managedBlocks[typedIndexes[setKeyIndexAssemblers][x]].block;

                    if (currentAssembler.IsQueueEmpty)
                        continue;

                    currentAssembler.GetQueue(currentProductionList);
                    currentMode = currentAssembler.Mode;
                    moveAll = currentAssembler.Mode == disassemblyMode && x < typedIndexes[setKeyIndexAssemblers].Count - 1;

                    for (int i = 0; i < currentProductionList.Count; i++)
                    {
                        if (PauseTickRun) yield return stateActive;
                        key = BlueprintSubtype(currentProductionList[i]);
                        if (!blueprintInformation.ContainsKey(currentMode) || !blueprintInformation[currentMode].ContainsKey(key) || blueprintInformation[currentMode][key].acceptingIndexList.Count == 1 || (!moveAll && i == 0 && currentAssembler.CurrentProgress >= 0.025f))
                            continue;

                        averageAmount = Math.Floor(blueprintInformation[currentMode][key].totalCount / (double)blueprintInformation[currentMode][key].acceptingIndexList.Count);
                        if (averageAmount <= 0)
                            continue;
                        minimalRange = Math.Max(minimalRange, 1.0 / averageAmount);
                        if (moveAll || OverRange((double)currentProductionList[i].Amount, averageAmount, minimalRange))
                        {
                            moveAmount = Math.Floor((double)currentProductionList[i].Amount - averageAmount);
                            if (moveAmount <= 0)
                                continue;
                            currentAssembler.RemoveQueueItem(i, moveAmount);
                            indexList.AddRange(blueprintInformation[currentMode][key].acceptingIndexList);
                            for (int z = 0; z < indexList.Count; z += 0)
                            {
                                if (PauseTickRun) yield return stateActive;
                                if (blueprintInformation[currentMode][key].individualCounts.ContainsKey(indexList[z]) && !UnderRange(blueprintInformation[currentMode][key].individualCounts[indexList[z]], averageAmount, minimalRange))
                                    indexList.RemoveAt(z);
                                else
                                    z++;
                            }
                            if (indexList.Count > 0)
                                while (!DistributeBlueprint(new Blueprint { amount = moveAmount, blueprintID = BlueprintSubtype(currentProductionList[i]) }, moveAmount, indexList, currentMode, false)) yield return stateActive;
                            indexList.Clear();
                        }
                    }
                    currentProductionList.Clear();
                }

                blueprintInformation.Clear();

                yield return stateContinue;
            }
        }

        bool BalanceItems(List<long> indexList)
        {
            selfContainedIdentifier = functionList[24];
            if (!IsStateRunning)
                tempBalanceItemIndexes = indexList;
            if (!IsStateActive)
            {
                InitializeState(BalanceState2(), selfContainedIdentifier);
                if (PauseTickRun) return false;
            }
            return RunStateManager;
        }

        IEnumerator<FunctionState> BalanceState2()
        {
            SortedList<MyItemType, SortedList<long, double>> countList = new SortedList<MyItemType, SortedList<long, double>>();

            while (true)
            {
                countList.Clear();
                // Initial Count
                foreach (long index in tempBalanceItemIndexes)
                {
                    if (PauseTickRun) yield return stateActive;
                    if (!IsBlockOk(index) || managedBlocks[index].Settings.NoSpreading) continue;

                    mainFunctionItemList.Clear();
                    managedBlocks[index].Input.GetItems(mainFunctionItemList);

                    foreach (MyInventoryItem item in mainFunctionItemList)
                    {
                        if (PauseTickRun) yield return stateActive;

                        try
                        {
                            if (!countList.ContainsKey(item.Type)) countList[item.Type] = new SortedList<long, double>();
                        }
                        catch { continue; }

                        if (!countList[item.Type].ContainsKey(index)) countList[item.Type][index] = (double)item.Amount;
                        else countList[item.Type][index] += (double)item.Amount;
                    }
                }
                // Include Blocks with 0
                foreach (KeyValuePair<MyItemType, SortedList<long, double>> kvp in countList)
                    foreach (long index in tempBalanceItemIndexes)
                    {
                        if (PauseTickRun) yield return stateActive;

                        if (IsBlockOk(index) && !managedBlocks[index].Settings.NoSpreading && !kvp.Value.ContainsKey(index) && AcceptsItem(managedBlocks[index], kvp.Key.TypeId, kvp.Key.SubtypeId)) kvp.Value[index] = 0;
                    }
                // Loop through each item
                foreach (KeyValuePair<MyItemType, SortedList<long, double>> kvpA in countList)
                {
                    double min = double.MaxValue, max = double.MinValue, average = 0, excess, transferAmount;
                    // List and sort by count in descending order
                    // Note mininum, maximum, and total values
                    foreach (KeyValuePair<long, double> kvpB in kvpA.Value)
                    {
                        if (PauseTickRun) yield return stateActive;

                        sortableListMain.Add(new SortableObject { amount = kvpB.Value, numberLong = kvpB.Key });
                        min = Math.Min(min, kvpB.Value);
                        max = Math.Max(max, kvpB.Value);
                        average += kvpB.Value;
                    }
                    sortableListMain = sortableListMain.OrderByDescending(b => b.amount).ToList();

                    // Convert the total value to the average desired among all blocks
                    average /= (double)sortableListMain.Count;

                    int averageIndex = -1, // Last index of a count above average
                        belowAverageCount = 0; // Count of items below average

                    // If a block is below average and another is above average
                    if (!FractionalItem(kvpA.Key.TypeId, kvpA.Key.SubtypeId) && max - max >= 2 || FractionalItem(kvpA.Key.TypeId, kvpA.Key.SubtypeId) && (min < average * 0.95 && max > average * 1.05 || min <= average * 0.25))
                    {
                        // Remove blocks close enough to average
                        for (int i = 0; i < sortableListMain.Count; i += 0)
                        {
                            if (PauseTickRun) yield return stateActive;
                            if (sortableListMain[i].amount >= average * 0.95 && sortableListMain[i].amount <= average * 1.001)
                                sortableListMain.RemoveAt(i);
                            else
                            {
                                if (sortableListMain[i].amount > average)
                                {
                                    averageIndex = i;
                                    i++;
                                }
                                else
                                {
                                    sortableListAlternate.Add(sortableListMain[i]);
                                    sortableListMain.RemoveAt(i);
                                }
                            }
                        }
                        if (sortableListMain.Count > 0 && sortableListAlternate.Count > 0)
                            for (int i = 0; i < sortableListMain.Count; i++)
                            {
                                if (PauseTickRun) yield return stateActive;
                                if (!IsBlockOk(sortableListMain[i].numberLong)) continue;

                                mainFunctionItemList.Clear();
                                managedBlocks[sortableListMain[i].numberLong].Input.GetItems(mainFunctionItemList, b => b.Type == kvpA.Key);
                                sortableListMain[i].amount = mainFunctionItemList.Count > 0 ? (double)mainFunctionItemList[0].Amount : 0;
                                excess = mainFunctionItemList.Count > 0 ? (double)mainFunctionItemList[0].Amount - average : 0;
                                if (excess > 0.0)
                                    for (int x = sortableListAlternate.Count - 1; x >= 0; x--)
                                    {
                                        if (PauseTickRun) yield return stateActive;
                                        if (!IsBlockOk(sortableListAlternate[x].numberLong)) continue;
                                        transferAmount = average - sortableListAlternate[x].amount;
                                        if (transferAmount > excess) transferAmount = excess;
                                        if (!FractionalItem(kvpA.Key.TypeId, kvpA.Key.SubtypeId))
                                        {
                                            transferAmount = Math.Floor(transferAmount);
                                            if (transferAmount < 1) break;
                                        }
                                        if (transferAmount > 0.0 && managedBlocks[sortableListAlternate[x].numberLong].Input.TransferItemFrom(managedBlocks[sortableListMain[i].numberLong].Input, mainFunctionItemList[0], (MyFixedPoint)transferAmount))
                                        {
                                            sortableListAlternate[x].amount += transferAmount;
                                            sortableListMain[i].amount -= transferAmount;
                                            excess -= transferAmount;
                                            if (sortableListAlternate[x].amount >= average)
                                            {
                                                sortableListAlternate.RemoveAt(x);
                                                belowAverageCount--;
                                            }
                                        }
                                    }
                            }
                    }
                    sortableListMain.Clear();
                    sortableListAlternate.Clear();
                }

                yield return stateContinue;
            }
        }

        bool CountItemsInList(ItemCollection count, List<long> indexes, string typeID = "", string subtypeID = "")
        {
            selfContainedIdentifier = functionList[8];
            if (!IsStateActive)
            {
                InitializeState(CountListState(count, indexes, typeID, subtypeID), selfContainedIdentifier);
                if (PauseTickRun) return false;
            }
            return RunStateManager;
        }

        IEnumerator<FunctionState> CountListState(ItemCollection count, List<long> indexes, string typeID, string subtypeID)
        {
            foreach (long index in indexes)
            {
                if (PauseTickRun) yield return stateActive;

                countByListA.Clear();
                if (!IsBlockOk(index))
                    continue;

                managedBlocks[index].Input.GetItems(countByListA);
                for (int x = 0; x < countByListA.Count; x++)
                {
                    if (PauseTickRun)
                        yield return stateActive;

                    if ((!TextHasLength(typeID) || countByListA[x].Type.TypeId == typeID) && (!TextHasLength(subtypeID) || countByListA[x].Type.SubtypeId == subtypeID))
                        count.AddItem(countByListA[x]);
                }
            }
            yield return stateComplete;
        }

        bool DistributeItems()
        {
            selfContainedIdentifier = functionList[9];
            if (!IsStateActive)
            {
                InitializeState(DistributeState(), selfContainedIdentifier);
                if (PauseTickRun) return false;
            }
            return RunStateManager;
        }

        IEnumerator<FunctionState> DistributeState()
        {
            string subtypeID;
            SortedList<long, double> acceptingIndexes = NewSortedListLongDouble;
            List<long> tempIndexes = NewLongList;
            while (true)
            {
                foreach (long index in typedIndexes[setKeyIndexStorage])
                {
                    if (PauseTickRun) yield return stateActive;

                    if (!IsBlockOk(index)) continue;

                    mainBlockDefinition = managedBlocks[index];
                    IMyInventory inventory = mainBlockDefinition.Input;
                    if (inventory.ItemCount == 0)
                        continue;

                    mainFunctionItemList.Clear();
                    inventory.GetItems(mainFunctionItemList);

                    for (int x = 0; x < mainFunctionItemList.Count; x++)
                    {
                        if (PauseTickRun) yield return stateActive;

                        if (Distributable(mainFunctionItemList[x], mainBlockDefinition))
                        {
                            subtypeID = mainFunctionItemList[x].Type.SubtypeId;
                            acceptingIndexes.Clear();
                            tempIndexes.Clear();

                            if (IsOre(mainFunctionItemList[x]))
                            {
                                if (RefinedOre(mainFunctionItemList[x]))
                                    tempIndexes.AddRange(typedIndexes[setKeyIndexRefinery]);
                            }
                            else if (IsAmmo(mainFunctionItemList[x]))
                                tempIndexes.AddRange(typedIndexes[setKeyIndexGun]);
                            else if (IsIngot(mainFunctionItemList[x]) && subtypeID == stoneType)
                                tempIndexes.AddRange(typedIndexes[setKeyIndexGravelSifters]);
                            else if (IsComponent(mainFunctionItemList[x]) && subtypeID == canvasType)
                                tempIndexes.AddRange(typedIndexes[setKeyIndexParachute]);

                            if (IsFuel(mainFunctionItemList[x]))
                                tempIndexes.AddRange(typedIndexes[setKeyIndexReactor]);

                            if (IsGas(mainFunctionItemList[x]))
                                tempIndexes.AddRange(typedIndexes[setKeyIndexGasGenerators]);

                            if (tempIndexes.Count > 0)
                            {
                                foreach (long subIndex in tempIndexes)
                                {
                                    if (PauseTickRun) yield return stateActive;
                                    acceptingIndexes[subIndex] = -1;
                                }

                                while (!DistributeItem(mainFunctionItemList[x], mainBlockDefinition, acceptingIndexes))
                                    yield return stateActive;
                            }
                        }
                    }
                }
                acceptingIndexes.Clear();
                yield return stateContinue;
            }
        }

        bool DistributeItem(MyInventoryItem item, BlockDefinition block, SortedList<long, double> acceptingIndexes, double specifixMax = -1)
        {
            selfContainedIdentifier = functionList[10];
            if (!IsStateRunning)
            {
                tempDistributeItem = item;
                tempDistributeItemBlockDefinition = block;
                tempDistributeItemIndexes = acceptingIndexes;
                tempDistributeItemMax = specifixMax;
            }
            if (!IsStateActive)
            {
                InitializeState(DistributeItemState(), selfContainedIdentifier);
                if (PauseTickRun) return false;
            }
            return RunStateManager;
        }

        IEnumerator<FunctionState> DistributeItemState()
        {
            IMyTerminalBlock managedBlock;
            SortedList<long, double> tempSortedIndexList = NewSortedListLongDouble;
            List<long> tempIndexList = NewLongList;
            foreach (KeyValuePair<long, double> kvp in tempDistributeItemIndexes)
                tempSortedIndexList[kvp.Key] = kvp.Value;

            double contained, totalAmount, splitAmount, originalSplitAmount,
                    maxAmount, balanceRange, balancedShare, tempMax, remainder = 0;

            for (int i = 0; i < tempSortedIndexList.Count; i += 0)
            {
                if (PauseTickRun) yield return stateActive;

                if (IsBlockOk(tempSortedIndexList.Keys[i]) && CurrentVolumePercentage(tempSortedIndexList.Keys[i]) < 0.99 && AcceptsItem(managedBlocks[tempSortedIndexList.Keys[i]], tempDistributeItem))
                {
                    tempIndexList.Add(tempSortedIndexList.Keys[i]);
                    i++;
                }
                else
                    tempSortedIndexList.Remove(tempSortedIndexList.Keys[i]);
            }

            if (tempSortedIndexList.Count > 0)
            {
                itemCollectionMain.Clear();

                while (!CountItemsInList(itemCollectionMain, tempIndexList, tempDistributeItem.Type.TypeId, tempDistributeItem.Type.SubtypeId))
                    yield return stateActive;

                tempIndexList.Clear();

                totalAmount = (double)tempDistributeItem.Amount;
                balanceRange = GetKeyDouble(setKeyBalanceRange);
                if (tempDistributeItemMax > 0 && totalAmount > tempDistributeItemMax)
                    totalAmount = tempDistributeItemMax;

                bool fractional = FractionalItem(tempDistributeItem);

                itemCollectionMain.AddItem(tempDistributeItem.Type.TypeId, tempDistributeItem.Type.SubtypeId, new VariableItemCount(totalAmount));

                if (itemCollectionMain.ItemTypeCount > 0 && tempSortedIndexList.Count > 0)
                {
                    balancedShare = itemCollectionMain.ItemCount(tempDistributeItem) / tempSortedIndexList.Count;
                    for (int i = 0; i < tempSortedIndexList.Count; i += 0)
                    {
                        if (PauseTickRun) yield return stateActive;

                        contained = tempSortedIndexList.Values[i];
                        if (contained == -1)
                        {
                            contained = 0;
                            managedBlock = managedBlocks[tempSortedIndexList.Keys[i]].block;
                            while (!AmountContained(ref contained, tempDistributeItem, managedBlock))
                                yield return stateActive;

                            tempSortedIndexList[tempSortedIndexList.Keys[i]] = contained;
                        }
                        if (contained > balancedShare + (balancedShare * balanceRange))
                            tempSortedIndexList.RemoveAt(i);
                        else
                            i++;
                    }
                }

                if (tempSortedIndexList.Count > 0)
                {
                    long key;
                    bool foundLimit;
                    int indexCount = 0;
                    foreach (KeyValuePair<long, double> kvp in tempSortedIndexList)
                    {
                        maxAmount = DefaultMax(tempDistributeItem, managedBlocks[kvp.Key]);
                        tempMax = managedBlocks[kvp.Key].Settings.limits.ItemCount(out foundLimit, tempDistributeItem, managedBlocks[kvp.Key].block);
                        if (foundLimit) maxAmount = tempMax;

                        foundLimit = maxAmount < double.MaxValue;

                        splitAmount = totalAmount / ((double)tempSortedIndexList.Count - indexCount);
                        contained = kvp.Value;
                        key = kvp.Key;
                        if (PauseTickRun) yield return stateActive;

                        if (contained == -1)
                        {
                            contained = 0;
                            managedBlock = managedBlocks[kvp.Key].block;
                            if (foundLimit)
                                while (!AmountContained(ref contained, tempDistributeItem, managedBlock))
                                    yield return stateActive;
                        }
                        if (splitAmount + contained > maxAmount)
                            splitAmount = maxAmount - contained;

                        if (!fractional)
                        {
                            remainder += splitAmount - Math.Floor(splitAmount);
                            splitAmount = Math.Floor(splitAmount);
                            if (remainder >= 1 && (contained + splitAmount + 1 <= maxAmount))
                            {
                                splitAmount++;
                                remainder--;
                            }
                        }
                        if (indexCount + 1 == tempSortedIndexList.Count && splitAmount + remainder <= maxAmount)
                            splitAmount += remainder;

                        originalSplitAmount = splitAmount;
                        while (!Transfer(ref splitAmount, tempDistributeItemBlockDefinition.Input, managedBlocks[kvp.Key], tempDistributeItem))
                            yield return stateActive;

                        if (splitAmount > 0)
                            totalAmount -= originalSplitAmount - splitAmount;
                        indexCount++;
                    }
                }
            }
            yield return stateComplete;
        }

        bool AmountContained(ref double amount, MyInventoryItem item, IMyTerminalBlock block)
        {
            return AmountContained(ref amount, item.Type.TypeId, item.Type.SubtypeId, block);
        }

        bool AmountContained(ref double amount, string itemID, string subtypeID, IMyTerminalBlock block)
        {
            selfContainedIdentifier = functionList[11];
            if (!IsStateActive)
            {
                InitializeState(AmountContainedState(itemID, subtypeID, block), selfContainedIdentifier);
                if (PauseTickRun) return false;
            }

            if (RunStateManager)
            {
                amount = countedAmount;
                countedAmount = 0;
                return true;
            }

            return false;
        }

        IEnumerator<FunctionState> AmountContainedState(string itemID, string subtypeID, IMyTerminalBlock block)
        {
            amountContainedListA.Clear();
            block.GetInventory(0).GetItems(amountContainedListA);
            for (int i = 0; i < amountContainedListA.Count; i++)
            {
                if (PauseTickRun)
                    yield return stateActive;

                if (amountContainedListA[i].Type.TypeId == itemID && amountContainedListA[i].Type.SubtypeId == subtypeID)
                    countedAmount += (double)amountContainedListA[i].Amount;
            }
            yield return stateComplete;
        }

        bool ProcessLimits()
        {
            selfContainedIdentifier = functionList[12];
            if (!IsStateActive)
            {
                InitializeState(ProcessLimitsState(), selfContainedIdentifier);
                if (PauseTickRun) return false;
            }
            return RunStateManager;
        }

        IEnumerator<FunctionState> ProcessLimitsState()
        {
            while (true)
            {
                double limit = 0, excess;
                foreach (long index in typedIndexes[setKeyIndexLimit])
                {
                    if (PauseTickRun) yield return stateActive;

                    if (!IsBlockOk(index))
                        continue;

                    mainBlockDefinition = managedBlocks[index];

                    mainFunctionItemList.Clear();
                    mainBlockDefinition.Input.GetItems(mainFunctionItemList);
                    if (PauseTickRun) yield return stateActive;

                    for (int x = 0; x < mainFunctionItemList.Count; x++)
                    {
                        if (PauseTickRun) yield return stateActive;

                        if (GetSetLimit(mainBlockDefinition, ref limit, mainFunctionItemList[x]))
                        {
                            excess = (double)mainFunctionItemList[x].Amount - limit;
                            if (excess > 0)
                                while (!PutInStorage(new List<MyInventoryItem> { mainFunctionItemList[x] }, index, 0, excess)) yield return stateActive;
                        }
                    }
                }
                yield return stateContinue;
            }
        }

        bool SortItems()
        {
            selfContainedIdentifier = functionList[13];
            if (!IsStateActive)
            {
                InitializeState(SortState(), selfContainedIdentifier);
                if (PauseTickRun) return false;
            }
            return RunStateManager;
        }

        IEnumerator<FunctionState> SortState()
        {
            IMyTerminalBlock block;
            while (true)
            {
                foreach (long index in typedIndexes[setKeyIndexSortable])
                {
                    if (PauseTickRun) yield return stateActive;
                    if (!IsBlockOk(index)) continue;

                    mainBlockDefinition = managedBlocks[index];
                    block = mainBlockDefinition.block;

                    if (mainBlockDefinition.Settings.RemoveInput || (!mainBlockDefinition.Settings.manual && Sortable(mainBlockDefinition, 0)))
                    {
                        mainFunctionItemList.Clear();
                        mainBlockDefinition.Input.GetItems(mainFunctionItemList);
                        if (!mainBlockDefinition.Settings.RemoveInput)
                            for (int x = 0; x < mainFunctionItemList.Count; x += 0)
                            {
                                if (PauseTickRun) yield return stateActive;

                                if (!Sortable(mainFunctionItemList[x], mainBlockDefinition))
                                    mainFunctionItemList.RemoveAt(x);
                                else x++;
                            }
                        while (!PutInStorage(mainFunctionItemList, index, 0)) yield return stateActive;
                    }
                    if (block.InventoryCount > 1 && (mainBlockDefinition.Settings.RemoveOutput || (!mainBlockDefinition.Settings.manual && Sortable(mainBlockDefinition, 1))))
                    {
                        mainFunctionItemList.Clear();
                        block.GetInventory(1).GetItems(mainFunctionItemList);
                        if (!mainBlockDefinition.Settings.RemoveOutput)
                            for (int x = 0; x < mainFunctionItemList.Count; x += 0)
                            {
                                if (PauseTickRun) yield return stateActive;
                                if (!Sortable(mainFunctionItemList[x], mainBlockDefinition, 1))
                                    mainFunctionItemList.RemoveAt(x);
                                else x++;
                            }

                        while (!PutInStorage(mainFunctionItemList, index, 1))
                            yield return stateActive;
                    }
                }
                yield return stateContinue;
            }
        }

        bool PutInStorage(List<MyInventoryItem> items, long blockIndex, int inventoryIndex, double max = -1, int priorityMax = -1, int storageIndexStart = 0)
        {
            selfContainedIdentifier = functionList[14];
            if (!IsStateRunning)
            {
                tempStorageItemList = items;
                tempStorageBlockIndex = blockIndex;
                tempStorageInventoryIndex = inventoryIndex;
                tempStorageMax = max;
                tempStoragePriorityMax = priorityMax;
                tempStorageIndexStart = storageIndexStart;
            }
            if (!IsStateActive)
            {
                InitializeState(PutInStorageState(), selfContainedIdentifier);
                if (PauseTickRun) return false;
            }
            return RunStateManager;
        }

        IEnumerator<FunctionState> PutInStorageState()
        {
            IMyTerminalBlock destBlock, origBlock;
            IMyInventory originInventory;
            List<long> tempIndexList = NewLongList;
            string itemID, typeID;
            while (true)
            {
                storageDefinitionA = managedBlocks[tempStorageBlockIndex];
                origBlock = storageDefinitionA.block;
                string typeKey;

                foreach (MyInventoryItem item in tempStorageItemList)
                {
                    tempIndexList.Clear();
                    itemID = item.Type.ToString();
                    typeID = item.Type.TypeId;
                    transferAmount = (double)item.Amount;
                    if (tempStorageMax > 0 && transferAmount > tempStorageMax)
                        transferAmount = tempStorageMax;

                    if (transferAmount > (double)item.Amount)
                        transferAmount = (double)item.Amount;

                    originInventory = origBlock.GetInventory(tempStorageInventoryIndex);
                    bool bottleException = fillingBottles && IsBottle(item) && !(origBlock is IMyGasGenerator || origBlock is IMyGasTank);
                    if (transferAmount > 0)
                    {
                        if (!bottleException && itemCategoryDictionary.ContainsKey(itemID) && indexesStorageLists.ContainsKey(itemCategoryDictionary[itemID]))
                            tempIndexList.AddRange(indexesStorageLists[itemCategoryDictionary[itemID]]);

                        if (tempIndexList.Count == 0)
                        {
                            typeKey =
                                bottleException ? "" :
                                IsComponent(item) ? componentKeyword :
                                IsTool(item) ? toolKeyword :
                                IsAmmo(item) ? ammoKeyword :
                                IsIngot(item) ? ingotKeyword :
                                IsOre(item) ? oreKeyword : "";

                            if (bottleException)
                            {
                                tempIndexList.AddRange(typedIndexes[item.Type.TypeId == oxyBottleType ? setKeyIndexOxygenTank : setKeyIndexHydrogenTank]);

                                tempIndexList.AddRange(typedIndexes[setKeyIndexGasGenerators]);
                            }
                            if (TextHasLength(typeKey) && indexesStorageLists.ContainsKey(typeKey))
                                tempIndexList.AddRange(indexesStorageLists[typeKey]);
                        }
                        if (!bottleException && tempIndexList.Count == 0)
                            tempIndexList.AddRange(typedIndexes[setKeyIndexStorage]);

                        for (int i = tempStorageIndexStart; i < tempIndexList.Count && (tempStoragePriorityMax <= 0 || i < tempStoragePriorityMax); i++)
                        {
                            if (PauseTickRun)
                                yield return stateActive;

                            if (!IsBlockOk(tempIndexList[i]) || CurrentVolumePercentage(tempIndexList[i]) >= 0.985)
                                continue;

                            destBlock = managedBlocks[tempIndexList[i]].block;
                            if (storageDefinitionA.block != destBlock)
                            {
                                while (!Transfer(ref transferAmount, originInventory, managedBlocks[tempIndexList[i]], item))
                                    yield return stateActive;

                                if (transferAmount <= 0)
                                    break;
                            }
                        }
                    }
                }

                yield return stateComplete;
            }
        }

        bool CountBlueprints()
        {
            selfContainedIdentifier = functionList[15];
            if (!IsStateActive)
            {
                InitializeState(CountBlueprintState(), selfContainedIdentifier);
                if (PauseTickRun) return false;
            }
            return RunStateManager;
        }

        IEnumerator<FunctionState> CountBlueprintState()
        {
            IMyAssembler assembler;
            bool assembly, queueAssembly = GetKeyBool(setKeyToggleQueueAssembly);
            List<Blueprint> blueprints = new List<Blueprint>();
            Blueprint blueprint;
            while (true)
            {
                blueprints.Clear();
                foreach (long index in typedIndexes[setKeyIndexAssemblers])
                {
                    if (PauseTickRun) yield return stateActive;

                    if (!IsBlockOk(index))
                        continue;

                    mainBlockDefinition = managedBlocks[index];
                    assembler = (IMyAssembler)mainBlockDefinition.block;
                    blueprintListMain.Clear();
                    assembler.GetQueue(blueprintListMain);
                    if (blueprintListMain.Count == 0)
                        assembler.Mode = assemblyMode;

                    assembly = ((IMyAssembler)mainBlockDefinition.block).Mode == assemblyMode;
                    for (int x = 0; x < blueprintListMain.Count; x++)
                    {
                        if (PauseTickRun) yield return stateActive;

                        AddBlueprintAmount(blueprintListMain[x], assembly);
                    }
                }
                foreach (KeyValuePair<string, SortedList<string, ItemDefinition>> kvpA in itemListMain)
                {
                    if (PauseTickRun) yield return stateActive;

                    foreach (KeyValuePair<string, ItemDefinition> kvpB in kvpA.Value)
                    {
                        if (PauseTickRun) yield return stateActive;

                        kvpB.Value.SwitchAssemblyCount();
                        kvpB.Value.SetDifferenceNeeded(allowedExcessPercent);
                        if (queueAssembly)
                        {
                            blueprint = new Blueprint { blueprintID = kvpB.Value.blueprintID, amount = kvpB.Value.currentNeededAssembly };
                            if (blueprint.amount >= 1)
                                blueprints.Add(blueprint.Clone());
                        }
                    }
                }

                assemblyNeededByMachine.Clear();
                if (queueAssembly)
                    while (!AddAssemblyNeeded(blueprints))
                        yield return stateActive;
                yield return stateContinue;
            }
        }

        bool AddAssemblyNeeded(List<Blueprint> blueprints)
        {
            selfContainedIdentifier = functionList[52];
            if (!IsStateActive)
            {
                InitializeState(AddAssemblyState(blueprints), selfContainedIdentifier);
                if (PauseTickRun) return false;
            }
            return RunStateManager;
        }

        IEnumerator<FunctionState> AddAssemblyState(List<Blueprint> blueprints)
        {
            MyDefinitionId blueprintID;
            IMyAssembler assembler;
            string key;
            foreach (long index in typedIndexes[setKeyIndexAssemblers])
            {
                if (PauseTickRun) yield return stateActive;
                if (!IsBlockOk(index))
                    continue;
                assembler = (IMyAssembler)managedBlocks[index].block;

                foreach (Blueprint blueprint in blueprints)
                {
                    if (PauseTickRun) yield return stateActive;
                    blueprintID = MyDefinitionId.Parse(MakeBlueprint(blueprint));
                    if (assembler.CanUseBlueprint(blueprintID))
                    {
                        key = BlockSubtype(assembler);

                        assemblyNeededByMachine.Add(BlockSubtype(assembler));
                        break;
                    }
                }
            }

            yield return stateComplete;
        }

        bool Count()
        {
            selfContainedIdentifier = functionList[16];
            if (!IsStateActive)
            {
                InitializeState(CountState(), selfContainedIdentifier);
                if (PauseTickRun) return false;
            }
            return RunStateManager;
        }

        IEnumerator<FunctionState> CountState()
        {
            IMyTerminalBlock block;
            while (true)
            {
                activeOres = 0;
                foreach (long index in typedIndexes[setKeyIndexInventory])
                {
                    if (PauseTickRun) yield return stateActive;

                    if (!IsBlockOk(index) || managedBlocks[index].Settings.NoCounting)
                        continue;

                    block = managedBlocks[index].block;

                    for (int inv = 0; inv < block.InventoryCount; inv++)
                    {
                        if (PauseTickRun) yield return stateActive;

                        mainFunctionItemList.Clear();
                        block.GetInventory(inv).GetItems(mainFunctionItemList);

                        for (int x = 0; x < mainFunctionItemList.Count; x++)
                        {
                            if (PauseTickRun) yield return stateActive;
                            AddAmount(mainFunctionItemList[x]);
                        }
                    }
                }
                foreach (KeyValuePair<string, SortedList<string, ItemDefinition>> kvpA in itemListMain)
                {
                    if (PauseTickRun)
                        yield return stateActive;

                    foreach (KeyValuePair<string, ItemDefinition> kvpB in kvpA.Value)
                    {
                        kvpB.Value.SwitchCount(dynamicQuotaMaxMultiplier, useDynamicQuota && IsBlueprint(kvpB.Value.blueprintID), dynamicQuotaNegativeThreshold, dynamicQuotaPositiveThreshold, dynamicQuotaMultiplierIncrement, increaseDynamicQuotaWhenLow);
                        if (IsOre(kvpB.Value.typeID) && kvpB.Value.subtypeID != "Ice" && kvpB.Value.amount >= 0.5 && kvpB.Value.refine)
                            activeOres++;
                    }
                }

                yield return stateContinue;
            }
        }

        bool Scan()
        {
            selfContainedIdentifier = functionList[17];
            if (!IsStateActive)
            {
                InitializeState(ScanState(), selfContainedIdentifier);
                return false;
            }
            return RunStateManager;
        }

        IEnumerator<FunctionState> ScanState()
        {
            yield return stateActive;
            long currentEntityID;
            IMyTextSurfaceProvider provider;
            IMyTerminalBlock currentBlock;
            BlockDefinition currentDefinition;
            bool currentPriority, emptyLoadout, isClone;
            string blockDef, typeID, subtypeID;
            IMyCubeGrid currentGrid;

            while (true)
            {
                // ----------------------------------------------------
                // ------------- Set Variables -------------
                // ----------------------------------------------------

                //Clear indexes
                for (int i = 0; i < typedIndexes.Count; i++)
                {
                    if (PauseTickRun) yield return stateActive;
                    typedIndexes.Values[i].Clear();
                }
                for (int i = 0; i < indexesStorageLists.Count; i++)
                {
                    if (PauseTickRun) yield return stateActive;
                    indexesStorageLists.Values[i].Clear();
                }

                //Clear cached information
                priorityCategories.Clear();
                priorityTypes.Clear();
                prioritySystemActivated = false;

                //Get scan variables
                bool sameGridOnly = GetKeyBool(setKeySameGridOnly),
                     addLoadoutsToQuota = GetKeyBool(setKeyAddLoadoutsToQuota),
                     conveyorControl = GetKeyBool(setKeyControlConveyors);

                // ----------------------------------------------------
                // ------------- Scan Blocks -------------
                // ----------------------------------------------------

                //Get exclusion groups
                gtSystem.GetBlockGroups(groupList, b => LeadsString(b.Name, $"nds {exclusionKeyword}"));
                //Record excluded IDs in removal list
                foreach (IMyBlockGroup group in groupList)
                {
                    if (PauseTickRun) yield return stateActive;
                    group.GetBlocks(groupBlocks);
                    foreach (IMyTerminalBlock block in groupBlocks)
                    {
                        if (PauseTickRun) yield return stateActive;
                        excludedIDs.Add(block.EntityId);
                    }
                    groupBlocks.Clear();
                }
                groupList.Clear();
                //excludedIDs

                //Scan accessible blocks
                gtSystem.GetBlocksOfType<IMyTerminalBlock>(scannedBlocks);
                //excludedIDs, scannedBlocks (all)

                //Remove globally filtered blocked from scan list
                for (int i = 0; i < scannedBlocks.Count; i += 0)
                {
                    if (PauseTickRun) yield return stateActive;
                    if (!ScanFilter(scannedBlocks[i], excludedIDs))
                        scannedBlocks.RemoveAt(i);
                    else
                        i++;
                }
                //excludedIDs, scannedBlocks (filtered)

                //Record accessible IDs
                foreach (IMyTerminalBlock block in scannedBlocks)
                {
                    if (PauseTickRun) yield return stateActive;
                    accessibleIDs.Add(block.EntityId);
                }
                //excludedIDs, scannedBlocks (filtered), accessibleIDs

                //Add scanned blocks to managed list
                foreach (IMyTerminalBlock block in scannedBlocks)
                {
                    if (PauseTickRun) yield return stateActive;
                    if (!managedBlocks.ContainsKey(block.EntityId))
                    {
                        managedBlocks[block.EntityId] = new BlockDefinition(block);
                        managedBlocks[block.EntityId].Settings.Initialize(crossGridKeyword, includeGridKeyword, exclusionKeyword, excludeGridKeyword);
                    }
                }
                scannedBlocks.Clear();
                //excludedIDs, accessibleIDs, managedBlocks (all)

                //Process block options
                foreach (KeyValuePair<long, BlockDefinition> kvp in managedBlocks)
                {
                    if (PauseTickRun) yield return stateActive;

                    if (!kvp.Value.IsClone) while (!ProcessBlockOptions(kvp.Value)) yield return stateActive;
                    else if (!TextHasLength(kvp.Value.DataSource)) setRemoveIDs.Add(kvp.Key);

                    if (kvp.Value.Settings.CrossGrid)
                        includedIDs.Add(kvp.Value.block.EntityId);

                    if (kvp.Value.Settings.ExcludedGrid)
                        excludedGridList.Add(kvp.Value.block.CubeGrid);

                    if (kvp.Value.Settings.IncludeGrid)
                        gridList.Add(kvp.Value.block.CubeGrid);
                }

                //excludedIDs, excludedGridList, accessibleIDs, managedBlocks (all-processed), gridList

                //Process clones separating from cloning
                foreach (long index in setRemoveIDs)
                    if (!clonedEntityIDs.Contains(index) && managedBlocks.ContainsKey(index))
                    {
                        managedBlocks[index].SetClone(null);
                        managedBlocks[index].Settings.Initialize(crossGridKeyword, includeGridKeyword, exclusionKeyword, excludeGridKeyword);
                    }

                setRemoveIDs.Clear();
                clonedEntityIDs.Clear();

                //Get included group blocks by ID
                if (sameGridOnly)
                {
                    gridList.Add(Me.CubeGrid);
                    gtSystem.GetBlockGroups(groupList, b => LeadsString(b.Name, $"nds {crossGridKeyword}"));
                    foreach (IMyBlockGroup group in groupList)
                    {
                        if (PauseTickRun)
                            yield return stateActive;

                        group.GetBlocks(groupBlocks);
                        foreach (IMyTerminalBlock block in groupBlocks)
                        {
                            if (PauseTickRun) yield return stateActive;

                            includedIDs.Add(block.EntityId);
                        }
                        groupBlocks.Clear();
                    }
                    groupList.Clear();
                }
                //excludedIDs, accessibleIDs, managedBlocks (all-processed), gridList, includedIDs

                //Queue managed blocks for removal if not accessible or excluded in some way
                foreach (KeyValuePair<long, BlockDefinition> kvp in managedBlocks)
                {
                    if (PauseTickRun) yield return stateActive;
                    currentEntityID = kvp.Value.block.EntityId;
                    currentGrid = kvp.Value.block.CubeGrid;
                    if (!accessibleIDs.Contains(currentEntityID) ||
                        kvp.Value.Settings.Excluded ||
                        excludedIDs.Contains(currentEntityID) ||
                        excludedGridList.Contains(currentGrid) ||
                        (sameGridOnly && !kvp.Value.Settings.CrossGrid && !gridList.Contains(currentGrid) && !includedIDs.Contains(currentEntityID)))
                        setRemoveIDs.Add(kvp.Key);
                }
                excludedIDs.Clear();
                accessibleIDs.Clear();
                gridList.Clear();
                excludedGridList.Clear();
                includedIDs.Clear();
                //managedBlocks (all-processed), setRemoveIDs

                //Remove blocks queued for removal
                foreach (long index in setRemoveIDs)
                {
                    if (PauseTickRun) yield return stateActive;
                    managedBlocks.Remove(index);
                }
                setRemoveIDs.Clear();
                //managedBlocks (filtered-processed)


                // ----------------------------------------------------
                // ------------- Process Blocks -------------
                // ----------------------------------------------------

                //Clear unused item category indexes
                for (int i = 0; i < indexesStorageLists.Count; i += 0)
                {
                    if (PauseTickRun)
                        yield return stateActive;
                    if (!itemCategoryList.Contains(indexesStorageLists.Keys[i]))
                        indexesStorageLists.RemoveAt(i);
                    else i++;
                }

                //Process managed blocks
                foreach (long index in managedBlocks.Keys)
                {
                    if (PauseTickRun) yield return stateActive;

                    currentDefinition = managedBlocks[index];
                    isClone = currentDefinition.IsClone;
                    currentBlock = currentDefinition.block;
                    currentPriority = currentDefinition.Settings.priority != 1.0;
                    prioritySystemActivated = prioritySystemActivated || currentPriority;
                    currentDefinition.isGravelSifter = IsGravelSifter(currentBlock);

                    if (!(IsPanelProvider(currentBlock)) || currentBlock is IMyShipController) //Process non-panel blocks
                    {
                        //Index automated blocks
                        if (!currentDefinition.Settings.manual)
                        {
                            if (currentDefinition.isGravelSifter)
                            {
                                typedIndexes[setKeyIndexGravelSifters].Add(index);
                                if (currentPriority)
                                    priorityTypes.Add(setKeyIndexGravelSifters);
                            }
                            else if (IsGun(currentDefinition))
                            {
                                typedIndexes[setKeyIndexGun].Add(index);
                                if (currentPriority)
                                    priorityTypes.Add(setKeyIndexGun);
                            }
                            else if (currentBlock is IMyAssembler)
                            {
                                typedIndexes[setKeyIndexAssemblers].Add(index);
                                if (currentPriority)
                                    priorityTypes.Add(setKeyIndexAssemblers);
                                if (currentDefinition.monitoredAssembler == null)
                                    currentDefinition.monitoredAssembler = new MonitoredAssembler { assembler = (IMyAssembler)currentBlock };
                            }
                            else if (currentBlock is IMyGasGenerator)
                            {
                                typedIndexes[setKeyIndexGasGenerators].Add(index);
                                if (currentPriority)
                                    priorityTypes.Add(setKeyIndexGasGenerators);
                            }
                            else if (currentBlock is IMyGasTank)
                            {
                                if (ContainsString(BlockSubtype(currentBlock), "hydrogen"))
                                {
                                    typedIndexes[setKeyIndexHydrogenTank].Add(index);
                                    if (currentPriority)
                                        priorityTypes.Add(setKeyIndexHydrogenTank);
                                }
                                else
                                {
                                    typedIndexes[setKeyIndexOxygenTank].Add(index);
                                    if (currentPriority)
                                        priorityTypes.Add(setKeyIndexOxygenTank);
                                }
                            }
                            else if (currentBlock is IMyParachute)
                            {
                                typedIndexes[setKeyIndexParachute].Add(index);
                                if (currentPriority)
                                    priorityTypes.Add(setKeyIndexParachute);
                            }
                            else if (currentBlock is IMyReactor)
                            {
                                typedIndexes[setKeyIndexReactor].Add(index);
                                if (currentPriority)
                                    priorityTypes.Add(setKeyIndexReactor);
                            }
                            else if (currentBlock is IMyRefinery)
                            {
                                typedIndexes[setKeyIndexRefinery].Add(index);
                                if (currentPriority)
                                    priorityTypes.Add(setKeyIndexRefinery);
                            }
                            if (currentDefinition.Settings.Storage)
                            {
                                typedIndexes[setKeyIndexStorage].Add(index);
                                if (currentPriority)
                                    priorityCategories.Add(setKeyIndexStorage);
                                foreach (string storageType in currentDefinition.Settings.storageCategories)
                                {
                                    if (PauseTickRun) yield return stateActive;
                                    if (storageType == "all")
                                        for (int x = 0; x < indexesStorageLists.Count; x++)
                                        {
                                            if (PauseTickRun) yield return stateActive;
                                            indexesStorageLists.Values[x].Add(index);
                                            if (currentPriority)
                                                priorityCategories.Add(indexesStorageLists.Keys[x]);
                                        }
                                    else if (indexesStorageLists.ContainsKey(storageType))
                                    {
                                        indexesStorageLists[storageType].Add(index);
                                        if (currentPriority)
                                            priorityCategories.Add(storageType);
                                    }
                                }
                            }
                        }
                        //Index blocks with inventories
                        if (currentDefinition.HasInventory)
                        {
                            typedIndexes[setKeyIndexInventory].Add(index);
                            if (currentPriority)
                                priorityTypes.Add(setKeyIndexInventory);
                            emptyLoadout = currentDefinition.Settings.loadout.ItemTypeCount == 0 && !currentDefinition.Settings.manual;
                            if (IsGun(currentDefinition))
                            {
                                blockDef = BlockSubtype(currentBlock);
                                if (currentDefinition.Input.ItemCount > 0)
                                    gunAmmoDictionary[blockDef] = $"{((MyInventoryItem)currentDefinition.Input.GetItemAt(0)).Type}";
                                if (!isClone && emptyLoadout && gunAmmoDictionary.ContainsKey(blockDef))
                                {
                                    SplitID(gunAmmoDictionary[blockDef], out typeID, out subtypeID);
                                    currentDefinition.Settings.loadout.AddItem(typeID, subtypeID, new VariableItemCount(DefaultMax(typeID, subtypeID, currentDefinition)));
                                }
                            }
                            if (!isClone && currentBlock is IMyParachute && emptyLoadout)
                                currentDefinition.Settings.loadout.AddItem(componentType, canvasType, new VariableItemCount(DefaultMax(componentType, canvasType, currentDefinition)));

                            if (currentDefinition.Settings.loadout.ItemTypeCount > 0)
                            {
                                typedIndexes[setKeyIndexLoadout].Add(index);
                                if (addLoadoutsToQuota)
                                    itemCollectionProcessTotalLoadout.AddCollection(currentDefinition.Settings.loadout, currentDefinition.block);
                                if (currentPriority)
                                    priorityTypes.Add(setKeyIndexLoadout);
                            }
                            if (currentDefinition.Settings.limits.ItemTypeCount > 0)
                            {
                                if (currentPriority)
                                    priorityTypes.Add(setKeyIndexLimit);
                                typedIndexes[setKeyIndexLimit].Add(index);
                            }
                            if ((!currentDefinition.Settings.manual || currentDefinition.Settings.RemoveInput || currentDefinition.Settings.RemoveOutput) && !(currentDefinition.Settings.KeepInput && currentDefinition.Settings.KeepOutput))
                                for (int i = 0; i < currentDefinition.block.InventoryCount; i++)
                                    if (Sortable(currentDefinition, i))
                                    {
                                        typedIndexes[setKeyIndexSortable].Add(index);
                                        if (currentPriority)
                                            priorityTypes.Add(setKeyIndexSortable);
                                        break;
                                    }
                            if (conveyorControl)
                                ConveyorControl(currentDefinition);
                        }
                    }
                    if (currentBlock is IMyTextPanel)
                    {
                        while (!ProcessPanelOptions(currentDefinition)) yield return stateActive;

                        if (IsPanel(currentDefinition))
                        {
                            typedIndexes[setKeyIndexPanel].Add(index);
                            panelMaster.CheckPanel(currentDefinition);
                        }
                    }
                    else if (IsPanelProvider(currentBlock) && ContainsString(currentBlock.CustomName, panelTag))
                    {
                        provider = (IMyTextSurfaceProvider)currentBlock;
                        for (int s = 0; s < provider.SurfaceCount; s++)
                        {
                            if (PauseTickRun) yield return stateActive;

                            while (!ProcessPanelOptions(currentDefinition, s)) yield return stateActive;

                            if (IsPanel(currentDefinition, s))
                            {
                                typedIndexes[setKeyIndexPanel].Add(index);
                                panelMaster.CheckPanel(currentDefinition, s);
                            }
                        }
                    }
                    if (currentDefinition.Settings.logicComparisons.Count > 0)
                        typedIndexes[setKeyIndexLogic].Add(currentBlock.EntityId);
                }

                //Order blocks by priority and remove duplicates
                foreach (KeyValuePair<string, List<long>> kvp in typedIndexes)
                    while (!OrderListByPriority(kvp.Value, priorityTypes.Contains(kvp.Key))) yield return stateActive;
                foreach (KeyValuePair<string, List<long>> kvp in indexesStorageLists)
                    while (!OrderListByPriority(kvp.Value, priorityCategories.Contains(kvp.Key))) yield return stateActive;

                while (!SetBlockQuotas(itemCollectionProcessTotalLoadout)) yield return stateActive;
                itemCollectionProcessTotalLoadout.Clear();

                //Find spanned panels
                foreach (long index in typedIndexes[setKeyIndexPanel])
                {
                    if (PauseTickRun) yield return stateActive;
                    foreach (PanelMasterClass.PanelDefinition panelDefinition in managedBlocks[index].panelDefinitionList.Values)
                    {
                        panelDefinition.spannedPanelList.Clear();
                        if (panelDefinition.span)
                            foreach (long counterIndex in typedIndexes[setKeyIndexPanel])
                            {
                                if (PauseTickRun) yield return stateActive;
                                foreach (PanelMasterClass.PanelDefinition opposingPanel in managedBlocks[counterIndex].panelDefinitionList.Values)
                                    if (opposingPanel.panelType == PanelType.Span && StringsMatch(panelDefinition.childSpanKey, opposingPanel.spanKey) && index != counterIndex || panelDefinition.surfaceIndex != opposingPanel.surfaceIndex)
                                        panelDefinition.spannedPanelList.Add(new SpanKey { index = counterIndex, surfaceIndex = opposingPanel.surfaceIndex });
                            }
                    }
                }

                yield return stateContinue;
            }
        }

        bool CollectionToString(StringBuilder builder, ItemCollection collection, bool includeAmount = true)
        {
            if (collection == null || collection.ItemTypeCount == 0)
                return true;
            selfContainedIdentifier = functionList[19];
            if (!IsStateActive)
            {
                InitializeState(CollectionToStringState(builder, collection, includeAmount), selfContainedIdentifier);
                if (PauseTickRun) return false;
            }
            return RunStateManager;
        }

        IEnumerator<FunctionState> CollectionToStringState(StringBuilder builder, ItemCollection collection, bool includeAmount)
        {
            SortedList<string, SortedList<string, List<string>>> likeValues = new SortedList<string, SortedList<string, List<string>>>();
            int items = 0;
            string amountString;
            ItemDefinition item;
            for (int i = 0; i < collection.ItemTypeCount; i++)
            {
                if (PauseTickRun) yield return stateActive;
                item = collection.ItemByIndex(i);
                amountString = collection.CountByIndex(i);
                item.category = Formatted(GetItemCategory(item.FullID));
                if (!likeValues.ContainsKey(item.category))
                    likeValues[item.category] = new SortedList<string, List<string>>();
                if (!likeValues[item.category].ContainsKey(amountString))
                    likeValues[item.category][amountString] = NewStringList;
                likeValues[item.category][amountString].Add($"{ItemName(item.typeID, item.subtypeID)}");
            }
            foreach (KeyValuePair<string, SortedList<string, List<string>>> kvpA in likeValues)
                foreach (KeyValuePair<string, List<string>> kvp in kvpA.Value)
                {
                    if (PauseTickRun) yield return stateActive;
                    if (items > 0)
                        builder.Append("|");
                    items++;
                    if (includeAmount)
                        builder.Append(kvp.Key);
                    builder.Append($":{kvpA.Key}");
                    foreach (string itemID in kvp.Value)
                    {
                        if (PauseTickRun) yield return stateActive;
                        builder.Append($":'{itemID}'");
                    }
                }

            yield return stateComplete;
        }

        void FillDict()
        {
            if (!UseVanillaLibrary || booted) return;
            AddItemDef("Bulletproof Glass", "BulletproofGlass", componentType, "BulletproofGlass");
            AddItemDef(canvasType, canvasType, componentType, PositionPrefix("0030", canvasType));
            AddItemDef("Computer", "Computer", componentType, "ComputerComponent");
            AddItemDef("Construction Comp", "Construction", componentType, "ConstructionComponent");
            AddItemDef("Detector Component", "Detector", componentType, "DetectorComponent");
            AddItemDef("Display", "Display", componentType, "Display");
            AddItemDef("Explosives", "Explosives", componentType, "ExplosivesComponent");
            AddItemDef("Girder", "Girder", componentType, "GirderComponent");
            AddItemDef("Gravity Gen. Comp", "GravityGenerator", componentType, "GravityGeneratorComponent");
            AddItemDef("Interior Plate", "InteriorPlate", componentType, "InteriorPlate");
            AddItemDef("Large Steel Tube", "LargeTube", componentType, "LargeTube");
            AddItemDef("Medical Component", "Medical", componentType, "MedicalComponent");
            AddItemDef("Metal Grid", "MetalGrid", componentType, "MetalGrid");
            AddItemDef("Motor", "Motor", componentType, "MotorComponent");
            AddItemDef("Power Cell", "PowerCell", componentType, "PowerCell");
            AddItemDef("Radio Comm. Comp", "RadioCommunication", componentType, "RadioCommunicationComponent");
            AddItemDef("Reactor Component", "Reactor", componentType, "ReactorComponent");
            AddItemDef("Small Steel Tube", "SmallTube", componentType, "SmallTube");
            AddItemDef("Solar Cell", "SolarCell", componentType, "SolarCell");
            AddItemDef("Steel Plate", "SteelPlate", componentType, "SteelPlate");
            AddItemDef("Superconductor", "Superconductor", componentType, "Superconductor");
            AddItemDef("Thruster Component", "Thrust", componentType, "ThrustComponent");
            AddItemDef("Zone Chip", "ZoneChip", componentType, nothingType, false);
            AddItemDef("MR-20", "AutomaticRifleItem", toolType, PositionPrefix("0040", "AutomaticRifle"));
            AddItemDef("MR-8P", "PreciseAutomaticRifleItem", toolType, PositionPrefix("0060", "PreciseAutomaticRifle"));
            AddItemDef("MR-50A", "RapidFireAutomaticRifleItem", toolType, PositionPrefix("0050", "RapidFireAutomaticRifle"));
            AddItemDef("MR-30E", "UltimateAutomaticRifleItem", toolType, PositionPrefix("0070", "UltimateAutomaticRifle"));
            AddItemDef("Welder 1", "WelderItem", toolType, PositionPrefix("0090", "Welder"));
            AddItemDef("Welder 2", "Welder2Item", toolType, PositionPrefix("0100", "Welder2"));
            AddItemDef("Welder 3", "Welder3Item", toolType, PositionPrefix("0110", "Welder3"));
            AddItemDef("Welder 4", "Welder4Item", toolType, PositionPrefix("0120", "Welder4"));
            AddItemDef("Grinder 1", "AngleGrinderItem", toolType, PositionPrefix("0010", "AngleGrinder"));
            AddItemDef("Grinder 2", "AngleGrinder2Item", toolType, PositionPrefix("0020", "AngleGrinder2"));
            AddItemDef("Grinder 3", "AngleGrinder3Item", toolType, PositionPrefix("0030", "AngleGrinder3"));
            AddItemDef("Grinder 4", "AngleGrinder4Item", toolType, PositionPrefix("0040", "AngleGrinder4"));
            AddItemDef("Drill 1", "HandDrillItem", toolType, PositionPrefix("0050", "HandDrill"));
            AddItemDef("Drill 2", "HandDrill2Item", toolType, PositionPrefix("0060", "HandDrill2"));
            AddItemDef("Drill 3", "HandDrill3Item", toolType, PositionPrefix("0070", "HandDrill3"));
            AddItemDef("Drill 4", "HandDrill4Item", toolType, PositionPrefix("0080", "HandDrill4"));
            AddItemDef("Datapad", "Datapad", dataPadType, "Datapad", false);
            AddItemDef("Powerkit", "Powerkit", consumableType, nothingType, false);
            AddItemDef("Medkit", "Medkit", consumableType, nothingType, false);
            AddItemDef("Clang Cola", "ClangCola", consumableType, nothingType, false);
            AddItemDef("Cosmic Coffee", "CosmicCoffee", consumableType, nothingType, false);
            AddItemDef("SpaceCredit", "SpaceCredit", physicalObjectType, nothingType, false);
            AddItemDef("Oxygen Bottle", "OxygenBottle", oxyBottleType, PositionPrefix("0010", "OxygenBottle"));
            AddItemDef("Hydrogen Bottle", "HydrogenBottle", hydBottleType, PositionPrefix("0020", "HydrogenBottle"));
            AddItemDef("NATO 25x184mm", "NATO_25x184mm", ammoType, PositionPrefix("0080", "NATO_25x184mmMagazine"));
            AddItemDef("Missile 200mm", "Missile200mm", ammoType, PositionPrefix("0100", "Missile200mm"));
            AddItemDef("Cobalt Ore", "Cobalt", oreType);
            AddItemDef("Gold Ore", "Gold", oreType);
            AddItemDef("Ice", "Ice", oreType);
            AddItemDef("Iron Ore", "Iron", oreType);
            AddItemDef("Magnesium Ore", "Magnesium", oreType);
            AddItemDef("Nickel Ore", "Nickel", oreType);
            AddItemDef("Platinum Ore", "Platinum", oreType);
            AddItemDef("Scrap Ore", "Scrap", oreType, "", false);
            AddItemDef("Silicon Ore", "Silicon", oreType);
            AddItemDef("Silver Ore", "Silver", oreType);
            AddItemDef(stoneType, stoneType, oreType);
            AddItemDef("Uranium Ore", "Uranium", oreType);
            AddItemDef("Cobalt Ingot", "Cobalt", ingotType);
            AddItemDef("Gold Ingot", "Gold", ingotType);
            AddItemDef("Gravel", stoneType, ingotType);
            AddItemDef("Iron Ingot", "Iron", ingotType, "", true, new List<string>() { "Scrap", stoneType });
            AddItemDef("Magnesium Powder", "Magnesium", ingotType);
            AddItemDef("Nickel Ingot", "Nickel", ingotType, "", true, new List<string>() { stoneType });
            AddItemDef("Platinum Ingot", "Platinum", ingotType, "");
            AddItemDef("Silicon Wafer", "Silicon", ingotType, "", true, new List<string>() { stoneType });
            AddItemDef("Silver Ingot", "Silver", ingotType);
            AddItemDef("Uranium Ingot", "Uranium", ingotType);
            AddItemDef("MR-20 Magazine", "AutomaticRifleGun_Mag_20rd", ammoType, PositionPrefix("0040", "AutomaticRifleGun_Mag_20rd"));
            AddItemDef("S-10E Magazine", "ElitePistolMagazine", ammoType, PositionPrefix("0030", "ElitePistolMagazine"));
            AddItemDef("S-20A Magazine", "FullAutoPistolMagazine", ammoType, PositionPrefix("0020", "FullAutoPistolMagazine"));
            AddItemDef("MR-8P Magazine", "PreciseAutomaticRifleGun_Mag_5rd", ammoType, PositionPrefix("0060", "PreciseAutomaticRifleGun_Mag_5rd"));
            AddItemDef("MR-50A Magazine", "RapidFireAutomaticRifleGun_Mag_50rd", ammoType, PositionPrefix("0050", "RapidFireAutomaticRifleGun_Mag_50rd"));
            AddItemDef("S-10 Magazine", "SemiAutoPistolMagazine", ammoType, PositionPrefix("0010", "SemiAutoPistolMagazine"));
            AddItemDef("MR-30E Magazine", "UltimateAutomaticRifleGun_Mag_30rd", ammoType, PositionPrefix("0070", "UltimateAutomaticRifleGun_Mag_30rd"));
            AddItemDef("Artillery Shell", "LargeCalibreAmmo", ammoType, PositionPrefix("0120", "LargeCalibreAmmo"));
            AddItemDef("Assault Cannon Shell", "MediumCalibreAmmo", ammoType, PositionPrefix("0110", "MediumCalibreAmmo"));
            AddItemDef("Autocannon Mag", "AutocannonClip", ammoType, PositionPrefix("0090", "AutocannonClip"));
            AddItemDef("Large Railgun Sabot", "LargeRailgunAmmo", ammoType, PositionPrefix("0140", "LargeRailgunAmmo"));
            AddItemDef("Small Railgun Sabot", "SmallRailgunAmmo", ammoType, PositionPrefix("0130", "SmallRailgunAmmo"));
            AddItemDef("PRO-1", "AdvancedHandHeldLauncherItem", toolType, PositionPrefix("0090", "AdvancedHandHeldLauncher"));
            AddItemDef("RO-1", "BasicHandHeldLauncherItem", toolType, PositionPrefix("0080", "BasicHandHeldLauncher"));
            AddItemDef("S-10E", "ElitePistolItem", toolType, PositionPrefix("0030", "EliteAutoPistol"));
            AddItemDef("S-20A", "FullAutoPistolItem", toolType, PositionPrefix("0020", "FullAutoPistol"));
            AddItemDef("S-10", "SemiAutoPistolItem", toolType, PositionPrefix("0010", "SemiAutoPistol"));
            booted = true;
        }

        bool GetTags(ItemCollection quota, string text, bool acceptZero = true)
        {
            selfContainedIdentifier = functionList[22];
            if (!TextHasLength(text))
                return true;

            if (!IsStateActive)
            {
                InitializeState(GetTagState(quota, text, acceptZero), selfContainedIdentifier);
                if (PauseTickRun) return false;
            }
            return RunStateManager;
        }

        IEnumerator<FunctionState> GetTagState(ItemCollection quota, string givenText, bool acceptZero)
        {
            Dictionary<char, char> limiters = new Dictionary<char, char> {
        { '{', '}' }, { '[', ']' }, { '<', '>' }
    };
            string typeID, amountString = "0", text = givenText;
            char keyChar = givenText[0];
            if (limiters.ContainsKey(keyChar) && givenText[givenText.Length - 1] == limiters[keyChar])
                text = givenText.Substring(1, givenText.Length - 2);

            string[] subGroups, groupTags;

            double parsedAmount;
            bool haveType;

            subGroups = text.ToLower().Split('|');
            typeID = "*";
            for (int y = 0; y < subGroups.Length; y++)
            {
                if (PauseTickRun) yield return stateActive;

                groupTags = subGroups[y].Split(':');
                tagTempStringList.Clear();
                haveType = false;
                for (int z = 0; z < groupTags.Length; z++)
                {
                    if (PauseTickRun)
                        yield return stateActive;

                    if (z == 0 && (EndsString(groupTags[z], "%") || double.TryParse(groupTags[z], out parsedAmount)))
                    {
                        amountString = groupTags[z];
                        continue;
                    }
                    if (z < 2 && !haveType && IsCategory(groupTags[z]))
                    {
                        typeID = groupTags[z];
                        haveType = true;
                    }
                    else
                        tagTempStringList.Add(groupTags[z]);
                }
                if (haveType && groupTags.Length == 1 && IsWildCard(typeID))
                    tagTempStringList.Add("*");

                for (int z = 0; z < tagTempStringList.Count; z++)
                    while (!MatchItems(quota, typeID, tagTempStringList[z], false, amountString, acceptZero))
                        yield return stateActive;
            }
            yield return stateComplete;
        }

        bool ProcessBlockOptions(BlockDefinition managedBlock)
        {
            selfContainedIdentifier = functionList[4];
            if (!IsStateRunning)
                tempBlockOptionDefinition = managedBlock;
            if (!IsStateActive)
            {
                InitializeState(ProcessBlockOptionState(), selfContainedIdentifier);
                if (PauseTickRun) return false;
            }
            return RunStateManager;
        }

        IEnumerator<FunctionState> ProcessBlockOptionState()
        {
            string dataSource, key, data;
            IMyTerminalBlock block;
            string[] dataLines;
            List<IMyTerminalBlock> groupBlocks = new List<IMyTerminalBlock>();
            while (true)
            {
                dataSource = tempBlockOptionDefinition.DataSource;
                block = tempBlockOptionDefinition.block;
                if (TextHasLength(dataSource) && TextHasLength(optionBlockFilter) && tempBlockOptionDefinition.headerIndex == 0)
                    OptionHeaderIndex(out tempBlockOptionDefinition.headerIndex, SplitLines(dataSource), optionBlockFilter);
                if (!TextHasLength(dataSource) || (TextHasLength(optionBlockFilter) && tempBlockOptionDefinition.headerIndex == 0))
                {
                    if (!(tempBlockOptionDefinition.block is IMyTextPanel) && GetKeyBool(setKeyAutoTagBlocks))
                        while (!GenerateBlockOptions(tempBlockOptionDefinition)) yield return stateActive;
                }
                else if (!StringsMatch(dataSource, tempBlockOptionDefinition.settingBackup))
                {
                    tempBlockOptionDefinition.Settings.Initialize(crossGridKeyword, includeGridKeyword, exclusionKeyword, excludeGridKeyword);
                    tempBlockOptionDefinition.cloneGroup = "";
                    dataLines = SplitLines(dataSource);
                    processBlockStorageList.Clear();
                    processBlockOptionList.Clear();
                    bool dataBool, storageSet = false, hasInventory = tempBlockOptionDefinition.HasInventory;
                    double dataDouble;

                    OptionHeaderIndex(out tempBlockOptionDefinition.headerIndex, dataLines, optionBlockFilter);

                    for (int i = tempBlockOptionDefinition.headerIndex; i < dataLines.Length; i++)
                    {
                        if (PauseTickRun) yield return stateActive;
                        if (tempBlockOptionDefinition.headerIndex > 0 && StringsMatch(dataLines[i], optionBlockFilter)) break;
                        if (!dataLines[i].StartsWith("//") && SplitData(dataLines[i], out key, out data))
                        {
                            data = data.Trim();
                            dataBool = StringsMatch(data, trueString);
                            double.TryParse(data, out dataDouble);
                            switch (key.ToLower())
                            {
                                case "automatic":
                                    tempBlockOptionDefinition.Settings.manual = !dataBool;
                                    break;
                                case "options":
                                    processBlockOptionList = RemoveSpaces(data, true).Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                                    break;
                                case "storage":
                                    if (hasInventory)
                                        processBlockStorageList = data.ToLower().Split('|').ToList();
                                    break;
                                case "loadout":
                                    if (hasInventory)
                                        while (!GetTags(tempBlockOptionDefinition.Settings.loadout, RemoveSpaces(data, true), false)) yield return stateActive;
                                    break;
                                case "limit":
                                    if (hasInventory)
                                        while (!GetTags(tempBlockOptionDefinition.Settings.limits, RemoveSpaces(data, true))) yield return stateActive;
                                    break;
                                case "logicand":
                                    while (!ProcessTimer(tempBlockOptionDefinition.Settings.logicComparisons, data)) yield return stateActive;
                                    tempBlockOptionDefinition.Settings.andComparison = tempBlockOptionDefinition.Settings.logicComparisons.Count > 0;
                                    break;
                                case "logicor":
                                    while (!ProcessTimer(tempBlockOptionDefinition.Settings.logicComparisons, data)) yield return stateActive;
                                    break;
                                case "priority":
                                    tempBlockOptionDefinition.Settings.priority = dataDouble;
                                    break;
                                case "clone group":
                                    tempBlockOptionDefinition.cloneGroup = data;
                                    break;
                            }
                        }
                    }
                    foreach (string option in processBlockOptionList)
                    {
                        if (PauseTickRun) yield return stateActive;
                        tempBlockOptionDefinition.Settings.ParseOption(option);
                    }
                    if (processBlockStorageList.Count > 0)
                    {
                        foreach (string category in processBlockStorageList)
                        {
                            if (PauseTickRun) yield return stateActive;
                            if (IsCategory(category))
                                tempBlockOptionDefinition.Settings.storageCategories.Add(category);
                        }
                        tempBlockOptionDefinition.Settings.Storage = tempBlockOptionDefinition.Settings.storageCategories.Count > 0;
                        storageSet = true;
                    }

                    if (tempBlockOptionDefinition.Settings.Storage)
                    {
                        if (tempBlockOptionDefinition.Settings.storageCategories.Count == 0)
                        {
                            tempBlockOptionDefinition.Settings.storageCategories.Add("all");
                            tempBlockOptionDefinition.Settings.storageCategories.AddRange(itemCategoryList);
                        }
                    }
                    else if (processBlockOptionList.Count == 0 && !storageSet && !tempBlockOptionDefinition.Settings.manual && tempBlockOptionDefinition.block is IMyCargoContainer && !IsGun(tempBlockOptionDefinition))
                    {
                        tempBlockOptionDefinition.Settings.Storage = true;
                        tempBlockOptionDefinition.Settings.storageCategories.Add("all");
                        tempBlockOptionDefinition.Settings.storageCategories.AddRange(itemCategoryList);
                    }

                    tempBlockOptionDefinition.settingBackup = dataSource;
                }
                if (TextHasLength(tempBlockOptionDefinition.cloneGroup))
                {
                    IMyBlockGroup blockGroup = gtSystem.GetBlockGroupWithName(tempBlockOptionDefinition.cloneGroup);
                    if (blockGroup != null)
                    {
                        groupBlocks.Clear();
                        blockGroup.GetBlocks(groupBlocks);

                        foreach (IMyTerminalBlock cBlock in groupBlocks)
                        {
                            if (PauseTickRun) yield return stateActive;
                            if (tempBlockOptionDefinition.block != cBlock && managedBlocks.ContainsKey(cBlock.EntityId))
                            {
                                clonedEntityIDs.Add(cBlock.EntityId);
                                managedBlocks[cBlock.EntityId].SetClone(tempBlockOptionDefinition);
                                if (!TextHasLength(cBlock.CustomData))
                                    cBlock.CustomData = $"Cloning: {tempBlockOptionDefinition.block.CustomName}";
                            }
                        }
                    }
                }

                yield return stateContinue;
            }
        }

        bool ProcessPanelOptions(BlockDefinition managedBlock, int surfaceIndex = 0)
        {
            selfContainedIdentifier = functionList[34];
            if (!IsStateRunning)
            {
                tempBlockOptionDefinition = managedBlock;
                tempProcessPanelOptionSurfaceIndex = surfaceIndex;
            }
            if (!IsStateActive)
            {
                InitializeState(ProcessPanelOptionState(), selfContainedIdentifier);
                if (PauseTickRun) return false;
            }
            return RunStateManager;
        }

        IEnumerator<FunctionState> ProcessPanelOptionState()
        {
            PanelMasterClass.PanelDefinition panelDefinition;
            string dataSource, key, data, blockDefinition;
            StringBuilder keyBuilder = NewBuilder;
            string[] dataLines, dataOptions;
            while (true)
            {
                if (!tempBlockOptionDefinition.panelDefinitionList.ContainsKey(tempProcessPanelOptionSurfaceIndex))
                    SetPanelDefinition(tempBlockOptionDefinition, tempProcessPanelOptionSurfaceIndex);
                panelDefinition = tempBlockOptionDefinition.panelDefinitionList[tempProcessPanelOptionSurfaceIndex];
                dataSource = panelDefinition.DataSource;
                blockDefinition = BlockSubtype(tempBlockOptionDefinition.block);

                if (!TextHasLength(dataSource))
                {
                    if (TextHasLength(panelDefinition.settingBackup))
                        SetPanelDefinition(tempBlockOptionDefinition, tempProcessPanelOptionSurfaceIndex);
                    if (GetKeyBool(setKeyAutoTagBlocks))
                    {
                        panelDefinition.DataSource = presetPanelOptions;
                        tempBlockOptionDefinition.block.CustomName = tempBlockOptionDefinition.block.CustomName.Replace(panelTag, panelTag.ToUpper());
                    }
                }
                else if (!StringsMatch(dataSource, panelDefinition.settingBackup))
                {
                    panelDefinition.items.Clear();
                    keyBuilder.Clear();
                    dataLines = SplitLines(dataSource);
                    bool dataBool, rowSet = false;
                    double dataDouble;
                    int startIndex;
                    OptionHeaderIndex(out startIndex, dataLines, optionBlockFilter);
                    for (int i = startIndex; i < dataLines.Length; i++)
                    {
                        if (PauseTickRun) yield return stateActive;
                        if (startIndex > 0 && StringsMatch(dataLines[i], optionBlockFilter)) break;
                        if (!dataLines[i].StartsWith("//") && SplitData(dataLines[i], out key, out data))
                        {
                            dataBool = StringsMatch(data, trueString);
                            double.TryParse(data, out dataDouble);
                            switch (key.ToLower())
                            {
                                case "type":
                                    switch (data.ToLower())
                                    {
                                        case "item":
                                            panelDefinition.panelType = PanelType.Item;
                                            break;
                                        case "cargo":
                                            panelDefinition.panelType = PanelType.Cargo;
                                            break;
                                        case "output":
                                            panelDefinition.panelType = PanelType.Output;
                                            break;
                                        case "status":
                                            panelDefinition.panelType = PanelType.Status;
                                            break;
                                        case "span":
                                            panelDefinition.panelType = PanelType.Span;
                                            break;
                                    }
                                    keyBuilder.Append($"{panelDefinition.panelType}");
                                    break;
                                case "categories":
                                    dataOptions = data.ToLower().Split('|');
                                    panelDefinition.itemCategories.Clear();
                                    for (int x = 0; x < dataOptions.Length; x++)
                                    {
                                        if (PauseTickRun) yield return stateActive;
                                        if (IsCategory(dataOptions[x]))
                                        {
                                            panelDefinition.itemCategories.Add(dataOptions[x]);
                                            keyBuilder.Append(dataOptions[x]);
                                        }
                                    }
                                    break;
                                case "items":
                                    while (!GetTags(panelDefinition.items, data.ToLower()))
                                        yield return stateActive;
                                    if (panelDefinition.items.ItemTypeCount > 0)
                                        while (!CollectionToString(keyBuilder, panelDefinition.items, false)) yield return stateActive;
                                    break;
                                case "sorting":
                                    switch (data.ToLower())
                                    {
                                        case "alphabetical":
                                            panelDefinition.panelItemSorting = PanelItemSorting.Alphabetical;
                                            break;
                                        case "ascendingamount":
                                            panelDefinition.panelItemSorting = PanelItemSorting.AscendingAmount;
                                            break;
                                        case "descendingamount":
                                            panelDefinition.panelItemSorting = PanelItemSorting.DescendingAmount;
                                            break;
                                        case "ascendingpercent":
                                            panelDefinition.panelItemSorting = PanelItemSorting.AscendingPercent;
                                            break;
                                        case "descendingpercent":
                                            panelDefinition.panelItemSorting = PanelItemSorting.DescendingPercent;
                                            break;
                                    }
                                    keyBuilder.Append(panelDefinition.panelItemSorting.ToString());
                                    break;
                                case "text color":
                                    GetColor(out panelDefinition.textColor, data);
                                    keyBuilder.Append(panelDefinition.textColor.ToVector4());
                                    break;
                                case "number color":
                                    GetColor(out panelDefinition.numberColor, data);
                                    keyBuilder.Append(panelDefinition.numberColor);
                                    break;
                                case "back color":
                                    GetColor(out panelDefinition.backdropColor, data);
                                    keyBuilder.Append(panelDefinition.backdropColor);
                                    break;
                                case "rows":
                                    panelDefinition.rows = (int)dataDouble;
                                    rowSet = true;
                                    break;
                                case "name length":
                                    panelDefinition.nameLength = (int)dataDouble;
                                    keyBuilder.Append(panelDefinition.nameLength);
                                    break;
                                case "decimals":
                                    panelDefinition.decimals = (int)dataDouble;
                                    keyBuilder.Append(panelDefinition.decimals);
                                    break;
                                case "update delay":
                                    panelDefinition.updateDelay = dataDouble;
                                    break;
                                case "span id":
                                    panelDefinition.spanKey = data;
                                    break;
                                case "span child id":
                                    panelDefinition.childSpanKey = data;
                                    panelDefinition.span = TextHasLength(data);
                                    break;
                                case "number suffixes":
                                    dataOptions = data.Split('|');
                                    panelDefinition.suffixes.Clear();
                                    panelDefinition.suffixes.AddRange(dataOptions);
                                    break;
                                case "options":
                                    dataOptions = data.ToLower().Split('|');
                                    panelDefinition.belowQuota = dataOptions.Contains("belowquota");
                                    panelDefinition.showProgressBar = !dataOptions.Contains("hideprogressbar");
                                    break;
                                case "minimum value":
                                    panelDefinition.minimumItemAmount = dataDouble;
                                    keyBuilder.Append(panelDefinition.minimumItemAmount);
                                    break;
                                case "maximum value":
                                    panelDefinition.maximumItemAmount = dataDouble;
                                    keyBuilder.Append(panelDefinition.maximumItemAmount);
                                    break;
                                case "font":
                                    panelDefinition.font = GetFont(((IMyTextSurfaceProvider)tempBlockOptionDefinition.block).GetSurface(0), data);
                                    break;
                                case "item display":
                                    switch (data.ToLower())
                                    {
                                        case "detailed":
                                            panelDefinition.displayType = DisplayType.Detailed;
                                            break;
                                        case "compactamount":
                                            panelDefinition.displayType = DisplayType.CompactAmount;
                                            break;
                                        case "standard":
                                            panelDefinition.displayType = DisplayType.Standard;
                                            break;
                                        case "compactpercent":
                                            panelDefinition.displayType = DisplayType.CompactPercent;
                                            break;
                                    }
                                    break;
                            }
                        }
                    }
                    if (panelDefinition.panelType != PanelType.None)
                    {
                        IMyTextSurface surface = panelDefinition.GetSurface();
                        panelDefinition.size = surface.SurfaceSize;
                        panelDefinition.positionOffset = surface.TextureSize - surface.SurfaceSize;
                        if (panelDefinition.provider || panelDefinition.cornerPanel || blockDefinition == "LargeTextPanel" || blockDefinition == "LargeLCDPanel5x3")
                            panelDefinition.positionOffset /= 2f;
                        panelDefinition.settingKey = keyBuilder.ToString();
                        if (!rowSet)
                            panelDefinition.rows = 15;
                        switch (panelDefinition.panelType)
                        {
                            case PanelType.Cargo:
                                panelDefinition.columns = 1;
                                if (!rowSet)
                                    panelDefinition.rows = panelDefinition.itemCategories.Count;
                                break;
                            case PanelType.Output:
                                if (!rowSet)
                                    panelDefinition.rows = 13;
                                panelDefinition.columns = 2;
                                if (rowSet && panelDefinition.rows == 0)
                                    panelDefinition.columns = 1;
                                break;
                            case PanelType.Status:
                                panelDefinition.columns = 2;
                                if (!rowSet)
                                    panelDefinition.rows = 8;
                                if (rowSet && panelDefinition.rows == 0)
                                    panelDefinition.columns = 1;
                                break;
                            case PanelType.Span:
                                panelDefinition.columns = 1;
                                break;
                        }
                    }
                    else
                        tempBlockOptionDefinition.panelDefinitionList.Remove(tempProcessPanelOptionSurfaceIndex);
                }

                panelDefinition.settingBackup = dataSource;

                yield return stateContinue;
            }
        }

        bool GenerateBlockOptions(BlockDefinition blockDefinition)
        {
            selfContainedIdentifier = functionList[28];
            if (!IsStateActive)
            {
                InitializeState(GenerateBlockOptionState(blockDefinition), selfContainedIdentifier);
                if (PauseTickRun) return false;
            }
            return RunStateManager;
        }

        IEnumerator<FunctionState> GenerateBlockOptionState(BlockDefinition blockDefinition)
        {
            IMyTerminalBlock block = blockDefinition.block;
            StringBuilder builder = NewBuilder, optionBuilder = NewBuilder;
            bool none;
            if (TextHasLength(optionBlockFilter) && TextHasLength(blockDefinition.DataSource))
            {
                BuilderAppendLine(builder, blockDefinition.DataSource);
                BuilderAppendLine(builder);
            }
            if (TextHasLength(optionBlockFilter))
                BuilderAppendLine(builder, optionBlockFilter);
            BuilderAppendLine(builder, $"Automatic={!blockDefinition.Settings.manual}");
            builder.Append("Options=");

            AppendOption(blockDefinition.Settings.toggles, builder, optionBuilder);
            if (PauseTickRun) yield return stateActive;

            if (blockDefinition.HasInventory)
            {
                AppendOption(blockDefinition.Settings.inventoryToggles, builder, optionBuilder);

                if (block.InventoryCount > 1)
                {
                    AppendOption(blockDefinition.Settings.multiInventoryToggles, builder, optionBuilder);

                    if (block is IMyAssembler)
                        AppendOption(blockDefinition.Settings.assemblerToggles, builder, optionBuilder);
                }
            }
            BuilderAppendLine(builder);
            if (BuilderHasLength(optionBuilder))
                BuilderAppendLine(builder, optionBuilder.ToString());
            if (PauseTickRun) yield return stateActive;
            optionBuilder.Clear();
            AppendOption(builder, $"Priority={blockDefinition.Settings.priority}", blockDefinition.Settings.priority == 1.0);
            AppendOption(builder, $"Clone Group={blockDefinition.cloneGroup}", !TextHasLength(blockDefinition.cloneGroup));
            if (blockDefinition.HasInventory)
            {
                none = !blockDefinition.Settings.Storage;
                AppendOption(builder, optionBuilder, "Storage=", none);

                AppendOption(builder, optionBuilder, "All", none || !blockDefinition.Settings.storageCategories.Contains("all"));
                itemCategoryList.ForEach(category => AppendOption(builder, optionBuilder, category, none || !blockDefinition.Settings.storageCategories.Contains(category)));
                if (!none)
                    BuilderAppendLine(builder);
                if (BuilderHasLength(optionBuilder))
                    BuilderAppendLine(builder, optionBuilder.ToString());
                optionBuilder.Clear();
                if (PauseTickRun) yield return stateActive;
                if (blockDefinition.Settings.loadout.ItemTypeCount > 0)
                {
                    builder.Append("Loadout=");
                    while (!CollectionToString(builder, blockDefinition.Settings.loadout, true)) yield return stateActive;
                    BuilderAppendLine(builder);
                }
                else
                    AppendOption(builder, "Loadout=20%:ingot:iron:silicon:nickel|50:ore:ice");
                if (blockDefinition.Settings.limits.ItemTypeCount > 0)
                {
                    builder.Append("Limit=");
                    while (!CollectionToString(builder, blockDefinition.Settings.limits, true)) yield return stateActive;
                    BuilderAppendLine(builder);
                }
                else
                    AppendOption(builder, "Limit=25:ingot:*|10%:ore:*");
            }
            if (block is IMyFunctionalBlock)
            {
                if (blockDefinition.Settings.logicComparisons.Count > 0)
                {
                    builder.Append(blockDefinition.Settings.andComparison ? "LogicAnd=" : "LogicOr=");

                    foreach (LogicComparison logicComparison in blockDefinition.Settings.logicComparisons)
                    {
                        if (PauseTickRun) yield return stateActive;
                        if (builder[builder.Length - 1] != '=')
                            builder.Append("|");
                        builder.Append(logicComparison.GetString);
                    }
                    BuilderAppendLine(builder);
                }
                else
                {
                    AppendOption(builder, "LogicAnd=ingot:iron<100 | ore:iron>=quota*0.1");
                    AppendOption(builder, "LogicOr=ingot:iron<quota*0.95 | ingot:silicon<quota*0.95");
                }
            }
            AppendOption(builder, $"Definition={block.BlockDefinition}");
            if (TextHasLength(optionBlockFilter))
                BuilderAppendLine(builder, optionBlockFilter);

            blockDefinition.DataSource = builder.ToString().TrimEnd();
            blockDefinition.settingBackup = blockDefinition.DataSource;

            yield return stateComplete;
        }


        #endregion


        #region Return Methods

        string OptionLead(StringBuilder builder)
        {
            char last = BuilderHasLength(builder) ? builder[builder.Length - 1] : '\0';
            return !BuilderHasLength(builder) ? "//" : last != '=' && last != '\r' && last != '\n' ? "|" : "";
        }

        static string[] SplitLines(string data)
        {
            return data.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        }

        static void OptionHeaderIndex(out int startIndex, string[] lines, string key)
        {
            startIndex = 0;
            if (!TextHasLength(key)) return;

            for (int i = 0; i < lines.Length; i++)
                if (StringsMatch(lines[i], key))
                {
                    startIndex = i + 1;
                    return;
                }
            return;
        }

        TimeSpan SpanDelay(double seconds = 0)
        {
            return scriptSpan + TimeSpan.FromSeconds(seconds);
        }

        bool SpanElapsed(TimeSpan span)
        {
            return scriptSpan >= span;
        }

        double RemainingSpan(TimeSpan span)
        {
            return Math.Max(0, (span - scriptSpan).TotalSeconds);
        }

        bool IsGravelSifter(IMyTerminalBlock block)
        {
            return settingsListsStrings[setKeyGravelSifterKeys].Contains(BlockSubtype(block).ToLower());
        }

        static string PositionPrefix(string prefix, string input)
        {
            return $"Position{prefix}_{input}";
        }

        bool IsPanelProvider(IMyTerminalBlock block)
        {
            return block is IMyTextSurfaceProvider && (block is IMyTextPanel || block is IMyCockpit || block is IMyButtonPanel);
        }

        bool ScanFilter(IMyTerminalBlock block, HashSet<long> excludedIds)
        {
            return (block.IsFunctional && GlobalFilter(block) && PassPanel(block) &&
                (block.InventoryCount > 0 || IsPanelProvider(block) || block is IMyProgrammableBlock || block is IMyTimerBlock || TextHasLength(block.CustomData)) &&
                !excludedIds.Contains(block.EntityId));
        }

        string GetItemCategory(string itemID)
        {
            if (itemCategoryDictionary.ContainsKey(itemID))
                return itemCategoryDictionary[itemID];

            string typeID, subtypeID;
            SplitID(itemID, out typeID, out subtypeID);

            return
                IsAmmo(typeID) ? ammoKeyword :
                IsComponent(typeID) ? componentKeyword :
                IsIngot(typeID) ? ingotKeyword :
                IsOre(typeID) ? oreKeyword :
                IsTool(typeID) ? toolKeyword : typeID;
        }

        bool IsBlueprint(string data)
        {
            return TextHasLength(data) && !StringsMatch(data, "none");
        }

        bool HasBlueprintMatch(MyInventoryItem item, ref string matchingKey)
        {
            if (mergeLengthTolerance < 0)
                return false;

            string itemSubtype = AutoMatchNormalize(item.Type.SubtypeId), blueprintSubtype;
            for (int i = 0; i < modBlueprintList.Count; i++)
            {
                blueprintSubtype = AutoMatchNormalize(modBlueprintList[i]);
                if (Math.Abs(blueprintSubtype.Length - itemSubtype.Length) > mergeLengthTolerance)
                    continue;

                if (LeadsString(itemSubtype, blueprintSubtype) || EndsString(itemSubtype, blueprintSubtype) || LeadsString(blueprintSubtype, itemSubtype) || EndsString(blueprintSubtype, itemSubtype))
                {
                    matchingKey = modBlueprintList[i];
                    return true;
                }
            }
            return false;
        }

        bool HasItemMatch(MyProductionItem blueprint, ref string matchingKey)
        {
            if (mergeLengthTolerance < 0)
                return false;

            string itemSubtype, blueprintSubtype = AutoMatchNormalize(BlueprintSubtype(blueprint)), subtypeID, typeID;
            for (int i = 0; i < modItemDictionary.Count; i++)
            {
                SplitID(modItemDictionary.Keys[i], out typeID, out subtypeID);
                itemSubtype = AutoMatchNormalize(subtypeID);
                if (Math.Abs(blueprintSubtype.Length - itemSubtype.Length) > mergeLengthTolerance)
                    continue;

                if (LeadsString(itemSubtype, blueprintSubtype) || EndsString(itemSubtype, blueprintSubtype) || LeadsString(blueprintSubtype, itemSubtype) || EndsString(blueprintSubtype, itemSubtype))
                {
                    matchingKey = modItemDictionary.Keys[i];
                    return true;
                }
            }
            return false;
        }

        string AutoMatchNormalize(string source)
        {
            return source.ToLower().Replace("component", "").Replace("magazine", "").Replace("blueprint", "").Replace("tier", "t").Replace("hydrogen", "hydro").Replace("thruster", "thrust");
        }

        static string Formatted(string text)
        {
            if (text.Length > 1)
                return $"{text.Substring(0, 1).ToUpper()}{text.Substring(1).ToLower()}";

            return text.ToUpper();
        }

        bool ContainsString(string whole, string key)
        {
            if (whole.Length >= key.Length && TextHasLength(key))
                return whole.ToLower().Contains(key.ToLower());

            return false;
        }

        static string ShortMSTime(double milliseconds)
        {
            return $"{(milliseconds >= 1000.0 ? $"{ShortNumber2(milliseconds / 1000.0)}s" : $"{ShortNumber2(milliseconds)}ms")}";
        }

        string BlockSubtype(IMyTerminalBlock block)
        {
            return block.BlockDefinition.SubtypeName;
        }

        static string BlueprintSubtype(MyProductionItem blueprint)
        {
            return blueprint.BlueprintId.SubtypeName;
        }

        bool StateActive(string key, bool check = false)
        {
            if (check)
                return stateErrorCodes.ContainsKey(key);
            if (stateList.ContainsKey(key))
            {
                if (!stateErrorCodes.ContainsKey(key))
                    stateErrorCodes[key] = "";
                return true;
            }
            return false;
        }

        bool GlobalFilter(IMyTerminalBlock block)
        {
            return (!TextHasLength(globalFilterKeyword) || ContainsString(block.CustomName, globalFilterKeyword)) && !settingsListsStrings[setKeyExcludedDefinitions].Contains(BlockSubtype(block));
        }

        bool SplitData(string text, out string key, out string data, char splitter = '=', bool firstIndex = true)
        {
            int index = firstIndex ? text.IndexOf(splitter) : text.LastIndexOf(splitter);
            if (index != -1)
            {
                key = text.Substring(0, index);
                data = text.Substring(index + 1);
                return true;
            }
            key = data = "";
            return TextHasLength(data) && TextHasLength(key);
        }

        bool AcceptsItem(BlockDefinition managedBlock, string typeID, string subtypeID)
        {
            IMyTerminalBlock block = managedBlock.block;

            bool limited;

            double limit = managedBlock.Settings.limits.ItemCount(out limited, typeID, subtypeID, block);

            if (limited && limit <= 0)
                return false;

            if (IsAmmo(typeID) && gunAmmoDictionary.ContainsKey(BlockSubtype(block)))
                return gunAmmoDictionary[BlockSubtype(block)] == subtypeID;

            return true;
        }

        bool AcceptsItem(BlockDefinition managedBlock, MyInventoryItem item)
        {
            return AcceptsItem(managedBlock, item.Type.TypeId, item.Type.SubtypeId);
        }

        bool GetSetLimit(BlockDefinition managedBlock, ref double foundMax, MyInventoryItem item)
        {
            bool hasLimit;
            double tempMax = managedBlock.Settings.limits.ItemCount(out hasLimit, item, managedBlock.block);
            if (!hasLimit)
                tempMax = managedBlock.Settings.loadout.ItemCount(out hasLimit, item, managedBlock.block);

            if (hasLimit)
            {
                foundMax = tempMax;
                return true;
            }
            return false;
        }

        string GetKeyString(string key, bool lower = true)
        {
            foreach (KeyValuePair<string, SortedList<string, string>> kvp in settingDictionaryStrings)
                if (kvp.Value.ContainsKey(key))
                    return lower ? kvp.Value[key].ToLower() : kvp.Value[key];

            return "-0.1";
        }

        double GetKeyDouble(string key)
        {
            foreach (KeyValuePair<string, SortedList<string, double>> kvp in settingDictionaryDoubles)
                if (kvp.Value.ContainsKey(key))
                    return kvp.Value[key];

            return -0.1;
        }

        bool GetKeyBool(string key)
        {
            foreach (KeyValuePair<string, SortedList<string, bool>> kvp in settingDictionaryBools)
                if (kvp.Value.ContainsKey(key))
                    return kvp.Value[key];

            return false;
        }

        bool SetKeyString(string key, string data)
        {
            foreach (KeyValuePair<string, SortedList<string, string>> kvp in settingDictionaryStrings)
                if (kvp.Value.ContainsKey(key))
                {
                    kvp.Value[key] = data;
                    return true;
                }

            return false;
        }

        bool SetKeyDouble(string key, double data)
        {
            foreach (KeyValuePair<string, SortedList<string, double>> kvp in settingDictionaryDoubles)
                if (kvp.Value.ContainsKey(key))
                {
                    kvp.Value[key] = data;
                    return true;
                }

            return false;
        }

        bool SetKeyBool(string key, bool data)
        {
            foreach (KeyValuePair<string, SortedList<string, bool>> kvp in settingDictionaryBools)
                if (kvp.Value.ContainsKey(key))
                {
                    kvp.Value[key] = data;
                    return true;
                }

            return false;
        }

        static bool StringsMatch(string a, string b)
        {
            return a.Length == b.Length && string.Compare(a, b, true) == 0;
        }

        double LeastKeyedOrePercentage(string subtypeID)
        {
            double leastFound = 0.5, keyOffset;
            ItemDefinition definition;
            bool first = true;
            for (int x = 0; x < oreKeyedItemDictionary.Count; x++)
                if (GetDefinition(out definition, oreKeyedItemDictionary.Keys[x]))
                {
                    keyOffset = definition.oreKeys.IndexOf(subtypeID);
                    if (keyOffset != -1)
                    {
                        leastFound = first ? definition.Percentage + (keyOffset * 0.00001) : Math.Min(leastFound, definition.Percentage + (keyOffset * 0.00001));
                        first = false;
                    }
                }
            return leastFound;
        }

        double LeastKeyedOrePercentage(MyInventoryItem item)
        {
            return LeastKeyedOrePercentage(item.Type.SubtypeId);
        }

        Blueprint ItemToBlueprint(ItemDefinition definition)
        {
            return new Blueprint { blueprintID = definition.blueprintID, subtypeID = definition.subtypeID, typeID = definition.typeID, multiplier = definition.assemblyMultiplier };
        }

        bool UnknownItem(MyInventoryItem item)
        {
            ItemDefinition definition;
            return !modItemDictionary.ContainsKey(item.Type.ToString()) && !GetDefinition(out definition, item.Type.ToString());
        }

        bool UnknownBlueprint(MyProductionItem blueprint)
        {
            if (BlueprintSubtype(blueprint) == stoneOreToIngotBasicID)
                return false;

            return !modBlueprintList.Contains(BlueprintSubtype(blueprint)) && !blueprintList.ContainsKey(BlueprintSubtype(blueprint));
        }

        bool LogicPass(ItemDefinition definition, string comparison, string compare)
        {
            string compareAgainst = compare.ToLower();
            double comparisonNumber = 0, changeNumber;
            if (LeadsString(compareAgainst, "quota"))
            {
                comparisonNumber = definition.currentQuota;
                int indexP = compareAgainst.IndexOf("+"), indexM = compareAgainst.IndexOf("*");
                if (indexP > 0)
                {
                    if (!double.TryParse(compareAgainst.Substring(indexP + 1), out changeNumber))
                        comparisonNumber += changeNumber;
                }
                else if (indexM > 0 && !double.TryParse(compareAgainst.Substring(indexM + 1), out changeNumber))
                    comparisonNumber *= changeNumber;
            }
            else if (ContainsString(compare, "/"))
            {
                ItemDefinition comparisonDefinition;
                if (GetDefinition(out comparisonDefinition, compare))
                    comparisonNumber = comparisonDefinition.amount;
            }
            else if (!double.TryParse(compareAgainst, out comparisonNumber))
                return false;

            return
                comparison == ">=" ? definition.amount >= comparisonNumber :
                comparison == "<=" ? definition.amount <= comparisonNumber :
                comparison == ">" ? definition.amount > comparisonNumber :
                comparison == "<" ? definition.amount < comparisonNumber :
                comparison == "=" && definition.amount == comparisonNumber;
        }

        bool PassPanel(IMyTerminalBlock block)
        {
            return block.InventoryCount > 0 || block is IMyTimerBlock || (IsPanelProvider(block) && (block is IMyProgrammableBlock || ContainsString(block.CustomName, panelTag)));
        }

        bool IsPanel(BlockDefinition managedBlock, int surfaceIndex = 0)
        {
            return managedBlock.panelDefinitionList.ContainsKey(surfaceIndex) && managedBlock.panelDefinitionList[surfaceIndex].panelType != PanelType.None;
        }

        static string ShortenName(string name, int length = 20, bool pad = false)
        {
            string shortName = name.Length <= length ? name : $"{name.Substring(0, (int)Math.Ceiling((length - 1.0) / 2.0))}.{name.Substring(name.Length - (int)Math.Floor((length - 1.0) / 2.0))}";

            if (pad)
                shortName = shortName.PadRight(length);

            return shortName;
        }

        bool IsBlockOk(long index)
        {
            if (!managedBlocks.ContainsKey(index))
                return false;

            blockCheckDefinitionA = managedBlocks[index];
            if (blockCheckDefinitionA.block == null)
                return false;

            return blockCheckDefinitionA.block.CubeGrid.CubeExists(blockCheckDefinitionA.block.Position) && blockCheckDefinitionA.block.IsFunctional && !blockCheckDefinitionA.block.Closed;
        }

        static bool GetDefinition(out ItemDefinition definition, string itemID)
        {
            string typeID, subtypeID;
            SplitID(itemID, out typeID, out subtypeID);
            if (itemListMain.ContainsKey(typeID) && itemListMain[typeID].TryGetValue(subtypeID, out definition))
                return true;

            definition = null;
            return false;
        }

        static string TruncateNumber(double number, int decimalPlaces)
        {
            return $"{Math.Floor(number * (Math.Pow(10.0, decimalPlaces))) / (Math.Pow(10.0, decimalPlaces))}";
        }

        void ItemDefinitionToBuilders(ref StringBuilder builder, ItemDefinition definition)
        {
            builder.Append($"Name={definition.displayName}||Category={Formatted(definition.category)}||Quota={definition.quota}");
            BuilderAppendLine(builder, definition.quota >= 0 && definition.quotaMax > definition.quota ? $"<{definition.quotaMax}" : "");

            builder.Append($"^Type={definition.typeID}||Subtype={definition.subtypeID}");

            if (IsBlueprint(definition.blueprintID))
                builder.Append($"||Blueprint={definition.blueprintID}||Assembly Multiplier={definition.assemblyMultiplier}||Assemble={definition.assemble}||Disassemble={definition.disassemble}");
            else if (StringsMatch(definition.blueprintID, "none"))
                builder.Append("||Blueprint=None");

            if (IsOre(definition.typeID))
                builder.Append($"||Refine={definition.refine}");

            builder.Append($"||Fuel={definition.fuel}||Display={definition.display}");
            if (definition.gas || IsIce(definition.typeID, definition.subtypeID))
                builder.Append($"||Gas={definition.gas}");

            if (IsIngot(definition.typeID) || definition.oreKeys.Count > 0)
            {
                builder.Append("||Ore Keys=[");
                for (int i = 0; i < definition.oreKeys.Count; i++)
                    builder.Append($"{(i > 0 ? "|" : "")}{definition.oreKeys[i]}");
                builder.Append("]");
            }
            BuilderAppendLine(builder);
        }

        bool IsGas(MyInventoryItem item)
        {
            return IsGas(item.Type.TypeId, item.Type.SubtypeId);
        }

        bool IsGas(string typeID, string subtypeID)
        {
            ItemDefinition definition;
            if (GetDefinition(out definition, $"{typeID}/{subtypeID}"))
                return definition.gas;

            return IsIce(typeID, subtypeID);
        }

        bool IsIce(string typeID, string subtypeID)
        {
            return IsOre(typeID) && subtypeID == "Ice";
        }

        bool UsableAssembler(BlockDefinition block, MyDefinitionId blueprintID, MyAssemblerMode mode)
        {
            IMyAssembler assembler = (IMyAssembler)block.block;

            if (!assembler.Enabled)
                return false;

            if (!GetKeyBool(setKeySurvivalKitAssembly) && blueprintID.SubtypeName != stoneOreToIngotBasicID && ContainsString(BlockSubtype(assembler), "survival"))
                return false;

            if ((block.Settings.AssemblyOnly && mode == disassemblyMode) || (block.Settings.DisassemblyOnly && mode == assemblyMode))
                return false;

            return assembler.CanUseBlueprint(blueprintID) && (assembler.IsQueueEmpty || assembler.Mode == mode);
        }

        string MakeBlueprint(Blueprint blueprint)
        {
            return $"{blueprintPrefix}/{blueprint.blueprintID}";
        }

        double AssemblyAmount(Blueprint blueprint, bool disassembly = false)
        {
            ItemDefinition definition;
            if (GetDefinition(out definition, $"{blueprint.typeID}/{blueprint.subtypeID}"))
                return disassembly && definition.differenceNeeded < 0 ? -definition.differenceNeeded : !disassembly && definition.differenceNeeded > 0 ? definition.differenceNeeded : 0;
            return 0;
        }

        double BlueprintPercentage(MyDefinitionId blueprintID)
        {
            if (blueprintID.SubtypeName == stoneOreToIngotBasicID)
                return double.MaxValue;

            string typeID, subtypeID;
            if (GetItemFromBlueprint(blueprintID.SubtypeName, out typeID, out subtypeID))
                return ItemPercentage(typeID, subtypeID);

            return -1;
        }

        double ItemPercentage(string typeID, string subtypeID)
        {
            ItemDefinition definition;
            if (GetDefinition(out definition, $"{typeID}/{subtypeID}"))
                return definition.Percentage;

            return 100;
        }

        bool FunctionDelay(string functionKey)
        {
            if (!delaySpans.ContainsKey(functionKey))
            {
                delaySpans[functionKey] = ZeroSpan;
                return true;
            }
            return SpanElapsed(delaySpans[functionKey]);
        }

        double AvailableVolume(IMyTerminalBlock block)
        {
            return (double)block.GetInventory(0).MaxVolume - (double)block.GetInventory(0).CurrentVolume;
        }

        double CurrentVolumePercentage(long index)
        {
            return (double)managedBlocks[index].Input.CurrentVolume / (double)managedBlocks[index].Input.MaxVolume;
        }

        bool OverRange(double amount, double goal, double multiplier)
        {
            return amount > goal + (goal * multiplier);
        }

        bool UnderRange(double amount, double goal, double multiplier)
        {
            return amount < goal - (goal * multiplier);
        }

        bool Distributable(MyInventoryItem item, BlockDefinition block)
        {
            if (LoadoutHome(block, item))
                return false;

            string subtypeID = item.Type.SubtypeId;

            return (IsOre(item) && RefinedOre(item)) || IsAmmo(item) || IsFuel(item) || (typedIndexes[setKeyIndexGravelSifters].Count > 0 && IsIngot(item) && subtypeID == stoneType) ||
                   (IsComponent(item) && subtypeID == canvasType) || IsGas(item);
        }

        bool RefinedOre(MyInventoryItem item)
        {
            ItemDefinition def;
            if (GetDefinition(out def, item.Type.ToString()))
                return def.refine;

            return true;
        }

        double DefaultMax(MyInventoryItem item, BlockDefinition block)
        {
            return DefaultMax(item.Type.TypeId, item.Type.SubtypeId, block);
        }

        double DefaultMax(string typeID, string subtypeID, BlockDefinition blockDef)
        {
            IMyTerminalBlock block = blockDef.block;
            if (block is IMyGasGenerator)
            {
                if (IsGas(typeID, subtypeID))
                    return PercentageMax((float)GetKeyDouble(setKeyIcePerGenerator), typeID, subtypeID, block);
            }
            else if (block is IMyRefinery)
            {
                if (activeOres > 0)
                    return ((double)blockDef.Input.MaxVolume / activeOres) / ItemVolume(typeID, subtypeID);
            }
            else
                return
                    IsGun(blockDef) ? Math.Ceiling(PercentageMax((float)GetKeyDouble(setKeyAmmoPerGun), typeID, subtypeID, block)) :
                    block is IMyReactor ? PercentageMax((float)GetKeyDouble(setKeyFuelPerReactor), typeID, subtypeID, block) :
                    block is IMyParachute ? Math.Ceiling(PercentageMax((float)GetKeyDouble(setKeyCanvasPerParachute), typeID, subtypeID, block)) : double.MaxValue;

            return double.MaxValue;
        }

        static double PercentageMax(float value, string typeID, string subtypeID, IMyTerminalBlock block)
        {
            float calcedValue = value;
            if (value <= 1f)
            {
                calcedValue = ((float)block.GetInventory(0).MaxVolume / ItemVolume(typeID, subtypeID)) * value;
                if (!FractionalItem(typeID, subtypeID))
                    calcedValue = (float)Math.Floor(calcedValue);
            }

            return calcedValue;
        }

        static float ItemVolume(string typeID, string subtypeID)
        {
            string key = $"{typeID}/{subtypeID}";
            try
            {
                MyItemType type = MyItemType.Parse(key);
                if (type.GetItemInfo().Volume > 0f)
                    return type.GetItemInfo().Volume;
            }
            catch { }
            ItemDefinition def;
            if (GetDefinition(out def, key) && def.volume != 0)
                return def.volume;
            return 0.17f;
        }

        bool IsFuel(MyInventoryItem item)
        {
            ItemDefinition definition;
            return GetDefinition(out definition, item.Type.ToString()) && definition.fuel;
        }

        string ItemName(MyInventoryItem item)
        {
            return ItemName(item.Type.TypeId, item.Type.SubtypeId);
        }

        string ItemName(string typeID, string subtypeID)
        {
            ItemDefinition definition;
            if (GetDefinition(out definition, $"{typeID}/{subtypeID}"))
                return definition.displayName;

            return subtypeID;
        }

        double GetCurrentVolumeLimit(MyInventoryItem item, IMyTerminalBlock block)
        {
            return AvailableVolume(block) / ItemVolume(item);
        }

        static bool FractionalItem(string typeId, string subtypeID)
        {
            ItemDefinition definition;
            if (GetDefinition(out definition, $"{typeId}/{subtypeID}"))
                return definition.fractional;

            return false;
        }

        static bool FractionalItem(MyInventoryItem item)
        {
            return item.Type.GetItemInfo().UsesFractions;
        }

        bool Sortable(BlockDefinition blockDef, int inventoryIndex)
        {
            if (inventoryIndex == 0 && blockDef.Settings.KeepInput)
                return false;
            if (inventoryIndex == 1 && blockDef.Settings.KeepOutput)
                return false;
            IMyTerminalBlock block = blockDef.block;
            if (blockDef.isGravelSifter || block is IMyRefinery)
                return inventoryIndex == 1;

            if (block is IMyAssembler)
            {
                IMyAssembler assembler = (IMyAssembler)block;
                return (assembler.IsQueueEmpty || ((assembler.Mode == assemblyMode && inventoryIndex == 1) || (assembler.Mode == disassemblyMode && inventoryIndex == 0)));
            }
            if (block is IMyReactor || block is IMyParachute || IsGun(blockDef) || block is IMySafeZoneBlock)
                return false;

            return true;
        }

        bool IsGun(BlockDefinition block)
        {
            return block is IMyUserControllableGun || block.Settings.GunOverride;
        }

        bool Sortable(MyInventoryItem item, BlockDefinition blockDef, int inventoryIndex = 0)
        {
            IMyTerminalBlock block = blockDef.block;
            if (fillingBottles && IsBottle(item))
                return !(block is IMyGasGenerator || block is IMyGasTank);

            if (inventoryIndex == 0 && (LoadoutHome(blockDef, item) || IsStorage(blockDef, item) || (block is IMyGasGenerator && (IsGas(item)))))
                return false;

            if (block is IMyAssembler)
            {
                IMyAssembler assembler = (IMyAssembler)block;
                return assembler.IsQueueEmpty || (inventoryIndex == 0 && assembler.Mode == disassemblyMode) || (inventoryIndex == 1 && assembler.Mode == assemblyMode);
            }
            if (block is IMyShipWelder && ((IMyShipWelder)block).Enabled && IsComponent(item))
                return false;

            return true;
        }

        bool IsStorage(BlockDefinition block, MyInventoryItem item)
        {
            return block.Settings.storageCategories.Contains("all") || block.Settings.storageCategories.Contains("*") ||
                   block.Settings.storageCategories.Contains(GetItemCategory(item.Type.ToString()));
        }

        bool LoadoutHome(BlockDefinition block, MyInventoryItem item)
        {
            return block.Settings.loadout.ItemCount(item) > 0;
        }

        float ItemVolume(MyInventoryItem item)
        {
            return item.Type.GetItemInfo().Volume;
        }

        bool IsBottle(MyInventoryItem item)
        {
            return IsBottle(item.Type.TypeId);
        }

        bool IsBottle(string typeID)
        {
            return StringsMatch(typeID, hydBottleType) || StringsMatch(typeID, oxyBottleType);
        }

        bool IsIngot(string typeID)
        {
            return IsWildCard(typeID) || StringsMatch(typeID, ingotType) || StringsMatch(typeID, ingotKeyword) || (typeID.Length > 1 && LeadsString(ingotKeyword, typeID));
        }

        bool IsOre(string typeID)
        {
            return IsWildCard(typeID) || StringsMatch(typeID, oreType) || StringsMatch(typeID, oreKeyword) || (typeID.Length > 1 && LeadsString(oreKeyword, typeID));
        }

        bool IsAmmo(string typeID)
        {
            return IsWildCard(typeID) || StringsMatch(typeID, ammoType) || StringsMatch(typeID, ammoKeyword) || (typeID.Length > 1 && LeadsString(ammoKeyword, typeID));
        }

        bool GetItemFromBlueprint(string blueprintID, out string typeID, out string subtypeID)
        {
            Blueprint blueprint;
            typeID = subtypeID = "";
            if (blueprintList.TryGetValue(blueprintID, out blueprint))
            {
                typeID = blueprint.typeID;
                subtypeID = blueprint.subtypeID;
                return true;
            }
            return false;
        }

        bool IsComponent(string typeID)
        {
            return IsWildCard(typeID) || StringsMatch(typeID, componentType) || StringsMatch(typeID, componentKeyword) || (typeID.Length > 1 && LeadsString(componentKeyword, typeID));
        }

        bool IsTool(string typeID)
        {
            return IsWildCard(typeID) || StringsMatch(typeID, toolType) || IsBottle(typeID) || StringsMatch(typeID, dataPadType) || StringsMatch(typeID, consumableType) || StringsMatch(typeID, physicalObjectType) || StringsMatch(typeID, toolKeyword) ||
                (
                    typeID.Length > 1 &&
                    (
                        LeadsString(toolType, typeID) ||
                        LeadsString(dataPadType, typeID) ||
                        LeadsString(consumableType, typeID) ||
                        LeadsString(physicalObjectType, typeID) ||
                        LeadsString(toolKeyword, typeID)
                    )
                );
        }

        bool EndsString(string whole, string end)
        {
            return RemoveSpaces(whole, true).EndsWith(RemoveSpaces(end, true));
        }

        bool LeadsString(string whole, string lead)
        {
            return RemoveSpaces(whole, true).StartsWith(RemoveSpaces(lead, true));
        }

        bool IsIngot(MyInventoryItem item)
        {
            return item.Type.GetItemInfo().IsIngot;
        }

        bool IsOre(MyInventoryItem item)
        {
            return item.Type.GetItemInfo().IsOre;
        }

        bool IsComponent(MyInventoryItem item)
        {
            return item.Type.GetItemInfo().IsComponent;
        }

        bool IsTool(MyInventoryItem item)
        {
            return item.Type.GetItemInfo().IsTool || IsTool(item.Type.TypeId);
        }

        bool IsAmmo(MyInventoryItem item)
        {
            return item.Type.GetItemInfo().IsAmmo;
        }

        bool IsCategory(string typeID)
        {
            return IsWildCard(typeID) || itemCategoryList.Contains(typeID.ToLower());
        }

        bool UnavailableActions()
        {
            double mult = 1;
            mult = Math.Max(0.00001, Math.Min(mult, overheatAverage > 0.0 ? 1.0 - (torchAverage / (overheatAverage * 1.001)) : 1.0 - (torchAverage / (runTimeLimiter * 2.0))));

            return
            Runtime.CurrentInstructionCount >= (Runtime.MaxInstructionCount * actionLimiterMultiplier * mult) ||
            (Now - tickStartTime).TotalMilliseconds >= runTimeLimiter * mult;
        }

        bool StateManager(string identifier, bool updateStatus = true, bool reportErrors = true)
        {
            if (updateStatus)
                currentFunction = identifier;


            bool errorCaught = false, endReached;
            DateTime startTime = Now, endTime;
            int currentActions = Runtime.CurrentInstructionCount;
            FunctionState currentState;
            try
            {
                endReached = !stateList[identifier].MoveNext();
                currentState = !endReached && stateList.ContainsKey(identifier) ? stateList[identifier].Current : stateComplete;
            }
            catch
            {
                errorCaught = true;
                currentState = stateComplete;
            }
            endTime = Now;
            if (errorCaught && reportErrors)
                Output($"Error in function: {identifier} : {stateErrorCodes[identifier]}");

            stateRecords[identifier].PostRun(Runtime.CurrentInstructionCount - currentActions, Now - startTime, errorCaught, reportErrors, currentState != stateActive);

            if (errorCaught || currentState != stateActive)
            {
                scriptHealth = 0;
                foreach (KeyValuePair<string, StateRecord> pair in stateRecords)
                    scriptHealth += (double)pair.Value.health;
                scriptHealth /= (double)stateRecords.Count;
                if (errorCaught || currentState == stateComplete)
                    StateDisposal(identifier);
                else
                    stateErrorCodes.Remove(identifier);
            }
            if (currentState != stateActive)
            {
                if (identifier == selfContainedIdentifier)
                    selfContainedIdentifier = "";
                if (identifier == currentMajorFunction)
                    currentMajorFunction = "Idle";
            }
            return currentState != stateActive;
        }

        double Round(double number, int decimals = 2)
        {
            return Math.Round(number, decimals);
        }

        static string ShortNumberAbs2(double number, List<string> suffixes, int decimals)
        {
            double divisor = 1, currentNumber = Math.Abs(number);
            int index = -1, highestIndex = -1;

            while (number >= divisor * 1000.0 && index + 1 < suffixes.Count)
            {
                divisor *= 1000.0;
                index++;
                if (TextHasLength(suffixes[index]))
                    highestIndex = index;
            }

            currentNumber /= Math.Pow(1000, highestIndex + 1);

            return highestIndex >= 0 ? $"{TruncateNumber(currentNumber, decimals)}{suffixes[highestIndex]}" : TruncateNumber(currentNumber, decimals);
        }

        static string ShortNumber2(double number, List<string> suffixesList = null, int decimals = 2, int padding = 0, bool left = true)
        {
            List<string> suffixes = NewStringList;
            suffixes.AddRange(suffixesList ?? settingsListsStrings[setKeyDefaultSuffixes]);
            string prefix = number < 0 ? "-" : "";

            return left ? $"{prefix}{ShortNumberAbs2(Math.Abs(number), suffixes, decimals)}".PadLeft(padding) : $"{prefix}{ShortNumberAbs2(Math.Abs(number), suffixes, decimals)}".PadRight(padding);
        }

        static bool TextHasLength(string text)
        {
            return text.Length > 0;
        }

        static bool BuilderHasLength(StringBuilder builder)
        {
            return builder.Length > 0;
        }

        static string RemoveSpaces(string text, bool lower = false)
        {
            return lower ? text.Replace(" ", "").ToLower() : text.Replace(" ", "");
        }

        bool IsWildCard(string text)
        {
            return StringsMatch(text, "all") || text == "*";
        }

        string GetFont(IMyTextSurface surface, string fontType)
        {
            List<string> fontList = NewStringList;
            surface.GetFonts(fontList);
            for (int i = 0; i < fontList.Count; i++)
                if (StringsMatch(fontType, fontList[i]))
                    return fontList[i];
            return "Monospace";
        }

        bool GetColor(out Color color, string colorSet)
        {
            color = Color.White;
            try
            {
                string[] colorArray = colorSet.Split(':');
                if (colorArray.Length == 4)
                    color = new Color(int.Parse(colorArray[0]), int.Parse(colorArray[1]), int.Parse(colorArray[2]), int.Parse(colorArray[3]));
                else if (colorArray.Length == 3)
                    color = new Color(int.Parse(colorArray[0]), int.Parse(colorArray[1]), int.Parse(colorArray[2]));
                else
                    return false;
                return true;
            }
            catch { }
            return false;
        }


        #endregion


        #region Methods

        static double TorchAverage(double a, double b)
        {
            return (a + (b * tickWeight)) * (1.0 - tickWeight);
        }

        static void BuilderAppendLine(StringBuilder builder, string text = "")
        {
            builder.AppendLine(text);
        }

        void PadEcho()
        {
            for (int i = 0; i < 10; i++)
                Echo("");
        }

        void FinalizeKeys(ref ItemDefinition definition)
        {
            definition.FinalizeKeys();
            if (definition.oreKeys.Count > 0)
                oreKeyedItemDictionary[definition.FullID] = definition.subtypeID;
            else
                oreKeyedItemDictionary.Remove(definition.FullID);
        }

        void AddCategory(string category)
        {
            if (!itemCategoryList.Contains(category))
                itemCategoryList.Add(category);
            if (!indexesStorageLists.ContainsKey(category))
                indexesStorageLists[category] = NewLongList;
        }

        void AppendHeader(ref StringBuilder builder, string header)
        {
            string prefix = "", suffix = "", spacer = "", cap = "";
            for (int i = 0; i < header.Length; i++)
            {
                prefix += "/";
                suffix += @"\";
                spacer += " ";
                cap += "|";
            }
            BuilderAppendLine(builder);

            BuilderAppendLine(builder, $"{prefix}{spacer}{suffix}");
            BuilderAppendLine(builder, $"{cap}  {header}  {cap}");
            BuilderAppendLine(builder, $"{suffix}{spacer}{prefix}");
            BuilderAppendLine(builder);
        }

        static void SplitID(string itemID, out string typeID, out string subtypeID)
        {
            int index = itemID.IndexOf("/");
            if (index == -1)
            {
                typeID = itemID;
                subtypeID = itemID;
                return;
            }
            typeID = itemID.Substring(0, index);
            subtypeID = itemID.Substring(index + 1);
        }

        void SetLastString(string text)
        {
            lastString = text;
            lastActionClearTime = Now.AddSeconds(15);
        }

        void SetGroup(string name, string amountString)
        {
            ItemCollection collection;
            double amount, maxAmount;
            ParseAmountAndMax(amountString, out amount, out maxAmount);
            if (customItemGroups.TryGetValue(name.ToLower(), out collection))
            {
                int count = collection.ItemTypeCount;
                for (int i = 0; i < count; i++)
                    SetQuota(collection.ItemIDByIndex(i), amount, maxAmount);
            }
        }

        void SetQuota(string itemID, double amount, double maxAmount)
        {
            string typeId, subtypeID;
            SplitID(itemID, out typeId, out subtypeID);
            SetQuota(typeId, subtypeID, amount, maxAmount);
        }

        void SetQuota(string typeID, string subtypeID, double amount, double maxAmount)
        {
            ItemDefinition definition;
            if (GetDefinition(out definition, $"{typeID}/{subtypeID}"))
                SetDefinitionQuota(definition, amount, maxAmount);
        }

        void SetItemQuotaMain(string name, string amountString)
        {
            if (TextHasLength(name))
            {
                double amount, maxAmount;
                ParseAmountAndMax(amountString, out amount, out maxAmount);
                bool category = IsCategory(name);
                SetTypeQuota(amount, maxAmount, category ? "" : name, category ? (IsWildCard(name) ? "" : name) : "");
            }
        }

        void ParseAmountAndMax(string data, out double amount, out double maxAmount)
        {
            int index = data.IndexOf("<");
            amount = index > 0 ? double.Parse(data.Substring(0, index)) : double.Parse(data);
            maxAmount = index > 0 ? double.Parse(data.Substring(index + 1)) : amount;
        }

        void SetTypeQuota(double amount, double maxAmount, string filter = "", string category = "")
        {
            List<ItemDefinition> itemList = GetAllItems;
            string subFilter = RemoveSpaces(filter);
            bool exactMatch = subFilter.Length > 2 && LeadsString(subFilter, "'") && EndsString(subFilter, "'");
            if (exactMatch)
                subFilter = subFilter.Substring(1, subFilter.Length - 2);
            for (int i = 0; i < itemList.Count; i++)
                if ((!TextHasLength(category) || StringsMatch(category, itemList[i].category)) &&
                    (!TextHasLength(subFilter) || (!exactMatch && LeadsString(itemList[i].displayName, subFilter)) || (exactMatch && StringsMatch(RemoveSpaces(itemList[i].displayName), subFilter))))
                    SetDefinitionQuota(itemList[i], amount, maxAmount);
        }

        void SetDefinitionQuota(ItemDefinition definition, double amount, double max)
        {
            double maxAmount = max + definition.blockQuota;
            definition.quota = amount;
            definition.quotaMax = amount <= max ? max : amount;
            definition.currentMax = maxAmount >= definition.quotaMax ? maxAmount : definition.quotaMax;

            if (definition.quotaMax < 0)
                definition.quotaMax = 0;

            definition.SetCurrentQuota();
        }

        void AddModItem(MyInventoryItem item)
        {
            string subtypeID = item.Type.SubtypeId, itemID = item.Type.ToString(), blueprintMatchKey = subtypeID;
            if (AddItemDef(subtypeID, subtypeID, item.Type.TypeId, ""))
                saving = true;

            if (IsIngot(item) || IsOre(item))
                return;

            if (modBlueprintList.Contains(blueprintMatchKey) || HasBlueprintMatch(item, ref blueprintMatchKey))
            {
                if (UpdateItemDef(itemID, blueprintMatchKey))
                {
                    SetLastString($"Merged Mod Item: {ShortenName(subtypeID, 15)}");
                    saving = true;
                }
            }
            else if (!modItemDictionary.ContainsKey(itemID))
                modItemDictionary[itemID] = subtypeID;
        }

        void AddModBlueprint(MyProductionItem blueprint)
        {
            string subtypeID = BlueprintSubtype(blueprint), itemMatchKey = "";
            if (HasItemMatch(blueprint, ref itemMatchKey))
            {
                if (UpdateItemDef(itemMatchKey, subtypeID))
                {
                    SetLastString($"Merged Mod Item: {ShortenName(itemMatchKey, 15)}");
                    saving = true;
                }
            }
            else if (!modBlueprintList.Contains(subtypeID))
                modBlueprintList.Add(subtypeID);
        }

        void ClearFunctions()
        {
            int index = 0;
            while (index < stateList.Count)
                if (stateList.Keys[index] == functionList[0])
                    index++;
                else
                    StateDisposal(stateList.Keys[index]);
        }

        void Output(string output)
        {
            if (TextHasLength(output))
            {
                if (LeadsString(output, "Error"))
                {
                    currentErrorCount++;
                    totalErrorCount++;
                    AddOutput(output, ref outputErrorList);
                    SetLastString(output);
                }
                AddOutput(output, ref outputList, outputLimit);
            }
        }

        void AddOutput(string output, ref List<OutputObject> list, int limit = 30)
        {
            bool unique = true;

            for (int i = 0; i < list.Count; i++)
                if (list[i].text == output)
                {
                    list[i].count++;
                    list.Move(i, 0);
                    unique = false;
                    break;
                }

            if (unique)
                list.Insert(0, new OutputObject { text = output });

            if (list.Count > limit)
                list.RemoveRange(limit, list.Count - limit);
        }

        void InitializeState(IEnumerator<FunctionState> stateFunction, string identifier)
        {

            stateList[identifier] = stateFunction;
            if (!stateRecords.ContainsKey(identifier))
                stateRecords[identifier] = new StateRecord();

            stateErrorCodes[identifier] = "";
        }

        void ResetRuntimes()
        {
            Runtime.UpdateFrequency = UpdateFrequency.None;
            Runtime.UpdateFrequency &= ~UpdateFrequency.Update1;
            Runtime.UpdateFrequency &= ~UpdateFrequency.Update10;
            Runtime.UpdateFrequency &= ~UpdateFrequency.Update100;

            Runtime.UpdateFrequency = updateFrequency == 100 ? UpdateFrequency.Update100 : updateFrequency == 1 ? UpdateFrequency.Update1 : UpdateFrequency.Update10;
        }

        void AddBlueprintAmount(string blueprintID, bool assembly, double amount, bool current)
        {
            Blueprint blueprint;
            if (blueprintList.TryGetValue(blueprintID, out blueprint))
                AddBlueprintAmount(blueprint.typeID, blueprint.subtypeID, assembly, amount, current);
        }

        void AddBlueprintAmount(MyProductionItem item, bool assembly, bool current = false)
        {
            if (blueprintList.ContainsKey(BlueprintSubtype(item)))
            {
                Blueprint blueprint = blueprintList[BlueprintSubtype(item)];
                AddBlueprintAmount(blueprint.typeID, blueprint.subtypeID, assembly, (double)item.Amount, current);
            }
        }

        void AddBlueprintAmount(string typeID, string subtypeID, bool assembly, double amount, bool current)
        {
            ItemDefinition definition;
            if (GetDefinition(out definition, $"{typeID}/{subtypeID}"))
            {
                if (assembly)
                    definition.AddAssemblyAmount((MyFixedPoint)amount, current);
                else
                    definition.AddDisassemblyAmount((MyFixedPoint)amount, current);
            }
        }

        void AddAmount(MyInventoryItem item)
        {
            ItemDefinition definition;
            if (GetDefinition(out definition, item.Type.ToString()))
            {
                definition.AddAmount(item.Amount);
                if (definition.volume == 0)
                {
                    definition.volume = ItemVolume(item);
                    definition.fractional = FractionalItem(item);
                }
            }
        }

        void ConveyorControl(BlockDefinition managedBlock)
        {
            IMyTerminalBlock block = managedBlock.block;
            bool applyOverride = false, autoConveyor = managedBlock.Settings.AutoConveyor, tempBool;

            if (block is IMyAssembler)
                ((IMyProductionBlock)block).UseConveyorSystem = true;
            else if (block is IMyRefinery)
                ((IMyProductionBlock)block).UseConveyorSystem = autoConveyor || GetKeyBool(setKeyAutoConveyorRefineries);
            else if (block is IMyReactor)
                ((IMyReactor)block).UseConveyorSystem = autoConveyor || GetKeyBool(setKeyAutoConveyorReactors);
            else if (block is IMyGasGenerator)
                ((IMyGasGenerator)block).UseConveyorSystem = autoConveyor || GetKeyBool(setKeyAutoConveyorGasGenerators);
            else if (block is IMySmallGatlingGun)
            {
                tempBool = ((IMySmallGatlingGun)block).UseConveyorSystem;
                if (autoConveyor || GetKeyBool(setKeyAutoConveyorGuns))
                    applyOverride = !tempBool;
                else
                    applyOverride = tempBool;
            }
            else if (block is IMyLargeGatlingTurret)
            {
                tempBool = ((IMyLargeGatlingTurret)block).UseConveyorSystem;
                if (autoConveyor || GetKeyBool(setKeyAutoConveyorGuns))
                    applyOverride = !tempBool;
                else
                    applyOverride = tempBool;
            }
            else if (block is IMyLargeMissileTurret)
            {
                tempBool = ((IMyLargeMissileTurret)block).UseConveyorSystem;
                if (autoConveyor || GetKeyBool(setKeyAutoConveyorGuns))
                    applyOverride = !tempBool;
                else
                    applyOverride = tempBool;
            }
            else if (block is IMySmallMissileLauncherReload)
            {
                tempBool = ((IMySmallMissileLauncherReload)block).UseConveyorSystem;
                if (autoConveyor || GetKeyBool(setKeyAutoConveyorGuns))
                    applyOverride = !tempBool;
                else
                    applyOverride = tempBool;
            }
            else if (block is IMySmallMissileLauncher)
            {
                tempBool = ((IMySmallMissileLauncher)block).UseConveyorSystem;
                if (autoConveyor || GetKeyBool(setKeyAutoConveyorGuns))
                    applyOverride = !tempBool;
                else
                    applyOverride = tempBool;
            }
            if (applyOverride)
                block.ApplyAction("UseConveyor");
        }

        bool AddItemDef(string name, string subtypeID, string typeID, string blueprintID = nothingType, bool display = true, List<string> oreKeys = null)
        {
            if (!itemListMain.ContainsKey(typeID))
                itemListMain[typeID] = new SortedList<string, ItemDefinition>();

            if (!itemListMain[typeID].ContainsKey(subtypeID))
            {
                itemListMain[typeID][subtypeID] = new ItemDefinition
                {
                    typeID = typeID,
                    subtypeID = subtypeID,
                    displayName = name,
                    blueprintID = blueprintID,
                    display = display
                };
                InitialDefinition($"{typeID}/{subtypeID}", blueprintID, oreKeys);
                return true;
            }
            return false;
        }

        void InitialDefinition(string itemID, string blueprintID, List<string> oreKeys = null)
        {
            ItemDefinition definition;
            if (GetDefinition(out definition, itemID))
            {
                if (IsIngot(definition.typeID))
                {
                    definition.quota = 100;
                    definition.quotaMax = 100;
                    if (definition.subtypeID == "Uranium")
                        definition.fuel = true;
                }
                definition.category = GetItemCategory(definition.FullID);
                if (IsIce(definition.typeID, definition.subtypeID))
                {
                    definition.gas = true;
                    definition.refine = false;
                }
                else if (IsOre(definition.typeID))
                    definition.refine = true;

                UpdateItemDef(itemID, blueprintID, oreKeys);
            }
        }

        bool UpdateItemDef(string itemID, string blueprintID = "", List<string> oreKeys = null)
        {
            ItemDefinition definition;
            if (GetDefinition(out definition, itemID))
            {
                if (IsIngot(definition.typeID) && definition.oreKeys.Count == 0)
                    definition.oreKeys.Add(definition.subtypeID);

                if (oreKeys != null)
                    definition.oreKeys.AddRange(oreKeys);

                FinalizeKeys(ref definition);
                if (IsBlueprint(definition.blueprintID))
                    blueprintList.Remove(definition.blueprintID);

                definition.blueprintID = blueprintID;
                if (IsBlueprint(blueprintID))
                    blueprintList[blueprintID] = ItemToBlueprint(definition);

                string category = definition.category;
                itemCategoryDictionary[definition.FullID] = category;
                AddCategory(category);
                CheckModdedItem(definition);
                return true;
            }
            return false;
        }

        void CheckModdedItem(ItemDefinition definition)
        {
            if (!IsIngot(definition.typeID) && !IsOre(definition.typeID) && !TextHasLength(definition.blueprintID))
            {
                if (!modItemDictionary.ContainsKey(definition.FullID))
                    modItemDictionary[definition.FullID] = definition.subtypeID;
            }
            else
                modItemDictionary.Remove(definition.FullID);

            if (IsBlueprint(definition.blueprintID))
                modBlueprintList.Remove(definition.blueprintID);
        }

        void StateDisposal(string identifier, bool dispose = true)
        {
            if (dispose)
            {
                try
                {
                    stateList[identifier].Dispose();
                }
                catch { }
                try
                {
                    stateList[identifier] = null;
                }
                catch { }
                stateList.Remove(identifier);
            }
            stateErrorCodes.Remove(identifier);
        }

        static void AppendOption(StringBuilder builder, string option, bool optional = true)
        {
            BuilderAppendLine(builder, $"{(optional ? "//" : "")}{option}");
        }

        void AppendOption(StringBuilder builder, StringBuilder optionBuilder, string option, bool optional)
        {
            if (optional)
                optionBuilder.Append($"{OptionLead(optionBuilder)}{Formatted(option)}");
            else
                builder.Append($"{OptionLead(builder)}{Formatted(option)}");
        }

        void AppendOption(SortedList<string, bool> list, StringBuilder builder, StringBuilder optionBuilder)
        {
            foreach (KeyValuePair<string, bool> option in list)
                AppendOption(builder, optionBuilder, option.Key, !option.Value);
        }

        void SetPanelDefinition(BlockDefinition managedBlock, int surfaceIndex)
        {
            managedBlock.panelDefinitionList[surfaceIndex] = new PanelMasterClass.PanelDefinition { surfaceIndex = surfaceIndex, provider = !(managedBlock.block is IMyTextPanel), suffixes = settingsListsStrings[setKeyDefaultSuffixes], parent = managedBlock };
        }


        #endregion


        #region Classes


        public class PanelMasterClass
        {
            #region Variables

            public static Program parent;

            StringBuilder measurementBuilder = NewBuilder;

            SortedList<string, PregeneratedPanels> generatedPanels = new SortedList<string, PregeneratedPanels>();

            double tempCapacity = 0;

            string selfContainedIdentifier;

            SortedList<string, int> assemblyList = new SortedList<string, int>(),
                                    disassemblyList = new SortedList<string, int>();

            #endregion


            #region Short Links

            bool PauseTickRun { get { return parent.PauseTickRun; } }
            bool IsStateActive { get { return StateActive(selfContainedIdentifier); } }
            bool RunStateManager { get { return StateManager(selfContainedIdentifier); } }
            public static List<PanelObject> NewPanelObjectList { get { return new List<PanelObject>(); } }

            bool StateActive(string identifier)
            {
                return parent.StateActive(identifier);
            }

            bool StateManager(string identifier)
            {
                return parent.StateManager(identifier, true, false);
            }

            void InitializeState(IEnumerator<FunctionState> stateFunction, string identifier)
            {
                parent.InitializeState(stateFunction, identifier);
            }

            #endregion


            #region Methods

            public static string PresetPanelOption(string itemCategoryString)
            {
                StringBuilder builder = NewBuilder;
                BuilderAppendLine(builder, $"Type=Item/Cargo/Output/Status/Span");
                AppendOption(builder, "Font=Monospace");
                AppendOption(builder, $"Categories={itemCategoryString}");
                AppendOption(builder, "Items=ingot:Iron|ore:Iron");
                AppendOption(builder, "Item Display=Standard|Detailed|CompactAmount|CompactPercent");
                AppendOption(builder, "Sorting=Alphabetical|AscendingAmount|DescendingAmount|AscendingPercent|DescendingPercent");
                AppendOption(builder, "Options=BelowQuota|HideProgressBar");
                AppendOption(builder, "Minimum Value=1");
                AppendOption(builder, "Maximum Value=150000");
                AppendOption(builder, "Number Suffixes=K|M|B|T");
                AppendOption(builder, "Text Color=0:0:0:255");
                AppendOption(builder, "Number Color=120:0:0:255");
                AppendOption(builder, "Back Color=255:255:255:0");
                AppendOption(builder, "Rows=15");
                AppendOption(builder, "Name Length=18");
                AppendOption(builder, "Decimals=2");
                AppendOption(builder, "Update Delay=1");
                AppendOption(builder, "Span ID=Span A");
                AppendOption(builder, "Span Child ID=Span B");
                return builder.ToString().Trim();
            }

            public void CheckPanel(BlockDefinition blockDefinition, int surfaceIndex = 0)
            {
                PanelMasterClass.PanelDefinition panelDefinition = blockDefinition.panelDefinitionList[surfaceIndex];
                string hashKey = panelDefinition.EntityFlickerID();

                if (parent.antiflickerSet.Add(hashKey))
                {
                    IMyTextSurface surface = panelDefinition.GetSurface();
                    if (surface.ContentType != ContentType.SCRIPT)
                        surface.ContentType = ContentType.SCRIPT;

                    if (surface.Script != nothingType)
                        surface.Script = nothingType;

                    if (surface.ScriptForegroundColor == parent.defPanelForegroundColor)
                    {
                        surface.ScriptForegroundColor = Color.Black;
                        surface.ScriptBackgroundColor = new Color(73, 141, 255, 255);
                    }
                }
            }

            void ClonePanelObjects(List<PanelObject> panelObjects, List<PanelObject> list)
            {
                panelObjects.Clear();
                foreach (PanelObject panelObject in list)
                    panelObjects.Add(panelObject.Clone());
            }

            string BlockStatusTitle(string title, int disabled)
            {
                string formTitle = title;
                if (disabled > 0)
                    formTitle += $" -({ShortNumber2(disabled)})";
                return formTitle;
            }

            Vector2 NewVector2(float x = 0f, float y = 0f)
            {
                return new Vector2(x, y);
            }

            MySprite GenerateTextureSprite(string textureType, Color detailColor, Vector2 position, Vector2 size)
            {
                return new MySprite(SpriteType.TEXTURE, textureType, position + (size / 2f), size, detailColor);
            }

            MySprite GenerateTextSprite(string text, Color detailColor, Vector2 position, Vector2 currentSize, TextAlignment alignment, string font, IMyTextSurface surface)
            {
                Vector2 textOffset, fontMeasurement, paddingOffset = currentSize * 0.035f, size;
                size = currentSize * 0.93f;
                measurementBuilder.Clear();
                measurementBuilder.Append(text);
                fontMeasurement = surface.MeasureStringInPixels(measurementBuilder, font, 1f);
                float fontSize = Math.Min(size.X / fontMeasurement.X, size.Y / fontMeasurement.Y);
                fontMeasurement = surface.MeasureStringInPixels(measurementBuilder, font, (float)fontSize);

                textOffset = alignment == centerAlignment ? NewVector2(size.X * 0.5f) : NewVector2();
                textOffset.Y = (size.Y * 0.5f) - (fontMeasurement.Y * 0.5f);
                return new MySprite(SpriteType.TEXT, text, position + textOffset + paddingOffset, size, detailColor, font, alignment, fontSize);
            }

            List<PanelDetail> GenerateProgressBarDetails(float givenPercent)
            {
                float percent = Math.Max(Math.Min(1f, givenPercent), 0f);
                List<PanelDetail> myDetails = new List<PanelDetail>
            {
                new PanelDetail { textureType = "SquareHollow", textureColor = new Color(0, 0, 0, 180), ratio = 0.25f },
                new PanelDetail { textureType = "SquareSimple", textureColor = new Color((int)(230f * (float)percent), (int)(230f * (1f - (float)percent)), 0, 220), ratio = 0.25f * percent }
            };
                return myDetails;
            }

            List<MySprite> GenerateProgressBarSprites(float givenPercent, Vector2 offset, Vector2 size)
            {
                float percent = Math.Max(Math.Min(1f, givenPercent), 0f);
                List<MySprite> mySprites = new List<MySprite>
                {
                    GenerateTextureSprite("SquareHollow", new Color(0, 0, 0, 180), offset, size),
                    GenerateTextureSprite("SquareSimple", new Color((int)(230f * (1f - (float)percent)), (int)(230f * (float)percent), 0, 220), offset, NewVector2(size.X * percent, size.Y))
                };
                return mySprites;
            }

            void AddOutputItem(PanelDefinition panelDefinition, string text)
            {
                panelDefinition.AddPanelDetail(text.PadRight(panelDefinition.nameLength));
            }

            void BlockStatus(long index, ref int assembling, ref int disassembling, ref int idle, ref int disabled, SortedList<string, int> assemblyList, SortedList<string, int> disassemblyList)
            {
                IMyTerminalBlock block = managedBlocks[index].block;
                MyInventoryItem item;
                MyProductionItem productionItem;
                string key;
                if (!parent.IsBlockOk(index) || !((IMyFunctionalBlock)block).Enabled)
                {
                    disabled++;
                    return;
                }
                if (block is IMyAssembler)
                {
                    IMyAssembler assembler = (IMyAssembler)block;
                    if (assembler.IsQueueEmpty)
                        idle++;
                    else
                    {
                        List<MyProductionItem> productionList = NewProductionList;
                        assembler.GetQueue(productionList);
                        productionItem = productionList[0];
                        key = BlueprintSubtype(productionItem);
                        if (parent.blueprintList.ContainsKey(key))
                        {
                            Blueprint blueprint = parent.blueprintList[key];
                            key = parent.ItemName(blueprint.typeID, blueprint.subtypeID);
                        }
                        if (assembler.Mode == assemblyMode)
                        {
                            assembling++;
                            if (!assemblyList.ContainsKey(key))
                                assemblyList[key] = (int)productionItem.Amount;
                            else
                                assemblyList[key] += (int)productionItem.Amount;
                        }
                        else
                        {
                            disassembling++;
                            if (!disassemblyList.ContainsKey(key))
                                disassemblyList[key] = (int)productionItem.Amount;
                            else
                                disassemblyList[key] += (int)productionItem.Amount;
                        }
                    }
                }
                else if (block is IMyRefinery)
                {
                    if (block.GetInventory(0).ItemCount == 0)
                        idle++;
                    else
                    {
                        assembling++;
                        item = (MyInventoryItem)block.GetInventory(0).GetItemAt(0);
                        key = parent.ItemName(item);
                        if (!assemblyList.ContainsKey(key))
                            assemblyList[key] = (int)(item).Amount;
                        else
                            assemblyList[key] += (int)(item).Amount;
                    }
                }
                else if (block is IMyGasGenerator)
                {
                    if (block.GetInventory(0).ItemCount > 0)
                    {
                        item = (MyInventoryItem)block.GetInventory(0).GetItemAt(0);
                        key = parent.ItemName(item);
                        if (!assemblyList.ContainsKey(key))
                            assemblyList[key] = (int)item.Amount;
                        else
                            assemblyList[key] += (int)item.Amount;
                        if (parent.IsGas(item))
                            assembling++;
                        else
                            idle++;
                    }
                    else
                        idle++;
                }
            }


            #endregion


            #region State Functions

            bool PopulateSprites(PanelDefinition panelDefinition)
            {
                selfContainedIdentifier = functionList[3];
                if (!IsStateActive)
                {
                    parent.InitializeState(PopulateSpriteState(panelDefinition), selfContainedIdentifier);
                    if (PauseTickRun) return false;
                }
                return RunStateManager;
            }

            IEnumerator<FunctionState> PopulateSpriteState(PanelDefinition panelDefinition)
            {
                panelDefinition.spriteList.Clear();
                int column = 0;

                if (panelDefinition.panelObjects.Count > 0)
                {
                    while (!PopulateSpriteList(panelDefinition, panelDefinition.panelObjects, column, false)) yield return stateActive;
                    column = 1;
                }
                if (panelDefinition.spannableObjects.Count > 0)
                    while (!PopulateSpriteList(panelDefinition, panelDefinition.spannableObjects, column, true)) yield return stateActive;

                yield return stateComplete;
            }

            bool PopulateSpriteList(PanelDefinition panelDefinition, List<PanelObject> panelObjects, float column, bool span)
            {
                selfContainedIdentifier = functionList[18];
                if (!IsStateActive)
                {
                    parent.InitializeState(PopulateSpriteListState(panelDefinition, panelObjects, column, span), selfContainedIdentifier);
                    if (PauseTickRun) return false;
                }
                return RunStateManager;
            }

            IEnumerator<FunctionState> PopulateSpriteListState(PanelDefinition panelDefinition, List<PanelObject> panelObjects, float column, bool span)
            {
                List<PanelObject> leftoverObjects = NewPanelObjectList;
                IMyTextSurface surface = panelDefinition.GetSurface();
                int numberPadding = panelDefinition.decimals + 4;
                float percent;
                if (panelDefinition.decimals > 0)
                    numberPadding++;
                Vector2 maxLocation = NewVector2(panelDefinition.columns, panelDefinition.rows),
                        currentLocation,
                        objectSize,
                        currentOffset,
                        detailSize,
                        subOffset;

                if (!span)
                    maxLocation.Y = panelObjects.Count;

                objectSize = NewVector2(panelDefinition.size.X / maxLocation.X, panelDefinition.size.Y / maxLocation.Y);
                currentLocation = NewVector2(column);

                //Cycle objects
                foreach (PanelObject panelObject in panelObjects)
                {
                    if (PauseTickRun) yield return stateActive;
                    if (!span || currentLocation.Y < maxLocation.Y) // If objects are fixed (not spannable) and the row is within bounds
                    {
                        // Set current position
                        currentOffset = NewVector2(currentLocation.X * objectSize.X, currentLocation.Y * objectSize.Y) + panelDefinition.positionOffset;

                        //Generate single backdrop, if any
                        if (TextHasLength(panelObject.backdropType) && panelDefinition.backdropColor.A > 0)
                            panelDefinition.spriteList.Add(GenerateTextureSprite(panelObject.backdropType, panelDefinition.backdropColor, currentOffset, objectSize));

                        //Cycle details
                        foreach (PanelDetail panelDetail in panelObject.panelDetails)
                        {
                            if (PauseTickRun) yield return stateActive;
                            if (panelObject.item) // Process item
                            {
                                percent = (float)(panelDetail.itemAmount / panelDetail.itemQuota);
                                if (panelDetail.itemQuota <= 0f)
                                    percent = 1f;
                                switch (panelDefinition.displayType)
                                {
                                    case DisplayType.CompactAmount:
                                        //Name @ 75%
                                        detailSize = NewVector2(objectSize.X * 0.75f, objectSize.Y);
                                        panelDefinition.spriteList.Add(GenerateTextSprite(panelDetail.itemName, panelDefinition.textColor, currentOffset, detailSize, panelDetail.alignment, panelDefinition.font, surface));
                                        subOffset = NewVector2(detailSize.X);
                                        //Number @ 25%
                                        detailSize = NewVector2(objectSize.X * 0.25f, objectSize.Y);
                                        panelDefinition.spriteList.Add(GenerateTextSprite(ShortNumber2(panelDetail.itemAmount, panelDefinition.suffixes, panelDefinition.decimals, numberPadding), panelDefinition.numberColor, currentOffset + subOffset, detailSize, panelDetail.alignment, panelDefinition.font, surface));
                                        break;
                                    case DisplayType.CompactPercent:
                                        //Name @ 75%
                                        detailSize = NewVector2(objectSize.X * 0.75f, objectSize.Y);
                                        panelDefinition.spriteList.Add(GenerateTextSprite(panelDetail.itemName, panelDefinition.textColor, currentOffset, detailSize, panelDetail.alignment, panelDefinition.font, surface));
                                        subOffset = NewVector2(detailSize.X);
                                        //Percentage @ 25%
                                        if (panelDefinition.showProgressBar)
                                        {
                                            detailSize = NewVector2(objectSize.X * 0.25f, objectSize.Y);
                                            panelDefinition.spriteList.AddRange(GenerateProgressBarSprites(percent, currentOffset + subOffset, detailSize));
                                        }
                                        break;
                                    case DisplayType.Detailed:
                                        //Name @ 60% x 50%
                                        detailSize = NewVector2(objectSize.X * 0.6f, objectSize.Y / 2f);
                                        panelDefinition.spriteList.Add(GenerateTextSprite(panelDetail.itemName, panelDefinition.textColor, currentOffset, detailSize, panelDetail.alignment, panelDefinition.font, surface));
                                        subOffset = NewVector2(detailSize.X);
                                        //Count @ 20% x 50%
                                        detailSize.X = objectSize.X * 0.2f;
                                        panelDefinition.spriteList.Add(GenerateTextSprite($"{ShortNumber2(panelDetail.itemAmount, panelDefinition.suffixes, panelDefinition.decimals, numberPadding)}/", panelDefinition.numberColor, currentOffset + subOffset, detailSize, panelDetail.alignment, panelDefinition.font, surface));
                                        subOffset += NewVector2(detailSize.X);
                                        //Percent @ 20% x 50%
                                        if (panelDefinition.showProgressBar)
                                            panelDefinition.spriteList.AddRange(GenerateProgressBarSprites(percent, currentOffset + subOffset, detailSize));
                                        //Quota @ 20% x 50%
                                        panelDefinition.spriteList.Add(GenerateTextSprite(ShortNumber2(panelDetail.itemQuota, panelDefinition.suffixes, panelDefinition.decimals, numberPadding), panelDefinition.numberColor, currentOffset + subOffset, detailSize, panelDetail.alignment, panelDefinition.font, surface));
                                        subOffset = NewVector2(0, detailSize.Y);
                                        measurementBuilder.Clear();
                                        if (panelDetail.assemblyAmount > 0)
                                            measurementBuilder.Append($"Assembling: {ShortNumber2(panelDetail.assemblyAmount, panelDefinition.suffixes, panelDefinition.decimals, numberPadding)}");
                                        if (panelDetail.disassemblyAmount > 0)
                                        {
                                            if (BuilderHasLength(measurementBuilder))
                                                measurementBuilder.Append(", ");
                                            measurementBuilder.Append($"Disassembling: {ShortNumber2(panelDetail.disassemblyAmount, panelDefinition.suffixes, panelDefinition.decimals, numberPadding)}");
                                        }
                                        //Assembly status @ 75% x 50%
                                        detailSize.X = objectSize.X * 0.75f;
                                        panelDefinition.spriteList.Add(GenerateTextSprite(measurementBuilder.ToString(), panelDefinition.textColor, currentOffset + subOffset, detailSize, panelDetail.alignment, panelDefinition.font, surface));
                                        subOffset += NewVector2(detailSize.X);
                                        //Rate @ 25% x 50%
                                        detailSize.X = objectSize.X * 0.25f;
                                        panelDefinition.spriteList.Add(GenerateTextSprite($"Rate: {ShortNumber2(panelDetail.amountDifference, panelDefinition.suffixes, panelDefinition.decimals, numberPadding)}", panelDefinition.textColor, currentOffset + subOffset, detailSize, panelDetail.alignment, panelDefinition.font, surface));
                                        break;
                                    case DisplayType.Standard:
                                        //Name @ 60%
                                        detailSize = NewVector2(objectSize.X * 0.6f, objectSize.Y);
                                        panelDefinition.spriteList.Add(GenerateTextSprite(panelDetail.itemName, panelDefinition.textColor, currentOffset, detailSize, panelDetail.alignment, panelDefinition.font, surface));
                                        subOffset = NewVector2(detailSize.X);
                                        //Count @ 20%
                                        detailSize = NewVector2(objectSize.X * 0.2f, objectSize.Y);
                                        panelDefinition.spriteList.Add(GenerateTextSprite($"{ShortNumber2(panelDetail.itemAmount, panelDefinition.suffixes, panelDefinition.decimals, numberPadding)}/", panelDefinition.numberColor, currentOffset + subOffset, detailSize, panelDetail.alignment, panelDefinition.font, surface));
                                        subOffset += NewVector2(detailSize.X);
                                        //Percent @ 20%
                                        panelDefinition.spriteList.AddRange(GenerateProgressBarSprites(percent, currentOffset + subOffset, detailSize));
                                        //Quota @ 20%
                                        panelDefinition.spriteList.Add(GenerateTextSprite(ShortNumber2(panelDetail.itemQuota, panelDefinition.suffixes, panelDefinition.decimals, numberPadding), panelDefinition.numberColor, currentOffset + subOffset, detailSize, panelDetail.alignment, panelDefinition.font, surface));
                                        break;
                                }
                            }
                            else // Process everything else
                            {
                                detailSize = NewVector2(objectSize.X * panelDetail.ratio, objectSize.Y);

                                panelDefinition.spriteList.Add
                                    (
                                        TextHasLength(panelDetail.textureType) ? GenerateTextureSprite(panelDetail.textureType, panelDetail.textureColor, currentOffset, detailSize) :
                                        TextHasLength(panelDetail.text) ? GenerateTextSprite(panelDetail.text, panelDefinition.textColor, currentOffset, detailSize, panelDetail.alignment, panelDefinition.font, surface) :
                                        GenerateTextSprite(ShortNumber2(panelDetail.value, panelDefinition.suffixes, panelDefinition.decimals, numberPadding), panelDefinition.textColor, currentOffset, detailSize, panelDetail.alignment, panelDefinition.font, surface)
                                    );

                                if (panelDetail.reservedArea)
                                    currentOffset += NewVector2(detailSize.X);
                            }
                        }
                        currentLocation.Y += 1f;
                    }
                    else if (panelDefinition.span)
                        leftoverObjects.Add(panelObject.Clone());
                    else
                        break;
                }

                foreach (SpanKey spanKey in panelDefinition.spannedPanelList)
                {
                    if (PauseTickRun) yield return stateActive;
                    managedBlocks[spanKey.index].panelDefinitionList[spanKey.surfaceIndex].spannableObjects.Clear();
                    if (leftoverObjects.Count > 0)
                        ClonePanelObjects(managedBlocks[spanKey.index].panelDefinitionList[spanKey.surfaceIndex].spannableObjects, leftoverObjects);
                }

                yield return stateComplete;
            }

            public bool TotalPanelV2(PanelDefinition panelDefinition)
            {
                selfContainedIdentifier = functionList[51];
                if (!IsStateActive)
                {
                    InitializeState(TotalPanelStateV2(panelDefinition), selfContainedIdentifier);
                    if (PauseTickRun) return false;
                }
                return RunStateManager;
            }

            IEnumerator<FunctionState> TotalPanelStateV2(PanelDefinition panelDefinition)
            {
                if (Now >= panelDefinition.nextUpdateTime)
                {
                    string panelOptionString = panelDefinition.settingKey;
                    panelDefinition.panelObjects.Clear();
                    if (panelDefinition.panelType != PanelType.Span)
                    {
                        bool cachePanel = TextHasLength(panelOptionString);
                        panelDefinition.spannableObjects.Clear();
                        if (cachePanel && generatedPanels.ContainsKey(panelOptionString) && generatedPanels[panelOptionString].nextUpdateTime > Now)
                        {
                            ClonePanelObjects(panelDefinition.panelObjects, generatedPanels[panelOptionString].panelObjects);
                            ClonePanelObjects(panelDefinition.spannableObjects, generatedPanels[panelOptionString].spannableObjects);
                        }
                        else
                        {
                            switch (panelDefinition.panelType)
                            {
                                case PanelType.Cargo:
                                    while (!CargoPanel(panelDefinition)) yield return stateActive;
                                    break;
                                case PanelType.Item:
                                    while (!ItemPanel(panelDefinition)) yield return stateActive;
                                    break;
                                case PanelType.Output:
                                    while (!OutputPanel(panelDefinition)) yield return stateActive;
                                    break;
                                case PanelType.Status:
                                    while (!StatusPanel(panelDefinition)) yield return stateActive;
                                    break;
                            }
                            if (cachePanel)
                            {
                                if (!generatedPanels.ContainsKey(panelOptionString))
                                    generatedPanels[panelOptionString] = new PregeneratedPanels();
                                else
                                {
                                    generatedPanels[panelOptionString].panelObjects.Clear();
                                    generatedPanels[panelOptionString].spannableObjects.Clear();
                                }
                                ClonePanelObjects(generatedPanels[panelOptionString].panelObjects, panelDefinition.panelObjects);
                                ClonePanelObjects(generatedPanels[panelOptionString].spannableObjects, panelDefinition.spannableObjects);
                                generatedPanels[panelOptionString].nextUpdateTime = Now.AddSeconds(panelDefinition.updateDelay);
                            }
                        }
                    }
                    panelDefinition.nextUpdateTime = Now.AddSeconds(panelDefinition.updateDelay);

                    while (!PopulateSprites(panelDefinition)) yield return stateActive;
                    if (PauseTickRun) yield return stateActive;
                    MySpriteDrawFrame frame = panelDefinition.GetSurface().DrawFrame();
                    frame.AddRange(panelDefinition.spriteList);
                    frame.Dispose();
                    for (int i = 0; i < generatedPanels.Count; i += 0)
                    {
                        if (PauseTickRun) yield return stateActive;
                        if ((Now - generatedPanels.Values[i].nextUpdateTime).TotalSeconds >= 60)
                            generatedPanels.RemoveAt(i);
                        else
                            i++;
                    }
                }
                yield return stateComplete;
            }

            bool CargoPanel(PanelDefinition panelDefinition)
            {
                selfContainedIdentifier = functionList[25];
                if (!IsStateActive)
                {
                    InitializeState(CargoPanelState(panelDefinition), selfContainedIdentifier);
                    if (PauseTickRun) return false;
                }
                return RunStateManager;
            }

            IEnumerator<FunctionState> CargoPanelState(PanelDefinition panelDefinition)
            {
                double capacity = 0;
                if (panelDefinition.itemCategories.Contains("all"))
                {
                    while (!CargoCapacity(ref capacity, typedIndexes[setKeyIndexStorage])) yield return stateActive;
                    panelDefinition.AddPanelDetail("Total:".PadRight(panelDefinition.nameLength), false, 0.75f, true, true);
                    if (panelDefinition.showProgressBar)
                        panelDefinition.AddPanelDetails(GenerateProgressBarDetails((float)capacity));
                    panelDefinition.AddPanelDetail($"{ShortNumber2(capacity * 100.0, panelDefinition.suffixes, 2, 5)}%", false, 0.25f, false);
                }
                foreach (string category in panelDefinition.itemCategories)
                {
                    if (PauseTickRun) yield return stateActive;
                    if (category != "all" && parent.indexesStorageLists.ContainsKey(category))
                    {
                        while (!CargoCapacity(ref capacity, parent.indexesStorageLists[category])) yield return stateActive;
                        panelDefinition.AddPanelDetail($"{Formatted(category)}:".PadRight(panelDefinition.nameLength), false, 0.75f, true, true);
                        if (panelDefinition.showProgressBar)
                            panelDefinition.AddPanelDetails(GenerateProgressBarDetails((float)capacity));
                        panelDefinition.AddPanelDetail($"{ShortNumber2(capacity * 100.0, panelDefinition.suffixes, 2, 5)}%", false, 0.25f, false);
                    }
                }
                yield return stateComplete;
            }

            bool CargoCapacity(ref double percentage, List<long> indexList)
            {
                selfContainedIdentifier = functionList[6];
                if (!IsStateActive)
                {
                    InitializeState(CargoCapacityState(indexList), selfContainedIdentifier);
                    if (PauseTickRun) return false;
                }
                bool done = RunStateManager;

                if (done)
                    percentage = tempCapacity;

                return done;
            }

            IEnumerator<FunctionState> CargoCapacityState(List<long> indexList)
            {
                double max = 0, current = 0;
                IMyInventory inventory;
                foreach (long index in indexList)
                {
                    if (PauseTickRun) yield return stateActive;

                    if (!parent.IsBlockOk(index))
                        continue;

                    inventory = managedBlocks[index].Input;
                    max += (double)inventory.MaxVolume;
                    current += (double)inventory.CurrentVolume;
                }
                tempCapacity = current / max;
                yield return stateComplete;
            }

            bool ItemPanel(PanelDefinition panelDefinition)
            {
                selfContainedIdentifier = functionList[20];
                if (!IsStateActive)
                {
                    InitializeState(ItemPanelState(panelDefinition), selfContainedIdentifier);
                    if (PauseTickRun) return false;
                }
                return RunStateManager;
            }

            IEnumerator<FunctionState> ItemPanelState(PanelDefinition panelDefinition)
            {
                List<ItemDefinition> allItemList = parent.GetAllItems, foundItemList = new List<ItemDefinition>();
                bool found;
                foreach (ItemDefinition item in allItemList)
                {
                    if (PauseTickRun) yield return stateActive;
                    found = panelDefinition.itemCategories.Contains(item.category);
                    if (!found) panelDefinition.items.ItemCount(out found, item.typeID, item.subtypeID, null);
                    if (found && item.display && item.amount >= panelDefinition.minimumItemAmount && item.amount <= panelDefinition.maximumItemAmount &&
                        (!panelDefinition.belowQuota || item.amount < item.currentQuota))
                        foundItemList.Add(item);
                }
                switch (panelDefinition.panelItemSorting)
                {
                    case PanelItemSorting.Alphabetical:
                        foundItemList = foundItemList.OrderBy(x => x.displayName).ToList();
                        break;
                    case PanelItemSorting.AscendingAmount:
                        foundItemList = foundItemList.OrderBy(x => x.amount).ToList();
                        break;
                    case PanelItemSorting.DescendingAmount:
                        foundItemList = foundItemList.OrderByDescending(x => x.amount).ToList();
                        break;
                    case PanelItemSorting.AscendingPercent:
                        foundItemList = foundItemList.OrderBy(x => x.Percentage).ToList();
                        break;
                    case PanelItemSorting.DescendingPercent:
                        foundItemList = foundItemList.OrderByDescending(x => x.Percentage).ToList();
                        break;
                }
                foreach (ItemDefinition item in foundItemList)
                {
                    if (PauseTickRun) yield return stateActive;
                    panelDefinition.AddPanelItem(item.displayName.PadRight(panelDefinition.nameLength), item.amount, item.currentQuota, item.queuedAssemblyAmount, item.queuedDisassemblyAmount, item.amountDifference);
                }
                yield return stateComplete;
            }

            bool OutputPanel(PanelDefinition panelDefinition)
            {
                selfContainedIdentifier = functionList[7];
                if (!IsStateActive)
                {
                    InitializeState(OutputPanelState(panelDefinition), selfContainedIdentifier);
                    if (PauseTickRun) return false;
                }
                return RunStateManager;
            }

            IEnumerator<FunctionState> OutputPanelState(PanelDefinition panelDefinition)
            {
                List<OutputObject> tempOutputList;

                AddOutputItem(panelDefinition, $"NDS Inventory Manager {scriptVersion}");
                AddOutputItem(panelDefinition, currentMajorFunction);
                AddOutputItem(panelDefinition, $"Runtime:    {ShortMSTime(torchAverage)}");
                AddOutputItem(panelDefinition, $"Blocks:     {ShortNumber2(managedBlocks.Count, panelDefinition.suffixes)}");
                AddOutputItem(panelDefinition, $"Storages:   {ShortNumber2(typedIndexes[setKeyIndexStorage].Count, panelDefinition.suffixes)}");
                AddOutputItem(panelDefinition, $"Assemblers: {ShortNumber2(typedIndexes[setKeyIndexAssemblers].Count, panelDefinition.suffixes)}");
                AddOutputItem(panelDefinition, $"H2/O2 Gens: {ShortNumber2(typedIndexes[setKeyIndexGasGenerators].Count, panelDefinition.suffixes)}");
                AddOutputItem(panelDefinition, $"Refineries: {ShortNumber2(typedIndexes[setKeyIndexRefinery].Count, panelDefinition.suffixes)}");
                AddOutputItem(panelDefinition, $"H2 Tanks:   {ShortNumber2(typedIndexes[setKeyIndexHydrogenTank].Count, panelDefinition.suffixes)}");
                AddOutputItem(panelDefinition, $"O2 Tanks:   {ShortNumber2(typedIndexes[setKeyIndexOxygenTank].Count, panelDefinition.suffixes)}");
                AddOutputItem(panelDefinition, $"Weapons:    {ShortNumber2(typedIndexes[setKeyIndexGun].Count, panelDefinition.suffixes)}");
                AddOutputItem(panelDefinition, $"Reactors:   {ShortNumber2(typedIndexes[setKeyIndexReactor].Count, panelDefinition.suffixes)}");

                if (parent.errorFilter)
                {
                    tempOutputList = new List<OutputObject>(parent.outputErrorList);
                    AddOutputItem(panelDefinition, $"Errors:     {ShortNumber2(parent.currentErrorCount, panelDefinition.suffixes, 0, 6)} of {ShortNumber2(parent.totalErrorCount, panelDefinition.suffixes, 0, 6)}");
                }
                else
                {
                    tempOutputList = new List<OutputObject>(parent.outputList);
                    AddOutputItem(panelDefinition, $"Status:  {ShortNumber2(parent.scriptHealth, null, 3, 6)}%");
                }

                foreach (OutputObject outputObject in tempOutputList)
                {
                    if (PauseTickRun) yield return stateActive;
                    panelDefinition.AddPanelDetail(outputObject.Output, true);
                }

                yield return stateComplete;
            }

            bool StatusPanel(PanelDefinition panelDefinition)
            {
                selfContainedIdentifier = functionList[5];
                if (!IsStateActive)
                {
                    InitializeState(StatusPanelState(panelDefinition), selfContainedIdentifier);
                    if (PauseTickRun) return false;
                }
                return RunStateManager;
            }

            IEnumerator<FunctionState> StatusPanelState(PanelDefinition panelDefinition)
            {
                int assembling, disassembling, idle, disabled;
                assembling = disassembling = idle = disabled = 0;
                assemblyList.Clear();
                disassemblyList.Clear();
                foreach (long index in typedIndexes[setKeyIndexAssemblers])
                {
                    if (PauseTickRun) yield return stateActive;
                    BlockStatus(index, ref assembling, ref disassembling, ref idle, ref disabled, assemblyList, disassemblyList);
                }
                foreach (KeyValuePair<string, int> kvp in assemblyList)
                {
                    if (PauseTickRun) yield return stateActive;
                    panelDefinition.AddPanelDetail($"Assembling x{ShortNumber2(kvp.Value, panelDefinition.suffixes, panelDefinition.decimals, 4, false)} {ShortenName(kvp.Key, panelDefinition.nameLength, true)}", true);
                }
                foreach (KeyValuePair<string, int> kvp in disassemblyList)
                {
                    if (PauseTickRun) yield return stateActive;
                    panelDefinition.AddPanelDetail($"Disassembling x{ShortNumber2(kvp.Value, panelDefinition.suffixes, panelDefinition.decimals, 4, false)} {ShortenName(kvp.Key, panelDefinition.nameLength, true)}", true);
                }
                AddOutputItem(panelDefinition, BlockStatusTitle($"Assemblers x{ShortNumber2(typedIndexes[setKeyIndexAssemblers].Count, panelDefinition.suffixes, panelDefinition.decimals, 4, false)}", disabled).PadRight(panelDefinition.nameLength));
                AddOutputItem(panelDefinition, $" Assembling:    {ShortNumber2(assembling, panelDefinition.suffixes, panelDefinition.decimals, 4)}".PadRight(panelDefinition.nameLength));
                AddOutputItem(panelDefinition, $" Disassembling: {ShortNumber2(disassembling, panelDefinition.suffixes, panelDefinition.decimals, 4)}".PadRight(panelDefinition.nameLength));
                AddOutputItem(panelDefinition, $" Idle:          {ShortNumber2(idle, panelDefinition.suffixes, panelDefinition.decimals, 4)}".PadRight(panelDefinition.nameLength));
                assembling = idle = disabled = 0;
                assemblyList.Clear();
                foreach (long index in typedIndexes[setKeyIndexRefinery])
                {
                    if (PauseTickRun) yield return stateActive;
                    BlockStatus(index, ref assembling, ref disassembling, ref idle, ref disabled, assemblyList, disassemblyList);
                }
                foreach (KeyValuePair<string, int> kvp in assemblyList)
                {
                    if (PauseTickRun) yield return stateActive;
                    panelDefinition.AddPanelDetail($"Refining x{ShortNumber2(kvp.Value, panelDefinition.suffixes, panelDefinition.decimals, 4, false)} {ShortenName(kvp.Key, panelDefinition.nameLength, true)}", true);
                }
                AddOutputItem(panelDefinition, BlockStatusTitle($"Refineries x{ShortNumber2(typedIndexes[setKeyIndexRefinery].Count, panelDefinition.suffixes, panelDefinition.decimals, 4, false)}", disabled).PadRight(panelDefinition.nameLength));
                AddOutputItem(panelDefinition, $" Refining:      {ShortNumber2(assembling, panelDefinition.suffixes, panelDefinition.decimals, 4)}".PadRight(panelDefinition.nameLength));
                AddOutputItem(panelDefinition, $" Idle:          {ShortNumber2(idle, panelDefinition.suffixes, panelDefinition.decimals, 4)}".PadRight(panelDefinition.nameLength));
                assembling = idle = disabled = 0;
                assemblyList.Clear();
                foreach (long index in typedIndexes[setKeyIndexGasGenerators])
                {
                    if (PauseTickRun) yield return stateActive;
                    BlockStatus(index, ref assembling, ref disassembling, ref idle, ref disabled, assemblyList, disassemblyList);
                }
                AddOutputItem(panelDefinition, BlockStatusTitle($"O2/H2 Gens x{ShortNumber2(typedIndexes[setKeyIndexGasGenerators].Count, panelDefinition.suffixes, panelDefinition.decimals, 4, false)}", disabled).PadRight(panelDefinition.nameLength));
                AddOutputItem(panelDefinition, $" Active:        {ShortNumber2(assembling, panelDefinition.suffixes, panelDefinition.decimals, 4)}".PadRight(panelDefinition.nameLength));
                AddOutputItem(panelDefinition, $" Idle:          {ShortNumber2(idle, panelDefinition.suffixes, panelDefinition.decimals, 4)}".PadRight(panelDefinition.nameLength));
                foreach (KeyValuePair<string, int> kvp in assemblyList)
                {
                    if (PauseTickRun) yield return stateActive;
                    panelDefinition.AddPanelDetail($"Processing x{ShortNumber2(kvp.Value, panelDefinition.suffixes, panelDefinition.decimals, 4, false)} {ShortenName(kvp.Key, panelDefinition.nameLength, true)}", true);
                }

                yield return stateComplete;
            }

            #endregion

            public class PregeneratedPanels
            {
                public DateTime nextUpdateTime = Now.AddSeconds(1);

                public List<PanelObject> panelObjects = NewPanelObjectList, spannableObjects = NewPanelObjectList;
            }

            public class PanelDetail
            {
                public string itemName = "", text = "", textureType = "";
                public double itemAmount = 0, itemQuota = 0, value = 0, assemblyAmount = 0, disassemblyAmount = 0, amountDifference = 0;
                public TextAlignment alignment = leftAlignment;
                public float ratio = -1;
                public bool reservedArea = false;
                public Color textureColor = Color.White;

                public PanelDetail Clone()
                {
                    PanelDetail panelDetail = new PanelDetail()
                    {
                        itemName = itemName,
                        text = text,
                        textureType = textureType,
                        itemAmount = itemAmount,
                        itemQuota = itemQuota,
                        value = value,
                        assemblyAmount = assemblyAmount,
                        disassemblyAmount = disassemblyAmount,
                        amountDifference = amountDifference,
                        alignment = alignment,
                        ratio = ratio,
                        reservedArea = reservedArea,
                        textureColor = new Color(textureColor, textureColor.A)
                    };

                    return panelDetail;
                }
            }

            public class PanelObject
            {
                public double sortableValue = 0;
                public string backdropType = "SquareSimple", sortableText = "";
                public bool item = false;
                public List<PanelDetail> panelDetails = new List<PanelDetail>();

                public PanelObject Clone()
                {
                    PanelObject panelObject = new PanelObject
                    {
                        sortableValue = sortableValue,
                        backdropType = backdropType,
                        sortableText = sortableText,
                        item = item
                    };

                    foreach (PanelDetail panelDetail in panelDetails)
                        panelObject.panelDetails.Add(panelDetail.Clone());

                    return panelObject;
                }
            }

            public class PanelDefinition
            {
                public BlockDefinition parent;

                public List<MySprite> spriteList = new List<MySprite>();

                public List<PanelMasterClass.PanelObject>
                    panelObjects = PanelMasterClass.NewPanelObjectList,
                    spannableObjects = PanelMasterClass.NewPanelObjectList;

                public List<string> itemCategories = NewStringList, suffixes;

                public List<SpanKey> spannedPanelList = new List<SpanKey>();

                public int decimals = 2, rows = -1, columns = 1, nameLength = 18, surfaceIndex = 0;

                public double updateDelay = 1, minimumItemAmount = 0, maximumItemAmount = double.MaxValue;

                public bool span = false, cornerPanel = false, belowQuota = false, showProgressBar = true, provider = false;

                public PanelItemSorting panelItemSorting = PanelItemSorting.Alphabetical;

                public PanelType panelType = PanelType.None;

                public DisplayType displayType = DisplayType.Standard;

                public DateTime nextUpdateTime = Now;

                public ItemCollection items = NewCollection;

                public Color textColor = Color.Black, numberColor = Color.Black, backdropColor = Color.GhostWhite;

                public Vector2 size = new Vector2(1, 1), positionOffset = new Vector2(0, 0);

                public string font = "Monospace", settingKey = "", spanKey = "", childSpanKey = "", settingBackup = "";

                public void AddPanelDetails(List<PanelMasterClass.PanelDetail> list)
                {
                    panelObjects[panelObjects.Count - 1].panelDetails.AddRange(list);
                }

                void AddPanelObject(bool spannable = false, bool item = false)
                {
                    if (spannable)
                        spannableObjects.Add(new PanelMasterClass.PanelObject { item = item });
                    else
                        panelObjects.Add(new PanelMasterClass.PanelObject());
                }

                public void AddPanelItem(string name, double amount, double quota, double assemblyAmount, double disassemblyAmount, double amountDifference)
                {
                    AddPanelObject(true, true);
                    spannableObjects[spannableObjects.Count - 1].sortableText = name.Trim();
                    if (panelItemSorting == PanelItemSorting.AscendingPercent || panelItemSorting == PanelItemSorting.DescendingPercent)
                        spannableObjects[spannableObjects.Count - 1].sortableValue = quota > 0 ? amount / quota : 0;
                    spannableObjects[spannableObjects.Count - 1].panelDetails.Add(new PanelMasterClass.PanelDetail { itemAmount = amount, itemName = name, itemQuota = quota, assemblyAmount = assemblyAmount, disassemblyAmount = disassemblyAmount, amountDifference = amountDifference });
                }

                public void AddPanelDetail(string text, bool spannable = false, float ratio = 1f, bool nextObject = true, bool reservedArea = false, TextAlignment alignment = leftAlignment)
                {
                    if (nextObject)
                        AddPanelObject(spannable);
                    if (spannable)
                        spannableObjects[spannableObjects.Count - 1].panelDetails.Add(new PanelMasterClass.PanelDetail { text = text, ratio = ratio, reservedArea = reservedArea, alignment = alignment });
                    else
                        panelObjects[panelObjects.Count - 1].panelDetails.Add(new PanelMasterClass.PanelDetail { text = text, ratio = ratio, reservedArea = reservedArea, alignment = alignment });
                }

                public IMyTextSurface GetSurface()
                {
                    if (!provider)
                        return (IMyTextPanel)parent.block;
                    return ((IMyTextSurfaceProvider)parent.block).GetSurface(surfaceIndex);
                }

                public string DataSource
                {
                    get
                    {
                        if (provider)
                        {
                            IMyTextSurface surface = GetSurface();
                            StringBuilder builder = NewBuilder;
                            surface.ReadText(builder);
                            return builder.ToString();
                        }
                        return parent.DataSource;
                    }
                    set
                    {
                        if (provider)
                        {
                            IMyTextSurface surface = GetSurface();
                            StringBuilder builder = new StringBuilder(value);
                            surface.WriteText(builder);
                        }
                        else
                            parent.DataSource = value;
                    }
                }

                public string EntityFlickerID()
                {
                    string id = parent.block.EntityId.ToString();
                    if (provider)
                        id += $":{surfaceIndex}";
                    return id;
                }
            }


        }

        public class BlueprintSpreadInformation
        {
            public List<long> acceptingIndexList = NewLongList;

            public double totalCount = 0;

            public SortedList<long, double> individualCounts = NewSortedListLongDouble;

            public void AddCount(long entityID, double count)
            {
                if (!individualCounts.ContainsKey(entityID))
                    individualCounts[entityID] = 0;
                individualCounts[entityID] += count;
                totalCount += count;
            }
        }

        public class StateRecord
        {
            public int currentTicks = 0, currentActions = 0, lastTicks = 0, lastActions = 0, runs = 0,
                       minTicks = 0, maxTicks = 0, minActions = 0, maxActions = 0;
            public TimeSpan
                minSpan = TimeSpan.Zero, maxSpan = TimeSpan.Zero, currentSpan = TimeSpan.Zero, lastSpan = TimeSpan.Zero;
            public double averageTime = 0, averageActions = 0;
            public decimal health = 100m;

            public void PostRun(int actions, TimeSpan span, bool errorCaught, bool reportError, bool complete)
            {
                if (errorCaught)
                {
                    currentTicks = currentActions = 0;
                    currentSpan = TimeSpan.Zero;
                    if (reportError)
                        health = Math.Max(0m, health - 5m);
                    return;
                }
                currentActions += actions;
                currentSpan += span;
                currentTicks++;
                if (complete)
                {
                    if (runs == 0)
                    {
                        minTicks = maxTicks = currentTicks;
                        minActions = maxActions = currentActions;
                        minSpan = maxSpan = currentSpan;
                    }
                    else
                    {
                        minTicks = Math.Min(minTicks, currentTicks);
                        maxTicks = Math.Max(maxTicks, currentTicks);
                        minActions = Math.Min(minActions, currentActions);
                        maxActions = Math.Max(maxActions, currentActions);
                        minSpan = currentSpan < minSpan ? currentSpan : minSpan;
                        maxSpan = currentSpan > maxSpan ? currentSpan : maxSpan;
                    }
                    lastActions = currentActions;
                    lastSpan = currentSpan;
                    lastTicks = currentTicks;
                    averageTime = TorchAverage(averageTime, currentSpan.TotalMilliseconds);
                    averageActions = TorchAverage(averageActions, currentActions);



                    currentTicks = currentActions = 0;
                    currentSpan = TimeSpan.Zero;
                    runs++;
                    if (health < 100m)
                    {
                        health += (100m - health) / 10m;
                        if (health >= 99.999m)
                            health = 100m;
                    }
                }
            }
        }

        public class MonitoredAssembler
        {
            MyAssemblerMode mode;
            public SortedList<string, double> productionComparison = new SortedList<string, double>();
            DateTime nextCheck = Now;
            public float currentProgress = 0f;
            public IMyAssembler assembler;
            public bool stalling = false;

            public bool Check(double delay)
            {
                if (Now < nextCheck)
                    return false;

                bool check = assembler.Enabled && !assembler.IsQueueEmpty && assembler.CurrentProgress == currentProgress && assembler.Mode == mode;
                if (!check)
                {
                    productionComparison.Clear();
                    stalling = false;
                    nextCheck = Now.AddSeconds(5);
                }
                else
                    nextCheck = Now.AddSeconds(delay);

                mode = assembler.Mode;
                currentProgress = assembler.CurrentProgress;
                return check;
            }

            public void Reset()
            {
                stalling = false;
                productionComparison.Clear();
                nextCheck = Now.AddSeconds(5);
            }
        }

        public class OutputObject
        {
            public string text = "";
            public int count = 1;
            public string Output { get { return count > 1 ? $"{text} x{count}" : text; } }

        }

        public class SortableObject
        {
            public double amount = 0;
            public string key = "", text = "";
            public int integer = 0;
            public long numberLong = 0;
        }

        public class Blueprint
        {
            public string blueprintID = "", typeID = "", subtypeID = "";
            public double amount = 0, multiplier = 1;

            public Blueprint Clone()
            {
                return new Blueprint { blueprintID = blueprintID, typeID = typeID, subtypeID = subtypeID, amount = amount, multiplier = multiplier };
            }
        }

        public class VariableItemCount
        {
            public double count;
            public bool percentage, manual;

            public VariableItemCount(double countA, bool percentageA = false, bool manualA = false)
            {
                count = countA;
                percentage = percentageA;
                manual = manualA;
            }

            public override string ToString()
            {
                return $"{(percentage ? $"{count * 100.0}%" : $"{count}")}";
            }
        }

        public class ItemCollection
        {
            public static Program parent;

            public SortedList<string, VariableItemCount> itemList = new SortedList<string, VariableItemCount>();

            public int ItemTypeCount { get { return itemList.Count; } }

            public bool IsEmpty
            {
                get
                {
                    for (int i = 0; i < itemList.Count; i++)
                        if (itemList.Values[i].count > 0)
                            return false;

                    return true;
                }
            }

            public void Clear(bool manual = true, bool automatic = true)
            {
                if (manual && automatic) itemList.Clear();
                else
                    for (int i = 0; i < itemList.Count; i += 0)
                    {
                        if ((manual && itemList.Values[i].manual) || (automatic && !itemList.Values[i].manual))
                            itemList.RemoveAt(i);
                        else
                            i++;
                    }
            }

            public void AddCollection(ItemCollection collection, IMyTerminalBlock block)
            {
                string typeID, subtypeID;
                foreach (KeyValuePair<string, VariableItemCount> kvp in collection.itemList)
                {
                    SplitID(kvp.Key, out typeID, out subtypeID);
                    AddItemInternal(kvp.Key, new VariableItemCount(kvp.Value.percentage ? PercentageMax((float)kvp.Value.count, typeID, subtypeID, block) : kvp.Value.count), true);
                }
            }

            public string CountByIndex(int index)
            {
                if (index < ItemTypeCount)
                    return itemList.Values[index].ToString();

                return "0";
            }

            public ItemDefinition ItemByIndex(int index)
            {
                if (index < ItemTypeCount)
                    return GetItem(index);

                return new ItemDefinition();
            }

            ItemDefinition GetItem(int index)
            {
                string typeID, subtypeID;
                double amount;
                SplitID(itemList.Keys[index], out typeID, out subtypeID);
                amount = itemList.Values[index].count;
                return new ItemDefinition { typeID = typeID, subtypeID = subtypeID, amount = amount };
            }

            public string ItemIDByIndex(int index)
            {
                return itemList.Keys[index];
            }

            public void AddItem(string typeID, string subtypeID, VariableItemCount amount, bool append = true)
            {
                string itemID = $"{typeID}/{subtypeID}";
                AddItemInternal(itemID, amount, append);
            }

            public void AddItem(MyInventoryItem item)
            {
                AddItem(item.Type.TypeId, item.Type.SubtypeId, new VariableItemCount((double)item.Amount));
            }

            void AddItemInternal(string itemID, VariableItemCount amount, bool append)
            {
                if (!itemList.ContainsKey(itemID) || !append || amount.manual && !itemList[itemID].manual) itemList[itemID] = amount;
                else if (itemList[itemID].manual == amount.manual && itemList[itemID].percentage == amount.percentage) itemList[itemID].count += amount.count;
            }

            public double ItemCount(MyInventoryItem item, IMyTerminalBlock block = null)
            {
                bool temp;
                return ItemCount(out temp, item, block);
            }

            public double ItemCount(string typeID, string subtypeID, IMyTerminalBlock block)
            {
                bool temp;
                return ItemCount(out temp, typeID, subtypeID, block);
            }

            public double ItemCount(out bool found, MyInventoryItem item, IMyTerminalBlock block)
            {
                return ItemCount(out found, item.Type.TypeId, item.Type.SubtypeId, block);
            }

            public double ItemCount(out bool found, string typeID, string subtypeID, IMyTerminalBlock block)
            {
                string itemID = $"{typeID}/{subtypeID}";
                if (itemList.ContainsKey(itemID))
                {
                    found = true;
                    return itemList[itemID].percentage && block != null ? PercentageMax((float)itemList[itemID].count, typeID, subtypeID, block) : itemList[itemID].count;
                }

                found = false;
                return 0;
            }
        }

        public class LogicComparison
        {
            public string typeID = "", comparison = "", compareAgainst = "";

            public string GetString { get { ItemDefinition def; return $"{(GetDefinition(out def, typeID) ? $"{def.category}:{def.displayName}" : typeID)}{comparison}{compareAgainst}"; } }
        }

        public class PotentialAssembler
        {
            public long index;
            public bool empty, specific;
        }

        public class BlockDefinition
        {
            public IMyTerminalBlock block;

            public MonitoredAssembler monitoredAssembler;

            public BlockDefinition cloneSource = null;

            public SortedList<int, PanelMasterClass.PanelDefinition> panelDefinitionList = new SortedList<int, PanelMasterClass.PanelDefinition>();
            public string settingBackup = "", cloneGroup = "";
            public int headerIndex = 0;

            private BlockSettings innerSettings = new BlockSettings();

            private bool isClone = false;
            public bool isGravelSifter = false;

            public BlockSettings Settings { get { return isClone && cloneSource != null ? cloneSource.Settings : innerSettings; } set { innerSettings = value; } }

            public bool HasInventory { get { return block.InventoryCount > 0; } }

            public IMyInventory Input { get { return block.GetInventory(0); } }

            public bool IsClone { get { return isClone; } }

            public BlockDefinition(IMyTerminalBlock block)
            {
                this.block = block;
            }

            public void SetClone(BlockDefinition definition)
            {
                isClone = definition != null;
                cloneSource = definition;
            }

            public string DataSource
            {
                get { return block.CustomData; }
                set { block.CustomData = value; }
            }
        }

        public class BlockSettings
        {
            public List<LogicComparison> logicComparisons = new List<LogicComparison>();

            public List<string> storageCategories = NewStringList;

            public SortedList<string, bool>
                toggles,
                inventoryToggles = new SortedList<string, bool>
                {
                    { storageKey, false }, { autoConveyorKey, false }, { keepInputKey, false },
                    { removeInputKey, false },  { noSortingKey, false }, { noSpreadKey, false },
                    { noCountKey, false }, { gunOverrideKey, false }
                },
                multiInventoryToggles = new SortedList<string, bool>
                {
                    { keepOutputKey, false }, { removeOutputKey, false },
                },
                assemblerToggles = new SortedList<string, bool>
                {
                    { assemblyOnlyKey, false }, { disassemblyOnlyKey, false }, { uniqueBlueprinteOnlyKey, false}, { noIdleResetKey, false }
                };

            private string
                crossGridKey, includeGridKey,
                excludedKey, excludeGridKey;

            private const string
                storageKey = "storage", autoConveyorKey = "autoconveyor", assemblyOnlyKey = "assemblyonly",
                disassemblyOnlyKey = "disassemblyonly", keepInputKey = "keepinput", keepOutputKey = "keepoutput",
                removeInputKey = "removeinput", removeOutputKey = "removeoutput", noIdleResetKey = "noidlereset",
                noSortingKey = "nosorting", noSpreadKey = "nospreading", noCountKey = "nocounting",
                uniqueBlueprinteOnlyKey = "uniqueblueprintsonly", gunOverrideKey = "gunoverride";

            public bool CrossGrid { get { return toggles[crossGridKey]; } set { toggles[crossGridKey] = value; } }
            public bool IncludeGrid { get { return toggles[includeGridKey]; } set { toggles[includeGridKey] = value; } }
            public bool Excluded { get { return toggles[excludedKey]; } set { toggles[excludedKey] = value; } }
            public bool ExcludedGrid { get { return toggles[excludeGridKey]; } set { toggles[excludeGridKey] = value; } }
            public bool Storage { get { return inventoryToggles[storageKey]; } set { inventoryToggles[storageKey] = value; } }
            public bool AutoConveyor { get { return inventoryToggles[autoConveyorKey]; } set { inventoryToggles[autoConveyorKey] = value; } }
            public bool KeepInput { get { return inventoryToggles[keepInputKey]; } set { inventoryToggles[keepInputKey] = value; } }
            public bool RemoveInput { get { return inventoryToggles[removeInputKey]; } set { inventoryToggles[removeInputKey] = value; } }
            public bool NoSorting { get { return inventoryToggles[noSortingKey]; } set { inventoryToggles[noSortingKey] = value; } }
            public bool NoSpreading { get { return inventoryToggles[noSpreadKey]; } set { inventoryToggles[noSpreadKey] = value; } }
            public bool NoCounting { get { return inventoryToggles[noCountKey]; } set { inventoryToggles[noCountKey] = value; } }
            public bool GunOverride { get { return inventoryToggles[gunOverrideKey]; } set { inventoryToggles[gunOverrideKey] = value; } }
            public bool KeepOutput { get { return multiInventoryToggles[keepOutputKey]; } set { multiInventoryToggles[keepOutputKey] = value; } }
            public bool RemoveOutput { get { return multiInventoryToggles[removeOutputKey]; } set { multiInventoryToggles[removeOutputKey] = value; } }
            public bool AssemblyOnly { get { return assemblerToggles[assemblyOnlyKey]; } set { assemblerToggles[assemblyOnlyKey] = value; } }
            public bool DisassemblyOnly { get { return assemblerToggles[disassemblyOnlyKey]; } set { assemblerToggles[disassemblyOnlyKey] = value; } }
            public bool UniqueBlueprintsOnly { get { return assemblerToggles[uniqueBlueprinteOnlyKey]; } set { assemblerToggles[uniqueBlueprinteOnlyKey] = value; } }
            public bool NoIdleReset { get { return assemblerToggles[noIdleResetKey]; } set { assemblerToggles[noIdleResetKey] = value; } }

            public bool andComparison, manual;

            public ItemCollection limits = NewCollection, loadout = NewCollection;

            public double priority;

            public void Initialize(string crossGridKey, string includeGridKey, string excludedKey, string excludeGridKey)
            {
                this.crossGridKey = crossGridKey;
                this.includeGridKey = includeGridKey;
                this.excludedKey = excludedKey;
                this.excludeGridKey = excludeGridKey;
                toggles = new SortedList<string, bool>
                {
                    { crossGridKey, false }, { includeGridKey, false }, { excludedKey, false }, { excludeGridKey, false }
                };
                inventoryToggles.Keys.ToList().ForEach(b => inventoryToggles[b] = false);
                multiInventoryToggles.Keys.ToList().ForEach(b => multiInventoryToggles[b] = false);
                assemblerToggles.Keys.ToList().ForEach(b => assemblerToggles[b] = false);
                limits.Clear();
                loadout.Clear();
                storageCategories.Clear();
                logicComparisons.Clear();
                andComparison = manual = false;
                priority = 1;
            }

            public void ParseOption(string key)
            {
                if (assemblerToggles.ContainsKey(key)) assemblerToggles[key] = true;
                else if (inventoryToggles.ContainsKey(key)) inventoryToggles[key] = true;
                else if (multiInventoryToggles.ContainsKey(key)) multiInventoryToggles[key] = true;
                else if (toggles.ContainsKey(key)) toggles[key] = true;
            }
        }

        public class ItemCountRecord
        {
            public double count = 0, disassembling = 0;
            public DateTime countTime = Now;
        }

        public class ItemDefinition
        {
            public string typeID = "", subtypeID = "", blueprintID = "", displayName = "", category = "";

            public double amount = 0, queuedAssemblyAmount = 0, queuedDisassemblyAmount = 0,
                          countAmount = 0, countAssemblyAmount = 0, countDisassemblyAmount = 0,
                          quota = 0, amountDifference = 0, blockQuota = 0, quotaMultiplier = 1,
                          assemblyMultiplier = 1, dynamicQuotaCounter = 0,
                          quotaMax = 0, differenceNeeded = 0, currentQuota = 0,
                          currentAssemblyAmount = 0, currentDisassemblyAmount = 0,
                          currentExcessAssembly = 0, currentExcessDisassembly = 0,
                          currentMax = 0, currentNeededAssembly = 0, displayQuota = 0,
                          postAssemblyAmount = 0;

            public float volume = 0f;

            public bool fuel = false, assemble = true,
                disassemble = true, refine = true, display = true,
                disassembleAll = false, gas = false, fractional = true;

            public List<string> oreKeys = NewStringList;

            public List<ItemCountRecord> countRecordList = new List<ItemCountRecord>();

            public DateTime countRecordTime = Now, dynamicQuotaTime = Now;

            public MyItemType ItemType { get { return new MyItemType(typeID, subtypeID); } }

            public string FullID { get { return $"{typeID}/{subtypeID}"; } }

            public double Percentage { get { return currentQuota == 0 ? double.MaxValue : (amount / currentQuota) * 100.0; } }

            public void FinalizeKeys()
            {
                if (oreKeys.Count > 0)
                    oreKeys = oreKeys.Distinct().ToList();
            }

            public void SwitchCount(double maxMultiplier, bool useMultiplier, double negativeThreshold, double positiveThreshold, double multiplierChange, bool increaseWhenLow)
            {
                amount = countAmount;
                countAmount = 0;
                if (Now >= countRecordTime)
                {
                    countRecordList.Add(new ItemCountRecord { count = amount, countTime = Now, disassembling = queuedDisassemblyAmount });
                    countRecordTime = Now.AddSeconds(1.25);
                    if (countRecordList.Count > 12)
                        countRecordList.RemoveRange(0, countRecordList.Count - 12);
                }
                double averageSpanSeconds = Math.Min(20, (Now - countRecordList[0].countTime).TotalSeconds + 0.0001),
                    lastSeconds = (Now - dynamicQuotaTime).TotalSeconds, tempDifference,
                    tempQuota = 0, disassembleCount = Math.Max(countRecordList[0].disassembling, queuedDisassemblyAmount);
                if (countRecordList.Count > 1 && averageSpanSeconds == 20)
                    countRecordList.RemoveAt(0);

                bool allOut = amount < 1.0;

                amountDifference = averageSpanSeconds > 0 ? (amount - countRecordList[0].count) / averageSpanSeconds : 0;

                tempDifference = amountDifference;

                tempQuota += (quota > 0 ? quota : 0) + (blockQuota > 0 ? blockQuota : 0);

                if (useMultiplier && tempQuota > 0)
                {
                    if (lastSeconds >= 1)
                    {
                        if (increaseWhenLow && allOut)
                            tempDifference -= tempQuota * 0.001;

                        if (!allOut && tempDifference == 0)
                            tempDifference += tempQuota * 0.1;

                        if (disassembleCount >= 1)
                            tempDifference = tempQuota * 0.005;

                        if (amount >= tempQuota)
                            tempDifference = tempQuota * 0.15;

                        dynamicQuotaCounter += ((tempDifference / tempQuota) * averageSpanSeconds) * Math.Min(2.5, lastSeconds);
                        if (dynamicQuotaCounter <= -negativeThreshold)
                        {
                            quotaMultiplier += multiplierChange;
                            if (quotaMultiplier > maxMultiplier)
                                quotaMultiplier = maxMultiplier;

                            dynamicQuotaCounter = 0;
                        }
                        else if (dynamicQuotaCounter >= positiveThreshold)
                        {
                            quotaMultiplier -= multiplierChange;
                            if (quotaMultiplier < 1)
                                quotaMultiplier = 1;

                            dynamicQuotaCounter = 0;
                        }
                        dynamicQuotaTime = Now;
                    }
                }
                else
                {
                    quotaMultiplier = 1;
                    dynamicQuotaTime = Now;
                    dynamicQuotaCounter = positiveThreshold * 0.95;
                }
                SetCurrentQuota();
            }

            public void SwitchAssemblyCount()
            {
                queuedAssemblyAmount = Math.Floor(countAssemblyAmount);
                queuedDisassemblyAmount = Math.Floor(countDisassemblyAmount);
                countAssemblyAmount = countDisassemblyAmount = 0;
            }

            public void AddAmount(MyFixedPoint amount)
            {
                countAmount += (double)amount;
            }

            public void AddAssemblyAmount(MyFixedPoint amount, bool current)
            {
                if (current)
                    queuedAssemblyAmount += (double)amount;
                else
                    countAssemblyAmount += (double)amount;
            }

            public void AddDisassemblyAmount(MyFixedPoint amount, bool current)
            {
                if (current)
                    queuedDisassemblyAmount += (double)amount;
                else
                    countDisassemblyAmount += (double)amount;
            }

            public void SetDifferenceNeeded(double excessAmount)
            {
                //Set Variables
                currentNeededAssembly = differenceNeeded = currentExcessAssembly = currentExcessDisassembly = 0;
                currentAssemblyAmount = queuedAssemblyAmount * assemblyMultiplier;
                currentDisassemblyAmount = queuedDisassemblyAmount;
                postAssemblyAmount = amount + (currentAssemblyAmount) - (currentDisassemblyAmount);
                SetCurrentQuota();
                if (TextHasLength(blueprintID) && blueprintID != nothingType)
                {
                    currentMax =
                        currentQuota < 0 ? 0 :
                        quotaMax > currentQuota ? quotaMax :
                        currentQuota > 0 ? Math.Floor(currentQuota * (1.0 + excessAmount)) : double.MaxValue;

                    if (!disassembleAll)
                    {
                        if (currentAssemblyAmount > 0 && amount + currentAssemblyAmount > currentMax)
                            currentExcessAssembly = (amount + currentAssemblyAmount) - currentMax;

                        if (currentDisassemblyAmount > 0 && amount - currentDisassemblyAmount < currentQuota)
                            currentExcessDisassembly = currentQuota - (amount - currentDisassemblyAmount);
                    }
                    else
                    {
                        currentExcessAssembly = queuedAssemblyAmount;
                        if (currentDisassemblyAmount > 0 && currentDisassemblyAmount > amount)
                            currentExcessDisassembly = currentDisassemblyAmount - amount;
                    }
                    if (currentExcessAssembly > 0)
                        currentExcessAssembly = Math.Floor(currentExcessAssembly / assemblyMultiplier);

                    if (currentDisassemblyAmount > 0 && currentDisassemblyAmount < assemblyMultiplier)
                        currentExcessDisassembly = currentDisassemblyAmount;

                    if (!assemble)
                        currentExcessAssembly = 0;

                    if (!disassemble)
                        currentExcessDisassembly = 0;
                    //Logic
                    if (disassembleAll)
                    {
                        if (postAssemblyAmount > 0)
                            differenceNeeded = -postAssemblyAmount;
                    }
                    else
                    {
                        if (currentQuota == 0)
                            differenceNeeded = 0;
                        else
                        {
                            if (postAssemblyAmount < currentQuota)
                                differenceNeeded = currentQuota - postAssemblyAmount;
                            else if (postAssemblyAmount > currentMax)
                                differenceNeeded = currentMax - postAssemblyAmount;
                        }
                    }

                    if (differenceNeeded > 0)
                        differenceNeeded = Math.Ceiling(differenceNeeded / assemblyMultiplier);
                    else if (differenceNeeded < 0)
                        differenceNeeded = Math.Abs(differenceNeeded) < assemblyMultiplier ? 0 : Math.Floor(Math.Abs(differenceNeeded) / assemblyMultiplier) * -assemblyMultiplier;


                    if (differenceNeeded > 0)
                        currentNeededAssembly = differenceNeeded;

                    if (differenceNeeded > 0 && (currentDisassemblyAmount > 0 || !assemble))
                        differenceNeeded = 0;

                    if (differenceNeeded < 0 && (currentAssemblyAmount > 0 || !disassemble))
                        differenceNeeded = 0;
                }
            }

            public void SetCurrentQuota()
            {
                currentQuota =
                    (quota > 0 ? quota : 0) +
                    (blockQuota > 0 ? blockQuota : 0);

                disassembleAll = currentQuota == 0 && quota < 0;
                if (!disassembleAll && currentQuota > 0)
                    currentQuota *= quotaMultiplier;

                currentQuota = Math.Floor(currentQuota);
                displayQuota = currentQuota;
                if (disassembleAll)
                    displayQuota = -1;
            }
        }

        public class SpanKey
        {
            public long index;
            public int surfaceIndex = 0;
        }


        #endregion
    }
}