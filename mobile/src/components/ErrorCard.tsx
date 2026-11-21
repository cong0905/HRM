import React from "react";
import { View, Text, StyleSheet } from "react-native";
import { colors } from "../theme/colors";
import { spacing } from "../theme/spacing";
import { typography } from "../theme/typography";
import { Button } from "./Button";

export interface ErrorCardProps {
  message: string;
  traceId?: string;
  onRetry?: () => void;
}

export const ErrorCard: React.FC<ErrorCardProps> = ({
  message,
  traceId,
  onRetry,
}) => {
  return (
    <View style={styles.card} accessibilityRole="alert">
      <Text style={styles.title} allowFontScaling={true}>
        Thông báo lỗi
      </Text>
      <Text style={styles.message} allowFontScaling={true}>
        {message}
      </Text>
      {traceId && (
        <Text style={styles.traceId} allowFontScaling={true}>
          Mã tra cứu: {traceId}
        </Text>
      )}
      {onRetry && (
        <Button
          title="Thử lại"
          variant="secondary"
          onPress={onRetry}
          style={styles.retryButton}
        />
      )}
    </View>
  );
};

const styles = StyleSheet.create({
  card: {
    backgroundColor: colors.errorLight,
    borderWidth: 1,
    borderColor: colors.error,
    borderRadius: spacing.radiusSm,
    padding: spacing.md,
    marginVertical: spacing.sm,
  },
  title: {
    ...typography.captionMedium,
    color: colors.error,
    marginBottom: spacing.xs,
  },
  message: {
    ...typography.body,
    color: colors.textPrimary,
    marginBottom: spacing.xs,
  },
  traceId: {
    ...typography.caption,
    color: colors.textSecondary,
    fontSize: 11,
    marginBottom: spacing.sm,
  },
  retryButton: {
    marginTop: spacing.xs,
    alignSelf: "flex-start",
  },
});
