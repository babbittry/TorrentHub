using Sakura.PT.Entities;

namespace Sakura.PT.Services;

public interface ICommentService
{
    Task<(bool Success, string Message, Comment? Comment)> PostCommentAsync(int torrentId, string commentText, int userId);
}
