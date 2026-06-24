import js from '@eslint/js'
import globals from 'globals'
import reactHooks from 'eslint-plugin-react-hooks'
import reactRefresh from 'eslint-plugin-react-refresh'
import tseslint from 'typescript-eslint'
import { defineConfig, globalIgnores } from 'eslint/config'

export default defineConfig([
  globalIgnores(['dist', 'src/router.ts']),
  {
    files: ['**/*.{ts,tsx}'],
    extends: [
      js.configs.recommended,
      tseslint.configs.recommended,
      reactHooks.configs.flat.recommended,
      reactRefresh.configs.vite,
    ],
    languageOptions: {
      globals: globals.browser,
    },
  },
  {
    // shadcn/ui primitives are owned but generated; they co-export variant
    // helpers (e.g. buttonVariants) alongside components by design.
    files: ['src/components/ui/**/*.{ts,tsx}'],
    rules: {
      'react-refresh/only-export-components': 'off',
    },
  },
  {
    // These shell files use `let` holders that a feature's build-time conditional guard assigns or
    // reassigns. The holder can't be `const` (the same source must compile in the full repo and in each
    // edition's strip), and the object-based rewrite that satisfies these rules either can't be processed
    // by the template engine across multiple guards in one file or doesn't fit a non-node value. Scope off
    // the rules that fire only because the guarded assignment is stripped in some editions; every other
    // file uses the clean const-holder pattern.
    files: [
      'src/components/app/app-topbar.tsx',
      'src/components/app/nav.tsx',
      'src/components/app/shell-brand.tsx',
      'src/pages/(app)/_layout.tsx',
    ],
    rules: {
      'prefer-const': 'off',
      'no-useless-assignment': 'off',
      'no-unassigned-vars': 'off',
    },
  },
])
