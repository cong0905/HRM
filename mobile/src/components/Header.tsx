import React from "react";
import { View, Text, TouchableOpacity, StyleSheet } from "react-native";
import { colors } from "../theme/colors";
import { spacing } from "../theme/spacing";
import { typography } from "../theme/typography";

export interface HeaderProps {
  title: string;
  subtitle?: string;
  onBack?: () => void;
  rightAction?: React.ReactNode;
}

export const Header: React.FC<HeaderProps> = ({
  title,
  subtitle,
  onBack,
  rightAction,
}) => {
  return (
    <View style={styles.container}>
      <View style={styles.topRow}>
        {onBack ? (
          <TouchableOpacity
            style={styles.backButton}
            onPress={onBack}
            accessibilityRole="button"
            accessibilityLabel="Quay lại"
          >
            <Text style={styles.backText}>‹ Trở về</Text>
          </TouchableOpacity>
        ) : (
          <View style={styles.placeholder} />
        )}

        <View style={styles.titleWrapper}>
          <Text style={styles.title} numberOfLines={1} allowFontScaling={true}>
            {title}
          </Text>
        </View>

        <View style={styles.rightWrapper}>{rightAction || <View style={styles.placeholder} />}</View>
      </View>

      {subtitle && (
        <Text style={styles.subtitle} allowFontScaling={true}>
          {subtitle}
        </Text>
      )}
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    backgroundColor: colors.surface,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
    paddingHorizontal: spacing.lg,
    paddingVertical: spacing.sm,
  },
  topRow: {
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
    minHeight: spacing.minTouchTarget,
  },
  backButton: {
    minWidth: 60,
    minHeight: spacing.minTouchTarget,
    justifyContent: "center",
  },
  backText: {
    ...typography.bodyMedium,
    color: colors.primary,
  },
  titleWrapper: {
    flex: 1,
    alignItems: "center",
    justifyContent: "center",
  },
  title: {
    ...typography.h2,
    textAlign: "center",
  },
  rightWrapper: {
    minWidth: 60,
    minHeight: spacing.minTouchTarget,
    justifyContent: "center",
    alignItems: "flex-end",
  },
  placeholder: {
    width: 60,
  },
  subtitle: {
    ...typography.caption,
    textAlign: "center",
    marginTop: spacing.xs,
    marginBottom: spacing.xs,
  },
});
