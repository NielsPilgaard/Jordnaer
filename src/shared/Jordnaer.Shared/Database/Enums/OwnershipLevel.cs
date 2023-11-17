using NetEscapades.EnumGenerators;

namespace Jordnaer.Shared;

[EnumExtensions]
public enum OwnershipLevel
{
    None = 0,
    Member = 1,
    InheritsOwnership = 2,
    Owner = 3
}
