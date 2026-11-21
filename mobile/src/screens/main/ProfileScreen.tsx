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
import { useAuth } from "../../context/AuthContext";
import { Header } from "../../components/Header";
import { colors } from "../../theme/colors";
import { spacing } from "../../theme/spacing";
import { typography } from "../../theme/typography";
import { getMyProfile } from "../../api/profile";
import { getFriendlyErrorMessage } from "../../api/errors";
import type { EmployeeProfileResponse } from "../../api/types";

interface ProfileScreenProps {
  onBack?: () => void;
}

export const ProfileScreen: React.FC<ProfileScreenProps> = ({ onBack }) => {
  const { user } = useAuth();
  const [profile, setProfile] = useState<EmployeeProfileResponse | null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);
  const [refreshing, setRefreshing] = useState<boolean>(false);

  const loadProfile = useCallback(async (bypassCache = false) => {
    try {
      setErrorMsg(null);
      const data = await getMyProfile(bypassCache);
      setProfile(data);
    } catch (err) {
      const friendly = getFriendlyErrorMessage(err, "Không thể tải thông tin hồ sơ.");
      setErrorMsg(friendly.message);
    } finally {
      setIsLoading(false);
      setRefreshing(false);
    }
  }, []);

  useEffect(() => {
    void loadProfile();
  }, [loadProfile]);

  const onRefresh = useCallback(() => {
    setRefreshing(true);
    void loadProfile(true);
  }, [loadProfile]);

  return (
    <SafeAreaView style={styles.safeArea}>
      <Header title="Hồ sơ nhân viên" onBack={onBack} />
      <ScrollView
        contentContainerStyle={styles.scrollContent}
        refreshControl={
          <RefreshControl refreshing={refreshing} onRefresh={onRefresh} colors={[colors.primary]} />
        }
      >
        {isLoading && !refreshing ? (
          <View style={styles.centerBox}>
            <ActivityIndicator size="large" color={colors.primary} />
            <Text style={styles.loadingText}>Đang tải hồ sơ nhân sự...</Text>
          </View>
        ) : errorMsg ? (
          <View style={styles.errorBox}>
            <Text style={styles.errorTitle}>Lỗi tải hồ sơ</Text>
            <Text style={styles.errorBody}>{errorMsg}</Text>
            <TouchableOpacity style={styles.retryBtn} onPress={() => loadProfile(true)}>
              <Text style={styles.retryBtnText}>Thử lại</Text>
            </TouchableOpacity>
          </View>
        ) : (
          <>
            {/* Identity & Work Info Card */}
            <View style={styles.card}>
              <Text style={styles.cardTitle} allowFontScaling={true}>
                Thông tin nhân sự
              </Text>

              <View style={styles.row}>
                <Text style={styles.label}>Họ và tên:</Text>
                <Text style={styles.valueHighlight}>
                  {profile?.fullName || user?.username || "Chưa cập nhật"}
                </Text>
              </View>

              <View style={styles.row}>
                <Text style={styles.label}>Mã nhân viên:</Text>
                <Text style={styles.valueMono}>{profile?.employeeCode || "N/A"}</Text>
              </View>

              <View style={styles.row}>
                <Text style={styles.label}>Trạng thái công tác:</Text>
                <View style={styles.statusBadge}>
                  <Text style={styles.statusText}>{profile?.status || "Đang làm việc"}</Text>
                </View>
              </View>

              <View style={styles.row}>
                <Text style={styles.label}>Ngày vào làm:</Text>
                <Text style={styles.value}>{profile?.hireDate || "Chưa xác định"}</Text>
              </View>
            </View>

            {/* Personal Details Card */}
            <View style={styles.card}>
              <Text style={styles.cardTitle} allowFontScaling={true}>
                Thông tin cá nhân & Liên hệ
              </Text>

              <View style={styles.row}>
                <Text style={styles.label}>Căn cước công dân (CCCD):</Text>
                <Text style={styles.value}>{profile?.citizenId || "Chưa cập nhật"}</Text>
              </View>

              <View style={styles.row}>
                <Text style={styles.label}>Email liên hệ:</Text>
                <Text style={styles.value}>{profile?.email || "Chưa cập nhật"}</Text>
              </View>

              <View style={styles.row}>
                <Text style={styles.label}>Số điện thoại:</Text>
                <Text style={styles.value}>{profile?.phone || "Chưa cập nhật"}</Text>
              </View>

              <View style={styles.row}>
                <Text style={styles.label}>Địa chỉ thường trú:</Text>
                <Text style={styles.value}>{profile?.address || "Chưa cập nhật"}</Text>
              </View>

              <View style={styles.row}>
                <Text style={styles.label}>Ngày sinh:</Text>
                <Text style={styles.value}>{profile?.dateOfBirth || "Chưa cập nhật"}</Text>
              </View>

              <View style={styles.row}>
                <Text style={styles.label}>Giới tính:</Text>
                <Text style={styles.value}>{profile?.gender || "Chưa xác định"}</Text>
              </View>
            </View>

            {/* System Account Card */}
            <View style={styles.card}>
              <Text style={styles.cardTitle} allowFontScaling={true}>
                Tài khoản & Phân quyền hệ thống
              </Text>

              <View style={styles.row}>
                <Text style={styles.label}>Tên đăng nhập:</Text>
                <Text style={styles.value}>{user?.username || "N/A"}</Text>
              </View>

              <View style={styles.row}>
                <Text style={styles.label}>Mã người dùng (UserId):</Text>
                <Text style={styles.valueMono}>{user?.id || "N/A"}</Text>
              </View>

              <Text style={styles.subHeader}>Vai trò được gán:</Text>
              <View style={styles.badgeWrap}>
                {user?.roles?.map((role) => (
                  <View key={role} style={styles.roleBadge}>
                    <Text style={styles.roleText}>{role}</Text>
                  </View>
                )) || <Text style={styles.emptyText}>Chưa có vai trò</Text>}
              </View>
            </View>

            {/* MVP Policy Notice */}
            <View style={styles.noticeBox}>
              <Text style={styles.noticeTitle}>Quy tắc hồ sơ cá nhân (MVP 8 tuần):</Text>
              <Text style={styles.noticeBody}>
                Hồ sơ cá nhân trên ứng dụng di động hoạt động ở chế độ chỉ đọc. Mọi yêu cầu điều chỉnh
                thông tin liên hệ, chức danh hoặc thông tin nhân sự do phòng Nhân sự (HR) tiếp nhận và
                cập nhật qua hệ thống quản lý tập trung.
              </Text>
            </View>
          </>
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
  centerBox: {
    paddingVertical: spacing.xxl,
    alignItems: "center",
  },
  loadingText: {
    ...typography.caption,
    color: colors.textSecondary,
    marginTop: spacing.md,
  },
  errorBox: {
    backgroundColor: colors.errorLight,
    padding: spacing.lg,
    borderRadius: spacing.radiusSm,
    borderWidth: 1,
    borderColor: colors.error,
    alignItems: "center",
  },
  errorTitle: {
    ...typography.h3,
    color: colors.error,
    marginBottom: spacing.xs,
  },
  errorBody: {
    ...typography.body,
    color: colors.textPrimary,
    textAlign: "center",
    marginBottom: spacing.md,
  },
  retryBtn: {
    backgroundColor: colors.error,
    paddingHorizontal: spacing.lg,
    paddingVertical: spacing.sm,
    borderRadius: spacing.radiusSm,
  },
  retryBtnText: {
    ...typography.button,
    color: "#fff",
  },
  card: {
    backgroundColor: colors.surface,
    padding: spacing.lg,
    borderRadius: spacing.radiusSm,
    borderWidth: 1,
    borderColor: colors.border,
    marginBottom: spacing.md,
  },
  cardTitle: {
    ...typography.h3,
    color: colors.textPrimary,
    marginBottom: spacing.md,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
    paddingBottom: spacing.xs,
  },
  row: {
    marginBottom: spacing.sm,
  },
  label: {
    ...typography.captionMedium,
    color: colors.textSecondary,
    marginBottom: 2,
  },
  value: {
    ...typography.bodyMedium,
    color: colors.textPrimary,
  },
  valueHighlight: {
    ...typography.h3,
    color: colors.primary,
  },
  valueMono: {
    ...typography.caption,
    color: colors.textPrimary,
    fontFamily: "monospace",
  },
  statusBadge: {
    backgroundColor: colors.successLight,
    paddingHorizontal: spacing.sm,
    paddingVertical: 2,
    borderRadius: spacing.radiusSm,
    alignSelf: "flex-start",
  },
  statusText: {
    ...typography.captionMedium,
    color: colors.success,
  },
  subHeader: {
    ...typography.captionMedium,
    color: colors.textSecondary,
    marginTop: spacing.sm,
    marginBottom: spacing.xs,
  },
  badgeWrap: {
    flexDirection: "row",
    flexWrap: "wrap",
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
  emptyText: {
    ...typography.caption,
    color: colors.textMuted,
  },
  noticeBox: {
    backgroundColor: colors.surface,
    padding: spacing.md,
    borderRadius: spacing.radiusSm,
    borderWidth: 1,
    borderColor: colors.border,
    marginTop: spacing.sm,
  },
  noticeTitle: {
    ...typography.captionMedium,
    color: colors.textPrimary,
    marginBottom: spacing.xs,
  },
  noticeBody: {
    ...typography.caption,
    color: colors.textSecondary,
    lineHeight: 18,
  },
});
