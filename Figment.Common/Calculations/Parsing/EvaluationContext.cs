using Figment.Common.Errors;

namespace Figment.Common.Calculations.Parsing;

public readonly record struct EvaluationContext
{
    /// <summary>
    /// A default context that contains no additional data.
    /// </summary>
    public static readonly EvaluationContext EMPTY = new();

    /// <summary>
    /// Gets the context data provided for calls to <see cref="ExpressionParser.Parse(string)"/>.
    /// </summary>
    private Dictionary<string, ExpressionResult> RowData { get; init; } = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="EvaluationContext"/> class.
    /// </summary>
    public EvaluationContext()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EvaluationContext"/> class.
    /// </summary>
    /// <param name="schema">A <see cref="Schema"/> object that should be injected into the context, mocking its properties as prototypes for an evaluation.</param>
    /// <remarks>The purpose of this method is to support <see cref="EvaluationContext(Schema)"/> and <see cref="ValidateImportMapCommnad"/>.</remarks>
    public EvaluationContext(Schema schema)
    {
        foreach (var prop in schema.Properties)
        {
            switch (prop.Value.Type)
            {
                case SchemaArrayField.SCHEMA_FIELD_TYPE:
                    var arr = new SchemaArrayField(prop.Key);
                    arr.TryMassageInput(Array.Empty<string>(), out object? arrOutput);
                    RowData.Add(prop.Key.ToLowerInvariant(), ExpressionResult.Success(arrOutput));
                    break;
                case SchemaBooleanField.SCHEMA_FIELD_TYPE:
                    var boo = new SchemaBooleanField(prop.Key);
                    boo.TryMassageInput(true, out object? booOutput);
                    RowData.Add(prop.Key.ToLowerInvariant(), ExpressionResult.Success(booOutput));
                    break;
                case SchemaDateField.SCHEMA_FIELD_TYPE:
                    var date = new SchemaDateField(prop.Key);
                    date.TryMassageInput(DateTimeOffset.UtcNow, out object? dateOutput);
                    RowData.Add(prop.Key.ToLowerInvariant(), ExpressionResult.Success(dateOutput));
                    break;
                case SchemaEmailField.SCHEMA_FIELD_TYPE:
                    var email = new SchemaEmailField(prop.Key);
                    email.TryMassageInput("example@example.com", out object? emailOutput);
                    RowData.Add(prop.Key.ToLowerInvariant(), ExpressionResult.Success(emailOutput));
                    break;
                case SchemaEnumField.SCHEMA_FIELD_TYPE:
                    var enu = new SchemaEnumField(prop.Key, ["red", "blue"]);
                    enu.TryMassageInput("red", out object? enuOutput);
                    RowData.Add(prop.Key.ToLowerInvariant(), ExpressionResult.Success(enuOutput));
                    break;
                case SchemaIntegerField.SCHEMA_FIELD_TYPE:
                    var inte = new SchemaIntegerField(prop.Key);
                    inte.TryMassageInput(8675309, out object? inteOutput);
                    RowData.Add(prop.Key.ToLowerInvariant(), ExpressionResult.Success(inteOutput));
                    break;
                case SchemaMonthDayField.SCHEMA_FIELD_TYPE:
                    var monthDay = new SchemaMonthDayField(prop.Key);
                    monthDay.TryMassageInput("January 26", out object? monthDayOutput);
                    RowData.Add(prop.Key.ToLowerInvariant(), ExpressionResult.Success(monthDayOutput));
                    break;
                case SchemaNumberField.SCHEMA_FIELD_TYPE:
                    var num = new SchemaNumberField(prop.Key);
                    num.TryMassageInput(123.456, out object? numOutput);
                    RowData.Add(prop.Key.ToLowerInvariant(), ExpressionResult.Success(numOutput));
                    break;
                case SchemaPhoneField.SCHEMA_FIELD_TYPE:
                    var phone = new SchemaPhoneField(prop.Key);
                    phone.TryMassageInput("1238675309", out object? phoneOutput);
                    RowData.Add(prop.Key.ToLowerInvariant(), ExpressionResult.Success(phoneOutput));
                    break;
                case SchemaRefField.SCHEMA_FIELD_TYPE:
                    var refe = new SchemaRefField(prop.Key, Guid.Empty.ToString());
                    refe.TryMassageInput(refe.Id, out object? refeOutput);
                    RowData.Add(prop.Key.ToLowerInvariant(), ExpressionResult.Success(refeOutput));
                    break;
                case SchemaTextField.SCHEMA_FIELD_TYPE:
                    var text = new SchemaTextField(prop.Key);
                    text.TryMassageInput("example", out object? textOutput);
                    RowData.Add(prop.Key.ToLowerInvariant(), ExpressionResult.Success(textOutput));
                    break;
                case SchemaUriField.SCHEMA_FIELD_TYPE:
                    var url = new SchemaUriField(prop.Key);
                    url.TryMassageInput("http://example.com/", out object? urlOutput);
                    RowData.Add(prop.Key.ToLowerInvariant(), ExpressionResult.Success(urlOutput));
                    break;
                default:
                    AmbientErrorContext.Provider.LogDebug($"Unable to mock property '{prop.Key}' with type '{prop.Value.Type}'");
                    break;
            }
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EvaluationContext"/> class.
    /// </summary>
    /// <param name="thing">A <see cref="Thing"/> object that should be injected into the context, making its properties available.</param>
    public EvaluationContext(Thing thing)
    {
        // Add set properties.
        foreach (var prop in thing.GetProperties(CancellationToken.None).ToBlockingEnumerable())
        {
            RowData.Add(prop.SimpleDisplayName.ToLowerInvariant(), ExpressionResult.Success(prop.Value));
        }

        // Add unset properties.
        foreach (var usp in thing.GetUnsetProperties(CancellationToken.None).Result)
        {
            RowData.Add(usp.SimpleDisplayName.ToLowerInvariant(), ExpressionResult.Error(CalculationErrorType.BadValue, "Unset property"));
        }

        // Populate meta properties always override.
        RowData["Name"] = ExpressionResult.Success(thing.Name);
        RowData["CreatedOn"] = ExpressionResult.Success(thing.CreatedOn);
        RowData["LastAccessed"] = ExpressionResult.Success(thing.LastAccessed);
        RowData["LastModified"] = ExpressionResult.Success(thing.LastModified);
    }

    /// <summary>
    /// Retrieves a value from the context map.
    /// </summary>
    /// <param name="name">Name of the value to retrieve from the context.</param>
    /// <returns>The value of the context, or an error if not found.</returns>
    public ExpressionResult GetField(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));

        if (!RowData.TryGetValue(name.ToLowerInvariant(), out var val))
        {
            return ExpressionResult.Error(CalculationErrorType.FormulaParse, $"Field '{name}' not found");
        }

        return val;
    }
}