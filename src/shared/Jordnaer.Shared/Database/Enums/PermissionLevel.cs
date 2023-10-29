using NetEscapades.EnumGenerators;

namespace Jordnaer.Shared;

[EnumExtensions]
[Flags]
public enum PermissionLevel
{
    None = 1,
    Read = 2,
    Write = 4,
    Moderator = 8,
    Admin = 16
}
