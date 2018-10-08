﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
        public static Status PartyStatus { get; set; }
        public static void Main(string[] args)
        {
            LoadData();

            StartMonitoringGame();

            StartServer(args);
        }

        public static Status ExtractStatusFromMap(FF7SaveMap map)
        {
            var status = new Status()
            {
                Gil = map.LiveGil,
                Location = map.LiveMapName,
                Party = new Character[3]
            };

            var chars = map.LiveParty;

            for (var i = 0; i < chars.Length; ++i)
            {
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
                    BackRow = !chars[i].AtFront
                };

                if ((chars[i].Flags & 0x10) == 0x10)
                {
                    chr.Status = "Sadness";
                }
                if ((chars[i].Flags & 0x20) == 0x20)
                {
                    chr.Status = "Fury";
                }

                for (var m = 0; m < chars[i].WeaponMateria.Length; ++m)
                {
                    chr.WeaponMateria[m] = MateriaDatabase.FirstOrDefault(x => x.Id == chars[i].WeaponMateria[m]);
                }
                for (var m = 0; m < chars[i].ArmorMateria.Length; ++m)
                {
                    chr.ArmletMateria[m] = MateriaDatabase.FirstOrDefault(x => x.Id == chars[i].ArmorMateria[m]);
                }

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
            FF7 = Process.GetProcessesByName("ff7_en").FirstOrDefault();
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

            //lock (SaveMap)
            {
                SaveMap = new FF7SaveMap(ref bytes);
            }

            PartyStatus = ExtractStatusFromMap(SaveMap);
        }

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

                // Only need to load the base set if there's more than one option available.
                MergeData(Path.Combine("Data", "default"));
            }
            else
            {
                selectedData = dataDirectories.First();
            }
            MergeData(selectedData.FullName);
        }

        private static void MergeData(string path)
        {
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
                .UseStartup<Startup>();
    }
}