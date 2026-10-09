using HockeyCoach.Sim.Random;

namespace HockeyCoach.Sim.Tests.Random;

public class Pcg32Tests
{
    // Reference output of pcg32-demo from pcg-c-basic (seed 42, stream 54), "Round 1".
    private static readonly uint[] ReferenceWords =
    {
        0xa15c02b7, 0x7b47f409, 0xba1d3330, 0x83d2f293, 0xbfa4784b, 0xcbed606e,
    };

    [Fact]
    public void NextUInt_MatchesPcgCBasicReferenceVector_ForSeed42Stream54()
    {
        var rng = new Pcg32(42UL, 54UL);

        var actual = new uint[ReferenceWords.Length];
        for (int i = 0; i < actual.Length; i++)
        {
            actual[i] = rng.NextUInt();
        }

        Assert.Equal(ReferenceWords, actual);
    }

    [Fact]
    public void NextInt_MatchesPcgCBasicReferenceCoinsAndRolls_ForSeed42Stream54()
    {
        // pcg32-demo continues the same stream: 6 words, 65 coin flips (boundedrand 2), 33 dice rolls (boundedrand 6 + 1).
        var rng = new Pcg32(42UL, 54UL);
        for (int i = 0; i < 6; i++)
        {
            rng.NextUInt();
        }

        var coins = new char[65];
        for (int i = 0; i < coins.Length; i++)
        {
            coins[i] = rng.NextInt(2) == 1 ? 'H' : 'T';
        }

        var rolls = new int[33];
        for (int i = 0; i < rolls.Length; i++)
        {
            rolls[i] = rng.NextInt(6) + 1;
        }

        Assert.Equal("HHTTTHTHHHTHTTTHHHHHTTTHHHTHTHTHTTHTTTHHHHHHTTTTHHTTTTTHTTTTTTTHT", new string(coins));
        Assert.Equal(
            new[] { 3, 4, 1, 1, 2, 2, 3, 2, 4, 3, 2, 4, 3, 3, 5, 2, 3, 1, 3, 1, 5, 1, 4, 1, 5, 6, 4, 6, 6, 2, 6, 3, 3 },
            rolls);
    }

    [Fact]
    public void SameSeedAndStream_ProduceIdenticalSequences()
    {
        var a = new Pcg32(12345UL, 7UL);
        var b = new Pcg32(12345UL, 7UL);

        for (int i = 0; i < 1000; i++)
        {
            Assert.Equal(a.NextUInt(), b.NextUInt());
        }
    }

    [Fact]
    public void DifferentStreams_ProduceDifferentSequences()
    {
        var a = new Pcg32(12345UL, 1UL);
        var b = new Pcg32(12345UL, 2UL);

        int equal = 0;
        for (int i = 0; i < 100; i++)
        {
            if (a.NextUInt() == b.NextUInt())
            {
                equal++;
            }
        }

        Assert.True(equal < 5);
    }

    [Fact]
    public void NextDouble_StaysInHalfOpenUnitInterval()
    {
        var rng = new Pcg32(1UL);
        for (int i = 0; i < 100_000; i++)
        {
            double d = rng.NextDouble();
            Assert.InRange(d, 0.0, 1.0);
            Assert.True(d < 1.0);
        }
    }

    [Fact]
    public void NextDouble_HasMeanNearHalf()
    {
        var rng = new Pcg32(2UL);
        const int n = 200_000;
        double sum = 0;
        for (int i = 0; i < n; i++)
        {
            sum += rng.NextDouble();
        }

        Assert.InRange(sum / n, 0.495, 0.505);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(20)]
    public void NextInt_StaysWithinBounds_AndHitsEveryValue(int bound)
    {
        var rng = new Pcg32(3UL);
        var seen = new bool[bound];
        for (int i = 0; i < 10_000; i++)
        {
            int v = rng.NextInt(bound);
            Assert.InRange(v, 0, bound - 1);
            seen[v] = true;
        }

        Assert.All(seen, Assert.True);
    }

    [Fact]
    public void NextIntRange_StaysWithinBounds()
    {
        var rng = new Pcg32(4UL);
        for (int i = 0; i < 10_000; i++)
        {
            Assert.InRange(rng.NextInt(-3, 4), -3, 3);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NextInt_Throws_WhenBoundIsNotPositive(int bound)
    {
        var rng = new Pcg32(5UL);
        Assert.Throws<ArgumentOutOfRangeException>(() => rng.NextInt(bound));
    }

    [Fact]
    public void Chance_ConsumesOneDoubleDraw_RegardlessOfProbability()
    {
        var a = new Pcg32(6UL);
        var b = new Pcg32(6UL);

        a.Chance(0.0);
        a.Chance(1.0);
        b.NextDouble();
        b.NextDouble();

        Assert.Equal(a.NextUInt(), b.NextUInt());
    }

    [Fact]
    public void Chance_ApproximatesProbability()
    {
        var rng = new Pcg32(7UL);
        const int n = 100_000;
        int hits = 0;
        for (int i = 0; i < n; i++)
        {
            if (rng.Chance(0.85))
            {
                hits++;
            }
        }

        Assert.InRange(hits / (double)n, 0.845, 0.855);
    }
}
