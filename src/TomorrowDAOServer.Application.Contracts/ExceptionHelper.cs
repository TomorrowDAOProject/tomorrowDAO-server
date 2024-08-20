using System;
using Volo.Abp;

namespace TomorrowDAOServer;

public static class ExceptionHelper
{
    public static void ThrowArgumentException()
    {
        throw new UserFriendlyException("Invalid input.");
    }

    public static void ThrowSystemException(string message, Exception innerException = null)
    {
        if (innerException != null)
        {
            throw new UserFriendlyException(
                $"System exception occurred during {message}. {innerException.Message}");
        }
        throw new UserFriendlyException(
            $"System exception occurred during {message}");
    }
}