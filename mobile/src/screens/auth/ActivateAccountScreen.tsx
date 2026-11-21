import React, { useState, useEffect } from "react";
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
import { activateAccount } from "../../api/auth";
import { getFriendlyErrorMessage } from "../../api/errors";
import { colors } from "../../theme/colors";
import { spacing } from "../../theme/spacing";
import { typography } from "../../theme/typography";

export interface ActivateAccountScreenProps {
  onBack: () => void;
  onSuccess: () => void;
}

export const ActivateAccountScreen: React.FC<ActivateAccountScreenProps> = ({
  onBack,
  onSuccess,
}) => {
  const [userId, setUserId] = useState("");
  const [code, setCode] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [loading, setLoading] = useState(false);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);
  const [isSuccess, setIsSuccess] = useState(false);

  // Xóa mã kích hoạt và mật khẩu khi rời màn hình
  useEffect(() => {
    return () => {
      setCode("");
      setNewPassword("");
      setConfirmPassword("");
    };
  }, []);

  const handleSubmit = async () => {
    setErrorMsg(null);

    const trimmedUserId = userId.trim();
    const trimmedCode = code.trim();

    if (!trimmedUserId || !trimmedCode || !newPassword || !confirmPassword) {
      setErrorMsg("Vui lòng điền đầy đủ tất cả các trường.");
      return;
    }

    if (newPassword !== confirmPassword) {
      setErrorMsg("Mật khẩu xác nhận không khớp.");
      return;
    }

    if (newPassword.length < 12) {
      setErrorMsg("Mật khẩu phải có độ dài tối thiểu 12 ký tự.");
      return;
    }

    setLoading(true);
    try {
      await activateAccount({
        userId: trimmedUserId,
        code: trimmedCode,
        newPassword,
      });
      setCode("");
      setNewPassword("");
      setConfirmPassword("");
      setIsSuccess(true);
    } catch (err) {
      const parsed = getFriendlyErrorMessage(err, "Kích hoạt tài khoản thất bại.");
      setErrorMsg(parsed.message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <SafeAreaView style={styles.safeArea}>
      <Header title="Kích hoạt tài khoản" onBack={onBack} />
      <KeyboardAvoidingView
        style={styles.keyboardView}
        behavior={Platform.OS === "ios" ? "padding" : undefined}
      >
        <ScrollView contentContainerStyle={styles.scrollContent} keyboardShouldPersistTaps="handled">
          {isSuccess ? (
            <View style={styles.successBox} accessibilityRole="alert">
              <Text style={styles.successTitle} allowFontScaling={true}>
                Tài khoản đã kích hoạt thành công
              </Text>
              <Text style={styles.successText} allowFontScaling={true}>
                Tài khoản của bạn đã sẵn sàng hoạt động. Bạn có thể đăng nhập vào ứng dụng ngay bây giờ.
              </Text>
              <Button
                title="Đăng nhập ngay"
                onPress={onSuccess}
                style={styles.loginButton}
              />
            </View>
          ) : (
            <>
              <Text style={styles.description} allowFontScaling={true}>
                Nhập mã người dùng (UserId), mã kích hoạt nhận được và tự thiết lập mật khẩu của bạn.
              </Text>

              {errorMsg && <ErrorCard message={errorMsg} />}

              <Input
                label="Mã người dùng (UserId GUID)"
                placeholder="Ví dụ: 3fa85f64-5717-4562-b3fc-2c963f66afa6"
                value={userId}
                onChangeText={(t) => {
                  setUserId(t);
                  if (errorMsg) setErrorMsg(null);
                }}
                editable={!loading}
              />

              <Input
                label="Mã kích hoạt"
                placeholder="Nhập mã kích hoạt nhận được"
                value={code}
                onChangeText={(t) => {
                  setCode(t);
                  if (errorMsg) setErrorMsg(null);
                }}
                editable={!loading}
              />

              <Input
                label="Mật khẩu thiết lập mới"
                placeholder="Tối thiểu 12 ký tự"
                value={newPassword}
                onChangeText={(t) => {
                  setNewPassword(t);
                  if (errorMsg) setErrorMsg(null);
                }}
                isPassword={true}
                editable={!loading}
              />

              <Input
                label="Xác nhận mật khẩu"
                placeholder="Nhập lại mật khẩu"
                value={confirmPassword}
                onChangeText={(t) => {
                  setConfirmPassword(t);
                  if (errorMsg) setErrorMsg(null);
                }}
                isPassword={true}
                editable={!loading}
              />

              <Button
                title="Kích hoạt tài khoản"
                onPress={handleSubmit}
                loading={loading}
                disabled={loading}
                style={styles.submitButton}
              />
            </>
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
  submitButton: {
    marginTop: spacing.md,
  },
  successBox: {
    backgroundColor: colors.successLight,
    borderWidth: 1,
    borderColor: colors.success,
    borderRadius: spacing.radiusSm,
    padding: spacing.lg,
    marginTop: spacing.md,
  },
  successTitle: {
    ...typography.h3,
    color: colors.success,
    marginBottom: spacing.xs,
  },
  successText: {
    ...typography.body,
    color: colors.textPrimary,
    marginBottom: spacing.lg,
  },
  loginButton: {
    marginTop: spacing.sm,
  },
});
