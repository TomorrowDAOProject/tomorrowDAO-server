using System;
using Nest;

namespace TomorrowDAOServer.DAO;

public class FileInfo
{
    public File File { get; set; }
    [Keyword] public string Uploader { get; set; }
    public DateTime UploadTime { get; set; }
}

public class File
{
    [Keyword] public string Name { get; set; }
    [Keyword] public string Cid { get; set; }
    [Keyword] public string Url { get; set; }
}