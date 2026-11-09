// @ts-check
const eslint = require('@eslint/js');
const { defineConfig, globalIgnores } = require('eslint/config');
const tseslint = require('typescript-eslint');
const angular = require('angular-eslint');
const prettier = require('eslint-config-prettier');

module.exports = defineConfig([
  // src/app/api: code sinh bởi ng-openapi-gen – không lint, không sửa tay.
  globalIgnores(['src/app/api/**', 'dist/**', '.angular/**', 'coverage/**']),
  {
    files: ['**/*.ts'],
    extends: [
      eslint.configs.recommended,
      tseslint.configs.recommended,
      tseslint.configs.stylistic,
      angular.configs.tsRecommended,
      prettier,
    ],
    processor: angular.processInlineTemplates,
    rules: {
      '@angular-eslint/directive-selector': [
        'error',
        // 'co': directive phân quyền dạng *coQuyen (CLAUDE.md / Phần 3).
        { type: 'attribute', prefix: ['app', 'co'], style: 'camelCase' },
      ],
      '@angular-eslint/component-selector': [
        'error',
        { type: 'element', prefix: 'app', style: 'kebab-case' },
      ],
      // CLAUDE.md §6: mọi component OnPush, dùng inject() thay constructor injection.
      '@angular-eslint/prefer-on-push-component-change-detection': 'error',
      '@angular-eslint/prefer-inject': 'error',
      '@typescript-eslint/no-explicit-any': 'error',
    },
  },
  {
    files: ['**/*.html'],
    extends: [angular.configs.templateRecommended, angular.configs.templateAccessibility],
    rules: {
      '@angular-eslint/template/prefer-control-flow': 'error',
      // Component nhập liệu của NG-ZORRO được coi là control khi nằm trong <label>.
      '@angular-eslint/template/label-has-associated-control': [
        'error',
        {
          controlComponents: [
            'nz-select',
            'nz-input-number',
            'nz-date-picker',
            'nz-switch',
            'nz-radio-group',
            'nz-checkbox-group',
            'nz-upload',
          ],
        },
      ],
    },
  },
]);
