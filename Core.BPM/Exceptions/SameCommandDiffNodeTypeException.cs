using System;

namespace Core.BPM.Exceptions;

public class SameCommandDiffNodeTypeException(string typeName) : Exception($"different nodeType for command '{typeName}' found");