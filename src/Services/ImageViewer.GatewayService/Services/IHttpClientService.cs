namespace ImageViewer.GatewayService.Services;

/// <summary>
/// HTTP 클라이언트 서비스 인터페이스
/// </summary>
public interface IHttpClientService
{
    /// <summary>
    /// GET 요청을 보냅니다.
    /// </summary>
    /// <param name="serviceUrl">서비스 URL</param>
    /// <param name="endpoint">엔드포인트</param>
    /// <param name="headers">추가 헤더</param>
    /// <returns>HTTP 응답</returns>
    Task<HttpResponseMessage> GetAsync(string serviceUrl, string endpoint, Dictionary<string, string>? headers = null);

    /// <summary>
    /// POST 요청을 보냅니다.
    /// </summary>
    /// <param name="serviceUrl">서비스 URL</param>
    /// <param name="endpoint">엔드포인트</param>
    /// <param name="content">요청 내용</param>
    /// <param name="headers">추가 헤더</param>
    /// <returns>HTTP 응답</returns>
    Task<HttpResponseMessage> PostAsync(string serviceUrl, string endpoint, HttpContent? content = null, Dictionary<string, string>? headers = null);

    /// <summary>
    /// PUT 요청을 보냅니다.
    /// </summary>
    /// <param name="serviceUrl">서비스 URL</param>
    /// <param name="endpoint">엔드포인트</param>
    /// <param name="content">요청 내용</param>
    /// <param name="headers">추가 헤더</param>
    /// <returns>HTTP 응답</returns>
    Task<HttpResponseMessage> PutAsync(string serviceUrl, string endpoint, HttpContent? content = null, Dictionary<string, string>? headers = null);

    /// <summary>
    /// DELETE 요청을 보냅니다.
    /// </summary>
    /// <param name="serviceUrl">서비스 URL</param>
    /// <param name="endpoint">엔드포인트</param>
    /// <param name="headers">추가 헤더</param>
    /// <returns>HTTP 응답</returns>
    Task<HttpResponseMessage> DeleteAsync(string serviceUrl, string endpoint, Dictionary<string, string>? headers = null);

    /// <summary>
    /// PATCH 요청을 보냅니다.
    /// </summary>
    /// <param name="serviceUrl">서비스 URL</param>
    /// <param name="endpoint">엔드포인트</param>
    /// <param name="content">요청 내용</param>
    /// <param name="headers">추가 헤더</param>
    /// <returns>HTTP 응답</returns>
    Task<HttpResponseMessage> PatchAsync(string serviceUrl, string endpoint, HttpContent? content = null, Dictionary<string, string>? headers = null);
}