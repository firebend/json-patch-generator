namespace Firebend.JsonPatch.Models;

public enum JsonChange
{
    Unknown = 0,

    /// <summary>
    /// A json patch add operation
    /// </summary>
    Add = 1,

    /// <summary>
    /// A json patch replace operation
    /// </summary>
    Replace = 2,

    /// <summary>
    /// A json patch remove operation
    /// </summary>
    Remove = 3
}
