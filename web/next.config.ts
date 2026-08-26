import type { NextConfig } from "next";

// ponytail: watchOptions.poll — VM inotify cap (128 instances) exhausts watchpack;
//           polling trades CPU for watchers. Remove when this runs in k3s.
const nextConfig: NextConfig = {
  webpack: (config) => {
    config.watchOptions = { poll: 1000, aggregateTimeout: 300 };
    return config;
  },
};

export default nextConfig;
