using SemiTool.Domain;

namespace SemiTool.Application;

public sealed class RecipeService
{
    private readonly EquipmentProfile _profile;

    public RecipeService(EquipmentProfile profile)
    {
        _profile = profile;
        SelectedRecipeKey = _profile.Recipes.Keys.OrderBy(key => key).FirstOrDefault() ?? string.Empty;
    }

    public string SelectedRecipeKey { get; private set; }

    public Recipe? SelectedRecipe =>
        string.IsNullOrWhiteSpace(SelectedRecipeKey) ? null : _profile.Recipes[SelectedRecipeKey];

    public IReadOnlyDictionary<string, Recipe> Recipes => _profile.Recipes;

    public void SelectRecipe(string key)
    {
        if (!_profile.Recipes.ContainsKey(key))
        {
            throw new KeyNotFoundException($"Recipe '{key}' is not defined.");
        }

        SelectedRecipeKey = key;
    }
}
