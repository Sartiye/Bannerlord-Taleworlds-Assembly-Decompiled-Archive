using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TaleWorlds.Library.Http;

namespace TaleWorlds.MountAndBlade.DotNetCore;

public class DotNetCoreHttpDriver : IHttpDriver
{
	private HttpClient _httpClient;

	public DotNetCoreHttpDriver()
	{
		ServicePointManager.DefaultConnectionLimit = 5;
		ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
		_httpClient = new HttpClient();
		_httpClient.DefaultRequestVersion = HttpVersion.Version30;
		_httpClient.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
	}

	IHttpRequestTask IHttpDriver.CreateHttpPostRequestTask(string address, string postData, bool withUserToken, CancellationToken cancellationToken)
	{
		return new HttpPostRequest(_httpClient, address, postData, new Version("3.0"), cancellationToken);
	}

	IHttpRequestTask IHttpDriver.CreateHttpGetRequestTask(string address, bool withUserToken)
	{
		return new HttpGetRequest(_httpClient, address, new Version("3.0"));
	}

	async Task<string> IHttpDriver.HttpGetString(string url, bool withUserToken)
	{
		HttpResponseMessage responseMessage = await _httpClient.GetAsync(url);
		string text = await responseMessage.Content.ReadAsStringAsync();
		if (!responseMessage.IsSuccessStatusCode)
		{
			throw new Exception(text);
		}
		return text;
	}

	async Task<string> IHttpDriver.HttpPostString(string url, string postData, string mediaType, bool withUserToken)
	{
		using HttpResponseMessage response = await _httpClient.PostAsync(url, new StringContent(postData, Encoding.Unicode, mediaType));
		using HttpContent content = response.Content;
		return await content.ReadAsStringAsync();
	}

	async Task<byte[]> IHttpDriver.HttpDownloadData(string url)
	{
		return await _httpClient.GetByteArrayAsync(url);
	}
}
