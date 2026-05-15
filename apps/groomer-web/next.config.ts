import type { NextConfig } from "next";

const nextConfig: NextConfig = {
    reactStrictMode: true,
    output: "standalone",
    transpilePackages: ["@tailbook/frontend-api"]
};

export default nextConfig;
