using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Lacos.Api.ModelBinding;

/// <summary>
/// Permite que enums em query string, rota e formulário usem o mesmo formato do JSON e do banco
/// (UPPER_SNAKE_CASE, ex.: ?categories=MEDICATION_ADMINISTRATION&amp;status=IN_FOLLOW_UP),
/// mantendo compatibilidade com o nome do enum (ex.: MedicationAdministration).
/// Valores numéricos são rejeitados para evitar valores fora do enum.
/// </summary>
public sealed class UpperSnakeEnumModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var value = bindingContext.ValueProvider.GetValue(bindingContext.ModelName).FirstValue;
        if (string.IsNullOrWhiteSpace(value)) return Task.CompletedTask;

        var enumType = Nullable.GetUnderlyingType(bindingContext.ModelType) ?? bindingContext.ModelType;
        var normalized = value.Replace("_", string.Empty).Trim();
        if (!normalized.All(char.IsDigit) && Enum.TryParse(enumType, normalized, ignoreCase: true, out var parsed) && Enum.IsDefined(enumType, parsed!))
        {
            bindingContext.Result = ModelBindingResult.Success(parsed);
        }
        else
        {
            bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, $"Valor inválido: '{value}'.");
        }
        return Task.CompletedTask;
    }
}

/// <summary>Registra o binder para enums e enums anuláveis (arrays usam o binder do elemento).</summary>
public sealed class UpperSnakeEnumModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        var type = Nullable.GetUnderlyingType(context.Metadata.ModelType) ?? context.Metadata.ModelType;
        return type.IsEnum && context.BindingInfo.BindingSource != BindingSource.Body ? new UpperSnakeEnumModelBinder() : null;
    }
}
