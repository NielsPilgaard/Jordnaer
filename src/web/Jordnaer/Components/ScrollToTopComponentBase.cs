using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Jordnaer.Components;

public class ScrollToTopComponentBase : ComponentBase, IDisposable, IAsyncDisposable
{
	[Inject]
	private IScrollManager ScrollManager { get; set; } = null!;

	private CancellationTokenSource? _cancellationTokenSource;

	protected CancellationToken CancellationToken => (_cancellationTokenSource ??= new CancellationTokenSource()).Token;

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		await ScrollManager.ScrollToTopAsync("html");
	}

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

	public async ValueTask DisposeAsync()
	{
		GC.SuppressFinalize(this);

		if (_cancellationTokenSource is null)
		{
			return;
		}

		await _cancellationTokenSource.CancelAsync();
		_cancellationTokenSource.Dispose();
		_cancellationTokenSource = null;
	}
}