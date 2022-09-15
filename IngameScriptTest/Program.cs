using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        /*
        // This file contains your actual script.
        //
        // You can either keep all your code here, or you can create separate
        // code files to make your program easier to navigate while coding.
        //
        // In order to add a new utility class, right-click on your project, 
        // select 'New' then 'Add Item...'. Now find the 'Space Engineers'
        // category under 'Visual C# Items' on the left hand side, and select
        // 'Utility Class' in the main area. Name it in the box below, and
        // press OK. This utility class will be merged in with your code when
        // deploying your final script.
        //
        // You can also simply create a new utility class manually, you don't
        // have to use the template if you don't want to. Just do so the first
        // time to see what a utility class looks like.
        // 
        // Go to:
        // https://github.com/malware-dev/MDK-SE/wiki/Quick-Introduction-to-Space-Engineers-Ingame-Scripts
        //
        // to learn more about ingame scripts.
        */


        //--------Start--------

        List<DisplayLayout> displayLayouts = new List<DisplayLayout>()
        {
            //new DisplayLayout(new string[]
            //{
            //    "disp_Items_0_0",
            //    "disp_Items_1_0",
            //    "disp_Items_0_1",
            //}, 12)
            //{
            //    showItems = true,
            //    groupItemsByCargos = false, //нужно ли отображать содержимое каждого инвентаря отдельно. (сейчас работает только false)                
            //    types = new ResourceType[] { ResourceType.Component, ResourceType.UnDefined },
            //},

            //new DisplayLayout(new string[]
            //{
            //    "disp_Items_0_2",
            //    "disp_Items_1_2",
            //}, 12)
            //{
            //    showItems = true,
            //    groupItemsByCargos = false, //нужно ли отображать содержимое каждого инвентаря отдельно. (сейчас работает только false)                
            //    types = new ResourceType[] { ResourceType.Ore, ResourceType.Ingot },
            //},

            //new DisplayLayout(new string[]
            //{
            //    "disp_energy"
            //})
            //{
            //    showEnergy = true,
            //    detailedEnergy = true,
            //},

            //new DisplayLayout(new string[]
            //{
            //    "disp_gas"
            //})
            //{
            //    showHydrogen = true,
            //    detailedHydrogen = true,
            //}


            new DisplayLayout(new string[]
            {
                "d_01",
                "d_11",
                "d_21",
            }, 12)
            {
                showItems = true,
                groupItemsByCargos = false,
            },

            new DisplayLayout(new string[]
            {
                "d_10",
                "d_20",                
            }, 12)
            {
                showItems = true,
                groupItemsByCargos = false,
                alarmOnly = true,
            },
        };


        public void Main(string argument, UpdateType updateSource)
        {
            displayLayouts.ForEach(x => x.displayRows = new List<DisplayRow>());
            displayLayouts.ForEach(x => x.fillPanels(GridTerminalSystem));
            DisplayItems();
            DisplayEnergy();
            DisplayHydrogen();
            displayLayouts.ForEach(x => x.DisplayAllRows());
        }
        void DisplayItems()
        {
            List<IMyCargoContainer> cargos = new List<IMyCargoContainer>();
            GridTerminalSystem.GetBlocksOfType(cargos);
            Echo("Найдено контейнеров: " + cargos.Count);

            List<DisplayItemRow> list = new List<DisplayItemRow>();
            foreach (IMyCargoContainer cargo in cargos)
            {
                List<MyInventoryItem> items = new List<MyInventoryItem>();
                cargo.GetInventory().GetItems(items);
                foreach (MyInventoryItem item in items)
                {
                    Resource resource = null;
                    Resource LocalizedResource = LocalizedResources.FirstOrDefault(x => x.originalName == item.Type.SubtypeId);
                    if (LocalizedResource != null)
                    {
                        resource = LocalizedResource;
                    }
                    else
                    {
                        resource = new Resource()
                        {
                            isLocalized = false,
                            originalName = item.Type.SubtypeId,
                            unit = "ед.",
                            lowLimit = -1,
                            scale = 0.000001f,
                            type = ResourceType.UnDefined
                        };
                    }
                    long value = item.Amount.RawValue;
                    DisplayItemRow row = resource.ToDisplayRow(value);
                    row.cargoId = cargo.GetId();
                    row.cargoName = cargo.DisplayName;
                    list.Add(row);
                }
            }
            Echo("Найдено рессурсов (до группировки): " + list.Count);

            foreach (DisplayLayout displayLayout in displayLayouts)
            {
                List<DisplayItemRow> resultList = new List<DisplayItemRow>();
                resultList.AddRange(list);
                if (displayLayout.alarmOnly)
                {
                    foreach (Resource res in LocalizedResources)
                    {
                        if(!resultList.Any(x => x.resource.originalName == res.originalName))
                        {
                            resultList.Add(new DisplayItemRow(res, 0));
                        }
                    }
                }

                if (displayLayout.showItems)
                    displayLayout.AddIt(resultList);
            }
        }

        void DisplayEnergy()
        {
            List<IMyBatteryBlock> batteries = new List<IMyBatteryBlock>();
            GridTerminalSystem.GetBlocksOfType(batteries);
            Echo("Найдено батарей: " + batteries.Count);

            float maxCharge = 0;
            float currentCharge = 0;
            List<DisplayEnergyRow> detailedRows = new List<DisplayEnergyRow>();
            foreach (IMyBatteryBlock battery in batteries)
            {
                currentCharge = currentCharge + battery.CurrentStoredPower;
                maxCharge = maxCharge + battery.MaxStoredPower;
                detailedRows.Add(new DisplayEnergyRow() { sourceName = battery.DisplayNameText, currentValue = battery.CurrentStoredPower, maxValue = battery.MaxStoredPower });
            }

            List<DisplayEnergyRow> rows = new List<DisplayEnergyRow>();
            if (maxCharge != 0)
            {
                rows.Add(new DisplayEnergyRow() { sourceName = "Всего", currentValue = currentCharge, maxValue = maxCharge, isGeneral = true });
                rows.AddRange(detailedRows);
            }
            else
            {
                Echo("А нету заряда...");
            }

            foreach (DisplayLayout displayLayout in displayLayouts)
            {
                if (displayLayout.showEnergy)
                    displayLayout.AddIt(rows);
            }
        }

        void DisplayHydrogen()
        {
            List<IMyGasTank> gasTanks = new List<IMyGasTank>();
            GridTerminalSystem.GetBlocksOfType(gasTanks);
            Echo("Найдено баков: " + gasTanks.Count);

            double generalMaxCapacity = 0;
            double generalCurrentCapacity = 0;
            List<DisplayHydrogenRow> detailedRows = new List<DisplayHydrogenRow>();
            foreach (IMyGasTank gasTank in gasTanks)
            {
                generalMaxCapacity = generalMaxCapacity + gasTank.Capacity;
                generalCurrentCapacity = generalCurrentCapacity + (gasTank.Capacity * gasTank.FilledRatio);
                detailedRows.Add(new DisplayHydrogenRow() { sourceName = gasTank.DisplayNameText, currentRatio = gasTank.FilledRatio, maxValue = gasTank.Capacity });
            }

            List<DisplayHydrogenRow> rows = new List<DisplayHydrogenRow>();
            if (generalMaxCapacity != 0)
            {
                rows.Add(new DisplayHydrogenRow() { sourceName = "Всего", currentRatio = generalCurrentCapacity / generalMaxCapacity, maxValue = (float)generalMaxCapacity, isGeneral = true });
                rows.AddRange(detailedRows);
            }
            else
            {
                Echo("А нету водороду...");
            }

            foreach (DisplayLayout displayLayout in displayLayouts)
            {
                if (displayLayout.showHydrogen)
                    displayLayout.AddIt(rows);
            }
        }


        List<Resource> LocalizedResources => GetLocalizedResources();
        List<Resource> GetLocalizedResources()
        {
            return new List<Resource>()
            {
                CreateLocalizedResources("Computer",            "Компьютер",            ResourceType.Component),
                CreateLocalizedResources("SmallTube",           "Малая трубка",         ResourceType.Component, 50),
                CreateLocalizedResources("InteriorPlate",       "Внутренняя пластина",  ResourceType.Component, 100),
                CreateLocalizedResources("Medical",             "Мед. компоненты",      ResourceType.Component),
                CreateLocalizedResources("NATO_25x184mm",       "БП 25x184 мм NATO",    ResourceType.Component, -1),
                CreateLocalizedResources("Thrust",              "Деталь ион. ускорителя",ResourceType.Component),
                CreateLocalizedResources("Construction",        "Строит. компоненты",   ResourceType.Component, 100),
                CreateLocalizedResources("LargeTube",           "Большая трубка",   ResourceType.Component),
                CreateLocalizedResources("BulletproofGlass",    "Брон. стекло",     ResourceType.Component),
                CreateLocalizedResources("Girder",              "Балка",            ResourceType.Component),
                CreateLocalizedResources("Explosives",          "Взрывчатка",       ResourceType.Component),
                CreateLocalizedResources("Display",             "Дисплей",          ResourceType.Component),
                CreateLocalizedResources("Detector",            "Комп-ты детектора",ResourceType.Component),
                CreateLocalizedResources("SolarCell",           "Солнечная ячейка", ResourceType.Component),
                CreateLocalizedResources("PowerCell",           "Энергоячейка",     ResourceType.Component),
                CreateLocalizedResources("RadioCommunication",  "Радио компоненты", ResourceType.Component),
                CreateLocalizedResources("MetalGrid",           "Комп-ты решетки",  ResourceType.Component),
                CreateLocalizedResources("Motor",               "Мотор",            ResourceType.Component, 50),
                CreateLocalizedResources("Missile200mm",        "Ракета 200 мм",    ResourceType.Component, -1),
                CreateLocalizedResources("Reactor",             "Компоненты реактора",  ResourceType.Component),
                CreateLocalizedResources("SteelPlate",          "Метал. пластина",      ResourceType.Component, 200),
                CreateLocalizedResources("NATO_5p56x45mm",      "БП 5.56x45 мм NATO",   ResourceType.Component, -1),
                CreateLocalizedResources("Medkit",              "Аптечка",              ResourceType.Component, -1),
                CreateLocalizedResources("SpaceCredit",         "Кредит",               ResourceType.Component, -1),
                CreateLocalizedResources("Nickel",              "Никель",   ResourceType.Ingot),
                CreateLocalizedResources("Iron",                "Железо",   ResourceType.Ingot),
                CreateLocalizedResources("Cobalt",              "Кобальт",  ResourceType.Ingot),
                CreateLocalizedResources("Silicon",             "Кремний",  ResourceType.Ingot),
                CreateLocalizedResources("Silver",              "Серебро",  ResourceType.Ingot),
                CreateLocalizedResources("Gold",                "Золото",   ResourceType.Ingot),
                CreateLocalizedResources("Magnesium",           "Магний",   ResourceType.Ingot),
                CreateLocalizedResources("Scrap",               "Металолом",ResourceType.Ingot, -1),
                CreateLocalizedResources("Ice",                 "Лед",      ResourceType.Ore, 5),
                CreateLocalizedResources("Stone",               "Камень",   ResourceType.Ore),
                CreateLocalizedResources("HydrogenBottle",      "Водородный балон", ResourceType.Instument),
                CreateLocalizedResources("AutomaticRifleItem",  "Автомат. винтовка",ResourceType.Instument),
                CreateLocalizedResources("HandDrill2Item",      "Дрель т2",         ResourceType.Instument),
                CreateLocalizedResources("WelderItem",          "Сварщик",          ResourceType.Instument),
                CreateLocalizedResources("HandDrillItem",       "Дрель",            ResourceType.Instument),
                CreateLocalizedResources("AngleGrinderItem",    "Резак",            ResourceType.Instument),
                CreateLocalizedResources("AngleGrinder2Item",   "Резак т2",         ResourceType.Instument),
                CreateLocalizedResources("Welder2Item",         "Сварщик т2",       ResourceType.Instument),



            };
        }
        Resource CreateLocalizedResources(string origName, string locName, ResourceType type, float limit = -10, string unit = "ед.")
        {
            Resource ret = new Resource()
            {
                isLocalized = true,
                localizedName = locName,
                originalName = origName,
                type = type
            };
            switch (type)
            {
                case ResourceType.Ore:
                    ret.scale = 0.000000001f;
                    ret.lowLimit = 20;
                    ret.unit = "кг.";
                    break;
                case ResourceType.UnDefined:
                    ret.scale = 0.000001f;
                    ret.lowLimit = -1;
                    ret.unit = "ед.";
                    break;
                case ResourceType.Component:
                    ret.scale = 0.000001f;
                    ret.lowLimit = 10;
                    ret.unit = "шт.";
                    break;
                case ResourceType.Ingot:
                    ret.scale = 0.000001f;
                    ret.lowLimit = 50;
                    ret.unit = "слит.";
                    break;
                case ResourceType.Instument:
                    ret.scale = 0.000001f;
                    ret.lowLimit = -1;
                    ret.unit = "шт.";
                    break;
                default:
                    ret.scale = 0.000001f;
                    ret.lowLimit = -1;
                    ret.unit = "ед.";
                    break;
            }
            if (limit != -10) ret.lowLimit = limit;
            return ret;
        }


        static string dots(int count)
        {
            if (count <= 0) return "";
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < count; i++)
            {
                sb.Append('.');
            }
            return sb.ToString();
        }

        class DisplayLayout
        {
            public bool showItems { get; set; } = false;
            public bool groupItemsByCargos { get; set; } = false;
            public bool alarmOnly { get; set; } = false;
            public bool showHydrogen { get; set; } = false;
            public bool detailedHydrogen { get; set; } = false;
            public bool showEnergy { get; set; } = false;
            public bool detailedEnergy { get; set; } = false;
            public string[] panelsNames { get; set; }
            public IMyTextPanel[] panels { get; private set; }
            public ResourceType[] types { get; set; }
            public int onPanelRowsCount { get; private set; }
            public int rowLenth { get; private set; }
            public List<DisplayRow> displayRows { get; set; } = new List<DisplayRow>();
            public DisplayLayout(string[] textPanelsNames, int onPanelRowsCount = 0, int rowLenth = 30)
            {
                int maxRowLenth = 20;
                if (rowLenth < maxRowLenth) throw new Exception($"Слишком маленькая максимальная длинна строки! Установите значение больше {maxRowLenth}");
                this.onPanelRowsCount = onPanelRowsCount;
                this.rowLenth = rowLenth;
                this.panelsNames = textPanelsNames;
            }
            public void fillPanels(IMyGridTerminalSystem gridTerminalSystem)
            {
                List<IMyTextPanel> panelList = new List<IMyTextPanel>();
                foreach (string name in panelsNames)
                {
                    IMyTextPanel panel = gridTerminalSystem.GetBlockWithName(name) as IMyTextPanel;
                    panel.WriteText("");
                    panelList.Add(panel);
                }
                panels = panelList.ToArray();
            }
            public void AddIt(List<DisplayItemRow> rows)
            {
                if (types != null && types.Length > 0)
                    rows = rows.Where(x => types.Contains(x.resource.type)).ToList();

                rows = rows.OrderBy(x => x.resource.name).ToList();

                List<DisplayRow> finalRows = new List<DisplayRow>();
                finalRows.Add(new DisplayHeaderRow() { title = "Запасы рессурсов:", alingCenter = true });
                if (groupItemsByCargos)
                {
                    //throw new NotImplementedException("itemsGrouping установите в false, я недоделал");
                }
                else
                {
                    finalRows.AddRange(rows
                        .GroupBy(x => x.resource.name)
                        .Select(g => new DisplayItemRow(g.First().resource, g.Sum(y => y.value)))
                        .Where(x => (x.alert && alarmOnly) || !alarmOnly).ToList());
                }

                displayRows.AddRange(finalRows);
            }
            public void AddIt(List<DisplayEnergyRow> rows)
            {
                List<DisplayRow> finalRows = new List<DisplayRow>();
                finalRows.Add(new DisplayHeaderRow() { title = "Заряд батарей:" });
                if (detailedEnergy)
                {
                    finalRows.AddRange(rows);
                    if (rows.Count == 0)
                        finalRows.Add(new DisplayHeaderRow() { title = "Заряда нет, идите нахуй" });
                }
                else
                {
                    DisplayEnergyRow generalRow = rows.FirstOrDefault(x => x.isGeneral);
                    if (generalRow != null)
                        finalRows.Add(generalRow);
                    else
                        finalRows.Add(new DisplayHeaderRow() { title = "Заряда нет, идите нахуй" });
                }
                displayRows.AddRange(finalRows);
            }
            public void AddIt(List<DisplayHydrogenRow> rows)
            {
                List<DisplayRow> finalRows = new List<DisplayRow>();
                finalRows.Add(new DisplayHeaderRow() { title = "Объем цистерн:" });
                if (detailedHydrogen)
                {
                    finalRows.AddRange(rows);
                    if (rows.Count == 0)
                        finalRows.Add(new DisplayHeaderRow() { title = "Водороду нету, идите нахуй" });
                }
                else
                {
                    DisplayHydrogenRow generalRow = rows.FirstOrDefault(x => x.isGeneral);
                    if (generalRow != null)
                        finalRows.Add(generalRow);
                    else
                        finalRows.Add(new DisplayHeaderRow() { title = "Водороду нету, идите нахуй" });
                }
                displayRows.AddRange(finalRows);
            }
            public void DisplayAllRows(List<DisplayRow> rows = null)
            {
                if (rows == null) rows = displayRows;
                int rowIndex = 0;
                foreach (IMyTextPanel panel in panels) panel.WriteText("Нечего отображать...");
                foreach (IMyTextPanel panel in panels)
                {
                    StringBuilder sb = new StringBuilder();
                    int rowsCount = onPanelRowsCount == 0 ? rows.Count : onPanelRowsCount;
                    for (int i = 0; i < rowsCount; i++)
                    {
                        if (rows.Count > rowIndex)
                            sb.AppendLine(rows[rowIndex].ToStringAsSimpleRow());
                        else
                            break;

                        if (rows.Count - 1 > rowIndex && rows[rowIndex + 1] is DisplayHeaderRow)
                        {
                            sb.AppendLine();
                            i++;
                            rowsCount++;
                        }

                        rowIndex++;
                    }
                    string result = sb.ToString();
                    panel.WriteText(result);
                    if (rows.Count <= rowIndex) return;
                }
            }
        }
        abstract class DisplayRow
        {
            public abstract string ToStringAsSimpleRow();
            public abstract string ToStringAsTableRow(DisplayLayout displayLayout);
        }
        class DisplayItemRow : DisplayRow
        {
            public DisplayItemRow(Resource resource, float value)
            {
                this.resource = resource;
                this.value = value;
            }
            public Resource resource { get; set; }
            public float value { get; set; }
            public float scaledValue => value * resource.scale;
            public bool alert => scaledValue <= resource.lowLimit && resource.lowLimit >= 0;
            public float cargoId { get; set; }
            public string cargoName { get; set; }

            public override string ToStringAsSimpleRow()
            {
                string name = resource.name;
                string unit = resource.unit;
                string val;
                switch (resource.type)
                {
                    case ResourceType.Ingot:
                    case ResourceType.Ore:
                        val = Math.Round(scaledValue, 2).ToString();
                        if (val.Length > 5) val = val.Substring(0, val.Length - 3);
                        break;
                    default:
                        val = scaledValue.ToString();
                        break;
                }
                string result = $"{name}:\t {val} {unit}";
                return result;
            }
            public override string ToStringAsTableRow(DisplayLayout displayLayout)
            {
                string name = resource.name;
                string unit = resource.unit;
                string val;
                switch (resource.type)
                {
                    case ResourceType.Ingot:
                    case ResourceType.Ore:
                        val = Math.Round(scaledValue, 2).ToString();
                        if (val.Length > 5) val = val.Substring(0, val.Length - 3);
                        break;
                    default:
                        val = scaledValue.ToString();
                        break;
                }
                if (val.Length > 5) val = "> 10к";
                string result = $"{name} {val} {unit}";
                if (result.Length > displayLayout.rowLenth)
                {
                    name = name.Substring(0, result.Length - displayLayout.rowLenth - 3) + "...";
                    result = $"{name} {val} {unit}";
                }
                else
                {
                    result = $"{name}{dots(displayLayout.rowLenth - result.Length)} {val} {unit}";
                }
                return result;
            }

        }
        class DisplayHeaderRow : DisplayRow
        {
            public string title { get; set; }
            public bool alingCenter { get; set; }

            public override string ToStringAsSimpleRow()
            {
                return title;
            }

            public override string ToStringAsTableRow(DisplayLayout displayLayout)
            {
                if (alingCenter) return $"{dots((displayLayout.rowLenth - title.Length) / 2 - 1)} {title} {dots((displayLayout.rowLenth - title.Length) / 2 - 1)}";
                else return $"{title} {dots(displayLayout.rowLenth - title.Length + 1)}";
            }
        }
        class DisplayEnergyRow : DisplayRow
        {
            public string sourceName { get; set; }
            public float currentValue { get; set; }
            public float maxValue { get; set; }
            public bool isGeneral { get; set; } = false;

            public override string ToStringAsSimpleRow()
            {
                string current = Math.Round(currentValue, 2).ToString("F2");
                string max = Math.Round(maxValue, 2).ToString("F2");
                string percents = (Math.Round(currentValue / maxValue * 100, 2)).ToString();
                string choBlya = currentValue > maxValue ? "Че бля?!" : "";

                return $"{sourceName}: {current}/{max} MWh -- {percents}% {choBlya}";
            }

            public override string ToStringAsTableRow(DisplayLayout displayLayout)
            {
                return ToStringAsSimpleRow();
            }
        }
        class DisplayHydrogenRow : DisplayRow
        {
            public string sourceName { get; set; }
            public double currentRatio { get; set; }
            public float maxValue { get; set; }
            public bool isGeneral { get; set; } = false;

            public override string ToStringAsSimpleRow()
            {
                string current = (maxValue * (float)currentRatio).ToString("F20");
                if (current.Contains('.')) current = current.Substring(0, current.IndexOf('.'));

                string max = maxValue.ToString("F20");
                if (max.Contains('.')) max = max.Substring(0, max.IndexOf('.'));

                string percents = (Math.Round(currentRatio * 100, 2)).ToString();

                return $"{sourceName}: {current}/{max} л -- {percents}%";
            }

            public override string ToStringAsTableRow(DisplayLayout displayLayout)
            {
                return ToStringAsSimpleRow();
            }
        }

        class Resource
        {
            public ResourceType type { get; set; } = ResourceType.UnDefined;
            public string originalName { get; set; }
            public bool isLocalized { get; set; } = false;
            public string localizedName { private get; set; }
            public string name => isLocalized ? localizedName : originalName;
            public string unit { get; set; }
            public float scale { get; set; }
            public float lowLimit { get; set; }

            public DisplayItemRow ToDisplayRow(float value) => new DisplayItemRow(this, value);
        }

        enum ResourceType
        {
            UnDefined = 0,
            Ore = 1,
            Ingot = 2,
            Component = 3,
            Instument = 4
        }
        //---------End---------
    }
}
