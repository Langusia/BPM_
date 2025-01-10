namespace Core.BPM.Exceptions;

public class SameCommandForBranchFoundException(params string[] sameCommandNames)
    : Exception($"commands named '{string.Join(',', sameCommandNames)}' are put on same branch. consider using anyTime node");