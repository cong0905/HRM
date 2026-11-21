import React, { useEffect, useState, useCallback } from "react";
import {
  View,
  Text,
  StyleSheet,
  ScrollView,
  ActivityIndicator,
  TouchableOpacity,
  RefreshControl,
} from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";
import { Header } from "../../components/Header";
import { colors } from "../../theme/colors";
import { spacing } from "../../theme/spacing";
import { typography } from "../../theme/typography";
import { getMyPayslips, getMyPayslipDetail } from "../../api/payroll";
import { getFriendlyErrorMessage } from "../../api/errors";
import type { PayslipResponse, PayslipDetailResponse } from "../../api/types";

interface PayrollScreenProps {
  onBack?: () => void;
}

export const PayrollScreen: React.FC<PayrollScreenProps> = ({ onBack }) => {
  const [payslips, setPayslips] = useState<PayslipResponse[]>([]);
  const [selectedPayslipId, setSelectedPayslipId] = useState<string | null>(null);
  const [detail, setDetail] = useState<PayslipDetailResponse | null>(null);
  const [isLoadingList, setIsLoadingList] = useState<boolean>(true);
  const [isLoadingDetail, setIsLoadingDetail] = useState<boolean>(false);
  const [refreshing, setRefreshing] = useState<boolean>(false);
  const [bannerError, setBannerError] = useState<string | null>(null);

  const loadList = useCallback(async (bypassCache = false) => {
    try {
      setBannerError(null);
      const res = await getMyPayslips(1, 20, bypassCache);
      setPayslips(res.items || []);
    } catch (err) {
      const friendly = getFriendlyErrorMessage(err, "Không thể tải danh sách phiếu lương.");
      setBannerError(friendly.message);
    } finally {
      setIsLoadingList(false);
      setRefreshing(false);
    }
  }, []);

  const loadDetail = useCallback(async (id: string, bypassCache = false) => {
    setIsLoadingDetail(true);
    setBannerError(null);
    try {
      const res = await getMyPayslipDetail(id, bypassCache);
      setDetail(res);
    } catch (err) {
      const friendly = getFriendlyErrorMessage(err, "Không thể tải chi tiết phiếu lương.");
      setBannerError(friendly.message);
    } finally {
      setIsLoadingDetail(false);
    }
  }, []);

  useEffect(() => {
    void loadList();
  }, [loadList]);

  const onRefresh = useCallback(() => {
    setRefreshing(true);
    if (selectedPayslipId) {
      void loadDetail(selectedPayslipId, true);
    } else {
      void loadList(true);
    }
  }, [selectedPayslipId, loadDetail, loadList]);

  const handleSelectPayslip = (id: string) => {
    setSelectedPayslipId(id);
    void loadDetail(id);
  };

  const handleBackToList = () => {
    setSelectedPayslipId(null);
    setDetail(null);
    setBannerError(null);
  };

  const formatCurrency = (amountStr?: string | null, currency = "VND"): string => {
    if (!amountStr) return "0 " + currency;
    const num = parseFloat(amountStr);
    if (isNaN(num)) return amountStr + " " + currency;
    return num.toLocaleString("vi-VN") + " " + currency;
  };

  const formatDate = (isoString?: string | null): string => {
    if (!isoString) return "--/--/----";
    try {
      const d = new Date(isoString);
      return d.toLocaleDateString("vi-VN", {
        day: "2-digit",
        month: "2-digit",
        year: "numeric",
      });
    } catch {
      return isoString;
    }
  };

  return (
    <SafeAreaView style={styles.safeArea}>
      <Header
        title={selectedPayslipId ? "Chi tiết phiếu lương" : "Phiếu lương đã công bố"}
        onBack={selectedPayslipId ? handleBackToList : onBack}
      />
      <ScrollView
        contentContainerStyle={styles.scrollContent}
        refreshControl={
          <RefreshControl refreshing={refreshing} onRefresh={onRefresh} colors={[colors.primary]} />
        }
      >
        {/* Banner thông báo lỗi */}
        {bannerError && (
          <View style={styles.bannerError}>
            <Text style={styles.bannerErrorText}>{bannerError}</Text>
          </View>
        )}

        {/* Chế độ xem chi tiết phiếu lương */}
        {selectedPayslipId ? (
          isLoadingDetail ? (
            <View style={styles.centerBox}>
              <ActivityIndicator size="large" color={colors.primary} />
              <Text style={styles.loadingText}>Đang tải chi tiết thành phần lương...</Text>
            </View>
          ) : detail ? (
            <View style={styles.detailContainer}>
              {/* Thẻ tổng quan Thực nhận (Net Pay) */}
              <View style={styles.netCard}>
                <Text style={styles.netCardLabel}>LƯƠNG THỰC NHẬN (NET)</Text>
                <Text style={styles.netAmountText}>
                  {formatCurrency(detail.calculation.net, detail.currency)}
                </Text>
                <Text style={styles.netSubText}>
                  Kỳ lương: {detail.periodCode} • Ngày công bố: {formatDate(detail.publishedAtUtc)}
                </Text>
                <View style={styles.publishedBadge}>
                  <Text style={styles.publishedBadgeText}>ĐÃ CÔNG BỐ (PUBLISHED)</Text>
                </View>
              </View>

              {/* Giải thích thành phần thu nhập */}
              <View style={styles.sectionCard}>
                <Text style={styles.sectionTitle}>Các khoản thu nhập</Text>

                <View style={styles.rowItem}>
                  <Text style={styles.rowLabel}>Lương tiêu chuẩn (Standard Pay):</Text>
                  <Text style={styles.rowValue}>
                    {formatCurrency(detail.calculation.standardPay, detail.currency)}
                  </Text>
                </View>

                <View style={styles.rowItem}>
                  <Text style={styles.rowLabel}>Lương làm thêm giờ (Overtime Pay):</Text>
                  <Text style={styles.rowValue}>
                    {formatCurrency(detail.calculation.overtimePay, detail.currency)}
                  </Text>
                </View>

                <View style={[styles.rowItem, styles.highlightRow]}>
                  <Text style={styles.rowLabelBold}>Tổng thu nhập trước giảm trừ (Gross):</Text>
                  <Text style={styles.rowValueBold}>
                    {formatCurrency(detail.calculation.gross, detail.currency)}
                  </Text>
                </View>
              </View>

              {/* Dòng tiền chi tiết & Giảm trừ */}
              {detail.calculation.lines && detail.calculation.lines.length > 0 && (
                <View style={styles.sectionCard}>
                  <Text style={styles.sectionTitle}>Khoản phụ cấp & Giảm trừ quy định</Text>
                  {detail.calculation.lines.map((line, idx) => (
                    <View key={idx} style={styles.rowItem}>
                      <Text style={styles.rowLabel}>{line.code}:</Text>
                      <Text style={styles.rowValue}>
                        {formatCurrency(line.amount, detail.currency)}
                      </Text>
                    </View>
                  ))}
                </View>
              )}

              {/* Phân tích thời gian công và làm việc */}
              <View style={styles.sectionCard}>
                <Text style={styles.sectionTitle}>Thống kê thời gian làm việc</Text>

                <View style={styles.rowItem}>
                  <Text style={styles.rowLabel}>Thời gian tiêu chuẩn tháng:</Text>
                  <Text style={styles.rowValue}>
                    {detail.calculation.standardMonthMinutes} phút (~
                    {(parseFloat(detail.calculation.standardMonthMinutes || "0") / 60).toFixed(1)}{" "}
                    giờ)
                  </Text>
                </View>

                <View style={styles.rowItem}>
                  <Text style={styles.rowLabel}>Thời gian công được tính:</Text>
                  <Text style={styles.rowValue}>
                    {detail.calculation.creditedMinutes} phút (~
                    {(parseFloat(detail.calculation.creditedMinutes || "0") / 60).toFixed(1)} giờ)
                  </Text>
                </View>

                <View style={styles.rowItem}>
                  <Text style={styles.rowLabel}>Thời gian OT được phê duyệt:</Text>
                  <Text style={styles.rowValue}>
                    {detail.calculation.approvedOvertimeMinutes} phút
                  </Text>
                </View>
              </View>

              {/* Giải thích điều chỉnh làm tròn */}
              <View style={styles.sectionCard}>
                <Text style={styles.sectionTitle}>Quy tắc làm tròn</Text>

                <View style={styles.rowItem}>
                  <Text style={styles.rowLabel}>Thực nhận trước làm tròn:</Text>
                  <Text style={styles.rowValue}>
                    {formatCurrency(detail.calculation.unroundedNet, detail.currency)}
                  </Text>
                </View>

                <View style={styles.rowItem}>
                  <Text style={styles.rowLabel}>Điều chỉnh làm tròn (Rounding):</Text>
                  <Text style={styles.rowValue}>
                    {formatCurrency(detail.calculation.roundingAdjustment, detail.currency)}
                  </Text>
                </View>

                <Text style={styles.policyNotice}>
                  Chính sách áp dụng: Phiên bản {detail.policy.version} • Mẫu hợp đồng chuẩn hóa Asia/Ho_Chi_Minh.
                </Text>
              </View>

              <TouchableOpacity style={styles.backBtn} onPress={handleBackToList}>
                <Text style={styles.backBtnText}>← Quay lại danh sách phiếu lương</Text>
              </TouchableOpacity>
            </View>
          ) : null
        ) : (
          /* Chế độ xem danh sách phiếu lương */
          <View>
            <Text style={styles.listHeaderSubtitle}>
              Chỉ các phiếu lương ở trạng thái Published (đã được phòng Lương phê duyệt và công bố)
              mới hiển thị trên thiết bị di động.
            </Text>

            {isLoadingList && !refreshing ? (
              <ActivityIndicator
                size="large"
                color={colors.primary}
                style={{ marginVertical: spacing.xxl }}
              />
            ) : payslips.length === 0 ? (
              <View style={styles.emptyCard}>
                <Text style={styles.emptyTitle}>Chưa có phiếu lương công bố</Text>
                <Text style={styles.emptyText}>
                  Hiện tại bạn chưa có phiếu lương nào được công bố trong hệ thống.
                </Text>
              </View>
            ) : (
              payslips.map((item) => (
                <TouchableOpacity
                  key={item.id}
                  style={styles.payslipCard}
                  onPress={() => handleSelectPayslip(item.id)}
                >
                  <View style={styles.payslipCardHeader}>
                    <Text style={styles.payslipAmountText}>{formatCurrency(item.amount)}</Text>
                    <View style={styles.statusBadgeSmall}>
                      <Text style={styles.statusBadgeTextSmall}>{item.status}</Text>
                    </View>
                  </View>

                  <Text style={styles.payslipDateText}>
                    Ngày công bố: {formatDate(item.publishedAtUtc)}
                  </Text>

                  <View style={styles.viewDetailRow}>
                    <Text style={styles.viewDetailText}>Xem chi tiết thành phần lương →</Text>
                  </View>
                </TouchableOpacity>
              ))
            )}
          </View>
        )}
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
  centerBox: {
    paddingVertical: spacing.xxl,
    alignItems: "center",
  },
  loadingText: {
    ...typography.caption,
    color: colors.textSecondary,
    marginTop: spacing.md,
  },
  listHeaderSubtitle: {
    ...typography.caption,
    color: colors.textSecondary,
    marginBottom: spacing.md,
    lineHeight: 18,
  },
  emptyCard: {
    backgroundColor: colors.surface,
    padding: spacing.xl,
    borderRadius: spacing.radiusSm,
    borderWidth: 1,
    borderColor: colors.border,
    alignItems: "center",
  },
  emptyTitle: {
    ...typography.h3,
    color: colors.textPrimary,
    marginBottom: spacing.xs,
  },
  emptyText: {
    ...typography.caption,
    color: colors.textMuted,
    textAlign: "center",
  },
  payslipCard: {
    backgroundColor: colors.surface,
    padding: spacing.lg,
    borderRadius: spacing.radiusSm,
    borderWidth: 1,
    borderColor: colors.border,
    marginBottom: spacing.md,
  },
  payslipCardHeader: {
    flexDirection: "row",
    justifyContent: "space-between",
    alignItems: "center",
    marginBottom: spacing.xs,
  },
  payslipAmountText: {
    ...typography.h2,
    color: colors.primary,
    fontSize: 20,
    fontWeight: "700",
  },
  statusBadgeSmall: {
    backgroundColor: colors.successLight,
    paddingHorizontal: spacing.sm,
    paddingVertical: 2,
    borderRadius: spacing.radiusSm,
  },
  statusBadgeTextSmall: {
    ...typography.captionMedium,
    color: colors.success,
    fontSize: 11,
  },
  payslipDateText: {
    ...typography.caption,
    color: colors.textSecondary,
    marginBottom: spacing.sm,
  },
  viewDetailRow: {
    borderTopWidth: 1,
    borderTopColor: colors.border,
    paddingTop: spacing.xs,
  },
  viewDetailText: {
    ...typography.captionMedium,
    color: colors.primary,
  },
  detailContainer: {
    gap: spacing.md,
  },
  netCard: {
    backgroundColor: colors.surface,
    padding: spacing.xl,
    borderRadius: spacing.radiusSm,
    borderWidth: 1,
    borderColor: colors.primary,
    alignItems: "center",
  },
  netCardLabel: {
    ...typography.captionMedium,
    color: colors.textSecondary,
    letterSpacing: 1,
  },
  netAmountText: {
    ...typography.h1,
    color: colors.primary,
    fontSize: 28,
    fontWeight: "800",
    marginVertical: spacing.xs,
  },
  netSubText: {
    ...typography.caption,
    color: colors.textSecondary,
    marginBottom: spacing.sm,
  },
  publishedBadge: {
    backgroundColor: colors.successLight,
    paddingHorizontal: spacing.md,
    paddingVertical: 4,
    borderRadius: spacing.radiusSm,
  },
  publishedBadgeText: {
    ...typography.captionMedium,
    color: colors.success,
    fontSize: 11,
    letterSpacing: 0.5,
  },
  sectionCard: {
    backgroundColor: colors.surface,
    padding: spacing.lg,
    borderRadius: spacing.radiusSm,
    borderWidth: 1,
    borderColor: colors.border,
  },
  sectionTitle: {
    ...typography.h3,
    color: colors.textPrimary,
    marginBottom: spacing.md,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
    paddingBottom: spacing.xs,
  },
  rowItem: {
    flexDirection: "row",
    justifyContent: "space-between",
    alignItems: "center",
    marginBottom: spacing.sm,
  },
  highlightRow: {
    borderTopWidth: 1,
    borderTopColor: colors.border,
    paddingTop: spacing.xs,
    marginTop: spacing.xs,
  },
  rowLabel: {
    ...typography.captionMedium,
    color: colors.textSecondary,
    flex: 1,
  },
  rowValue: {
    ...typography.bodyMedium,
    color: colors.textPrimary,
    fontWeight: "600",
  },
  rowLabelBold: {
    ...typography.bodyMedium,
    fontWeight: "700",
    color: colors.textPrimary,
    flex: 1,
  },
  rowValueBold: {
    ...typography.h3,
    color: colors.textPrimary,
  },
  policyNotice: {
    ...typography.caption,
    color: colors.textMuted,
    fontStyle: "italic",
    marginTop: spacing.xs,
  },
  backBtn: {
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.border,
    padding: spacing.md,
    borderRadius: spacing.radiusSm,
    alignItems: "center",
    marginTop: spacing.xs,
    marginBottom: spacing.lg,
  },
  backBtnText: {
    ...typography.captionMedium,
    color: colors.textPrimary,
  },
});
