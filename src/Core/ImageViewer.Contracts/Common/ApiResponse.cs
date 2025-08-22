namespace ImageViewer.Contracts.Common;

/// <summary>
/// 표준화된 API 응답 형식
/// 모든 API 엔드포인트에서 일관된 응답 구조를 제공
/// </summary>
/// <typeparam name="T">응답 데이터의 타입</typeparam>
public record ApiResponse<T>
{
    /// <summary>
    /// 요청 성공 여부
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// 응답 데이터
    /// </summary>
    public T? Data { get; init; }

    /// <summary>
    /// 오류 메시지 (실패 시)
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// 오류 코드 (실패 시)
    /// </summary>
    public string? ErrorCode { get; init; }

    /// <summary>
    /// 추가 오류 정보 (검증 실패 등)
    /// </summary>
    public IDictionary<string, string[]>? ValidationErrors { get; init; }

    /// <summary>
    /// 요청 타임스탬프
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// 성공 응답 생성
    /// </summary>
    /// <param name="data">응답 데이터</param>
    /// <returns>성공 응답</returns>
    public static ApiResponse<T> SuccessResponse(T data)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data
        };
    }

    /// <summary>
    /// 실패 응답 생성
    /// </summary>
    /// <param name="errorMessage">오류 메시지</param>
    /// <param name="errorCode">오류 코드</param>
    /// <returns>실패 응답</returns>
    public static ApiResponse<T> ErrorResponse(string errorMessage, string? errorCode = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            ErrorMessage = errorMessage,
            ErrorCode = errorCode
        };
    }

    /// <summary>
    /// 검증 실패 응답 생성
    /// </summary>
    /// <param name="validationErrors">검증 오류</param>
    /// <returns>검증 실패 응답</returns>
    public static ApiResponse<T> ValidationErrorResponse(IDictionary<string, string[]> validationErrors)
    {
        return new ApiResponse<T>
        {
            Success = false,
            ErrorMessage = "유효성 검사에 실패했습니다.",
            ErrorCode = "VALIDATION_FAILED",
            ValidationErrors = validationErrors
        };
    }
}

/// <summary>
/// 데이터가 없는 API 응답
/// </summary>
public record ApiResponse : ApiResponse<object>
{
    /// <summary>
    /// 성공 응답 생성 (데이터 없음)
    /// </summary>
    /// <returns>성공 응답</returns>
    public static ApiResponse SuccessResponse()
    {
        return new ApiResponse
        {
            Success = true
        };
    }

    /// <summary>
    /// 실패 응답 생성 (데이터 없음)
    /// </summary>
    /// <param name="errorMessage">오류 메시지</param>
    /// <param name="errorCode">오류 코드</param>
    /// <returns>실패 응답</returns>
    public static ApiResponse ErrorResponse(string errorMessage, string? errorCode = null)
    {
        return new ApiResponse
        {
            Success = false,
            ErrorMessage = errorMessage,
            ErrorCode = errorCode
        };
    }
}