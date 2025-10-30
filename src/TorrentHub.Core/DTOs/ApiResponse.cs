namespace TorrentHub.Core.DTOs;

/// <summary>
/// A standardized API response wrapper.
/// </summary>
/// <typeparam name="T">The type of the data being returned.</typeparam>
public class ApiResponse<T>
{
    /// <summary>
    /// Indicates whether the request was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The data payload of the response.
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// A message providing more information about the response.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// A dictionary of validation errors.
    /// </summary>
    public Dictionary<string, string[]>? Errors { get; set; }

    public static ApiResponse<T> SuccessResult(T data, string? message = null)
        => new() { Success = true, Data = data, Message = message };
        
    public static ApiResponse<T> ErrorResult(string message, Dictionary<string, string[]>? errors = null)
        => new() { Success = false, Message = message, Errors = errors };
}

/// <summary>
/// A standardized API response wrapper for non-data responses.
/// </summary>
public class ApiResponse : ApiResponse<object>
{
    public static ApiResponse SuccessResult(string? message = null)
        => new() { Success = true, Message = message };
}