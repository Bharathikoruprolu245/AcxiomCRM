using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AcxiomCRM.Validation;

public sealed class NotInPastAttribute : ValidationAttribute, IClientModelValidator
{
    public NotInPastAttribute(string message) { ErrorMessage = message; }
    public override bool IsValid(object? value) => value is not DateTime d || d.Date >= DateTime.Today;
    public void AddValidation(ClientModelValidationContext context)
    {
        Merge(context.Attributes, "data-val", "true");
        Merge(context.Attributes, "data-val-notinpast", ErrorMessage ?? "Date cannot be in the past.");
    }
    private static void Merge(IDictionary<string,string> attrs,string key,string value)
    {
        if(!attrs.ContainsKey(key))attrs.Add(key,value);
    }
}
