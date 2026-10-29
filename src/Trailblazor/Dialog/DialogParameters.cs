using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNetCore.Components;

namespace Trailblazor;

/// <summary>Typed parameters for a dialog component, set with <see cref="Set{TValue}"/>.</summary>
public sealed class DialogParameters<TDialog>
{
    internal Dictionary<string, object?> Values { get; } = [];

    /// <summary>Sets one of the dialog's parameters, named by a property expression such as
    /// <c>d => d.Entity</c>.</summary>
    public DialogParameters<TDialog> Set<TValue>(Expression<Func<TDialog, TValue>> parameter, TValue value)
    {
        if (parameter.Body is not MemberExpression { Expression: ParameterExpression } member
            || member.Member.GetCustomAttribute<ParameterAttribute>() is null)
        {
            throw new ArgumentException("Name one of the dialog's own [Parameter] properties, such as d => d.Entity.",
                nameof(parameter));
        }

        Values[member.Member.Name] = value;
        return this;
    }
}
