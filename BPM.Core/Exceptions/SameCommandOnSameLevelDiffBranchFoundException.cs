using System;

namespace BPM.Core.Exceptions;

public class SameCommandOnSameLevelDiffBranchFoundException(string typeName)
    : Exception($"two or more commands '{typeName}' found on same level of different branches consider splitting root branch after");