using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TomorrowDAOServer.ResourceToken.Dtos;

public class GetRecordsInput : IValidatableObject
{
    public int Limit { get; set; }
    public int Page { get; set; }
    public string Order { get; set; }
    public string Address { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Limit < 0)
        {
            yield return new ValidationResult($"Limit invalid: less than 0.");
        }
        if (Page < 0)
        {
            yield return new ValidationResult($"Page invalid: less than 0.");
        }
    }
}