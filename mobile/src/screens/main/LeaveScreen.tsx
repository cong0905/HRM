import React, { useEffect, useState, useCallback } from "react";
import {
  View,
  Text,
  StyleSheet,
  ScrollView,
  ActivityIndicator,
  TouchableOpacity,
  RefreshControl,
  TextInput,
  Alert,
} from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";
import { Header } from "../../components/Header";
import { colors } from "../../theme/colors";
import { spacing } from "../../theme/spacing";
import { typography } from "../../theme/typography";
import {
  getMyLeaveBalances,
  getMyLeaveRequests,
  createLeaveRequest,
  cancelLeaveRequest,
} from "../../api/leave";
import { getFriendlyErrorMessage } from "../../api/errors";
import { generateIdempotencyKey } from "../../utils/uuid";
import type { LeaveBalanceResponse, LeaveRecordResponse } from "../../api/types";

interface LeaveScreenProps {
  onBack?: () => void;
}

export const LeaveScreen: React.FC<LeaveScreenProps> = ({ onBack }) => {
  const [balances, setBalances] = useState<LeaveBalanceResponse[]>([]);
  const [requests, setRequests] = useState<LeaveRecordResponse[]>([]);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [refreshing, setRefreshing] = useState<boolean>(false);
  const [isSubmitting, setIsSubmitting] = useState<boolean>(false);
  const [cancellingId, setCancellingId] = useState<string | null>(null);

  // Form tạo đơn nghỉ phép
  const [showCreateForm, setShowCreateForm] = useState<boolean>(false);
  const [startDate, setStartDate] = useState<string>("");
  const [endDate, setEndDate] = useState<string>("");
  const [reason, setReason] = useState<string>("");
  const [formError, setFormError] = useState<string | null>(null);
  const [bannerSuccess, setBannerSuccess] = useState<string | null>(null);
  const [bannerError, setBannerError] = useState<string | null>(null);

  const loadData = useCallback(async (bypassCache = false) => {
    try {
      const currentYear = new Date().getFullYear();
      const [balRes, reqRes] = await Promise.all([
        getMyLeaveBalances(currentYear, bypassCache),
        getMyLeaveRequests(1, 20, bypassCache),
      ]);
      setBalances(balRes || []);
      setRequests(reqRes.items || []);
    } catch (err) {
      const friendly = getFriendlyErrorMessage(err, "Không thể tải dữ liệu phép.");
      setBannerError(friendly.message);
    } finally {
      setIsLoading(false);
      setRefreshing(false);
    }
  }, []);

  useEffect(() => {
    void loadData();
  }, [loadData]);

  const onRefresh = useCallback(() => {
    setRefreshing(true);
    setBannerError(null);
    setBannerSuccess(null);
    void loadData(true);
  }, [loadData]);

  const handleCreateRequest = async () => {
    setFormError(null);
    const trimmedStart = startDate.trim();
    const trimmedEnd = endDate.trim();
    const trimmedReason = reason.trim();

    if (!trimmedStart || !trimmedEnd) {
      setFormError("Vui lòng nhập ngày bắt đầu và kết thúc (YYYY-MM-DD).");
      return;
    }

    const dateRegex = /^\d{4}-\d{2}-\d{2}$/;
    if (!dateRegex.test(trimmedStart) || !dateRegex.test(trimmedEnd)) {
      setFormError("Định dạng ngày không hợp lệ. Vui lòng sử dụng YYYY-MM-DD (VD: 2026-11-20).");
      return;
    }

    if (trimmedStart > trimmedEnd) {
      setFormError("Ngày bắt đầu không được lớn hơn ngày kết thúc.");
      return;
    }

    if (!trimmedReason) {
      setFormError("Vui lòng nhập lý do xin nghỉ phép.");
      return;
    }

    setIsSubmitting(true);
    setBannerError(null);
    setBannerSuccess(null);

    const idempotencyKey = generateIdempotencyKey();

    try {
      await createLeaveRequest(
        {
          startDate: trimmedStart,
          endDate: trimmedEnd,
          reason: trimmedReason,
        },
        idempotencyKey
      );

      setBannerSuccess("Tạo đơn nghỉ phép thành công. Đơn đang chờ cấp quản lý phê duyệt.");
      setShowCreateForm(false);
      setStartDate("");
      setEndDate("");
      setReason("");
      // Tải lại dữ liệu sau khi tạo đơn
      void loadData(true);
    } catch (err) {
      const friendly = getFriendlyErrorMessage(err, "Tạo đơn nghỉ phép thất bại.");
      setFormError(friendly.message);
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleCancelRequest = async (item: LeaveRecordResponse) => {
    if (cancellingId) return;

    setCancellingId(item.id);
    setBannerError(null);
    setBannerSuccess(null);

    const idempotencyKey = generateIdempotencyKey();

    try {
      await cancelLeaveRequest(
        item.id,
        "Nhân viên chủ động hủy đơn qua ứng dụng",
        item.rowVersion || undefined,
        idempotencyKey
      );
      setBannerSuccess(`Đã hủy thành công đơn nghỉ phép ngày ${item.startDate}.`);
      void loadData(true);
    } catch (err) {
      const friendly = getFriendlyErrorMessage(err, "Không thể hủy đơn nghỉ phép này.");
      setBannerError(friendly.message);
    } finally {
      setCancellingId(null);
    }
  };

  const currentBalance = balances[0];

  return (
    <SafeAreaView style={styles.safeArea}>
      <Header title="Quản lý nghỉ phép" onBack={onBack} />
      <ScrollView
        contentContainerStyle={styles.scrollContent}
        refreshControl={
          <RefreshControl refreshing={refreshing} onRefresh={onRefresh} colors={[colors.primary]} />
        }
      >
        {/* Banner thông báo */}
        {bannerError && (
          <View style={styles.bannerError}>
            <Text style={styles.bannerErrorText}>{bannerError}</Text>
          </View>
        )}

        {bannerSuccess && (
          <View style={styles.bannerSuccess}>
            <Text style={styles.bannerSuccessText}>{bannerSuccess}</Text>
          </View>
        )}

        {/* Quỹ phép (Leave Balance Preview) */}
        <View style={styles.cardBalance}>
          <Text style={styles.cardHeaderTitle}>
            Quỹ phép năm {currentBalance?.year || new Date().getFullYear()}
          </Text>

          <View style={styles.balanceGrid}>
            <View style={styles.balanceCol}>
              <Text style={styles.balanceNumHighlight}>
                {currentBalance?.remainingDays ?? "--"}
              </Text>
              <Text style={styles.balanceLabel}>Ngày còn lại</Text>
            </View>

            <View style={styles.balanceDivider} />

            <View style={styles.balanceCol}>
              <Text style={styles.balanceNum}>{currentBalance?.usedDays ?? "0"}</Text>
              <Text style={styles.balanceLabel}>Đã sử dụng</Text>
            </View>

            <View style={styles.balanceDivider} />

            <View style={styles.balanceCol}>
              <Text style={styles.balanceNum}>{currentBalance?.allocatedDays ?? "12"}</Text>
              <Text style={styles.balanceLabel}>Được cấp</Text>
            </View>
          </View>
        </View>

        {/* Nút bật form tạo đơn */}
        {!showCreateForm ? (
          <TouchableOpacity
            style={styles.openFormBtn}
            onPress={() => {
              setShowCreateForm(true);
              setFormError(null);
            }}
          >
            <Text style={styles.openFormBtnText}>+ Tạo đơn xin nghỉ phép</Text>
          </TouchableOpacity>
        ) : (
          /* Form tạo đơn nghỉ phép */
          <View style={styles.formCard}>
            <Text style={styles.formTitle}>Tạo đơn xin nghỉ phép mới</Text>

            {formError && (
              <View style={styles.formErrorBox}>
                <Text style={styles.formErrorText}>{formError}</Text>
              </View>
            )}

            <Text style={styles.inputLabel}>Từ ngày (YYYY-MM-DD):</Text>
            <TextInput
              style={styles.input}
              placeholder="VD: 2026-11-20"
              placeholderTextColor={colors.textMuted}
              value={startDate}
              onChangeText={setStartDate}
              editable={!isSubmitting}
            />

            <Text style={styles.inputLabel}>Đến ngày (YYYY-MM-DD):</Text>
            <TextInput
              style={styles.input}
              placeholder="VD: 2026-11-21"
              placeholderTextColor={colors.textMuted}
              value={endDate}
              onChangeText={setEndDate}
              editable={!isSubmitting}
            />

            <Text style={styles.inputLabel}>Lý do nghỉ phép:</Text>
            <TextInput
              style={[styles.input, styles.textArea]}
              placeholder="Nhập lý do chi tiết..."
              placeholderTextColor={colors.textMuted}
              value={reason}
              onChangeText={setReason}
              multiline={true}
              numberOfLines={3}
              editable={!isSubmitting}
            />

            <View style={styles.formBtnRow}>
              <TouchableOpacity
                style={styles.cancelBtn}
                onPress={() => setShowCreateForm(false)}
                disabled={isSubmitting}
              >
                <Text style={styles.cancelBtnText}>Hủy bỏ</Text>
              </TouchableOpacity>

              <TouchableOpacity
                style={[styles.submitBtn, isSubmitting && styles.disabledBtn]}
                onPress={handleCreateRequest}
                disabled={isSubmitting}
              >
                {isSubmitting ? (
                  <ActivityIndicator size="small" color="#fff" />
                ) : (
                  <Text style={styles.submitBtnText}>Gửi đơn</Text>
                )}
              </TouchableOpacity>
            </View>
          </View>
        )}

        {/* Danh sách đơn nghỉ phép */}
        <View style={styles.listSection}>
          <Text style={styles.sectionTitle}>Lịch sử đơn nghỉ phép</Text>

          {isLoading && !refreshing ? (
            <ActivityIndicator
              size="large"
              color={colors.primary}
              style={{ marginVertical: spacing.lg }}
            />
          ) : requests.length === 0 ? (
            <View style={styles.emptyCard}>
              <Text style={styles.emptyText}>Chưa có đơn xin nghỉ phép nào.</Text>
            </View>
          ) : (
            requests.map((item) => {
              const isPending = item.status === "Pending";
              const isApproved = item.status === "Approved";
              const isRejected = item.status === "Rejected";
              const isCancelled = item.status === "Cancelled";

              return (
                <View key={item.id} style={styles.requestCard}>
                  <View style={styles.requestCardHeader}>
                    <Text style={styles.requestDates}>
                      {item.startDate} → {item.endDate}
                    </Text>
                    <View
                      style={[
                        styles.statusPill,
                        isApproved && styles.pillApproved,
                        isPending && styles.pillPending,
                        isRejected && styles.pillRejected,
                        isCancelled && styles.pillCancelled,
                      ]}
                    >
                      <Text
                        style={[
                          styles.statusPillText,
                          isApproved && styles.textApproved,
                          isPending && styles.textPending,
                          isRejected && styles.textRejected,
                          isCancelled && styles.textCancelled,
                        ]}
                      >
                        {item.status}
                      </Text>
                    </View>
                  </View>

                  <Text style={styles.requestDays}>Số ngày làm việc: {item.workingDays} ngày</Text>
                  <Text style={styles.requestReason}>Lý do: {item.reason}</Text>

                  {item.decisionReason && (
                    <Text style={styles.decisionNote}>Phản hồi duyệt: {item.decisionReason}</Text>
                  )}

                  {item.cancellationReason && (
                    <Text style={styles.cancelNote}>Lý do hủy: {item.cancellationReason}</Text>
                  )}

                  {/* Chỉ chủ đơn mới được hủy đơn Pending */}
                  {isPending && (
                    <View style={styles.actionRow}>
                      <TouchableOpacity
                        style={[
                          styles.cancelRequestBtn,
                          cancellingId === item.id && styles.disabledBtn,
                        ]}
                        onPress={() => handleCancelRequest(item)}
                        disabled={cancellingId === item.id}
                      >
                        {cancellingId === item.id ? (
                          <ActivityIndicator size="small" color="#dc2626" />
                        ) : (
                          <Text style={styles.cancelRequestBtnText}>Hủy đơn Pending</Text>
                        )}
                      </TouchableOpacity>
                    </View>
                  )}
                </View>
              );
            })
          )}
        </View>
      </ScrollView>
    </SafeAreaView>
  );
};

const styles = StyleSheet.create({
  safeArea: {
    flex: 1,
    backgroundColor: colors.background,
  },
  scrollContent: {
    padding: spacing.lg,
  },
  bannerError: {
    backgroundColor: colors.errorLight,
    padding: spacing.md,
    borderRadius: spacing.radiusSm,
    marginBottom: spacing.md,
    borderWidth: 1,
    borderColor: colors.error,
  },
  bannerErrorText: {
    ...typography.captionMedium,
    color: colors.error,
  },
  bannerSuccess: {
    backgroundColor: colors.successLight,
    padding: spacing.md,
    borderRadius: spacing.radiusSm,
    marginBottom: spacing.md,
    borderWidth: 1,
    borderColor: colors.success,
  },
  bannerSuccessText: {
    ...typography.captionMedium,
    color: colors.success,
  },
  cardBalance: {
    backgroundColor: colors.surface,
    padding: spacing.lg,
    borderRadius: spacing.radiusSm,
    borderWidth: 1,
    borderColor: colors.border,
    marginBottom: spacing.md,
  },
  cardHeaderTitle: {
    ...typography.h3,
    color: colors.textPrimary,
    marginBottom: spacing.md,
    textAlign: "center",
  },
  balanceGrid: {
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-around",
  },
  balanceCol: {
    alignItems: "center",
    flex: 1,
  },
  balanceNumHighlight: {
    ...typography.h1,
    color: colors.primary,
    fontSize: 26,
    fontWeight: "700",
  },
  balanceNum: {
    ...typography.h1,
    color: colors.textPrimary,
    fontSize: 22,
  },
  balanceLabel: {
    ...typography.caption,
    color: colors.textSecondary,
    marginTop: 4,
  },
  balanceDivider: {
    width: 1,
    height: 40,
    backgroundColor: colors.border,
  },
  openFormBtn: {
    backgroundColor: colors.primary,
    padding: spacing.md,
    borderRadius: spacing.radiusSm,
    alignItems: "center",
    marginBottom: spacing.lg,
  },
  openFormBtnText: {
    ...typography.button,
    color: "#fff",
  },
  formCard: {
    backgroundColor: colors.surface,
    padding: spacing.lg,
    borderRadius: spacing.radiusSm,
    borderWidth: 1,
    borderColor: colors.border,
    marginBottom: spacing.lg,
  },
  formTitle: {
    ...typography.h3,
    color: colors.textPrimary,
    marginBottom: spacing.md,
  },
  formErrorBox: {
    backgroundColor: colors.errorLight,
    padding: spacing.sm,
    borderRadius: spacing.radiusSm,
    marginBottom: spacing.sm,
    borderWidth: 1,
    borderColor: colors.error,
  },
  formErrorText: {
    ...typography.caption,
    color: colors.error,
  },
  inputLabel: {
    ...typography.captionMedium,
    color: colors.textSecondary,
    marginBottom: 4,
    marginTop: spacing.xs,
  },
  input: {
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: spacing.radiusSm,
    padding: spacing.sm,
    color: colors.textPrimary,
    backgroundColor: colors.background,
    marginBottom: spacing.sm,
    fontSize: 14,
  },
  textArea: {
    minHeight: 64,
    textAlignVertical: "top",
  },
  formBtnRow: {
    flexDirection: "row",
    justifyContent: "flex-end",
    gap: spacing.md,
    marginTop: spacing.sm,
  },
  cancelBtn: {
    paddingVertical: spacing.sm,
    paddingHorizontal: spacing.md,
    borderRadius: spacing.radiusSm,
  },
  cancelBtnText: {
    ...typography.captionMedium,
    color: colors.textSecondary,
  },
  submitBtn: {
    backgroundColor: colors.primary,
    paddingVertical: spacing.sm,
    paddingHorizontal: spacing.lg,
    borderRadius: spacing.radiusSm,
    minWidth: 90,
    alignItems: "center",
  },
  submitBtnText: {
    ...typography.button,
    color: "#fff",
  },
  disabledBtn: {
    opacity: 0.5,
  },
  listSection: {
    marginTop: spacing.xs,
  },
  sectionTitle: {
    ...typography.h3,
    color: colors.textPrimary,
    marginBottom: spacing.md,
  },
  emptyCard: {
    backgroundColor: colors.surface,
    padding: spacing.lg,
    borderRadius: spacing.radiusSm,
    borderWidth: 1,
    borderColor: colors.border,
    alignItems: "center",
  },
  emptyText: {
    ...typography.caption,
    color: colors.textMuted,
  },
  requestCard: {
    backgroundColor: colors.surface,
    padding: spacing.md,
    borderRadius: spacing.radiusSm,
    borderWidth: 1,
    borderColor: colors.border,
    marginBottom: spacing.sm,
  },
  requestCardHeader: {
    flexDirection: "row",
    justifyContent: "space-between",
    alignItems: "center",
    marginBottom: spacing.xs,
  },
  requestDates: {
    ...typography.bodyMedium,
    fontWeight: "700",
    color: colors.textPrimary,
  },
  statusPill: {
    paddingHorizontal: spacing.sm,
    paddingVertical: 2,
    borderRadius: spacing.radiusSm,
  },
  pillApproved: {
    backgroundColor: colors.successLight,
  },
  pillPending: {
    backgroundColor: "#fef3c7",
  },
  pillRejected: {
    backgroundColor: colors.errorLight,
  },
  pillCancelled: {
    backgroundColor: colors.background,
  },
  statusPillText: {
    ...typography.captionMedium,
    fontSize: 12,
  },
  textApproved: {
    color: colors.success,
  },
  textPending: {
    color: "#b45309",
  },
  textRejected: {
    color: colors.error,
  },
  textCancelled: {
    color: colors.textMuted,
  },
  requestDays: {
    ...typography.caption,
    color: colors.textSecondary,
    marginBottom: 2,
  },
  requestReason: {
    ...typography.body,
    color: colors.textPrimary,
    fontSize: 13,
  },
  decisionNote: {
    ...typography.caption,
    color: colors.primary,
    marginTop: 4,
    fontStyle: "italic",
  },
  cancelNote: {
    ...typography.caption,
    color: colors.textMuted,
    marginTop: 4,
    fontStyle: "italic",
  },
  actionRow: {
    marginTop: spacing.sm,
    alignItems: "flex-end",
  },
  cancelRequestBtn: {
    borderWidth: 1,
    borderColor: "#dc2626",
    paddingHorizontal: spacing.md,
    paddingVertical: 4,
    borderRadius: spacing.radiusSm,
  },
  cancelRequestBtnText: {
    ...typography.captionMedium,
    color: "#dc2626",
  },
});
