using Core.BPM.Interfaces;

namespace Playground.Presentation.Registration.Commands.EnrollingKYC;

public record EnrolledKYC(string PersonId, bool KYCParam1, string KYCParam2, int KYCParam3) : IBpmEvent;