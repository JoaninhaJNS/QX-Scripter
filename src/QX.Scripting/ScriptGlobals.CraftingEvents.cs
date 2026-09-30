using Qx.Model.Messages.Incoming;

namespace Qx.Scripting;

public partial class ScriptGlobals
{
    /// <summary>
    /// Registers a handler that runs when the server sends the list of craftable products.
    /// </summary>
    /// <param name="handler">The handler to call with the product list.</param>
    /// <returns>A handle that removes the handler when disposed.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the script globals have already been disposed.</exception>
    public IDisposable OnCraftableProducts(Action<CraftableProducts> handler)
        => Subscribe(handler, value => Crafting.ProductsReceived += value,
            value => Crafting.ProductsReceived -= value);

    /// <summary>
    /// Registers a handler that runs when the server sends the ingredients of a recipe.
    /// </summary>
    /// <param name="handler">The handler to call with the ingredient list.</param>
    /// <returns>A handle that removes the handler when disposed.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the script globals have already been disposed.</exception>
    public IDisposable OnCraftingRecipe(Action<CraftingRecipe> handler)
        => Subscribe(handler, value => Crafting.RecipeReceived += value,
            value => Crafting.RecipeReceived -= value);

    /// <summary>
    /// Registers a handler that runs when the server sends the result of a craft.
    /// </summary>
    /// <param name="handler">The handler to call with the result, which holds the success flag and the crafted product.</param>
    /// <returns>A handle that removes the handler when disposed.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the script globals have already been disposed.</exception>
    public IDisposable OnCraftingResult(Action<CraftingResult> handler)
        => Subscribe(handler, value => Crafting.ResultReceived += value,
            value => Crafting.ResultReceived -= value);

    /// <summary>
    /// Registers a handler that runs when the server sends the number of recipes available for a
    /// set of ingredients.
    /// </summary>
    /// <param name="handler">The handler to call with the match count and the completeness flag.</param>
    /// <returns>A handle that removes the handler when disposed.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the script globals have already been disposed.</exception>
    public IDisposable OnCraftingRecipesAvailable(
        Action<CraftingRecipesAvailable> handler)
        => Subscribe(handler, value => Crafting.AvailableRecipesReceived += value,
            value => Crafting.AvailableRecipesReceived -= value);

    /// <summary>
    /// Registers a handler that runs when the crafting state is cleared after the hotel connection
    /// closes.
    /// </summary>
    /// <remarks>
    /// It does not run when the state is cleared because a new session starts.
    /// </remarks>
    /// <param name="handler">The handler to call with no arguments.</param>
    /// <returns>A handle that removes the handler when disposed.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the script globals have already been disposed.</exception>
    public IDisposable OnCraftingReset(Action handler)
        => Subscribe(handler, value => Crafting.ResetCompleted += value,
            value => Crafting.ResetCompleted -= value);
}
