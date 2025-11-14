using System.Reflection;

namespace PingisTurnering.Tests;

internal static class ReflectionHelpers
{
    internal static T GetPrivateField<T>(object instance, string fieldName)
    {
        var f = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new MissingFieldException(instance.GetType().FullName, fieldName);
        return (T)f.GetValue(instance)!;
    }
}