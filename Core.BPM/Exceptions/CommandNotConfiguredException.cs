using System;

namespace Core.BPM.Exceptions;

public class CommandNotConfiguredException : Exception
{
    private CommandNotConfiguredException(string processName, string commandName) : base(
        $"Command with name '{commandName}' for '{processName}' process  is not configured")
    {
    }


    public static CommandNotConfiguredException For<TP, TC>() =>
        new CommandNotConfiguredException(typeof(TP).Name, typeof(TC).Name);

    public static CommandNotConfiguredException For(Type processType, Type commandType) =>
        new CommandNotConfiguredException(processType.Name, commandType.Name);
}