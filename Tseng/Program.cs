using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Timers;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Shojy.FF7.Elena;
using Shojy.FF7.Elena.Equipment;
using Tseng.Models;
using Accessory = Tseng.Models.Accessory;
using Timer = System.Timers.Timer;
using Weapon = Tseng.Models.Weapon;

namespace Tseng
{
    public class Program
    {
        private static Process FF7 { get; set; }
        private static NativeMemoryReader MemoryReader { get; set; }
        private static Timer Timer { get; set; }
        private static FF7SaveMap SaveMap { get; set; }
        public static List<Materia> MateriaDatabase { get; set; } = new List<Materia>();
        public static List<Weapon> WeaponDatabase { get; set; } = new List<Weapon>();
        public static List<Armlet> ArmletDatabase { get; set; } = new List<Armlet>();
        public static List<Accessory> AccessoryDatabase { get; set; } = new List<Accessory>();
        public static GameStatus PartyStatus { get; set; }
        public static void Main(string[] args)
        {
            LocateGameProcess();

            LoadDataFromKernel();
            StartMonitoringGame();
            StartServer(args);

            Console.ReadLine();
        }

        private static void LoadDataFromKernel()
        {
            if (FF7?.MainModule == null)
            {
                throw new Exception("FF7 Process MUST be discovered before data can be loaded");
            }
            var ff7Exe = FF7.MainModule?.FileName;
            var ff7Folder = Path.GetDirectoryName(ff7Exe);
            var kernelLocation = Path.Combine(ff7Folder, "data", "lang-en", "kernel");

            var elena = new KernelReader(Path.Combine(kernelLocation, "KERNEL.BIN"));
            elena.MergeKernel2Data(Path.Combine(kernelLocation, "kernel2.bin"));

            // Map Elena's data into local data dbs.
            foreach (var materia in elena.MateriaData.Materias)
            {
                var m = new Materia { Id = materia.Index, Name = materia.Name };
                switch (materia.MateriaType)
                {
                    case Shojy.FF7.Elena.Materias.MateriaType.Command:
                        m.Type = MateriaType.Command;
                        break;
                    case Shojy.FF7.Elena.Materias.MateriaType.Magic:
                        m.Type = MateriaType.Magic;
                        break;
                    case Shojy.FF7.Elena.Materias.MateriaType.Summon:
                        m.Type = MateriaType.Summon;
                        break;
                    case Shojy.FF7.Elena.Materias.MateriaType.Support:
                        m.Type = MateriaType.Support;
                        break;
                    case Shojy.FF7.Elena.Materias.MateriaType.Independent:
                        m.Type = MateriaType.Independent;
                        break;
                    default:
                        m.Type = MateriaType.None;
                        break;
                }
                MateriaDatabase.Add(m);
            }

            MateriaDatabase.Add(new Materia {Id = 255, Name = "Empty Slot", Type = MateriaType.None});

            foreach (var wpn in elena.WeaponData.Weapons)
            {
                var w = new Weapon
                {
                    Name = wpn.Name,
                    Id = wpn.Index,
                    Growth = (int) wpn.GrowthRate,
                    LinkedSlots = wpn.MateriaSlots.Count(slot =>
                        slot == MateriaSlot.EmptyLeftLinkedSlot
                        || slot == MateriaSlot.EmptyRightLinkedSlot
                        || slot == MateriaSlot.NormalLeftLinkedSlot
                        || slot == MateriaSlot.NormalRightLinkedSlot),
                    SingleSlots = wpn.MateriaSlots.Count(slot =>
                        slot == MateriaSlot.EmptyUnlinkedSlot
                        || slot == MateriaSlot.NormalUnlinkedSlot)
                };
                // Work out what weapon icon to use
                if ((wpn.EquipableBy & (EquipableBy.Cloud | EquipableBy.YoungCloud)) == wpn.EquipableBy)
                {
                    w.Type = WeaponType.Sword;
                }
                else if (wpn.EquipableBy == (wpn.EquipableBy & EquipableBy.Barret))
                {
                    w.Type = WeaponType.Arm;
                }
                else if (wpn.EquipableBy == (wpn.EquipableBy & EquipableBy.Tifa))
                {
                    w.Type = WeaponType.Glove;
                }
                else if (wpn.EquipableBy == (wpn.EquipableBy & EquipableBy.Aeris))
                {
                    w.Type = WeaponType.Staff;
                }
                else if (wpn.EquipableBy == (wpn.EquipableBy & EquipableBy.RedXIII))
                {
                    w.Type = WeaponType.Hairpin;
                }
                else if (wpn.EquipableBy == (wpn.EquipableBy & EquipableBy.Yuffie))
                {
                    w.Type = WeaponType.Shuriken;
                }
                else if (wpn.EquipableBy == (wpn.EquipableBy & EquipableBy.CaitSith))
                {
                    w.Type = WeaponType.Megaphone;
                }
                else if (wpn.EquipableBy == (wpn.EquipableBy & EquipableBy.Vincent))
                {
                    w.Type = WeaponType.Gun;
                }
                else if (wpn.EquipableBy == (wpn.EquipableBy & EquipableBy.Cid))
                {
                    w.Type = WeaponType.Pole;
                }
                else
                {
                    w.Type = WeaponType.Other;
                }
                WeaponDatabase.Add(w);
            }

            foreach (var arm in elena.ArmorData.Armors)
            {
                ArmletDatabase.Add(new Armlet
                {
                    Id = arm.Index,
                    Name = arm.Name,
                    Growth = (int)arm.GrowthRate,
                    LinkedSlots = arm.MateriaSlots.Count(slot =>
                        slot == MateriaSlot.EmptyLeftLinkedSlot
                        || slot == MateriaSlot.EmptyRightLinkedSlot
                        || slot == MateriaSlot.NormalLeftLinkedSlot
                        || slot == MateriaSlot.NormalRightLinkedSlot),
                    SingleSlots = arm.MateriaSlots.Count(slot =>
                        slot == MateriaSlot.EmptyUnlinkedSlot
                        || slot == MateriaSlot.NormalUnlinkedSlot)
                });
            }

            foreach (var acc in elena.AccessoryData.Accessories)
            {
                AccessoryDatabase.Add(new Accessory
                {
                    Id = acc.Index,
                    Name = acc.Name
                });
            }
        }

