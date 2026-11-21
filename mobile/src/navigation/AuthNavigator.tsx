import React, { useEffect, useState } from "react";
import { BackHandler, View, StyleSheet } from "react-native";
import { LoginScreen } from "../screens/auth/LoginScreen";
import { ForgotPasswordScreen } from "../screens/auth/ForgotPasswordScreen";
import { ResetPasswordScreen } from "../screens/auth/ResetPasswordScreen";
import { ActivateAccountScreen } from "../screens/auth/ActivateAccountScreen";

type AuthScreenName = "Login" | "ForgotPassword" | "ResetPassword" | "ActivateAccount";

export const AuthNavigator: React.FC = () => {
  const [currentScreen, setCurrentScreen] = useState<AuthScreenName>("Login");
  useEffect(() => {
    const handler = BackHandler.addEventListener('hardwareBackPress', () => {
      if (currentScreen === 'Login') return false;
      setCurrentScreen('Login');
      return true;
    });
    return () => handler.remove();
  }, [currentScreen]);

  switch (currentScreen) {
    case "ForgotPassword":
      return (
        <View style={styles.container}>
          <ForgotPasswordScreen
            onBack={() => setCurrentScreen("Login")}
            onNavigateReset={() => setCurrentScreen("ResetPassword")}
          />
        </View>
      );
    case "ResetPassword":
      return (
        <View style={styles.container}>
          <ResetPasswordScreen
            onBack={() => setCurrentScreen("Login")}
            onSuccess={() => setCurrentScreen("Login")}
          />
        </View>
      );
    case "ActivateAccount":
      return (
        <View style={styles.container}>
          <ActivateAccountScreen
            onBack={() => setCurrentScreen("Login")}
            onSuccess={() => setCurrentScreen("Login")}
          />
        </View>
      );
    case "Login":
    default:
      return (
        <View style={styles.container}>
          <LoginScreen
            onNavigateForgotPassword={() => setCurrentScreen("ForgotPassword")}
            onNavigateActivateAccount={() => setCurrentScreen("ActivateAccount")}
            onNavigateResetPassword={() => setCurrentScreen("ResetPassword")}
          />
        </View>
      );
  }
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
  },
});
