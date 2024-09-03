using System.Collections.Generic;
using System.Linq;

namespace TomorrowDAOServer.Common;

public class AllFieldsEqualComparer<T> : IEqualityComparer<T>
{
    public bool Equals(T x, T y)
    {
        if (x == null || y == null)
        {
            return false;
        }
        
        var fields = typeof(T).GetProperties();
        foreach (var field in fields)
        {
            var valueX = field.GetValue(x);
            var valueY = field.GetValue(y);
            if (valueX == null || valueY == null)
            {
                if (valueX != valueY)
                {
                    return false;
                }
            }
            else if (!valueX.Equals(valueY))
            {
                return false; 
            }
        }
        
        return true; 
    }

    public int GetHashCode(T obj)
    {
        var fields = typeof(T).GetProperties();
        return fields
            .Select(field => field.GetValue(obj))
            .Aggregate(17, (current, value) => current * 31 + (value?.GetHashCode() ?? 0));
    }
}