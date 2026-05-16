import { defineConfig } from "vitest/config";

export default defineConfig({
    test: {
        include: ["lib/**/*.test.ts", "app/**/*.test.tsx"],
        environment: "node",
        coverage: {
            provider: "v8",
            include: ["lib/**", "app/**"],
            exclude: ["**/*.test.*", "**/*.spec.*"],
            reporter: ["text", "html", "lcov"]
        }
    }
});
