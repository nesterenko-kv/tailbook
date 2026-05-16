import { defineConfig } from "vitest/config";
import path from "path";

export default defineConfig({
    test: {
        include: ["lib/**/*.test.ts", "app/**/*.test.tsx"],
        environment: "node",
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
