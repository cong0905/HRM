"""T07 authorization and scope gate: complete SQL authorization & audit matrix plus foundation/contracts/audit/legacy gate."""
import xml.etree.ElementTree as ET
from verify_t05 import main, NS

AUTHORIZATION_CASES = (
    'TableDriven_AnonymousRequests_AreRejectedWith401',
    'TableDriven_InsufficientPermission_Returns403',
    'IDOR_EmployeeCannotReadOrModifyOtherEmployeeData_OrBulkExport',
    'MassAssignment_CannotModifyRoleOrCitizenIdOrUserId',
    'SystemAdmin_WithoutPayrollPermission_CannotReadSalary_FakeHeadersIgnored',
    'RoleAssignment_AntiSelfEscalation_AndSessionRevocationSLA',
    'Audit_SensitiveMutationsRecordedWithMaskedData_NoPasswordOrRawCccd',
    'AtomicRollback_WhenAuditOrMutationFails_BothRollbackOnRealSqlServer',
    'AuditQuery_RequiresPermission_FiltersAndEnforcesBoundedPagination',
    'AntiSelfApproval_HRManagerCannotApproveOwnLeave',
    'SeparationOfDuties_PayrollCalculator_CannotApproveOwnCalculation_AnotherPayrollUserCanApprove',
    'PayrollPeriod_PublishedOrLockedPeriod_CannotBeRecalculatedOrReopened',
    'Audit_SystemAdmin_CannotReadPiiOrSecretMarkers_FromReasonOrPayload',
    'Payroll_GetEmployee_ReceivesCompensationProjectionWithoutAddressOrRawCccd',
    'BoundedPagination_EnforcedOnPersonalAndPayrollLists',
    'Audit_FreeTextAssignments_RedactsPasswordAndTokenValues_NotJustKeys',
    'PayrollPeriod_StaleCalculate_CannotOverwritePublishedPeriod_UnderRace',
    'PayrollPeriod_MissingCalculationOrApprovalMetadata_FailsClosedWithConflict',
    'Audit_CredentialAliases_CurrentPasswordAndNewPassword_RedactedFromReasonAndPayload',
    'Audit_CredentialAliases_VariantsAndNestedPayload_AreRedacted',
    'Audit_SerializedCredentialJsonInReason_IsRedactedFromDbAndAuditResponse',
    'HrScope_UnassignedHr_CannotReadOrPatchOtherEmployee_OrApproveLeave',
    'HrScope_AssignedHr_CanAccessOnlyAssignedEmployees_UnassignedReturns403',
    'HrScope_CombinedHrAndPayroll_ReceivesPayrollProjectionForOutOfScopeEmployee',
    'HrScope_Revocation_TakesEffectImmediatelyOnNextRequest',
    'HrScope_AntiSelfAssignment_AndAdminHasNoImplicitPiiAccess',
    'HrScope_BulkAndList_EnforcesAssignedScope',
)


def verify_authorization_matrix(path):
    results = ET.parse(path).findall('.//t:UnitTestResult', NS)
    counts = {}
    for case in AUTHORIZATION_CASES:
        matched = [r for r in results if ('.' + case) in r.get('testName', '')]
        if len(matched) < 1 or any(r.get('outcome') != 'Passed' for r in matched):
            raise ValueError(f'Missing or failing authorization case: {case}')
        counts[case] = len(matched)
    return counts


if __name__ == '__main__':
    raise SystemExit(main(task='T07'))

