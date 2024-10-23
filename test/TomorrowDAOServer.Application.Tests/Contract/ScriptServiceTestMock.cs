using System;
using System.Collections.Generic;
using Google.Protobuf;
using Moq;
using TomorrowDAOServer.Contract.Dto;
using TomorrowDAOServer.Enums;

namespace TomorrowDAOServer.Contract;

public partial class ScriptServiceTest
{
    public static ITransactionService MockTransactionService()
    {
        var mock = new Mock<ITransactionService>();

        mock.Setup(m => m.CallTransactionAsync<GetCurrentMinerPubkeyListDto>(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new GetCurrentMinerPubkeyListDto
            {
                Pubkeys = new List<string>() {PublicKey1, PublicKey2}
            });
        
        mock.Setup(m => m.CallTransactionAsync<GetCurrentMinerListWithRoundNumberDto>(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new GetCurrentMinerListWithRoundNumberDto
            {
                Pubkeys = new List<string>()
                {
                    PublicKey1,
                    PublicKey2
                },
                RoundNumber = 10
            });
        
        mock.Setup(m => m.CallTransactionAsync<GetVictoriesDto>(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IMessage>()))
            .ReturnsAsync(new GetVictoriesDto
            {
                Value = new  List<string>() {Address1, Address2}
            });
        
        mock.Setup(m => m.CallTransactionAsync<GetProposalInfoDto>(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IMessage>()))
            .ReturnsAsync(new GetProposalInfoDto
            {
                ProposalStatus = ProposalStatus.Approved.ToString(),
                ProposalStage = ProposalStage.Active.ToString(),
            });


        return mock.Object;
    }
}