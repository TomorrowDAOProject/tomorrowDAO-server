using System.Text;
using Orleans;
using Serilog;
using TomorrowDAOServer.Grains.State.Sequence;

namespace TomorrowDAOServer.Grains.Grain.Sequence;

public interface ISequenceGrain : IGrainWithStringKey
{
    Task<string> GetNextValAsync();
    Task<List<string>> GetNextValAsync(int batchSize);
}

public class SequenceGrain : Grain<SequenceState>, ISequenceGrain
{
    private static readonly char[] Characters = new char[]
    {
        '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l',
        'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H',
        'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z'
    };

    public override Task OnActivateAsync()
    {
        ReadStateAsync();
        return base.OnActivateAsync();
    }

    public override async Task OnDeactivateAsync()
    {
        await WriteStateAsync();
        await base.OnDeactivateAsync();
    }

    public async Task<string> GetNextValAsync()
    {
        var currentValue = State.Sequence;
        currentValue++;
        var value = currentValue;
        var sb = new StringBuilder();
        while (value > 0)
        {
            var index = (int)(value % Characters.Length);
            sb.Insert(0, Characters[index].ToString());
            value /= Characters.Length;
        }
        State.Sequence = currentValue;
        await WriteStateAsync();

        return new string(sb.ToString().Reverse().ToArray());
    }

    public async Task<List<string>> GetNextValAsync(int batchSize)
    {
        if (batchSize <= 0)
        {
            return new List<string>();
        }

        var currentValue = State.Sequence;
        Log.Information("CurrentValue: {0}, batchSize={1}", currentValue, batchSize);
        var res = new List<string>();
        for (var i = 0; i < batchSize; i++)
        {
            ++currentValue;

            var value = currentValue;
            var sb = new StringBuilder();
            while (value > 0)
            {
                var index = (int)(value % Characters.Length);
                sb.Append(Characters[index]);
                value /= Characters.Length;
            }
            res.Add(new string(sb.ToString().Reverse().ToArray()));
        }
        
        State.Sequence = currentValue;
        await WriteStateAsync();

        return res;
    }
}