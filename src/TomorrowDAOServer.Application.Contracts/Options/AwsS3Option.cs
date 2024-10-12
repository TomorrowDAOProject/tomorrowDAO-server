namespace TomorrowDAOServer.Options;

public class AwsS3Option
{
    public string AccessKeyID { get; set; }
    public string SecretKey { get; set; }
    public string BucketName { get; set; }
    public string S3Key { get; set; } = "DAO";
    public string ServiceURL { get; set; }
}