using Core.BPM.Attributes;

namespace BPM.Client.Features.XProcess;

public record S1Completed(string Name) : BpmEvent;
public record S2Completed(bool Approved) : BpmEvent;
public record S3Completed() : BpmEvent;
public record S4Completed(int Score) : BpmEvent;
public record S5Completed() : BpmEvent;
public record S6Completed() : BpmEvent;
public record S7Completed() : BpmEvent;
