using Core.BPM.BCommand;
using MediatR;

namespace MyCredo.Features.RecoveringPassword.IdentifyingFace;

public record IdentifiedFace() : BpmEvent;