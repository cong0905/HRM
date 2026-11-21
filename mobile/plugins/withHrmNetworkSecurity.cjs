const { withAndroidManifest } = require('expo/config-plugins');

module.exports = function withHrmNetworkSecurity(config) {
  return withAndroidManifest(config, (mod) => {
    const application = mod.modResults.manifest.application[0];
    application.$['android:usesCleartextTraffic'] = 'false';
    return mod;
  });
};
