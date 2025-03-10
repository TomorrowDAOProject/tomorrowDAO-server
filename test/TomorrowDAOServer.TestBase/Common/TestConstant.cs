
using System;
using AElf.Types;

namespace TomorrowDAOServer.Common;

public static class TestConstant
{
    public const string ChainIdAELF = "AELF";
    public const string ChainIdtDVW = "tDVW";
    public const string ChainIdtDVV = "tDVV";
    public const string ELF = "ELF";
    
    public const string DaoId = "bfbd1a01fd5a931d2fef9ed77146d47b1ba18498cdd300ad6530cd832583152a";
    public const string CustomDaoId = "41e6b23e081bea37f6adf992f03fcf7c9f6f5ee0fafc11f6ea5211c7643ff74e";
    
    public static readonly string PrivateKey1 = Environment.GetEnvironmentVariable("UNITTEST_KEY_01");
    public const string PublicKey1 =
        "04f5db833e5377cab193e3fc663209ac3293ef67736021ee9cebfd1b95a058a5bb400aaeb02ed15dc93177c9bcf38057c4b8069f46601a2180e892a555345c89cf";
    public const string Address1 = "2Md6Vo6SWrJPRJKjGeiJtrJFVkbc5EARXHGcxJoeD75pMSfdN2";
    public static readonly string PrivateKey2 = Environment.GetEnvironmentVariable("UNITTEST_KEY_02");
    public const string PublicKey2 =
        "04de4367b534d76e8586ac191e611c4ac05064b8bc585449aee19a8818e226ad29c24559216fd33c28abe7acaa8471d2b521152e8b40290dfc420d6eb89f70861a";
    public const string Address2 = "2DA5orGjmRPJBCDiZQ76NSVrYm7Sn5hwgVui76kCJBMFJYxQFw";

    public const long LongestChainHeight = 1000;
    public const string LongestChainHash = "c5f2066524b1654d1e391d5003c4365a9b369747f59c4399dcb9d506a3de8e96";
    public const string GenesisBlockHash = "6f395aec3a2a9a6ef2a64ace22ea3d8b8c025f544d3f6cab3c2ea3611454ebf1";
    public const string GenesisContractAddress = "2UKQnHcQvhBT6X6ULtfnuh3b9PVRvVMEroHHkcK4YfcoH1Z1x2";
    public const string LastIrreversibleBlockHash = "8724b42cafd7c758ddfd2647b1910e23c8688c2fa2ce703a2fa056a7370145e8";
    public const long LastIrreversibleBlockHeight = 999;
    public static Hash TransactionHash = Hash.LoadFromHex("20e3a65e0f8c2a70c06d2bee8376293438cf926c0b1974ec2d2aa315fcfab12e");

    public const string TreasuryContractAddress = "3FdTVXDuBMVAsXJ598aTm3GifQQey5ahFsonjhanTLs4qnukT";
    public const string TreasuryAddress = "2Svg2WHtfgd9L2AMTj1gkMatByRgLk88kwyX8H7zL6pejKfL32";
}