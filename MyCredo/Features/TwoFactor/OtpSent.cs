﻿using Core.BPM;
using Core.BPM.Application.Events;
using Core.BPM.Attributes;

namespace MyCredo.Features.TwoFactor;

public record OtpSent(Guid ParentProcessId, string OtpHash) : BpmEvent;