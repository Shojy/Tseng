using System;

public class FF7SaveMap
{
    public struct Character
    {
        public byte ID;
        public string DefaultName;
        public string Name; // 0x10
        public byte Level; // 0x01
        public byte Strength; // 0x02
        public byte Vitality; // 0x03
        public byte Magic; // 0x04
        public byte Spirit; // 0x05
        public byte Dexterity; // 0x06
        public byte Luck; // 0x07
        public byte StrBonus; // 0x08
        public byte VitBonus; // 0x09
        public byte MagBonus; // 0x0A
        public byte SprBonus; // 0x0B
        public byte DexBonus; // 0x0C
        public byte LucBonus; // 0x0D
        public byte LimitLevel; // 0x0E
        public byte LimitBar; // 0x0F
        public byte Weapon; // 0x1C
        public byte Armor; // 0x1D
        public byte Accessory; // 0x1E
        public byte Flags; // 0x1F
        public bool AtFront; // 0x20
        public byte LevelProgress; // 0x21
        public Int16 LimitMask; // 0x22, See Qhimm/BC SaveMap
        public Int16 Kills; // 0x24
        public Int16[] LimitTimes; // 0x26, 0x28, 0x2A
        public Int16 HP; // 0x2C
        public Int16 BaseHP; // 0x2E
        public Int16 MP; // 0x30
        public Int16 BaseMP; // 0x32
        public Int16 MaxHP; // 0x38
        public Int16 MaxMP; // 0x3A
        public Int32 Experience; // 0x34
        public Int32[] WeaponMateria; // 0x40, 0x44, 0x48, 0x4C, 0x50, 0x54, 0x58, 0x5C
        public Int32[] ArmorMateria; // 0x60, 0x64, 0x68, 0x6C, 0x60, 0x74, 0x78, 0x7C
        public Int32 ExpToLevel; // 0x80
    }


    private byte[] _Map;
    private bool Valid;

    public FF7SaveMap(ref byte[] Map)
    {
        // Not much else to do here. Checking validity of the response won't be useful in a constructor since
        // we can't get "out" of it, though the caller of our constructor can check .IsValid 
        Update(ref Map);
    }

    public Character[] PreviewParty
    {
        get
        {
            Character[] resultArray = new Character[3];
            if (FillChar(_Map[0x5], ref resultArray[0]) == false)
                return null;
            if (FillChar(_Map[0x6], ref resultArray[1]) == false)
            {
                resultArray[1] = default(Character);
                resultArray[2] = default(Character);
                return resultArray;
            }
            if (FillChar(_Map[0x7], ref resultArray[2]) == false)
            {
                resultArray[2] = default(Character);
                return resultArray;
            }
            return resultArray;
        }
    }

    public Character[] LiveParty
    {
        get
        {
            Character[] resultArray = new Character[3];
            if (FillChar(_Map[0x4F8], ref resultArray[0]) == false)
            {
                resultArray[0] = new Character() { ID = 0xFF};
            }
            if (FillChar(_Map[0x4F9], ref resultArray[1]) == false)
            {
                resultArray[1] = new Character() { ID = 0xFF };
            }
            if (FillChar(_Map[0x4FA], ref resultArray[2]) == false)
            {
                resultArray[2] = new Character() { ID = 0xFF };
            }
            return resultArray;
        }
    }

    // Stubbing this out as it's a 1:1 copy of our live party.
    // Public ReadOnly Property YetAnotherParty As Character()
    // Get
    // Dim resultArray(2) As Character
    // If FillChar(_Map(&HCAD), resultArray(0)) = False Then
    // Return Nothing
    // End If
    // If FillChar(_Map(&HCAE), resultArray(1)) = False Then
    // resultArray(1) = Nothing
    // resultArray(2) = Nothing
    // Return resultArray
    // End If
    // If FillChar(_Map(&HCAF), resultArray(2)) = False Then
    // resultArray(2) = Nothing
    // Return resultArray
    // End If
    // Return resultArray
    // End Get
    // End Property

    public Int32 PreviewGil
    {
        get
        {
            return BitConverter.ToInt32(_Map, 0x20);
        }
    }

    public Int32 PreviewTotalSeconds
    {
        get
        {
            return BitConverter.ToInt32(_Map, 0x24);
        }
    }

    public string PreviewMapName
    {
        get
        {
            byte[] TempArr = new byte[32];
            Array.Copy(_Map, 0x28, TempArr, 0, 32);
            return FFStringtoString(TempArr);
        }
    }

    public byte[] LiveCharIDs
    {
        get
        {
            byte[] RetArray = new byte[3];
            RetArray[0] = _Map[0x4F8];
            RetArray[1] = _Map[0x4F9];
            RetArray[2] = _Map[0x4FA];
            return RetArray;
        }
    }

    // Skipping right along because I'm *not* reading the entire item and materia lists...

    public Int32 LiveGil
    {
        get
        {
            return BitConverter.ToInt32(_Map, 0xB7C);
        }
    }

    public Int32 LiveTotalSeconds
    {
        get
        {
            return BitConverter.ToInt32(_Map, 0xB80);
        }
    }

