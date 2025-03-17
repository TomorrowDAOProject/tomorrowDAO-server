using System;
using System.Collections.Generic;

namespace TomorrowDAOServer.ResourceToken.Dtos;

public class TurnoverDto
{
    public DateTime Date { get; set; }
    public string Volume { get; set; }
    public List<string> Prices { get; set; }
}