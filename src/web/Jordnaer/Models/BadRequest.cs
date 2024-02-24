namespace Jordnaer.Models;

internal readonly ref struct BadRequest(ReadOnlySpan<char> error)
{
	public ReadOnlySpan<char> Error { get; } = error;
}