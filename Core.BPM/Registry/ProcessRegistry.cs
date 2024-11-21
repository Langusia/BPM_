using System.Linq.Expressions;
using System.Reflection;

namespace Core.BPM.Registry;

public class ProcessRegistry
{
    private readonly Dictionary<Type, Dictionary<Type, Action<object, object>>> _applyMethodCache = new();

    public void RegisterAggregate(Type aggregateType)
    {
        var applyMethods = new Dictionary<Type, Action<object, object>>();

        // Find all Apply(EventType) methods
        var methods = aggregateType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(m => m.Name == "Apply" && m.GetParameters().Length == 1);

        foreach (var method in methods)
        {
            var eventType = method.GetParameters()[0].ParameterType;

            // Create a delegate for the Apply method
            var applyDelegate = CreateApplyDelegate(aggregateType, eventType, method);
            applyMethods[eventType] = applyDelegate;
        }

        _applyMethodCache[aggregateType] = applyMethods;
    }

    public Action<object, object> GetApplyMethod(Type aggregateType, Type eventType)
    {
        if (!_applyMethodCache.TryGetValue(aggregateType, out var eventMethods))
        {
            throw new InvalidOperationException($"No methods registered for aggregate type {aggregateType.Name}.");
        }

        if (!eventMethods.TryGetValue(eventType, out var applyMethod))
        {
            throw new InvalidOperationException($"No Apply method found for event type {eventType.Name} on aggregate {aggregateType.Name}.");
        }

        return applyMethod;
    }

    private Action<object, object> CreateApplyDelegate(Type aggregateType, Type eventType, MethodInfo methodInfo)
    {
        // Parameters: (object aggregate, object @event)
        var aggregateParam = Expression.Parameter(typeof(object), "aggregate");
        var eventParam = Expression.Parameter(typeof(object), "event");

        // Convert parameters to their actual types
        var typedAggregate = Expression.Convert(aggregateParam, aggregateType);
        var typedEvent = Expression.Convert(eventParam, eventType);

        // Call the Apply method
        var methodCall = Expression.Call(typedAggregate, methodInfo, typedEvent);

        // Compile the expression into a delegate
        return Expression.Lambda<Action<object, object>>(methodCall, aggregateParam, eventParam).Compile();
    }
}