using HockeyCoach.Sim.Checks;

namespace HockeyCoach.Sim.Tests.Checks;

public class CheckMathTests
{
    [Theory]
    [InlineData(0.5)]
    [InlineData(0.85)]
    [InlineData(0.02)]
    [InlineData(0.999)]
    public void Sigmoid_InvertsLogit(double p)
    {
        Assert.Equal(p, CheckMath.Sigmoid(CheckMath.Logit(p)), 12);
    }

    [Fact]
    public void Logit_OfHalf_IsZero()
    {
        Assert.Equal(0.0, CheckMath.Logit(0.5), 15);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    [InlineData(-0.1)]
    [InlineData(double.NaN)]
    public void Logit_Throws_OutsideOpenUnitInterval(double p)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CheckMath.Logit(p));
    }

    [Theory]
    [InlineData(1000.0, 1.0)]
    [InlineData(-1000.0, 0.0)]
    [InlineData(0.0, 0.5)]
    public void Sigmoid_IsFiniteAndBounded_ForExtremeInputs(double x, double expected)
    {
        double p = CheckMath.Sigmoid(x);

        Assert.False(double.IsNaN(p));
        Assert.Equal(expected, p, 12);
    }

    [Fact]
    public void Sigmoid_IsSymmetric()
    {
        for (double x = -10; x <= 10; x += 0.5)
        {
            Assert.Equal(1.0, CheckMath.Sigmoid(x) + CheckMath.Sigmoid(-x), 12);
        }
    }

    [Theory]
    [InlineData(0.01, 0.02)]
    [InlineData(0.5, 0.5)]
    [InlineData(0.99, 0.98)]
    public void Clamp_KeepsValueWithinBounds(double value, double expected)
    {
        Assert.Equal(expected, CheckMath.Clamp(value, 0.02, 0.98));
    }
}
