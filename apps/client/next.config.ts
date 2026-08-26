import type { NextConfig } from "next";
import path from "path";
import { loadEnvConfig } from "@next/env";

// Load environment variables from the root workspace directory
loadEnvConfig(path.resolve(process.cwd(), "../../"));

const backendUrl =
	process.env.NODE_ENV === "production"
		? (process.env.API_URL ?? "")
		: "http://localhost:5186";

const centrifugoUrl =
	process.env.NODE_ENV === "production"
		? "http://centrifugo:8000"
		: "http://localhost:8000";

const nextConfig: NextConfig = {
	turbopack: {
		root: path.resolve(__dirname, "../../"),
	},
	async rewrites() {
		return [
			{
				source: "/connection/:path*",
				destination: `${centrifugoUrl}/connection/:path*`,
			},
			{
				source: "/health",
				destination: `${backendUrl}/health`,
			},
			{
				source: "/uploads/:path*",
				destination: `${backendUrl}/uploads/:path*`,
			},
			{
				// redirect all paths except /api/auth/* to server 
				source: "/api/:path((?!auth).*)",
				destination: `${backendUrl}/api/:path*`,
			},
		];
	},
};

export default nextConfig;
