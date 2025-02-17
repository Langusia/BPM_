using Core.BPM.Attributes;
using MediatR;

namespace MyCredo.Features.RecoveringPassword.IdentifyingFace;

public record IdentifiedFace() : BpmEvent;