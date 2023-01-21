using System.Collections.Generic;
using System.Linq;

namespace CSDTPTest;

internal class Custom
{
    public int A { get; set; } = 0;
    public string B { get; set; } = "";
    public List<string> C { get; set; } = new();

    public override bool Equals(object? obj)
    {
        if (obj == null || !(GetType() == obj.GetType())) return false;

        var other = (Custom)obj;
        return A == other.A && B == other.B && C.SequenceEqual(other.C);
    }

    public override int GetHashCode()
    {
        return 0;
    }
}