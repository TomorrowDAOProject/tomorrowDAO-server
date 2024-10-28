using System;

namespace TomorrowDAOServer.User.Dtos;

public class MyPointsDto
{
    public string Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public long Points { get; set; }
    public string PointsType { get; set; }
    public DateTime PointsTime { get; set; }
}