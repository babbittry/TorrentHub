namespace TorrentHub.Core.Enums;

/// <summary>
/// Defines the type of UI interaction to be triggered on the frontend when purchasing a store item.
/// </summary>
public enum StoreActionType
{
    /// <summary>
    /// A simple purchase without any additional user input.
    /// </summary>
    SimplePurchase,

    /// <summary>
    /// Requires the user to input a quantity (e.g., buying upload credit).
    /// </summary>
    PurchaseWithQuantity,

    /// <summary>
    /// Requires the user to provide a new username.
    /// </summary>
    ChangeUsername,
    
    /// <summary>
    /// A special type for purchasing badges, frontend might want to show the badge icon.
    /// </summary>
    PurchaseBadge
}