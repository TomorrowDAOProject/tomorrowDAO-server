using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TomorrowDAOServer.ResourceToken.Dtos;

public class GetRealtimeRecordsInput : IValidatableObject
{
    public int Limit { get; set; }
    public string Type { get; set; }
    
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Limit < 0)
        {
            yield return new ValidationResult($"Limit invalid: less than 0.");
        }
    }
}