namespace TomorrowDAOServer.Common;

public static class DoubleHelper
{
    public static double GetFactor(long denominator)
    {
        return denominator > 0 ? 1.0 / denominator : 0.0;
    }
    
    public static decimal GetFactor(decimal denominator)
    {
        return denominator > 0 ? 1.0m / denominator : 0.0m;
    }
}