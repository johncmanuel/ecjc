import type { NextConfig } from "next";

const backendUrl =
	process.env.NODE_ENV === "production"
		? (process.env.API_URL ?? "")
		: "http://localhost:5186";

const nextConfig: NextConfig = {
	async rewrites() {
		return [
			{
				source: "/health",
				destination: `${backendUrl}/health`,
			},
			{
				source: "/api/:path*",
				destination: `${backendUrl}/api/:path*`,
			},
		];
	},
};

export default nextConfig;

// Enable calling `getCloudflareContext()` in `next dev`.
// See https://opennext.js.org/cloudflare/bindings#local-access-to-bindings.
import { initOpenNextCloudflareForDev } from "@opennextjs/cloudflare";
initOpenNextCloudflareForDev();