    public Int32 CountDownTimer
    {
        get
        {
            return BitConverter.ToInt32(_Map, 0xB84);
        }
    }

    public Int16 MapID
    {
        get
        {
            return BitConverter.ToInt16(_Map, 0xB94);
        }
    }

    public Int16 LocID
    {
        get
        {
            return BitConverter.ToInt16(_Map, 0xB96);
        }
    }

    public Int16 PosX
    {
        get
        {
            return BitConverter.ToInt16(_Map, 0xB9A);
        }
    }

    public Int16 PosY
    {
        get
        {
            return BitConverter.ToInt16(_Map, 0xB9);
        }
    }

    public byte Dir
    {
        get
        {
            return _Map[0xBA0];
        }
    }

    public Int32 FieldTotalSeconds
    {
        get
        {
            Int32 RetValue = _Map[0xBB6];
            RetValue = RetValue + (_Map[0xBB5] * 60);
            RetValue = RetValue + (_Map[0xBB4] * 3600);
            return RetValue;
        }
    }

    public Int16 BattlesFought
    {
        get
        {
            return BitConverter.ToInt16(_Map, 0xBB);
        }
    }

    public Int16 Escapes
    {
        get
        {
            return BitConverter.ToInt16(_Map, 0xBBE);
        }
    }

    public UInt32 UltimateWeaponHP
    {
        get
        {
            // Dirty hack time! It's a 24-bit value and we don't exactly have a "ToInt24"
            byte TempStorage;
            UInt32 RetValue;
            TempStorage = _Map[0xBFE];
            _Map[0xBFE] = 0;
            RetValue = BitConverter.ToUInt32(_Map, 0xBFE);
            _Map[0xBFE] = TempStorage;
            return RetValue;
        }
    }

    public Int16 BattlePoints
    {
        get
        {
            return BitConverter.ToInt16(_Map, 0xC14);
        }
    }

    // Stubbing this out because it's a copy of our live CharIDs
    // 
    // Public ReadOnly Property YetAnotherCharIDs As Byte()
    // Get
    // Dim RetArray(2) As Byte
    // RetArray(0) = _Map(&HCAD)
    // RetArray(1) = _Map(&HCAE)
    // RetArray(2) = _Map(&HCAF)
    // Return RetArray
    // End Get
    // End Property

    public Int16 PartyGP
    {
        get
        {
            return BitConverter.ToInt16(_Map, 0xCEE);
        }
    }

    public byte Disc
    {
        get
        {
            return _Map[0xEA4];
        }
    }

    public bool IsValid
    {
        get
        {
            return Valid;
        }
    }

    public string LiveMapName
    {
        get
        {
            byte[] TempArr = new byte[32];
            Array.Copy(_Map, 0xF0C, TempArr, 0, 32);
            return FFStringtoString(TempArr);
        }
    }

    private string NamestoFaces(ref byte Input)
    {
        switch (Input)
        {
            case 0:
                {
                    return "Cloud";
                }

            case 1:
                {
                    return "Barrett";
                }

            case 2:
                {
                    return "Tifa";
                }

            case 3:
                {
                    return "Aerith";
                }

            case 4:
                {
                    return "Red XIII";
                }

            case 5:
                {
                    return "Yuffie";
                }

            case 6:
                {
                    return "Cait Sith";
                }

            case 7:
                {
                    return "Vincent";
                }

            case 8:
                {
                    return "Cid";
                }

            case 9:
                {
                    return "Young Cloud";
                }

            case 10:
                {
                    return "Sephiroth";
                }

            case 11:
                {
                    return "Chocobo";
                }

            case 255:
                {
                    return "None";
                }

            default:
                {
                    return "Invalid Value";
                }
        }
        return "Error";
    }

    private Int16 GetCharOffset(byte ID)
    {
        switch (ID)
        {
            case 0:
                {
                    return 0x0054;
                }

            case 1:
                {
                    return 0x00D8;
                }

            case 2:
                {
                    return 0x015C;
                }

            case 3:
                {
                    return 0x01E0;
                }

            case 4:
                {
                    return 0x0264;
                }

            case 5:
                {
                    return 0x02E8;
                }

            case 6:
                {
                    return 0x036;
                }

            case 7:
                {
                    return 0x03F0;
                }

            case 8:
                {
                    return 0x0474;
                }

            case 9:
                {
                    return 0x036C;
                }

            case 10:
                {
                    return 0x03F0;
                }

            default:
                {
                    return -1;
                }
        }
        return -1;
    }

