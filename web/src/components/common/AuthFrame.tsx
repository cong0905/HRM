import React from "react";

export const AuthFrame: React.FC<React.PropsWithChildren> = ({ children }) => (
  <div className="auth-page">
    <header className="auth-brand"><strong>HRM Next</strong><span>Quản lý nhân sự</span></header>
    <main className="auth-main">{children}</main>
    <footer className="auth-footer">Nhân sự và tiền lương</footer>
  </div>
);
