using System;

namespace Core.BPM.Application.Exceptions;

public class BpmProcessNotFoundException : Exception
{
    private BpmProcessNotFoundException(string typeName, string id) : base($"{typeName} with id '{id}' was not found")
    {
    }

    public static BpmProcessNotFoundException For<T>(Guid id) =>
        For<T>(id.ToString());

    public static BpmProcessNotFoundException For<T>(string id) =>
        new BpmProcessNotFoundException(typeof(T).Name, id);
}