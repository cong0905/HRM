import React from "react";
import {
  TouchableOpacity,
  Text,
  ActivityIndicator,
  StyleSheet,
  ViewStyle,
  TextStyle,
  AccessibilityRole,
} from "react-native";
import { colors } from "../theme/colors";
import { spacing } from "../theme/spacing";
import { typography } from "../theme/typography";

export interface ButtonProps {
  title: string;
  onPress: () => void;
  variant?: "primary" | "secondary" | "danger" | "ghost";
  disabled?: boolean;
  loading?: boolean;
  style?: ViewStyle;
  textStyle?: TextStyle;
  accessibilityLabel?: string;
  accessibilityHint?: string;
}

export const Button: React.FC<ButtonProps> = ({
  title,
  onPress,
  variant = "primary",
  disabled = false,
  loading = false,
  style,
  textStyle,
  accessibilityLabel,
  accessibilityHint,
}) => {
  const isInteractive = !disabled && !loading;

  const getContainerStyle = (): ViewStyle[] => {
    const list: ViewStyle[] = [styles.base];

    switch (variant) {
      case "primary":
        list.push(styles.primary);
        break;
      case "secondary":
        list.push(styles.secondary);
        break;
      case "danger":
        list.push(styles.danger);
        break;
      case "ghost":
        list.push(styles.ghost);
        break;
    }

    if (disabled) {
      list.push(styles.disabled);
    }

    if (style) {
      list.push(style);
    }

    return list;
  };

  const getTextStyle = (): TextStyle[] => {
    const list: TextStyle[] = [styles.baseText];

    switch (variant) {
      case "primary":
      case "danger":
        list.push(styles.textWhite);
        break;
      case "secondary":
        list.push(styles.textSecondary);
        break;
      case "ghost":
        list.push(styles.textGhost);
        break;
    }

    if (disabled) {
      list.push(styles.textDisabled);
    }

    if (textStyle) {
      list.push(textStyle);
    }

    return list;
  };

  return (
    <TouchableOpacity
      onPress={onPress}
      disabled={!isInteractive}
      activeOpacity={0.7}
      style={getContainerStyle()}
      accessibilityRole={"button" as AccessibilityRole}
      accessibilityLabel={accessibilityLabel || title}
      accessibilityHint={accessibilityHint}
      accessibilityState={{ disabled: !isInteractive, busy: loading }}
    >
      {loading ? (
        <ActivityIndicator
          size="small"
          color={variant === "primary" || variant === "danger" ? colors.surface : colors.primary}
        />
      ) : (
        <Text style={getTextStyle()} allowFontScaling={true}>
          {title}
        </Text>
      )}
    </TouchableOpacity>
  );
};

const styles = StyleSheet.create({
  base: {
    minHeight: spacing.minTouchTarget,
    height: spacing.buttonHeight,
    borderRadius: spacing.radiusSm,
    paddingHorizontal: spacing.lg,
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "center",
  },
  primary: {
    backgroundColor: colors.primary,
  },
  secondary: {
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.border,
  },
  danger: {
    backgroundColor: colors.error,
  },
  ghost: {
    backgroundColor: "transparent",
  },
  disabled: {
    backgroundColor: colors.disabledBackground,
    borderColor: colors.disabledBackground,
  },
  baseText: {
    ...typography.button,
  },
  textWhite: {
    color: colors.textInverse,
  },
  textSecondary: {
    color: colors.textPrimary,
  },
  textGhost: {
    color: colors.primary,
  },
  textDisabled: {
    color: colors.disabledText,
  },
});
