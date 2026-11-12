using System.Diagnostics.CodeAnalysis;

namespace Trailblazor;

/// <summary>What <see cref="TbTreeSelect{TItem, TKey}"/> and <see cref="TbMultiTreeSelect{TItem, TKey}"/> share: a
/// form field that opens a tree under it, with an optional filter.</summary>
public abstract partial class TbTreeSelectBase<TItem,
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TKey,
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TValue>;