        public static GameStatus ExtractStatusFromMap(FF7SaveMap map, FF7BattleMap battleMap)
        {
            var time = map.LiveTotalSeconds;

            var t = $"{(time / 3600):00}:{((time % 3600) / 60):00}:{(time % 60):00}";
            var status = new GameStatus()
            {
                Gil = map.LiveGil,
                Location = map.LiveMapName,
                Party = new Character[3],
                ActiveBattle = battleMap.IsActiveBattle,
                ColorTopLeft = map.TopLeft,
                ColorBottomLeft = map.BottomLeft,
                ColorBottomRight = map.BottomRight,
                ColorTopRight = map.TopRight,
                TimeActive = t
            };
            var party = battleMap.Party;

            var chars = map.LiveParty;


            for (var i = 0; i < chars.Length; ++i)
            {
                // Skip empty party
                if (chars[i].ID == 0xFF) continue;

                var chr = new Character()
                {
                    MaxHp = chars[i].MaxHP,
                    MaxMp = chars[i].MaxMP,
                    CurrentHp = chars[i].HP,
                    CurrentMp = chars[i].MP,
                    Name = chars[i].Name,
                    Level = chars[i].Level,
                    Weapon = WeaponDatabase.FirstOrDefault(w => w.Id == chars[i].Weapon),
                    Armlet = ArmletDatabase.FirstOrDefault(a => a.Id == chars[i].Armor),
                    Accessory = AccessoryDatabase.FirstOrDefault(a => a.Id == chars[i].Accessory),
                    WeaponMateria = new Materia[8],
                    ArmletMateria = new Materia[8],
                    Face = GetFaceForCharacter(chars[i]),
                    BackRow = !chars[i].AtFront,

                };



                for (var m = 0; m < chars[i].WeaponMateria.Length; ++m)
                {
                    chr.WeaponMateria[m] = MateriaDatabase.FirstOrDefault(x => x.Id == chars[i].WeaponMateria[m]);
                }
                for (var m = 0; m < chars[i].ArmorMateria.Length; ++m)
                {
                    chr.ArmletMateria[m] = MateriaDatabase.FirstOrDefault(x => x.Id == chars[i].ArmorMateria[m]);
                }

                var effect = (StatusEffect)chars[i].Flags;

                if (battleMap.IsActiveBattle)
                {
                    chr.CurrentHp = party[i].CurrentHp;
                    chr.MaxHp = party[i].MaxHp;
                    chr.CurrentMp = party[i].CurrentMp;
                    chr.MaxMp = party[i].MaxMp;
                    chr.Level = party[i].Level;
                    effect = party[i].Status;
                    chr.BackRow = party[i].IsBackRow;
                }


                var effs = effect.ToString()
                    .Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries)
                    .ToList();
                effs.RemoveAll(x => new[] { "None", "Death" }.Contains(x));
                chr.StatusEffects = effs.ToArray();
                status.Party[i] = chr;
            }

            return status;
        }

