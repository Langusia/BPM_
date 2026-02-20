using System;

namespace BPM.Core.Exceptions;

public class NoDefinitionFoundException(string aggregateName) : Exception($"no definition for '{aggregateName}' found.");