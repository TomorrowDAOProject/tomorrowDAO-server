using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AElf;
using AElf.ExceptionHandler;
using TomorrowDAOServer.Common.Handler;

namespace TomorrowDAOServer.Common;

public static class ValidHelper
{
    public const int MaxResultNumber = 200;
    private const char Underline = '_';

    private const string PatternLetters = @"^[A-Za-z]+$"; 
    private const string UppercaseNumericHyphen = @"^[A-Z0-9\-]+$"; 
    
    public static bool MatchesPattern(this string input, string pattern)
    {
        return Regex.IsMatch(input, pattern);
    }

    public static bool MatchesChainId(this string chainId)
    {
        return chainId.MatchesPattern(PatternLetters);
    }    

    public static bool MatchesNftSymbol(this string symbol)
    {
        return symbol.MatchesPattern(UppercaseNumericHyphen);
    }
    
    [ExceptionHandler(typeof(Exception), TargetType = typeof(TmrwDaoExceptionHandler),
        MethodName = TmrwDaoExceptionHandler.DefaultReturnMethodName, ReturnDefault = ReturnDefault.Default)]
    public static async Task<bool> MatchesAddress(this string address)
    {
        if (address.IndexOf(Underline) > -1)
        {
            var parts = address.Split(Underline);
            address = parts[1];
        }
        return Base58CheckEncoding.Verify(address);
    }
}