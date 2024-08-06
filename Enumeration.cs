using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace RegExCompiler;

public abstract class Enumeration<T>(int id, T value) : IComparable where T : notnull
{
    public T Value { get; private set; } = value;

    private int Id { get; set; } = id;

    public override string ToString() => Value?.ToString() ?? "null";

    public static IEnumerable<TK> GetAll<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] TK>()
        where TK : Enumeration<T> =>
        typeof(TK).GetFields(BindingFlags.Public |
                             BindingFlags.Static |
                             BindingFlags.DeclaredOnly)
            .Select(f => f.GetValue(null))
            .Cast<TK>();

    public override bool Equals(object? obj)
    {
        if (obj is not Enumeration<T> otherValue)
        {
            return false;
        }

        var typeMatches = GetType() == obj.GetType();
        var valueMatches = Id.Equals(otherValue.Id);

        return typeMatches && valueMatches;
    }

    public static bool FromValue<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] TK>(T value,
        [NotNullWhen(true)] out TK? res) where TK : Enumeration<T>
    {
        var fitting = GetAll<TK>().Where(elem => elem.Value.Equals(value)).ToList();
        if (fitting.Count != 1)
        {
            res = null;
            return false;
        }

        res = fitting.Single();
        return true;
    }

    public int CompareTo(object? other)
    {
        return other is null ? 1 : Id.CompareTo(((Enumeration<T>)other).Id);
    }
}