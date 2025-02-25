// Updated with xxHash4 for spacial hashing
using Unity.Mathematics;

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
        _accumulator = accumulator;
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

// For loop vectorization using Burst: https://docs.unity3d.com/Packages/com.unity.burst@1.8/manual/optimization-loop-vectorization.html
// Changed uint / int to uint4 / int4, will enable looping over multiple values at same time
public readonly struct xxHash4
{
    private readonly uint4 _accumulator;

    const uint PRIME32_2 = 0x85EBCA77U;  // 0b10000101111010111100101001110111
    const uint PRIME32_3 = 0xC2B2AE3DU;  // 0b11000010101100101010111000111101
    const uint PRIME32_4 = 0x27D4EB2FU;  // 0b00100111110101001110101100101111
    const uint PRIME32_5 = 0x165667B1U;  // 0b00010110010101100110011110110001

    public xxHash4(uint4 accumulator)
    {
        _accumulator = accumulator;
    }

    public static xxHash4 Seed(int4 seed) => (uint4)seed + PRIME32_5;

    // returns the passed hash as uint implicitly
    // e.g. uint u = new xxHash(0) will work, as well as
    // xxHash h = new xxHash(0)
    public static implicit operator uint4(xxHash4 hash)
    {
        uint4 avalanche = hash._accumulator;
        avalanche ^= avalanche >> 15;
        avalanche *= PRIME32_2;
        avalanche ^= avalanche >> 13;
        avalanche *= PRIME32_3;
        avalanche ^= avalanche >> 16;
        return avalanche;
    }

    public static implicit operator xxHash4(uint4 accumulator) => new xxHash4(accumulator);

    public xxHash4 Eat(int4 data) => RotateLeft(_accumulator + (uint4)data * PRIME32_3, 17) * PRIME32_4;

    private static uint4 RotateLeft(uint4 data, int steps) => ((data << steps) | (data >> 32 - steps));

    public static implicit operator xxHash4(xxHash hash) => new xxHash4(hash._accumulator);
};