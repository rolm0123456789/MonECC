namespace MonECC.Domain.Model;

public readonly record struct Point(long X, long Y)
{
    // Élément neutre (Point à l'infini). 
    // Dans ce TP, (0,0) n'est pas sur la courbe, on peut l'utiliser comme marqueur "Infini".
    public static Point Infinity => new(0, 0);

    public bool IsInfinity => X == 0 && Y == 0;

    // Format requis par le TP : "Qx;Qy"
    public override string ToString() => $"{X};{Y}";
}