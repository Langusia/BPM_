using System.Runtime.CompilerServices;

// BpmProducer and BpmEvent moved to BPM.Contracts (so command assemblies can
// compile without the engine) keeping their original namespaces. Forwarders
// preserve binary compatibility for assemblies compiled against older BPM.Core.
[assembly: TypeForwardedTo(typeof(BPM.Core.Attributes.BpmProducer))]
[assembly: TypeForwardedTo(typeof(BPM.Core.Events.BpmEvent))]
