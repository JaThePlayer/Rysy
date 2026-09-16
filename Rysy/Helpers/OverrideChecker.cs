using System.Reflection;
using MemberTypes = System.Reflection.MemberTypes;

namespace Rysy.Helpers;

/// <summary>
/// Allows for easily checking whether a derived type overrides a given member.
/// </summary>
public sealed class OverrideChecker {
    private const BindingFlags Flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;
    
    private readonly IReadOnlySet<Type> _baseTypes;
    private readonly string _memberName;
    private readonly MemberTypes _memberType;

    private OverrideChecker(IReadOnlySet<Type> baseTypes, string memberName, MemberTypes memberType) {
        _baseTypes = baseTypes;
        _memberName = memberName;
        _memberType = memberType;
    }
    
    public static OverrideChecker GetFor(HashSet<Type> baseTypes, string memberName, MemberTypes memberType) {
        return new OverrideChecker(baseTypes, memberName, memberType);
    }
    
    public bool IsMemberOverridenIn(Type? extendingType) {
        if (extendingType is null || _baseTypes.Contains(extendingType))
            return false;

        var actualMemberDeclaringType = extendingType.GetMember(_memberName, _memberType, Flags).Single().DeclaringType;
        
        return actualMemberDeclaringType is not null && !_baseTypes.Contains(actualMemberDeclaringType);
    }
}
