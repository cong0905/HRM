using System.Security.Cryptography;
using System.Text;

namespace HRM.Ops.Migration;

/// <summary>
/// Generates deterministic GUIDs (UUIDv5 equivalent) from namespace and name.
/// This guarantees idempotent re-runs: the same legacy entity always gets the exact same GUID.
/// </summary>
public static class DeterministicGuid
{
    // Namespaces for different entities
    public static readonly Guid DepartmentNamespace = Guid.Parse("10000000-0000-0000-0000-000000000001");
    public static readonly Guid PositionNamespace = Guid.Parse("10000000-0000-0000-0000-000000000002");
    public static readonly Guid EmployeeNamespace = Guid.Parse("10000000-0000-0000-0000-000000000003");
    public static readonly Guid UserNamespace = Guid.Parse("10000000-0000-0000-0000-000000000004");
    public static readonly Guid AttendanceNamespace = Guid.Parse("10000000-0000-0000-0000-000000000005");
    public static readonly Guid LeaveNamespace = Guid.Parse("10000000-0000-0000-0000-000000000006");
    public static readonly Guid PayrollPeriodNamespace = Guid.Parse("10000000-0000-0000-0000-000000000007");
    public static readonly Guid PayrollBatchNamespace = Guid.Parse("10000000-0000-0000-0000-000000000008");
    public static readonly Guid PayrollSnapshotNamespace = Guid.Parse("10000000-0000-0000-0000-000000000009");
    public static readonly Guid PayslipNamespace = Guid.Parse("10000000-0000-0000-0000-00000000000A");
    public static readonly Guid SalaryHistoryNamespace = Guid.Parse("10000000-0000-0000-0000-00000000000B");
    public static readonly Guid LeaveBalanceNamespace = Guid.Parse("10000000-0000-0000-0000-00000000000C");
    public static readonly Guid LeaveLedgerNamespace = Guid.Parse("10000000-0000-0000-0000-00000000000D");

    public static Guid Create(Guid namespaceId, string value)
    {
        var namespaceBytes = namespaceId.ToByteArray();
        SwapByteOrder(namespaceBytes);

        var valueBytes = Encoding.UTF8.GetBytes(value);
        var combined = new byte[namespaceBytes.Length + valueBytes.Length];
        Buffer.BlockCopy(namespaceBytes, 0, combined, 0, namespaceBytes.Length);
        Buffer.BlockCopy(valueBytes, 0, combined, namespaceBytes.Length, valueBytes.Length);

        var hash = SHA1.HashData(combined);
        var newGuid = new byte[16];
        Array.Copy(hash, 0, newGuid, 0, 16);

        // Set version 5
        newGuid[6] = (byte)((newGuid[6] & 0x0F) | (5 << 4));
        // Set variant RFC 4122
        newGuid[8] = (byte)((newGuid[8] & 0x3F) | 0x80);

        SwapByteOrder(newGuid);
        return new Guid(newGuid);
    }

    private static void SwapByteOrder(byte[] guidBytes)
    {
        Swap(guidBytes, 0, 3);
        Swap(guidBytes, 1, 2);
        Swap(guidBytes, 4, 5);
        Swap(guidBytes, 6, 7);
    }

    private static void Swap(byte[] array, int left, int right)
    {
        (array[left], array[right]) = (array[right], array[left]);
    }
}
