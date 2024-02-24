using Microsoft.AspNetCore.Components;

namespace Jordnaer.Components;

public class CancellableComponent : ComponentBase, IDisposable
{
	private CancellationTokenSource? _cancellationTokenSource;

	protected CancellationToken CancellationToken => (_cancellationTokenSource ??= new CancellationTokenSource()).Token;

	public void Dispose()
	{
		GC.SuppressFinalize(this);

		if (_cancellationTokenSource is null)
		{
			return;
		}

		_cancellationTokenSource.Cancel();
		_cancellationTokenSource.Dispose();
		_cancellationTokenSource = null;
	}
}