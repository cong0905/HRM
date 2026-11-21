export type AuthStackParamList = {
  Login: undefined;
  ForgotPassword: undefined;
  ResetPassword: { userId?: string; code?: string } | undefined;
  ActivateAccount: { userId?: string; code?: string } | undefined;
};

export type MainTabParamList = {
  Overview: undefined;
  Attendance: undefined;
  Leaves: undefined;
  Payroll: undefined;
  Profile: undefined;
  AccountSettings: undefined;
  ChangePassword: undefined;
};

export type RootRoute = "Loading" | "Auth" | "Main" | "Offline";
