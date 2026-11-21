import React, { useState, useEffect } from "react";
import {
  View,
  Text,
  StyleSheet,
  ScrollView,
  KeyboardAvoidingView,
  Platform,
  TouchableOpacity,
} from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";
import { useAuth } from "../../context/AuthContext";
import { Input } from "../../components/Input";
import { Button } from "../../components/Button";
import { ErrorCard } from "../../components/ErrorCard";
import { colors } from "../../theme/colors";
import { spacing } from "../../theme/spacing";
import { typography } from "../../theme/typography";
import { getFriendlyErrorMessage } from "../../api/errors";

export interface LoginScreenProps {
  onNavigateForgotPassword: () => void;
  onNavigateActivateAccount: () => void;
  onNavigateResetPassword: () => void;
}

export const LoginScreen: React.FC<LoginScreenProps> = ({
  onNavigateForgotPassword,
  onNavigateActivateAccount,
  onNavigateResetPassword,
}) => {
  const { login, isLoading, serverRevocationNotice, clearServerRevocationNotice } = useAuth();
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [traceId, setTraceId] = useState<string | undefined>(undefined);

  // Xóa mật khẩu khi rời màn hình để đảm bảo an toàn bộ nhớ
  useEffect(() => {
    return () => {
      setPassword("");
    };
  }, []);

  const handleLogin = async () => {
    setErrorMessage(null);
    setTraceId(undefined);
    clearServerRevocationNotice();

    const trimmedUser = username.trim();
    if (!trimmedUser || !password) {
      setErrorMessage("Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu.");
      return;
    }

    try {
      await login({
        username: trimmedUser,
        password,
      });
      setPassword(""); // Xóa ngay mật khẩu khỏi bộ nhớ sau khi đăng nhập xong
    } catch (error) {
      const parsed = getFriendlyErrorMessage(error, "Đăng nhập thất bại.");
      setErrorMessage(parsed.message);
      setTraceId(parsed.traceId);
    }
  };

  return (
    <SafeAreaView style={styles.safeArea}>
      <KeyboardAvoidingView
        style={styles.keyboardView}
        behavior={Platform.OS === "ios" ? "padding" : undefined}
      >
        <ScrollView
          contentContainerStyle={styles.scrollContent}
          keyboardShouldPersistTaps="handled"
        >
          {/* Header */}
          <View style={styles.headerContainer}>
            <Text style={styles.title} allowFontScaling={true}>
              Đăng nhập hệ thống
            </Text>
            <Text style={styles.subtitle} allowFontScaling={true}>
              Cổng thông tin nhân sự và dịch vụ nội bộ
            </Text>
          </View>

          {/* Form */}
          <View style={styles.formContainer}>
            {/* Thông báo thu hồi hoặc chưa xác nhận từ server sau khi logout */}
            {serverRevocationNotice && (
              <View style={styles.noticeCard} accessibilityRole="alert">
                <Text style={styles.noticeText} allowFontScaling={true}>
                  {serverRevocationNotice}
                </Text>
              </View>
            )}

            {errorMessage && (
              <ErrorCard
                message={errorMessage}
                traceId={traceId}
                onRetry={handleLogin}
              />
            )}

            <Input
              label="Tên đăng nhập"
              placeholder="Nhập tên tài khoản"
              value={username}
              onChangeText={(text) => {
                setUsername(text);
                if (errorMessage) setErrorMessage(null);
              }}
              autoComplete="username"
              editable={!isLoading}
            />

            <Input
              label="Mật khẩu"
              placeholder="Nhập mật khẩu"
              value={password}
              onChangeText={(text) => {
                setPassword(text);
                if (errorMessage) setErrorMessage(null);
              }}
              isPassword={true}
              autoComplete="password"
              editable={!isLoading}
            />

            <Button
              title="Đăng nhập"
              onPress={handleLogin}
              loading={isLoading}
              disabled={isLoading}
              style={styles.submitButton}
            />

            <View style={styles.linksContainer}>
              <TouchableOpacity
                style={styles.linkButton}
                onPress={onNavigateForgotPassword}
                accessibilityRole="button"
                accessibilityLabel="Quên mật khẩu"
              >
                <Text style={styles.linkText} allowFontScaling={true}>
                  Quên mật khẩu?
                </Text>
              </TouchableOpacity>

              <TouchableOpacity
                style={styles.linkButton}
                onPress={onNavigateResetPassword}
                accessibilityRole="button"
                accessibilityLabel="Đặt lại mật khẩu với mã"
              >
                <Text style={styles.linkTextSecondary} allowFontScaling={true}>
                  Đã có mã xác thực đặt lại mật khẩu?
                </Text>
              </TouchableOpacity>

              <TouchableOpacity
                style={styles.linkButton}
                onPress={onNavigateActivateAccount}
                accessibilityRole="button"
                accessibilityLabel="Kích hoạt tài khoản mới"
              >
                <Text style={styles.linkText} allowFontScaling={true}>
                  Kích hoạt tài khoản nhân viên mới
                </Text>
              </TouchableOpacity>
            </View>
          </View>
        </ScrollView>
      </KeyboardAvoidingView>
    </SafeAreaView>
  );
};

const styles = StyleSheet.create({
  safeArea: {
    flex: 1,
    backgroundColor: colors.surface,
  },
  keyboardView: {
    flex: 1,
  },
  scrollContent: {
    flexGrow: 1,
    paddingHorizontal: spacing.lg,
    paddingVertical: spacing.xl,
    justifyContent: "center",
  },
  headerContainer: {
    marginBottom: spacing.xl,
  },
  title: {
    ...typography.h1,
    color: colors.textPrimary,
  },
  subtitle: {
    ...typography.caption,
    color: colors.textSecondary,
    marginTop: spacing.xs,
  },
  formContainer: {
    flex: 1,
  },
  noticeCard: {
    backgroundColor: colors.warningLight,
    borderWidth: 1,
    borderColor: colors.warning,
    borderRadius: spacing.radiusSm,
    padding: spacing.md,
    marginBottom: spacing.md,
  },
  noticeText: {
    ...typography.caption,
    color: colors.textPrimary,
    lineHeight: 18,
  },
  submitButton: {
    marginTop: spacing.md,
  },
  linksContainer: {
    marginTop: spacing.lg,
    alignItems: "center",
  },
  linkButton: {
    minHeight: spacing.minTouchTarget,
    justifyContent: "center",
    paddingVertical: spacing.xs,
  },
  linkText: {
    ...typography.bodyMedium,
    color: colors.primary,
  },
  linkTextSecondary: {
    ...typography.captionMedium,
    color: colors.textSecondary,
  },
});
