using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;
using MudBlazor.Utilities;

namespace Jordnaer.Components;
public partial class ScrollToBottom : IDisposable
{
	private IScrollListener? _scrollListener;

	protected string Classname =>
		new CssBuilder("mud-scroll-to-top")
			.AddClass("visible", Visible && string.IsNullOrWhiteSpace(VisibleCssClass))
			.AddClass("hidden", !Visible && string.IsNullOrWhiteSpace(HiddenCssClass))
			.AddClass(VisibleCssClass, Visible && !string.IsNullOrWhiteSpace(VisibleCssClass))
			.AddClass(HiddenCssClass, !Visible && !string.IsNullOrWhiteSpace(HiddenCssClass))
			.AddClass(Class)
			.Build();

	[Inject]
	private IScrollListenerFactory ScrollListenerFactory { get; set; } = null!;

	[Inject]
	private IScrollManager ScrollManager { get; set; } = null!;

	[Parameter]
	[Category(CategoryTypes.ScrollToTop.Behavior)]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>
	/// The CSS selector to which the scroll event will be attached
	/// </summary>
	[Parameter]
	[Category(CategoryTypes.ScrollToTop.Behavior)]
	public string? Selector { get; set; }

	/// <summary>
	/// If set to true, it starts Visible. If sets to false, it will become visible when the MinimumBottomOffset amount of scrolled pixels is reached
	/// </summary>
	[Parameter]
	[Category(CategoryTypes.ScrollToTop.Behavior)]
	public bool Visible { get; set; }

	/// <summary>
	/// CSS class for the Visible state. Here, apply some transitions and animations that will happen when the component becomes visible
	/// </summary>
	[Parameter]
	[Category(CategoryTypes.ScrollToTop.Appearance)]
	public string? VisibleCssClass { get; set; }

	/// <summary>
	/// CSS class for the Hidden state. Here, apply some transitions and animations that will happen when the component becomes invisible
	/// </summary>
	[Parameter]
	[Category(CategoryTypes.ScrollToTop.Appearance)]
	public string? HiddenCssClass { get; set; }

	/// <summary>
	/// The distance in pixels scrolled from the bottom of the selected element from which 
	/// the component becomes visible
	/// </summary>
	[Parameter]
	[Category(CategoryTypes.ScrollToTop.Behavior)]
	public int MinimumBottomOffset { get; set; } = 900;

	/// <summary>
	/// Smooth or Auto
	/// </summary>
	[Parameter]
	[Category(CategoryTypes.ScrollToTop.Behavior)]
	public ScrollBehavior ScrollBehavior { get; set; } = ScrollBehavior.Smooth;

	/// <summary>
	/// Called when scroll event is fired
	/// </summary>
	[Parameter]
	public EventCallback<ScrollEventArgs> OnScroll { get; set; }

	[Parameter]
	public EventCallback<MouseEventArgs> OnClick { get; set; }

	protected override void OnAfterRender(bool firstRender)
	{
		if (firstRender)
		{

			var selector = !string.IsNullOrWhiteSpace(Selector)
							   ? Selector
							   : null; // null is defaulted to document element in JS function

			_scrollListener = ScrollListenerFactory.Create(selector);

			//subscribe to event
			_scrollListener.OnScroll += ScrollListener_OnScroll;
		}
	}

	/// <summary>
	/// event received when scroll in the selected element happens
	/// </summary>
	/// <param name="sender">ScrollListener instance</param>
	/// <param name="e">Information about the position of the scrolled element</param>
	private async void ScrollListener_OnScroll(object? sender, ScrollEventArgs e)
	{
		await OnScroll.InvokeAsync(e);

		var distanceFromBottom = e.ScrollHeight - e.ScrollTop;

		if (distanceFromBottom >= MinimumBottomOffset && Visible != true)
		{
			Visible = true;
			await InvokeAsync(StateHasChanged);
		}

		if (distanceFromBottom < MinimumBottomOffset && Visible)
		{
			Visible = false;
			await InvokeAsync(StateHasChanged);
		}
	}

	/// <summary>
	/// Scrolls to top when clicked and invokes OnClick
	/// </summary>
	private async Task OnButtonClick(MouseEventArgs args)
	{
		if (_scrollListener?.Selector is not null)
		{
			await ScrollManager.ScrollToBottomAsync(_scrollListener.Selector, ScrollBehavior);
			await OnClick.InvokeAsync(args);
		}
	}

	/// <summary>
	/// Remove the event
	/// </summary>
	public void Dispose()
	{
		GC.SuppressFinalize(this);

		if (_scrollListener is null)
		{
			return;
		}

		_scrollListener.OnScroll -= ScrollListener_OnScroll;
		_scrollListener.Dispose();
	}
}