using BPM.Core.Process;

namespace BPM.Core.Definition;

public class ProcessConfig<T>(BProcess process) where T : Aggregate;
