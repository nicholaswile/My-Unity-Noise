using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// xxHash: https://github.com/Cyan4973/xxHash/blob/dev/doc/xxhash_spec.md by Yann Collet

public readonly struct xxHash
{
    public readonly uint _accumulator;
    
    const uint PRIME32_1 = 0x9E3779B1U;  // 0b10011110001101110111100110110001
    const uint PRIME32_2 = 0x85EBCA77U;  // 0b10000101111010111100101001110111
    const uint PRIME32_3 = 0xC2B2AE3DU;  // 0b11000010101100101010111000111101
    const uint PRIME32_4 = 0x27D4EB2FU;  // 0b00100111110101001110101100101111
    const uint PRIME32_5 = 0x165667B1U;  // 0b00010110010101100110011110110001
    
    public xxHash(uint accumulator)
    {
        this._accumulator = accumulator;
    }

    public static xxHash Seed(int seed) => (uint)seed + PRIME32_5;

    // returns the passed hash as uint implicitly
    // e.g. uint u = new xxHash(0) will work, as well as
    // xxHash h = new xxHash(0)
    public static implicit operator uint(xxHash hash)
    {
        uint avalanche = hash._accumulator;
        avalanche ^= avalanche >> 15;
        avalanche *= PRIME32_2;
        avalanche ^= avalanche >> 13;
        avalanche *= PRIME32_3;
        avalanche ^= avalanche >> 16;
        return avalanche;
    }

    public static implicit operator xxHash(uint accumulator) => new xxHash(accumulator);

    public xxHash Eat(int data) => RotateLeft(_accumulator + (uint)data * PRIME32_3, 17) * PRIME32_4;

    public xxHash Eat(byte data) => RotateLeft(_accumulator + data * PRIME32_5, 11) * PRIME32_1;

    private static uint RotateLeft(uint data, int steps) => ((data << steps) | (data >> 32 - steps));
};