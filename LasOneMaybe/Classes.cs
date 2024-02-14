using System.Linq.Expressions;
using Core.BPM;
using LasOneMaybe.Documents;

namespace LasOneMaybe;





public class ProcessorDefinition<T>
{
    public void Define()
    {
        new BpmNode<Registration, CheckClientType>()
            .AppendRight<CheckClientType2>(x => x.clientId == 0)
            .AppendRight<CheckClientType3>()
            .ThenAppendRight<CheckClientType2>();
    }
}