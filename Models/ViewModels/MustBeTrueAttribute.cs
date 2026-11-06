using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace HealthTrack.Models.ViewModels
{
    // Requires a checkbox to be ticked, on the server and in the browser.
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class MustBeTrueAttribute : ValidationAttribute, IClientModelValidator
    {
        public override bool IsValid(object? value) => value is true;

        public void AddValidation(ClientModelValidationContext context)
        {
            context.Attributes.TryAdd("data-val", "true");
            context.Attributes.TryAdd("data-val-required", FormatErrorMessage(context.ModelMetadata.GetDisplayName()));
        }
    }
}
