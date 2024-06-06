namespace MyCredo.Retail.Loan.Application.Features.RequestLoanProcess.CarPawnshop;

public record AggregateResult<T>
{
    public Guid ProcessId { get; set; }
    public List<string> NextNodes { get; set; }
    public T Data { get; set; }
}
