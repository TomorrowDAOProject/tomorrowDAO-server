namespace TomorrowDAOServer.Monitor.Common;

public static class MonitorConstant
{
    public const string MetricExceptions = "Exceptions";
    public const string MetricOrleansMethodsService = "Orleans_Methods_Service";
    public const string MetricCaServerTransaction = "CAServer_Transaction";
    public const string MetricCaServerEvent = "CAServer_Event";

    public const string LabelHostName = "_Host";
    
    public const string Application = "TMRWDAO";
    public const string LabelApplication = "Application";
    public const string LabelModule = "Module";
    public const string LabelChart = "Chart";
    public const string LabelType = "Type";
    public const string LabelDuration = "Duration";
    public const string Api = "Api";
    public const string Grain = "Grain";
    public const string GraphQl = "GraphQL";
    
    public const string LabelTimestamp = "_Timestamp";
    public const string LabelStartTime = "Start";
    public const string LabelResponseCode = "Code";
    public const string LabelSuccess = "Success";
    public const string LabelCommandName = "Command";
    
    public const string LabelMessage = "Message";
    public const string LabelInterface = "Interface";
    public const string LabelMethod = "Method";


    public const string Unknown = "Unknown";
    
    public const string Biz = "Biz";

    public const string DataTimeFormat = "yyyy-MM-dd HH:mm:ss";

    public const long MaxDuration = 500;
}