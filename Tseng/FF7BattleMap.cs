using System;
using Tseng.Models;

namespace Tseng
{
    public class FF7BattleMap
    {
        public FF7BattleMap(ref byte[] bytes, byte activeBattle)
        {
            IsActiveBattle = activeBattle == 0x01;
            _map = bytes;
        }

        private byte[] _map;

        public bool IsActiveBattle { get; set; }

        private int _size = 0x68;
        private int _charStart = 0x00;
        private int _oppsStart = 0x01A0;

        public struct Actor
        {
            public int CurrentHp { get; set; } // 0x2C
            public int MaxHp { get; set; } // 0x30
            public short CurrentMp { get; set; } // 0x28
            public short MaxMp { get; set; } // 0x2A
            public byte Level { get; set; } // 0x09
            public StatusEffect Status { get; set; } // 0x00
            public bool IsBackRow { get; set; } //0x04

        }

        public Actor[] Party => GetActors(_charStart, 4);
        public Actor[] Opponents => GetActors(_oppsStart, 6);

        private Actor[] GetActors(int start, int count)
        {
            var acts = new Actor[count];

            for(var i = 0; i < count; ++i)
            {
                var offset = start + i * _size;
                var a = new Actor
                {
                    CurrentHp = BitConverter.ToInt32(_map, offset + 0x2C),
                    MaxHp = BitConverter.ToInt32(_map, offset + 0x30),
                    CurrentMp = BitConverter.ToInt16(_map, offset + 0x28),
                    MaxMp = BitConverter.ToInt16(_map, offset + 0x2A),
                    Level = _map[offset+0x09],
                    Status = (StatusEffect)BitConverter.ToUInt32(_map, offset + 0x00),
                    IsBackRow = (_map[offset + 0x04] & 0x40) == 0x40
                };
                acts[i] = a;
            }

            return acts;
        }

    }
}