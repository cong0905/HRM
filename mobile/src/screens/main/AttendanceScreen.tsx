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
} from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";
import { Header } from "../../components/Header";
import { colors } from "../../theme/colors";
import { spacing } from "../../theme/spacing";
import { typography } from "../../theme/typography";
import {
  getOwnTodayAttendance,
  getOwnAttendanceHistory,
  checkIn,
  checkOut,
} from "../../api/attendance";
import { getFriendlyErrorMessage } from "../../api/errors";
import { generateIdempotencyKey } from "../../utils/uuid";
import type { AttendanceDayResponse } from "../../api/types";

interface AttendanceScreenProps {
  onBack?: () => void;
}

export const AttendanceScreen: React.FC<AttendanceScreenProps> = ({ onBack }) => {
  const [todayAttendance, setTodayAttendance] = useState<AttendanceDayResponse | null>(null);
  const [historyItems, setHistoryItems] = useState<AttendanceDayResponse[]>([]);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [isSubmitting, setIsSubmitting] = useState<boolean>(false);
  const [refreshing, setRefreshing] = useState<boolean>(false);
  const [errorBanner, setErrorBanner] = useState<string | null>(null);
  const [successBanner, setSuccessBanner] = useState<string | null>(null);
  const [note, setNote] = useState<string>("");

  const loadData = useCallback(async (bypassCache = false) => {
    setErrorBanner(null);
    try {
      const [todayRes, historyRes] = await Promise.all([
        getOwnTodayAttendance(bypassCache),
        getOwnAttendanceHistory(1, 20, bypassCache),
      ]);
      setTodayAttendance(todayRes);
      setHistoryItems(historyRes.items || []);
    } catch (err) {
      const friendly = getFriendlyErrorMessage(err, "Không thể tải dữ liệu chấm công.");
      setErrorBanner(friendly.message);
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
    setErrorBanner(null);
    setSuccessBanner(null);
    void loadData(true);
  }, [loadData]);

  const handleCheckIn = async () => {
    if (isSubmitting) return;
    setIsSubmitting(true);
    setErrorBanner(null);
    setSuccessBanner(null);

    const idempotencyKey = generateIdempotencyKey();

    try {
      const updated = await checkIn(note.trim() || undefined, idempotencyKey);
      // Chỉ cập nhật trạng thái khi đã nhận phản hồi xác nhận từ máy chủ
      setTodayAttendance(updated);
      setSuccessBanner("Chấm công vào (Check-in) thành công lúc " + formatTime(updated.checkInUtc));
      setNote("");
      // Tải lại lịch sử công
      void loadData(true);
    } catch (err) {
      // Mất mạng hoặc timeout: tuyệt đối không coi là thành công
      const friendly = getFriendlyErrorMessage(err, "Chấm công vào thất bại. Vui lòng thử lại.");
      setErrorBanner(friendly.message);
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleCheckOut = async () => {
    if (isSubmitting) return;
    setIsSubmitting(true);
    setErrorBanner(null);
    setSuccessBanner(null);

    const idempotencyKey = generateIdempotencyKey();

    try {
      const updated = await checkOut(note.trim() || undefined, idempotencyKey);
      // Chỉ cập nhật trạng thái khi đã nhận phản hồi xác nhận từ máy chủ
      setTodayAttendance(updated);
      setSuccessBanner(
        "Chấm công ra (Check-out) thành công lúc " +
          (updated.checkOutUtc ? formatTime(updated.checkOutUtc) : "bây giờ")
      );
      setNote("");
      // Tải lại lịch sử công
      void loadData(true);
    } catch (err) {
      // Mất mạng hoặc timeout: tuyệt đối không coi là thành công
      const friendly = getFriendlyErrorMessage(err, "Chấm công ra thất bại. Vui lòng thử lại.");
      setErrorBanner(friendly.message);
    } finally {
      setIsSubmitting(false);
    }
  };

  const formatTime = (isoString?: string | null): string => {
    if (!isoString) return "--:--";
    try {
      const d = new Date(isoString);
      return d.toLocaleTimeString("vi-VN", {
        hour: "2-digit",
        minute: "2-digit",
        second: "2-digit",
      });
    } catch {
      return isoString;
    }
  };

  const formatDate = (isoString?: string | null): string => {
    if (!isoString) return "";
    try {
      const d = new Date(isoString);
      return d.toLocaleDateString("vi-VN", {
        weekday: "short",
        day: "2-digit",
        month: "2-digit",
        year: "numeric",
      });
    } catch {
      return isoString;
    }
  };

  const hasCheckedIn = Boolean(todayAttendance?.checkInUtc);
  const hasCheckedOut = Boolean(todayAttendance?.checkOutUtc);

  return (
    <SafeAreaView style={styles.safeArea}>
      <Header title="Chấm công trực tuyến" onBack={onBack} />
      <ScrollView
        contentContainerStyle={styles.scrollContent}
        refreshControl={
          <RefreshControl refreshing={refreshing} onRefresh={onRefresh} colors={[colors.primary]} />
        }
      >
        {/* Banner thông báo trạng thái */}
        {errorBanner && (
          <View style={styles.bannerError} accessibilityRole="alert">
            <Text style={styles.bannerErrorText}>{errorBanner}</Text>
          </View>
        )}

        {successBanner && (
          <View style={styles.bannerSuccess}>
            <Text style={styles.bannerSuccessText}>{successBanner}</Text>
          </View>
        )}

        {/* Khối trạng thái hôm nay */}
        <View style={styles.cardToday}>
          <Text style={styles.sectionSubtitle}>HÔM NAY</Text>
          <Text style={styles.todayDateText}>{formatDate(new Date().toISOString())}</Text>

          <View style={styles.timeRowContainer}>
            <View style={styles.timeBox}>
              <Text style={styles.timeLabel}>Giờ vào (Check-in)</Text>
              <Text style={styles.timeValue}>{formatTime(todayAttendance?.checkInUtc)}</Text>
            </View>

            <View style={styles.timeDivider} />

            <View style={styles.timeBox}>
              <Text style={styles.timeLabel}>Giờ ra (Check-out)</Text>
              <Text style={styles.timeValue}>{formatTime(todayAttendance?.checkOutUtc)}</Text>
            </View>
          </View>

          {todayAttendance && (
            <View style={styles.metricsRow}>
              <View style={styles.metricItem}>
                <Text style={styles.metricVal}>{todayAttendance.eligibleMinutes || "0"} ph</Text>
                <Text style={styles.metricLbl}>Công hợp lệ</Text>
              </View>
              <View style={styles.metricItem}>
                <Text style={styles.metricVal}>{todayAttendance.lateMinutes || "0"} ph</Text>
                <Text style={styles.metricLbl}>Đi trễ</Text>
              </View>
              <View style={styles.metricItem}>
                <Text style={styles.metricVal}>{todayAttendance.earlyLeaveMinutes || "0"} ph</Text>
                <Text style={styles.metricLbl}>Về sớm</Text>
              </View>
              <View style={styles.metricItem}>
                <Text style={styles.metricVal}>
                  {todayAttendance.confirmedOvertimeMinutes || "0"} ph
                </Text>
                <Text style={styles.metricLbl}>OT xác nhận</Text>
              </View>
            </View>
          )}

          {/* Ô nhập ghi chú */}
          <TextInput
            style={styles.noteInput}
            placeholder="Ghi chú chấm công (tùy chọn)..."
            placeholderTextColor={colors.textMuted}
            value={note}
            onChangeText={setNote}
            editable={!isSubmitting}
          />

          {/* Nút bấm hành động */}
          <View style={styles.actionBtnRow}>
            <TouchableOpacity
              style={[
                styles.actionBtn,
                styles.checkInBtn,
                (hasCheckedIn || isSubmitting) && styles.disabledBtn,
              ]}
              onPress={handleCheckIn}
              disabled={hasCheckedIn || isSubmitting}
            >
              {isSubmitting && !hasCheckedIn ? (
                <ActivityIndicator color="#fff" size="small" />
              ) : (
                <Text style={styles.actionBtnText}>
                  {hasCheckedIn ? "Đã Check-in" : "Check-in vào"}
                </Text>
              )}
            </TouchableOpacity>

            <TouchableOpacity
              style={[
                styles.actionBtn,
                styles.checkOutBtn,
                (!hasCheckedIn || hasCheckedOut || isSubmitting) && styles.disabledBtn,
              ]}
              onPress={handleCheckOut}
              disabled={!hasCheckedIn || hasCheckedOut || isSubmitting}
            >
              {isSubmitting && hasCheckedIn && !hasCheckedOut ? (
                <ActivityIndicator color="#fff" size="small" />
              ) : (
                <Text style={styles.actionBtnText}>
                  {hasCheckedOut ? "Đã Check-out" : "Check-out ra"}
                </Text>
              )}
            </TouchableOpacity>
          </View>
        </View>

        {/* Lịch sử chấm công */}
        <View style={styles.historySection}>
          <Text style={styles.sectionTitle}>Lịch sử chấm công gần đây</Text>

          {isLoading && !refreshing ? (
            <ActivityIndicator
              size="large"
              color={colors.primary}
              style={{ marginVertical: spacing.lg }}
            />
          ) : historyItems.length === 0 ? (
            <View style={styles.emptyCard}>
              <Text style={styles.emptyText}>Chưa có bản ghi chấm công nào.</Text>
            </View>
          ) : (
            historyItems.map((item) => (
              <View key={item.id} style={styles.historyCard}>
                <View style={styles.historyCardHeader}>
                  <Text style={styles.historyDate}>{item.businessDate}</Text>
                  <View
                    style={[
                      styles.statusPill,
                      item.status === "DuCong" || item.status === "Approved"
                        ? styles.pillSuccess
                        : styles.pillDefault,
                    ]}
                  >
                    <Text style={styles.pillText}>{item.status}</Text>
                  </View>
                </View>

                <View style={styles.historyDetails}>
                  <Text style={styles.historyDetailText}>
                    Vào: {formatTime(item.checkInUtc)} | Ra: {formatTime(item.checkOutUtc)}
                  </Text>
                  <Text style={styles.historyDetailSub}>
                    Công: {item.eligibleMinutes} phút | Trễ: {item.lateMinutes} phút | Sớm:{" "}
                    {item.earlyLeaveMinutes} phút
                  </Text>
                </View>
              </View>
            ))
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
  cardToday: {
    backgroundColor: colors.surface,
    padding: spacing.lg,
    borderRadius: spacing.radiusSm,
    borderWidth: 1,
    borderColor: colors.border,
    marginBottom: spacing.lg,
  },
  sectionSubtitle: {
    ...typography.captionMedium,
    color: colors.textSecondary,
    letterSpacing: 0.5,
  },
  todayDateText: {
    ...typography.h2,
    color: colors.textPrimary,
    marginTop: 2,
    marginBottom: spacing.md,
  },
  timeRowContainer: {
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
    backgroundColor: colors.background,
    padding: spacing.md,
    borderRadius: spacing.radiusSm,
    marginBottom: spacing.md,
  },
  timeBox: {
    flex: 1,
    alignItems: "center",
  },
  timeLabel: {
    ...typography.caption,
    color: colors.textSecondary,
    marginBottom: 4,
  },
  timeValue: {
    ...typography.h1,
    color: colors.primary,
    fontSize: 22,
  },
  timeDivider: {
    width: 1,
    height: 40,
    backgroundColor: colors.border,
  },
  metricsRow: {
    flexDirection: "row",
    justifyContent: "space-around",
    backgroundColor: colors.background,
    padding: spacing.sm,
    borderRadius: spacing.radiusSm,
    marginBottom: spacing.md,
  },
  metricItem: {
    alignItems: "center",
  },
  metricVal: {
    ...typography.captionMedium,
    fontWeight: "700",
    color: colors.textPrimary,
  },
  metricLbl: {
    ...typography.caption,
    fontSize: 11,
    color: colors.textSecondary,
  },
  noteInput: {
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: spacing.radiusSm,
    padding: spacing.sm,
    color: colors.textPrimary,
    backgroundColor: colors.background,
    marginBottom: spacing.md,
    fontSize: 14,
  },
  actionBtnRow: {
    flexDirection: "row",
    gap: spacing.md,
  },
  actionBtn: {
    flex: 1,
    minHeight: 48,
    borderRadius: spacing.radiusSm,
    alignItems: "center",
    justifyContent: "center",
  },
  checkInBtn: {
    backgroundColor: colors.primary,
  },
  checkOutBtn: {
    backgroundColor: "#d97706",
  },
  disabledBtn: {
    opacity: 0.45,
  },
  actionBtnText: {
    ...typography.button,
    color: "#fff",
  },
  historySection: {
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
  historyCard: {
    backgroundColor: colors.surface,
    padding: spacing.md,
    borderRadius: spacing.radiusSm,
    borderWidth: 1,
    borderColor: colors.border,
    marginBottom: spacing.sm,
  },
  historyCardHeader: {
    flexDirection: "row",
    justifyContent: "space-between",
    alignItems: "center",
    marginBottom: spacing.xs,
  },
  historyDate: {
    ...typography.bodyMedium,
    color: colors.textPrimary,
    fontWeight: "700",
  },
  statusPill: {
    paddingHorizontal: spacing.sm,
    paddingVertical: 2,
    borderRadius: spacing.radiusSm,
  },
  pillSuccess: {
    backgroundColor: colors.successLight,
  },
  pillDefault: {
    backgroundColor: colors.background,
  },
  pillText: {
    ...typography.captionMedium,
    fontSize: 12,
    color: colors.textPrimary,
  },
  historyDetails: {
    marginTop: 2,
  },
  historyDetailText: {
    ...typography.captionMedium,
    color: colors.textPrimary,
  },
  historyDetailSub: {
    ...typography.caption,
    color: colors.textSecondary,
    marginTop: 2,
  },
});
