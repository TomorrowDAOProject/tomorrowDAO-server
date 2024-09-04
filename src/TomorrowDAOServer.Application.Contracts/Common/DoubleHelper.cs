namespace TomorrowDAOServer.Common;

public class DoubleHelper
{
    public static double GetFactor(long denominator)
    {
        return denominator > 0 ? 1.0 / denominator : 0.0;
    }
}