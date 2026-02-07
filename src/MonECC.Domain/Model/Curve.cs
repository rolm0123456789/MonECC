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

        return y2 == ModularArithmetic.Mod(x3 + ax + B, P);
    }

    /// <summary>
    /// Additionne deux points sur la courbe (ou double si p1 == p2).
    /// </summary>
    public Point Add(Point p1, Point p2)
    {
        // Élément neutre : O + P = P
        if (p1.IsInfinity) return p2;
        if (p2.IsInfinity) return p1;

        // P + (-P) = O (même X, Y opposés)
        if (p1.X == p2.X && p1.Y != p2.Y) return Point.Infinity;

        long slope;

        // Doublement (P == P) : pente = (3*Px² + a) / (2*Py)
        if (p1 == p2)
        {
            long num = ModularArithmetic.Mod(3 * p1.X * p1.X + A, P);
            long den = ModularArithmetic.Mod(2 * p1.Y, P);
            if (den == 0) return Point.Infinity; // Tangente verticale
            slope = ModularArithmetic.Mod(num * ModularArithmetic.Inverse(den, P), P);
        }
        // Addition (P != Q) : pente = (Qy - Py) / (Qx - Px)
        else
        {
            long num = ModularArithmetic.Mod(p2.Y - p1.Y, P);
            long den = ModularArithmetic.Mod(p2.X - p1.X, P);
            if (den == 0) return Point.Infinity;
            slope = ModularArithmetic.Mod(num * ModularArithmetic.Inverse(den, P), P);
        }

        // Coordonnées du point résultant
        long x3 = ModularArithmetic.Mod(slope * slope - p1.X - p2.X, P);  // Rx = s² - Px - Qx
        long y3 = ModularArithmetic.Mod(slope * (p1.X - x3) - p1.Y, P);   // Ry = s(Px - Rx) - Py

        return new Point(x3, y3);
    }

    /// <summary>
    /// Multiplication scalaire Q = kP (Double-and-Add).
    /// </summary>
    public Point Multiply(Point point, long k)
    {
        if (k == 0) return Point.Infinity;
        if (point.IsInfinity) return Point.Infinity;

        Point result = Point.Infinity;
        Point addend = point;
        long scalar = k;

        // Parcours bit à bit de k (du poids faible au poids fort)
        while (scalar > 0)
        {
            // Si le bit courant est 1, on accumule le point
            if ((scalar & 1) == 1)
                result = Add(result, addend);

            // On double le point à chaque itération
            addend = Add(addend, addend);
            scalar >>= 1;
        }

        return result;
    }
}