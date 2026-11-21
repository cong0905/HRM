import React from "react";
import { View, Text, StyleSheet } from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";
import { useAuth } from "../context/AuthContext";
import { AuthNavigator } from "./AuthNavigator";
import { MainNavigator } from "./MainNavigator";
import { LoadingScreen } from "../screens/common/LoadingScreen";
import { NetworkErrorBanner } from "../screens/common/NetworkErrorBanner";
import { Button } from "../components/Button";
import { colors } from "../theme/colors";
import { spacing } from "../theme/spacing";
import { typography } from "../theme/typography";

export const RootNavigator: React.FC = () => {
  const { status, retryBootstrap, logout, isLoading, isAppBackgrounded } = useAuth();

  // Màn hình che nội dung riêng tư khi ứng dụng chuyển sang chạy ngầm (App Switcher)
  if (isAppBackgrounded) {
    return (
      <View style={styles.privacyShield}>
        <Text style={styles.privacyShieldText} allowFontScaling={true}>
          HRM Next
        </Text>
      </View>
    );
  }

  if (status === "bootstrapping") {
    return <LoadingScreen message="Đang khôi phục phiên làm việc..." />;
  }

  if (status === "offline_session") {
    return (
      <SafeAreaView style={styles.offlineContainer}>
        <NetworkErrorBanner onRetry={retryBootstrap} isRetrying={isLoading} />
        <View style={styles.offlineBody}>
          <Text style={styles.offlineTitle} allowFontScaling={true}>
            Tạm dừng kết nối máy chủ
          </Text>
          <Text style={styles.offlineDesc} allowFontScaling={true}>
            Phiên đăng nhập vẫn được lưu trữ an toàn trên thiết bị. Vui lòng kết nối Internet để tiếp tục sử dụng.
          </Text>

          <Button
            title="Thử lại kết nối"
            onPress={retryBootstrap}
            loading={isLoading}
            style={styles.retryBtn}
          />

          <Button
            title="Đăng xuất khỏi thiết bị này"
            variant="ghost"
            onPress={logout}
            style={styles.logoutBtn}
          />
        </View>
      </SafeAreaView>
    );
  }

  if (status === "authenticated") {
    return <MainNavigator />;
  }

  return <AuthNavigator />;
};

const styles = StyleSheet.create({
  privacyShield: {
    flex: 1,
    backgroundColor: colors.primary,
    justifyContent: "center",
    alignItems: "center",
  },
  privacyShieldText: {
    ...typography.h1,
    color: colors.textInverse,
    letterSpacing: 1,
  },
  offlineContainer: {
    flex: 1,
    backgroundColor: colors.surface,
  },
  offlineBody: {
    flex: 1,
    padding: spacing.xl,
    justifyContent: "center",
    alignItems: "center",
  },
  offlineTitle: {
    ...typography.h2,
    color: colors.textPrimary,
    marginBottom: spacing.sm,
    textAlign: "center",
  },
  offlineDesc: {
    ...typography.body,
    color: colors.textSecondary,
    textAlign: "center",
    marginBottom: spacing.xl,
    lineHeight: 22,
  },
  retryBtn: {
    width: "100%",
    marginBottom: spacing.md,
  },
  logoutBtn: {
    width: "100%",
  },
});
