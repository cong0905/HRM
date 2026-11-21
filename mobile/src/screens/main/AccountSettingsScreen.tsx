import React from "react";
import { View, Text, StyleSheet, ScrollView, Alert } from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";
import { Header } from "../../components/Header";
import { Button } from "../../components/Button";
import { useAuth } from "../../context/AuthContext";
import { colors } from "../../theme/colors";
import { spacing } from "../../theme/spacing";
import { typography } from "../../theme/typography";

export interface AccountSettingsScreenProps {
  onNavigateChangePassword: () => void;
  onNavigateProfile?: () => void;
}

export const AccountSettingsScreen: React.FC<AccountSettingsScreenProps> = ({
  onNavigateChangePassword,
  onNavigateProfile,
}) => {
  const { logout, isLoading, user } = useAuth();

  const handleLogoutPress = () => {
    Alert.alert(
      "Xác nhận đăng xuất",
      "Bạn có chắc chắn muốn đăng xuất khỏi ứng dụng? Mọi phiên làm việc trên thiết bị sẽ được xóa sạch.",
      [
        { text: "Hủy", style: "cancel" },
        {
          text: "Đăng xuất",
          style: "destructive",
          onPress: async () => {
            try {
              await logout();
            } catch {
              // Xóa phiên cục bộ đã hoàn tất
            }
          },
        },
      ]
    );
  };

  return (
    <SafeAreaView style={styles.safeArea}>
      <Header title="Tài khoản" />
      <ScrollView contentContainerStyle={styles.scrollContent}>
        {/* Thông tin tài khoản */}
        <View style={styles.card}>
          <Text style={styles.cardTitle} allowFontScaling={true}>
            Thông tin định danh
          </Text>
          <Text style={styles.usernameText} allowFontScaling={true}>
            {user?.username || "Chưa xác định"}
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

          {onNavigateProfile && (
            <Button
              title="Xem hồ sơ nhân viên chi tiết"
              variant="secondary"
              onPress={onNavigateProfile}
              style={{ marginTop: spacing.sm }}
            />
          )}

          <Button
            title="Đổi mật khẩu tài khoản"
            variant="secondary"
            onPress={onNavigateChangePassword}
            style={styles.changePassButton}
          />
        </View>

        {/* Hành động Đăng xuất */}
        <View style={styles.logoutContainer}>
          <Button
            title="Đăng xuất khỏi thiết bị"
            variant="danger"
            onPress={handleLogoutPress}
            loading={isLoading}
            disabled={isLoading}
          />
          <Text style={styles.logoutHelper} allowFontScaling={true}>
            Thao tác đăng xuất sẽ che toàn bộ dữ liệu, hủy phiên trên máy chủ và xóa sạch thông tin đăng nhập trên thiết bị.
          </Text>
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
    marginBottom: spacing.sm,
  },
  usernameText: {
    ...typography.h2,
    color: colors.primary,
    marginBottom: 2,
  },
  captionText: {
    ...typography.caption,
    color: colors.textSecondary,
    marginBottom: spacing.md,
  },
  rolesRow: {
    flexDirection: "row",
    flexWrap: "wrap",
    gap: spacing.xs,
    marginBottom: spacing.md,
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
  changePassButton: {
    marginTop: spacing.xs,
  },
  permList: {
    gap: spacing.xs,
  },
  permItem: {
    paddingVertical: 2,
  },
  permText: {
    ...typography.caption,
    color: colors.textPrimary,
    fontSize: 13,
  },
  logoutContainer: {
    marginTop: spacing.md,
  },
  logoutHelper: {
    ...typography.caption,
    color: colors.textSecondary,
    textAlign: "center",
    marginTop: spacing.sm,
    lineHeight: 18,
  },
});
