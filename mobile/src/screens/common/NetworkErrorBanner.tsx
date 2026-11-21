import React from "react";
import { View, Text, StyleSheet } from "react-native";
import { colors } from "../../theme/colors";
import { spacing } from "../../theme/spacing";
import { typography } from "../../theme/typography";
import { Button } from "../../components/Button";

export interface NetworkErrorBannerProps {
  onRetry: () => void;
  isRetrying?: boolean;
}

export const NetworkErrorBanner: React.FC<NetworkErrorBannerProps> = ({
  onRetry,
  isRetrying = false,
}) => {
  return (
    <View style={styles.banner} accessibilityRole="alert">
      <View style={styles.textColumn}>
        <Text style={styles.title} allowFontScaling={true}>
          Không có kết nối mạng
        </Text>
        <Text style={styles.description} allowFontScaling={true}>
          Phiên làm việc trên thiết bị vẫn được bảo lưu an toàn. Vui lòng kết nối Internet để tiếp tục.
        </Text>
      </View>
      <Button
        title="Thử lại"
        variant="secondary"
        onPress={onRetry}
        loading={isRetrying}
        style={styles.retryButton}
      />
    </View>
  );
};

const styles = StyleSheet.create({
  banner: {
    backgroundColor: colors.warningLight,
    borderBottomWidth: 1,
    borderBottomColor: colors.warning,
    paddingHorizontal: spacing.lg,
    paddingVertical: spacing.md,
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
  },
  textColumn: {
    flex: 1,
    marginRight: spacing.md,
  },
  title: {
    ...typography.captionMedium,
    color: colors.warning,
    fontWeight: "700",
  },
  description: {
    ...typography.caption,
    color: colors.textPrimary,
    marginTop: 2,
  },
  retryButton: {
    minWidth: 80,
    height: 38,
  },
});
