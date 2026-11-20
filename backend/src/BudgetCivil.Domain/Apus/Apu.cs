namespace BudgetCivil.Domain.Apus;

/// <summary>
/// Análisis de precio unitario propio de un proyecto (`REQ-APUS-01`, `REQ-APUS-02`, `RF-07`, `BR-016`,
/// `D-31`). No almacena código, no tiene baja lógica y siempre se elimina con el ítem del EDT al que está
/// vinculado (`D-49`, `D-114`, `D-119`).
/// </summary>
public sealed class Apu
{
    public const int UnitMaxLength = 10;

    private readonly List<ApuResource> _lines = [];

    private Apu()
    {
    }

    public Guid Id { get; private set; }

    public Guid ProjectId { get; private set; }

    public string Description { get; private set; } = string.Empty;

    public string Unit { get; private set; } = string.Empty;

    public ApuStatus Status { get; private set; } = ApuStatus.FaltaInformacion;

    public Guid LastModifiedByUserId { get; private set; }

    public DateTime LastModifiedAtUtc { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    public IReadOnlyCollection<ApuResource> Lines => _lines;

    public static Apu Create(
        Guid projectId,
        string? description,
        string? unit,
        Guid actorUserId,
        DateTime utcNow)
    {
        if (projectId == Guid.Empty)
        {
            throw new InvalidOperationException("Project is required.");
        }

        if (actorUserId == Guid.Empty)
        {
            throw new InvalidOperationException("APU author is required.");
        }

        return new Apu
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Description = NormalizeDescription(description),
            Unit = NormalizeUnit(unit),
            Status = ApuStatus.FaltaInformacion,
            LastModifiedByUserId = actorUserId,
            LastModifiedAtUtc = utcNow
        };
    }

    /// <summary>
    /// Edita descripción y unidad sin alterar el vínculo con el ítem (`REQ-APUS-02`). Los valores nulos
    /// conservan el valor vigente para permitir la edición parcial (`REQ-APUS-21`).
    /// </summary>
    public void ChangeDetails(
        string? description,
        string? unit,
        Guid actorUserId,
        DateTime utcNow)
    {
        if (description is not null)
        {
            Description = NormalizeDescription(description);
        }

        if (unit is not null)
        {
            Unit = NormalizeUnit(unit);
        }

        TouchAuthor(actorUserId, utcNow);
    }

    /// <summary>
    /// Agrega una línea de composición; la validación del insumo (proyecto y tipo) corresponde al caso de
    /// uso y aquí solo se valida la semántica de consumo y desperdicio (`REQ-APUS-04`).
    /// </summary>
    public ApuResource AddLine(
        Guid resourceId,
        decimal consumption,
        decimal wastePercentage,
        Guid actorUserId,
        DateTime utcNow)
    {
        var line = ApuResource.Create(Id, resourceId, consumption, wastePercentage);
        _lines.Add(line);
        TouchAuthor(actorUserId, utcNow);
        return line;
    }

    public ApuResource? FindLine(Guid lineId)
    {
        return _lines.SingleOrDefault(line => line.Id == lineId);
    }

    /// <summary>Edita el consumo y el desperdicio de una línea (`SC-APUS-20`).</summary>
    public ApuResource UpdateLine(
        Guid lineId,
        decimal consumption,
        decimal wastePercentage,
        Guid actorUserId,
        DateTime utcNow)
    {
        var line = FindLine(lineId)
            ?? throw new InvalidOperationException("APU line was not found.");

        line.Change(consumption, wastePercentage);
        TouchAuthor(actorUserId, utcNow);
        return line;
    }

    /// <summary>Retiro físico de una línea; el insumo referenciado no se elimina (`D-04`).</summary>
    public ApuResource RemoveLine(Guid lineId, Guid actorUserId, DateTime utcNow)
    {
        var line = FindLine(lineId)
            ?? throw new InvalidOperationException("APU line was not found.");

        _lines.Remove(line);
        TouchAuthor(actorUserId, utcNow);
        return line;
    }

    /// <summary>
    /// Calcula la completitud del APU (`REQ-APUS-03`, `INC-06`). El APU está completo cuando descripción y
    /// unidad no están vacías, existe al menos una línea y toda línea de Matriz referencia una Matriz con
    /// unidad y componentes. El precio no interviene.
    /// </summary>
    public ApuStatus CalculateStatus(bool referencedMatricesComplete)
    {
        var isComplete =
            !string.IsNullOrWhiteSpace(Description) &&
            !string.IsNullOrWhiteSpace(Unit) &&
            _lines.Count > 0 &&
            referencedMatricesComplete;

        Status = isComplete ? ApuStatus.Completo : ApuStatus.FaltaInformacion;
        return Status;
    }

    /// <summary>Registra el actor autenticado y la fecha UTC de la modificación (`REQ-APUS-14`, `D-08`).</summary>
    public void TouchAuthor(Guid actorUserId, DateTime utcNow)
    {
        if (actorUserId == Guid.Empty)
        {
            throw new InvalidOperationException("APU author is required.");
        }

        LastModifiedByUserId = actorUserId;
        LastModifiedAtUtc = utcNow;
    }

    public static string NormalizeDescription(string? description)
    {
        var normalized = description?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidOperationException("APU description is invalid.");
        }

        return normalized;
    }

    /// <summary>La unidad tiene entre 1 y 10 caracteres (`D5`, `REQ-APUS-02`, `SC-APUS-109`).</summary>
    public static string NormalizeUnit(string? unit)
    {
        var normalized = unit?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length > UnitMaxLength)
        {
            throw new InvalidOperationException("APU unit is invalid.");
        }

        return normalized;
    }
}
