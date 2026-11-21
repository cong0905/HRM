import React from "react";
import { View, Text, ActivityIndicator, StyleSheet } from "react-native";
import { colors } from "../../theme/colors";
import { spacing } from "../../theme/spacing";
import { typography } from "../../theme/typography";

export interface LoadingScreenProps {
  message?: string;
}

export const LoadingScreen: React.FC<LoadingScreenProps> = ({
  message = "Đang kiểm tra phiên làm việc...",
}) => {
  return (
    <View style={styles.container}>
      <ActivityIndicator size="large" color={colors.primary} />
      <Text style={styles.message} allowFontScaling={true}>
        {message}
      </Text>
      <Text style={styles.subMessage} allowFontScaling={true}>
        HRM Next • Không gian làm việc nhân sự
      </Text>
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: colors.surface,
    justifyContent: "center",
    alignItems: "center",
    padding: spacing.xl,
  },
  message: {
    ...typography.bodyMedium,
    marginTop: spacing.lg,
    color: colors.textPrimary,
    textAlign: "center",
  },
  subMessage: {
    ...typography.caption,
    marginTop: spacing.sm,
    color: colors.textSecondary,
    textAlign: "center",
  },
});
