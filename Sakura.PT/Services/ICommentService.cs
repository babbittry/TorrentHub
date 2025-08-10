using Sakura.PT.Entities;
using Sakura.PT.DTOs;

namespace Sakura.PT.Services;

public interface ICommentService
{
    Task<(bool Success, string Message, Comment? Comment)> PostCommentAsync(int torrentId, CreateCommentRequestDto request, int userId);
}
