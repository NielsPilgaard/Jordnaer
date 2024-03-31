namespace Jordnaer.Components.Account.Shared;

public readonly record struct AlertMessage
{
	public AlertMessage(string message, bool isError = false)
	{
		Message = message;
		IsError = isError;
	}

	public AlertMessage(IEnumerable<string> messages, bool isError = false)
	{
		Message = string.Join(", ", messages);
		IsError = isError;
	}

	public string? Message { get; }

	public bool IsError { get; }
}