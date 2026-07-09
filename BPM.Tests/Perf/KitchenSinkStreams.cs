using BPM.Core.Configuration;
using BPM.Core.Events;
using BPM.Core.Nodes;
using BPM.Core.Process;

namespace BPM.Tests.Perf;

/// <summary>
/// Generates correctly stamped event streams for the kitchen-sink process.
/// Events get their NodeId set to the producing node's NodeLevel, exactly as
/// Process.AppendEvents does at runtime — otherwise AnyTime/node matching
/// (ContainsNodeEvent) silently fails and the graph looks incomplete.
///
/// The happy path drives the DEEP branch (Amount > 10_000) so the expensive
/// shape — nested group inside a conditional plus the guest process — is what
/// gets measured. Streams are padded to the requested length with repeated
/// AnyTime (RatesRefreshed) events, which is the graph-legal way to grow a
/// stream arbitrarily without violating step order.
///
/// The generated stream stops one step short of completion: CloseCase is the
/// single remaining mainline command, so benches/tests execute it repeatedly
/// against a stable stream.
/// </summary>
public static class KitchenSinkStreams
{
    /// <summary>Happy-path stream padded to <paramref name="targetLength"/> events, ready to close.</summary>
    public static List<BpmEvent> ReadyToClose(int targetLength = 0)
    {
        var main = BProcessGraphConfiguration.GetConfig(nameof(KitchenSink))
                   ?? throw new InvalidOperationException("KitchenSink process not registered.");
        var guest = BProcessGraphConfiguration.GetConfig(nameof(ComplianceReview))
                    ?? throw new InvalidOperationException("ComplianceReview process not registered.");

        int MainLevel<TCommand>() => LevelOf(main, typeof(TCommand));
        int GuestLevel<TCommand>() => LevelOf(guest, typeof(TCommand));

        var events = new List<BpmEvent>
        {
            Stamp(new CaseOpened(50_000m), MainLevel<OpenCase>()),
            Stamp(new IdDocUploaded(), MainLevel<UploadIdDoc>()),
            Stamp(new IncomeDocUploaded(), MainLevel<UploadIncomeDoc>()),
            Stamp(new DeepCheckPassed(), MainLevel<DeepCheck>()),
            Stamp(new IncomeVerified(), MainLevel<VerifyIncome>()),
            Stamp(new CollateralVerified(), MainLevel<VerifyCollateral>()),
            Stamp(new OfferPrepared(), MainLevel<PrepareOffer>()),
            // Nested-configure section: Amount 50k also wins the inner
            // If (> 30_000), so the vip branch is the driven path. The
            // UnlockOptional (RequestDiscount) is deliberately NOT executed —
            // optional nodes must not block branch completion.
            Stamp(new CustomerNotified(), MainLevel<NotifyCustomer>()),
            Stamp(new FollowUpScheduled(), MainLevel<ScheduleFollowUp>()),
            // Level-2 fork (parallel group) — both legs, then the level-3 gate:
            Stamp(new DocsPackPrepared(), MainLevel<PrepareDocsPack>()),
            Stamp(new SlotReserved(), MainLevel<ReserveSlot>()),
            Stamp(new PlanConfirmed(), MainLevel<ConfirmPlan>()),
            Stamp(new ManagerAssigned(), MainLevel<AssignManager>()),
            // Guest process, appended to the same stream:
            Stamp(new ReviewOpened(), GuestLevel<OpenReview>()),
            Stamp(new ReviewApproved(), GuestLevel<ApproveReview>()),
        };

        var anyTimeLevel = MainLevel<RefreshRates>();
        while (events.Count < targetLength)
            events.Add(Stamp(new RatesRefreshed(), anyTimeLevel));

        return events;
    }

    private static int LevelOf(BProcess config, Type commandType) =>
        DeepNodes(config.RootNode, new HashSet<INode>())
            .First(n => n.CommandType == commandType)
            .NodeLevel;

    /// <summary>
    /// NodeBase.GetAllNodes() only follows NextSteps; it never descends into
    /// GroupNode.SubRootNodes or ConditionalNode.IfNodeRoots/ElseNodeRoots.
    /// The kitchen-sink graph keeps commands inside those containers, so the
    /// stream generator needs its own exhaustive walk.
    /// </summary>
    private static IEnumerable<INode> DeepNodes(INode node, HashSet<INode> seen)
    {
        if (node is null || !seen.Add(node))
            yield break;

        yield return node;

        switch (node)
        {
            case GroupNode group:
                foreach (var sub in group.SubRootNodes)
                foreach (var n in DeepNodes(sub, seen))
                    yield return n;
                break;
            case ConditionalNode cond:
                foreach (var root in cond.IfNodeRoots ?? [])
                foreach (var n in DeepNodes(root, seen))
                    yield return n;
                foreach (var root in cond.ElseNodeRoots ?? [])
                foreach (var n in DeepNodes(root, seen))
                    yield return n;
                break;
        }

        foreach (var next in node.NextSteps ?? [])
        foreach (var n in DeepNodes(next, seen))
            yield return n;
    }

    private static BpmEvent Stamp(BpmEvent e, int nodeLevel)
    {
        e.NodeId = nodeLevel;
        return e;
    }
}
