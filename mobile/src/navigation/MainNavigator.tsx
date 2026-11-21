import React, { useEffect, useState } from "react";
import { BackHandler, View, Text, TouchableOpacity, StyleSheet } from "react-native";
import { useSafeAreaInsets } from "react-native-safe-area-context";
import { OverviewScreen } from "../screens/main/OverviewScreen";
import { AttendanceScreen } from "../screens/main/AttendanceScreen";
import { LeaveScreen } from "../screens/main/LeaveScreen";
import { PayrollScreen } from "../screens/main/PayrollScreen";
import { ProfileScreen } from "../screens/main/ProfileScreen";
import { AccountSettingsScreen } from "../screens/main/AccountSettingsScreen";
import { ChangePasswordScreen } from "../screens/main/ChangePasswordScreen";
import { colors } from "../theme/colors";
import { spacing } from "../../src/theme/spacing";
import { typography } from "../../src/theme/typography";

export type MainTabKey =
  | "overview"
  | "attendance"
  | "leaves"
  | "payroll"
  | "profile"
  | "account"
  | "changePassword";

export const MainNavigator: React.FC = () => {
  const [activeTab, setActiveTab] = useState<MainTabKey>("overview");
  const insets = useSafeAreaInsets();

  useEffect(() => {
    const handler = BackHandler.addEventListener("hardwareBackPress", () => {
      if (activeTab === "overview") return false;
      if (activeTab === "changePassword" || activeTab === "profile") {
        setActiveTab("account");
        return true;
      }
      setActiveTab("overview");
      return true;
    });
    return () => handler.remove();
  }, [activeTab]);

  const renderContent = () => {
    switch (activeTab) {
      case "attendance":
        return <AttendanceScreen onBack={() => setActiveTab("overview")} />;
      case "leaves":
        return <LeaveScreen onBack={() => setActiveTab("overview")} />;
      case "payroll":
        return <PayrollScreen onBack={() => setActiveTab("overview")} />;
      case "profile":
        return <ProfileScreen onBack={() => setActiveTab("account")} />;
      case "changePassword":
        return <ChangePasswordScreen onBack={() => setActiveTab("account")} />;
      case "account":
        return (
          <AccountSettingsScreen
            onNavigateChangePassword={() => setActiveTab("changePassword")}
            onNavigateProfile={() => setActiveTab("profile")}
          />
        );
      case "overview":
      default:
        return <OverviewScreen onNavigateTab={(tab) => setActiveTab(tab as MainTabKey)} />;
    }
  };

  const isSubScreen = activeTab === "changePassword" || activeTab === "profile";

  return (
    <View style={styles.container}>
      <View style={styles.contentArea}>{renderContent()}</View>

      {/* Thanh điều hướng 5 Tab chính */}
      {!isSubScreen && (
        <View
          style={[styles.tabBar, { paddingBottom: Math.max(insets.bottom, spacing.xs) }]}
          accessibilityRole="tablist"
        >
          <TouchableOpacity
            style={[styles.tabButton, activeTab === "overview" && styles.tabButtonActive]}
            onPress={() => setActiveTab("overview")}
            accessibilityRole="tab"
            accessibilityLabel="Màn hình Tổng quan"
            accessibilityState={{ selected: activeTab === "overview" }}
          >
            <Text
              style={[styles.tabText, activeTab === "overview" && styles.tabTextActive]}
              allowFontScaling={true}
            >
              Tổng quan
            </Text>
          </TouchableOpacity>

          <TouchableOpacity
            style={[styles.tabButton, activeTab === "attendance" && styles.tabButtonActive]}
            onPress={() => setActiveTab("attendance")}
            accessibilityRole="tab"
            accessibilityLabel="Màn hình Chấm công"
            accessibilityState={{ selected: activeTab === "attendance" }}
          >
            <Text
              style={[styles.tabText, activeTab === "attendance" && styles.tabTextActive]}
              allowFontScaling={true}
            >
              Chấm công
            </Text>
          </TouchableOpacity>

          <TouchableOpacity
            style={[styles.tabButton, activeTab === "leaves" && styles.tabButtonActive]}
            onPress={() => setActiveTab("leaves")}
            accessibilityRole="tab"
            accessibilityLabel="Màn hình Nghỉ phép"
            accessibilityState={{ selected: activeTab === "leaves" }}
          >
            <Text
              style={[styles.tabText, activeTab === "leaves" && styles.tabTextActive]}
              allowFontScaling={true}
            >
              Nghỉ phép
            </Text>
          </TouchableOpacity>

          <TouchableOpacity
            style={[styles.tabButton, activeTab === "payroll" && styles.tabButtonActive]}
            onPress={() => setActiveTab("payroll")}
            accessibilityRole="tab"
            accessibilityLabel="Màn hình Lương"
            accessibilityState={{ selected: activeTab === "payroll" }}
          >
            <Text
              style={[styles.tabText, activeTab === "payroll" && styles.tabTextActive]}
              allowFontScaling={true}
            >
              Bảng lương
            </Text>
          </TouchableOpacity>

          <TouchableOpacity
            style={[styles.tabButton, activeTab === "account" && styles.tabButtonActive]}
            onPress={() => setActiveTab("account")}
            accessibilityRole="tab"
            accessibilityLabel="Màn hình Tài khoản"
            accessibilityState={{ selected: activeTab === "account" }}
          >
            <Text
              style={[styles.tabText, activeTab === "account" && styles.tabTextActive]}
              allowFontScaling={true}
            >
              Tài khoản
            </Text>
          </TouchableOpacity>
        </View>
      )}
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: colors.background,
  },
  contentArea: {
    flex: 1,
  },
  tabBar: {
    minHeight: 56,
    backgroundColor: colors.surface,
    borderTopWidth: 1,
    borderTopColor: colors.border,
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-around",
    paddingTop: spacing.xs,
  },
  tabButton: {
    flex: 1,
    minHeight: spacing.minTouchTarget,
    alignItems: "center",
    justifyContent: "center",
    paddingVertical: spacing.xs,
  },
  tabButtonActive: {
    borderTopWidth: 2,
    borderTopColor: colors.primary,
  },
  tabText: {
    ...typography.captionMedium,
    color: colors.textSecondary,
    fontSize: 11,
  },
  tabTextActive: {
    color: colors.primary,
    fontWeight: "700",
  },
});
