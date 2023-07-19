using Jordnaer.Shared.Contracts;

namespace Jordnaer.Shared;
public readonly record struct SetChildProfilePicture(ChildProfile ChildProfile, byte[] FileBytes);
