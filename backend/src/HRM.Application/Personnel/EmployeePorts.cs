using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HRM.Next.Application.Personnel;

public sealed record EmployeeProfileResponse(
    Guid Id,
    Guid UserId,
    string FullName,
    string? CitizenId,
    string? Phone,
    string? Address,
    Guid? DepartmentId,
    string EmployeeCode = "",
    string? Email = null,
    DateOnly? DateOfBirth = null,
    string? Gender = null,
    string? MaritalStatus = null,
    Guid? PositionId = null,
    DateOnly? HireDate = null,
    DateOnly? TerminationDate = null,
    string Status = "DangLamViec",
    string? RowVersion = null);

public sealed record CreateEmployeeRequest(
    [Required, StringLength(100)] string FullName,
    [Required, StringLength(20)] string CitizenId,
    [Required, EmailAddress, StringLength(256)] string Email,
    [Required, StringLength(20)] string Phone,
    [Required, StringLength(256)] string Address,
    [Required] DateOnly DateOfBirth,
    [Required, StringLength(20)] string Gender,
    [Required] DateOnly HireDate,
    [StringLength(50)] string? MaritalStatus,
    Guid? DepartmentId,
    Guid? PositionId,
    [Required, StringLength(500)] string Reason,
    [StringLength(100)] string? Username = null);

public sealed record CreateEmployeeResponse(
    EmployeeProfileResponse Profile,
    string DeliveryStatus,
    string ProvisioningStatus);

public sealed class UpdateEmployeeRequest
{
    private readonly HashSet<string> _setProperties = new(StringComparer.OrdinalIgnoreCase);

    private string? _phone;
    [StringLength(20)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Phone
    {
        get => _phone;
        init
        {
            _phone = value;
            _setProperties.Add(nameof(Phone));
        }
    }

    private string? _address;
    [StringLength(256)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Address
    {
        get => _address;
        init
        {
            _address = value;
            _setProperties.Add(nameof(Address));
        }
    }

    private string? _fullName;
    [StringLength(100)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FullName
    {
        get => _fullName;
        init
        {
            _fullName = value;
            _setProperties.Add(nameof(FullName));
        }
    }

    private DateOnly? _dateOfBirth;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateOnly? DateOfBirth
    {
        get => _dateOfBirth;
        init
        {
            _dateOfBirth = value;
            _setProperties.Add(nameof(DateOfBirth));
        }
    }

    private string? _gender;
    [StringLength(20)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Gender
    {
        get => _gender;
        init
        {
            _gender = value;
            _setProperties.Add(nameof(Gender));
        }
    }

    private string? _maritalStatus;
    [StringLength(50)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MaritalStatus
    {
        get => _maritalStatus;
        init
        {
            _maritalStatus = value;
            _setProperties.Add(nameof(MaritalStatus));
        }
    }

    private Guid? _departmentId;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? DepartmentId
    {
        get => _departmentId;
        init
        {
            _departmentId = value;
            _setProperties.Add(nameof(DepartmentId));
        }
    }

    private Guid? _positionId;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? PositionId
    {
        get => _positionId;
        init
        {
            _positionId = value;
            _setProperties.Add(nameof(PositionId));
        }
    }

    private DateOnly? _hireDate;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateOnly? HireDate
    {
        get => _hireDate;
        init
        {
            _hireDate = value;
            _setProperties.Add(nameof(HireDate));
        }
    }

    [Required, StringLength(500)]
    public string Reason { get; init; } = string.Empty;

    public string? RowVersion { get; init; }

    public bool IsSpecified(string propertyName) => _setProperties.Contains(propertyName);

    public UpdateEmployeeRequest() { }

    public UpdateEmployeeRequest(string? phone, string? address, string reason, string? rowVersion = null)
    {
        if (phone is not null) Phone = phone;
        if (address is not null) Address = address;
        Reason = reason;
        RowVersion = rowVersion;
    }
}

public sealed record OffboardEmployeeRequest(
    [Required] DateOnly TerminationDate,
    [Required, StringLength(500)] string Reason,
    [Required] string RowVersion);

public sealed record BulkGetEmployeesRequest(
    [Required] List<Guid> EmployeeIds);
