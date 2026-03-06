using System;

namespace BPM.Core.Attributes;

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
public class BpmInternalAttribute : Attribute { }
