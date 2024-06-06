namespace MyCredo.Retail.Loan.Application.Features.RequestLoanProcess.CarPawnshop.Initiating;

public record RequestLoanInitiateResponse
{
    public Guid ProcessId { get; set; }
    public List<string> NextNodes { get; set; }
    public bool RequiresAuthentification { get; set; }
    //public Status Status { get; set; }
}
