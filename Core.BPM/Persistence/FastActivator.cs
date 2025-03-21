using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace Core.BPM.Persistence;

public static class FastActivator
{
     private static readonly ConcurrentDictionary<Type, Func<object>> _activators = new();
 
     public static object CreateAggregate(Type aggregateType)
     {
         return _activators.GetOrAdd(aggregateType, CreateFactory).Invoke();
     }
 
     private static Func<object> CreateFactory(Type type)
     {
         var ctor = type.GetConstructor(Type.EmptyTypes);
         if (ctor == null)
         {
             throw new InvalidOperationException($"Aggregate type {type.Name} must have a parameterless constructor.");
         }
 
         var newExpr = Expression.New(ctor);
         var lambda = Expression.Lambda<Func<object>>(newExpr).Compile();
         return lambda;
     }
 }