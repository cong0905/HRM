import React, { useState, useEffect } from "react";
import {
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
import { useAuth } from "../../context/AuthContext";
import { getFriendlyErrorMessage } from "../../api/errors";
import { colors } from "../../theme/colors";
import { spacing } from "../../theme/spacing";
import { typography } from "../../theme/typography";

export interface ChangePasswordScreenProps {
  onBack: () => void;
}

export const ChangePasswordScreen: React.FC<ChangePasswordScreenProps> = ({ onBack }) => {
  const { changePassword, isLoading } = useAuth();
  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  // Xóa mật khẩu nhạy cảm khỏi bộ nhớ khi rời màn hình
  useEffect(() => {
    return () => {
      setCurrentPassword("");
      setNewPassword("");
      setConfirmPassword("");
    };
  }, []);

  const handleSubmit = async () => {
    setErrorMessage(null);

    if (!currentPassword || !newPassword || !confirmPassword) {
      setErrorMessage("Vui lòng nhập đầy đủ mật khẩu hiện tại và mật khẩu mới.");
      return;
    }

    if (newPassword !== confirmPassword) {
      setErrorMessage("Mật khẩu xác nhận không trùng khớp.");
      return;
    }

    if (newPassword.length < 12) {
      setErrorMessage("Mật khẩu mới phải có tối thiểu 12 ký tự.");
      return;
    }

    try {
      await changePassword({
        currentPassword,
        newPassword,
      });
      setCurrentPassword("");
      setNewPassword("");
      setConfirmPassword("");
    } catch (err) {
      const parsed = getFriendlyErrorMessage(err, "Đổi mật khẩu không thành công.");
      setErrorMessage(parsed.message);
    }
  };

  return (
    <SafeAreaView style={styles.safeArea}>
      <Header title="Đổi mật khẩu" onBack={onBack} />
      <KeyboardAvoidingView
        style={styles.keyboardView}
        behavior={Platform.OS === "ios" ? "padding" : undefined}
      >
        <ScrollView contentContainerStyle={styles.scrollContent} keyboardShouldPersistTaps="handled">
          <Text style={styles.warningNote} allowFontScaling={true}>
            Lưu ý: Đổi mật khẩu sẽ tự động thu hồi tất cả các phiên đăng nhập đang hoạt động. Bạn sẽ cần đăng nhập lại bằng mật khẩu mới.
          </Text>

          {errorMessage && <ErrorCard message={errorMessage} />}

          <Input
            label="Mật khẩu hiện tại"
            placeholder="Nhập mật khẩu đang dùng"
            value={currentPassword}
            onChangeText={(t) => {
              setCurrentPassword(t);
              if (errorMessage) setErrorMessage(null);
            }}
            isPassword={true}
            editable={!isLoading}
          />

          <Input
            label="Mật khẩu mới"
            placeholder="Tối thiểu 12 ký tự"
            value={newPassword}
            onChangeText={(t) => {
              setNewPassword(t);
              if (errorMessage) setErrorMessage(null);
            }}
            isPassword={true}
            editable={!isLoading}
          />

          <Input
            label="Xác nhận mật khẩu mới"
            placeholder="Nhập lại mật khẩu mới"
            value={confirmPassword}
            onChangeText={(t) => {
              setConfirmPassword(t);
              if (errorMessage) setErrorMessage(null);
            }}
            isPassword={true}
            editable={!isLoading}
          />

          <Button
            title="Xác nhận đổi mật khẩu"
            onPress={handleSubmit}
            loading={isLoading}
            disabled={isLoading}
            style={styles.submitButton}
          />
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
  warningNote: {
    ...typography.caption,
    color: colors.warning,
    backgroundColor: colors.warningLight,
    padding: spacing.md,
    borderRadius: spacing.radiusSm,
    marginBottom: spacing.lg,
    lineHeight: 18,
  },
  submitButton: {
    marginTop: spacing.md,
  },
});
