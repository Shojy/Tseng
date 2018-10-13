using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Tseng.Models;

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
            LoadData();

            StartMonitoringGame();

            StartServer(args);
        }

        public static GameStatus ExtractStatusFromMap(FF7SaveMap map, FF7BattleMap battleMap)
        {
            var time = map.LiveTotalSeconds;

            var t = $"{(time / 3600):00}:{((time%3600)/60):00}:{(time%60):00}";
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

        private static void StartMonitoringGame()
        {
            var firstRun = true;
            while (FF7 is null)
            {
                if (!firstRun)
                {
                    Console.WriteLine("Could not locate FF7. Is the game running?");
                    Console.WriteLine("Press enter key to try again, or input process name if different to normal (Eg. ff7_en):");
                    var process = Console.ReadLine().Trim();
                    if (!string.IsNullOrWhiteSpace(process))
                    {
                        FF7 = Process.GetProcessesByName(process).FirstOrDefault();
                    }
                }
                if (FF7 is null) FF7 = Process.GetProcessesByName("ff7_en").FirstOrDefault();
                if (FF7 is null) FF7 = Process.GetProcessesByName("ff7").FirstOrDefault();
                firstRun = false;
            }

            Console.WriteLine($"Located FF7 process {FF7.ProcessName}");

            Timer = new Timer(1000);
            Timer.Elapsed += Timer_Elapsed;
            Timer.AutoReset = true;

            MemoryReader = new NativeMemoryReader(FF7);

            Timer_Elapsed(null, null);
            Timer.Start();
        }

        private static void Timer_Elapsed(object sender, ElapsedEventArgs e)
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

        public static FF7BattleMap BattleMap { get; set; }

        private static void LoadData()
        {
            var direInfo = new DirectoryInfo("Data");
            var dataDirectories = direInfo.EnumerateDirectories().ToArray();
            DirectoryInfo selectedData;
            if (dataDirectories.Length > 1)
            {
                Console.WriteLine("Multiple data versions found. Please enter the number of the set to use.");

                for (var i = 0; i < dataDirectories.Length; ++i)
                {
                    Console.WriteLine($"{i + 1}. {dataDirectories[i].Name}");
                }

                var choice = -1;
                do
                {
                    Console.Write("Enter choice: ");
                } while (!int.TryParse(Console.ReadLine().Trim(), out choice) || choice <= 0 || choice > dataDirectories.Length);
                selectedData = dataDirectories[choice - 1];

            }
            else
            {
                selectedData = dataDirectories.First();
            }

            if (!selectedData.Name.Equals("default", StringComparison.InvariantCultureIgnoreCase))
            {
                // Only need to load the base set if there's more than one option available.
                MergeData(Path.Combine("Data", "default"));
            }
            MergeData(Path.Combine("Data", selectedData.Name));
        }

        private static void MergeData(string path)
        {
            Console.WriteLine($"Loading databases at {path}");
            if (File.Exists(Path.Combine(path, "materia.json")))
            {
                using (var reader = new StreamReader(Path.Combine(path, "materia.json")))
                {
                    var db = JsonConvert.DeserializeObject<List<Materia>>(reader.ReadToEnd());

                    foreach (var item in db)
                    {
                        var existingRecord = MateriaDatabase.FirstOrDefault(x => x.Id == item.Id);
                        if (!(existingRecord is null))
                        {
                            MateriaDatabase.Remove(existingRecord);
                        }

                        MateriaDatabase.Add(item);
                    }
                    Console.WriteLine("Loaded Materia Database.");
                }
            }


            if (File.Exists(Path.Combine(path, "weapons.json")))
            {
                using (var reader = new StreamReader(Path.Combine(path, "weapons.json")))
                {
                    var db = JsonConvert.DeserializeObject<List<Weapon>>(reader.ReadToEnd());
                    foreach (var item in db)
                    {
                        var existingRecord = WeaponDatabase.FirstOrDefault(x => x.Id == item.Id);
                        if (!(existingRecord is null))
                        {
                            WeaponDatabase.Remove(existingRecord);
                        }

                        WeaponDatabase.Add(item);
                    }
                    Console.WriteLine("Loaded Weapon Database.");
                }
            }


            if (File.Exists(Path.Combine(path, "armlets.json")))
            {
                using (var reader = new StreamReader(Path.Combine(path, "armlets.json")))
                {
                    var db = JsonConvert.DeserializeObject<List<Armlet>>(reader.ReadToEnd());
                    foreach (var item in db)
                    {
                        var existingRecord = ArmletDatabase.FirstOrDefault(x => x.Id == item.Id);
                        if (!(existingRecord is null))
                        {
                            ArmletDatabase.Remove(existingRecord);
                        }

                        ArmletDatabase.Add(item);
                    }
                    Console.WriteLine("Loaded Armlet Database.");
                }
            }


            if (File.Exists(Path.Combine(path, "accessories.json")))
            {
                using (var reader = new StreamReader(Path.Combine(path, "accessories.json")))
                {
                    var db = JsonConvert.DeserializeObject<List<Accessory>>(reader.ReadToEnd());
                    foreach (var item in db)
                    {
                        var existingRecord = AccessoryDatabase.FirstOrDefault(x => x.Id == item.Id);
                        if (!(existingRecord is null))
                        {
                            AccessoryDatabase.Remove(existingRecord);
                        }

                        AccessoryDatabase.Add(item);
                    }
                    Console.WriteLine("Loaded Accessory Database.");
                }
            }
        }

        private static void StartServer(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        private static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseKestrel(opts => opts.ListenAnyIP(5000))
                .UseStartup<Startup>();
    }
}
