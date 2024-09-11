using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TomorrowDAOServer.Common.Dtos;
using TomorrowDAOServer.DAO;
using TomorrowDAOServer.DAO.Dtos;
using TomorrowDAOServer.DAO.Indexer;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace TomorrowDAOServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("Dao")]
[Route("api/app/dao")]
public class DaoController
{
    private readonly IDAOAppService _daoAppService;
    private readonly ILogger<DaoController> _logger;

    public DaoController(IDAOAppService daoAppService, ILogger<DaoController> logger)
    {
        _daoAppService = daoAppService;
        _logger = logger;
    }
    
    [HttpGet("dao-info")]
    public async Task<DAOInfoDto> GetDAOByIdAsync(GetDAOInfoInput input)
    {
        var sw = Stopwatch.StartNew();
        var dto = await _daoAppService.GetDAOByIdAsync(input);
        
        sw.Stop();
        _logger.LogInformation("DaoController GetDAOByIdAsync duration:{0}", sw.ElapsedMilliseconds);
        
        return dto;
    }

    [HttpGet("member-list")]
    public async Task<PageResultDto<MemberDto>> GetMemberListAsync(GetMemberListInput listInput)
    {
        var sw = Stopwatch.StartNew();
        
        var result = await _daoAppService.GetMemberListAsync(listInput);
        sw.Stop();
        _logger.LogInformation("DaoController GetMemberListAsync duration:{0}", sw.ElapsedMilliseconds);
        
        return result;
        
    }
    
    [HttpGet("is-member")]
    public async Task<bool> IsDaoMemberAsync(IsDaoMemberInput input)
    {
        var sw = Stopwatch.StartNew();

        var result = await _daoAppService.IsDaoMemberAsync(input);
        
        sw.Stop();
        _logger.LogInformation("DaoController IsDaoMemberAsync duration:{0}", sw.ElapsedMilliseconds);
        return result;
    }

    [HttpGet("dao-list")]
    public async Task<PagedResultDto<DAOListDto>> GetDAOListAsync(QueryDAOListInput request)
    {
        var sw = Stopwatch.StartNew();
        var result = await _daoAppService.GetDAOListAsync(request);
        
        sw.Stop();
        _logger.LogInformation("DaoController GetDAOListAsync duration:{0}", sw.ElapsedMilliseconds);
        return result;
    }
    
    [HttpGet("bp-list")]
    public async Task<List<string>> BPList([Required]string chainId)
    {
        var sw = Stopwatch.StartNew();
        
        var bpList = await _daoAppService.GetBPList(chainId);
        
        sw.Stop();
        _logger.LogInformation("DaoController BPList duration:{0}", sw.ElapsedMilliseconds);
        return bpList;
    }

    [HttpGet("my-dao-list")]
    [Authorize]
    public async Task<List<MyDAOListDto>> MyDAOList(QueryMyDAOListInput input)
    {
        var sw = Stopwatch.StartNew();
        var result = await _daoAppService.GetMyDAOListAsync(input);
        sw.Stop();
        _logger.LogInformation("DaoController MyDAOList duration:{0}", sw.ElapsedMilliseconds);
        return result;
    }
}