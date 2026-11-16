import type { NextConfig } from "next";

const rawBasePath = process.env.NEXT_PUBLIC_BASE_PATH?.trim() || "";
const normalizedBasePath =
  rawBasePath && rawBasePath !== "/"
    ? `${rawBasePath.startsWith("/") ? "" : "/"}${rawBasePath.replace(/\/+$/, "")}`
    : "";

const nextConfig: NextConfig = {
  output: process.env.NEXT_OUTPUT_MODE === "standalone" ? "standalone" : "export",
  trailingSlash: true,
  ...(normalizedBasePath ? { basePath: normalizedBasePath } : {}),
  images: {
    unoptimized: true,
  },
  turbopack: {
    rules: {
      "*.css": {
        loaders: ["@tailwindcss/turbopack"],
        as: "*.css",
      },
    },
  },
};

export default nextConfig;
