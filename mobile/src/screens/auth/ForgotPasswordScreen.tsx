import React, { useState } from "react";
import {
  View,
  Text,
  StyleSheet,
  ScrollView,
  KeyboardAvoidingView,
  Platform,
} from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";
import { Header } from "../../components/Header";
import { Input } from "../../components/Input";
import { Button } from "../../components/Button";
import { ErrorCard } from "../../components/ErrorCard";
import { forgotPassword } from "../../api/auth";
import { getFriendlyErrorMessage } from "../../api/errors";
import { colors } from "../../theme/colors";
import { spacing } from "../../theme/spacing";
import { typography } from "../../theme/typography";
import type { DeliveryResponse } from "../../api/types";

export interface ForgotPasswordScreenProps {
  onBack: () => void;
  onNavigateReset: () => void;
}

export const ForgotPasswordScreen: React.FC<ForgotPasswordScreenProps> = ({
  onBack,
  onNavigateReset,
}) => {
  const [email, setEmail] = useState("");
  const [loading, setLoading] = useState(false);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);
  const [result, setResult] = useState<DeliveryResponse | null>(null);

  const handleSubmit = async () => {
    setErrorMsg(null);
    setResult(null);

    const trimmed = email.trim();
    if (!trimmed || !trimmed.includes("@")) {
      setErrorMsg("Vui lòng nhập địa chỉ email hợp lệ.");
      return;
    }

    setLoading(true);
    try {
      const response = await forgotPassword({ email: trimmed });
      setResult(response);
    } catch (err) {
      const parsed = getFriendlyErrorMessage(err, "Không thể gửi yêu cầu đặt lại mật khẩu.");
      setErrorMsg(parsed.message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <SafeAreaView style={styles.safeArea}>
      <Header title="Quên mật khẩu" onBack={onBack} />
      <KeyboardAvoidingView
        style={styles.keyboardView}
        behavior={Platform.OS === "ios" ? "padding" : undefined}
      >
        <ScrollView contentContainerStyle={styles.scrollContent} keyboardShouldPersistTaps="handled">
          <Text style={styles.description} allowFontScaling={true}>
            Nhập địa chỉ email liên kết với tài khoản nhân viên của bạn. Hệ thống sẽ phát hành mã xác thực để đặt lại mật khẩu.
          </Text>

          {errorMsg && <ErrorCard message={errorMsg} />}

          {result ? (
            <View style={styles.successBox} accessibilityRole="alert">
              <Text style={styles.successTitle} allowFontScaling={true}>
                Yêu cầu đã được tiếp nhận
              </Text>
              <Text style={styles.successMessage} allowFontScaling={true}>
                {result.message}
              </Text>
              <View style={styles.deliveryBadge}>
                <Text style={styles.deliveryText} allowFontScaling={true}>
                  Chế độ chuyển phát máy chủ: {result.deliveryMode}
                </Text>
              </View>
              <Text style={styles.noteText} allowFontScaling={true}>
                Lưu ý: Môi trường Staging/Dev sử dụng kênh thử nghiệm (Mailpit/Outbox), không gửi email thực tế ra Internet. Vui lòng lấy mã xác thực và UserId từ hộp thư thử nghiệm để tiếp tục.
              </Text>

              <Button
                title="Chuyển đến màn hình Đặt lại mật khẩu"
                onPress={onNavigateReset}
                style={styles.nextButton}
              />
            </View>
          ) : (
            <View style={styles.form}>
              <Input
                label="Email công ty"
                placeholder="ten.nhanvien@congty.com"
                value={email}
                onChangeText={(t) => {
                  setEmail(t);
                  if (errorMsg) setErrorMsg(null);
                }}
                keyboardType="email-address"
                autoComplete="email"
                editable={!loading}
              />

              <Button
                title="Gửi mã xác thực"
                onPress={handleSubmit}
                loading={loading}
                disabled={loading}
                style={styles.submitButton}
              />

              <Button
                title="Tôi đã có sẵn mã xác thực và UserId"
                variant="ghost"
                onPress={onNavigateReset}
                style={styles.haveCodeButton}
              />
            </View>
          )}
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
    padding: spacing.lg,
  },
  description: {
    ...typography.body,
    color: colors.textSecondary,
    marginBottom: spacing.lg,
  },
  form: {
    marginTop: spacing.sm,
  },
  submitButton: {
    marginTop: spacing.md,
  },
  haveCodeButton: {
    marginTop: spacing.md,
  },
  successBox: {
    backgroundColor: colors.primaryLight,
    borderWidth: 1,
    borderColor: colors.primary,
    borderRadius: spacing.radiusSm,
    padding: spacing.lg,
    marginTop: spacing.sm,
  },
  successTitle: {
    ...typography.h3,
    color: colors.primary,
    marginBottom: spacing.xs,
  },
  successMessage: {
    ...typography.body,
    color: colors.textPrimary,
    marginBottom: spacing.md,
  },
  deliveryBadge: {
    backgroundColor: colors.surface,
    paddingHorizontal: spacing.sm,
    paddingVertical: spacing.xs,
    borderRadius: spacing.radiusSm,
    borderWidth: 1,
    borderColor: colors.border,
    alignSelf: "flex-start",
    marginBottom: spacing.md,
  },
  deliveryText: {
    ...typography.captionMedium,
    color: colors.primary,
  },
  noteText: {
    ...typography.caption,
    color: colors.textSecondary,
    lineHeight: 18,
    marginBottom: spacing.lg,
  },
  nextButton: {
    marginTop: spacing.sm,
  },
});
