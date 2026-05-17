import { defineConfig } from "vitest/config";
import react from "@vitejs/plugin-react";
import path from "path";

export default defineConfig({
    plugins: [react()],
    test: {
        include: ["lib/**/*.test.ts", "app/**/*.test.tsx", "components/**/*.test.tsx"],
        environment: "jsdom",
        setupFiles: ["./lib/vitest-setup.ts"],
        coverage: {
            provider: "v8",
            include: ["lib/**", "app/**"],
            exclude: ["**/*.test.*", "**/*.spec.*", "lib/vitest-setup.ts"],
            reporter: ["text", "html", "lcov"]
        }
    },
    resolve: {
        alias: {
            "@": path.resolve(__dirname)
        }
    }
});
