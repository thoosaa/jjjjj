using Client.Pages;
using Microsoft.JSInterop;
using System.Reflection;

namespace Client.Services
{
	public class PageJsInvokeService : IPageJsInvokeService, IDisposable
	{
		private readonly IJSRuntime _jsRuntime;
		private bool _inited = false;
		private bool _disposed = false;

		public PageJsInvokeService(IJSRuntime jsRuntime)
		{
			_jsRuntime = jsRuntime;
		}

		public async Task RegisterAsync<T>(T page) where T : class
		{
			if (!_inited)
			{
				await _jsRuntime.InvokeVoidAsync("import", "./js/pageJsInvokeService.js");
				_inited = true;
			}

			DotNetObjectReference<T>? pageRef = DotNetObjectReference.Create(page);
			await _jsRuntime.InvokeAsync<string>("_registerPage", typeof(T).Name, pageRef);
		}

		public async Task UnregisterAsync<T>() where T : class
		{
			if (!_inited)
			{
				await _jsRuntime.InvokeVoidAsync("import", "./js/pageJsInvokeService.js");
				_inited = true;
			}

			var pageRef = await _jsRuntime.InvokeAsync<DotNetObjectReference<T>?>("_unregisterPage", typeof(T).Name);
			pageRef?.Dispose();
		}

		public async void Dispose()
		{
			if (_disposed)
				return;

			if (_inited)
			{
				var pageRefs = await _jsRuntime.InvokeAsync<IEnumerable<IDisposable>>("_unregisterAll");

				foreach (var pageRef in pageRefs)
				{
					pageRef.Dispose();
				}
			}

			_disposed = true;
			GC.SuppressFinalize(this);
		}
	}
}