        public static string GetFaceForCharacter(FF7SaveMap.Character chr)
        {
            switch (chr.DefaultName)
            {
                case "Cloud":
                    return "cloud";

                case "Barrett":
                    return "barret";

                case "Tifa":
                    return "tifa";

                case "Aerith":
                    return "aeris";

                case "Red XIII":
                    return "red-xiii";

                case "Yuffie":
                    return "yuffie";

                case "Cait Sith":
                    return "cait-sith";

                case "Vincent":
                    return "vincent";

                case "Cid":
                    return "cid";

                case "Young Cloud":
                    return "young-cloud";

                case "Sephiroth":
                    return "sephiroth";
                default:
                    return "";
            }
        }

        private static string ProcessName { get; set; }

        private static void StartMonitoringGame()
        {
            if (Timer is null)
            {
                Timer = new Timer(500);
                Timer.Elapsed += Timer_Elapsed;
                Timer.AutoReset = true;


                Timer_Elapsed(null, null);
                Timer.Start();
            }
        }

        private static void LocateGameProcess()
        {
            var firstRun = true;
            while (FF7 is null)
            {
                if (!firstRun)
                {
                    Console.WriteLine("Could not locate FF7. Is the game running?");
                    Console.WriteLine(
                        "Press enter key to try again, or input process name if different to normal (Eg. ff7_en):");
                    ProcessName = Console.ReadLine().Trim();
                    if (!string.IsNullOrWhiteSpace(ProcessName))
                    {
                        FF7 = Process.GetProcessesByName(ProcessName).FirstOrDefault();
                    }

                    if (FF7 is null)
                    {
                        SearchForProcess(ProcessName);
                    }
                }

                if (FF7 is null) FF7 = Process.GetProcessesByName("ff7_en").FirstOrDefault();
                if (FF7 is null) FF7 = Process.GetProcessesByName("ff7").FirstOrDefault();
                firstRun = false;
            }

            Console.WriteLine($"Located FF7 process {FF7.ProcessName}");
        }

        private static void SearchForProcess(string processName)
        {
            Console.WriteLine("Searching...");
            if (Timer is null)
            {
                Timer = new Timer(300);
                Timer.Elapsed += Timer_Elapsed;
                Timer.AutoReset = true;


                Timer_Elapsed(null, null);
                Timer.Start();

            }
            lock (Timer)
            {


                if (null != Timer)
                {
                    Timer.Enabled = false;
                }

                FF7 = null;
                while (FF7 is null)
                {
                    try
                    {
                        if (FF7 is null) FF7 = Process.GetProcessesByName("ff7_en").FirstOrDefault();
                        if (FF7 is null) FF7 = Process.GetProcessesByName("ff7").FirstOrDefault();
                        if (FF7 is null && !string.IsNullOrWhiteSpace(processName))
                            FF7 = Process.GetProcessesByName(processName).FirstOrDefault();

                    }
                    catch (Exception e)
                    {
                    }

                    Thread.Sleep(250);
                }

                MemoryReader = new NativeMemoryReader(FF7);
                Console.WriteLine("Found FF7");
                if (null != Timer)
                {
                    Timer.Enabled = true;
                }
            }
        }

        private static void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                var bytes = MemoryReader.ReadMemory(new IntPtr(0xDBFD38), 4342);
                var isBattle = MemoryReader.ReadMemory(new IntPtr(0x9A8AF8), 1).First();
                var battle = MemoryReader.ReadMemory(new IntPtr(0x9AB0DC), 0x750);
                var colors = MemoryReader.ReadMemory(new IntPtr(0x0091EFC8), 16);


                SaveMap = new FF7SaveMap(ref bytes);
                BattleMap = new FF7BattleMap(ref battle, isBattle);


                SaveMap.TopLeft = $"{colors[0x2]:X2}{colors[0x1]:X2}{colors[0x0]:X2}";
                SaveMap.BottomLeft = $"{colors[0x6]:X2}{colors[0x5]:X2}{colors[0x4]:X2}";
                SaveMap.TopRight = $"{colors[0xA]:X2}{colors[0x9]:X2}{colors[0x8]:X2}";
                SaveMap.BottomRight = $"{colors[0xE]:X2}{colors[0xD]:X2}{colors[0xC]:X2}";

                PartyStatus = ExtractStatusFromMap(SaveMap, BattleMap);
            }
            catch (Exception ex)
            {
                SearchForProcess(ProcessName);
            }
        }

        public static FF7BattleMap BattleMap { get; set; }

        private static void StartServer(string[] args)
        {
            var serverTask = CreateWebHostBuilder(args).Build().StartAsync();
            Process.Start("http://localhost:5000");
            serverTask.Wait();
        }

        private static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseKestrel(opts => opts.ListenAnyIP(5000))
                .UseStartup<Startup>();
    }
}
