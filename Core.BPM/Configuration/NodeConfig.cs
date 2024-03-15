namespace Core.BPM.Configuration;

public class NodeConfig<TProcess>
{
    public int? CommandTryCount { get; set; }
    public Predicate<TProcess>? Condition { get; set; }
}