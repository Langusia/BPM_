using Core.BPM.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Components.Forms;

namespace Core.BPM.AggregateConditions;

public class ConditionSandbox
{
    public INode RootNode { get; set; }

    public void ValidateFor<TCommand>() where TCommand : IBaseRequest
    {
        
    }
}