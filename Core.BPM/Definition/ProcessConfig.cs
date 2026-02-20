using Core.BPM.Process;

namespace Core.BPM.Definition;

public class ProcessConfig<T>(BProcess process) where T : Aggregate;
