namespace MonECC.Domain.Math;

public static class ModularArithmetic
{
    /// <summary>
    /// Calcule (a mod n) en garantissant un résultat positif [0, n-1].
    /// En C#, l'opérateur % peut retourner du négatif (ex: -5 % 101 = -5).
    /// </summary>
    public static long Mod(long a, long n)
    {
        long result = a % n;
        return (result < 0) ? result + n : result;
    }

    /// <summary>
    /// Calcule l'inverse modulaire de a modulo n via l'Algorithme d'Euclide Étendu.
    /// Requis pour la "division" dans les formules de pente (s = num * inv(den)).
    /// </summary>
    public static long Inverse(long a, long n)
    {
        long t = 0, newT = 1;
        long r = n, newR = Mod(a, n);

        while (newR != 0)
        {
            long quotient = r / newR;
            (t, newT) = (newT, t - quotient * newT);
            (r, newR) = (newR, r - quotient * newR);
        }

        if (r > 1)
            throw new ArithmeticException($"L'élément {a} n'est pas inversible modulo {n} (pas premiers entre eux).");

        return Mod(t, n);
    }
}