import React from "react";
import { View, Text, StyleSheet } from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";
import { Header } from "../../components/Header";
import { useAuth } from "../../context/AuthContext";
import { colors } from "../../theme/colors";
import { spacing } from "../../theme/spacing";
import { typography } from "../../theme/typography";

export interface PlaceholderScreenProps {
  title: string;
  requiredPermission: string;
  moduleName: string;
  taskTarget: string;
  onBack?: () => void;
}

export const PlaceholderScreen: React.FC<PlaceholderScreenProps> = ({
  title,
  requiredPermission,
  moduleName,
  taskTarget,
  onBack,
}) => {
  const { hasPermission } = useAuth();
  const isAllowed = hasPermission(requiredPermission);

  return (
    <SafeAreaView style={styles.safeArea}>
      <Header title={title} onBack={onBack} />
      <View style={styles.container}>
        {isAllowed ? (
          <View style={styles.contentBox}>
            <View style={styles.badgeSuccess}>
              <Text style={styles.badgeSuccessText} allowFontScaling={true}>
                ĐÃ XÁC THỰC QUYỀN TRUY CẬP
              </Text>
            </View>
            <Text style={styles.moduleTitle} allowFontScaling={true}>
              Không gian {moduleName}
            </Text>
            <Text style={styles.description} allowFontScaling={true}>
              Khung điều hướng và phiên an toàn (Safe Session) của tài khoản đã được xác minh thành công với máy chủ.
            </Text>
            <View style={styles.infoCard}>
              <Text style={styles.infoTitle} allowFontScaling={true}>
                Phạm vi triển khai ({taskTarget}):
              </Text>
              <Text style={styles.infoBody} allowFontScaling={true}>
                • Quyền yêu cầu: {requiredPermission} (Đạt)
              </Text>
              <Text style={styles.infoBody} allowFontScaling={true}>
                • Toàn bộ màn hình thao tác, lịch sử và biểu mẫu nghiệp vụ sẽ được tích hợp trong Task {taskTarget}.
              </Text>
            </View>
          </View>
        ) : (
          <View style={styles.deniedBox} accessibilityRole="alert">
            <View style={styles.badgeDenied}>
              <Text style={styles.badgeDeniedText} allowFontScaling={true}>
                TRUY CẬP BỊ TỪ CHỐI (403)
              </Text>
            </View>
            <Text style={styles.moduleTitle} allowFontScaling={true}>
              Chưa được cấp quyền
            </Text>
            <Text style={styles.description} allowFontScaling={true}>
              Tài khoản hiện tại của bạn không sở hữu quyền: {requiredPermission}.
            </Text>
            <Text style={styles.deniedHelp} allowFontScaling={true}>
              Vui lòng liên hệ Quản trị viên hệ thống (SystemAdmin) hoặc bộ phận Nhân sự để được phân quyền phù hợp.
            </Text>
          </View>
        )}
      </View>
    </SafeAreaView>
  );
};

const styles = StyleSheet.create({
  safeArea: {
    flex: 1,
    backgroundColor: colors.background,
  },
  container: {
    flex: 1,
    padding: spacing.lg,
    justifyContent: "center",
  },
  contentBox: {
    backgroundColor: colors.surface,
    padding: spacing.xl,
    borderRadius: spacing.radiusSm,
    borderWidth: 1,
    borderColor: colors.border,
    alignItems: "center",
  },
  badgeSuccess: {
    backgroundColor: colors.successLight,
    paddingHorizontal: spacing.md,
    paddingVertical: spacing.xs,
    borderRadius: spacing.radiusSm,
    marginBottom: spacing.md,
  },
  badgeSuccessText: {
    ...typography.captionMedium,
    color: colors.success,
    fontSize: 11,
    letterSpacing: 0.5,
  },
  badgeDenied: {
    backgroundColor: colors.errorLight,
    paddingHorizontal: spacing.md,
    paddingVertical: spacing.xs,
    borderRadius: spacing.radiusSm,
    marginBottom: spacing.md,
  },
  badgeDeniedText: {
    ...typography.captionMedium,
    color: colors.error,
    fontSize: 11,
    letterSpacing: 0.5,
  },
  moduleTitle: {
    ...typography.h2,
    color: colors.textPrimary,
    marginBottom: spacing.sm,
    textAlign: "center",
  },
  description: {
    ...typography.body,
    color: colors.textSecondary,
    textAlign: "center",
    marginBottom: spacing.lg,
    lineHeight: 22,
  },
  infoCard: {
    backgroundColor: colors.background,
    padding: spacing.md,
    borderRadius: spacing.radiusSm,
    borderWidth: 1,
    borderColor: colors.border,
    width: "100%",
  },
  infoTitle: {
    ...typography.captionMedium,
    color: colors.textPrimary,
    marginBottom: spacing.xs,
  },
  infoBody: {
    ...typography.caption,
    color: colors.textSecondary,
    lineHeight: 18,
  },
  deniedBox: {
    backgroundColor: colors.surface,
    padding: spacing.xl,
    borderRadius: spacing.radiusSm,
    borderWidth: 1,
    borderColor: colors.error,
    alignItems: "center",
  },
  deniedHelp: {
    ...typography.caption,
    color: colors.textSecondary,
    textAlign: "center",
    lineHeight: 18,
  },
});
