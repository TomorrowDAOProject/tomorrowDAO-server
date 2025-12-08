using Shouldly;
using TomorrowDAOServer.Common;
using Xunit;
using Xunit.Abstractions;

namespace TomorrowDAOServer.Application.Contracts.Tests.Common;

public class EncryptionHelperTest : TomorrowDaoServerApplicationContractsTestsBase
{
    private readonly string _plainText = "PlainText";
    private readonly string _password = "Password";
    private readonly string _base64 = "pjsD/bEeNJA+KB3229cQPx21sDDV6+LCgTM+gFv2tdM=";
    private readonly string _hex = "a63b03fdb11e34903e281df6dbd7103f1db5b030d5ebe2c281333e805bf6b5d3";

    public EncryptionHelperTest(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void EncryptBase64Test()
    {
        var encryptBase64 = EncryptionHelper.EncryptBase64(_plainText, _password);
        encryptBase64.ShouldNotBeNull();
        encryptBase64.ShouldBe("pjsD/bEeNJA+KB3229cQPx21sDDV6+LCgTM+gFv2tdM=");
    }

    [Fact]
    public void DecryptFromBase64Test()
    {
        var fromBase64 = EncryptionHelper.DecryptFromBase64(_base64, _password);
        fromBase64.ShouldNotBeNull();
        fromBase64.ShouldBe(_plainText);
    }

    [Fact]
    public void EncryptHexTest()
    {
        var encryptHex = EncryptionHelper.EncryptHex(_plainText, _password);
        encryptHex.ShouldNotBeNull();
        encryptHex.ShouldBe(_hex);
    }
    
    [Fact]
    public void DecryptFromHexTest()
    {
        var decryptFromHex = EncryptionHelper.DecryptFromHex(_hex, _password);
        decryptFromHex.ShouldNotBeNull();
        decryptFromHex.ShouldBe(_plainText);
    }
}