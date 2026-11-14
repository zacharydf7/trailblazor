// Chromium on macOS reports "macOS", and other browsers "MacIntel", "iPhone" or "iPad". It's read on each call, so a
// test can fake the platform after the page loads.
export const isApple = () => /mac|iphone|ipad/i.test(navigator.userAgentData?.platform ?? navigator.platform);
