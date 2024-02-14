namespace Playground.Application.Documents;

public class User
{
    public Guid Id { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }

    public bool Internal { get; set; }
}