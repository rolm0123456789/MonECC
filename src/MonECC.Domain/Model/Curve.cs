using MonECC.Domain.Math;

namespace MonECC.Domain.Model;

public class Curve(long a, long b, long p)
{
    // Propriétés de la courbe (y^2 = x^3 + ax + b mod p)
    public long A { get; } = a;
    public long B { get; } = b;
    public long P { get; } = p;

    /// <summary>
    /// Vérifie si un point appartient à la courbe.
    /// </summary>
    public bool Contains(Point point)
    {
        if (point.IsInfinity) return true;

        long y2 = ModularArithmetic.Mod(point.Y * point.Y, P);
        long x3 = ModularArithmetic.Mod(point.X * point.X * point.X, P);
        long ax = ModularArithmetic.Mod(A * point.X, P);

        long rightSide = ModularArithmetic.Mod(x3 + ax + B, P);

        return y2 == rightSide;
    }

    /// <summary>
    /// Additionne deux points (P + R = Q) ou Double (P + P = Q).
    /// </summary>
    public Point Add(Point p1, Point p2)
    {
        if (p1.IsInfinity) return p2;
        if (p2.IsInfinity) return p1;

        // Cas P + (-P) = Infinity (même X, mais Y opposés)
        if (p1.X == p2.X && p1.Y != p2.Y) return Point.Infinity;

        long slope;

        // === Cas 1: Doublement (P == P) ===
        if (p1 == p2)
        {
            // Formule : s = (3*Px^2 + a) / (2*Py)
            // Attention : division => multiplication par l'inverse modulaire
            long num = ModularArithmetic.Mod(3 * p1.X * p1.X + A, P);
            long den = ModularArithmetic.Mod(2 * p1.Y, P);

            if (den == 0) return Point.Infinity; // Tangente verticale

            slope = ModularArithmetic.Mod(num * ModularArithmetic.Inverse(den, P), P);
        }
        // === Cas 2: Addition (P != R) ===
        else
        {
            // Formule : s = (Ry - Py) / (Rx - Px)
            long num = ModularArithmetic.Mod(p2.Y - p1.Y, P);
            long den = ModularArithmetic.Mod(p2.X - p1.X, P);

            if (den == 0) return Point.Infinity; // Verticale

            slope = ModularArithmetic.Mod(num * ModularArithmetic.Inverse(den, P), P);
        }

        // Calcul de Qx = s^2 - Px - Rx (ou 2Px si doublement)
        long x3 = ModularArithmetic.Mod(slope * slope - p1.X - p2.X, P);

        // Calcul de Qy = s(Px - Qx) - Py
        long y3 = ModularArithmetic.Mod(slope * (p1.X - x3) - p1.Y, P);

        return new Point(x3, y3);
    }

    /// <summary>
    /// Multiplication scalaire Q = kP (Algorithme Double-and-Add).
    /// </summary>
    public Point Multiply(Point point, long k)
    {
        if (k == 0) return Point.Infinity;
        if (point.IsInfinity) return Point.Infinity;

        Point result = Point.Infinity;
        Point addend = point;
        long scalar = k;

        while (scalar > 0)
        {
            // Si le bit de poids faible est 1, on ajoute
            if ((scalar & 1) == 1)
            {
                result = Add(result, addend);
            }

            // On double le point "addend"
            addend = Add(addend, addend);

            // On passe au bit suivant
            scalar >>= 1;
        }

        return result;
    }
}