namespace TomorrowDAOServer.Entities;

public class TransactionInfo
{
    public string? ChainId { get; set; }
    public string? TransactionId { get; set; }
    public string? From { get; set; }
    public string? To { get; set; }
    public string? MethodName { get; set; }
    public bool IsAAForwardCall { get; set; }
    public string? PortKeyContract { get; set; }
    public string? CAHash { get; set; }
    public string? RealTo { get; set; }
    public string? RealMethodName { get; set; }
}