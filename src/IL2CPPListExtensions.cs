namespace PolyPlus;

// I'm just going to use the naming scheme that other Languages use.
// Idk why Microsoft uses "Select" instead of the much easier to read "Map"
public static class IL2CPPListExtensions
{
    public static bool Any<T>(this Il2CppSystem.Collections.Generic.List<T> list)
    {
        return list.Count > 0;
    }

    public static bool Any<T>(this Il2CppSystem.Collections.Generic.List<T> list, Func<T, bool> predicate)
    {
        foreach(var it in list) 
        {
            if (predicate(it)) return true;
        }
        return false;
    }
    
    public static bool All<T>(this Il2CppSystem.Collections.Generic.List<T> list, Func<T, bool> predicate)
    {
        foreach(var it in list) 
        {
            if (!predicate(it)) return false;
        }
        return true;
    }
    
    public static bool None<T>(this Il2CppSystem.Collections.Generic.List<T> list)
    {
        return list.Count <= 0;
    }
    
    public static bool None<T>(this Il2CppSystem.Collections.Generic.List<T> list, Func<T, bool> predicate)
    {
        foreach(var it in list) 
        {
            if (!predicate(it)) return true;
        }
        return false;
    }
    
    public static Il2CppSystem.Collections.Generic.List<T> Filter<T>(this Il2CppSystem.Collections.Generic.List<T> list, Func<T, bool> predicate)
    {
        var returnList = new Il2CppSystem.Collections.Generic.List<T>();
        foreach (var it in list)
        {
            if (predicate(it)) returnList.Add(it);
        }
        return returnList;
    }
    
    public static Il2CppSystem.Collections.Generic.List<TR> Map<TS, TR>(this Il2CppSystem.Collections.Generic.List<TS> list, Func<TS, TR> transform)
    {
        var returnList = new Il2CppSystem.Collections.Generic.List<TR>();
        foreach (var it in list)
        {
            returnList.Add(transform(it));
        }

        return returnList;
    }
}