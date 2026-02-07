using MonECC.Domain.Math;
using MonECC.Domain.Model;

namespace MonECC.Tests;

public class MathTests
{
    private readonly Curve _curve = new(35, 3, 101);

    [Theory]
    [InlineData(10, 101, 10)]
    [InlineData(-5, 101, 96)] // Test crucial : le modulo négatif
    [InlineData(106, 101, 5)]
    public void Modulo_ShouldBeAlwaysPositive(long a, long n, long expected)
    {
        Assert.Equal(expected, ModularArithmetic.Mod(a, n));
    }

    [Fact]
    public void Inverse_ShouldWork_ForValidInputs()
    {
        // 2 * 51 = 102 = 1 mod 101
        long inv = ModularArithmetic.Inverse(2, 101);
        Assert.Equal(51, inv);
    }

    [Fact]
    public void Inverse_ShouldThrow_ForNonCoprime()
    {
        // 101 est premier, donc 0 n'a pas d'inverse.
        // Si n n'était pas premier (ex: 10), Inverse(2, 10) planterait aussi.
        Assert.Throws<ArithmeticException>(() => ModularArithmetic.Inverse(0, 101));
    }

    [Fact]
    public void Point_Addition_ShouldWork()
    {
        var P = new Point(2, 9);
        var P2 = _curve.Add(P, P);

        Assert.True(_curve.Contains(P2), $"Le point 2P ({P2}) n'est pas sur la courbe !");

        // P + (-P) = Infini  (-P = (2, -9 mod 101) = (2, 92))
        var negP = new Point(2, 92);
        Assert.True(_curve.Add(P, negP).IsInfinity, "P + (-P) devrait être l'infini");
    }

    [Fact]
    public void Point_Multiplication_ShouldBeCorrect()
    {
        var P = new Point(2, 9);

        // 1 * P = P
        Assert.Equal(P, _curve.Multiply(P, 1));

        // 2 * P = P + P
        var P2_Add = _curve.Add(P, P);
        var P2_Mult = _curve.Multiply(P, 2);
        Assert.Equal(P2_Add, P2_Mult);

        // k * Infini = Infini
        Assert.True(_curve.Multiply(Point.Infinity, 50).IsInfinity);
    }
}