// using Marten.Events.Projections;
// using Microsoft.Extensions.DependencyInjection;
//
// namespace Core.Persistence;
//
// public static class ServiceCollectionExtensions
// {
//     public static void AddPersistence(this IServiceCollection serviceCollection, IProjection[] projections)
//     {
//     }
// }
//
// public static class BusinessProcessStepBuilder
// {
//     public static BusinessStepInfo RequireStep<T>(this BusinessStepInfo bp,
//         Action<BusinessProcessStepConfiguration>? cfg = null)
//     {
//         var bpSteps = bp.BusinessSteps ?? new List<BusinessStepInfo>();
//         var t = typeof(T);
//         var cfgObj = new BusinessProcessStepConfiguration();
//         if (cfg is not null)
//             cfg.Invoke(cfgObj);
//
//         bpSteps.Add(new BusinessStepInfo
//         {
//             Name = t.Name,
//             StepType = t,
//             Order = cfgObj.Order
//         });
//
//         return bp;
//     }
//
//     public static BusinessProcess StepIn<T>(this BusinessProcess bp,
//         Action<BusinessProcessStepConfiguration>? cfg = null)
//     {
//         var bpSteps = bp.BusinessSteps ?? new List<BusinessStepInfo>();
//         var t = typeof(T);
//         var cfgObj = new BusinessProcessStepConfiguration();
//         if (cfg is not null)
//             cfg.Invoke(cfgObj);
//
//         bpSteps.Add(new BusinessStepInfo
//         {
//             Name = t.Name,
//             StepType = t,
//             Order = cfgObj.Order
//         });
//
//
//         return bp;
//     }
// }
//
// public class BusinessProcessBuilder
// {
//     private BusinessProcess busienessProcessInfo;
//
//     public BusinessProcessBuilder BusinessProcesss<T>()
//     {
//         var t = typeof(T);
//         busienessProcessInfo = new BusinessProcess
//         {
//             BusinessProcessName = t.Name,
//             BusinessProcessType = t
//         };
//         return busienessProcessInfo;
//     }
// }
//
// public class BusinessProcess
// {
//     public Type BusinessProcessType { get; set; }
//     public string BusinessProcessName { get; set; }
//
//     public List<BusinessStepInfo> BusinessSteps { get; set; }
// }
//
// public class BusinessProcessConfiguration
// {
// }
//
// public class BusinessProcessStepConfiguration
// {
//     public int Order { get; set; }
// }
//
// public class BusinessStepInfo()
// {
//     public string Name { get; set; }
//     public int Order { get; set; }
//     public Type StepType { get; set; }
//     public List<BusinessStepInfo> BusinessSteps { get; set; }
// }