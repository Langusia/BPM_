using BPM.Core.Attributes;
using BPM.Core.Events;
using MediatR;

namespace MyCredo.Features.RecoveringPassword.IdentifyingFace;

public record IdentifiedFace() : BpmEvent;