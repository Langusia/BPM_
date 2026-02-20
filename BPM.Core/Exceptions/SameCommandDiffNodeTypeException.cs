using System;

namespace BPM.Core.Exceptions;

public class SameCommandDiffNodeTypeException(string typeName) : Exception($"different nodeType for command '{typeName}' found");