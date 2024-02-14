using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using Marten.Events.Aggregation;
using Playground.Application.Documents.Onboarding;

namespace Playground.Application.Documents.Registration;




public class Registration
{
    public Registration(CheckedClientType initEvent)
    {
        ClientType = initEvent.ClientType;
    }

    public Guid Id { get; set; }
    public ClientType ClientType { get; set; }
    public bool TwoFactorValidated { get; set; }
    public bool RequestedPhoneChange { get; set; }

    public string RegistrationParameter
    {
        get;
        set
        ;
    }

    public bool Registered { get; set; }
}

public record RegistrationParameterAdded(string RegistrationParameter);

public record RegistrationFinished(bool registered);

public class RegistrationProjection : SingleStreamProjection<Registration>
{
    public void Apply(Checked2FA @event, Registration snapshot)
    {
        snapshot.TwoFactorValidated = @event.IsValid;
    }

    public void Apply(CheckedClientType @event, Registration snapshot)
    {
        snapshot.ClientType = @event.ClientType;
    }

    public void Apply(RegistrationParameterAdded @event, Registration snapshot)
    {
        snapshot.RegistrationParameter = @event.RegistrationParameter;
    }
}

public class RegistrationProcessBuilder
{
    public void DoGraph()
    {
        var pr = new ProcessMap<Registration>();
        pr.Add(typeof(CheckedClientType), typeof(Checked2FA));
    }


    public class Node<TStep>
    {
        private string name = typeof(TStep).Name;
    }

    public class NodePath<TOriginiNode, TNextNode> : NodeMetadata
    {
        public NodePath() : base(typeof(TOriginiNode), typeof(TNextNode))
        {
        }
//node<CheckClientType>.Following()
        public Node<TOriginiNode> Origin;
        public Node<TNextNode> Next;
        public Expression<Func<TOriginiNode, bool>> Condition;
    }

    public class NodeMetadata
    {
        public NodeMetadata(Type fromType, Type toType)
        {
            FromType = fromType;
            ToType = toType;
        }

        public Type FromType { get; set; }
        public Type ToType { get; set; }
    }

    public class Node2
    {
        public Node2(Type nodeType)
        {
            name = nodeType.Name;
        }

        private string name;
    }

    public class Node2<TStep> : Node2
    {
        public Node2() : base(typeof(TStep))
        {
        }
    }


    public class Node2Path
    {
        public Node2Path(Node2 origin, Node2 next)
        {
            origin = this.origin;
            nextNode = this.nextNode;
        }

        private Node2 origin;
        private Node2 nextNode;
    }

    public class ProcessMap<TProcess>
    {
        private Dictionary<NodeMetadata, List<NodeMetadata>> map;
        private Dictionary<Node2, List<Node2Path>> map2;
        private Dictionary<Node2, Dictionary<Expression<Func<TProcess, bool>>, List<Node2Path>>> map3;

        //public void Add(Type keyNode, Type nextNode, Expression<Func<TProcess, bool>> exp = null)
        //{
        //    if (map is null)
        //    {
        //        map = new Dictionary<NodeMetadata, List<NodeMetadata>>();
        //    }
        //
        //    var keyMetaData = new NodeMetadata(keyNode, nextNode);
        //    map.Add(keyMetaData);
        //}

        public void Add(Type keyNode, params Type[] nextNodes)
        {
            if (map is null)
            {
                map = new Dictionary<NodeMetadata, List<NodeMetadata>>();
            }

            if (map2 is null)
            {
                map2 = new Dictionary<Node2, List<Node2Path>>();
            }

            var currentNode = new Node2(keyNode);
            var expectedNextNodes = new List<Node2Path>();
            foreach (var nextNode in nextNodes)
            {
                expectedNextNodes.Add(new Node2Path(currentNode, new Node2(keyNode)));
            }

            map2.Add(currentNode, expectedNextNodes);
        }
    }
}

public static class ProcessMapExtensions
{
    public static void AppendNodeMap<TOrigin, TNext>(this ProcessMap map, Node<TOrigin> node, Node<TNext> next)
    {
    }
}

public RegistrationProcessBuilder()
{
    /*
    BusinessProcessBuilder.BusinessProcesss<RegisterUser>()
        .Step<CheckClientType>() -> RegisterUser.ClientType
        .Step<OTP>
        .IfAggregateState(x=> x.ClientType == ClientType.FullMyCredo)
            .Step<OTP>
            .Step<RequestedPhoneChange>()
                .IncludeStep<FR>
                .IncludeStep<OTP>
                .IncludeStep<ChangeMobile>
            .Step<SecurityCheck>()
        .ElseIfState(x=> x.ClientType == ClientType.NoMyCredo)

    */
}

}