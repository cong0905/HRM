import { registerRootComponent } from "expo";
import App from "./App";

// registerRootComponent gọi AppRegistry.registerComponent('main', () => App);
// Đảm bảo môi trường Expo Go hoặc standalone native build đều khởi chạy đúng cấu hình.
registerRootComponent(App);
