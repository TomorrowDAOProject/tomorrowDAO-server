using Google.Type;

namespace TomorrowDAOServer.ResourceToken.Dtos;

public class RecordDto
{
    public string Tx_id { get; set; }
    public string Address { get; set; }
    public string Method { get; set; }
    public string Type { get; set; }
    public string Resource { get; set; }
    public string Elf { get; set; }
    public string Fee { get; set; }
    public string Chain_id { get; set; }
    public string Block_height { get; set; }
    public string Tx_status { get; set; }
    public DateTime Time { get; set; }
}