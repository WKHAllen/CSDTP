using System.Collections.Generic;
using System.Linq;

namespace CSDTPTest;

internal class Custom
{
    public int a { get; set; } = 0;
    public string b { get; set; } = "";
    public List<string> c { get; set; } = new();

    public override bool Equals(object? obj)
    {
        if (obj == null || !(GetType() == obj.GetType())) return false;

        var other = (Custom)obj;
        return a == other.a && b == other.b && c.SequenceEqual(other.c);
    }
}