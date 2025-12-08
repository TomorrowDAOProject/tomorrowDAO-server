using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using TomorrowDAOServer.Common;
using TomorrowDAOServer.Entities;
using Volo.Abp.DependencyInjection;

namespace TomorrowDAOServer.File.Provider;

public interface IFileUploadProvider
{
    Task AddOrUpdateAsync(string chainId, string address, string url);
}

public class FileUploadProvider : IFileUploadProvider, ISingletonDependency
{
    private readonly INESTRepository<FileUploadIndex, string> _fileUploadRepository;

    public FileUploadProvider(INESTRepository<FileUploadIndex, string> fileUploadRepository)
    {
        _fileUploadRepository = fileUploadRepository;
    }

    public async Task AddOrUpdateAsync(string chainId, string address, string url)
    {
        await _fileUploadRepository.AddOrUpdateAsync(new FileUploadIndex
        {
            Id = GuidHelper.GenerateGrainId(chainId, address, url),
            ChainId = chainId, Uploader = address, Url = url, CreateTime = DateTime.UtcNow
        });
    }
}