    private string FFStringtoString(byte[] FFString)
    {
        // Dirty translation, will only work for printable chars.
        byte[] DestBytes = new byte[FFString.Length - 1 + 1];
        byte myIterator = 0;
        byte endIndex = 255;
        foreach (byte ThisByte in FFString)
        {
            if (ThisByte < 0x60)
            {
                DestBytes[myIterator] = (byte)(ThisByte + 0x20);
                myIterator = (byte)(myIterator + 1);
            }
            else if (ThisByte == 0xD0)
                DestBytes[myIterator] = 0x20;
            else if (ThisByte < 0xE1)
                // Not going to bother doing all the non-english characters, sorry!
                DestBytes[myIterator] = 0x3F;
            else if (ThisByte == 0xE2)
                DestBytes[myIterator] = 0x9;
            else if (ThisByte == 0xE3)
                DestBytes[myIterator] = 0x2;
            else if (ThisByte < 0xFE)
                DestBytes[myIterator] = 0x3F;
            else if (ThisByte == 0xFF)
            {
                DestBytes[myIterator] = 0x0;
                if (endIndex == 255)
                    endIndex = myIterator;
            }
            else
                DestBytes[myIterator] = 0x3F;
        }
        string RetValue = System.Text.Encoding.ASCII.GetString(DestBytes);
        if (endIndex != 255)
            RetValue = RetValue.Remove(endIndex);
        return RetValue;
    }

    private bool FillChar(byte CharID, ref Character RetChar)
    {
        Int16 Offset;
        RetChar.DefaultName = NamestoFaces(ref CharID);
        if (RetChar.DefaultName == "None")
            return false;
        Offset = GetCharOffset(CharID);
        if (Offset == -1)
            return false;
        byte[] FFName = new byte[12];
        Array.Copy(_Map, Offset + 0x10, FFName, 0, 12);
        RetChar.Name = FFStringtoString(FFName);
        RetChar.Level = _Map[Offset + 0x01];
        RetChar.Strength = _Map[Offset + 0x02];
        RetChar.Vitality = _Map[Offset + 0x03];
        RetChar.Magic = _Map[Offset + 0x04];
        RetChar.Spirit = _Map[Offset + 0x05];
        RetChar.Dexterity = _Map[Offset + 0x06];
        RetChar.Luck = _Map[Offset + 0x07];
        RetChar.StrBonus = _Map[Offset + 0x08];
        RetChar.VitBonus = _Map[Offset + 0x09];
        RetChar.MagBonus = _Map[Offset + 0x0A];
        RetChar.SprBonus = _Map[Offset + 0x0B];
        RetChar.DexBonus = _Map[Offset + 0x0C];
        RetChar.LucBonus = _Map[Offset + 0x0D];
        RetChar.LimitLevel = _Map[Offset + 0x0E];
        RetChar.LimitBar = _Map[Offset + 0x0F];
        RetChar.Weapon = _Map[Offset + 0x1C];
        RetChar.Armor = _Map[Offset + 0x1D];
        RetChar.Accessory = _Map[Offset + 0x1E];
        RetChar.Flags = _Map[Offset + 0x1F];
        if (_Map[Offset + 0x20] == 0xFF)
            RetChar.AtFront = true;
        else
            RetChar.AtFront = false;
        RetChar.LevelProgress = _Map[Offset + 0x21];
        RetChar.LimitMask = BitConverter.ToInt16(_Map, Offset + 0x22);
        RetChar.Kills = BitConverter.ToInt16(_Map, Offset + 0x24);
        RetChar.LimitTimes = new short[3];
        RetChar.LimitTimes[0] = BitConverter.ToInt16(_Map, Offset + 0x26);
        RetChar.LimitTimes[1] = BitConverter.ToInt16(_Map, Offset + 0x28);
        RetChar.LimitTimes[2] = BitConverter.ToInt16(_Map, Offset + 0x2A);
        RetChar.HP = BitConverter.ToInt16(_Map, Offset + 0x2C);
        RetChar.BaseHP = BitConverter.ToInt16(_Map, Offset + 0x2E);
        RetChar.MP = BitConverter.ToInt16(_Map, Offset + 0x30);
        RetChar.BaseMP = BitConverter.ToInt16(_Map, Offset + 0x32);
        RetChar.MaxHP = BitConverter.ToInt16(_Map, Offset + 0x38);
        RetChar.MaxMP = BitConverter.ToInt16(_Map, Offset + 0x3A);
        RetChar.Experience = BitConverter.ToInt32(_Map, Offset + 0x3C);
        RetChar.WeaponMateria = new int[8];
        for (int MatId = 0; MatId <= 7; MatId++)
            RetChar.WeaponMateria[MatId] = _Map[Offset + 0x40 + (4 * MatId)];
        RetChar.ArmorMateria = new int[8];
        for (int MatId = 0; MatId <= 7; MatId++)
            RetChar.ArmorMateria[MatId] = _Map[Offset + 0x60 + (4 * MatId)];
        RetChar.ExpToLevel = BitConverter.ToInt32(_Map, Offset + 0x80);
        return true;
    }

    public bool Update(ref byte[] NewMap)
    {
        // Check the map for sanity, if it's not sane, return FALSE and set Valid to FALSE.
        bool Passed = true;
        try
        {
            if (NewMap[0x4FB] != 0xFF)
                Passed = false;
            if (NewMap[0xB98] != 0x0)
                Passed = false;
            if (NewMap[0xBA3] != 0x0)
                Passed = false;
        }
        catch (Exception ex)
        {
            Passed = false;
        }
        if (!Passed)
        {
            Valid = false;
            _Map = null;
        }
        else
        {
            Valid = true;
            _Map = NewMap;
        }
        return Passed;
    }
}
