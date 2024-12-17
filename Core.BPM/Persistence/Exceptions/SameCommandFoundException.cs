namespace Core.BPM.Persistence.Exceptions;

public class SameCommandFoundException(string typeName) : Exception($"two or more nodes with command '{typeName}' found on same branch level");