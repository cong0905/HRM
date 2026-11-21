"""T06 auth gate: complete SQL auth matrix plus foundation/contracts/audit/legacy gate."""
import xml.etree.ElementTree as ET
from verify_t05 import main, NS

AUTH_CASES = (
    'ApprovedAuthLifetimesAndSevenDayRefreshExpiry_AreEnforced',
    'Invitation_RequiresActivationAndNormalizedUsernameIsUnique',
    'WebCookie_CsrfAndLogout_AreEnforced',
    'CommittedRevocation_RejectsExistingCookieBearerAndRefresh',
    'Refresh_RotatesAtomicallyAndRejectsReplayAndWrongPurpose',
    'Expiry_AccessCookieAndRefreshHaveIndependentLifetimes',
    'ResetTokens_RejectEmailPurposeWrongAccountUsedAndConcurrentReplay',
    'ExpiredResetToken_IsRejected',
    'ForgotPassword_IsHonestAboutMissingEmailAndDoesNotEnumerate',
    'TestSender_IsExplicitlyLabelledAndNeverReturnsResetTokenInResponse',
    'LockoutAndIpLimit_BlockPasswordAbuseWithoutAccountEnumeration',
    'LogoutCurrentSession_RejectsItsTokensAndPreservesOtherSessions',
    'PublicRegistrationCannotCreateRolesAndPasswordsHaveNoDefaultBypass',
    'AuthOverPlainHttp_DoesNotIssueCredentials',
    'RefreshHasAbsoluteExpiryAndAuthenticatedAccountHasNoImplicitBusinessPermission',
    'IdentityMigration_UpgradeAndDownPreserveFoundationData',
    'ConcurrentRefreshAndDisable_NeverLeaveUsableTokensAfterCommit',
    'OffboardingPort_ParticipatesInCallerTransactionAndRollsBackWithIt',
)


def verify_auth_matrix(path):
    results = ET.parse(path).findall('.//t:UnitTestResult', NS)
    counts = {}
    for case in AUTH_CASES:
        matched = [r for r in results if ('.' + case) in r.get('testName', '')]
        expected = 6 if case.startswith('CommittedRevocation_') else 1
        if len(matched) != expected or any(r.get('outcome') != 'Passed' for r in matched):
            raise ValueError(f'Missing or failing auth case: {case}')
        counts[case] = len(matched)
    return counts


if __name__ == '__main__':
    raise SystemExit(main(task='T06'))
