import { defineConfig } from "eslint/config";
import adminConfig from "./apps/admin-web/eslint.config.mjs";
import clientConfig from "./apps/client-web/eslint.config.mjs";
import groomerConfig from "./apps/groomer-web/eslint.config.mjs";

export default defineConfig([
  ...adminConfig,
  ...clientConfig,
  ...groomerConfig,
]);
