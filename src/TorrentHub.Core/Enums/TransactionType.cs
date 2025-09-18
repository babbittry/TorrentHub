namespace TorrentHub.Core.Enums;

public enum TransactionType
{
    Transfer,       // 用户间转账
    Tip,            // 用户打赏
    SystemGrant,    // 系统发放 (例如：完成求种奖励)
    StorePurchase,  // 商店购买
    RequestCreate,  // 发布求种手续费
    RequestBounty,  // 求种悬赏支付
    UploadBonus,    // 上传种子奖励
    CommentBonus    // 评论奖励
}