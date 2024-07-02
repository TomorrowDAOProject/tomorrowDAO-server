using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using TomorrowDAOServer.Common.Provider;
using TomorrowDAOServer.DAO.Dtos;
using TomorrowDAOServer.Options;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.DAO.Provider;

public interface IDaoAliasProvider
{
    Task<string> GenerateDaoAliasAsync(DAOIndex daoIndex);
    Task<string> GenerateDaoAliasAsync(string daoName);
}

public class DaoAliasProvider : IDaoAliasProvider, ISingletonDependency
{
    private readonly ILogger<DaoAliasProvider> _logger;
    private readonly IGraphQLProvider _graphQlProvider;
    private readonly IOptionsMonitor<DaoAliasOptions> _daoAliasOptions;

    public DaoAliasProvider(ILogger<DaoAliasProvider> logger, IGraphQLProvider graphQlProvider,
        IOptionsMonitor<DaoAliasOptions> daoAliasOptions)
    {
        _logger = logger;
        _graphQlProvider = graphQlProvider;
        _daoAliasOptions = daoAliasOptions;
    }

    public async Task<string> GenerateDaoAliasAsync(DAOIndex daoIndex)
    {
        try
        {
            if (daoIndex == null)
            {
                return string.Empty;
            }

            var alias = await GenerateDaoAliasAsync(daoIndex.Metadata?.Name);
            if (alias.IsNullOrEmpty())
            {
                _logger.LogInformation("Generate dao alias fail, empty alias. daoIndex={0}",
                    JsonConvert.SerializeObject(daoIndex));
                return daoIndex.Id;
            }

            var serial = await _graphQlProvider.SetDaoAliasInfoAsync(daoIndex.ChainId, alias, new DaoAliasDto
            {
                DaoId = daoIndex.Id,
                DaoName = daoIndex.Metadata?.Name,
                Alias = alias,
                CharReplacements = JsonConvert.SerializeObject(_daoAliasOptions.CurrentValue.CharReplacements),
                FilteredChars = JsonConvert.SerializeObject(_daoAliasOptions.CurrentValue.FilteredChars),
            });

            if (serial != 0)
            {
                alias = alias + serial;
            }

            return alias;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Generate dao alias error, daoIndo={0}", JsonConvert.SerializeObject(daoIndex));
            return daoIndex!.Id;
        }
    }

    public Task<string> GenerateDaoAliasAsync(string daoName)
    {
        return Task.FromResult(FilterAndReplaceChars(daoName, _daoAliasOptions.CurrentValue.CharReplacements,
            _daoAliasOptions.CurrentValue.FilteredChars));
    }

    private static string FilterAndReplaceChars(string input, IDictionary<string, string> replacements,
        ISet<string> filters)
    {
        if (input.IsNullOrEmpty() || input.Trim().IsNullOrEmpty())
        {
            return input;
        }

        input = input.Trim();

        var charArray = input.ToCharArray();
        var sb = new StringBuilder();
        for (var i = 0; i < charArray.Length; i++)
        {
            var currentChar = charArray[i];
            if (filters.Contains(currentChar.ToString()))
            {
                continue;
            }

            if (replacements.TryGetValue(currentChar.ToString(), out var replacement))
            {
                sb.Append(replacement);
                var j = i;
                while (++j < charArray.Length && currentChar == charArray[j])
                {
                    i++;
                }
            }
            else
            {
                sb.Append(currentChar);
            }
        }

        return sb.ToString().ToLower();
    }
}