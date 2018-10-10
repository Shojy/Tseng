using System;

namespace Tseng.Models
{
    [Flags]
    public enum StatusEffect:uint
    {
        None          = 0x00000000,
        Barrier       = 0x00010000,
        MBarrier      = 0x00020000,
        Reflect       = 0x00040000,
        Shield        = 0x00100000,
        DeathSentence = 0x00200000,
        Manipulate    = 0x00400000,
        Berserk       = 0x00800000,
        Peerless      = 0x01000000,
        Paralyzed     = 0x02000000,
        Darkness      = 0x04000000,

        Death         = 0x0001,
        NearDeath     = 0x0002,
        Sleep         = 0x0004,
        Poison        = 0x0008,
        Sadness       = 0x0010,
        Fury          = 0x0020,
        Confusion     = 0x0040,
        Silence       = 0x0080,
        Haste         = 0x0100,
        Slow          = 0x0200,
        Stop          = 0x0400,
        Frog          = 0x0800,
        Small         = 0x1000,
        SlowNumb      = 0x2000,
        Petrify       = 0x4000,
        Regen         = 0x8000,
    }

    /*
     * 
1111 0222

1st Part

    0000 - None 
    0001 - Death 
    0002 - Near-death 
    0004 - Sleep 
    0008 - Poison 
    0010 - Sadness 
    0020 - Fury 
    0040 - Confusion 
    0080 - Silence 
    0100 - Haste 
    0200 - Slow 
    0400 - Stop 
    0800 - Frog 
    1000 - Small 
    2000 - Slow-numb 
    4000 - Petrify 
    8000 - Regen 
    FFFF - All Of The Above
2nd Part

    000 - None 
    001 - Barrier 
    002 - MBarrier 
    004 - Reflect 
    010 - Shield 
    020 - Death-sentence 
    040 - Manipulate 
    080 - Berserk 
    100 - Peerless 
    200 - Paralyzed 
    400 - Darkness 
    7F7 - All Of The Above
     */
}