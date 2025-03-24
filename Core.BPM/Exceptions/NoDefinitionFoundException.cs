namespace Core.BPM.Exceptions;

public class NoDefinitionFoundException(string aggregateName) : Exception($"no definition for '{aggregateName}' found.");