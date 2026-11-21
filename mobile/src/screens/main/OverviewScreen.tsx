import React, { useEffect, useState, useCallback } from "react";
import {
  View,
  Text,
  StyleSheet,
  ScrollView,
  TouchableOpacity,
  ActivityIndicator,
  RefreshControl,
} from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";
import { useAuth } from "../../context/AuthContext";
import { colors } from "../../theme/colors";
import { spacing } from "../../theme/spacing";
import { typography } from "../../theme/typography";
import { getOwnTodayAttendance, checkIn, checkOut } from "../../api/attendance";
import { getMyProfile } from "../../api/profile";
import { getFriendlyErrorMessage } from "../../api/errors";
import { generateIdempotencyKey } from "../../utils/uuid";
import type { AttendanceDayResponse, EmployeeProfileResponse } from "../../api/types";

interface OverviewScreenProps {
  onNavigateTab?: (tabKey: string) => void;
}

export const OverviewScreen: React.FC<OverviewScreenProps> = ({ onNavigateTab }) => {
  const { user } = useAuth();
  const [profile, setProfile] = useState<EmployeeProfileResponse | null>(null);
  const [todayAttendance, setTodayAttendance] = useState<AttendanceDayResponse | null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [isSubmitting, setIsSubmitting] = useState<boolean>(false);
  const [refreshing, setRefreshing] = useState<boolean>(false);
  const [bannerError, setBannerError] = useState<string | null>(null);
  const [bannerSuccess, setBannerSuccess] = useState<string | null>(null);

  const loadData = useCallback(async (bypassCache = false) => {
    try {
      setBannerError(null);
      const [profRes, attRes] = await Promise.all([
        getMyProfile(bypassCache).catch(() => null),
        getOwnTodayAttendance(bypassCache).catch(() => null),
      ]);
      if (profRes) setProfile(profRes);
      setTodayAttendance(attRes);
    } catch (err) {
      const friendly = getFriendlyErrorMessage(err, "Không thể tải dữ liệu trang chủ.");
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

  const handleQuickCheckIn = async () => {
    if (isSubmitting) return;
    setIsSubmitting(true);
    setBannerError(null);
    setBannerSuccess(null);

    const idempotencyKey = generateIdempotencyKey();

    try {
      const updated = await checkIn("Check-in nhanh từ trang chủ", idempotencyKey);
      setTodayAttendance(updated);
      setBannerSuccess("Check-in thành công!");
    } catch (err) {
      // Mất mạng: tuyệt đối không hiển thị thành công
      const friendly = getFriendlyErrorMessage(err, "Chấm công vào thất bại.");
      setBannerError(friendly.message);
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleQuickCheckOut = async () => {
    if (isSubmitting) return;
    setIsSubmitting(true);
    setBannerError(null);
    setBannerSuccess(null);

    const idempotencyKey = generateIdempotencyKey();

    try {
      const updated = await checkOut("Check-out nhanh từ trang chủ", idempotencyKey);
      setTodayAttendance(updated);
      setBannerSuccess("Check-out thành công!");
    } catch (err) {
      // Mất mạng: tuyệt đối không hiển thị thành công
      const friendly = getFriendlyErrorMessage(err, "Chấm công ra thất bại.");
      setBannerError(friendly.message);
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
      });
    } catch {
      return isoString;
    }
  };

  const hasCheckedIn = Boolean(todayAttendance?.checkInUtc);
  const hasCheckedOut = Boolean(todayAttendance?.checkOutUtc);

  return (
    <SafeAreaView style={styles.safeArea}>
      <ScrollView
        contentContainerStyle={styles.scrollContent}
        refreshControl={
          <RefreshControl refreshing={refreshing} onRefresh={onRefresh} colors={[colors.primary]} />
        }
      >
        {/* Banner thông báo lỗi / thành công */}
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

        {/* Welcome & Profile Section */}
        <View style={styles.welcomeBox}>
          <Text style={styles.greeting} allowFontScaling={true}>
            Xin chào, {profile?.fullName || user?.username || "Nhân viên"}
          </Text>
          <Text style={styles.userSub} allowFontScaling={true}>
            Mã NV: {profile?.employeeCode || "N/A"} • Trạng thái: {profile?.status || "Đang làm việc"}
          </Text>

          {user?.roles && user.roles.length > 0 && (
            <View style={styles.rolesRow}>
              {user.roles.map((role) => (
                <View key={role} style={styles.roleBadge}>
                  <Text style={styles.roleText} allowFontScaling={true}>
                    {role}
                  </Text>
                </View>
              ))}
            </View>
          )}
        </View>

        {/* Thẻ trạng thái ngày & Chấm công nhanh */}
        <View style={styles.dailyStatusCard}>
          <View style={styles.dailyHeader}>
            <Text style={styles.dailyTitle}>Trạng thái công hôm nay</Text>
            <View
              style={[
                styles.statusBadge,
                hasCheckedIn ? styles.badgeCheckedIn : styles.badgeNotCheckedIn,
              ]}
            >
              <Text style={styles.statusBadgeText}>
                {hasCheckedOut ? "ĐÃ CHECK-OUT" : hasCheckedIn ? "ĐANG LÀM VIỆC" : "CHƯA CHECK-IN"}
              </Text>
            </View>
          </View>

          <View style={styles.timeRow}>
            <View style={styles.timeBox}>
              <Text style={styles.timeLabel}>Giờ vào:</Text>
              <Text style={styles.timeValue}>{formatTime(todayAttendance?.checkInUtc)}</Text>
            </View>
            <View style={styles.timeBox}>
              <Text style={styles.timeLabel}>Giờ ra:</Text>
              <Text style={styles.timeValue}>{formatTime(todayAttendance?.checkOutUtc)}</Text>
            </View>
          </View>

          {/* Nút Check-in / Check-out trực tiếp */}
          <View style={styles.quickActionRow}>
            <TouchableOpacity
              style={[
                styles.quickBtn,
                styles.checkInBtn,
                (hasCheckedIn || isSubmitting) && styles.disabledBtn,
              ]}
              onPress={handleQuickCheckIn}
              disabled={hasCheckedIn || isSubmitting}
            >
              {isSubmitting && !hasCheckedIn ? (
                <ActivityIndicator size="small" color="#fff" />
              ) : (
                <Text style={styles.quickBtnText}>
                  {hasCheckedIn ? "Đã Check-in" : "Check-in ngay"}
                </Text>
              )}
            </TouchableOpacity>

            <TouchableOpacity
              style={[
                styles.quickBtn,
                styles.checkOutBtn,
                (!hasCheckedIn || hasCheckedOut || isSubmitting) && styles.disabledBtn,
              ]}
              onPress={handleQuickCheckOut}
              disabled={!hasCheckedIn || hasCheckedOut || isSubmitting}
            >
              {isSubmitting && hasCheckedIn && !hasCheckedOut ? (
                <ActivityIndicator size="small" color="#fff" />
              ) : (
                <Text style={styles.quickBtnText}>
                  {hasCheckedOut ? "Đã Check-out" : "Check-out ngay"}
                </Text>
              )}
            </TouchableOpacity>
          </View>
        </View>

        {/* Không gian chức năng (Feature Navigation) */}
        <Text style={styles.sectionTitle} allowFontScaling={true}>
          Không gian làm việc
        </Text>

        <View style={styles.moduleList}>
          {/* Chấm công */}
          <TouchableOpacity
            style={styles.moduleCard}
            onPress={() => onNavigateTab?.("attendance")}
          >
            <View style={styles.moduleHeader}>
              <Text style={styles.moduleTitle} allowFontScaling={true}>
                Chấm công & Lịch sử
              </Text>
              <Text style={styles.chevron}>→</Text>
            </View>
            <Text style={styles.moduleDescription} allowFontScaling={true}>
              Ghi nhận thời gian làm việc hàng ngày bằng Idempotency Key an toàn, theo dõi lịch sử
              công, số phút đi trễ, về sớm.
            </Text>
          </TouchableOpacity>

          {/* Nghỉ phép */}
          <TouchableOpacity style={styles.moduleCard} onPress={() => onNavigateTab?.("leaves")}>
            <View style={styles.moduleHeader}>
              <Text style={styles.moduleTitle} allowFontScaling={true}>
                Quản lý Nghỉ phép
              </Text>
              <Text style={styles.chevron}>→</Text>
            </View>
            <Text style={styles.moduleDescription} allowFontScaling={true}>
              Tra cứu quỹ phép năm, tạo đơn xin nghỉ mới và chủ động hủy đơn khi còn ở trạng thái
              Pending.
            </Text>
          </TouchableOpacity>

          {/* Phiếu lương */}
          <TouchableOpacity style={styles.moduleCard} onPress={() => onNavigateTab?.("payroll")}>
            <View style={styles.moduleHeader}>
              <Text style={styles.moduleTitle} allowFontScaling={true}>
                Phiếu lương đã công bố
              </Text>
              <Text style={styles.chevron}>→</Text>
            </View>
            <Text style={styles.moduleDescription} allowFontScaling={true}>
              Tra cứu các kỳ lương đã được phê duyệt và công bố, xem chi tiết lương tiêu chuẩn, làm
              thêm giờ, phụ cấp và thực nhận (Net).
            </Text>
          </TouchableOpacity>

          {/* Hồ sơ cá nhân */}
          <TouchableOpacity style={styles.moduleCard} onPress={() => onNavigateTab?.("profile")}>
            <View style={styles.moduleHeader}>
              <Text style={styles.moduleTitle} allowFontScaling={true}>
                Hồ sơ cá nhân
              </Text>
              <Text style={styles.chevron}>→</Text>
            </View>
            <Text style={styles.moduleDescription} allowFontScaling={true}>
              Xem thông tin nhân sự, phòng ban, chức vụ, thông tin định danh và phân quyền hệ thống.
            </Text>
          </TouchableOpacity>
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
  welcomeBox: {
    backgroundColor: colors.surface,
    padding: spacing.lg,
    borderRadius: spacing.radiusSm,
    borderWidth: 1,
    borderColor: colors.border,
    marginBottom: spacing.md,
  },
  greeting: {
    ...typography.h1,
    color: colors.textPrimary,
  },
  userSub: {
    ...typography.caption,
    color: colors.textSecondary,
    marginTop: spacing.xs,
  },
  rolesRow: {
    flexDirection: "row",
    flexWrap: "wrap",
    marginTop: spacing.md,
    gap: spacing.xs,
  },
  roleBadge: {
    backgroundColor: colors.primaryLight,
    paddingHorizontal: spacing.sm,
    paddingVertical: spacing.xs,
    borderRadius: spacing.radiusSm,
  },
  roleText: {
    ...typography.captionMedium,
    color: colors.primary,
  },
  dailyStatusCard: {
    backgroundColor: colors.surface,
    padding: spacing.lg,
    borderRadius: spacing.radiusSm,
    borderWidth: 1,
    borderColor: colors.border,
    marginBottom: spacing.lg,
  },
  dailyHeader: {
    flexDirection: "row",
    justifyContent: "space-between",
    alignItems: "center",
    marginBottom: spacing.sm,
  },
  dailyTitle: {
    ...typography.h3,
    color: colors.textPrimary,
  },
  statusBadge: {
    paddingHorizontal: spacing.sm,
    paddingVertical: 2,
    borderRadius: spacing.radiusSm,
  },
  badgeCheckedIn: {
    backgroundColor: colors.successLight,
  },
  badgeNotCheckedIn: {
    backgroundColor: colors.background,
  },
  statusBadgeText: {
    ...typography.captionMedium,
    fontSize: 11,
    color: colors.textPrimary,
  },
  timeRow: {
    flexDirection: "row",
    justifyContent: "space-around",
    backgroundColor: colors.background,
    padding: spacing.md,
    borderRadius: spacing.radiusSm,
    marginVertical: spacing.sm,
  },
  timeBox: {
    alignItems: "center",
  },
  timeLabel: {
    ...typography.caption,
    color: colors.textSecondary,
    marginBottom: 2,
  },
  timeValue: {
    ...typography.h2,
    color: colors.primary,
  },
  quickActionRow: {
    flexDirection: "row",
    gap: spacing.md,
    marginTop: spacing.sm,
  },
  quickBtn: {
    flex: 1,
    minHeight: 44,
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
  quickBtnText: {
    ...typography.button,
    color: "#fff",
    fontSize: 13,
  },
  sectionTitle: {
    ...typography.h2,
    color: colors.textPrimary,
    marginBottom: spacing.md,
  },
  moduleList: {
    gap: spacing.md,
  },
  moduleCard: {
    backgroundColor: colors.surface,
    padding: spacing.lg,
    borderRadius: spacing.radiusSm,
    borderWidth: 1,
    borderColor: colors.border,
  },
  moduleHeader: {
    flexDirection: "row",
    justifyContent: "space-between",
    alignItems: "center",
    marginBottom: spacing.xs,
  },
  moduleTitle: {
    ...typography.h3,
    color: colors.textPrimary,
  },
  chevron: {
    ...typography.h2,
    color: colors.primary,
  },
  moduleDescription: {
    ...typography.body,
    color: colors.textSecondary,
    lineHeight: 20,
  },
});
