import type { MetadataRoute } from "next";

export default function manifest(): MetadataRoute.Manifest {
  return {
    name: "AgroMarket — B2B Direct Produce",
    short_name: "AgroMarket",
    description:
      "Managed B2B agricultural marketplace connecting local farmers directly with restaurants and cafes.",
    start_url: "/",
    display: "standalone",
    background_color: "#FAF9F6",
    theme_color: "#1B4332",
    icons: [
      {
        src: "/icon.svg",
        sizes: "any",
        type: "image/svg+xml",
      },
    ],
  };
}
