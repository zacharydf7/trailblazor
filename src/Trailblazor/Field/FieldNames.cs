using System.Linq.Expressions;

namespace Trailblazor;

internal static class FieldNames
{
    // The name a static-SSR form post binds back to; only plain member chains such as Model.Address.City get one.
    public static string? From(LambdaExpression? expression)
    {
        var members = new List<string>();
        var node = expression?.Body;
        while (node is MemberExpression member)
        {
            members.Add(member.Member.Name);
            node = member.Expression;
        }

        if (node is not ConstantExpression || members.Count == 0)
        {
            return null;
        }

        members.Reverse();
        return string.Join('.', members);
    }
}
