import React, { useState } from "react";
import {
  View,
  Text,
  TextInput,
  TextInputProps,
  TouchableOpacity,
  StyleSheet,
  ViewStyle,
} from "react-native";
import { colors } from "../theme/colors";
import { spacing } from "../theme/spacing";
import { typography } from "../theme/typography";

export interface InputProps extends TextInputProps {
  label: string;
  error?: string;
  hint?: string;
  containerStyle?: ViewStyle;
  isPassword?: boolean;
}

export const Input: React.FC<InputProps> = ({
  label,
  error,
  hint,
  containerStyle,
  isPassword = false,
  ...textInputProps
}) => {
  const [isFocused, setIsFocused] = useState(false);
  const [showPassword, setShowPassword] = useState(!isPassword);

  const getBorderColor = (): string => {
    if (error) return colors.borderError;
    if (isFocused) return colors.borderFocused;
    return colors.border;
  };

  return (
    <View style={[styles.container, containerStyle]}>
      <Text style={styles.label} allowFontScaling={true}>
        {label}
      </Text>

      <View style={[styles.inputWrapper, { borderColor: getBorderColor() }]}>
        <TextInput
          style={styles.input}
          placeholderTextColor={colors.textMuted}
          onFocus={() => setIsFocused(true)}
          onBlur={() => setIsFocused(false)}
          secureTextEntry={isPassword && !showPassword}
          autoCapitalize="none"
          allowFontScaling={true}
          accessibilityLabel={label}
          accessibilityHint={hint}
          {...textInputProps}
        />

        {isPassword && (
          <TouchableOpacity
            style={styles.eyeButton}
            onPress={() => setShowPassword((prev) => !prev)}
            accessibilityRole="button"
            accessibilityLabel={showPassword ? "Ẩn mật khẩu" : "Hiện mật khẩu"}
          >
            <Text style={styles.eyeText}>
              {showPassword ? "Ẩn" : "Hiện"}
            </Text>
          </TouchableOpacity>
        )}
      </View>

      {error ? (
        <Text style={styles.errorText} allowFontScaling={true} accessibilityRole="alert">
          {error}
        </Text>
      ) : hint ? (
        <Text style={styles.hintText} allowFontScaling={true}>
          {hint}
        </Text>
      ) : null}
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    marginBottom: spacing.md,
  },
  label: {
    ...typography.label,
    marginBottom: spacing.xs,
  },
  inputWrapper: {
    minHeight: spacing.minTouchTarget,
    height: spacing.inputHeight,
    borderWidth: 1,
    borderRadius: spacing.radiusSm,
    backgroundColor: colors.surface,
    flexDirection: "row",
    alignItems: "center",
    paddingHorizontal: spacing.md,
  },
  input: {
    ...typography.body,
    flex: 1,
    height: "100%",
    color: colors.textPrimary,
  },
  eyeButton: {
    minWidth: 44,
    minHeight: 44,
    justifyContent: "center",
    alignItems: "center",
  },
  eyeText: {
    ...typography.captionMedium,
    color: colors.primary,
  },
  errorText: {
    ...typography.helperError,
    marginTop: spacing.xs,
  },
  hintText: {
    ...typography.caption,
    marginTop: spacing.xs,
  },
});
