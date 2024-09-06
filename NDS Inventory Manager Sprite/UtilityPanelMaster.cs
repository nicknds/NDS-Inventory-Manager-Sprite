using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        public class PanelMasterClass
        {
            #region Variables

            public static Program parent;

            Color defPanelForegroundColor = new Color(0.7019608f, 0.9294118f, 1f, 1f);

            public string presetPanelOptions;

            StringBuilder measurementBuilder = NewBuilder;

            SortedList<string, PregeneratedPanels> generatedPanels = new SortedList<string, PregeneratedPanels>();

            double tempCapacity = 0;

            FunctionIdentifier selfContainedIdentifier;

            // string selfContainedIdentifier;

            SortedList<string, int> assemblyList = new SortedList<string, int>(),
                                    disassemblyList = new SortedList<string, int>();

            BlockDefinition tempBlockOptionDefinition;
            PanelDefinition tempPanelDefinition;
            int tempProcessPanelOptionSurfaceIndex;
            List<PanelObject> tempPanelObjects;
            List<long> tempIndexList = NewLongList;
            public DateTime updateTime = Now;
            float tempColumn;
            bool tempSpan;

            #endregion


            #region Short Links

            bool PauseTickRun { get { return parent.PauseTickRun; } }
            bool IsStateRunning { get { return parent.StateRunning(selfContainedIdentifier); } }
            bool RunStateManager { get { return parent.StateManager(selfContainedIdentifier, true, true); } }
            public static List<PanelObject> NewPanelObjectList { get { return new List<PanelObject>(); } }
            Dictionary<long, BlockDefinition> managedBlocks { get { return parent.managedBlocks; } }
            SortedList<string, List<long>> typedIndexes { get { return parent.typedIndexes; } }
            SortedList<string, List<string>> settingsListsStrings { get { return parent.settingsListsStrings; } }

            #endregion


            #region Methods

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

            void SetPanelDefinition(BlockDefinition managedBlock, int surfaceIndex)
            {
                managedBlock.panelDefinitionList[surfaceIndex] = new PanelMasterClass.PanelDefinition { surfaceIndex = surfaceIndex, provider = !(managedBlock.block is IMyTextPanel), suffixes = settingsListsStrings[setKeyDefaultSuffixes], parent = managedBlock };
                managedBlock.panelDefinitionList[surfaceIndex].items.trackAmounts = false;
            }

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
                    IMyTextSurface surface = panelDefinition.Surface;
                    if (surface.ContentType != ContentType.SCRIPT)
                        surface.ContentType = ContentType.SCRIPT;

                    if (surface.Script != nothingType)
                        surface.Script = nothingType;

                    if (surface.ScriptForegroundColor == defPanelForegroundColor)
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
                    formTitle += $" -({ShortNumber2(disabled, settingsListsStrings[setKeyDefaultSuffixes])})";
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


            public bool ProcessPanelOptions(BlockDefinition managedBlock, int surfaceIndex = 0)
            {
                selfContainedIdentifier = FunctionIdentifier.Process_Panel_Options;

                if (!IsStateRunning)
                {
                    tempBlockOptionDefinition = managedBlock;
                    tempProcessPanelOptionSurfaceIndex = surfaceIndex;
                }

                return RunStateManager;
            }

            public IEnumerator<FunctionState> ProcessPanelOptionState()
            {
                PanelDefinition panelDefinition;
                string dataSource, key, data, blockDefinition;
                StringBuilder keyBuilder = NewBuilder;
                string[] dataLines, dataOptions;
                yield return stateContinue;

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
                        if (parent.GetKeyBool(setKeyAutoTagBlocks))
                        {
                            panelDefinition.DataSource = presetPanelOptions;
                            tempBlockOptionDefinition.block.CustomName = tempBlockOptionDefinition.block.CustomName.Replace(panelTag, panelTag.ToUpper());
                        }
                    }
                    else if (!StringsMatch(dataSource, panelDefinition.settingBackup))
                    {
                        panelDefinition.itemSearchString = "";
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
                                            if (parent.IsCategory(dataOptions[x]))
                                            {
                                                panelDefinition.itemCategories.Add(dataOptions[x]);
                                                keyBuilder.Append(dataOptions[x]);
                                            }
                                        }
                                        break;
                                    case "items":
                                        panelDefinition.itemSearchString = $"{(panelDefinition.itemSearchString.Length > 0 ? $"{panelDefinition.itemSearchString}|" : "")}{data}";
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
                            keyBuilder.Append(panelDefinition.itemSearchString);
                        }
                        if (panelDefinition.panelType != PanelType.None)
                        {
                            IMyTextSurface surface = panelDefinition.Surface;
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
                    if (updateTime < parent.itemAddedOrChanged ||
                        (panelDefinition.items.ItemTypeCount == 0 && panelDefinition.itemSearchString.Length > 0))
                    {
                        panelDefinition.items.Clear();
                        while (!parent.GetTags(panelDefinition.items, panelDefinition.itemSearchString))
                            yield return stateActive;
                    }
                    if (panelDefinition.items.ItemTypeCount > 0)
                        keyBuilder.Append(panelDefinition.items.ToString());

                    panelDefinition.settingBackup = dataSource;

                    yield return stateContinue;
                }
            }

            bool PopulateSprites()
            {
                selfContainedIdentifier = FunctionIdentifier.Main_Sprites;

                return RunStateManager;
            }

            public IEnumerator<FunctionState> PopulateSpriteState()
            {
                int column;
                yield return stateContinue;

                while (true)
                {
                    column = 0;
                    tempPanelDefinition.spriteList.Clear();

                    if (tempPanelDefinition.panelObjects.Count > 0)
                    {
                        while (!PopulateSpriteList(tempPanelDefinition.panelObjects, column, false)) yield return stateActive;
                        column = 1;
                    }
                    if (tempPanelDefinition.spannableObjects.Count > 0)
                        while (!PopulateSpriteList(tempPanelDefinition.spannableObjects, column, true)) yield return stateActive;

                    yield return stateContinue;
                }
            }

            bool PopulateSpriteList(List<PanelObject> panelObjects, float column, bool span)
            {
                selfContainedIdentifier = FunctionIdentifier.Generating_Sprites;

                if (!IsStateRunning)
                {
                    tempPanelObjects = panelObjects;
                    tempColumn = column;
                    tempSpan = span;
                }

                return RunStateManager;
            }

            public IEnumerator<FunctionState> PopulateSpriteListState()
            {
                List<PanelObject> leftoverObjects = NewPanelObjectList;
                IMyTextSurface surface;
                int numberPadding;
                float percent;
                Vector2 maxLocation, currentLocation,
                        objectSize, currentOffset,
                        detailSize, subOffset;
                yield return stateContinue;

                while (true)
                {
                    leftoverObjects.Clear();
                    surface = tempPanelDefinition.Surface;
                    numberPadding = tempPanelDefinition.decimals + 4;
                    if (tempPanelDefinition.decimals > 0)
                        numberPadding++;
                    maxLocation = NewVector2(tempPanelDefinition.columns, tempPanelDefinition.rows >= 0 ? tempPanelDefinition.rows : Math.Max(1, tempPanelObjects.Count));

                    if (!tempSpan)
                        maxLocation.Y = tempPanelObjects.Count;

                    objectSize = NewVector2(tempPanelDefinition.size.X / maxLocation.X, tempPanelDefinition.size.Y / maxLocation.Y);
                    currentLocation = NewVector2(tempColumn);

                    //Cycle objects
                    foreach (PanelObject panelObject in tempPanelObjects)
                    {
                        if (PauseTickRun) yield return stateActive;
                        if (!tempSpan || currentLocation.Y < maxLocation.Y) // If objects are fixed (not spannable) and the row is within bounds
                        {
                            // Set current position
                            currentOffset = NewVector2(currentLocation.X * objectSize.X, currentLocation.Y * objectSize.Y) + tempPanelDefinition.positionOffset;

                            //Generate single backdrop, if any
                            if (TextHasLength(panelObject.backdropType) && tempPanelDefinition.backdropColor.A > 0)
                                tempPanelDefinition.spriteList.Add(GenerateTextureSprite(panelObject.backdropType, tempPanelDefinition.backdropColor, currentOffset, objectSize));

                            //Cycle details
                            foreach (PanelDetail panelDetail in panelObject.panelDetails)
                            {
                                if (PauseTickRun) yield return stateActive;
                                if (panelObject.item) // Process item
                                {
                                    percent = (float)(panelDetail.itemAmount / panelDetail.itemQuota);
                                    if (panelDetail.itemQuota <= 0f)
                                        percent = 1f;
                                    switch (tempPanelDefinition.displayType)
                                    {
                                        case DisplayType.CompactAmount:
                                            //Name @ 75%
                                            detailSize = NewVector2(objectSize.X * 0.75f, objectSize.Y);
                                            tempPanelDefinition.spriteList.Add(GenerateTextSprite(panelDetail.itemName, tempPanelDefinition.textColor, currentOffset, detailSize, panelDetail.alignment, tempPanelDefinition.font, surface));
                                            subOffset = NewVector2(detailSize.X);
                                            //Number @ 25%
                                            detailSize = NewVector2(objectSize.X * 0.25f, objectSize.Y);
                                            tempPanelDefinition.spriteList.Add(GenerateTextSprite(ShortNumber2(panelDetail.itemAmount, tempPanelDefinition.suffixes, tempPanelDefinition.decimals, numberPadding), tempPanelDefinition.numberColor, currentOffset + subOffset, detailSize, panelDetail.alignment, tempPanelDefinition.font, surface));
                                            break;
                                        case DisplayType.CompactPercent:
                                            //Name @ 75%
                                            detailSize = NewVector2(objectSize.X * 0.75f, objectSize.Y);
                                            tempPanelDefinition.spriteList.Add(GenerateTextSprite(panelDetail.itemName, tempPanelDefinition.textColor, currentOffset, detailSize, panelDetail.alignment, tempPanelDefinition.font, surface));
                                            subOffset = NewVector2(detailSize.X);
                                            //Percentage @ 25%
                                            if (tempPanelDefinition.showProgressBar)
                                            {
                                                detailSize = NewVector2(objectSize.X * 0.25f, objectSize.Y);
                                                tempPanelDefinition.spriteList.AddRange(GenerateProgressBarSprites(percent, currentOffset + subOffset, detailSize));
                                            }
                                            break;
                                        case DisplayType.Detailed:
                                            //Name @ 60% x 50%
                                            detailSize = NewVector2(objectSize.X * 0.6f, objectSize.Y / 2f);
                                            tempPanelDefinition.spriteList.Add(GenerateTextSprite(panelDetail.itemName, tempPanelDefinition.textColor, currentOffset, detailSize, panelDetail.alignment, tempPanelDefinition.font, surface));
                                            subOffset = NewVector2(detailSize.X);
                                            //Count @ 20% x 50%
                                            detailSize.X = objectSize.X * 0.2f;
                                            tempPanelDefinition.spriteList.Add(GenerateTextSprite($"{ShortNumber2(panelDetail.itemAmount, tempPanelDefinition.suffixes, tempPanelDefinition.decimals, numberPadding)}/", tempPanelDefinition.numberColor, currentOffset + subOffset, detailSize, panelDetail.alignment, tempPanelDefinition.font, surface));
                                            subOffset += NewVector2(detailSize.X);
                                            //Percent @ 20% x 50%
                                            if (tempPanelDefinition.showProgressBar)
                                                tempPanelDefinition.spriteList.AddRange(GenerateProgressBarSprites(percent, currentOffset + subOffset, detailSize));
                                            //Quota @ 20% x 50%
                                            tempPanelDefinition.spriteList.Add(GenerateTextSprite(ShortNumber2(panelDetail.itemQuota, tempPanelDefinition.suffixes, tempPanelDefinition.decimals, numberPadding), tempPanelDefinition.numberColor, currentOffset + subOffset, detailSize, panelDetail.alignment, tempPanelDefinition.font, surface));
                                            subOffset = NewVector2(0, detailSize.Y);
                                            measurementBuilder.Clear();
                                            if (panelDetail.assemblyAmount > 0)
                                                measurementBuilder.Append($"Assembling: {ShortNumber2(panelDetail.assemblyAmount, tempPanelDefinition.suffixes, tempPanelDefinition.decimals, numberPadding)}");
                                            if (panelDetail.disassemblyAmount > 0)
                                            {
                                                if (BuilderHasLength(measurementBuilder))
                                                    measurementBuilder.Append(", ");
                                                measurementBuilder.Append($"Disassembling: {ShortNumber2(panelDetail.disassemblyAmount, tempPanelDefinition.suffixes, tempPanelDefinition.decimals, numberPadding)}");
                                            }
                                            //Assembly status @ 75% x 50%
                                            detailSize.X = objectSize.X * 0.75f;
                                            tempPanelDefinition.spriteList.Add(GenerateTextSprite(measurementBuilder.ToString(), tempPanelDefinition.textColor, currentOffset + subOffset, detailSize, panelDetail.alignment, tempPanelDefinition.font, surface));
                                            subOffset += NewVector2(detailSize.X);
                                            //Rate @ 25% x 50%
                                            detailSize.X = objectSize.X * 0.25f;
                                            tempPanelDefinition.spriteList.Add(GenerateTextSprite($"Rate: {ShortNumber2(panelDetail.amountDifference, tempPanelDefinition.suffixes, tempPanelDefinition.decimals, numberPadding)}", tempPanelDefinition.textColor, currentOffset + subOffset, detailSize, panelDetail.alignment, tempPanelDefinition.font, surface));
                                            break;
                                        case DisplayType.Standard:
                                            //Name @ 60%
                                            detailSize = NewVector2(objectSize.X * 0.6f, objectSize.Y);
                                            tempPanelDefinition.spriteList.Add(GenerateTextSprite(panelDetail.itemName, tempPanelDefinition.textColor, currentOffset, detailSize, panelDetail.alignment, tempPanelDefinition.font, surface));
                                            subOffset = NewVector2(detailSize.X);
                                            //Count @ 20%
                                            detailSize = NewVector2(objectSize.X * 0.2f, objectSize.Y);
                                            tempPanelDefinition.spriteList.Add(GenerateTextSprite($"{ShortNumber2(panelDetail.itemAmount, tempPanelDefinition.suffixes, tempPanelDefinition.decimals, numberPadding)}/", tempPanelDefinition.numberColor, currentOffset + subOffset, detailSize, panelDetail.alignment, tempPanelDefinition.font, surface));
                                            subOffset += NewVector2(detailSize.X);
                                            //Percent @ 20%
                                            tempPanelDefinition.spriteList.AddRange(GenerateProgressBarSprites(percent, currentOffset + subOffset, detailSize));
                                            //Quota @ 20%
                                            tempPanelDefinition.spriteList.Add(GenerateTextSprite(ShortNumber2(panelDetail.itemQuota, tempPanelDefinition.suffixes, tempPanelDefinition.decimals, numberPadding), tempPanelDefinition.numberColor, currentOffset + subOffset, detailSize, panelDetail.alignment, tempPanelDefinition.font, surface));
                                            break;
                                    }
                                }
                                else // Process everything else
                                {
                                    detailSize = NewVector2(objectSize.X * panelDetail.ratio, objectSize.Y);

                                    tempPanelDefinition.spriteList.Add
                                        (
                                            TextHasLength(panelDetail.textureType) ? GenerateTextureSprite(panelDetail.textureType, panelDetail.textureColor, currentOffset, detailSize) :
                                            TextHasLength(panelDetail.text) ? GenerateTextSprite(panelDetail.text, tempPanelDefinition.textColor, currentOffset, detailSize, panelDetail.alignment, tempPanelDefinition.font, surface) :
                                            GenerateTextSprite(ShortNumber2(panelDetail.value, tempPanelDefinition.suffixes, tempPanelDefinition.decimals, numberPadding), tempPanelDefinition.textColor, currentOffset, detailSize, panelDetail.alignment, tempPanelDefinition.font, surface)
                                        );

                                    if (panelDetail.reservedArea)
                                        currentOffset += NewVector2(detailSize.X);
                                }
                            }
                            currentLocation.Y += 1f;
                        }
                        else if (tempPanelDefinition.span)
                            leftoverObjects.Add(panelObject.Clone());
                        else
                            break;
                    }

                    foreach (SpanKey spanKey in tempPanelDefinition.spannedPanelList)
                    {
                        if (PauseTickRun) yield return stateActive;
                        managedBlocks[spanKey.index].panelDefinitionList[spanKey.surfaceIndex].spannableObjects.Clear();
                        if (leftoverObjects.Count > 0)
                            ClonePanelObjects(managedBlocks[spanKey.index].panelDefinitionList[spanKey.surfaceIndex].spannableObjects, leftoverObjects);
                    }

                    yield return stateContinue;
                }
            }

            public bool TotalPanelV2(PanelDefinition panelDefinition)
            {
                selfContainedIdentifier = FunctionIdentifier.Main_Panel;

                if (!IsStateRunning)
                    tempPanelDefinition = panelDefinition;

                return RunStateManager;
            }

            public IEnumerator<FunctionState> TotalPanelStateV2()
            {
                yield return stateContinue;

                while (true)
                {
                    if (Now >= tempPanelDefinition.nextUpdateTime)
                    {
                        string panelOptionString = tempPanelDefinition.settingKey;
                        tempPanelDefinition.panelObjects.Clear();
                        if (tempPanelDefinition.panelType != PanelType.Span)
                        {
                            bool cachePanel = TextHasLength(panelOptionString);
                            tempPanelDefinition.spannableObjects.Clear();
                            if (cachePanel && generatedPanels.ContainsKey(panelOptionString) && generatedPanels[panelOptionString].nextUpdateTime > Now)
                            {
                                ClonePanelObjects(tempPanelDefinition.panelObjects, generatedPanels[panelOptionString].panelObjects);
                                ClonePanelObjects(tempPanelDefinition.spannableObjects, generatedPanels[panelOptionString].spannableObjects);
                            }
                            else
                            {
                                switch (tempPanelDefinition.panelType)
                                {
                                    case PanelType.Cargo:
                                        while (!CargoPanel()) yield return stateActive;
                                        break;
                                    case PanelType.Item:
                                        while (!ItemPanel()) yield return stateActive;
                                        break;
                                    case PanelType.Output:
                                        while (!OutputPanel()) yield return stateActive;
                                        break;
                                    case PanelType.Status:
                                        while (!StatusPanel()) yield return stateActive;
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
                                    ClonePanelObjects(generatedPanels[panelOptionString].panelObjects, tempPanelDefinition.panelObjects);
                                    ClonePanelObjects(generatedPanels[panelOptionString].spannableObjects, tempPanelDefinition.spannableObjects);
                                    generatedPanels[panelOptionString].nextUpdateTime = Now.AddSeconds(tempPanelDefinition.updateDelay);
                                }
                            }
                        }
                        tempPanelDefinition.nextUpdateTime = Now.AddSeconds(tempPanelDefinition.updateDelay);

                        while (!PopulateSprites()) yield return stateActive;
                        if (PauseTickRun) yield return stateActive;
                        MySpriteDrawFrame frame = tempPanelDefinition.Surface.DrawFrame();
                        frame.AddRange(tempPanelDefinition.spriteList);
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
                    yield return stateContinue;
                }
            }

            bool CargoPanel()
            {
                selfContainedIdentifier = FunctionIdentifier.Cargo_Panel;

                return RunStateManager;
            }

            public IEnumerator<FunctionState> CargoPanelState()
            {
                double capacity;
                yield return stateContinue;

                while (true)
                {
                    capacity = 0;
                    if (tempPanelDefinition.itemCategories.Contains("all"))
                    {
                        while (!CargoCapacity(ref capacity, typedIndexes[setKeyIndexStorage])) yield return stateActive;
                        tempPanelDefinition.AddPanelDetail("Total:".PadRight(tempPanelDefinition.nameLength), false, 0.75f, true, true);
                        if (tempPanelDefinition.showProgressBar)
                            tempPanelDefinition.AddPanelDetails(GenerateProgressBarDetails((float)capacity));
                        tempPanelDefinition.AddPanelDetail($"{ShortNumber2(capacity * 100.0, tempPanelDefinition.suffixes, 2, 5)}%", false, 0.25f, false);
                    }
                    foreach (string category in tempPanelDefinition.itemCategories)
                    {
                        if (PauseTickRun) yield return stateActive;
                        if (category != "all" && parent.indexesStorageLists.ContainsKey(category))
                        {
                            while (!CargoCapacity(ref capacity, parent.indexesStorageLists[category])) yield return stateActive;
                            tempPanelDefinition.AddPanelDetail($"{Formatted(category)}:".PadRight(tempPanelDefinition.nameLength), false, 0.75f, true, true);
                            if (tempPanelDefinition.showProgressBar)
                                tempPanelDefinition.AddPanelDetails(GenerateProgressBarDetails((float)capacity));
                            tempPanelDefinition.AddPanelDetail($"{ShortNumber2(capacity * 100.0, tempPanelDefinition.suffixes, 2, 5)}%", false, 0.25f, false);
                        }
                    }
                    yield return stateContinue;
                }
            }

            bool CargoCapacity(ref double percentage, List<long> indexList)
            {
                selfContainedIdentifier = FunctionIdentifier.Measuring_Capacities;

                if (!IsStateRunning)
                {
                    tempIndexList.Clear();
                    tempIndexList.AddRange(indexList);
                }

                if (RunStateManager)
                {
                    percentage = tempCapacity;
                    return true;
                }
                return false;
            }

            public IEnumerator<FunctionState> CargoCapacityState()
            {
                double max, current;
                IMyInventory inventory;
                yield return stateContinue;

                while (true)
                {
                    max = current = 0;

                    foreach (long index in tempIndexList)
                    {
                        if (PauseTickRun) yield return stateActive;

                        if (!parent.IsBlockOk(index))
                            continue;

                        inventory = managedBlocks[index].Input;
                        max += (double)inventory.MaxVolume;
                        current += (double)inventory.CurrentVolume;
                    }
                    tempCapacity = current / max;

                    yield return stateContinue;
                }
            }

            bool ItemPanel()
            {
                selfContainedIdentifier = FunctionIdentifier.Item_Panel;

                return RunStateManager;
            }

            public IEnumerator<FunctionState> ItemPanelState()
            {
                List<ItemDefinition> allItemList = new List<ItemDefinition>(), foundItemList = new List<ItemDefinition>();
                yield return stateContinue;

                while (true)
                {
                    allItemList.Clear();
                    foundItemList.Clear();
                    allItemList.AddRange(parent.GetAllItems);
                    bool found;
                    foreach (ItemDefinition item in allItemList)
                    {
                        if (PauseTickRun) yield return stateActive;
                        found = tempPanelDefinition.itemCategories.Contains(item.category);
                        if (!found) tempPanelDefinition.items.ItemCount(out found, item.typeID, item.subtypeID, null);
                        if (found && item.display && item.amount >= tempPanelDefinition.minimumItemAmount && item.amount <= tempPanelDefinition.maximumItemAmount &&
                            (!tempPanelDefinition.belowQuota || item.amount < item.currentQuota))
                            foundItemList.Add(item);
                    }
                    switch (tempPanelDefinition.panelItemSorting)
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
                        tempPanelDefinition.AddPanelItem(item.displayName.PadRight(tempPanelDefinition.nameLength), item.amount, item.currentQuota, item.queuedAssemblyAmount, item.queuedDisassemblyAmount, item.amountDifference);
                    }
                    yield return stateContinue;
                }
            }

            bool OutputPanel()
            {
                selfContainedIdentifier = FunctionIdentifier.Output_Panel;

                return RunStateManager;
            }

            public IEnumerator<FunctionState> OutputPanelState()
            {
                List<OutputObject> tempOutputList = new List<OutputObject>();
                yield return stateContinue;

                while (true)
                {
                    AddOutputItem(tempPanelDefinition, $"NDS Inventory Manager {scriptVersion}");
                    AddOutputItem(tempPanelDefinition, currentMajorFunction.Replace("_", " "));
                    AddOutputItem(tempPanelDefinition, $"Runtime:    {parent.ShortMSTime(torchAverage)}");
                    AddOutputItem(tempPanelDefinition, $"Blocks:     {ShortNumber2(managedBlocks.Count, tempPanelDefinition.suffixes)}");
                    AddOutputItem(tempPanelDefinition, $"Storages:   {ShortNumber2(typedIndexes[setKeyIndexStorage].Count, tempPanelDefinition.suffixes)}");
                    AddOutputItem(tempPanelDefinition, $"Assemblers: {ShortNumber2(typedIndexes[setKeyIndexAssemblers].Count, tempPanelDefinition.suffixes)}");
                    AddOutputItem(tempPanelDefinition, $"H2/O2 Gens: {ShortNumber2(typedIndexes[setKeyIndexGasGenerators].Count, tempPanelDefinition.suffixes)}");
                    AddOutputItem(tempPanelDefinition, $"Refineries: {ShortNumber2(typedIndexes[setKeyIndexRefinery].Count, tempPanelDefinition.suffixes)}");
                    AddOutputItem(tempPanelDefinition, $"H2 Tanks:   {ShortNumber2(typedIndexes[setKeyIndexHydrogenTank].Count, tempPanelDefinition.suffixes)}");
                    AddOutputItem(tempPanelDefinition, $"O2 Tanks:   {ShortNumber2(typedIndexes[setKeyIndexOxygenTank].Count, tempPanelDefinition.suffixes)}");
                    AddOutputItem(tempPanelDefinition, $"Weapons:    {ShortNumber2(typedIndexes[setKeyIndexGun].Count, tempPanelDefinition.suffixes)}");
                    AddOutputItem(tempPanelDefinition, $"Reactors:   {ShortNumber2(typedIndexes[setKeyIndexReactor].Count, tempPanelDefinition.suffixes)}");

                    tempOutputList.Clear();
                    tempOutputList.AddRange(parent.errorFilter ? parent.outputErrorList : parent.outputList);

                    AddOutputItem(tempPanelDefinition, parent.errorFilter ? $"Errors:     {ShortNumber2(parent.currentErrorCount, tempPanelDefinition.suffixes, 0, 6)} of {ShortNumber2(parent.totalErrorCount, tempPanelDefinition.suffixes, 0, 6)}" : $"Status:  {ShortNumber2(parent.scriptHealth, null, 3, 6)}%");

                    foreach (OutputObject outputObject in tempOutputList)
                    {
                        if (PauseTickRun) yield return stateActive;
                        tempPanelDefinition.AddPanelDetail(outputObject.Output, true);
                    }

                    yield return stateContinue;
                }
            }

            bool StatusPanel()
            {
                selfContainedIdentifier = FunctionIdentifier.Status_Panel;

                return RunStateManager;
            }

            public IEnumerator<FunctionState> StatusPanelState()
            {
                int assembling, disassembling, idle, disabled;
                List<long> tempIndices = new List<long>();
                yield return stateContinue;

                while (true)
                {
                    assembling = disassembling = idle = disabled = 0;
                    assemblyList.Clear();
                    disassemblyList.Clear();
                    tempIndices.AddRange(typedIndexes[setKeyIndexAssemblers]);
                    foreach (long index in tempIndices)
                    {
                        if (PauseTickRun) yield return stateActive;
                        BlockStatus(index, ref assembling, ref disassembling, ref idle, ref disabled, assemblyList, disassemblyList);
                    }
                    tempIndices.Clear();
                    foreach (KeyValuePair<string, int> kvp in assemblyList)
                    {
                        if (PauseTickRun) yield return stateActive;
                        tempPanelDefinition.AddPanelDetail($"Assembling x{ShortNumber2(kvp.Value, tempPanelDefinition.suffixes, tempPanelDefinition.decimals, 4, false)} {ShortenName(kvp.Key, tempPanelDefinition.nameLength, true)}", true);
                    }
                    foreach (KeyValuePair<string, int> kvp in disassemblyList)
                    {
                        if (PauseTickRun) yield return stateActive;
                        tempPanelDefinition.AddPanelDetail($"Disassembling x{ShortNumber2(kvp.Value, tempPanelDefinition.suffixes, tempPanelDefinition.decimals, 4, false)} {ShortenName(kvp.Key, tempPanelDefinition.nameLength, true)}", true);
                    }
                    AddOutputItem(tempPanelDefinition, BlockStatusTitle($"Assemblers x{ShortNumber2(typedIndexes[setKeyIndexAssemblers].Count, tempPanelDefinition.suffixes, tempPanelDefinition.decimals, 4, false)}", disabled).PadRight(tempPanelDefinition.nameLength));
                    AddOutputItem(tempPanelDefinition, $" Assembling:    {ShortNumber2(assembling, tempPanelDefinition.suffixes, tempPanelDefinition.decimals, 4)}".PadRight(tempPanelDefinition.nameLength));
                    AddOutputItem(tempPanelDefinition, $" Disassembling: {ShortNumber2(disassembling, tempPanelDefinition.suffixes, tempPanelDefinition.decimals, 4)}".PadRight(tempPanelDefinition.nameLength));
                    AddOutputItem(tempPanelDefinition, $" Idle:          {ShortNumber2(idle, tempPanelDefinition.suffixes, tempPanelDefinition.decimals, 4)}".PadRight(tempPanelDefinition.nameLength));
                    assembling = idle = disabled = 0;
                    assemblyList.Clear();
                    tempIndices.AddRange(typedIndexes[setKeyIndexRefinery]);
                    foreach (long index in tempIndices)
                    {
                        if (PauseTickRun) yield return stateActive;
                        BlockStatus(index, ref assembling, ref disassembling, ref idle, ref disabled, assemblyList, disassemblyList);
                    }
                    tempIndices.Clear();
                    foreach (KeyValuePair<string, int> kvp in assemblyList)
                    {
                        if (PauseTickRun) yield return stateActive;
                        tempPanelDefinition.AddPanelDetail($"Refining x{ShortNumber2(kvp.Value, tempPanelDefinition.suffixes, tempPanelDefinition.decimals, 4, false)} {ShortenName(kvp.Key, tempPanelDefinition.nameLength, true)}", true);
                    }
                    AddOutputItem(tempPanelDefinition, BlockStatusTitle($"Refineries x{ShortNumber2(typedIndexes[setKeyIndexRefinery].Count, tempPanelDefinition.suffixes, tempPanelDefinition.decimals, 4, false)}", disabled).PadRight(tempPanelDefinition.nameLength));
                    AddOutputItem(tempPanelDefinition, $" Refining:      {ShortNumber2(assembling, tempPanelDefinition.suffixes, tempPanelDefinition.decimals, 4)}".PadRight(tempPanelDefinition.nameLength));
                    AddOutputItem(tempPanelDefinition, $" Idle:          {ShortNumber2(idle, tempPanelDefinition.suffixes, tempPanelDefinition.decimals, 4)}".PadRight(tempPanelDefinition.nameLength));
                    assembling = idle = disabled = 0;
                    assemblyList.Clear();
                    tempIndices.AddRange(typedIndexes[setKeyIndexGasGenerators]);
                    foreach (long index in tempIndices)
                    {
                        if (PauseTickRun) yield return stateActive;
                        BlockStatus(index, ref assembling, ref disassembling, ref idle, ref disabled, assemblyList, disassemblyList);
                    }
                    tempIndices.Clear();
                    AddOutputItem(tempPanelDefinition, BlockStatusTitle($"O2/H2 Gens x{ShortNumber2(typedIndexes[setKeyIndexGasGenerators].Count, tempPanelDefinition.suffixes, tempPanelDefinition.decimals, 4, false)}", disabled).PadRight(tempPanelDefinition.nameLength));
                    AddOutputItem(tempPanelDefinition, $" Active:        {ShortNumber2(assembling, tempPanelDefinition.suffixes, tempPanelDefinition.decimals, 4)}".PadRight(tempPanelDefinition.nameLength));
                    AddOutputItem(tempPanelDefinition, $" Idle:          {ShortNumber2(idle, tempPanelDefinition.suffixes, tempPanelDefinition.decimals, 4)}".PadRight(tempPanelDefinition.nameLength));
                    foreach (KeyValuePair<string, int> kvp in assemblyList)
                    {
                        if (PauseTickRun) yield return stateActive;
                        tempPanelDefinition.AddPanelDetail($"Processing x{ShortNumber2(kvp.Value, tempPanelDefinition.suffixes, tempPanelDefinition.decimals, 4, false)} {ShortenName(kvp.Key, tempPanelDefinition.nameLength, true)}", true);
                    }

                    yield return stateContinue;
                }
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

                public DateTime nextUpdateTime = Now, updateTime = Now;

                public ItemCollection items = NewCollection;

                public Color textColor = Color.Black, numberColor = Color.Black, backdropColor = Color.GhostWhite;

                public Vector2 size = new Vector2(1, 1), positionOffset = new Vector2(0, 0);

                public string font = "Monospace", settingKey = "", spanKey = "", childSpanKey = "", settingBackup = "", itemSearchString = "";

                public IMyTextSurface Surface { get { return provider ? ((IMyTextSurfaceProvider)parent.block).GetSurface(surfaceIndex) : (IMyTextPanel)parent.block; } }

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

                public string DataSource
                {
                    get
                    {
                        if (provider)
                        {
                            IMyTextSurface surface = Surface;
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
                            IMyTextSurface surface = Surface;
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
    }
}
