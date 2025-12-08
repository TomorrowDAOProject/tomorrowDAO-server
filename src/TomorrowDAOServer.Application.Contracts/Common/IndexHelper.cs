using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Nest;
using TomorrowDAOServer.Entities;

namespace TomorrowDAOServer.Common;

public class IndexHelper
{
    public static async Task<List<T>> GetAllIndex<T>(Func<QueryContainerDescriptor<T>, QueryContainer> filter, 
        INESTReaderRepository<T, string> repository) 
        where T : AbstractEntity<string>, IIndexBuild, new()
    {
        var res = new List<T>();
        List<T> list;
        var skipCount = 0;
        
        do
        {
            list = (await repository.GetListAsync(filterFunc: filter, skip: skipCount, limit: 5000)).Item2;
            var count = list.Count;
            res.AddRange(list);
            if (list.IsNullOrEmpty() || count < 5000)
            {
                break;
            }
            skipCount += count;
        } while (!list.IsNullOrEmpty());

        return res;
    }
}