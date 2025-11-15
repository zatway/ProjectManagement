namespace Application.CommonModels;

public sealed record SimpleCommandResult
{
    /// <summary>
    /// Успех
    /// </summary>
    public bool Successfully { get; set; }
}
