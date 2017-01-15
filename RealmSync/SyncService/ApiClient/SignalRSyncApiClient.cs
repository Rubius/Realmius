using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Client;

namespace RealmSync.SyncService
{
	public class SignalRSyncApiClient : IApiClient
	{
		private IHubProxy _hubProxy;
		private HubConnection _hubConnection;
		private ApiClientStartOptions _startOptions;
		public Uri Uri { get; set; }

		public SignalRSyncApiClient(Uri uri)
		{
			Uri = uri;
		}

		public event EventHandler<DownloadDataResponse> NewDataDownloaded;

		protected virtual void OnNewDataDownloaded(DownloadDataResponse e)
		{
			NewDataDownloaded?.Invoke(this, e);
		}



		public async Task Start(ApiClientStartOptions startOptions)
		{
			_startOptions = startOptions;

			await Reconnect();

		}

		private Action _hubUnsubscribe = () => { };
		private async Task Reconnect()
		{
			try
			{
				_hubUnsubscribe();
				_hubUnsubscribe = () => { };
				_hubConnection = new HubConnection(Uri.ToString());

				_hubProxy = _hubConnection.CreateHubProxy(Constants.SignalRHubName);
				var downloadHandler = _hubProxy.On<DownloadDataResponse>("DataDownloaded", OnNewDataDownloaded);

				_hubUnsubscribe += () =>
				{
					downloadHandler.Dispose();
				};

				await _hubConnection.Start();

				_hubConnection.Closed += _hubConnection_Closed;
				_hubUnsubscribe += () =>
				{
					_hubConnection.Closed -= _hubConnection_Closed;
				};
			}
			catch (WebException webEx)
			{
				LogAndReconnectWithDelay(webEx);
			}
			catch (HttpClientException webEx)
			{
				LogAndReconnectWithDelay(webEx);
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"{ex}");
			}
		}

		void LogAndReconnectWithDelay(Exception exception)
		{
			Debug.WriteLine($"{exception}");
			Task.Delay(1000).ContinueWith((x) => Reconnect());
		}

		void _hubConnection_Closed()
		{
			Reconnect();
		}

		public void Stop()
		{
			_hubConnection?.Stop();
			_hubConnection?.Dispose();
			_hubConnection = null;
		}

		public async Task<UploadDataResponse> UploadData(UploadDataRequest request)
		{
			try
			{
				var response = await _hubProxy.Invoke<UploadDataResponse>("UploadData", request);

				return response;
			}
			catch (Exception)
			{
				return new UploadDataResponse();
			}
		}

	}
}