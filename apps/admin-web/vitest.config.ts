import { defineConfig } from "vitest/config";
import path from "path";

export default defineConfig({
    test: {
        include: ["lib/**/*.test.ts", "app/**/*.test.tsx"],
        environment: "node",
        setupFiles: ["./lib/vitest-setup.ts"]
    },
    resolve: {
        alias: {
            "@": path.resolve(__dirname)
        }
    }
});
